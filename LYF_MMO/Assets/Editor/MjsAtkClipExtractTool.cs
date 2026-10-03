using UnityEditor;
using UnityEngine;

/// <summary>
/// MJS 攻击/技能动画批量提取工具。
/// 第一步：把 MJS/Atk 目录下指定 FBX 内自定义剪辑的 name 改成与 FBX 同名，
///         仅改 name 字段，takeName、帧范围、循环、mask、事件等其余导入设置原样保留。
/// 第二步：重命名并重新导入后，加载 FBX 内的 AnimationClip 子资产，
///         用 Object.Instantiate 复制一份并在同目录创建同名独立 .anim，
///         效果等同在 Project 窗口对 FBX 内剪辑按 Ctrl+D。
/// 已存在同名 .anim 的文件会被跳过，避免覆盖已有产物（如 Anim_JX_Atk01.anim）。
/// 同时提供一次性自动执行钩子：工程内存在 Assets/Editor/.mjs_extract_pending 哨兵文件时，
/// 脚本重载后自动运行一次并删除哨兵，便于在无法手动点击菜单时由外部触发。
/// </summary>
[InitializeOnLoad]
public static class MjsAtkClipExtractTool
{
    /// <summary>
    /// 一次性自动执行哨兵文件的工程相对路径。
    /// 该文件存在时，每次脚本域重载都会尝试自动执行；执行成功后删除，保证只跑一次。
    /// </summary>
    private const string AutoRunSentinelPath = "Assets/Editor/.mjs_extract_pending";

    /// <summary>
    /// 脚本域重载（含首次进入编辑器、编译完成）后由 Unity 自动调用的静态构造函数。
    /// 仅在哨兵文件存在时触发一次批量提取，避免每次重载都重复执行。
    /// </summary>
    static MjsAtkClipExtractTool()
    {
        // 哨兵不存在说明不是自动触发场景，直接返回，保留菜单手动执行能力。
        if (!System.IO.File.Exists(AutoRunSentinelPath))
        {
            return;
        }

        // 先删除哨兵，即使本次执行异常也不会在下次重载时无限重试。
        System.IO.File.Delete(AutoRunSentinelPath);

        // 延迟到编辑器空闲时再执行，避免在脚本域重载流程中直接操作资源数据库。
        EditorApplication.delayCall += () => ExtractAll();
    }

    /// <summary>
    /// 需要处理的 FBX 文件名清单（含扩展名）。
    /// 注意 Anim_JX_Skill04 扩展名是小写 .fbx，其余为大写 .FBX；
    /// Anim_JX_Atk01 已有独立 .anim 且剪辑已规范，按要求不纳入本清单。
    /// </summary>
    private static readonly string[] FbxFileNames = new string[]
    {
        "Anim_JX_Atk02.FBX",
        "Anim_JX_Atk03.FBX",
        "Anim_JX_Skill01.FBX",
        "Anim_JX_Skill02.FBX",
        "Anim_JX_Skill03.FBX",
        "Anim_JX_Skill04.fbx",
    };

    /// <summary>
    /// 目标 FBX 所在目录的工程相对路径（相对于 Unity 工程根目录）。
    /// </summary>
    private const string TargetDir = "Assets/GM_Game/Animas/Role/MJS/Atk";

