using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// 魔剑士主动技能「刀刃风暴」的服务端权威实现：一次施法按原版八段结果点分别结算伤害。
/// 每段独立查询施法者周围存活可攻击怪物，因此同一只怪可以被连续八次命中。
/// </summary>
public sealed class ROBladeStormSkill : ROSkill
{
    /// <summary>刀刃风暴的稳定技能主表编号，与 Luban SkillInfo 的 10110 对应。</summary>
    public const int BladeStormSkillId = 10110;

    /// <summary>
    /// 八段结果点相对施法提交时刻的毫秒偏移，对应原版 6101、6121×5、6141、6161 的动作时间轴。
    /// 段间隔不等距，主表只有一个命中间隔列无法表达，因此时间轴留在技能实现内。
    /// 前两段同为 90 毫秒，同一逻辑帧内必须各结算一次。
    /// </summary>
    private static readonly int[] SegmentMilliseconds = new int[] { 90, 90, 180, 270, 360, 450, 540, 800 };

    /// <summary>前七段的原版 damageAdd 附加值，最后一段为 -1。</summary>
    private const int SegmentDamageAdd = 2;

    /// <summary>末段的原版 damageAdd 附加值，用于表达收招一击的收束。</summary>
    private const int LastSegmentDamageAdd = -1;

    /// <summary>已提交施法的在线角色，服务端从该角色读取权威位置与实例归属。</summary>
    private readonly OnlineRole caster_;

    /// <summary>施法开始时所在的副本实例号，离开该实例后不再产生后续段。</summary>
    private readonly int instance_id_;

    /// <summary>本次技能主表配置的基准伤害，各段按万分比与附加值换算。</summary>
    private readonly int base_damage_;

    /// <summary>主表配置的单段伤害万分比，对应原版 damageMul=0.39。</summary>
    private readonly int damage_permyriad_;

    /// <summary>主表配置的单段命中半径，单位米，由厘米列换算。</summary>
    private readonly float radius_;

    /// <summary>主表配置的单段最大命中敌人数。</summary>
    private readonly int max_targets_;

    /// <summary>从动作提交起已经推进的毫秒数，用于对齐八段结果点。</summary>
    private int elapsed_ms_;

    /// <summary>下一个待结算段的零基下标，达到八表示全部段已发生。</summary>
    private int segment_index_;

    /// <summary>本段已命中的服务端实体编号，保证同一段内同一只怪只结算一次。</summary>
    private readonly HashSet<ulong> hit_targets_ = new HashSet<ulong>();

    /// <summary>以已通过权威校验的数据建立一次刀刃风暴施法实例。</summary>
    /// <param name="core">已学习且通过校验的技能核心，命中参数全部取自其等级效果表。</param>
    /// <param name="skill_uuid">本次已提交的施法标识。</param>
    /// <param name="end_tick">服务器动作截止逻辑帧。</param>
    /// <param name="using_times">本次表现段下标，沿用原有动作流程。</param>
    /// <param name="caster">发起技能的在线角色。</param>
    public ROBladeStormSkill(ROSkillCore core, ulong skill_uuid, long end_tick, int using_times, OnlineRole caster)
        : base(core, skill_uuid, end_tick, using_times)
    {
        caster_ = caster;
        instance_id_ = caster.mainRoleInfo.BaseInfo.InstanceId;
        base_damage_ = core.GetSkillTable().Hurt;
        // 当前技能等级对应的效果行（等级效果表的一行），含单段伤害万分比、命中半径厘米与最大目标数。
        // 命中参数只认这一行，避免表与实现各存一套导致静默打错伤害。
        var effect = core.GetEffect();
        damage_permyriad_ = effect.DamagePermyriad;
        radius_ = effect.RadiusCm / 100f;
        max_targets_ = effect.MaxTargets;
    }

    /// <summary>按固定逻辑帧推进八段结果点，同一帧内的多个到期段依次结算。</summary>
    /// <param name="delta_time">服务器本次固定步进的毫秒数。</param>
    /// <returns>本次施法是否仍需继续推进；false 表示已自然到期或被中断，可由组件结束动作。</returns>
    public override bool Update(int delta_time)
    {
        // playing 表示本次施法是否仍在进行（动作未自然到期且未被中断），布尔值，无单位。
        // 父类按 end_tick 判定；返回 false 时动作已经结束，直接结束更新，不再尝试结算任何尚未发生的段。
        bool playing = base.Update(delta_time);
        if (!playing) return false;
        // 施法状态已被清除（例如被打断换状态）时只终止后续段，已结算的段不回滚，动作寿命仍交父类管理。
        if (!Casting()) return playing;
        // 施法者离开本副本实例后只终止尚未发生的段，已发生的段不回滚。
        if (instance_id_ != caster_.mainRoleInfo.BaseInfo.InstanceId) return playing;
        // 施法者已阵亡时同样只终止后续段，避免死亡后继续造成伤害。
        if (caster_.mainRoleInfo.BaseInfo.CurrHp <= 0) return playing;
        elapsed_ms_ += delta_time;
        // 一次更新可能跨过多个结果点，包括前两段同帧到期的情况。
        while (segment_index_ < SegmentMilliseconds.Length)
        {
            // 本段结果点相对施法提交时刻的偏移，单位毫秒，取值来自 SegmentMilliseconds 的前八项。
            int dueMilliseconds = SegmentMilliseconds[segment_index_];
            if (elapsed_ms_ < dueMilliseconds) break;
            // 每段重新查询目标，同一只怪可在下一段再次受击。
            hit_targets_.Clear();
            TeamManager.Instance.OnBladeStormHit(caster_, SegmentDamage(segment_index_),
                hit_targets_, BladeStormSkillId, radius_, max_targets_);
            segment_index_++;
        }
        return playing;
    }

    /// <summary>按主表万分比与附加值换算单段伤害，避免每段重复一整份普攻伤害。</summary>
    /// <param name="segmentIndex">零基段号，第七段为末段。</param>
    /// <returns>该段送入现有防御与基因伤害链的技能伤害值。</returns>
    private int SegmentDamage(int segmentIndex)
    {
        // add 为本段叠加的原版 damageAdd 附加值，单位点：前七段为 +2，末段为 -1 表示收招一击略微收束。
        int add = SegmentDamageAdd;
        if (segmentIndex == SegmentMilliseconds.Length - 1) add = LastSegmentDamageAdd;
        // result 为本段最终技能伤害点数：基准伤害按万分比折算后再加附加值，最小值截到 0 不产生负伤害。
        // 用整数万分比换算，避免浮点在不同平台上产生末位差异。
        int result = (int)((long)base_damage_ * damage_permyriad_ / 10000) + add;
        if (result < 0) result = 0;
        return result;
    }
}
