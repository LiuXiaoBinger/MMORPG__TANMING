using System;
using System.Collections.Generic;
using cfg;
using Google.Protobuf;

/// <summary>角色商城、副本等业务次数管理器，按 Action 与业务键保存计数。</summary>
public sealed class RoleCountComponent : RoleComponentBase
{
    /// <summary>等价于参考项目 map<pair<Action,Key>, RoleCountItemInfo*> 的强类型字典。</summary>
    private readonly Dictionary<Tuple<long, long>, RoleCountItemInfo> _roleCountItemList = new Dictionary<Tuple<long, long>, RoleCountItemInfo>();
    // 计数变化通知包，复用后发送给客户端。
   // PtcG2C_CountInfoUpdateNotify notify_;
    /// <summary>是否存在未确认的修改。</summary>
    private bool _needSave;
    /// <summary>当前修改代数。</summary>
    private long _changeGeneration;
    /// <summary>最近保存快照的修改代数。</summary>
    private long _savedGeneration;
    /// <summary>最近保存快照对应的角色保存版本，ACK 必须与该版本一致。</summary>
    private long _savedVersion;

    /// <summary>创建角色次数组件。</summary>
    public RoleCountComponent(OnlineRole owner) : base(owner) { }

    /// <summary>读取次数，不存在时返回零。</summary>
    public long GetCount(RoleCountAction action, int key)
    {
        if (Owner == null || !Owner.IsRoleDataReady())
        {
            return 0L;
        }
        var item_info = GetAndCreateItemInfo(action, key);
        if (item_info==null) 
        {
            return 0;
        }
        return item_info.Count;
        
    }

    RoleCountItemInfo GetAndCreateItemInfo(RoleCountAction action, int key)
    {
        
        RoleCountItemInfo value;
        if (_roleCountItemList.TryGetValue
                (CreateKey(action, key), out value))
        {
            return value;
        }

        CountInfo conf = LubanMgr.Instance.GetCountInfo(action, key);
        if (conf == null)
        {
            // 没有次数配置时不能创建运行时记录，否则访问配置字段会触发空引用异常。
            return null;
        }

        // 新记录默认使用刷新后的次数；没有刷新次数时使用配置上限。
        long initialCount = 0;
      
        RoleCountItemInfo info = new RoleCountItemInfo((long)action,
            key, initialCount);
        _roleCountItemList[CreateKey(action, key)] = info;
        return info;
    }
    /// <summary>兼容旧业务类型查询。</summary>
    public long GetCount(RoleCountType type, long id, long scope = 0L) { return GetCount(ResolveAction(type, scope), (int)id); }

    /// <summary>设置次数并标记脏数据。</summary>
    public bool SetCount(RoleCountAction action, int key, long count)
    {
        if (Owner == null || !Owner.IsRoleDataReady())
        {
            return false;
        }
        if (action == RoleCountAction.None || key <= 0 || count < 0L) return false;
        Tuple<long, long> mapKey = CreateKey(action, key);
        RoleCountItemInfo value;
        if (!_roleCountItemList.TryGetValue(mapKey, out value))
        {
            value = new RoleCountItemInfo((long)action, key, 0L);
            _roleCountItemList.Add(mapKey, value);
        }
        if (value.Count == count) return true;
        value.Count = count;
        MarkChanged();
        return true;
    }

    /// <summary>兼容旧业务设置入口。</summary>
    public bool SetCount(RoleCountType type, long id, long count, long scope = 0L) { return SetCount(ResolveAction(type, scope), (int)id, count); }

