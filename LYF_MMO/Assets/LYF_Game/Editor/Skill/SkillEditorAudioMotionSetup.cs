using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ethan.ActionEditor;

/// <summary>为魔剑士技能及走跑跳动作配置课程音效，并沿原作者音轨导出运行数据。</summary>
public static class SkillEditorAudioMotionSetup
{

    #region 字段
    /// <summary>第 3 章课程音效的 Unity 资源根目录，用于加载攻击、技能及脚步等剪辑。</summary>
    private const string AudioDirectory = "Assets/LYF_Game/Audios/Shared/CourseChapter03/";

    /// <summary>当前正式角色普通动作的真实动画资源目录。</summary>
    private const string CommonAnimationDirectory = "Assets/LYF_Game/Animas/Role/MJS/Common/ALL/";

    /// <summary>七个已有表现行，不新增主技能或表格记录。</summary>
    private static readonly int[] PresentationIds = { 3001001, 3001002, 3001003, 3004001, 3005001, 3006001, 3007001 };

    /// <summary>八个普通动作作者文件后缀，含战斗持剑走跑与二、三段起跳，不作为服务器技能 ID。</summary>
    private static readonly string[] LocomotionNames = { "Walk", "Run", "JumpStart", "JumpEnd", "BattleWalk", "BattleRun", "JumpStart2", "JumpStart3" };

    /// <summary>普通动作对应的原完整剪辑文件名，不改原 Animator 资源；末两条是魔剑士二、三段起跳剪辑。</summary>
    private static readonly string[] LocomotionClips = { "Anim_MJS_Walk.anim", "Anim_MJS_Run_F.anim", "Anim_JX_Jump_Start.anim", "Anim_JX_Jump_End.anim", "Anim_MJS_Walk_ZD.anim", "Anim_MJS_Run_F_ZD.anim", "Anim_MJS_Jump_2.anim", "Anim_MJS_Jump_3.anim" };
    #endregion

