using System;
using System.Collections.Generic;
using cfg;

/// <summary>客户端角色次数组件，保存商城、副本等业务次数快照。</summary>
public sealed class RoleCountComponent : RoleComponentBase
{
    /// <summary>次数发生变化时通知客户端控制器刷新展示。</summary>
    public event Action<RoleCountItemInfo> CountChanged;

    /// <summary>按行为类型与业务编号组成复合键保存次数。</summary>
    private readonly Dictionary<Tuple<long, long>, RoleCountItemInfo> _roleCountItemList =
        new Dictionary<Tuple<long, long>, RoleCountItemInfo>();

    /// <summary>创建客户端角色次数组件。</summary>
    public RoleCountComponent(ClientRole owner) : base(owner)
    {
    }

    /// <summary>读取次数，不存在时返回零。</summary>
    public long GetCount(RoleCountAction action, int key)
    {
        RoleCountItemInfo itemInfo;
        if (_roleCountItemList.TryGetValue(CreateKey(action, key), out itemInfo))
        {
            return itemInfo.Count;
        }
        return 0L;
    }

    /// <summary>兼容旧业务类型读取次数。</summary>
    public long GetCount(RoleCountType type, long id, long scope = 0L)
    {
        return GetCount(ResolveAction(type, scope), (int)id);
    }

    /// <summary>设置客户端次数模型，并通知控制器刷新界面。</summary>
    public bool SetCount(RoleCountAction action, int key, long count)
    {
        if (Owner == null || !Owner.IsRoleDataReady() || action == RoleCountAction.None ||
            key <= 0 || count < 0L)
        {
            return false;
        }

        RoleCountItemInfo itemInfo = GetOrCreateItemInfo(action, key);
        if (itemInfo == null)
        {
            return false;
        }
        if (itemInfo.Count == count)
        {
            return true;
        }

        itemInfo.Count = count;
        NotifyChanged(itemInfo);
        return true;
    }

    /// <summary>兼容旧业务类型设置次数。</summary>
    public bool SetCount(RoleCountType type, long id, long count, long scope = 0L)
    {
        return SetCount(ResolveAction(type, scope), (int)id, count);
    }

    /// <summary>按配置上限增加客户端次数模型。</summary>
    public bool IncrCount(RoleCountAction action, int key, long increment,
        bool checkLimit = true)
    {
        if (Owner == null || !Owner.IsRoleDataReady() || increment <= 0L)
        {
            return false;
        }

        RoleCountItemInfo itemInfo = GetOrCreateItemInfo(action, key);
        if (itemInfo == null || itemInfo.Count > long.MaxValue - increment)
        {
            return false;
        }

        long count = itemInfo.Count + increment;
        if (checkLimit)
        {
            long topLimit = GetTopLimit(itemInfo);
            if (topLimit >= 0L && count > topLimit)
            {
                return false;
            }
        }

        itemInfo.Count = count;
        NotifyChanged(itemInfo);
        return true;
    }

    /// <summary>兼容旧业务类型增加次数。</summary>
    public bool IncrCount(RoleCountType type, long id, long increment, long scope = 0L)
    {
        return IncrCount(ResolveAction(type, scope), (int)id, increment);
    }

    /// <summary>获取剩余次数，无上限时返回 -1。</summary>
    public long GetLeftCount(RoleCountAction action, int key)
    {
        CountInfo config = GetConfig(action, key);
        if (config == null || config.Limit <= 0)
        {
            return -1L;
        }

        long left = config.Limit - GetCount(action, key);
        if (left < 0L)
        {
            return 0L;
        }
        return left;
    }

    /// <summary>兼容旧商城类型查询剩余次数。</summary>
    public long GetLeftCount(RoleCountType type, long id)
    {
        return GetLeftCount(RoleCountAction.ShopPurchase, (int)id);
    }

    /// <summary>判断指定次数是否达到业务上限。</summary>
    public bool IsTopLimit(RoleCountType type, long id, long topLimit, long scope = 0L)
    {
        return GetCount(type, id, scope) >= topLimit;
    }

