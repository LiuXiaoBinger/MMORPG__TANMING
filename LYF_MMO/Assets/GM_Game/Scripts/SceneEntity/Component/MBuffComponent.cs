using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 实体 Buff 组件，独立于角色数据和普通组件容器。
/// 以 Buff 配置 ID 为键、等级为值维护当前生效 Buff 表，供状态机和表现层查询。
/// </summary>
public sealed class MBuffComponent : EntityComponentBase
{
    /// <summary>当前生效 Buff 表，键为 Buff 配置 ID，值为 Buff 等级。</summary>
    private readonly Dictionary<int, int> _buffLevels = new Dictionary<int, int>();

    /// <summary>构造 Buff 组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public MBuffComponent(ROObject owner) : base(owner) { }

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Buff"/>。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Buff; } }

    /// <summary>查询实体是否已拥有指定 Buff。</summary>
    /// <param name="buffId">Buff 配置 ID。</param>
    /// <returns>已拥有返回 true，否则返回 false。</returns>
    public bool HasBuff(int buffId) { return _buffLevels.ContainsKey(buffId); }

    /// <summary>查询指定 Buff 的当前等级，未拥有时返回 0。</summary>
    /// <param name="buffId">Buff 配置 ID。</param>
    /// <returns>Buff 等级。</returns>
    public int GetLevel(int buffId)
    {
        int level;
        if (_buffLevels.TryGetValue(buffId, out level)) return level;
        return 0;
    }

    /// <summary>设置或更新 Buff 等级，ID 或等级非正时视为移除。</summary>
    /// <param name="buffId">Buff 配置 ID。</param>
    /// <param name="level">Buff 等级，小于等于 0 时移除该 Buff。</param>
    public void SetBuff(int buffId, int level)
    {
        if (buffId <= 0 || level <= 0) { _buffLevels.Remove(buffId); return; }
        _buffLevels[buffId] = level;
    }

    /// <summary>移除指定 Buff。</summary>
    /// <param name="buffId">Buff 配置 ID。</param>
    public void RemoveBuff(int buffId) { _buffLevels.Remove(buffId); }

    /// <summary>对象池回收时清空全部 Buff 记录。</summary>
    public override void Shutdown() { _buffLevels.Clear(); }

    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
