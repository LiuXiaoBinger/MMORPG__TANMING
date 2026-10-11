using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ethan.ActionEditor;

/// <summary>显式制作 MJS 技能及普通动作的声音和动画位移轨道，保留原粒子、用户位移段和资产身份。</summary>
public static class SkillEditorAudioMotionSetup
{
    /// <summary>MJS 角色的 RO 解码声音根目录；Movement、Jump 和 Skills 子目录及文件名标明动作用途。</summary>
    private const string AudioDirectory = "Assets/GM_Game/Audios/Characters/MJS/";
    /// <summary>当前正式角色普通动作的真实动画资源目录。</summary>
    private const string CommonAnimationDirectory = "Assets/GM_Game/Animas/Role/MJS/Common/ALL/";
    /// <summary>七个已有表现行，不新增主技能或表格记录。</summary>
    private static readonly int[] PresentationIds = { 3001001, 3001002, 3001003, 3004001, 3005001, 3006001, 3007001 };
    /// <summary>六个普通动作作者文件后缀，包含战斗持剑走跑，不作为服务器技能 ID。</summary>
    private static readonly string[] LocomotionNames = { "Walk", "Run", "JumpStart", "JumpEnd", "BattleWalk", "BattleRun" };
    /// <summary>普通动作对应的原完整剪辑文件名，不改原 Animator 资源。</summary>
    private static readonly string[] LocomotionClips = { "Anim_MJS_Walk.anim", "Anim_MJS_Run_F.anim", "Anim_JX_Jump_Start.anim", "Anim_JX_Jump_End.anim", "Anim_MJS_Walk_ZD.anim", "Anim_MJS_Run_F_ZD.anim" };

