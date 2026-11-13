using System.Collections.Generic;
using System.Numerics;

/// <summary>对应 RO 的 ROArtSkill，按当前普攻三段动作的碰撞窗口在游戏服查询怪物。</summary>
public sealed class ROArtSkill : ROSkill
{
    /// <summary>当前项目剑气普攻的主表编号，其他技能沿用原动作流程。</summary>
    public const int BasicAttackId = 10010;
    /// <summary>已提交普攻的在线角色；服务端从该角色读取位置和归属。</summary>
    private readonly OnlineRole caster_;
    /// <summary>动作开始时的副本实例号，离开该实例便结束命中。</summary>
    private readonly int instance_id_;
    /// <summary>施法点决定的水平单位朝向；没有有效方向的旧请求只播放动作。</summary>
    private readonly Vector2 forward_;
    /// <summary>本次普攻主表配置的单次伤害。</summary>
    private readonly int damage_;
    /// <summary>当前窗口已命中的实体，同一怪的多帧接触只结算一次。</summary>
    private readonly HashSet<ulong> hit_targets_ = new HashSet<ulong>();
    /// <summary>从动作提交起已经推进的毫秒数。</summary>
    private int elapsed_ms_;
    /// <summary>当前窗口的零基编号；进入第三段第二个窗口时清除命中集合。</summary>
    private int window_;

    /// <summary>开始动作时从权威位置计算瞄准方向，复用 ROSkill 的打断和结束时钟。</summary>
    /// <param name="core">已学习且通过校验的普攻核心。</param>
    /// <param name="skill_uuid">本次已提交的施法标识。</param>
    /// <param name="end_tick">服务器动作截止逻辑帧。</param>
    /// <param name="using_times">三个正式普攻表现的零基下标。</param>
    /// <param name="caster">发起攻击的在线角色。</param>
    /// <param name="cast_point">客户端指定的水平世界瞄准点，单位米。</param>
    public ROArtSkill(ROSkillCore core, ulong skill_uuid, long end_tick, int using_times, OnlineRole caster, ROVector3 cast_point)
        : base(core, skill_uuid, end_tick, using_times)
    {
        caster_ = caster;
        instance_id_ = caster.mainRoleInfo.BaseInfo.InstanceId;
        damage_ = core.GetSkillTable().Hurt;
        if (cast_point.x_ == 0f && cast_point.z_ == 0f) return;
        // 坐标只提供方向，查询中心始终使用角色的服务端位置。
        Vector3 position = caster.mainRoleInfo.BaseInfo.Pos.ToVector3();
        Vector2 delta = new Vector2(cast_point.x_ - position.X, cast_point.z_ - position.Z);
        float length = delta.Length();
        if (length > 0f && !float.IsInfinity(length)) forward_ = delta / length;
    }

    /// <summary>在 120 FPS 的原动作时间轴上推进窗口，复用基础动作寿命。</summary>
    /// <param name="delta_time">服务器本次固定步进的毫秒数。</param>
    public override bool Update(int delta_time)
    {
        // 父类决定动作是否结束；被打断的旧技能不会继续命中。
        bool playing = base.Update(delta_time);
        if (!Casting() || forward_ == Vector2.Zero || instance_id_ != caster_.mainRoleInfo.BaseInfo.InstanceId) return playing;
        // 三段分别使用第48、第23、以及第23和54帧开启窗口，各持续到第14个后续帧。
        int previous = elapsed_ms_ * 120 / 1000;
        elapsed_ms_ += delta_time;
        int frame = elapsed_ms_ * 120 / 1000;
        int variant = GetUsingTimes();
        int count = 1;
        if (variant == 2) count = 2;
        while (window_ < count)
        {
            // 对应 SkillRuntimePresentation_3001001/2/3 的 StartFrame、EndFrame。
            int start = 23;
            if (variant == 0) start = 48;
            if (window_ == 1) start = 54;
            if (frame < start) break;
            if (previous <= start + 14)
            {
                // 第三段是普攻唯一带前冲的一段，真命中后客户端要据此停掉本段剩余位移。
                ulong stop_uuid = 0UL;
                if (variant == 2) stop_uuid = GetSkillUUID();
                TeamManager.Instance.OnMonsterHit(caster_, forward_, damage_, hit_targets_, stop_uuid);
            }
            if (frame <= start + 14) break;
            window_++;
            hit_targets_.Clear();
        }
        return playing;
    }
}