    /// <summary>使用登录回包完整替换客户端次数快照。</summary>
    public void Load(IList<RoleCountInfo> values)
    {
        _roleCountItemList.Clear();
        if (values == null)
        {
            return;
        }

        for (int index = 0; index < values.Count; index++)
        {
            RoleCountInfo value = values[index];
            if (value == null || value.Action <= 0 || value.CountKey <= 0 || value.Count < 0L)
            {
                continue;
            }

            RoleCountItemInfo itemInfo = new RoleCountItemInfo(
                value.Action, value.CountKey, value.Count);
            if (value.LastRefreshTime > 0L &&
                value.LastRefreshTime <= DateTime.MaxValue.Ticks)
            {
                itemInfo.LastRefreshDate = new DateTime(
                    value.LastRefreshTime, DateTimeKind.Utc);
            }
            _roleCountItemList[CreateKey((RoleCountAction)value.Action,
                value.CountKey)] = itemInfo;
        }
    }

    /// <summary>应用服务器推送的次数变化，并通知商城等界面刷新。</summary>
    /// <param name="syncInfo">服务器次数增量消息。</param>
    public void ApplyServerSync(CountUpdateSyncInfo syncInfo)
    {
        if (syncInfo == null)
        {
            return;
        }
        for (int index = 0; index < syncInfo.ItemList.Count; index++)
        {
            CountItemInfo item = syncInfo.ItemList[index];
            if (item == null || item.Type <= 0 || item.Id <= 0 || item.Count < 0)
            {
                continue;
            }
            SetCount((RoleCountAction)item.Type, (int)item.Id, item.Count);
        }
    }

    /// <summary>清理客户端次数快照与事件订阅。</summary>
    public override void Dispose()
    {
        _roleCountItemList.Clear();
        CountChanged = null;
        base.Dispose();
    }

    /// <summary>读取次数配置。</summary>
    public CountInfo GetConfig(RoleCountAction action, int key)
    {
        return LubanMgr.Instance.GetCountInfo(action, key);
    }

    /// <summary>读取现有次数项，必要时按零次创建客户端记录。</summary>
    private RoleCountItemInfo GetOrCreateItemInfo(RoleCountAction action, int key)
    {
        Tuple<long, long> mapKey = CreateKey(action, key);
        RoleCountItemInfo itemInfo;
        if (_roleCountItemList.TryGetValue(mapKey, out itemInfo))
        {
            return itemInfo;
        }
        if (GetConfig(action, key) == null)
        {
            return null;
        }

        itemInfo = new RoleCountItemInfo((long)action, key, 0L);
        _roleCountItemList.Add(mapKey, itemInfo);
        return itemInfo;
    }

    /// <summary>计算配置上限，负数表示不限制。</summary>
    private long GetTopLimit(RoleCountItemInfo itemInfo)
    {
        if (itemInfo == null)
        {
            return 0L;
        }
        CountInfo config = GetConfig((RoleCountAction)itemInfo.Type, (int)itemInfo.Id);
        if (config == null || config.Limit <= 0)
        {
            return -1L;
        }
        return config.Limit;
    }

    /// <summary>通知订阅者刷新指定次数展示。</summary>
    private void NotifyChanged(RoleCountItemInfo itemInfo)
    {
        if (itemInfo != null && Owner != null)
        {
            // 次数变化只记录模块脏位，商城、副本和任务界面按需消费。
            ClientChangeContext context = new ClientChangeContext("RoleCount");
            MarkDirty(ClientDirtyModule.RoleCount, false, -1, context);
        }
    }

    /// <summary>构造次数字典复合键。</summary>
    private static Tuple<long, long> CreateKey(RoleCountAction action, long key)
    {
        return Tuple.Create((long)action, key);
    }

    /// <summary>将旧业务类型映射为统一次数行为。</summary>
    private static RoleCountAction ResolveAction(RoleCountType type, long scope)
    {
        if (scope > 0L)
        {
            return (RoleCountAction)scope;
        }
        if (type == RoleCountType.Dungeon)
        {
            return RoleCountAction.DungeonEnter;
        }
        return RoleCountAction.ShopPurchase;
    }
}
