using System;
using System.Collections.Generic;
using System.IO;
using cfg;
using Luban;
using UnityEditor;
using UnityEngine;
using Ethan.ActionEditor;

/// <summary>从旧资源建立 Y 编辑资产，并在显式校验后发布独立运行表现；不修改旧表或原动画。</summary>
public static class SkillEditorAssetTools
{
    /// <summary>用户指定的正式技能配置根目录。</summary>
    public const string SkillDirectory = "Assets/GM_Game/Scripts/Data/Skill";

    /// <summary>编辑资产位于 Editor 目录，不进入运行资源收集。</summary>
    public const string AuthoringDirectory = SkillDirectory + "/Editor/Authoring";

    /// <summary>运行表现资产单独收集，避免将 Y 编辑器与预览模型打入表现配置包。</summary>
    public const string RuntimeDirectory = SkillDirectory + "/Runtime";

    /// <summary>当前正式 MJS 动画目录；旧表 Location 仍以剪辑文件名定位。</summary>
    private const string MjsClipDirectory = "Assets/GM_Game/Animas/Role/MJS/Common/ALL/Skill";

    /// <summary>旧 JX 第一段 FBX，仅生成独立参考编辑资产。</summary>
    private const string JxReferencePath = "Assets/GM_Game/Animas/Role/JX/Atk/Anim_JX_Atk01.FBX";

    /// <summary>Unity 持久化浮点往返容限：时间为一微秒，变换为相应单位的一百万分之一。</summary>
    private const float SerializedFloatTolerance = 0.000001f;

    /// <summary>本版本明确允许承载于编辑资产的字段，新增包字段必须重新审查。</summary>
    private static readonly HashSet<string> SupportedFields = new HashSet<string>
    {
        "m_Script", "m_Name", "m_ObjectHideFlags", "m_EditorHideFlags", "m_CorrespondingSourceObject",
        "m_PrefabInstance", "m_PrefabAsset", "m_GameObject", "m_Enabled", "m_EditorClassIdentifier", "actionEditorSchemaVersion",
        "timingVersion", "timelineFrameRate", "skillID", "skillName", "skillDescription", "skillType",
        "previewModelName", "ownerType", "skillCD", "animSegments", "skillClip", "tracks", "fxList", "moveCurve", "moveSegmentList"
    };

    /// <summary>本游戏没有对应运行处理器的字段；必须保持此包版本的初始值，隐藏轨道也检查。</summary>
    private static readonly HashSet<string> DefaultOnlyFields = new HashSet<string>
    {
        "phases", "flowNodes", "cancelPriority", "energyCostOnActivate", "defaultEnergyGainOnHit", "exitFrame",
        "totalMoveDistance", "jumpList", "attackList", "cameraCues", "cancelList",
        "projectileList", "hitFxList", "interactionWindows", "moveCancelFrame", "warningCueList",
        "superArmorList", "adjustMotionList", "trailToggleList", "isSuperArmorSkill", "phase2AttackList",
        "phase2FxList", "phase2RangeMultiplier",
        // RO补充轨道尚无正式执行后端；允许保存作者资产，但发布时拒绝非默认数据，防止无声丢弃。
        "MobUnitList", "AIEventList", "BuffList", "ChangeColorList", "SkillBarList", "ShakeList",
        "CameraPostEffect", "CameraFxData", "RotateList"
    };

