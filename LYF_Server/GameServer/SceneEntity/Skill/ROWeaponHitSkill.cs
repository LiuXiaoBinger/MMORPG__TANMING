using System;
using System.Collections.Generic;
using System.Numerics;
using cfg;

/// <summary>按武功正式碰撞时间结算前方或瞄准落点的连续命中。</summary>
internal sealed class ROWeaponHitSkill : ROSkill
{
    /// <summary>与武功表现前方碰撞盒一致的盒心偏移，单位米。</summary>
    private const float ForwardBoxOffset = 1.7f;
    /// <summary>斩天剑的主表编号。第二段在角色前方的落剑特效处结算。</summary>
    private const int SkyCleaverSkillId = 10060;
    /// <summary>斩天剑第二段落剑特效相对施法者的水平盒心偏移，单位米。</summary>
    private const float SkyCleaverImpactOffset = 6f;
    /// <summary>落点武功无目标时的原特效默认瞄准距离，单位米。</summary>
    private const float DefaultAimDistance = 10f;
    /// <summary>持有权威位置和战斗属性的施法者。</summary>
    private readonly OnlineRole caster_;
    /// <summary>施放时的副本编号，离开此副本后停止后续命中。</summary>
    private readonly int instanceId_;
    /// <summary>当前子动作的命中时点、形状和段数。</summary>
    private readonly SkillEffectInfo hitEffect_;
    /// <summary>本次伤害使用的等级行，组合技能各段共享主技能的元素配置。</summary>
    private readonly SkillEffectInfo damageEffect_;
    /// <summary>此段招式的原基础伤害点数，结算时再合并角色面板攻击。</summary>
    private readonly int baseDamage_;
    /// <summary>施放开始时固定的水平单位朝向。</summary>
    private readonly Vector2 forward_;
    /// <summary>落点武功固定的范围中心，沿用服务端角色高度，单位米。</summary>
    private readonly Vector3 aimCenter_;
    /// <summary>当前结果点已经命中的怪物实例编号，下一段开始前清空。</summary>
    private readonly HashSet<ulong> hitTargets_ = new HashSet<ulong>();
    /// <summary>从该动作或子动作开始累计的毫秒数。</summary>
    private int elapsedMs_;
    /// <summary>下一段碰撞窗口开启时间，相对当前动作起点的毫秒数。</summary>
    private int nextHitMs_;
    /// <summary>已经处理的结果点数量，从零计数。</summary>
    private int hitCount_;

    /// <summary>从本段动作和实际施放等级构建服务端伤害实例。</summary>
    /// <param name="core">当前动作的已验证核心；组合技能传入子动作核心。</param>
    /// <param name="uuid">当前独立或组合施法的 UUID。</param>
    /// <param name="endTick">本段动作结束的服务器逻辑帧。</param>
    /// <param name="variant">本次选定的动画表现段下标。</param>
    /// <param name="caster">本次攻击的在线角色。</param>
    /// <param name="aim">客户端传入的水平世界瞄准点，零向量时使用已有朝向。</param>
    /// <param name="damageEffect">本次扣血归属的独立或组合主技能等级行。</param>
    internal ROWeaponHitSkill(ROSkillCore core, ulong uuid, long endTick, int variant, OnlineRole caster,
        ROVector3 aim, SkillEffectInfo damageEffect) : base(core, uuid, endTick, variant)
    {
        caster_ = caster;
        instanceId_ = caster.mainRoleInfo.BaseInfo.InstanceId;
        hitEffect_ = core.GetEffect();
        damageEffect_ = damageEffect;
        baseDamage_ = core.GetSkillTable().Hurt;
        nextHitMs_ = hitEffect_.ImpactDelayMs;

        // 服务器权威起点用于限制瞄准距离，避免直接使用客户端任意坐标。
        Vector3 origin = caster.mainRoleInfo.BaseInfo.Pos.ToVector3();
        // 只取水平向量，角色和怪物高度仍由命中盒判定。
        Vector2 delta = new Vector2(aim.x_ - origin.X, aim.z_ - origin.Z);
        // 没有有效瞄准点时沿用移动组件的已上报朝向。
        float distance = delta.Length();
        // 零坐标是现有施法协议中省略瞄准点的表示。
        bool hasAim = (aim.x_ != 0f || aim.z_ != 0f) && distance > 0f;
        if (hasAim && !float.IsInfinity(distance)) forward_ = delta / distance;
        else
        {
            // 游戏服朝向以度保存，水平前向按 (sin, cos) 计算。
            double radians = caster.GetComponent<ROMoveComponent>().Facing * Math.PI / 180;
            forward_ = new Vector2((float)Math.Sin(radians), (float)Math.Cos(radians));
            distance = DefaultAimDistance;
        }
        // 有最大射程的武功使用固定落点，普通近战由当前角色位置推进。
        // 落点武功（如无限剑决）的无目标空放不能把落点推到默认十米外：
        // 该落点盒半径仅由 RadiusCm 决定，推远后贴身敌人会完全落在盒外，表现为大剑特效落下却零伤害。
        // 无有效瞄准点时落点收回施法者自身，空放也能斩到身前敌人；有瞄准点时仍用权威距离裁切。
        float aimDistance = 0f;
        if (hasAim) aimDistance = Math.Min(distance, hitEffect_.MaxRangeCm / 100f);
        aimCenter_ = origin + new Vector3(forward_.X, 0f, forward_.Y) * aimDistance;
    }

    /// <summary>推进已有武功时间轴，到每个生效窗口时查询敌人并结算一次。</summary>
    /// <param name="delta_time">本次推进的毫秒数。</param>
    /// <returns>当前动作仍在有效时段时返回 true。</returns>
    public override bool Update(int delta_time)
    {
        if (!base.Update(delta_time)) return false;
        if (instanceId_ != caster_.mainRoleInfo.BaseInfo.InstanceId || caster_.mainRoleInfo.BaseInfo.CurrHp <= 0)
            return true;
        elapsedMs_ += delta_time;
        while (hitCount_ < hitEffect_.HitCount && elapsedMs_ >= nextHitMs_)
        {
            // 前方招式读取命中时的实际角色位置，落点招式使用经过距离裁切的固定中心。
            Vector3 origin = caster_.mainRoleInfo.BaseInfo.Pos.ToVector3();
            // 当前形状是固定落点时，盒体以落点为中心。
            float forwardOffset = ForwardBoxOffset;
            // 斩天剑的第一段仍在近处，第二段以落剑特效所在的前方区域为中心。
            if (SkillID() == SkyCleaverSkillId && hitCount_ == 1) forwardOffset = SkyCleaverImpactOffset;
            if (hitEffect_.MaxRangeCm > 0)
            {
                origin = aimCenter_;
                forwardOffset = 0f;
            }
            hitTargets_.Clear();
            TeamManager.Instance.OnRoleBoxSkillHit(caster_, origin, forward_, baseDamage_, hitTargets_,
                damageEffect_.SkillId, forwardOffset, hitEffect_.RadiusCm / 100f, damageEffect_);
            hitCount_++;
            nextHitMs_ += hitEffect_.HitIntervalMs;
        }
        return true;
    }
}