    /// <summary>
    /// 菜单入口：依次完成“重命名 FBX 内剪辑”与“提取独立 .anim”两步，最后统一保存并刷新资源数据库。
    /// </summary>
    [MenuItem("Tools/动画/批量提取MJS攻击技能剪辑")]
    public static void ExtractAll()
    {
        // 逐个处理清单中的 FBX。
        for (int i = 0; i < FbxFileNames.Length; i++)
        {
            // 当前要处理的 FBX 文件名，例如 Anim_JX_Atk02.FBX。
            string fbxFileName = FbxFileNames[i];

            // 目标剪辑名 = FBX 去掉扩展名后的文件名，例如 Anim_JX_Atk02。
            string clipName = System.IO.Path.GetFileNameWithoutExtension(fbxFileName);

            // FBX 在工程内的完整相对路径。
            string fbxPath = TargetDir + "/" + fbxFileName;

            // 目标独立剪辑资产路径，与 FBX 同目录、同名、扩展名 .anim。
            string animPath = TargetDir + "/" + clipName + ".anim";

            // 同名 .anim 已存在则跳过，防止覆盖已有产物。
            if (System.IO.File.Exists(animPath))
            {
                Debug.Log("[MjsAtkClipExtract] 同名 .anim 已存在，跳过：" + animPath);
                continue;
            }

            // 第一步：规范 FBX 内剪辑名。
            RenameImporterClip(fbxPath, clipName);

            // 第二步：把重命名后的剪辑复制成独立 .anim。
            ExtractClipToAsset(fbxPath, clipName, animPath);
        }

        // 统一落盘并刷新资源数据库，确保新 .anim 立即可见。
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MjsAtkClipExtract] MJS 攻击/技能剪辑批量提取完成。");
    }

    /// <summary>
    /// 读取指定 FBX 的 ModelImporter，把其自定义剪辑数组中第一个剪辑的 name 改为目标名，
    /// 其余导入设置保持原样，写回后强制重新导入。
    /// </summary>
    /// <param name="fbxPath">FBX 的工程相对路径。</param>
    /// <param name="clipName">目标剪辑名（与 FBX 同名）。</param>
    private static void RenameImporterClip(string fbxPath, string clipName)
    {
        // 获取模型导入器；路径错误或非模型资产时返回 null。
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("[MjsAtkClipExtract] 无法获取 ModelImporter：" + fbxPath);
            return;
        }

        // 读取当前已自定义的剪辑列表。
        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[MjsAtkClipExtract] clipAnimations 为空，无需重命名：" + fbxPath);
            return;
        }

        // 每个 FBX 当前只含一条主剪辑，仅修改其 name 字段；
        // takeName、firstFrame、lastFrame、loopTime、events、mask 等一律不动。
        clips[0].name = clipName;
        importer.clipAnimations = clips;

        // 若检测到多条剪辑，记录提示，避免误把其余剪辑也改成同名。
        if (clips.Length > 1)
        {
            Debug.LogWarning("[MjsAtkClipExtract] 该 FBX 含多条剪辑，仅重命名第一条：" + fbxPath);
        }

        // 把导入设置写回磁盘并强制重新导入，使新剪辑名在子资产上生效。
        AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
        AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// 加载 FBX 的全部子资产，过滤掉名字以 "__" 开头的内部/预览剪辑，
    /// 找到与目标名匹配的 AnimationClip，复制为独立资产写入 animPath。
    /// </summary>
    /// <param name="fbxPath">FBX 的工程相对路径。</param>
    /// <param name="clipName">要提取的剪辑名。</param>
    /// <param name="animPath">生成的 .anim 工程相对路径。</param>
    private static void ExtractClipToAsset(string fbxPath, string clipName, string animPath)
    {
        // 取 FBX 下所有子资产（含网格、材质、动画剪辑等）。
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

        // 在子资产中按名字定位目标 AnimationClip。
        AnimationClip sourceClip = null;
        for (int i = 0; i < subAssets.Length; i++)
        {
            // 只处理 AnimationClip 类型，跳过网格/材质等其他子资产。
            AnimationClip clip = subAssets[i] as AnimationClip;
            if (clip == null)
            {
                continue;
            }

            // 过滤名字以 "__" 开头的内部/预览剪辑（例如 __preview__xxx）。
            string assetName = clip.name;
            if (assetName.StartsWith("__"))
            {
                continue;
            }

            // 命中目标剪辑后记录并停止查找。
            if (assetName == clipName)
            {
                sourceClip = clip;
                break;
            }
        }

        if (sourceClip == null)
        {
            Debug.LogError("[MjsAtkClipExtract] 未找到目标剪辑：" + clipName + " @ " + fbxPath);
            return;
        }

        // 在内存中复制一份剪辑，避免直接改动 FBX 内的原始剪辑资产。
        AnimationClip clone = Object.Instantiate(sourceClip) as AnimationClip;
        if (clone == null)
        {
            Debug.LogError("[MjsAtkClipExtract] 实例化剪辑失败：" + clipName);
            return;
        }

        // 把克隆体命名为目标名，再作为独立资产写入磁盘。
        clone.name = clipName;
        AssetDatabase.CreateAsset(clone, animPath);
        Debug.Log("[MjsAtkClipExtract] 已生成独立剪辑：" + animPath);
    }
}