    #region 音频与移动技能配置装配导出
    /// <summary>追加固定音量单次声音，重复执行时同一帧同一声音不会再次插入。</summary>
    /// <param name="config">接收新增声音的原作者对象。</param>
    /// <param name="fileName">以 Assets/ 开头的完整音源路径，或课程音效根目录下的相对资源路径。</param>
    /// <param name="frame">明确的时间轴触发帧，零起始。</param>
    /// <param name="volume">制作时固定音量，范围零至一。</param>
    private static void AddAudio(SkillConfigSO config, string fileName, int frame, float volume)
    {
        // 新录制的完整 Assets/ 路径直接使用，旧课程素材仍沿用相对根目录的写法。
        string assetPath = fileName;
        if (!fileName.StartsWith("Assets/"))
        {
            assetPath = AudioDirectory + fileName;
        }
        // Unity 解析实际音频身份，禁止猜测 GUID 或 fileID。
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null) throw new InvalidOperationException("课程音效尚未导入：" + assetPath);
        // 原 Y 声音事件列表，通过序列化边界保留所有已有事件引用。
        SerializedObject serialized = new SerializedObject(config);
        // 当前粒子与声音混合列表，新增时只扩展尾部。
        SerializedProperty effects = serialized.FindProperty("fxList");
        // 幂等检查索引，不重排或覆盖已有粒子/声音。
        for (int index = 0; index < effects.arraySize; index++)
        {
            // 已有事件，资源和帧同时相等即保留其原音量及其他用户参数。
            SerializedProperty existing = effects.GetArrayElementAtIndex(index);
            if (existing.FindPropertyRelative("contentKind").intValue == 2 && existing.FindPropertyRelative("audioClip").objectReferenceValue == clip && existing.FindPropertyRelative("keyNumber").intValue == frame) return;
        }
        effects.arraySize++;
        // Unity 扩展数组可能复制前一项，因此逐项初始化声音结构，不能继承前一粒子的资源和变换。
        SerializedProperty item = effects.GetArrayElementAtIndex(effects.arraySize - 1);
        item.FindPropertyRelative("contentKind").intValue = 2;
        item.FindPropertyRelative("keyNumber").intValue = frame;
        item.FindPropertyRelative("endKeyNumber").intValue = 0;
        item.FindPropertyRelative("particleSystem").objectReferenceValue = null;
        item.FindPropertyRelative("audioClip").objectReferenceValue = clip;
        item.FindPropertyRelative("offset").vector3Value = Vector3.zero;
        item.FindPropertyRelative("socketPath").stringValue = "";
        item.FindPropertyRelative("placementKind").intValue = 0;
        item.FindPropertyRelative("forwardDistance").floatValue = 0f;
        item.FindPropertyRelative("followCharacter").boolValue = true;
        item.FindPropertyRelative("rotation").vector3Value = Vector3.zero;
        item.FindPropertyRelative("scale").vector3Value = Vector3.one;
        item.FindPropertyRelative("playbackSpeed").floatValue = 1f;
        item.FindPropertyRelative("useWorldSpace").boolValue = false;
        item.FindPropertyRelative("customLifetime").floatValue = 0f;
        item.FindPropertyRelative("audioRandomize").boolValue = false;
        item.FindPropertyRelative("pitchVariation").floatValue = 0.1f;
        item.FindPropertyRelative("volumeVariation").floatValue = 0.05f;
        item.FindPropertyRelative("baseVolume").floatValue = volume;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EnsureTrack(config, 2);
    }

    /// <summary>将八份普通动作稿单独发布，不改变技能表身份或原角色预制体。</summary>
    [MenuItem("Tools/技能/YCombat/导出MJS走跑跳声音")]
    public static void ExportLocomotion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在非运行模式发布普通动作。");
        // 固定路径由运行缓存加载，与七技能表现共享原 Runtime collector。
        string path = SkillEditorAssetTools.RuntimeDirectory + "/SkillRuntimeLocomotionConfig_MJS.asset";
        // 已存在时修改同一运行配置，保留 GUID 和依赖引用。
        SkillRuntimeLocomotionConfig runtime = AssetDatabase.LoadAssetAtPath<SkillRuntimeLocomotionConfig>(path);
        if (runtime != null && EditorUtility.IsDirty(runtime)) throw new InvalidOperationException("普通动作运行配置存在未保存修改，未覆盖：" + path);
        // 先转换八份作者稿，任何稿件无效时不修改当前运行资产。
        List<SkillRuntimeLocomotionAction> actions = new List<SkillRuntimeLocomotionAction>();
        // Walk/Run/JumpStart/JumpEnd 的稳定顺序与配置字段对应，末尾追加二、三段起跳。
        for (int index = 0; index < LocomotionNames.Length; index++)
        {
            // 已通过 Unity 加载的普通动作作者稿。
            SkillConfigSO config = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(LocomotionAuthoringPath(index));
            if (config == null || EditorUtility.IsDirty(config)) throw new InvalidOperationException("普通动作缺失或存在未保存修改：" + LocomotionNames[index]);
            // 每份普通动作仍沿用原完整动画，不能静默丢弃裁剪、混合或其他制作轨道。
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CommonAnimationDirectory + LocomotionClips[index]);
            ValidateLocomotion(config, clip);
            actions.Add(new SkillRuntimeLocomotionAction { AnimationLocation = AssetDatabase.GetAssetPath(clip), DurationSeconds = clip.length, AudioEvents = SkillEditorAudioMotionTools.ReadAudio(config, clip.length) });
        }
        if (runtime == null)
        {
            runtime = ScriptableObject.CreateInstance<SkillRuntimeLocomotionConfig>();
            AssetDatabase.CreateAsset(runtime, path);
        }
        Undo.RecordObject(runtime, "发布普通动作声音");
        runtime.Walk = actions[0];
        runtime.Run = actions[1];
        runtime.JumpStart = actions[2];
        runtime.JumpEnd = actions[3];
        runtime.BattleWalk = actions[4];
        runtime.BattleRun = actions[5];
        // 二、三段起跳写入新增具名字段，索引六、七对应数组末两条。
        runtime.JumpStart2 = actions[6];
        runtime.JumpStart3 = actions[7];
        EditorUtility.SetDirty(runtime);
        AssetDatabase.SaveAssetIfDirty(runtime);
    }

    /// <summary>为七份技能作者稿及八个走跑跳动作补齐课程音效，并发布对应运行配置。</summary>
    [MenuItem("Tools/技能/YCombat/编入魔剑士课程音效")]
    public static void PrepareAndExport()
    {
        EnsureEditableAssets();
        // 逐个加载原绑定，所有实际资源引用交 Unity 解析。
        for (int index = 0; index < PresentationIds.Length; index++)
        {
            // 本条固定表现行仅用于查找原绑定，不从数字推导技能归属。
            int presentationId = PresentationIds[index];
            // 原作者绑定持有既有 Config 与 SourceClip，修改同一对象以保持 GUID。
            SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(SkillEditorAssetTools.AuthoringDirectory + "/SkillEditorBinding_" + presentationId + ".asset");
            Undo.RecordObject(binding.Config, "编入魔剑士课程音效");
            // 课程剪辑沿用现有挥刀时点；相同剪辑和帧的作者事件由 AddAudio 保留。
            if (presentationId == 3001001) AddAudio(binding.Config, "Attack/role1_attack1.wav", 48, 0.18f);
            else if (presentationId == 3001002) AddAudio(binding.Config, "Attack/role1_attack2.wav", 23, 0.30f);
            else if (presentationId == 3001003)
            {
                AddAudio(binding.Config, "Attack/role1_attack3.wav", 23, 0.18f);
                AddAudio(binding.Config, "Attack/tianshen2_attack2.wav", 54, 0.28f);
            }
            else if (presentationId == 3004001)
            {
                AddAudio(binding.Config, "Attack/tianshen2_attack1.wav", 9, 0.46f);
                AddAudio(binding.Config, "Attack/tianshen2_attack2.wav", 36, 0.27f);
            }
            else if (presentationId == 3005001)
            {
                // R 的释放音随首个粒子时点触发，后续挥刀使用原有 F136。
                AddAudio(binding.Config, "Skill/tianshen8_skill2.wav", FirstFxFrame(binding.Config), 0.23f);
                AddAudio(binding.Config, "Attack/Clip_Test010.wav", 136, 0.27f);
            }
            else if (presentationId == 3006001)
            {
                AddAudio(binding.Config, "Skill/Female_Attack_11_Kimlinh.wav", 37, 0.27f);
                AddAudio(binding.Config, "Attack/tianshen2_attack1.wav", 142, 0.30f);
            }
            else if (presentationId == 3007001)
            {
                // 落剑从零帧蓄力，沿用正式音轨的 F49 冲击点。
                AddAudio(binding.Config, "Skill/JuQi/Clip_Skill05_Juqi.wav", 0, 0.38f);
                AddAudio(binding.Config, "Explosion/K Electric Impact.mp3", 49, 0.85f);
            }
            EditorUtility.SetDirty(binding.Config);
            AssetDatabase.SaveAssetIfDirty(binding.Config);
        }
        PrepareLocomotion();
        SkillEditorAssetTools.ExportAll();
        ExportLocomotion();
        SkillEditorAssetTools.ValidateAll();
        Debug.Log("已编入魔剑士课程音效，涵盖技能、走路、跑步、起跳和落地。");
    }

    /// <summary>发布前复用正式单动画与字段白名单校验，避免普通动作静默忽略未接入轨道。</summary>
    /// <param name="config">已保存的普通动作作者稿。</param>
    /// <param name="clip">对应的原完整动作剪辑。</param>
    private static void ValidateLocomotion(SkillConfigSO config, AnimationClip clip)
    {
        if (clip == null || config.skillClip != clip || config.skillID != 0) throw new InvalidOperationException("普通动作不能伪装成技能或替换原动画：" + config.name);
        // 只在内存构造验证绑定，不保存为新技能绑定，以免改变七技能数量。
        SkillEditorBinding temporary = ScriptableObject.CreateInstance<SkillEditorBinding>();
        try
        {
            temporary.Config = config;
            temporary.SourceClip = clip;
            temporary.PreviewModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LYF_Game/Prefabs/Role/Role_MJS.prefab");
            SkillEditorAssetTools.ValidateAuthoring(temporary);
            if (SkillEditorAudioMotionTools.ReadCharge(config, clip.length).Count != 0) throw new InvalidOperationException("走跑跳位移由原角色移动组件控制：" + config.name);
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
    }

    /// <summary>创建缺失普通动作稿，已有稿不重置，声音事件通过同一技能编辑器编辑。</summary>
    private static void PrepareLocomotion()
    {
        // 三段起跳各自的视频截取录音，按段号依次对应一段、二段、三段。
        string[] jumpAudios = {
            "Assets/LYF_Game/Audios/Characters/MJS/Jump/Jump_Stage01_Yimeng.wav",
            "Assets/LYF_Game/Audios/Characters/MJS/Jump/Jump_Stage02_Yimeng.wav",
            "Assets/LYF_Game/Audios/Characters/MJS/Jump/Jump_Stage03_Yimeng.wav"
        };
        // 八份作者稿均沿用原动画资源，每份仅新增声音制作数据。
        for (int index = 0; index < LocomotionNames.Length; index++)
        {
            // 本条作者资产路径，没有数字主技能身份。
            string path = LocomotionAuthoringPath(index);
            // 已有作者稿的对象引用，绝不重新创建覆盖用户资产。
            SkillConfigSO config = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(path);
            if (config != null) continue;
            // 普通动作完整动画，真实 fileID/GUID 由 Unity API 确定。
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CommonAnimationDirectory + LocomotionClips[index]);
            if (clip == null) throw new InvalidOperationException("原普通动作动画缺失：" + LocomotionClips[index]);
            config = ScriptableObject.CreateInstance<SkillConfigSO>();
            config.name = "SkillConfigSO_MJS_" + LocomotionNames[index];
            config.skillName = LocomotionNames[index];
            config.skillID = 0;
            config.previewModelName = "Role_MJS";
            config.timingVersion = 1;
            config.timelineFrameRate = 120f;
            config.skillClip = clip;
            // 第三方 Global 嵌套类型在序列化边界初始化，避免游戏同名类型冲突。
            SerializedObject serialized = new SerializedObject(config);
            // 唯一完整动画段，时间轴零帧开始且不裁剪。
            SerializedProperty animations = serialized.FindProperty("animSegments");
            animations.arraySize = 1;
            animations.GetArrayElementAtIndex(0).FindPropertyRelative("clip").objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureTrack(config, 0);
            EnsureTrack(config, 2);
            // 第一段起跳改用视频截取的一段落录音，零帧触发且固定音量。
            if (index == 2) AddAudio(config, jumpAudios[0], 0, 0.65f);
            // 第二段起跳改用独立的二段录音，与一段各播各的。
            else if (index == 6) AddAudio(config, jumpAudios[1], 0, 0.65f);
            // 第三段起跳改用独立的三段录音，索引七不会落入脚步分支。
            else if (index == 7) AddAudio(config, jumpAudios[2], 0, 0.65f);
            // 落地声保持原有零帧配置不变。
            else if (index == 3) AddAudio(config, "Footstep/Dirt_footstep8.wav", 0, 0.28f);
            else
            {
                // 根据原脚掌高度曲线的最低接地点制作左右脚步点。
                int leftFrame = FootContactFrame(clip, "LeftFootT.y", config.timelineFrameRate);
                // 另一只脚独立读原曲线，不假定动作从脚步零帧起。
                int rightFrame = FootContactFrame(clip, "RightFootT.y", config.timelineFrameRate);
                AddAudio(config, "Footstep/Dirt_footstep8.wav", leftFrame, 0.28f);
                AddAudio(config, "Footstep/Dirt_footstep9.wav", rightFrame, 0.30f);
            }
            AssetDatabase.CreateAsset(config, path);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }
    }

    /// <summary>从原脚掌相对高度曲线取周期内最低点，作为第一版可编辑脚步点。</summary>
    /// <param name="clip">原完整走路或跑步动画。</param>
    /// <param name="propertyName">原 Humanoid 脚掌高度属性名。</param>
    /// <param name="fps">作者时间轴每秒帧数。</param>
    /// <returns>零起始接触帧，不包括循环末尾与起点重合帧。</returns>
    private static int FootContactFrame(AnimationClip clip, string propertyName, float fps)
    {
        // 实际动画曲线绑定列表，按照原属性名寻找脚掌高度。
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        // 当前找到的脚掌高度曲线，缺失时明确阻止假步点制作。
        AnimationCurve curve = null;
        // 曲线绑定索引，用于寻找左右脚原数据。
        for (int index = 0; index < bindings.Length; index++)
        {
            if (bindings[index].propertyName == propertyName) { curve = AnimationUtility.GetEditorCurve(clip, bindings[index]); break; }
        }
        if (curve == null) throw new InvalidOperationException("普通动作缺少脚掌高度曲线：" + clip.name + "/" + propertyName);
        // 最低脚掌高度，单位沿原 Humanoid 标准化空间，仅比较位置而不换算米。
        float minimum = float.PositiveInfinity;
        // 最低接触位置对应时间轴帧号。
        int bestFrame = 0;
        // 排除循环结尾重复帧，保证每轮只播放一次每条脚步。
        int frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * fps));
        // 按作者帧率采样原曲线，确保返回的是实际可编辑帧。
        for (int frame = 0; frame < frameCount; frame++)
        {
            // 本帧脚掌高度，脚低点用作初始接地拍点。
            float height = curve.Evaluate(frame / fps);
            if (height < minimum) { minimum = height; bestFrame = frame; }
        }
        return bestFrame;
    }

    /// <summary>首次制作前检查全部目标没有用户未保存修改，并要求实际源声音能被 Unity 导入。</summary>
    private static void EnsureEditableAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请退出运行模式后制作动作轨道。");
        // 对全部现有目标先检查，避免碰到后面的脏资产时前半部分已经保存。
        for (int index = 0; index < PresentationIds.Length; index++)
        {
            // 此条已有绑定，用于取得当前作者对象和预览动画。
            SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(SkillEditorAssetTools.AuthoringDirectory + "/SkillEditorBinding_" + PresentationIds[index] + ".asset");
            if (binding == null || binding.Config == null || EditorUtility.IsDirty(binding.Config)) throw new InvalidOperationException("技能绑定缺失或作者稿存在未保存修改：" + PresentationIds[index]);
            SkillEditorAssetTools.Validate(binding);
            // 当前已发布运行资产可能被用户在 Inspector 修改，同样不能覆盖。
            SkillRuntimePresentation runtime = AssetDatabase.LoadAssetAtPath<SkillRuntimePresentation>(SkillEditorAssetTools.RuntimeDirectory + "/SkillRuntimePresentation_" + PresentationIds[index] + ".asset");
            if (runtime != null && EditorUtility.IsDirty(runtime)) throw new InvalidOperationException("技能运行表现存在未保存修改：" + PresentationIds[index]);
        }
        // 普通动作作者稿如果已经存在，保留其中用户编辑内容。
        for (int index = 0; index < LocomotionNames.Length; index++)
        {
            // 本条普通动作作者资源，缺失时稍后新建。
            SkillConfigSO config = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(LocomotionAuthoringPath(index));
            if (config != null && EditorUtility.IsDirty(config)) throw new InvalidOperationException("普通动作存在未保存修改：" + LocomotionNames[index]);
        }
        // 所需音源在写入前全量检查，缺声音不能留下半套已经保存的作者修改。
        string[] sounds = {
            "Attack/role1_attack1.wav", "Attack/role1_attack2.wav", "Attack/role1_attack3.wav",
            "Attack/tianshen2_attack1.wav", "Attack/tianshen2_attack2.wav", "Attack/Clip_Test010.wav",
            "Skill/tianshen8_skill2.wav", "Skill/Female_Attack_11_Kimlinh.wav", "Skill/JuQi/Clip_Skill05_Juqi.wav",
            "Explosion/K Electric Impact.mp3", "Footstep/Dirt_footstep8.wav", "Footstep/Dirt_footstep9.wav",
            // 三段起跳改用视频截取的三条独立录音，不再要求旧课程起跳声。
            "Assets/LYF_Game/Audios/Characters/MJS/Jump/Jump_Stage01_Yimeng.wav",
            "Assets/LYF_Game/Audios/Characters/MJS/Jump/Jump_Stage02_Yimeng.wav",
            "Assets/LYF_Game/Audios/Characters/MJS/Jump/Jump_Stage03_Yimeng.wav"
        };
        // 新建作者音轨所需素材：完整 Assets/ 路径直接检查，其余按 AudioDirectory 拼接。
        for (int index = 0; index < sounds.Length; index++)
        {
            // 本条素材的实际资源路径：完整 Assets/ 路径原样使用，课程相对路径稍后拼接根目录。
            string assetPath = sounds[index];
            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = AudioDirectory + assetPath;
            }
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath) == null) throw new InvalidOperationException("课程音效尚未导入：" + assetPath);
        }
        // 普通动作的运行配置也在全部制作前检查脏状态。
        SkillRuntimeLocomotionConfig locomotion = AssetDatabase.LoadAssetAtPath<SkillRuntimeLocomotionConfig>(SkillEditorAssetTools.RuntimeDirectory + "/SkillRuntimeLocomotionConfig_MJS.asset");
        if (locomotion != null && EditorUtility.IsDirty(locomotion)) throw new InvalidOperationException("普通动作运行配置存在未保存修改，未覆盖。");
    }

    /// <summary>显示原 Y 指定类型轨道，已有同类型行保留显示名和折叠状态。</summary>
    /// <param name="config">当前作者稿。</param>
    /// <param name="type">原 TrackType 固定数值，零动画、二特效声音、五位移。</param>
    private static void EnsureTrack(SkillConfigSO config, int type)
    {
        // 轨道布局属于作者元数据，通过原序列化字段追加。
        SerializedObject serialized = new SerializedObject(config);
        // 当前可见轨道列表，不改变已有布局顺序。
        SerializedProperty tracks = serialized.FindProperty("tracks");
        // 按索引寻找原类型，避免产生同类型重复轨道。
        for (int index = 0; index < tracks.arraySize; index++)
        {
            if (tracks.GetArrayElementAtIndex(index).FindPropertyRelative("type").intValue == type) return;
        }
        tracks.arraySize++;
        // 新布局行重置从上一行复制来的显示名。
        SerializedProperty item = tracks.GetArrayElementAtIndex(tracks.arraySize - 1);
        item.FindPropertyRelative("type").intValue = type;
        item.FindPropertyRelative("displayName").stringValue = "";
        item.FindPropertyRelative("expanded").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>取得当前作者动作的首个粒子帧，声音与现有蓄力/挥刀时点对齐。</summary>
    /// <param name="config">含已有粒子轨道的正式作者稿。</param>
    /// <returns>首个粒子帧；没有粒子时动作零帧触发。</returns>
    private static int FirstFxFrame(SkillConfigSO config)
    {
        // 原粒子/声音列表的第三方序列化边界。
        SerializedObject serialized = new SerializedObject(config);
        // 当前全部表现事件，音效不参与首粒子筛选。
        SerializedProperty effects = serialized.FindProperty("fxList");
        // 最早粒子帧；最大整数作为尚未找到的内部标记。
        int first = int.MaxValue;
        // 遍历实际事件，保留作者既有排序与资源引用。
        for (int index = 0; index < effects.arraySize; index++)
        {
            // 本条原表现事件引用。
            SerializedProperty item = effects.GetArrayElementAtIndex(index);
            if (item.FindPropertyRelative("contentKind").intValue == 1) first = Mathf.Min(first, item.FindPropertyRelative("keyNumber").intValue);
        }
        if (first == int.MaxValue) return 0;
        return first;
    }

    /// <summary>得到八份普通动作的固定作者资产路径，独立于正式技能数字 ID。</summary>
    /// <param name="index">普通动作索引，范围零至七；六、七为二、三段起跳。</param>
    /// <returns>现有作者目录下的完整 Unity 资源路径。</returns>
    private static string LocomotionAuthoringPath(int index)
    {
        return SkillEditorAssetTools.AuthoringDirectory + "/SkillConfigSO_MJS_" + LocomotionNames[index] + ".asset";
    }
    #endregion
}
