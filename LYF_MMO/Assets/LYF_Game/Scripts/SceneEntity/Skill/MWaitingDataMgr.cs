using UnityEngine;

/// <summary>
/// 把技能输入转成导航请求：先中断原导航，再通知本地玩家处理新请求。
/// 虽然名称含 Wait，当前不缓存技能，也不排队等待服务器确认。
/// </summary>
public sealed class MWaitingDataMgr
{
    /// <summary>刀刃风暴主表编号，按施法者位置结算，已有敌人只用于起手转身。</summary>
    internal const int BladeStormSkillId = 10110;

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

    /// <summary>常规有目标攻击按射程追近，刀刃风暴直接交接施法并保留锁定朝向。</summary>
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
        // 技能事件保存用于起手的目标，导航自身是否追近由下面的射程配置决定。
        MNavigationEventArgs navigation = MNavigationEventArgs.Create(skillEvent);
        // 主表攻击距离单位为世界米，与导航水平到达范围一致。
        cfg.SkillInfo skillInfo = LubanMgr.Instance.GetSkillInfoById(skillCore.SkillId);
        // 普攻和主动攻击沿用主表分类，其他类型无需按攻击射程追近。
        bool isAttack = skillInfo.Type == 1 || skillInfo.Type == 2;
        if (isAttack && target != null && skillCore.SkillId != BladeStormSkillId)
        {
            navigation.NeedTarget = true;
            navigation.Target = target;
            navigation.UpperRange = skillInfo.AtkDistance;
        }
        player.NavComp.Interrupt(false);
        player.DispatchEvent(MEventType.MEvent_Navigation, navigation);
    }
}
