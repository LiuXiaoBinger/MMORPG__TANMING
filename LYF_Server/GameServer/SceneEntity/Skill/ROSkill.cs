/// <summary>对应 RO 实际放出的技能；一次 UUID 只保存动作期限和结束状态，冷却属于角色组件。</summary>
public sealed class ROSkill
{
    /// <summary>本次施法使用的已装载等级核心，直到技能结束仍引用原等级。</summary>
    private readonly ROSkillCore core_;
    /// <summary>客户端本次施法 UUID，权威开始和结束通知沿用同一值。</summary>
    private readonly ulong skill_uuid_;
    /// <summary>动作结束的服务器单调 Tick，每 Tick 为 50 毫秒。</summary>
    private readonly long skill_end_time_;
    /// <summary>本次权威选定的零起始表现段下标。</summary>
    private readonly int using_times_;
    /// <summary>技能仍占用当前动作时为 true，结束后不能再次推进。</summary>
    private bool casting_ = true;
    /// <summary>非自然结束时为 true，供权威结束通知区分中断。</summary>
    public bool Interrupted { get; private set; }

    /// <summary>以已通过权威校验的数据建立一次施法实例。</summary>
    /// <param name="core">稳定身份与等级效果的只读核心。</param>
    /// <param name="skill_uuid">本次客户端请求携带的非零施法 UUID。</param>
    /// <param name="end_tick">按 50 毫秒向上取整后的动作截止 Tick。</param>
    /// <param name="using_times">本次表现列表的零起始段下标。</param>
    public ROSkill(ROSkillCore core, ulong skill_uuid, long end_tick, int using_times)
    {
        core_ = core;
        skill_uuid_ = skill_uuid;
        skill_end_time_ = end_tick;
        using_times_ = using_times;
    }

    /// <summary>返回本次施法 UUID，与技能主表 ID 无关。</summary>
    public ulong GetSkillUUID() { return skill_uuid_; }

    /// <summary>返回本次施法的稳定主表 ID。</summary>
    public int SkillID() { return core_.SkillID(); }

    /// <summary>返回本次施法的等级效果 ID。</summary>
    public int SkillEffectID() { return core_.SkillEffectID(); }

    /// <summary>返回本次施法固定的表现段下标。</summary>
    public int GetUsingTimes() { return using_times_; }

    /// <summary>当前动作仍未结束时为 true。</summary>
    public bool Casting() { return casting_; }

    /// <summary>按服务器当前逻辑 Tick 判断动作是否到期，不读取机器 UTC。</summary>
    public bool IsStateTimeEnd() { return GameGlobal.Instance.LogicTick >= skill_end_time_; }

    /// <summary>更新当前动作期限；自然到期返回 false，由组件发送唯一结束通知。</summary>
    /// <param name="delta_time">当前固定逻辑帧的毫秒跨度，结束判定复用全局 Tick。</param>
    public bool Update(int delta_time) { return casting_ && !IsStateTimeEnd(); }

    /// <summary>结束本次动作并记录中断原因，不退还已扣法力或技能冷却。</summary>
    /// <param name="interrupted">主动取消、死亡或离线结束为 true，自然到期为 false。</param>
    public void Freeze(bool interrupted)
    {
        Interrupted = interrupted;
        casting_ = false;
    }
}
