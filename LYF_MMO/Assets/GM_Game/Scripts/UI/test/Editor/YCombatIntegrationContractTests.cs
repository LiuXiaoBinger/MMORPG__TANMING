using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>仅供 Editor batch 执行的 YCombat 导出合同测试；只修改内存副本，不生成角色或更新正式资产。</summary>
public static class YCombatIntegrationContractTests
{
    /// <summary>本轮通过的验收断言数，每次 Run 从零开始。</summary>
    private static int _passed;

    /// <summary>读取已生成配置，验证合法导出、未接入字段拒绝、同帧顺序及两端规则文件一致。</summary>
    public static void Run()
    {
        _passed = 0;
        // 七份正式表现的稳定旧表行 ID，Q 三段分别校验。
        int[] presentationIds = { 3001001, 3001002, 3001003, 3004001, 3005001, 3006001, 3007001 };
        // index 为本次待验收的七份正式绑定的零起始下标。
        for (int index = 0; index < presentationIds.Length; index++)
        {
            SkillEditorAssetTools.Validate(LoadBinding(presentationIds[index]));
            Pass("正式表现合法：" + presentationIds[index]);
        }
        // 参考资产不绑定正式表，必须拒绝发布，原 JX 动画不能替换 MJS Q。
        SkillEditorBinding reference = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(
            SkillEditorAssetTools.AuthoringDirectory + "/JX_Attack01_ReferenceBinding.asset");
        ExpectRejected(reference, "JX 参考不能发布");
        // 带两条 FX 的普通技能用于验证序列化字段，不改变正式配置。
        SkillEditorBinding source = LoadBinding(3005001);
        TestUnsupportedRoot(source, "attackList", "隐藏攻击轨道仍拒绝");
        TestUnsupportedRoot(source, "jumpList", "跳转轨道拒绝");
        TestAnimationTrim(source);
        TestFxFields(source);
        TestStableSameFrame(source);
        // 大剑与其他技能使用同一编辑发布链，额外覆盖双阶段与同帧排序。
        SkillEditorBinding sword = LoadBinding(3007001);
        TestSwordFxTrack(sword);
        TestStableSameFrame(sword);
        TestRulesIdentity();
        Debug.Log("YCOMBAT_CONTRACT_PASS assertions=" + _passed);
    }

    /// <summary>加载稳定路径的正式绑定，不用模糊搜索替代真实表现行身份。</summary>
    /// <param name="presentationId">原 SkillPresentationInfo.Id。</param>
    /// <returns>已保存的正式绑定，缺失时直接终止测试。</returns>
    private static SkillEditorBinding LoadBinding(int presentationId)
    {
        // 与生产生成器一致的编辑绑定路径。
        string path = SkillEditorAssetTools.AuthoringDirectory + "/SkillEditorBinding_" + presentationId + ".asset";
        // 测试始终读取真实保存资产，但任何修改只作用于之后的副本。
        SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(path);
        if (binding == null) throw new InvalidOperationException("测试缺少已生成绑定：" + path);
        return binding;
    }

    /// <summary>复制绑定和第三方 Y 配置，资源引用保持原资产；销毁副本不会改动原文件。</summary>
    /// <param name="source">只读正式绑定。</param>
    /// <returns>调用方必须通过 DestroyCopy 归还的内存绑定。</returns>
    private static SkillEditorBinding Copy(SkillEditorBinding source)
    {
        // ScriptableObject 克隆只有内存生命周期，未调用 CreateAsset/SaveAsset。
        SkillEditorBinding copy = UnityEngine.Object.Instantiate(source);
        copy.Config = UnityEngine.Object.Instantiate(source.Config);
        return copy;
    }

    /// <summary>销毁测试复制的配置和绑定，保留剪辑、模型、粒子等正式资源。</summary>
    /// <param name="copy">只属于本项测试的内存绑定。</param>
    private static void DestroyCopy(SkillEditorBinding copy)
    {
        UnityEngine.Object.DestroyImmediate(copy.Config);
        UnityEngine.Object.DestroyImmediate(copy);
    }

    /// <summary>在未显示的 Y 列表写入事件，检查导出白名单仍可拒绝。</summary>
    /// <param name="source">带合法动画与特效的正式绑定。</param>
    /// <param name="field">明确未接入的第三方序列化列表字段名。</param>
    /// <param name="label">此项验收含义。</param>
    private static void TestUnsupportedRoot(SkillEditorBinding source, string field, string label)
    {
        // 独占副本用于模拟编辑器隐藏轨道内仍有数据。
        SkillEditorBinding copy = Copy(source);
        try
        {
            // 第三方序列化适配边界，不直接声明包内同名 Global 嵌套结构。
            SerializedObject serialized = new SerializedObject(copy.Config);
            serialized.FindProperty(field).arraySize = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ExpectRejected(copy, label);
        }
        finally { DestroyCopy(copy); }
    }