    /// <summary>按 CountInfo 上限增加次数。</summary>
    public bool IncrCount(RoleCountAction action, int key, long increment,bool check_limit = true)
    {
        if (Owner == null || !Owner.IsRoleDataReady())
        {
            return false;
        }
        if (increment <= 0L) return true;
        
        var item_info = GetAndCreateItemInfo(action, key);
        if (item_info == null)
        {
            LogMsg.Info(string.Format("roleid:%llu get and create count item info failed type:%lld id:%lld", 
                Owner.GetID(), action, key));
            return false;
        }
        long count = item_info.Count + increment;
        if (check_limit)
        {
            if (count < 0)
            {
                LogMsg.Info(string.Format("roleid:%llu set count < 0 now:%lld" +
                                           " incr:%lld type:%lld id:%lld", 
                    Owner.GetID(), item_info.Count, increment, (int)action, key));
                return false;
            }
            long top_limit = GetTopLimit(item_info);
            if (top_limit >= 0 && count > top_limit)
            {
                LogMsg.Info(msg: string.Format
                ("roleid:%llu set count > %lld now:%lld " +
                 "incr:%lld " + "type:%lld id:%lld", Owner.GetID(), top_limit, 
                    item_info.Count, increment,(int)action, key));
                return false;
            }
        }
        
        item_info.Count = count;
        SetModify();
        var conf = GetConfig((RoleCountAction)item_info.Type, (int)item_info.Id);

        if (conf != null && conf.TellClient == 1)
        {
            NotifyToClient(item_info);
        }

        return true;

    }

    private void NotifyToClient(RoleCountItemInfo itemInfo)
    {
        if (itemInfo==null)
        {
            return;
        }

        CountUpdateSyncInfo info =  new CountUpdateSyncInfo();
        // 次数同步属于成功通知，明确填写结果码，网关才能按同步协议透传。
        info.CmdCode = CmdCode.Succeed;
        CountItemInfo ItemInfo = new CountItemInfo();
        ItemInfo.Type = itemInfo.Type;
        ItemInfo.Id = itemInfo.Id;
        ItemInfo.Count = (int)itemInfo.Count;
        info.ItemList.Add(ItemInfo);

        Owner.SendToClient(NetDefine.CMD_CountUpdateSyncInfoCode, info.ToByteString());
    }

    private long GetTopLimit(RoleCountItemInfo itemInfo)
    {
        if (itemInfo ==null)
        {
            return 0;
        }
        var conf_ptr = GetConfig((RoleCountAction)itemInfo.Type, (int)itemInfo.Id);
        
        if (conf_ptr == null)
        {
            return 0;
        }
        return conf_ptr.Limit + GetExtraActivityCount(itemInfo);
    }

    /// <summary>兼容旧业务增加入口。</summary>
    public bool IncrCount(RoleCountType type, long id, long increment, long scope = 0L) { return IncrCount(ResolveAction(type, scope), (int)id, increment); }

    /// <summary>获取剩余次数，无上限时返回 -1。</summary>
    public long GetLeftCount(RoleCountAction action, int key)
    {
        CountInfo config = GetConfig(action, key);
        if (config == null || config.Limit <= 0) 
            return -1L;
        long left = config.Limit - GetCount(action, key);
        if (left < 0L) return 0L;
        return left;
    }

    /// <summary>兼容旧商城查询。</summary>
    public long GetLeftCount(RoleCountType type, long id) { return GetLeftCount(RoleCountAction.ShopPurchase, (int)id); }

    /// <summary>判断是否达到上限。</summary>
    public bool IsTopLimit(RoleCountType type, long id, long topLimit, long scope = 0L) { return GetCount(type, id, scope) >= topLimit; }

    /// <summary>按配置刷新已过期的次数。</summary>
    public void Refresh(DateTime now)
    {
        if (Owner == null || !Owner.IsRoleDataReady())
        {
            return;
        }

        List<RoleCountItemInfo> values = new List<RoleCountItemInfo>(_roleCountItemList.Values);
        for (int i = 0; i < values.Count; i++)
        {
            RoleCountItemInfo value = values[i];
            CountInfo config = GetConfig((RoleCountAction)value.Type, (int)value.Id);
            if (config == null || config.RefreshType == 0 || !IsRefreshDue(value.LastRefreshDate, now, config.RefreshType)) continue;
            value.Count = config.RefreshNum;
            value.LastRefreshDate = now;
            MarkChanged();
        }
    }

