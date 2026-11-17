using System.Collections.Generic;

/// <summary>合并角色各模块尚未刷新的变化；同一格子只记录一次，控制器刷新后再清除。</summary>
public sealed class ClientDirtyTracker
{
    /// <summary>保存一个模块或背包页尚未处理的刷新要求。</summary>
    private sealed class DirtyState
    {
        /// <summary>是否至少有一次变化要求重建整页。</summary>
        public bool FullRefresh;
        /// <summary>变化格子的编号，重复标记同一格不会增加记录。</summary>
        public readonly HashSet<int> Slots = new HashSet<int>();
        /// <summary>最近一次提供的变化来源；空来源不会覆盖旧值。</summary>
        public ClientChangeContext Context;
    }

    /// <summary>按模块保存脏状态。</summary>
    private readonly Dictionary<ClientDirtyModule, DirtyState> _states =
        new Dictionary<ClientDirtyModule, DirtyState>();

    /// <summary>按背包类型分别保存页面脏状态。</summary>
    private readonly Dictionary<KnapsackType, DirtyState> _knapsackStates =
        new Dictionary<KnapsackType, DirtyState>();

    #region 查询与判定
    /// <summary>复制 module 的待刷新记录到 snapshot；读取不会清除，未记录时返回 false。</summary>
    public bool TryGet(ClientDirtyModule module, out ClientDirtySnapshot snapshot)
    {
        DirtyState state;
        if (!_states.TryGetValue(module, out state))
        {
            snapshot = null;
            return false;
        }
        List<int> slots = new List<int>(state.Slots);
        snapshot = new ClientDirtySnapshot(state.FullRefresh, slots, state.Context);
        return true;
    }

    /// <summary>复制 bagType 背包页的待刷新记录到 snapshot；读取后仍保留原记录。</summary>
    public bool TryGetKnapsack(KnapsackType bagType, out ClientDirtySnapshot snapshot)
    {
        DirtyState state;
        if (!_knapsackStates.TryGetValue(bagType, out state))
        {
            snapshot = null;
            return false;
        }
        snapshot = new ClientDirtySnapshot(state.FullRefresh,
            new List<int>(state.Slots), state.Context);
        return true;
    }
    #endregion

    #region 核心逻辑
    /// <summary>记录 module 的变化；fullRefresh 要求整页刷新，slotIndex 非负时记录该格，context 保存最近一次来源。</summary>
    public void Mark(ClientDirtyModule module, bool fullRefresh, int slotIndex,
        ClientChangeContext context)
    {
        if (module == ClientDirtyModule.None)
        {
            return;
        }
        MarkState(module, fullRefresh, slotIndex, context);
    }

    /// <summary>合并 module 的刷新要求；整页标志只会累加为 true，格子编号通过集合去重。</summary>
    private void MarkState(ClientDirtyModule module, bool fullRefresh, int slotIndex,
        ClientChangeContext context)
    {
        DirtyState state;
        if (!_states.TryGetValue(module, out state))
        {
            state = new DirtyState();
            _states.Add(module, state);
        }
        state.FullRefresh = state.FullRefresh || fullRefresh;
        if (slotIndex >= 0)
        {
            state.Slots.Add(slotIndex);
        }
        if (context != null)
        {
            state.Context = context;
        }
    }

    /// <summary>按独立背包页标记变化，避免不同页互相触发刷新。</summary>
    public void MarkKnapsack(KnapsackType bagType, bool fullRefresh, int slotIndex,
        ClientChangeContext context)
    {
        MarkState(ClientDirtyModule.Knapsack, fullRefresh, slotIndex, context);
        DirtyState state;
        if (!_knapsackStates.TryGetValue(bagType, out state))
        {
            state = new DirtyState();
            _knapsackStates.Add(bagType, state);
        }
        state.FullRefresh = state.FullRefresh || fullRefresh;
        if (slotIndex >= 0)
        {
            state.Slots.Add(slotIndex);
        }
        if (context != null)
        {
            state.Context = context;
        }
    }

    /// <summary>清除 bagType 页已处理的变化；所有页都处理完后，同时清除背包模块标记。</summary>
    public void ClearKnapsack(KnapsackType bagType)
    {
        _knapsackStates.Remove(bagType);
        if (_knapsackStates.Count == 0)
        {
            _states.Remove(ClientDirtyModule.Knapsack);
        }
    }

    /// <summary>清除 module 的模块记录；不会清除独立保存的背包页记录。</summary>
    public void Clear(ClientDirtyModule module)
    {
        _states.Remove(module);
    }

    /// <summary>角色切换或断线时清理全部脏状态。</summary>
    public void ClearAll()
    {
        _states.Clear();
        _knapsackStates.Clear();
    }
    #endregion
}
