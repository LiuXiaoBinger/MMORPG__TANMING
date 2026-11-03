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
    private const string ReportPath = "../_temp/skill-cancel-feel-20261103/client-contract.txt";

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
            Advance(combined, 1.49f);
            Require(ReferenceEquals(Current(combined), first), "E 提前交出动作");
            Advance(combined, 0.02f);
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
            // 通过真实动作停止钩子清理已登记的未来特效/音频票据，而不是直接 Reset。
            System.Collections.Generic.List<uint> pending = (System.Collections.Generic.List<uint>)typeof(MSkill)
                .GetField("_tokens", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(first);
            Require(pending.Count > 0, "取消测试没有登记未来时间轴事件");
            typeof(MActionComponent).GetField("_isFinished", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, false);
            component.Stop(false);
            MTimerMgr.singleton.Update(7f);
            Require(!combined.IsActive && !first.IsActive && pending.Count == 0, "中断后仍保留子动作或未来事件");
            Require(component.CurrentSkill == null, "中断后原组合仍占用主动作");
            // Stop 已将组合归还池，后续用例可能再次借用它，finally 不能重复回池。
            combined = null;
            VerifyCancellation();
            File.WriteAllText(ReportPath, "PASS：真实资产 E→2 在1.5秒接段、同UUID、确认传递、主槽位移归属、自然收尾、中断回收、滑步/跳跃取消及无效跳跃保留原动作。\n边界：编辑态逻辑验证，未覆盖画面、YooAsset资源包或四服登录联调。\n");
            Debug.Log("COMBINATION_CONTRACT_PASS：1.5秒接段、确认、动作归属、滑步和跳跃取消。");
        }
        finally
        {
            if (combined != null) MSkillFactory.ReturnSkill(combined);
            if (core != null) core.Release();
            SkillRuntimePresentationCache.Release(3004001);
            SkillRuntimePresentationCache.Release(3006001);
        }
    }

    /// <summary>使用真实玩家组件和预测入口验证滑步自转移、起跳许可及取消后的动作清理。</summary>
    private static void VerifyCancellation()
    {
        RetainEditorPresentation(3008001);
        // 独立编辑态玩家不加入场景世界，不连接网络或挂载正式预制体。
        PlayerEntity actor = new PlayerEntity(930102UL, 0);
        actor.UpdateBaseInfo(new RoleBaseInfo { RoleId = 930102, JobId = 1 });
        actor.Initialize();
        try
        {
            // 使用 E、2、滑步和组合覆盖单段、旧禁止取消技能及组合主动作。
            int[] skillIds = { 10040, 10060, 10080, 10100 };
            // 当前登记的真实学习技能下标。
            for (int index = 0; index < skillIds.Length; index++) actor.Skill.AttachSkillInfo(skillIds[index], 1);
            Require(actor.Skill.BeginPrediction(10100, 401UL, 100f, 10000), "组合预测入口失败");
            // 原组合由滑步接管，必须立刻停止而非移到旧施法尾段列表。
            MCombineSkill combined = actor.Skill.CurrentSkill as MCombineSkill;
            // 保留旧子段引用以检查取消状态，后续不依赖已被池化重置的主 ID。
            MSkill child = Current(combined);
            Require(actor.Skill.BeginPrediction(10080, 402UL, 100f, 10000), "滑步不能打断组合");
            // 子实例可能被对象池立即复用为新滑步，只检查旧 UUID 已消失。
            Require(!combined.IsActive && child.SkillUuid != 401UL, "滑步保留了原组合或子段");
            Require(actor.Skill.CurrentSkill.SkillId == 10080, "自转移错误清除了新滑步");
            Require(actor.MoveComp.TryJump(), "跳跃不能取消滑步");
            Require(actor.Skill.CurrentSkill == null && actor.IsJump, "取消后未进入跳跃");
            actor.MoveComp.EndJump();
            // 当前待测试的技能下标；每个技能都应允许地面起跳和滑步取消。
            for (int index = 0; index < skillIds.Length; index++)
            {
                Require(actor.Skill.BeginPrediction(skillIds[index], (ulong)(410 + index), 200f + index, 10000), "取消测试未开始技能");
                // 本次待取消的原动作实例。
                MSkill skill = actor.Skill.CurrentSkill;
                Require(actor.Skill.CanSkill(10080), "滑步默认取消权限未生效");
                Require(!actor.MoveComp.BeginJump(4) && skill.IsActive, "无效四段跳错误取消技能");
                actor.IsMovable = false;
                Require(!actor.MoveComp.TryJump() && skill.IsActive, "禁止移动时错误取消技能");
                actor.IsMovable = true;
                Require(actor.MoveComp.TryJump(), "技能中不能起跳 " + skillIds[index]);
                Require(!skill.IsActive && actor.Skill.CurrentSkill == null, "起跳后技能仍活动");
                Require(actor.Machine.CurrentState == actor.Machine.DefaultState, "起跳后技能仍占用状态机");
                MTimerMgr.singleton.Update(7f);
                Require(actor.Skill.CurrentSkill == null, "起跳后旧时间轴重新开始后续段");
                actor.MoveComp.EndJump();
            }
        }
        finally
        {
            actor.Destroy();
            SkillRuntimePresentationCache.Release(3008001);
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
