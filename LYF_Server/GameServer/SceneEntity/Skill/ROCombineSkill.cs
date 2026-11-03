using System.Collections.Generic;

/// <summary>RO 组合技能主实例；共用主技能 UUID、消耗和冷却，按每段状态时长推进普通技能实例。</summary>
public sealed class ROCombineSkill : ROSkill
{
    /// <summary>本次主技能等级对应的子技能核心，顺序与 CombineSkillList 一致。</summary>
    private readonly List<ROSkillCore> skill_cores_;
    /// <summary>各子技能动作占用毫秒数，每项独立向上对齐服务器 50 ms Tick。</summary>
    private readonly List<int> state_times_;
    /// <summary>下一项子技能的零起始序号。</summary>
    private int combine_num_;
    /// <summary>当前执行的子技能，组合自然结束或中断后为空。</summary>
    public ROSkill CurrentChild { get; private set; }

    /// <summary>创建组合主实例并立即启动第一个子技能，不再次进入吟唱或费用校验。</summary>
    /// <param name="core">组合技能本身的主表和等级效果。</param>
    /// <param name="skillUuid">本次施法唯一 UUID。</param>
    /// <param name="endTick">全部子技能合计动作的截止 Tick。</param>
    /// <param name="children">按顺序执行的子技能核心列表，由本实例持有。</param>
    /// <param name="stateTimes">与核心一一对应的毫秒状态时长。</param>
    public ROCombineSkill(ROSkillCore core, ulong skillUuid, long endTick,
        List<ROSkillCore> children, List<int> stateTimes) : base(core, skillUuid, endTick, 0)
    {
        skill_cores_ = children;
        state_times_ = stateTimes;
        StateEnd();
    }

    /// <summary>当前子技能自然到期后立即开始下一段，整套组合结束才向外返回 false。</summary>
    /// <param name="delta_time">本次服务器逻辑帧的毫秒数。</param>
    public override bool Update(int delta_time)
    {
        if (!Casting()) return false;
        if (CurrentChild != null && !CurrentChild.Update(delta_time)) StateEnd();
        return CurrentChild != null;
    }

    /// <summary>按 RO StateEnd 顺序推进，不创建额外网络请求或独立冷却。</summary>
    public void StateEnd()
    {
        if (CurrentChild != null) CurrentChild.Freeze(false);
        CurrentChild = null;
        if (combine_num_ >= skill_cores_.Count) return;
        // 每段以服务器固定步进向上取整，保证不会在动作末帧前解锁。
        long endTick = GameGlobal.Instance.LogicTick + (state_times_[combine_num_] + 49L) / 50L;
        CurrentChild = new ROSkill(skill_cores_[combine_num_], GetSkillUUID(), endTick, 0);
        combine_num_++;
    }

    /// <summary>主技能中断时同步停止当前子技能，不再启动后续段。</summary>
    /// <param name="interrupted">被打断为 true，自然结束为 false。</param>
    public override void Freeze(bool interrupted)
    {
        if (CurrentChild != null) CurrentChild.Freeze(interrupted);
        CurrentChild = null;
        base.Freeze(interrupted);
    }
}
