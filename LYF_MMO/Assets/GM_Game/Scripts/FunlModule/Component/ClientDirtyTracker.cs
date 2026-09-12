using System.Collections.Generic;

/// <summary>客户端角色脏标记聚合器，记录模块和局部格位变化。</summary>
public sealed class ClientDirtyTracker
{
    private sealed class DirtyState
    {
        public bool FullRefresh;
        public readonly HashSet<int> Slots = new HashSet<int>();
        public ClientChangeContext Context;
    }

    /// <summary>按模块保存脏状态。</summary>
    private readonly Dictionary<ClientDirtyModule, DirtyState> _states =
        new Dictionary<ClientDirtyModule, DirtyState>();
    /// <summary>按背包类型分别保存页面脏状态。</summary>
    private readonly Dictionary<KnapsackType, DirtyState> _knapsackStates =
        new Dictionary<KnapsackType, DirtyState>();

    /// <summary>标记模块发生变化，可附带一个或多个受影响格位。</summary>
    public void Mark(ClientDirtyModule module, bool fullRefresh, int slotIndex,
        ClientChangeContext context)
    {
        if (module == ClientDirtyModule.None)
        {
            return;
        }
        MarkState(module, fullRefresh, slotIndex, context);
    }

    /// <summary>写入通用模块状态的内部实现，避免背包页标记产生递归。</summary>
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

    /// <summary>读取模块脏状态但不清除，便于控制器决定是否消费。</summary>
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

    /// <summary>读取指定背包页脏状态但不清除。</summary>
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

    /// <summary>消费指定背包页脏状态。</summary>
    public void ClearKnapsack(KnapsackType bagType)
    {
        _knapsackStates.Remove(bagType);
        if (_knapsackStates.Count == 0)
        {
            _states.Remove(ClientDirtyModule.Knapsack);
        }
    }

    /// <summary>消费并清除指定模块的脏状态。</summary>
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
}



