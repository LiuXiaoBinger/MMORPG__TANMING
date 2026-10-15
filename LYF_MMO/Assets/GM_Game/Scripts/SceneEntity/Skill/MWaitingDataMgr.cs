using UnityEngine;

/// <summary>保留RO技能到导航的交接模块；缓存等待和自动攻击尚未实现，当前输入立即交接。</summary>
public sealed class MWaitingDataMgr
{
    /// <summary>RO同名全局交接入口，不保存角色、技能或等待状态。</summary>
    public static readonly MWaitingDataMgr singleton = new MWaitingDataMgr();

    /// <summary>交接模块只保留一个无状态实例。</summary>
    private MWaitingDataMgr() { }

    /// <summary>RO同名技能提交接口；尚无缓存等待，立即执行本次导航交接。</summary>
    /// <param name="castingSkill">输入绑定的已学习技能核心。</param>
    /// <param name="castPoint">世界空间释放点，单位米。</param>
    /// <param name="target">当前选择目标；现阶段无目标技能传空。</param>
    public void SetWaitSkill(MSkillCore castingSkill, Vector3 castPoint, MEntity target)
    {
        DoCastSkill(castingSkill, castPoint, target);
    }

    /// <summary>按RO顺序中断旧导航并派发技能导航指令，不缓存等待服务器确认。</summary>
    /// <param name="skillCore">本次输入对应的技能核心。</param>
    /// <param name="castPoint">世界空间释放点，单位米。</param>
    /// <param name="target">选择目标；当前不扩展目标协议。</param>
    private void DoCastSkill(MSkillCore skillCore, Vector3 castPoint, MEntity target)
    {
        // 当前场景本地主角，导航组件通过实体普通组件生命周期挂载。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        // 本次技能指令在同步导航事件完成后随载荷归还，不保留跨帧引用。
        MSkillEventArgs skillEvent = MSkillEventArgs.Create(skillCore.SkillId, castPoint, target);
        player.NavComp.Interrupt(false);
        player.DispatchEvent(MEventType.MEvent_Navigation, MNavigationEventArgs.Create(skillEvent));
    }
}
