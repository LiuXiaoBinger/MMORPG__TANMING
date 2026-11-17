using UnityEngine;

/// <summary>技能开始事件的强类型载荷，派发结束即回收，不允许跨帧持有本实例。</summary>
public sealed class SkillStartedEventArgs : IEventArg
{
    /// <summary>本次释放的技能主表 ID。</summary>
    public int SkillId { get; private set; }

    /// <summary>本次技能等级对应的效果表 ID，用于拒绝错配的表现配置。</summary>
    public int EffectId { get; private set; }

    /// <summary>本次释放使用的已学习等级，从 1 开始。</summary>
    public int Level { get; private set; }

    /// <summary>本次施法从本地请求到服务器广播共用的非零 UUID。</summary>
    public ulong SkillUuid { get; private set; }

    /// <summary>目标实体在当前客户端场景世界中的 UID，无目标时为零。</summary>
    public ulong TargetUid { get; private set; }

    /// <summary>世界空间施法点，单位为 Unity 世界单位。</summary>
    public Vector3 CastPoint { get; private set; }

    /// <summary>本次事件是否由本地输入先于服务器确认发起。</summary>
    public bool IsPrediction { get; private set; }

    /// <summary>池化生命周期标记，true 表示本实例已被回收。</summary>
    public bool Deprecated { get; set; }

    /// <summary>初始化空载荷，仅供技能事件池创建实例。</summary>
    public SkillStartedEventArgs()
    {
        ResetData();
    }

    /// <summary>从事件池取得一次技能开始派发所需的强类型载荷。</summary>
    /// <param name="skillId">技能主表 ID，必须为正数。</param>
    /// <param name="effectId">当前等级效果配置 ID，必须与核心配置一致。</param>
    /// <param name="level">角色已学习的技能等级，从 1 开始。</param>
    /// <param name="skillUuid">贯穿请求、预表现和广播的非零单次施法 UUID。</param>
    /// <param name="targetUid">目标的客户端运行时 UID，无目标时为零。</param>
    /// <param name="castPoint">世界空间施法点，单位为 Unity 世界单位。</param>
    /// <param name="isPrediction">是否在服务器确认之前播放本地表现。</param>
    /// <returns>派发结束后由实体事件分发器自动回收的载荷。</returns>
    public static SkillStartedEventArgs Create(int skillId, int effectId, int level,
        ulong skillUuid, ulong targetUid,
        Vector3 castPoint, bool isPrediction)
    {
        // 本次事件使用的池化实例，避免连续施法造成不必要的托管分配。
        SkillStartedEventArgs eventArgs = MEventPool<SkillStartedEventArgs>.GetEvent();
        eventArgs.ResetData();
        eventArgs.SkillId = skillId;
        eventArgs.EffectId = effectId;
        eventArgs.Level = level;
        eventArgs.SkillUuid = skillUuid;
        eventArgs.TargetUid = targetUid;
        eventArgs.CastPoint = castPoint;
        eventArgs.IsPrediction = isPrediction;
        return eventArgs;
    }

    /// <summary>复制当前施法快照；跨回调保存时由调用方回收复制件。</summary>
    /// <returns>字段相同、生命周期独立的池化技能事件载荷。</returns>
    public IEventArg Clone()
    {
        return Create(SkillId, EffectId, Level, SkillUuid,
            TargetUid, CastPoint, IsPrediction);
    }

    /// <summary>清除本次施法字段并把实例归还技能事件池。</summary>
    public void Recycle()
    {
        ResetData();
        MEventPool<SkillStartedEventArgs>.Recycle(this);
    }

    /// <summary>重置全部会跨施法变化的字段，防止对象池复用时携带旧目标。</summary>
    private void ResetData()
    {
        SkillId = 0;
        EffectId = 0;
        Level = 0;
        SkillUuid = 0;
        TargetUid = 0;
        CastPoint = Vector3.zero;
        IsPrediction = false;
    }
}