    /// <summary>尝试裁剪正式动画，防止编辑器保存成功但运行仅播放旧完整剪辑。</summary>
    /// <param name="source">真实单段技能绑定。</param>
    private static void TestAnimationTrim(SkillEditorBinding source)
    {
        // 唯一裁剪修改只作用于此临时配置。
        SkillEditorBinding copy = Copy(source);
        try
        {
            // 动画嵌套参数只能在序列化边界访问，正式运行不承载 Y 数据。
            SerializedObject serialized = new SerializedObject(copy.Config);
            serialized.FindProperty("animSegments").GetArrayElementAtIndex(0).FindPropertyRelative("clipStartFrame").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ExpectRejected(copy, "裁剪动画拒绝");
        }
        finally { DestroyCopy(copy); }
    }

    /// <summary>检查音频、错误挂点及无效缩放会被生产导出器拒绝。</summary>
    /// <param name="source">至少含一条 FX 的正式绑定。</param>
    private static void TestFxFields(SkillEditorBinding source)
    {
        // 仅用于音频引用拒绝测试的一秒内存声音，不保存为资源。
        AudioClip audio = AudioClip.Create("合同测试临时声音", 1000, 1, 1000, false);
        // 三种不允许发布的变更分别使用全新的副本，避免相互掩盖错误。
        for (int index = 0; index < 3; index++)
        {
            // 本项独占的内存配置。
            SkillEditorBinding copy = Copy(source);
            try
            {
                // 通过既有第三方字段结构产生明确的错误输入。
                SerializedObject serialized = new SerializedObject(copy.Config);
                SerializedProperty effect = serialized.FindProperty("fxList").GetArrayElementAtIndex(0);
                if (index == 0) effect.FindPropertyRelative("audioClip").objectReferenceValue = audio;
                else if (index == 1)
                {
                    effect.FindPropertyRelative("placementKind").intValue = 1;
                    effect.FindPropertyRelative("socketPath").stringValue = "合同测试不存在的挂点/不存在";
                    effect.FindPropertyRelative("followCharacter").boolValue = true;
                    effect.FindPropertyRelative("useWorldSpace").boolValue = false;
                    effect.FindPropertyRelative("forwardDistance").floatValue = 0f;
                }
                else effect.FindPropertyRelative("scale").vector3Value = Vector3.zero;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ExpectRejected(copy, "错误 FX 输入拒绝：" + index);
            }
            finally { DestroyCopy(copy); }
        }
        UnityEngine.Object.DestroyImmediate(audio);
    }

    /// <summary>将两个事件设为同帧，验证生产排序仍保持配置数组原序。</summary>
    /// <param name="source">含两条事件的正式技能绑定。</param>
    private static void TestStableSameFrame(SkillEditorBinding source)
    {
        // 内存副本允许改变帧号，正式运行资源不会重新发布。
        SkillEditorBinding copy = Copy(source);
        try
        {
            // FX 事件只有帧号被修改，其余已验证字段保持原值。
            SerializedObject serialized = new SerializedObject(copy.Config);
            SerializedProperty effects = serialized.FindProperty("fxList");
            if (effects.arraySize < 2) throw new InvalidOperationException("同帧测试需要两条旧特效。");
            // index 为两条粒子配置的零起始序号，使用相同 F10 触发帧。
            for (int index = 0; index < effects.arraySize; index++) effects.GetArrayElementAtIndex(index).FindPropertyRelative("keyNumber").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SkillEditorAssetTools.Validate(copy);
            // 私有导出函数仅在反射测试边界访问，object[] 不进入业务数据或正式接口。
            MethodInfo readFx = typeof(SkillEditorAssetTools).GetMethod("ReadFx", BindingFlags.NonPublic | BindingFlags.Static);
            List<SkillRuntimeFxEvent> events = readFx.Invoke(null, new object[] { copy }) as List<SkillRuntimeFxEvent>;
            // 事件列表缺失或不足两条时，视为顺序校验失败。
            if (events == null || events.Count < 2)
                throw new InvalidOperationException("同帧事件没有保持原配置顺序。");
            // 两条事件帧号均应为第 10 帧。
            if (events[0].Frame != 10 || events[1].Frame != 10)
                throw new InvalidOperationException("同帧事件没有保持原配置顺序。");
            // 两条事件的源序号应保持原配置顺序。
            if (events[0].SourceEventIndex != 0 || events[1].SourceEventIndex != 1)
                throw new InvalidOperationException("同帧事件没有保持原配置顺序。");
            Pass("同帧稳定顺序");
        }
        finally { DestroyCopy(copy); }
    }

