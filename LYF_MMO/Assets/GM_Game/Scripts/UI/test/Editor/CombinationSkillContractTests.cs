using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>编辑态使用正式组合资产和真实技能实例验证接段；不挂载正式场景，不连接服务器。</summary>
public static class CombinationSkillContractTests
{
    /// <summary>本次测试报告仅写入项目临时目录。</summary>
    private const string ReportPath = "../_temp/combo-skill-20261103/client-contract.txt";

    /// <summary>加载真实保存资产，验证组合装配、同 UUID、确认、动作归属和中断回收。</summary>
    [MenuItem("Tools/技能/测试/组合技能验收")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请在编辑态运行组合技能验收。");
        // 组合正式绑定只读，测试不会改写作者配置或场景。
        SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(
            SkillEditorAssetTools.AuthoringDirectory + "/SkillEditorBinding_Combine_10100.asset");
        Require(binding != null && binding.Config.TypeToken == 1, "组合资产未保存");
        Require(binding.Config.CombineSkillList.Count == 2, "组合数量错误");
        Require(binding.Config.CombineSkillList[0] == 10040 && binding.Config.CombineSkillList[1] == 10060, "保存的顺序不是 E→2");
        LubanMgr.Instance.Init();
        // 两端实际发布的规则必须完全一致，避免编辑器和服务器使用不同顺序。
        string clientRules = File.ReadAllText("Assets/GM_Game/Scripts/Data/Skill/Resources/SkillCastingRules.json");
        Require(clientRules == File.ReadAllText("../LYF_Server/GameServer/Config/SkillCastingRules.json"), "两端组合规则不同");
        // 编辑态不启动 YooAsset，用真实导出资产填充其加载完成边界；测试不声称覆盖资源包加载和可视效果。
        RetainEditorPresentation(3004001);
        RetainEditorPresentation(3006001);
        // 工厂原样读取新 Luban 主表、等级效果和职业配置。
        MSkillCore core = MSkillFactory.CreateCore(10100, 1, 1);
        // 工厂必须选择组合类型，不能退化为单段普通表现。
        MCombineSkill combined = MSkillFactory.CreateSkill(core) as MCombineSkill;
        try
        {
            Require(core != null && combined != null, "组合核心或运行实例装配失败");
            // 不创建模型的实体用于验证真实逻辑；粒子和声音回调保留原缺模型处理。
            RoleEntity actor = new RoleEntity(930101UL, 0);
            // 构造函数会借出空模型；归还并在反射测试边界解除引用，避免测试等待未装载 GO。
            MModelMgr.Instance.Release(actor.Model);
            typeof(MEntity).GetField("_model", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(actor, null);
            // 只挂接内存技能组件，模拟状态机已经占用组合主动作槽。
            MSkillComponent component = new MSkillComponent(actor);
            typeof(MEntity).GetProperty("Skill").SetValue(actor, component);
            typeof(MSkillComponent).GetField("_skill", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, combined);
            Require(combined.Fire(core, actor, 301UL, 0UL, Vector3.zero, true), "组合启动失败");
            // 首段必须沿用 E 的原表现资产和主 UUID。
            MSkill first = Current(combined);
            Require(first.SkillId == 10040 && first.SkillUuid == 301UL, "首段身份错误");
            Require(OwnsMovement(first, actor), "子技能未取得主动作槽的位移权限");
            Require(combined.Confirm(301UL) && !first.IsPredicted, "主确认未传递给已开始子技能");
            Advance(combined, 3f);
            Require(ReferenceEquals(Current(combined), first), "E 提前交出动作");
            Advance(combined, 0.11f);
            // 第二段使用原 2 技能数据，保持确认状态且不产生新施法 UUID。
            MSkill second = Current(combined);
            Require(second != null && second.SkillId == 10060, "E 结束后未接 2");
            Require(second.SkillUuid == 301UL && !second.IsPredicted, "第二段身份或确认状态丢失");
            Require(!OwnsMovement(first, actor) && OwnsMovement(second, actor), "接段未交接程序位移归属");
            Advance(combined, 3.1f);
            Require(!combined.IsEnd(), "末段提前结束");
            Advance(combined, 0.1f);
            Require(combined.IsEnd(), "整套完成后未结束动作");
            combined.ReleaseAnimation();
            Advance(combined, 0.1f);
            Require(combined.IsPresentationFinished, "原时间轴事件未自然收尾");
            // 同一实例重新 Fire 后中断，必须停止第一段并清空待接的第二段。
            Require(combined.Fire(core, actor, 302UL, 0UL, Vector3.zero, true), "重新施放失败");
            first = Current(combined);
            combined.Reset();
            MTimerMgr.singleton.Update(7f);
            Require(!combined.IsActive && !first.IsActive && Current(combined) == null, "中断后仍保留子动作");
            File.WriteAllText(ReportPath, "PASS：真实资产 E→2、同 UUID、确认传递、主槽位移归属、自然收尾、中断回收。\n边界：编辑态无角色模型，未覆盖画面、YooAsset 资源包或四服登录联调。\n");
            Debug.Log("COMBINATION_CONTRACT_PASS：E→2、确认、动作归属、收尾和中断。");
        }
        finally
        {
            if (combined != null) MSkillFactory.ReturnSkill(combined);
            if (core != null) core.Release();
            SkillRuntimePresentationCache.Release(3004001);
            SkillRuntimePresentationCache.Release(3006001);
        }
    }

    /// <summary>在资源加载的反射测试边界提供正式导出资产，不添加业务测试钩子。</summary>
    /// <param name="presentationId">本次真实子技能的表现行 ID。</param>
    private static void RetainEditorPresentation(int presentationId)
    {
        // 私有缓存属于第三方资源加载适配边界，非泛型字典只在测试反射中使用。
        IDictionary entries = (IDictionary)typeof(SkillRuntimePresentationCache).GetField("Entries", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Require(!entries.Contains(presentationId), "测试不能覆盖已有运行缓存");
        SkillRuntimePresentationCache.Retain(presentationId);
        // object 仅承接私有 Entry 的反射对象，不作为业务载荷。
        object entry = entries[presentationId];
        // 私有 Entry 的元数据只用于模拟 YooAsset 已完成加载。
        Type entryType = entry.GetType();
        entryType.GetField("Asset").SetValue(entry, AssetDatabase.LoadAssetAtPath<SkillRuntimePresentation>(
            "Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_" + presentationId + ".asset"));
        entryType.GetField("LoadStarted").SetValue(entry, true);
        entryType.GetField("LoadCompleted").SetValue(entry, true);
        Require(SkillRuntimePresentationCache.GetReady(presentationId) != null, "缺少原子技能表现资产");
    }

    /// <summary>只读当前子实例，用于核验真实运行顺序。</summary>
    /// <param name="combined">正在测试的组合主实例。</param>
    private static MSkill Current(MCombineSkill combined)
    {
        return (MSkill)typeof(MCombineSkill).GetField("_current", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combined);
    }

    /// <summary>沿实际运动组件调用的内部判定，检查子实例能否代表主动作。</summary>
    /// <param name="skill">待检查的子技能。</param>
    /// <param name="actor">当前主动作槽所属的原实体。</param>
    private static bool OwnsMovement(MSkill skill, MEntity actor)
    {
        // 实际实例代次只在反射测试边界读取，不向业务接口添加访问器。
        ulong generation = (ulong)typeof(MSkill).GetField("_fireGeneration", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(skill);
        // object[] 为 CLR 反射 Invoke 强制要求的参数数组，业务仍使用原强类型函数。
        return (bool)typeof(MSkill).GetMethod("OwnsDisplacement", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(skill, new object[] { actor, skill.SkillUuid, generation });
    }

    /// <summary>先交付全局时间轴回调，再推进同一帧的组合状态。</summary>
    /// <param name="combined">本轮待推进的组合实例。</param>
    /// <param name="seconds">测试帧的非负秒数。</param>
    private static void Advance(MCombineSkill combined, float seconds)
    {
        MTimerMgr.singleton.Update(seconds);
        combined.Update(seconds);
    }

    /// <summary>断言失败时中止菜单执行，Unity 控制台保留具体原因。</summary>
    /// <param name="condition">本项实际观察结果。</param>
    /// <param name="message">失败时的中文定位信息。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