    /// <summary>创建缺失的七份 MJS 与独立 JX 参考资产；已有编辑资产仅校验，不重置用户编辑。</summary>
    [MenuItem("Tools/技能/YCombat/生成缺失技能资产并导出")]
    public static void GenerateAndExport()
    {
        EnsureEditMode();
        EnsureDirectory(AuthoringDirectory);
        EnsureDirectory(RuntimeDirectory);
        // 直接读取项目现有 Luban 二进制，不依赖登录或业务单例初始化。
        TbSkillPresentationInfo presentations = ReadPresentations();
        // 本次只读加载的原 Luban 效果表，用效果行 ID 核对表现所属主技能，不改写表数据。
        TbSkillEffectInfo effects = ReadEffects();
        // 当前正式技能主表快照，仅获取编辑信息。
        TbSkillInfo skills = ReadSkills();
        // 只制作当前已配置的 MJS 技能表现，其他模型行不自动迁移。
        List<SkillPresentationInfo> rows = presentations.DataList;
        // 本轮识别的 MJS 表现数量，验收必须等于七。
        int generatedCount = 0;
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < rows.Count; index++)
        {
            // 本条正式表现行，完整身份会写入对应绑定。
            SkillPresentationInfo row = rows[index];
            if (row.ModelKey != "Role_MJS") continue;
            // 表现效果归属决定技能 ID，不能从表现 ID 数字猜测。
            SkillEffectInfo effect = effects.GetOrDefault(row.EffectId);
            if (effect == null) throw new InvalidOperationException("表现缺少效果行：" + row.Id);
            // 主技能记录仅用于编辑名称、类型和说明，不发布权威数值。
            SkillInfo skill = skills.GetOrDefault(effect.SkillId);
            if (skill == null) throw new InvalidOperationException("效果缺少主技能行：" + effect.Id);
            CreateBinding(row, skill);
            generatedCount++;
        }
        if (generatedCount != 7) throw new InvalidOperationException("预期七条 MJS 表现，实际为：" + generatedCount);
        CreateJxReference();
        ExportAll();
        SkillCastingRulesExportTool.CreateDefaultAssetAndExport();
        EnsureRuntimeCollector();
        AssetDatabase.SaveAssets();
        ValidateAll();
        Debug.Log("YCombat 技能资产已创建/保留并导出：7 条 MJS 表现、1 条独立 JX_Attack01 参考。");
    }

    /// <summary>先校验全部绑定再发布，任何不支持轨道都会阻止此轮运行资产更新。</summary>
    [MenuItem("Tools/技能/YCombat/校验并导出全部表现")]
    public static void ExportAll()
    {
        EnsureEditMode();
        // 按 GUID 查找实际绑定资产，引用仍指向用户编辑过的原对象。
        string[] guids = AssetDatabase.FindAssets("t:SkillEditorBinding", new[] { AuthoringDirectory });
        // 本轮全部已通过校验的正式绑定引用，后续发布仍修改原资产。
        List<SkillEditorBinding> bindings = new List<SkillEditorBinding>();
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < guids.Length; index++)
        {
            // 查询结果的真实资产，不以文件名替代身份。
            SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(AssetDatabase.GUIDToAssetPath(guids[index]));
            if (binding == null || binding.PresentationId == 0) continue;
            Validate(binding);
            bindings.Add(binding);
        }
        if (bindings.Count == 0) throw new InvalidOperationException("没有正式表现绑定可导出。");
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < bindings.Count; index++) Publish(bindings[index]);
        AssetDatabase.SaveAssets();
    }

    /// <summary>校验并导出单一绑定；菜单保存 Y 资产后必须显式执行本入口才更新游戏表现。</summary>
    /// <param name="binding">用户当前选择的正式表现绑定，不接受 PresentationId 为零的参考资产。</param>
    public static void Export(SkillEditorBinding binding)
    {
        EnsureEditMode();
        Validate(binding);
        Publish(binding);
        AssetDatabase.SaveAssets();
        Debug.Log("技能表现导出完成：" + binding.PresentationId);
    }

    /// <summary>使用原编辑器窗口载入真实资产和对应的现有预览角色。</summary>
    /// <param name="binding">提供 Y 资产与角色预制体的编辑绑定。</param>
    public static void Open(SkillEditorBinding binding)
    {
        if (binding == null || binding.Config == null) throw new ArgumentException("编辑绑定缺少 Y 资产。");
        // 唯一的原 Y 窗口，正式角色不添加 ActionPlayer 或第二 Animator 播放器。
        SkillEditorWindow window = EditorWindow.GetWindow<SkillEditorWindow>(SkillEditorLocalization.Text("ACT Action Editor", "ACT 技能编辑器"));
        window.minSize = new Vector2(900f, 600f);
        window.LoadAction(binding.Config, binding.PreviewModel);
        window.Show();
    }

    /// <summary>重新读取保存后的全部资产并核对运行内容、量化误差和依赖；适用于 batch 验收。</summary>
    [MenuItem("Tools/技能/YCombat/验证已保存资产与依赖")]
    public static void ValidateAll()
    {
        // 编辑资产 GUID 数组，不依赖 Unity 当前选择。
        string[] guids = AssetDatabase.FindAssets("t:SkillEditorBinding", new[] { AuthoringDirectory });
        // 本轮验证的正式 MJS 绑定数量。
        int formalCount = 0;
        // 本轮验证的独立参考绑定数量，预期为一。
        int referenceCount = 0;
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < guids.Length; index++)
        {
            // 每条保存后的绑定重新从 AssetDatabase 读取。
            SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(AssetDatabase.GUIDToAssetPath(guids[index]));
            ValidateConfig(binding);
            if (binding.PresentationId == 0)
            {
                referenceCount++;
                continue;
            }
            Validate(binding);
            // 正式表现资源名即现有运行预取使用的 Location。
            string runtimePath = RuntimePath(binding.PresentationId);
            // 保存后的运行表现资产，引用身份与既有 GUID 保持。
            SkillRuntimePresentation runtime = AssetDatabase.LoadAssetAtPath<SkillRuntimePresentation>(runtimePath);
            if (runtime == null || runtime.PresentationId != binding.PresentationId || runtime.SkillId != binding.SkillId)
                throw new InvalidOperationException("运行表现资产缺失或身份不一致：" + runtimePath);
            // 当前旧表是导出数值的来源，重新读表避免把未同步运行资产当作通过。
            SkillPresentationInfo row = ReadPresentations().Get(binding.PresentationId);
            // 显式落为 float 再比较，避免 Mono 浮点求值栈与持久化单精度位数不同。
            float expectedDuration = row.DurationMs / 1000f;
            // 作者制作范围可以超过动画；旧零字段资产仍兼容，重新发布后逐项核对明确的制作范围。
            float expectedTimelineDuration = ReadTimelineDuration(binding.Config, expectedDuration);
            if (runtime.AnimationLocation != row.ClipLocation || !SerializedFloatEquals(runtime.DurationSeconds, expectedDuration) ||
                (runtime.TimelineDurationSeconds != 0f &&
                    !SerializedFloatEquals(runtime.EffectiveTimelineDurationSeconds, expectedTimelineDuration)))
                throw new InvalidOperationException("运行动画或时长尚未同步：" + binding.name +
                    " actual=[" + runtime.AnimationLocation + "," + runtime.DurationSeconds.ToString("R") +
                    "] expected=[" + row.ClipLocation + "," + expectedDuration.ToString("R") + "]");
            // 强引用粒子必须在资源依赖图内，而非仅在字符串中出现。
            string[] dependencies = AssetDatabase.GetDependencies(runtimePath, true);
            // 当前编辑资产解析出的完整强类型粒子事件列表。
            List<SkillRuntimeFxEvent> events = ReadFx(binding);
            // 运行事件数量必须覆盖当前编辑时间轴的全部粒子。
            int expectedCount = events.Count;
            if (runtime.FxEvents.Count != expectedCount) throw new InvalidOperationException("运行事件数量与编辑资产不一致：" + binding.name);
            // eventIndex 为当前集合的零起始下标，按索引访问原对象引用。
            for (int eventIndex = 0; eventIndex < runtime.FxEvents.Count; eventIndex++)
            {
                // 当前派发粒子必须由运行资产保持真实依赖。
                SkillRuntimeFxEvent item = runtime.FxEvents[eventIndex];
                // 从当前 Y 时间轴计算的期望事件；所有运行字段必须与保存结果一致。
                SkillRuntimeFxEvent expected = events[eventIndex];
                if (!SameFx(item, expected)) throw new InvalidOperationException("运行事件尚未同步：" + binding.name + "/" + eventIndex);
                if (!Contains(dependencies, AssetDatabase.GetAssetPath(item.Particle)))
                    throw new InvalidOperationException("运行资产没有粒子依赖：" + item.Particle.name);
            }
            SkillEditorAudioMotionTools.ValidatePublished(binding, runtime, dependencies);
            formalCount++;
            Debug.Log("YCombat 验证：" + binding.name + " clip=" + binding.SourceClip.name + " fps=" + binding.Config.timelineFrameRate + " fx=" + events.Count);
        }
        if (formalCount != 7 || referenceCount != 1) throw new InvalidOperationException("资产数量不符合七正式/一参考：" + formalCount + "/" + referenceCount);
    }

    /// <summary>核对完整旧表身份、动作时长以及可发布的特效时间轴。</summary>
    /// <param name="binding">待发布的具名绑定。</param>
    public static void Validate(SkillEditorBinding binding)
    {
        ValidateConfig(binding);
        if (binding.PresentationId <= 0) throw new InvalidOperationException("参考资产不能导出到正式技能。");
        // 保存的绑定必须仍对应当前旧表中唯一表现行。
        SkillPresentationInfo row = ReadPresentations().GetOrDefault(binding.PresentationId);
        if (row == null || row.EffectId != binding.EffectId || row.ProfessionId != binding.ProfessionId ||
            row.ModelKey != binding.ModelKey || row.WeaponType != binding.WeaponType || row.VariantIndex != binding.VariantIndex)
            throw new InvalidOperationException("表现绑定五元组与旧表不一致：" + binding.name);
        // 技能归属以效果表为准，不信任编辑器技能 ID。
        SkillEffectInfo effect = ReadEffects().GetOrDefault(row.EffectId);
        if (effect == null || effect.SkillId != binding.SkillId || binding.Config.skillID != binding.SkillId)
            throw new InvalidOperationException("编辑技能归属错误：" + binding.name);
        if (binding.SourceClip != FindMjsClip(row.ClipLocation) || binding.SkillStateTimeMs != row.SkillStateTimeMs)
            throw new InvalidOperationException("旧剪辑或状态窗口与绑定不一致：" + binding.name);
        if (Mathf.Abs(binding.SourceClip.length - row.DurationMs / 1000f) > 0.002f)
            throw new InvalidOperationException("剪辑时长与旧表相差超过两毫秒：" + binding.name);
        // 粒子和声音允许按作者制作时间轴触发，动画与位移仍使用旧表完整动作时长。
        ReadFx(binding);
        SkillEditorAudioMotionTools.ReadAudio(binding.Config, ReadTimelineDuration(binding.Config, row.DurationMs / 1000f));
        SkillEditorAudioMotionTools.ReadRootMotion(binding.Config, row.DurationMs / 1000f);

    }

    /// <summary>检查 Y 配置可准确落地的白名单，所有隐藏列表与未知字段同样受检。</summary>
    /// <param name="binding">具备真实剪辑和预览模型的编辑绑定。</param>
    private static void ValidateConfig(SkillEditorBinding binding)
    {
        if (binding == null || binding.Config == null || binding.SourceClip == null || binding.PreviewModel == null)
            throw new InvalidOperationException("技能编辑绑定缺少配置、动画或预览模型。");
        // Y 自身完整配置校验先检查无效帧、坏引用与结构错误。
        List<ActionValidationIssue> issues = ActionConfigValidator.Validate(binding.Config);
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < issues.Count; index++)
        {
            if (issues[index].Severity == ActionValidationSeverity.Error)
                throw new InvalidOperationException(binding.name + "：" + issues[index].Message);
        }
        if (!binding.Config.UsesExplicitTiming || !IsFinite(binding.Config.timelineFrameRate) || binding.Config.timelineFrameRate <= 0f)
            throw new InvalidOperationException("必须使用明确且有效的时间轴 FPS：" + binding.name);
        // 使用 SerializedObject 作为第三方序列化边界，避免两程序集同名 Global 类型冲突。
        SerializedObject serialized = new SerializedObject(binding.Config);
        // 临时默认 Y 配置，用于拒绝宿主不支持的非默认字段，finally 内销毁。
        SkillConfigSO defaults = ScriptableObject.CreateInstance<SkillConfigSO>();
        try
        {
            // 默认实例仅用于固定版本字段对照，完成后立即销毁。
            SerializedObject baseline = new SerializedObject(defaults);
            // 遍历当前配置顶层字段的序列化游标。
            SerializedProperty property = serialized.GetIterator();
            // 仅第一次进入配置根节点，后续跳过字段内部数据，逐个检查顶层字段。
            bool enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = false;
                if (SupportedFields.Contains(property.name)) continue;
                if (!DefaultOnlyFields.Contains(property.name))
                    throw new InvalidOperationException("尚未审查的包字段：" + property.name);
                // 不支持字段必须严格保持默认值，tracks 显隐不能改变检查结果。
                SerializedProperty original = baseline.FindProperty(property.name);
                if (original == null || !SerializedProperty.DataEquals(property, original))
                    throw new InvalidOperationException("本游戏尚未接入字段/轨道：" + property.name);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(defaults);
        }
        // 编辑元数据可调整；CD 仍由服务器/旧表结算，不由此资产覆盖。
        SerializedProperty segments = serialized.FindProperty("animSegments");
        if (segments.arraySize != 1) throw new InvalidOperationException("当前宿主只支持一段完整动画。");
        // 唯一完整动画段的序列化数据。
        SerializedProperty segment = segments.GetArrayElementAtIndex(0);
        // 裁剪结束帧属于动画源采样率；零是完整剪辑哨兵，正数到达自然末尾也表示完整剪辑。
        int clipEndFrame = segment.FindPropertyRelative("clipEndFrame").intValue;
        // 动画源每秒采样帧数；与作者时间轴 FPS 独立，沿原 ActionTiming 的三十帧回退规则。
        float sourceFrameRate = binding.SourceClip.frameRate;
        if (sourceFrameRate <= 0f)
        {
            sourceFrameRate = 30f;
        }
        // 与原 SourceEnd 相同的秒边界；大于自然末尾的结束帧被原制作语义夹到完整剪辑末尾。
        float sourceEndSeconds = binding.SourceClip.length;
        if (clipEndFrame > 0)
        {
            sourceEndSeconds = Mathf.Min(clipEndFrame / sourceFrameRate, binding.SourceClip.length);
        }
        if (segment.FindPropertyRelative("clip").objectReferenceValue != binding.SourceClip ||
            segment.FindPropertyRelative("startFrame").intValue != 0 || segment.FindPropertyRelative("clipStartFrame").intValue != 0 ||
            Mathf.Abs(sourceEndSeconds - binding.SourceClip.length) > SerializedFloatTolerance ||
            segment.FindPropertyRelative("blendInFrames").intValue != 0)
            throw new InvalidOperationException("不支持替换、裁剪、偏移或混合动画。");
        if (binding.Config.skillClip != null && binding.Config.skillClip != binding.SourceClip)
            throw new InvalidOperationException("旧 skillClip 不能与正式动画段引用不同剪辑。");
        ValidateZeroCurve(binding.Config.moveCurve);
        ReadFx(binding);
        SkillEditorAudioMotionTools.ReadAudio(binding.Config, ReadTimelineDuration(binding.Config, binding.SourceClip.length));
        SkillEditorAudioMotionTools.ReadRootMotion(binding.Config, binding.SourceClip.length);
    }

    /// <summary>为非技能普通动作复用相同字段与完整动画门禁，不要求技能表身份。</summary>
    /// <param name="binding">仅在内存构造的普通动作绑定，提供真实作者稿、原动画及预览模型。</param>
    public static void ValidateAuthoring(SkillEditorBinding binding)
    {
        ValidateConfig(binding);
    }

    /// <summary>按开始帧读取 Y 单次粒子并稳定排序；结束帧保留在作者稿作为制作标记，不截断或重复播放。</summary>
    /// <param name="binding">提供编辑配置、真实预览模型和动画的绑定。</param>
    /// <returns>按触发秒数及原配置序号稳定排列的强类型运行事件。</returns>
    private static List<SkillRuntimeFxEvent> ReadFx(SkillEditorBinding binding)
    {
        // 第三方嵌套 Global 类型只在序列化边界读取，不作为业务载荷传递。
        SerializedObject serialized = new SerializedObject(binding.Config);
        // 当前绑定配置的 fxList 粒子事件序列化列表，逐条校验后转换为宿主强类型运行事件。
        SerializedProperty effects = serialized.FindProperty("fxList");
        // 本函数当前累计的强类型结果；返回后由调用方接管。
        List<SkillRuntimeFxEvent> result = new List<SkillRuntimeFxEvent>();
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < effects.arraySize; index++)
        {
            // 当前 Y 粒子序列化结构，所有参数逐项校验后才产生宿主领域事件。
            SerializedProperty item = effects.GetArrayElementAtIndex(index);
            // 当前绑定和原列表下标组成错误定位路径，不将粒子参数错误归因于独立声音轨道。
            string eventPath = binding.name + "/fxList[" + index + "]";
            // 原 Y Audio 内容由声音导出路径处理，不能按空粒子误判或重复产生特效。
            if (item.FindPropertyRelative("contentKind").intValue == 2) continue;
            // 当前时间轴事件的零起始帧号。
            int frame = item.FindPropertyRelative("keyNumber").intValue;
            // 时间轴帧按显式 FPS 换算的秒数，作为运行触发时刻。
            float time = frame / binding.Config.timelineFrameRate;
            // 当前事件实际引用的粒子预制体组件。
            ParticleSystem particle = item.FindPropertyRelative("particleSystem").objectReferenceValue as ParticleSystem;
            // 当前事件的定位枚举值，只允许已经实现的零至四。
            int placement = item.FindPropertyRelative("placementKind").intValue;
            if (frame < 0 || !IsFinite(time) || time > ReadTimelineDuration(binding.Config, binding.SourceClip.length) ||
                particle == null || placement < 0 || placement > 4)
                throw new InvalidOperationException("粒子引用、帧或定位模式无效：" + eventPath);
            // 原 Y 预览对每条事件只生成一个实例，endKeyNumber 不控制粒子寿命；非负且小于等于开始帧沿原单帧哨兵契约处理。
            // 负帧与越过显式退出帧仍由先执行的 ActionConfigValidator 拒绝，不在此重复或改变包校验规则。
            // 导出只携开始帧，作者的区间字段保持原值，实际触发时长继续遵循 ActionTiming.ContentEnd。
            if (item.FindPropertyRelative("contentKind").intValue != 1)
                throw new InvalidOperationException("粒子内容类型未接入：" + eventPath + ".contentKind=" +
                    item.FindPropertyRelative("contentKind").intValue + "；只支持 VisualEffect（1）。");
            if (item.FindPropertyRelative("audioClip").objectReferenceValue != null)
                throw new InvalidOperationException("粒子事件不能混合音频：" + eventPath + ".audioClip=" +
                    item.FindPropertyRelative("audioClip").objectReferenceValue.name + "；请使用独立声音事件。");
            if (item.FindPropertyRelative("playbackSpeed").floatValue != 1f)
                throw new InvalidOperationException("粒子播放倍率未接入：" + eventPath + ".playbackSpeed=" +
                    item.FindPropertyRelative("playbackSpeed").floatValue.ToString("R") + "；正式表现只支持 1。");
            if (item.FindPropertyRelative("customLifetime").floatValue != 0f)
                throw new InvalidOperationException("粒子自定义寿命未接入：" + eventPath + ".customLifetime=" +
                    item.FindPropertyRelative("customLifetime").floatValue.ToString("R") + " 秒；正式表现只支持 0（自然单轮收尾）。");
            if (item.FindPropertyRelative("audioRandomize").boolValue)
                throw new InvalidOperationException("粒子事件不能配置随机声音：" + eventPath + ".audioRandomize=true；正式表现只支持 false。");
            if (item.FindPropertyRelative("pitchVariation").floatValue != 0.1f)
                throw new InvalidOperationException("粒子事件音高变化参数未接入：" + eventPath + ".pitchVariation=" +
                    item.FindPropertyRelative("pitchVariation").floatValue.ToString("R") + "；须保留原默认值 0.1。");
            if (item.FindPropertyRelative("volumeVariation").floatValue != 0.05f)
                throw new InvalidOperationException("粒子事件音量变化参数未接入：" + eventPath + ".volumeVariation=" +
                    item.FindPropertyRelative("volumeVariation").floatValue.ToString("R") + "；须保留原默认值 0.05。");
            if (item.FindPropertyRelative("baseVolume").floatValue != 1f)
                throw new InvalidOperationException("粒子事件基础音量参数未接入：" + eventPath + ".baseVolume=" +
                    item.FindPropertyRelative("baseVolume").floatValue.ToString("R") + "；须保留原默认值 1。");
            // 定位模式同时决定粒子是否跟随；多余的旧世界空间标记不可产生不同运行行为。
            bool follows = placement == 0 || placement == 1;
            // 仅绝对世界模式启用原世界空间标记。
            bool worldAbsolute = placement == 4;
            if (item.FindPropertyRelative("followCharacter").boolValue != follows || item.FindPropertyRelative("useWorldSpace").boolValue != worldAbsolute)
                throw new InvalidOperationException("FX 跟随/世界空间标记与定位模式不一致：" + index);
            // 全部向量和距离必须为有限数，防止异常资产污染 Transform。
            Vector3 position = item.FindPropertyRelative("offset").vector3Value;
            // 当前粒子生成旋转的欧拉角，单位为度。
            Vector3 rotation = item.FindPropertyRelative("rotation").vector3Value;
            // 粒子三轴缩放倍率，各轴必须为正有限数。
            Vector3 scale = item.FindPropertyRelative("scale").vector3Value;
            // 世界前方模式或目标兜底使用的米数，不得为负。
            float forward = item.FindPropertyRelative("forwardDistance").floatValue;
            // 挂点完整路径；仅 NamedSocket 模式允许非空。
            string socket = item.FindPropertyRelative("socketPath").stringValue;
            if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) || !IsFinite(forward) || forward < 0f)
                throw new InvalidOperationException("FX 变换或距离不是有效有限数：" + index);
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                throw new InvalidOperationException("FX 缩放每个轴必须大于零：" + index);
            if (placement == 1)
            {
                if (string.IsNullOrEmpty(socket) || binding.PreviewModel.transform.Find(socket) == null)
                    throw new InvalidOperationException("FX 完整挂点路径不存在：" + socket);
            }
            else if (!string.IsNullOrEmpty(socket)) throw new InvalidOperationException("只有 NamedSocket 能配置挂点。");
            if (placement != 2 && placement != 3 && forward != 0f) throw new InvalidOperationException("当前定位模式不使用前方距离。");
            if (placement == 3 && (rotation.y != 0f || rotation.z != 0f)) throw new InvalidOperationException("前方落点 Y/Z 朝向来自角色，应在配置中保持零。");
            result.Add(new SkillRuntimeFxEvent
            {
                SourceEventIndex = index, Frame = frame, TimeSeconds = time, Particle = particle,
                PlacementKind = (SkillFxPlacementKind)placement, SocketPath = socket,
                LocalPosition = position, LocalEulerAngles = rotation, LocalScale = scale, ForwardDistance = forward
            });
        }
        result.Sort(CompareFx);
        return result;
    }

    /// <summary>同帧按原编辑配置数组序号排序，不因排序算法重排同秒事件。</summary>
    /// <param name="left">待比较的第一条时间轴事件。</param>
    /// <param name="right">待比较的第二条时间轴事件。</param>
    /// <returns>标准升序比较值。</returns>
    private static int CompareFx(SkillRuntimeFxEvent left, SkillRuntimeFxEvent right)
    {
        // 时间优先，序号是同秒事件的稳定顺序键。
        int timeOrder = left.TimeSeconds.CompareTo(right.TimeSeconds);
        if (timeOrder != 0) return timeOrder;
        return left.SourceEventIndex.CompareTo(right.SourceEventIndex);
    }

    /// <summary>把已校验配置发布到固定 Location，保留已有运行资产 GUID 与用户引用。</summary>
    /// <param name="binding">此轮通过完整校验的正式表现绑定。</param>
    private static void Publish(SkillEditorBinding binding)
    {
        EnsureDirectory(RuntimeDirectory);
        // 完整表现行决定动画位置与时长，不使用 Y 的自然结束帧改写旧状态窗口。
        SkillPresentationInfo row = ReadPresentations().Get(binding.PresentationId);
        // 保存后的运行表现资产，引用身份与既有 GUID 保持。
        SkillRuntimePresentation runtime = AssetDatabase.LoadAssetAtPath<SkillRuntimePresentation>(RuntimePath(row.Id));
        if (runtime == null)
        {
            runtime = ScriptableObject.CreateInstance<SkillRuntimePresentation>();
            AssetDatabase.CreateAsset(runtime, RuntimePath(row.Id));
        }
        Undo.RecordObject(runtime, "导出技能表现");
        runtime.PresentationId = row.Id;
        runtime.SkillId = binding.SkillId;
        runtime.AnimationLocation = row.ClipLocation;
        runtime.DurationSeconds = row.DurationMs / 1000f;
        runtime.TimelineDurationSeconds = ReadTimelineDuration(binding.Config, runtime.DurationSeconds);
        runtime.FxEvents = ReadFx(binding);
        runtime.AudioEvents = SkillEditorAudioMotionTools.ReadAudio(binding.Config, runtime.TimelineDurationSeconds);
        runtime.RootMotionSegments = SkillEditorAudioMotionTools.ReadRootMotion(binding.Config, runtime.DurationSeconds);
        runtime.ChargeList = SkillEditorAudioMotionTools.ReadCharge(binding.Config, runtime.DurationSeconds);
        EditorUtility.SetDirty(runtime);
        AssetDatabase.SaveAssetIfDirty(runtime);
    }

    /// <summary>读取作者最后制作帧的秒边界，包含晚于动画的触发点；音频波形长度和粒子尾迹不参与。</summary>
    /// <param name="config">已检查显式时间轴 FPS 的作者配置，不修改其中的开始帧和结束帧。</param>
    /// <param name="animationDurationSeconds">原表或原剪辑的完整动画秒数，只作为制作范围的下限。</param>
    /// <returns>覆盖完整动画和最后制作帧的有限秒数。</returns>
    private static float ReadTimelineDuration(SkillConfigSO config, float animationDurationSeconds)
    {
        // 原 Y 制作内容的非负结束帧；不使用编辑器仅显示粒子和声音尾部的预览范围。
        int lastFrame = ActionTiming.ContentEnd(config);
        // 帧按作者 FPS 转秒，不能使用动画源采样率或音频采样率。
        float durationSeconds = Mathf.Max(animationDurationSeconds, lastFrame / config.timelineFrameRate);
        if (!IsFinite(durationSeconds) || durationSeconds <= 0f)
        {
            throw new InvalidOperationException("技能制作时间轴时长无效：" + config.name);
        }
        return durationSeconds;
    }

    /// <summary>创建缺失的正式绑定与编辑资产，已有配置不重新导入旧事件。</summary>
    /// <param name="row">原表现表中一条 MJS 变体行。</param>
    /// <param name="skill">效果对应的原主技能记录。</param>
    private static void CreateBinding(SkillPresentationInfo row, SkillInfo skill)
    {
        // 绑定路径只用于资产组织，真实身份始终由字段和旧表对照。
        string bindingPath = AuthoringDirectory + "/SkillEditorBinding_" + row.Id + ".asset";
        // 当前真实绑定资产；新创建时只初始化此资产，不重置已有编辑数据。
        SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(bindingPath);
        if (binding != null)
        {
            CompleteSourceStageReferences(binding);
            return;
        }
        binding = ScriptableObject.CreateInstance<SkillEditorBinding>();
        binding.PresentationId = row.Id;
        binding.SkillId = skill.Id;
        binding.EffectId = row.EffectId;
        binding.ProfessionId = row.ProfessionId;
        binding.ModelKey = row.ModelKey;
        binding.WeaponType = row.WeaponType;
        binding.VariantIndex = row.VariantIndex;
        binding.SkillStateTimeMs = row.SkillStateTimeMs;
        binding.SourceClip = FindMjsClip(row.ClipLocation);
        binding.PreviewModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GM_Game/Prefabs/Role/Role_MJS.prefab");
        binding.Config = CreateConfig("SkillConfigSO_" + row.Id, binding, skill.Name, skill.Desc, skill.Type);
        AssetDatabase.CreateAsset(binding, bindingPath);
    }

    /// <summary>生成 JX_Attack01 原 FBX/特效参考，不能覆盖 MJS Q 的正式绑定。</summary>
    private static void CreateJxReference()
    {
        // 与正式绑定不同的固定参考身份，PresentationId=0 阻止发布。
        string path = AuthoringDirectory + "/JX_Attack01_ReferenceBinding.asset";
        // 已存在的参考绑定只补原事件阶段来源，不重新生成可编辑 Y 轨道。
        SkillEditorBinding existing = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(path);
        if (existing != null)
        {
            CompleteSourceStageReferences(existing);
            return;
        }
        // 当前真实绑定资产；新创建时只初始化此资产，不重置已有编辑数据。
        SkillEditorBinding binding = ScriptableObject.CreateInstance<SkillEditorBinding>();
        binding.SkillId = 10010;
        binding.SourceClip = FindImportedClip(JxReferencePath);
        binding.PreviewModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GM_Game/Prefabs/Role/Role_JX.prefab");
        binding.ModelKey = "Role_JX";
        binding.Config = CreateConfig("JX_Attack01_Reference", binding, "JX_Attack01 原动画特效参考", "旧 JX 动画与 PlayEffect 实际事件；不绑定正式 MJS Q。", 1);
        AssetDatabase.CreateAsset(binding, path);
    }

    /// <summary>为新编辑资产建立单段原剪辑与真实旧事件，任何已存在的 Y 资产都不重置。</summary>
    /// <param name="assetName">新 Y 资产文件名，不含扩展名。</param>
    /// <param name="binding">携带真实剪辑和预览模型的绑定。</param>
    /// <param name="displayName">原技能名称或独立参考名称。</param>
    /// <param name="description">编辑用途说明，不包含实现或网络测试入口。</param>
    /// <param name="skillType">原技能类型，仅作为编辑标识。</param>
    /// <returns>保持原 GUID 的已有资产，或刚创建的新 Y 资产。</returns>
    private static SkillConfigSO CreateConfig(string assetName, SkillEditorBinding binding, string displayName, string description, int skillType)
    {
        // 单独资产持有 Y 类型，游戏运行程序集不引用该第三方数据结构。
        string path = AuthoringDirectory + "/" + assetName + ".asset";
        // 当前真实 Y 编辑资产，已存在时保留全部用户编辑。
        SkillConfigSO config = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(path);
        if (config != null) return config;
        if (binding.SourceClip == null || binding.PreviewModel == null) throw new InvalidOperationException("原动画或角色资源缺失：" + assetName);
        config = ScriptableObject.CreateInstance<SkillConfigSO>();
        config.InitializeExplicitTiming(binding.SourceClip.frameRate);
        config.SetActionEditorSchemaVersion(1);
        config.skillID = binding.SkillId;
        config.skillName = displayName;
        config.skillDescription = description;
        config.skillType = skillType;
        config.previewModelName = binding.PreviewModel.name;
        config.skillClip = binding.SourceClip;
        // 序列化适配避免直接使用包内 Global.AnimClipSegment/FxAndSound。
        SerializedObject serialized = new SerializedObject(config);
        // 完整动画段序列化列表，宿主首期仅接受一段。
        SerializedProperty segments = serialized.FindProperty("animSegments");
        segments.arraySize = 1;
        segments.GetArrayElementAtIndex(0).FindPropertyRelative("clip").objectReferenceValue = binding.SourceClip;
        // 只展示当前宿主已接入的两种轨道；隐藏其他轨道不能绕过导出校验。
        SerializedProperty tracks = serialized.FindProperty("tracks");
        tracks.arraySize = 2;
        tracks.GetArrayElementAtIndex(0).FindPropertyRelative("type").intValue = 0;
        tracks.GetArrayElementAtIndex(0).FindPropertyRelative("expanded").boolValue = true;
        tracks.GetArrayElementAtIndex(1).FindPropertyRelative("type").intValue = 2;
        tracks.GetArrayElementAtIndex(1).FindPropertyRelative("expanded").boolValue = true;
        // 从 Unity 实际导入事件读取时间与资源引用，不猜测 FBX meta 的归一化数值。
        List<SkillRuntimeFxEvent> sourceEvents = BuildSourceFx(binding.SourceClip, binding.PreviewModel);
        // 原剪辑完整事件数组用于保留阶段 stringParameter 的真实来源。
        AnimationEvent[] originalEvents = AnimationUtility.GetAnimationEvents(binding.SourceClip);
        // 新建 Y 草稿的 fxList 粒子序列化列表，仅首次创建时填入原剪辑事件，不重写已有草稿。
        SerializedProperty effects = serialized.FindProperty("fxList");
        effects.arraySize = sourceEvents.Count;
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < sourceEvents.Count; index++)
        {
            // 原事件保持绝对秒作为参考，运行发布使用可编辑的 Y 整数帧/FPS。
            SkillRuntimeFxEvent item = sourceEvents[index];
            // 当前新建粒子事件的第三方序列化结构。
            SerializedProperty entry = effects.GetArrayElementAtIndex(index);
            // 当前时间轴事件的零起始帧号。
            int frame = Mathf.RoundToInt(item.TimeSeconds * config.timelineFrameRate);
            entry.FindPropertyRelative("keyNumber").intValue = frame;
            entry.FindPropertyRelative("endKeyNumber").intValue = 0;
            entry.FindPropertyRelative("contentKind").intValue = 1;
            entry.FindPropertyRelative("particleSystem").objectReferenceValue = item.Particle;
            entry.FindPropertyRelative("offset").vector3Value = item.LocalPosition;
            entry.FindPropertyRelative("rotation").vector3Value = item.LocalEulerAngles;
            entry.FindPropertyRelative("scale").vector3Value = item.LocalScale;
            entry.FindPropertyRelative("socketPath").stringValue = item.SocketPath;
            entry.FindPropertyRelative("placementKind").intValue = (int)item.PlacementKind;
            entry.FindPropertyRelative("forwardDistance").floatValue = item.ForwardDistance;
            entry.FindPropertyRelative("followCharacter").boolValue = item.PlacementKind == SkillFxPlacementKind.ActorRoot || item.PlacementKind == SkillFxPlacementKind.NamedSocket;
            entry.FindPropertyRelative("useWorldSpace").boolValue = item.PlacementKind == SkillFxPlacementKind.WorldAbsolute;
            entry.FindPropertyRelative("playbackSpeed").floatValue = 1f;
            entry.FindPropertyRelative("customLifetime").floatValue = 0f;
            entry.FindPropertyRelative("audioClip").objectReferenceValue = null;
            entry.FindPropertyRelative("audioRandomize").boolValue = false;
            entry.FindPropertyRelative("pitchVariation").floatValue = 0.1f;
            entry.FindPropertyRelative("volumeVariation").floatValue = 0.05f;
            entry.FindPropertyRelative("baseVolume").floatValue = 1f;
            binding.FxSources.Add(new SkillEditorFxSource { SourceEventIndex = item.SourceEventIndex,
                OriginalTimeSeconds = item.TimeSeconds, ImportedFrame = frame, Stage = originalEvents[item.SourceEventIndex].stringParameter });
            Debug.Log(assetName + " FX" + index + "：原事件 " + item.TimeSeconds + "s → F" + frame + " / " + config.timelineFrameRate + "FPS；误差 " + (frame / config.timelineFrameRate - item.TimeSeconds) + "s");
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    /// <summary>对早期生成资产仅补缺失的只读阶段来源，不重置用户时间轴、动画或原始秒数。</summary>
    /// <param name="binding">已有真实剪辑与原事件索引记录的编辑绑定。</param>
    private static void CompleteSourceStageReferences(SkillEditorBinding binding)
    {
        if (binding.SourceClip == null || binding.FxSources == null) return;
        // 从同一原剪辑读取实际事件，用已保存索引定位，不猜测技能阶段。
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(binding.SourceClip);
        // 只有参考记录缺阶段且原事件确有阶段时才写入，已有编辑数据全部保留。
        bool changed = false;
        // 原事件来源列表的零起始索引；按索引取得原记录引用，仅补缺失的阶段文本。
        for (int index = 0; index < binding.FxSources.Count; index++)
        {
            // 此条只读来源记录包含原完整事件数组下标。
            SkillEditorFxSource source = binding.FxSources[index];
            if (source.SourceEventIndex < 0 || source.SourceEventIndex >= events.Length || !string.IsNullOrEmpty(source.Stage)) continue;
            if (events[source.SourceEventIndex].functionName != "PlayEffect" || string.IsNullOrEmpty(events[source.SourceEventIndex].stringParameter)) continue;
            source.Stage = events[source.SourceEventIndex].stringParameter;
            changed = true;
        }
        if (changed)
        {
            EditorUtility.SetDirty(binding);
            AssetDatabase.SaveAssetIfDirty(binding);
        }
    }

    /// <summary>读取旧剪辑 PlayEffect 的真实资源、配置索引与特殊落点，绝不修改共享旧配置。</summary>
    /// <param name="clip">AssetDatabase 导入的真实动画剪辑。</param>
    /// <param name="model">对应预览角色预制体，用于将旧挂点解析为完整路径。</param>
    /// <returns>保持原数组顺序的事件，TimeSeconds 为原事件精确秒。</returns>
    private static List<SkillRuntimeFxEvent> BuildSourceFx(AnimationClip clip, GameObject model)
    {
        // AnimationUtility 是 Unity 导入后的权威事件读取接口。
        AnimationEvent[] source = AnimationUtility.GetAnimationEvents(clip);
        // 本函数当前累计的强类型结果；返回后由调用方接管。
        List<SkillRuntimeFxEvent> result = new List<SkillRuntimeFxEvent>();
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < source.Length; index++)
        {
            // 只迁移旧正式特效指令；AnimaEnd 等仍由现有状态机负责。
            AnimationEvent sourceEvent = source[index];
            if (sourceEvent.functionName != "PlayEffect") continue;
            // 当前旧 PlayEffect 携带的原 RoleAtkConfig 引用，intParameter 选择攻击条目；此函数只读不回写。
            RoleAtkConfig config = sourceEvent.objectReferenceParameter as RoleAtkConfig;
            if (config == null || config.atkConfigEntities == null || sourceEvent.intParameter < 0 || sourceEvent.intParameter >= config.atkConfigEntities.Count)
                throw new InvalidOperationException("旧 PlayEffect 配置或索引无效：" + clip.name + "/" + index);
            // 当前原攻击条目，生命周期属于旧资产，不回写它的 EffectInfo。
            atkConfigEntity attack = config.atkConfigEntities[sourceEvent.intParameter];
            if (attack == null || attack._effectInfo == null || attack._effectInfo._fx == null)
                throw new InvalidOperationException("旧 PlayEffect 缺少粒子：" + clip.name + "/" + index);
            // 原粒子配置或旧效果归属记录，不写回共享资源。
            EffectInfo effect = attack._effectInfo;
            // 从本条旧事件建立的独立运行事件；保留原触发秒数与粒子引用，默认世界定位再按阶段修正。
            SkillRuntimeFxEvent item = new SkillRuntimeFxEvent
            {
                SourceEventIndex = index, TimeSeconds = sourceEvent.time, Particle = effect._fx,
                LocalPosition = effect._position, LocalEulerAngles = effect._eulerAngles,
                LocalScale = Vector3.one, SocketPath = string.Empty, PlacementKind = SkillFxPlacementKind.WorldAbsolute
            };
            if (sourceEvent.stringParameter == "skill03_2")
            {
                item.PlacementKind = SkillFxPlacementKind.CasterForwardWorld;
                item.ForwardDistance = 6f;
                item.LocalPosition = Vector3.zero;
                item.LocalEulerAngles = new Vector3(-90f, 0f, 0f);
            }
            else if (sourceEvent.stringParameter == "skill04_2")
            {
                item.PlacementKind = SkillFxPlacementKind.TargetTop;
                item.ForwardDistance = 10f;
            }
            else if (!string.IsNullOrEmpty(effect._parentName))
            {
                item.PlacementKind = SkillFxPlacementKind.NamedSocket;
                item.SocketPath = ResolveSocketPath(model, effect._parentName);
            }
            result.Add(item);
        }
        return result;
    }

    /// <summary>将旧短名称解析为预制体内唯一完整挂点路径，缺失/歧义会停止生成。</summary>
    /// <param name="model">角色预制体根对象，不修改其层级。</param>
    /// <param name="path">旧攻击配置的父节点名或路径。</param>
    /// <returns>相对角色根的完整 Transform 路径。</returns>
    private static string ResolveSocketPath(GameObject model, string path)
    {
        // 完整路径首先按原 Transform.Find 语义解析。
        Transform socket = SkillFxSocketResolver.Resolve(model.transform, path);
        if (socket == null && path.IndexOf('/') < 0)
        {
            // 仅唯一短名可补全路径，避免同名骨骼产生错误挂点。
            Transform[] nodes = model.GetComponentsInChildren<Transform>(true);
            // index 为当前集合的零起始下标，按索引访问原对象引用。
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].name != path) continue;
                if (socket != null) throw new InvalidOperationException("旧挂点名不唯一：" + path);
                socket = nodes[index];
            }
        }
        if (socket == null) throw new InvalidOperationException("原模型没有特效挂点：" + path);
        return AnimationUtility.CalculateTransformPath(socket, model.transform);
    }

    /// <summary>读取单个 FBX 中的唯一非预览动画，避免误把预览子资产当技能。</summary>
    /// <param name="path">旧 FBX 的真实项目资源路径。</param>
    /// <returns>唯一实际导入剪辑。</returns>
    private static AnimationClip FindImportedClip(string path)
    {
        // Unity 多子资产边界返回 UnityEngine.Object 数组，逐个转为明确 AnimationClip。
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        // 当前找到的唯一非预览剪辑引用；空值表示尚未命中，第二次命中会拒绝不明确的来源。
        AnimationClip result = null;
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < assets.Length; index++)
        {
            // FBX 的模型/Avatar 子资产不参与技能动画选择。
            AnimationClip clip = assets[index] as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
            if (result != null) throw new InvalidOperationException("FBX 中有多条非预览剪辑，需要明确选择：" + path);
            result = clip;
        }
        if (result == null) throw new InvalidOperationException("FBX 未导入技能剪辑：" + path);
        return result;
    }

    /// <summary>按旧表 Location 从已确认的独立动画目录查找，禁止自动换成 JX 参考。</summary>
    /// <param name="location">旧表现表不含扩展名的动画资源键。</param>
    /// <returns>真实独立 MJS 动画剪辑。</returns>
    private static AnimationClip FindMjsClip(string location)
    {
        // 旧表实际使用完整 Assets 路径；兼容短资源键时仍限定原 MJS 目录。
        string path = location;
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) path = MjsClipDirectory + "/" + location + ".anim";
        if (!path.StartsWith(MjsClipDirectory + "/", StringComparison.Ordinal) || !path.EndsWith(".anim", StringComparison.Ordinal))
            throw new InvalidOperationException("旧表现动画不在正式 MJS 独立剪辑目录：" + location);
        // 从精确项目路径或 FBX 子资产取得的实际动画剪辑。
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) throw new InvalidOperationException("旧表现动画资源不存在：" + location);
        return clip;
    }

    /// <summary>逐项核对已发布事件与当前编辑事件，避免同数量旧资产被误判为已同步。</summary>
    /// <param name="actual">从保存后的运行资产读取的事件。</param>
    /// <param name="expected">由当前编辑资产解析的期望事件。</param>
    /// <returns>所有受支持运行字段完全一致时返回 true。</returns>
    private static bool SameFx(SkillRuntimeFxEvent actual, SkillRuntimeFxEvent expected)
    {
        return actual != null && expected != null && actual.SourceEventIndex == expected.SourceEventIndex &&
            actual.Frame == expected.Frame && SerializedFloatEquals(actual.TimeSeconds, expected.TimeSeconds) && actual.Particle == expected.Particle &&
            actual.PlacementKind == expected.PlacementKind && actual.SocketPath == expected.SocketPath &&
            SerializedVectorEquals(actual.LocalPosition, expected.LocalPosition) && SerializedVectorEquals(actual.LocalEulerAngles, expected.LocalEulerAngles) &&
            SerializedVectorEquals(actual.LocalScale, expected.LocalScale) && SerializedFloatEquals(actual.ForwardDistance, expected.ForwardDistance);
    }

    /// <summary>比较持久化后的有限浮点，容限仅覆盖单精度往返，不接受 NaN 或无穷。</summary>
    /// <param name="actual">实际保存并重新加载的单精度数据。</param>
    /// <param name="expected">由当前配置计算的期望单精度数据。</param>
    /// <returns>均有限且差异不超过明确的一百万分之一单位时返回 true。</returns>
    private static bool SerializedFloatEquals(float actual, float expected)
    {
        return IsFinite(actual) && IsFinite(expected) && Mathf.Abs(actual - expected) <= SerializedFloatTolerance;
    }

    /// <summary>逐轴比较持久化变换，避开 Unity Vector3 运算符隐含的其他距离阈值。</summary>
    /// <param name="actual">实际运行配置中的米、度或缩放分量。</param>
    /// <param name="expected">当前编辑事件中的对应分量。</param>
    /// <returns>所有分量均通过有限浮点和明确容限检查时返回 true。</returns>
    private static bool SerializedVectorEquals(Vector3 actual, Vector3 expected)
    {
        return SerializedFloatEquals(actual.x, expected.x) && SerializedFloatEquals(actual.y, expected.y) && SerializedFloatEquals(actual.z, expected.z);
    }

    /// <summary>给运行配置追加专属收集目录，保留现有 YooAsset 包与所有旧收集器。</summary>
    private static void EnsureRuntimeCollector()
    {
        // YooAsset 设置是第三方序列化边界，通过实际字段结构追加一条专属收集器。
        UnityEngine.Object setting = AssetDatabase.LoadMainAssetAtPath("Assets/AssetBundleCollectorSetting.asset");
        if (setting == null) throw new InvalidOperationException("YooAsset 收集设置不存在。");
        // 现有 YooAsset 收集资产的序列化访问器，仅查找或追加技能专属组，保留原包、分组与收集器。
        SerializedObject serialized = new SerializedObject(setting);
        // YooAsset 当前全部资源包的序列化列表。
        SerializedProperty packages = serialized.FindProperty("Packages");
        // packageIndex 为当前集合的零起始下标，按索引访问原对象引用。
        for (int packageIndex = 0; packageIndex < packages.arraySize; packageIndex++)
        {
            // 只加入现有默认包，不创建新资源包或改变原地址规则。
            SerializedProperty package = packages.GetArrayElementAtIndex(packageIndex);
            if (package.FindPropertyRelative("PackageName").stringValue != "DefaultPackage") continue;
            // DefaultPackage 已有资源分组，追加新组时保留其他分组。
            SerializedProperty groups = package.FindPropertyRelative("Groups");
            // groupIndex 为当前集合的零起始下标，按索引访问原对象引用。
            for (int groupIndex = 0; groupIndex < groups.arraySize; groupIndex++)
            {
                // 已存在的技能表现组保持原对象字段，验证目录即可。
                SerializedProperty oldGroup = groups.GetArrayElementAtIndex(groupIndex);
                if (oldGroup.FindPropertyRelative("GroupName").stringValue != "SkillRuntimePresentation") continue;
                return;
            }
            groups.arraySize++;
            // Unity 新数组元素可能复制前一个组，必须明确重写该组及其所有收集条目。
            SerializedProperty group = groups.GetArrayElementAtIndex(groups.arraySize - 1);
            SetString(group, "GroupName", "SkillRuntimePresentation");
            SetString(group, "GroupDesc", "YCombat 已校验技能运行表现");
            SetString(group, "AssetTags", "skill-presentation");
            SetString(group, "ActiveRuleName", "EnableGroup");
            // 技能表现专属资源收集条目列表。
            SerializedProperty collectors = group.FindPropertyRelative("Collectors");
            collectors.arraySize = 1;
            // 唯一技能表现收集器，使用原项目的文件名地址和目录打包规则。
            SerializedProperty collector = collectors.GetArrayElementAtIndex(0);
            SetString(collector, "CollectPath", RuntimeDirectory);
            SetString(collector, "CollectorGUID", AssetDatabase.AssetPathToGUID(RuntimeDirectory));
            collector.FindPropertyRelative("CollectorType").intValue = 0;
            SetString(collector, "AddressRuleName", "AddressByFileName");
            SetString(collector, "PackRuleName", "PackDirectory");
            SetString(collector, "FilterRuleName", "CollectAll");
            SetString(collector, "AssetTags", "skill-presentation");
            SetString(collector, "UserData", string.Empty);
            Undo.RecordObject(setting, "增加技能表现资源收集");
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(setting);
            return;
        }
        throw new InvalidOperationException("YooAsset 没有 DefaultPackage。");
    }

    /// <summary>在第三方序列化结构中写入已知字符串字段。</summary>
    /// <param name="parent">YooAsset 已知配置结构。</param>
    /// <param name="field">当前固定版本的字段名称。</param>
    /// <param name="value">待写入的地址、说明或规则字符串。</param>
    private static void SetString(SerializedProperty parent, string field, string value)
    {
        parent.FindPropertyRelative(field).stringValue = value;
    }

    /// <summary>确保资产目录存在，由 AssetDatabase 生成目录 meta。</summary>
    /// <param name="directory">Assets 下的正式配置目录。</param>
    private static void EnsureDirectory(string directory)
    {
        if (AssetDatabase.IsValidFolder(directory)) return;
        // 递归确保父目录后只创建当前一层，资产不会落入临时目录。
        string parent = Path.GetDirectoryName(directory).Replace('\\', '/');
        EnsureDirectory(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(directory));
    }

    /// <summary>检查零位移曲线，不要求默认非空曲线被改为空。</summary>
    /// <param name="curve">Y 的旧整段位移曲线，纵轴必须处处零。</param>
    private static void ValidateZeroCurve(AnimationCurve curve)
    {
        if (curve == null) return;
        // 键值和切线全部为零时整个三次插值段等价于零位移。
        Keyframe[] keys = curve.keys;
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < keys.Length; index++)
        {
            if (keys[index].value != 0f || keys[index].inTangent != 0f || keys[index].outTangent != 0f)
                throw new InvalidOperationException("本游戏尚未接入非零 moveCurve 位移。");
        }
    }

    /// <summary>限制发布在非 Play Mode 执行，避免改动施法中的正式配置。</summary>
    private static void EnsureEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请退出 Play Mode 后编辑/导出技能。");
    }

    /// <summary>构造运行资产固定路径，文件名同时是 YooAsset Location。</summary>
    /// <param name="presentationId">旧表现行唯一正整数 ID。</param>
    /// <returns>对应的正式 .asset 路径。</returns>
    private static string RuntimePath(int presentationId)
    {
        return RuntimeDirectory + "/SkillRuntimePresentation_" + presentationId + ".asset";
    }

    /// <summary>读取现有正式表现二进制表，不重新生成或改写旧表。</summary>
    /// <returns>本次编辑校验使用的表快照。</returns>
    private static TbSkillPresentationInfo ReadPresentations()
    {
        return new TbSkillPresentationInfo(new ByteBuf(File.ReadAllBytes(TablePath("tbskillpresentationinfo"))));
    }

    /// <summary>读取现有效果二进制表，核对表现的主技能归属。</summary>
    /// <returns>本次校验使用的效果表快照。</returns>
    private static TbSkillEffectInfo ReadEffects()
    {
        return new TbSkillEffectInfo(new ByteBuf(File.ReadAllBytes(TablePath("tbskilleffectinfo"))));
    }

    /// <summary>读取现有主技能表，仅获取编辑显示信息。</summary>
    /// <returns>现有正式主技能表快照。</returns>
    private static TbSkillInfo ReadSkills()
    {
        return new TbSkillInfo(new ByteBuf(File.ReadAllBytes(TablePath("tbskillinfo"))));
    }

    /// <summary>定位同仓库现有服务端正式表，不使用临时或历史生成文件。</summary>
    /// <param name="tableName">明确的 Luban 小写表文件名，不含扩展名。</param>
    /// <returns>项目正式二进制表绝对路径。</returns>
    private static string TablePath(string tableName)
    {
        // Unity Assets 父目录是 LYF_MMO，再上一级是共享项目根。
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
        return Path.Combine(projectRoot, "LYF_Server", "Network", "LubanManage", "Tb", tableName + ".bytes");
    }

    /// <summary>检查 Unity 依赖列表是否含指定精确资源路径。</summary>
    /// <param name="paths">Unity 返回的资源依赖路径数组。</param>
    /// <param name="path">待核对的真实粒子资源路径。</param>
    /// <returns>精确路径存在时返回 true。</returns>
    private static bool Contains(string[] paths, string path)
    {
        // index 为当前集合的零起始下标，按索引访问原对象引用。
        for (int index = 0; index < paths.Length; index++) if (paths[index] == path) return true;
        return false;
    }

    /// <summary>判断浮点值能否安全写入时间、位置或距离。</summary>
    /// <param name="value">待检查的浮点数据。</param>
    /// <returns>既非 NaN 也非无穷时返回 true。</returns>
    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>逐轴校验三维位置、旋转或缩放。</summary>
    /// <param name="value">待检查的三轴向量。</param>
    /// <returns>三个轴均有限时返回 true。</returns>
    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }
}
