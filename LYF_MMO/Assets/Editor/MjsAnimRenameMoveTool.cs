using UnityEditor;
using UnityEngine;

/// <summary>
/// MJS 独立动画剪辑批量重命名并移动工具。
/// 把 MJS/Atk 目录下 7 个独立 .anim（Anim_JX_*）移动到 Common/ALL/Skill 目录，
/// 同时改名为 Anim_MJS_*，并用 AssetDatabase.MoveAsset 一步完成移动+重命名，
/// 使 .meta 随资产一同迁移、GUID 保持不变（Animator 已有引用不会断）。
/// 移动后再加载新路径下的 AnimationClip，把内部 clip.name（即 m_Name）同步改成新名字。
/// 只移动 .anim，FBX 及其 .meta 全部留在 Atk 目录不动。
/// 另提供一次性哨兵自动执行：工程内存在 Assets/Editor/.mjs_rename_move_pending 时，
/// 脚本域重载后自动执行一次并删除哨兵，便于无法手动点菜单时由外部触发。
/// </summary>
[InitializeOnLoad]
public static class MjsAnimRenameMoveTool
{
    /// <summary>
    /// 一次性自动执行哨兵文件的工程相对路径。存在时脚本域重载后自动跑一次并删除。
    /// </summary>
    private const string AutoRunSentinelPath = "Assets/Editor/.mjs_rename_move_pending";

    /// <summary>
    /// 7 个待处理剪辑的旧资产名（不含扩展名），与 NewNames 按下标一一对应。
    /// </summary>
    private static readonly string[] OldNames = new string[]
    {
        "Anim_JX_Atk01",
        "Anim_JX_Atk02",
        "Anim_JX_Atk03",
        "Anim_JX_Skill01",
        "Anim_JX_Skill02",
        "Anim_JX_Skill03",
        "Anim_JX_Skill04",
    };

    /// <summary>
    /// 7 个待处理剪辑的新资产名（不含扩展名），由 JX 改为 MJS。
    /// </summary>
    private static readonly string[] NewNames = new string[]
    {
        "Anim_MJS_Atk01",
        "Anim_MJS_Atk02",
        "Anim_MJS_Atk03",
        "Anim_MJS_Skill01",
        "Anim_MJS_Skill02",
        "Anim_MJS_Skill03",
        "Anim_MJS_Skill04",
    };

    /// <summary>
    /// .anim 当前所在目录的工程相对路径（MJS/Atk）。
    /// </summary>
    private const string SourceDir = "Assets/GM_Game/Animas/Role/MJS/Atk";

    /// <summary>
    /// .anim 移动目标目录的工程相对路径（MJS/Common/ALL/Skill）。
    /// </summary>
    private const string TargetDir = "Assets/GM_Game/Animas/Role/MJS/Common/ALL/Skill";

    /// <summary>
    /// 脚本域重载后由 Unity 自动调用的静态构造函数。
    /// 仅在哨兵文件存在时触发一次批量移动重命名，执行前删除哨兵防止重复跑。
    /// </summary>
    static MjsAnimRenameMoveTool()
    {
        // 哨兵不存在说明是普通重载，直接返回，保留菜单手动执行能力。
        if (!System.IO.File.Exists(AutoRunSentinelPath))
        {
            return;
        }

        // 先删哨兵，即使本次执行异常也不会在下次重载无限重试。
        System.IO.File.Delete(AutoRunSentinelPath);

        // 延迟到编辑器空闲再执行，避免在脚本域重载流程中直接操作资源数据库。
        EditorApplication.delayCall += () => MoveAndRenameAll();
    }

    /// <summary>
    /// 菜单入口：逐个把旧 .anim 移动重命名到目标目录，并同步内部 clip.name。
    /// </summary>
    [MenuItem("Tools/动画/重命名并移动MJS技能剪辑")]
    public static void MoveAndRenameAll()
    {
        // 遍历旧/新名字配对数组。
        for (int i = 0; i < OldNames.Length; i++)
        {
            // 当前剪辑旧资产名，例如 Anim_JX_Atk01。
            string oldName = OldNames[i];
            // 当前剪辑新资产名，例如 Anim_MJS_Atk01。
            string newName = NewNames[i];

            // 旧 .anim 的完整工程路径。
            string oldPath = SourceDir + "/" + oldName + ".anim";
            // 新 .anim 的完整工程路径（含新文件名）。
            string newPath = TargetDir + "/" + newName + ".anim";

            // 旧资产不存在则跳过，避免 MoveAsset 报错。
            if (!System.IO.File.Exists(oldPath))
            {
                Debug.LogWarning("[MjsAnimRenameMove] 旧 .anim 不存在，跳过：" + oldPath);
                continue;
            }

            // 目标路径已存在同名资产则跳过，防止覆盖。
            if (System.IO.File.Exists(newPath))
            {
                Debug.LogWarning("[MjsAnimRenameMove] 目标已存在同名资产，跳过：" + newPath);
                continue;
            }

            // 一步完成移动+重命名；.meta 随之迁移，GUID 保持不变。
            string moveError = AssetDatabase.MoveAsset(oldPath, newPath);
            // MoveAsset 返回空字符串表示成功，非空为错误信息。
            if (!string.IsNullOrEmpty(moveError))
            {
                Debug.LogError("[MjsAnimRenameMove] MoveAsset 失败：" + oldPath + " -> " + newPath + "，原因：" + moveError);
                continue;
            }

            // 移动成功后加载新路径下的剪辑，把内部 m_Name 同步改成新名字。
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
            if (clip == null)
            {
                Debug.LogError("[MjsAnimRenameMove] 移动后加载剪辑失败：" + newPath);
                continue;
            }

            // 内部剪辑名与文件名保持一致。
            clip.name = newName;
            EditorUtility.SetDirty(clip);
            Debug.Log("[MjsAnimRenameMove] 已移动并重命名：" + oldName + " -> " + newName);
        }

        // 统一落盘并刷新资源数据库。
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MjsAnimRenameMove] MJS 技能剪辑重命名移动完成。");
    }
}
