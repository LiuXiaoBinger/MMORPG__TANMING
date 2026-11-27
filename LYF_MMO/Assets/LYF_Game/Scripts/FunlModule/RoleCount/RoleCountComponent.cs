using System;
using System.Collections.Generic;
using cfg;

/// <summary>客户端角色次数组件，保存商城、副本等业务次数快照。</summary>
public sealed class RoleCountComponent : RoleComponentBase
{
    /// <summary>角色存档中 DungeonBossKill 的协议行为值，旧版客户端公共程序集也使用同一数值键。</summary>
    private const long DungeonBossKillAction = 12L;

    /// <summary>按游戏服规则取得新秀每日、其余难度周日开始的 UTC 日序号。</summary>
    /// <param name="difficulty">正式难度编号，新秀为 1。</param><param name="utc">当前 UTC 时刻。</param>
    public static int BossPeriodDay(int difficulty, DateTime utc)
    {
        // 与角色持久化记录比较的周期起始日。
        if (difficulty == DungeonFinalWeek.Difficulty) return DungeonFinalWeek.Period(utc);
        DateTime day = utc.Date;
        if (difficulty != 1) day = day.AddDays(-(int)day.DayOfWeek);
        return (int)(day.Ticks / TimeSpan.TicksPerDay);
    }

    /// <summary>本角色收到终绝成绩或领箱状态后通知已打开的周赏。</summary>
    public event Action DungeonFinalUpdated;

    /// <summary>列出上周及此前仍欠领的有成绩周期，最近一期在前。</summary>
    /// <param name="current">本周的北京周五日序。</param>
    public List<int> DungeonFinalHistory(int current)
    {
        // 成绩以周为键，离线跨过多周仍可补领原来的盒子。
        List<int> weeks = new List<int>();
        List<RoleCountItemInfo> rows = new List<RoleCountItemInfo>(_roleCountItemList.Values);
        for (int index = 0; index < rows.Count; index++)
        {
            // 每周最高层只有一条，本周不能在上周页被领取。
            RoleCountItemInfo row = rows[index];
            if (row.Type != DungeonFinalWeek.Highest || row.Id >= current || row.Count <= 0) continue;
            int week = (int)row.Id;
            if (week == current - 7 || GetCount((RoleCountAction)DungeonFinalWeek.Claimed, week) == 0) weeks.Add(week);
        }
        weeks.Sort((left, right) => right.CompareTo(left));
        return weeks;
    }

    /// <summary>从登录次数快照或本次在线的权威击杀通知读取单只首领状态。</summary>
    /// <param name="awardId">唯一标识副本、难度及首领的奖励行编号。</param><param name="period">当前展示的周期起始日。</param>
    public bool IsDungeonBossDefeated(int awardId, int period)
    {
        return GetCount((RoleCountAction)DungeonBossKillAction, awardId) == period;
    }

    /// <summary>保留的次数变化事件；当前类内没有触发它，界面通过 DirtyTracker 得知需要刷新。</summary>
    public event Action<RoleCountItemInfo> CountChanged;

    /// <summary>按行为类型与业务编号组成复合键保存次数。</summary>
    private readonly Dictionary<Tuple<long, long>, RoleCountItemInfo> _roleCountItemList =
        new Dictionary<Tuple<long, long>, RoleCountItemInfo>();

    #region 构造与初始化
    /// <summary>创建客户端角色次数组件。</summary>
    public RoleCountComponent(PlayerEntity owner) : base(owner)
    {
    }

    /// <summary>清理客户端次数快照与事件订阅。</summary>
    public override void Dispose()
    {
        _roleCountItemList.Clear();
        CountChanged = null;
        base.Dispose();
    }
    #endregion

    /// <summary>当 itemInfo 和所属角色有效时，登记次数模块需要刷新；不直接调用 CountChanged。</summary>
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

    #region 查询与判定
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

    /// <summary>旧接口保留 type 参数，但实际始终按商品购买次数查询 id。</summary>
    public long GetLeftCount(RoleCountType type, long id)
    {
        return GetLeftCount(RoleCountAction.ShopPurchase, (int)id);
    }

    /// <summary>判断指定次数是否达到业务上限。</summary>
    public bool IsTopLimit(RoleCountType type, long id, long topLimit, long scope = 0L)
    {
        return GetCount(type, id, scope) >= topLimit;
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
    #endregion

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
            // 计数快照缺失或动作、键值非法时跳过。
            if (value == null || value.Action <= 0 || value.CountKey <= 0)
            {
                continue;
            }
            // 计数值不得为负。
            if (value.Count < 0L)
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

    #region 核心逻辑
    /// <summary>写入 action、key 对应的客户端次数 count，并标记次数界面需要刷新。</summary>
    public bool SetCount(RoleCountAction action, int key, long count)
    {
        // 所属角色缺失、数据未就绪或动作为空时拒绝写入。
        if (Owner == null || !Owner.IsRoleDataReady() || action == RoleCountAction.None)
        {
            return false;
        }
        // 计数键必须为正，且计数值不得为负。
        if (key <= 0 || count < 0L)
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

    /// <summary>按服务器给出的绝对次数更新本地记录，再标记相关界面需要刷新。</summary>
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
            // 计数项缺失或类型、ID 非法时跳过。
            if (item == null || item.Type <= 0 || item.Id <= 0)
            {
                continue;
            }
            // 计数值不得为负。
            if (item.Count < 0)
            {
                continue;
            }
            // 首领进度键来自奖励表，游戏服下发的绝对周期与角色登录存档相同。
            // 周箱的层数、不同副本数与领取标记也使用服务端绝对值。
            bool weekly = item.Type == DungeonFinalWeek.Highest || item.Type == DungeonFinalWeek.Distinct || item.Type == DungeonFinalWeek.Claimed;
            if (item.Type == DungeonBossKillAction || weekly)
            {
                // 每只首领独立记录，显示名相同也不会误标其他首领。
                Tuple<long, long> key = Tuple.Create(item.Type, item.Id);
                _roleCountItemList[key] = new RoleCountItemInfo(item.Type, item.Id, item.Count);
                if (weekly) DungeonFinalUpdated?.Invoke();
                continue;
            }
            SetCount((RoleCountAction)item.Type, (int)item.Id, item.Count);
        }
    }
    #endregion
}