    /// <summary>给七份现有作者稿追加声音，给无位移段的 Q2/Q3/E/F 加动画模式，并生成六份普通动作及其运行声音配置。</summary>
    [MenuItem("Tools/技能/YCombat/编入RO音效与MJS根运动")]
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
            Undo.RecordObject(binding.Config, "编入RO动作声音与根运动");
            // 根据当前动作表现选择原 RO 样本；触发帧优先和现有挥刀粒子一致。
            if (presentationId == 3001001) AddAudio(binding.Config, "Skills/Shared/Skill_BasicAttack_Slash01.wav", 48, 0.8f);
            else if (presentationId == 3001002) AddAudio(binding.Config, "Skills/Shared/Skill_BasicAttackF_Slash02.wav", 23, 0.7f);
            else if (presentationId == 3001003)
            {
                AddAudio(binding.Config, "Skills/Shared/Skill_BasicAttack_Slash01.wav", 23, 0.8f);
                UpdateInitialAudio(binding.Config, "Skills/Shared/Skill_Combo_FastSlash.wav", 54, 0.6f, "Skills/Shared/Skill_Combo_FastSlash.wav", 54, 0.2f);
            }
            else if (presentationId == 3004001)
            {
                // 首段改用短挥刀，避免原长声音的主起音晚于本动画第一快速动作。
                UpdateInitialAudio(binding.Config, "Skills/Legacy/Skill_E_LegacyFull.wav", 9, 0.65f, "Skills/E/Skill_E_Phase01_HeavySlash.wav", 9, 0.65f);
                // 第二快速挥刀使用独立短声，原 F36 刀光及根运动保持。
                AddAudio(binding.Config, "Skills/E/Skill_E_Phase02_ShortSlash.wav", 36, 0.3f);
            }
            else if (presentationId == 3005001)
            {
                // 原能量声继续覆盖释放过程，降低默认音量给第二刀留出层次。
                UpdateInitialAudio(binding.Config, "Skills/R/Skill_R_Full.wav", FirstFxFrame(binding.Config), 0.6f, "Skills/R/Skill_R_Full.wav", FirstFxFrame(binding.Config), 0.35f);
                // F136 触发短挥刀，其样本主起音位于后续约 0.14 秒的快速挥刀阶段。
                AddAudio(binding.Config, "Skills/R/Skill_R_Followup_HeavySlash.wav", 136, 0.45f);
            }
            else if (presentationId == 3006001)
            {
                AddAudio(binding.Config, "Skills/Shared/Skill_BasicAttackF_Slash02.wav", 37, 0.65f);
                // 第二挥刀原手曲线速度峰约 1.258 秒，短声主起音需由 F142 提前约 0.08 秒触发。
                UpdateInitialAudio(binding.Config, "Skills/Shared/Skill_Combo_FastSlash.wav", 84, 0.6f, "Skills/Shared/Skill_Combo_FastSlash.wav", 142, 0.25f);
            }
            else if (presentationId == 3007001)
            {
                AddAudio(binding.Config, "Skills/SwordDrop/Skill_SwordDrop_Charge.wav", 4, 0.35f);
                AddAudio(binding.Config, "Skills/SwordDrop/Skill_SwordDrop_ReleaseImpact.wav", 9, 0.65f);
            }
            if (presentationId == 3001001 || presentationId == 3001002 || presentationId == 3001003 || presentationId == 3004001 || presentationId == 3006001)
                AddRootMotionIfEmpty(binding.Config, binding.SourceClip);
            EditorUtility.SetDirty(binding.Config);
            AssetDatabase.SaveAssetIfDirty(binding.Config);
        }
        PrepareLocomotion();
        SkillEditorAssetTools.ExportAll();
        ExportLocomotion();
        SkillEditorAssetTools.ValidateAll();
        Debug.Log("已编入 RO 声音：七份技能作者稿与运行表现、走路/跑步/起跳/落地动作；已有粒子与用户位移段保留。");
    }

    /// <summary>将六份普通动作稿单独发布，不改变技能表身份或原角色预制体。</summary>
    [MenuItem("Tools/技能/YCombat/导出MJS走跑跳声音")]
    public static void ExportLocomotion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在非运行模式发布普通动作。");
        // 固定路径由运行缓存加载，与七技能表现共享原 Runtime collector。
        string path = SkillEditorAssetTools.RuntimeDirectory + "/SkillRuntimeLocomotionConfig_MJS.asset";
        // 已存在时修改同一运行配置，保留 GUID 和依赖引用。
        SkillRuntimeLocomotionConfig runtime = AssetDatabase.LoadAssetAtPath<SkillRuntimeLocomotionConfig>(path);
        if (runtime != null && EditorUtility.IsDirty(runtime)) throw new InvalidOperationException("普通动作运行配置存在未保存修改，未覆盖：" + path);
        // 先转换六份作者稿，任何稿件无效时不修改当前运行资产。
        List<SkillRuntimeLocomotionAction> actions = new List<SkillRuntimeLocomotionAction>();
        // Walk/Run/JumpStart/JumpEnd 的稳定顺序与配置字段对应。
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
        EditorUtility.SetDirty(runtime);
        AssetDatabase.SaveAssetIfDirty(runtime);
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
            temporary.PreviewModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GM_Game/Prefabs/Role/Role_MJS.prefab");
            SkillEditorAssetTools.ValidateAuthoring(temporary);
            if (SkillEditorAudioMotionTools.ReadRootMotion(config, clip.length).Count != 0) throw new InvalidOperationException("走跑跳位移由原角色移动组件控制：" + config.name);
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
    }

    /// <summary>创建缺失普通动作稿，已有稿不重置，声音事件通过同一技能编辑器编辑。</summary>
    private static void PrepareLocomotion()
    {
        // 六份作者稿均沿用原动画资源，每份仅新增声音制作数据。
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
            if (index < 2 || index > 3)
            {
                // 根据原脚掌高度曲线的最低接地点制作左右脚步点。
                int leftFrame = FootContactFrame(clip, "LeftFootT.y", config.timelineFrameRate);
                // 另一只脚独立读原曲线，不假定动作从脚步零帧起。
                int rightFrame = FootContactFrame(clip, "RightFootT.y", config.timelineFrameRate);
                AddAudio(config, "Movement/Movement_Footstep01.wav", leftFrame, 0.65f);
                AddAudio(config, "Movement/Movement_Footstep02.wav", rightFrame, 0.65f);
            }
            else if (index == 2) AddAudio(config, "Jump/Jump_TakeoffWhoosh.wav", 0, 0.35f);
            else AddAudio(config, "Jump/Jump_LandingFootstep.wav", 0, 0.8f);
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
        string[] sounds = { "Skills/Shared/Skill_BasicAttack_Slash01.wav", "Skills/Shared/Skill_BasicAttackF_Slash02.wav", "Skills/Shared/Skill_Combo_FastSlash.wav", "Skills/Legacy/Skill_E_LegacyFull.wav", "Skills/R/Skill_R_Full.wav", "Skills/E/Skill_E_Phase01_HeavySlash.wav", "Skills/E/Skill_E_Phase02_ShortSlash.wav", "Skills/R/Skill_R_Followup_HeavySlash.wav", "Skills/SwordDrop/Skill_SwordDrop_Charge.wav", "Skills/SwordDrop/Skill_SwordDrop_ReleaseImpact.wav", "Movement/Movement_Footstep01.wav", "Movement/Movement_Footstep02.wav", "Jump/Jump_LandingFootstep.wav", "Jump/Jump_TakeoffWhoosh.wav" };
        // 所需声音分类相对路径索引，实际身份仍由 Unity 资源数据库返回。
        for (int index = 0; index < sounds.Length; index++)
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDirectory + sounds[index]) == null) throw new InvalidOperationException("RO 声音尚未导入：" + sounds[index]);
        }
        // 普通动作的运行配置也在全部制作前检查脏状态。
        SkillRuntimeLocomotionConfig locomotion = AssetDatabase.LoadAssetAtPath<SkillRuntimeLocomotionConfig>(SkillEditorAssetTools.RuntimeDirectory + "/SkillRuntimeLocomotionConfig_MJS.asset");
        if (locomotion != null && EditorUtility.IsDirty(locomotion)) throw new InvalidOperationException("普通动作运行配置存在未保存修改，未覆盖。");
    }

    /// <summary>迁移上一版自动制作的声音初值；用户已调整音量时保留原事件，已迁移时不重复追加。</summary>
    /// <param name="config">本次更新的原作者对象，粒子和位移列表不参与修改。</param>
    /// <param name="oldFileName">上一版自动稿声音在分类根目录下的相对路径，仅用于定位旧默认事件。</param>
    /// <param name="oldFrame">上一版默认触发帧，使用作者时间轴帧率。</param>
    /// <param name="oldVolume">上一版默认音量，只有仍等于此值才迁移，范围零至一。</param>
    /// <param name="fileName">本版声音在分类根目录下的相对路径，真实引用由 Unity 资源数据库解析。</param>
    /// <param name="frame">本版默认触发帧，使用作者时间轴帧率。</param>
    /// <param name="volume">本版默认音量，范围零至一，不修改原波形。</param>
    private static void UpdateInitialAudio(SkillConfigSO config, string oldFileName, int oldFrame, float oldVolume, string fileName, int frame, float volume)
    {
        // 旧声音资源仍保留，只作为精确识别上一版初稿的引用。
        AudioClip previousClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDirectory + oldFileName);
        // 新资源引用必须真实存在，禁止手写序列化身份。
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDirectory + fileName);
        if (clip == null) throw new InvalidOperationException("RO 声音尚未导入：" + fileName);
        // 原 Y 作者对象的序列化适配边界，仅更新声音的资源、时点和音量。
        SerializedObject serialized = new SerializedObject(config);
        // 原声音和粒子混合列表，引用类型对象由 Unity 序列化接口维护。
        SerializedProperty effects = serialized.FindProperty("fxList");
        // 逐条寻找上一版精确资源和时点，不改变排序和其他事件。
        for (int index = 0; index < effects.arraySize; index++)
        {
            // 当前候选事件，只允许 contentKind=2 的单次音效被迁移。
            SerializedProperty item = effects.GetArrayElementAtIndex(index);
            if (item.FindPropertyRelative("contentKind").intValue != 2 || item.FindPropertyRelative("audioClip").objectReferenceValue != previousClip || item.FindPropertyRelative("keyNumber").intValue != oldFrame) continue;
            if (!Mathf.Approximately(item.FindPropertyRelative("baseVolume").floatValue, oldVolume)) return;
            item.FindPropertyRelative("audioClip").objectReferenceValue = clip;
            item.FindPropertyRelative("keyNumber").intValue = frame;
            item.FindPropertyRelative("baseVolume").floatValue = volume;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
        AddAudio(config, fileName, frame, volume);
    }

    /// <summary>追加固定音量单次声音，重复执行时同一帧同一声音不会再次插入。</summary>
    /// <param name="config">接收新增声音的原作者对象。</param>
    /// <param name="fileName">真实 RO 样本在 MJS 角色声音根目录下的英文相对资源路径。</param>
    /// <param name="frame">明确的时间轴触发帧，零起始。</param>
    /// <param name="volume">制作时固定音量，范围零至一。</param>
    private static void AddAudio(SkillConfigSO config, string fileName, int frame, float volume)
    {
        // Unity 解析实际音频身份，禁止猜测 GUID 或 fileID。
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDirectory + fileName);
        if (clip == null) throw new InvalidOperationException("RO 声音尚未导入：" + fileName);
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

    /// <summary>仅给没有位移段的动作添加全动画根运动，用户已编辑段完全保留。</summary>
    /// <param name="config">原技能作者稿。</param>
    /// <param name="clip">原完整动画，用于计算时间轴结束帧。</param>
    private static void AddRootMotionIfEmpty(SkillConfigSO config, AnimationClip clip)
    {
        // 原位移段列表，已有非空时不扩展或改变用户运动窗口。
        SerializedObject serialized = new SerializedObject(config);
        // 所有 Move 轨道段，不依赖轨道当前显隐。
        SerializedProperty moves = serialized.FindProperty("moveSegmentList");
        if (moves.arraySize != 0) return;
        moves.arraySize = 1;
        // 新根运动窗口保留原 Y 类默认程序字段，这些字段在动画模式不执行。
        SerializedProperty item = moves.GetArrayElementAtIndex(0);
        item.FindPropertyRelative("keyNumber").intValue = 0;
        item.FindPropertyRelative("endKeyNumber").intValue = Mathf.Max(0, Mathf.RoundToInt(clip.length * config.timelineFrameRate) - 1);
        item.FindPropertyRelative("driveMode").intValue = 1;
        item.FindPropertyRelative("rootMotionScale").floatValue = 1f;
        item.FindPropertyRelative("ignoreGravity").boolValue = false;
        item.FindPropertyRelative("ignoreActorCollision").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EnsureTrack(config, 5);
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

    /// <summary>得到六份普通动作的固定作者资产路径，独立于正式技能数字 ID。</summary>
    /// <param name="index">普通动作索引，范围零至五。</param>
    /// <returns>现有作者目录下的完整 Unity 资源路径。</returns>
    private static string LocomotionAuthoringPath(int index)
    {
        return SkillEditorAssetTools.AuthoringDirectory + "/SkillConfigSO_MJS_" + LocomotionNames[index] + ".asset";
    }
}
