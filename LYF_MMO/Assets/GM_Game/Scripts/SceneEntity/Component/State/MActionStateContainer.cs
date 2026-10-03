using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 状态机专用状态容器，负责注册、查询、遍历和池回收动作状态。
/// 同时维护按 StateDefine 索引、按 (StateDefine, EntityType) 复合键索引和按注册顺序遍历三套结构。
/// </summary>
public sealed class MActionStateContainer
{
    /// <summary>按状态枚举索引的主字典，用于状态机按 StateDefine 直接查找。</summary>
    private readonly Dictionary<StateDefine, MActionComponent> _states =
        new Dictionary<StateDefine, MActionComponent>();
    /// <summary>按 (StateDefine, EntityType) 复合键索引的辅助字典，支持按实体类型细分状态。</summary>
    private readonly Dictionary<ActionStateKey, MActionComponent> _keyStates =
        new Dictionary<ActionStateKey, MActionComponent>();
    /// <summary>按注册顺序排列的状态列表，用于 Clear 时逆序回收。</summary>
    private readonly List<MActionComponent> _ordered = new List<MActionComponent>();

    /// <summary>已注册状态总数。</summary>
    public int Count { get { return _ordered.Count; } }

    /// <summary>注册一个动作状态到主字典、复合键字典和顺序列表。</summary>
    /// <param name="state">要注册的动作状态实例。</param>
    /// <returns>注册成功返回 true，参数为空或同 StateDefine 已注册返回 false。</returns>
    public bool Register(MActionComponent state)
    {
        if (state == null || _states.ContainsKey(state.SelfState))
        {
            return false;
        }
        _states.Add(state.SelfState, state);
        // 取宿主实体类型作为复合键的一部分，支持后续按实体类型细分状态
        MEntity entity = state.EntityForState;
        EntityType entityType = EntityType.Role;
        if (entity != null)
        {
            entityType = entity.EntityType;
        }
        _keyStates.Add(new ActionStateKey(state.SelfState, entityType), state);
        _ordered.Add(state);
        return true;
    }

    /// <summary>按状态枚举尝试获取动作状态。</summary>
    /// <param name="state">目标状态枚举。</param>
    /// <param name="component">输出参数，成功时为动作状态实例。</param>
    /// <returns>获取成功返回 true。</returns>
    public bool TryGet(StateDefine state, out MActionComponent component)
    {
        return _states.TryGetValue(state, out component);
    }

    /// <summary>按状态枚举获取动作状态，未注册时返回 null。</summary>
    /// <param name="state">目标状态枚举。</param>
    /// <returns>动作状态实例，未注册返回 null。</returns>
    public MActionComponent Get(StateDefine state)
    {
        MActionComponent component;
        if (_states.TryGetValue(state, out component))
        {
            return component;
        }
        return null;
    }

    /// <summary>按复合键尝试获取动作状态。</summary>
    /// <param name="key">复合状态键，由 StateDefine 和 EntityType 组成。</param>
    /// <param name="component">输出参数，成功时为动作状态实例。</param>
    /// <returns>获取成功返回 true。</returns>
    public bool TryGet(ActionStateKey key, out MActionComponent component)
    {
        return _keyStates.TryGetValue(key, out component);
    }

    /// <summary>逆序释放全部已注册动作状态到动作池，并清空三套索引。</summary>
    /// <param name="owner">宿主实体，用于回收时的上下文（当前未直接使用）。</param>
    public void Clear(MEntity owner)
    {
        // 逆序回收，保证后注册的状态先归还
        for (int index = _ordered.Count - 1; index >= 0; index--)
        {
            MComponentMgr.Instance.ReleaseActionState(_ordered[index]);
        }
        _ordered.Clear();
        _states.Clear();
        _keyStates.Clear();
    }

    /// <summary>仅清空内部索引，不回收动作状态实例（用于外部已自行管理生命周期的场景）。</summary>
    public void Reset()
    {
        _ordered.Clear();
        _states.Clear();
        _keyStates.Clear();
    }
}
