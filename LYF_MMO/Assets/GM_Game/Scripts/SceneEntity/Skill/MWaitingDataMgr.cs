using UnityEngine;

/// <summary>
/// 把技能输入转成导航请求：先中断原导航，再通知本地玩家处理新请求。
/// 虽然名称含 Wait，当前不缓存技能，也不排队等待服务器确认。
/// </summary>
public sealed class MWaitingDataMgr
{
    /// <summary>全局输入交接入口；本对象不保存角色、技能或等待状态。</summary>
    public static readonly MWaitingDataMgr singleton = new MWaitingDataMgr();

    /// <summary>交接模块只保留一个无状态实例。</summary>
    private MWaitingDataMgr() { }

    /// <summary>提交技能输入后立即转交导航，不在这里等待。</summary>
    /// <param name="castingSkill">输入绑定的已学习技能核心。</param>
    /// <param name="castPoint">世界空间释放点，单位米。</param>
    /// <param name="target">当前选择目标；现阶段无目标技能传空。</param>
    public void SetWaitSkill(MSkillCore castingSkill, Vector3 castPoint, MEntity target)
    {
        DoCastSkill(castingSkill, castPoint, target);
    }

    /// <summary>带目标的普攻和攻击技能先追近至各自配置射程，无目标及其他类型保持原立即交接。</summary>
    /// <param name="skillCore">本次输入对应的技能核心。</param>
    /// <param name="castPoint">世界空间释放点，单位米。</param>
    /// <param name="target">输入层保留或自动获取的敌人，空值表示沿角色当前朝向释放。</param>
    private void DoCastSkill(MSkillCore skillCore, Vector3 castPoint, MEntity target)
    {
        // 当前场景本地主角，导航组件通过实体普通组件生命周期挂载。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        // 本次技能指令在同步导航事件完成后随载荷归还，不保留跨帧引用。
        MSkillEventArgs skillEvent = MSkillEventArgs.
            Create(skillCore.SkillId, castPoint, target);
        // 导航仍独占到达后的技能指令；有目标的攻击先追近，不在远处提前出招。
        MNavigationEventArgs navigation = MNavigationEventArgs.Create(skillEvent);
        // 主表攻击距离单位为世界米，与导航水平到达范围一致。
        cfg.SkillInfo skillInfo = LubanMgr.Instance.GetSkillInfoById(skillCore.SkillId);
        if ((skillInfo.Type == 1 || skillInfo.Type == 2) && target != null)
        {
            navigation.NeedTarget = true;
            navigation.Target = target;
            navigation.UpperRange = skillInfo.AtkDistance;
        }
        player.NavComp.Interrupt(false);
        player.DispatchEvent(MEventType.MEvent_Navigation, navigation);
    }
}