    /// <summary>核对大剑双阶段时间轴完整导出，修改特效后仍按同一生产规则接受。</summary>
    /// <param name="source">包含蓄力和目标头顶落剑轨道的大剑技能绑定。</param>
    private static void TestSwordFxTrack(SkillEditorBinding source)
    {
        // 所有编辑仅作用于当前内存副本，正式轨道与运行配置保持原值。
        SkillEditorBinding copy = Copy(source);
        try
        {
            // 私有生产导出入口仅在测试反射边界访问，不扩大业务接口。
            MethodInfo readFx = typeof(SkillEditorAssetTools).GetMethod("ReadFx", BindingFlags.NonPublic | BindingFlags.Static);
            if (readFx == null) throw new InvalidOperationException("生产导出入口不存在。");
            // 强类型导出事件由实际轨道计算；object[] 仅为反射调用边界。
            List<SkillRuntimeFxEvent> events = readFx.Invoke(null, new object[] { copy }) as List<SkillRuntimeFxEvent>;
            // 事件列表缺失或数量不为两条时，视为导出不完整。
            if (events == null || events.Count != 2)
                throw new InvalidOperationException("大剑蓄力或目标头顶落剑事件导出不完整。");
            // 两条事件均必须携带粒子资源。
            if (events[0].Particle == null || events[1].Particle == null)
                throw new InvalidOperationException("大剑蓄力或目标头顶落剑事件导出不完整。");
            // 首条事件必须挂在命名字段且提供套接路径。
            if (events[0].PlacementKind != SkillFxPlacementKind.NamedSocket || string.IsNullOrEmpty(events[0].SocketPath))
                throw new InvalidOperationException("大剑蓄力或目标头顶落剑事件导出不完整。");
            // 次条事件必须落于目标头顶且前向距离为十米。
            if (events[1].PlacementKind != SkillFxPlacementKind.TargetTop || events[1].ForwardDistance != 10f)
                throw new InvalidOperationException("大剑蓄力或目标头顶落剑事件导出不完整。");
            // 序列化访问第三方轨道结构，读取本次真实帧率、帧号和局部位置。
            SerializedObject serialized = new SerializedObject(copy.Config);
            // 本次轨道双阶段列表，对应生产导出的同序事件。
            SerializedProperty effects = serialized.FindProperty("fxList");
            // 当前时间轴零起始帧换算的秒数，精度容限一微秒。
            float chargeTime = effects.GetArrayElementAtIndex(0).FindPropertyRelative("keyNumber").intValue / copy.Config.timelineFrameRate;
            // 落剑秒数独立计算，不能从旧 AnimationEvent 时间代替。
            float dropTime = effects.GetArrayElementAtIndex(1).FindPropertyRelative("keyNumber").intValue / copy.Config.timelineFrameRate;
            if (Mathf.Abs(events[0].TimeSeconds - chargeTime) > 0.000001f || Mathf.Abs(events[1].TimeSeconds - dropTime) > 0.000001f)
                throw new InvalidOperationException("大剑触发秒数没有遵循编辑时间轴。");
            Pass("10070 双阶段按时间轴完整导出");
            // 模拟用户修改蓄力位置，校验与导出必须使用修改后的具体数值。
            Vector3 position = effects.GetArrayElementAtIndex(0).FindPropertyRelative("offset").vector3Value + Vector3.right;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("offset").vector3Value = position;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SkillEditorAssetTools.Validate(copy);
            events = readFx.Invoke(null, new object[] { copy }) as List<SkillRuntimeFxEvent>;
            if (events == null || events.Count != 2 || events[0].LocalPosition != position)
                throw new InvalidOperationException("10070 编辑特效没有进入生产导出结果。");
            Pass("10070 编辑 FX 被接受且进入导出结果");
        }
        finally { DestroyCopy(copy); }
    }

    /// <summary>确认两端导出内容完全一致，并能由共用规则校验接受。</summary>
    private static void TestRulesIdentity()
    {
        // 当前 Unity 项目的共享仓库根，不依赖当前 shell 工作目录。
        string root = Directory.GetParent(Application.dataPath).Parent.FullName;
        // 两端正式导出文件都由同一次序列化产物生成。
        string client = File.ReadAllText(Path.Combine(root, "LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Resources/SkillCastingRules.json"));
        string server = File.ReadAllText(Path.Combine(root, "LYF_Server/GameServer/Config/SkillCastingRules.json"));
        if (client != server) throw new InvalidOperationException("客户端和服务端施法规则内容不同。");
        // JSON 输入在序列化边界转为具名强类型规则。
        SkillCastingRulesCollection collection = JsonUtility.FromJson<SkillCastingRulesCollection>(client);
        // 规则校验返回的领域错误文本，仅供此轮测试失败定位。
        string error;
        if (collection == null || !SkillCastingRulesValidation.Validate(collection.Rules, out error))
            throw new InvalidOperationException("同源施法规则无法解析或未通过结构校验。");
        Pass("两端 JSON 相同且结构有效");
    }

    /// <summary>验证生产导出器确实拒绝此输入，意外接受立即终止整个 batch。</summary>
    /// <param name="binding">正式只读资产或被刻意改坏的内存副本。</param>
    /// <param name="label">本项应拒绝的业务原因。</param>
    private static void ExpectRejected(SkillEditorBinding binding, string label)
    {
        try { SkillEditorAssetTools.Validate(binding); }
        catch (InvalidOperationException) { Pass(label); return; }
        throw new InvalidOperationException("生产校验意外接受：" + label);
    }

    /// <summary>登记一项成功合同断言，最终日志显示本轮实际执行数量。</summary>
    /// <param name="label">已验证通过的输入或拒绝场景。</param>
    private static void Pass(string label)
    {
        _passed++;
        Debug.Log("YCombat 合同断言通过：" + label);
    }
}