    /// <summary>装载登录回包的次数快照，装载不产生脏数据。</summary>
    public void Load(IEnumerable<RoleCountItemInfo> values)
    {
        _roleCountItemList.Clear();
        if (values != null)
        {
            List<RoleCountItemInfo> loadedValues = new List<RoleCountItemInfo>(values);
            for (int i = 0; i < loadedValues.Count; i++)
            {
                RoleCountItemInfo value = loadedValues[i];
                if (value != null && value.Type > 0L && value.Id > 0L) _roleCountItemList[Tuple.Create(value.Type, value.Id)] = value;
            }
        }
        _needSave = false;
        _changeGeneration = 0L;
        _savedGeneration = 0L;
        _savedVersion = 0L;
    }

    /// <summary>复制当前快照，ACK 前不清理脏状态。</summary>
    public List<RoleCountItemInfo> Save()
    {
        List<RoleCountItemInfo> result = new List<RoleCountItemInfo>();
        if (Owner == null || !Owner.IsRoleDataReady())
        {
            return result;
        }
        List<RoleCountItemInfo> values = new List<RoleCountItemInfo>(_roleCountItemList.Values);
        for (int i = 0; i < values.Count; i++)
        {
            RoleCountItemInfo value = values[i];
            result.Add(value.Clone());
        }
        return result;
    }

    /// <summary>把次数快照写入统一角色保存请求。</summary>
    public override bool Save(RoleDataFieldManager fieldManager, long saveVersion)
    {
        if (Owner == null || !Owner.IsRoleDataReady() || !_needSave || fieldManager == null)
        {
            return false;
        }
        List<RoleCountItemInfo> snapshot = Save();
        _savedGeneration = _changeGeneration;
        _savedVersion = saveVersion;
        fieldManager.SaveRoleCounts(snapshot);
        return true;
    }

    /// <summary>ACK 后仅清理已包含在快照中的修改。</summary>
    public override void OnSaveAcknowledged(long saveVersion)
    {
        if (_needSave && _savedVersion == saveVersion && _changeGeneration == _savedGeneration)
        {
            _needSave = false;
        }
    }

    /// <summary>组件是否需要保存。</summary>
    public override bool NeedSave() { return _needSave; }
    /// <summary>获取模块编号。</summary>
    public override RoleModuleType GetModuleId() { return RoleModuleType.kRoleModuleTypeCount; }
    /// <summary>固定帧刷新。</summary>
    public override void Update(int deltaMilliseconds) { Refresh(DateTime.UtcNow); }
    /// <summary>释放组件。</summary>
    public override void Dispose()
    {
        _roleCountItemList.Clear();
        _needSave = false;
        _savedVersion = 0L;
        base.Dispose();
    }

    /// <summary>标记一次修改。</summary>
    /// <summary>记录次数变化并重置该模块的修改时间。</summary>
    private void MarkChanged()
    {
        if (_changeGeneration < long.MaxValue)
        {
            _changeGeneration++;
        }
        _needSave = true;
        SetModify();
    }
    /// <summary>构造强类型复合键。</summary>
    private static Tuple<long, long> CreateKey(RoleCountAction action, long key) { return Tuple.Create((long)action, key); }
    /// <summary>将旧模块类型映射为通用行为。</summary>
    private static RoleCountAction ResolveAction(RoleCountType type, long scope)
    {
        if (scope > 0L) return (RoleCountAction)scope;
        if (type == RoleCountType.Dungeon) return RoleCountAction.DungeonEnter;
        return RoleCountAction.ShopPurchase;
    }
    /// <summary>判断每日或每周刷新是否到期，RefreshType 约定 1=每日、2=每周。</summary>
    private static bool IsRefreshDue(DateTime last, DateTime now, int refreshType)
    {
        if (last == DateTime.MinValue) return true;
        if (refreshType == 1) return last.Date != now.Date;
        if (refreshType == 2)
        {
            DateTime lastWeek = last.Date.AddDays(-(int)last.DayOfWeek);
            DateTime nowWeek = now.Date.AddDays(-(int)now.DayOfWeek);
            return lastWeek != nowWeek;
        }
        return false;
    }
    // 获取活动额外提供的次数。
    int GetExtraActivityCount( RoleCountItemInfo info)
    {
        //todo
        return 0;
    }
    /// <summary>查询次数配置。</summary>
    public CountInfo GetConfig(RoleCountAction action, int key) 
    { return LubanMgr.Instance.GetCountInfo(action, key); }
}
