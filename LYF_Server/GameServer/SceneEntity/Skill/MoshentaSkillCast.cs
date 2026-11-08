using System.Numerics;

/// <summary>一次原技能的冻结施法上下文；延迟结果和投射物引用它而非下一次动作。</summary>
internal sealed class MoshentaSkillCast
{
    /// <summary>原技能配置。</summary>
    internal MoshentaSkillDefinition Skill;
    /// <summary>本次实际等级。</summary>
    internal int Level;
    /// <summary>起手选定实体UID。</summary>
    internal ulong TargetId;
    /// <summary>起手原施法位置。</summary>
    internal Vector3 Point;
    /// <summary>原施法方向角，单位度。</summary>
    internal float Facing;
    /// <summary>原ASPD对应动作时间倍率。</summary>
    internal float TimeScale;
    /// <summary>真正攻击开始逻辑毫秒。</summary>
    internal long Start;
    /// <summary>动作锁定截止逻辑毫秒。</summary>
    internal long End;
    /// <summary>施法起手攻击者属性快照。</summary>
    internal MoshentaDamageSnapshot Snapshot;
}
