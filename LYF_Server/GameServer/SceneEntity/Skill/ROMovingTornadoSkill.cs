using System;
using System.Collections.Generic;
using System.Numerics;
using cfg;

/// <summary>副本内的龙卷斩实例，在龙卷风通过前方结果点时结算一次范围攻击。</summary>
public sealed class ROMovingTornadoSkill : ROSkill
{
    /// <summary>龙卷斩的技能主表编号，和角色学习记录保持一致。</summary>
    public const int TornadoSlashSkillId = 10130;

    /// <summary>动作第 30 帧（60 帧每秒）放出龙卷风，施法开始后 600 毫秒命中，比动作结束早一个服务端逻辑步。</summary>
    private const int ImpactMilliseconds = 600;

    /// <summary>风体放出 100 毫秒后的世界距离，包含起点前移的 0.65 米。</summary>
    private const float ImpactDistance = 2.95f;

    /// <summary>提交本次施法的在线角色，用于判断副本归属及查询伤害属性。</summary>
    private readonly OnlineRole caster_;

    /// <summary>施法提交时的副本实例号，离开这个副本后不再执行本次结果。</summary>
    private readonly int instance_id_;

    /// <summary>服务器在施法提交时锁定的角色位置，单位米，不随表现或客户端坐标变化。</summary>
    private readonly Vector3 origin_;

    /// <summary>施法瞄准点相对服务器位置的水平单位向量，为零时没有方向信息。</summary>
    private readonly Vector2 forward_;

    /// <summary>本次真实已学等级对应的只读效果行，给出伤害系数、范围和目标上限。</summary>
    private readonly SkillEffectInfo effect_;

    /// <summary>送入通用防御计算的伤害点数，取主表基准和当前等级效果。</summary>
    private readonly int damage_;
    /// <summary>苍云斩按本次消耗计算的最终伤害增幅，单位万分比。</summary>
    private readonly int damageBonusPermyriad_;

    /// <summary>施法已推进的服务器逻辑毫秒数，从提交成功后开始累计。</summary>
    private int elapsed_ms_;

    /// <summary>当前实例的唯一结果点是否已消费，有无命中目标都只处理一次。</summary>
    private bool impact_applied_;

    /// <summary>从正式施法数据锁定本次特效对应的水平朝向与副本起点。</summary>
    /// <param name="core">通过校验且已按学习等级装载的技能核心。</param>
    /// <param name="skill_uuid">本次施法的唯一标识，与开始和结束通知一致。</param>
    /// <param name="end_tick">由服务端按固定 50 毫秒步进算出的动作结束帧。</param>
    /// <param name="using_times">服务端选定的零起始动画变体编号。</param>
    /// <param name="caster">从现有在线角色系统取得的技能发起人。</param>
    /// <param name="cast_point">角色当前朝向上的世界瞄准点，应用时只使用 X/Z 分量。</param>
    /// <param name="trueQiSpent">本次成功施放扣除的真气点数。</param>
    public ROMovingTornadoSkill(ROSkillCore core, ulong skill_uuid, long end_tick,
        int using_times, OnlineRole caster, ROVector3 cast_point, int trueQiSpent = 0)
        : base(core, skill_uuid, end_tick, using_times)
    {
        caster_ = caster;
        instance_id_ = caster.mainRoleInfo.BaseInfo.InstanceId;
        origin_ = caster.mainRoleInfo.BaseInfo.Pos.ToVector3();
        effect_ = core.GetEffect();
        // 外攻系数和固定点数合成基础伤害，防御后的最终百分比单独保存。
        long scaled_damage = (long)caster.AttrComp.ExternalAttack * effect_.DamagePermyriad / 10000;
        damage_ = (int)Math.Min(scaled_damage + effect_.FlatDamage, int.MaxValue);
        // 每消费一个完整档数追加一档增伤，按本次快照计算。
        int bonus = effect_.TornadoDamageBonusPermyriad;
        if (effect_.TrueQiDamageStep > 0)
            bonus += trueQiSpent / effect_.TrueQiDamageStep * effect_.TrueQiStepBonusPermyriad;
        damageBonusPermyriad_ = bonus;
        if (cast_point.x_ == 0f && cast_point.z_ == 0f) return;
        // 向量来自瞄准点和权威起点的差，只用于计算水平朝向。
        Vector2 delta = new Vector2(cast_point.x_ - origin_.X, cast_point.z_ - origin_.Z);
        // 水平距离以米为单位，用于排除零长度并得到单位朝向。
        float length = delta.Length();
        if (length > 0f && !float.IsInfinity(length)) forward_ = delta / length;
    }

    /// <summary>推进至唯一命中点，使用通用副本范围入口处理实际扣血与广播。</summary>
    /// <param name="delta_time">游戏服本次固定逻辑帧经过的毫秒数。</param>
    /// <returns>施法动作是否仍在有效时段内。</returns>
    public override bool Update(int delta_time)
    {
        if (!base.Update(delta_time)) return false;
        if (impact_applied_ || forward_ == Vector2.Zero) return true;
        if (instance_id_ != caster_.mainRoleInfo.BaseInfo.InstanceId) return true;
        if (caster_.mainRoleInfo.BaseInfo.CurrHp <= 0) return true;
        elapsed_ms_ += delta_time;
        if (elapsed_ms_ < ImpactMilliseconds) return true;

        impact_applied_ = true;
        // 结果点位于原施法起点的正前方，以米为单位。
        Vector3 center = origin_ + new Vector3(forward_.X, 0f, forward_.Y) * ImpactDistance;
        TeamManager.Instance.OnRoleAreaSkillHit(caster_, center, damage_, new HashSet<ulong>(),
            TornadoSlashSkillId, effect_.RadiusCm / 100f, effect_.MaxTargets, effect_, damageBonusPermyriad_);
        return true;
    }
}
