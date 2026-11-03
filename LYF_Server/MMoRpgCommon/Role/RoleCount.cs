using System;

/// <summary>角色单条业务次数及其刷新时间。</summary>
public class RoleCountItemInfo
{

    #region 构造
    /// <summary>建立字段为默认值的次数记录，供快照复制时逐项填充。</summary>
    public RoleCountItemInfo() { }

    /// <summary>建立一个业务对象的已用次数，刷新时间先记为未设置。</summary>
    /// <param name="type">商城购买或副本等行为编号。</param>
    /// <param name="id">该行为对应商品或副本的业务键。</param>
    /// <param name="count">初始累计次数。</param>
    public RoleCountItemInfo(long type, long id, long count)
    {
        Type = type;
        Id = id;
        Count = count;
        LastRefreshDate = DateTime.MinValue;
    }
    #endregion

    /// <summary>CountInfo.Action 行为类型。</summary>
    public long Type;

    /// <summary>CountInfo.Key 业务对象标识。</summary>
    public long Id;

    /// <summary>当前已经使用的次数。</summary>
    public long Count;

    /// <summary>上次执行周期刷新的时间。</summary>
    public DateTime LastRefreshDate;

    /// <summary>复制快照，避免保存期间引用运行时对象。</summary>
    public RoleCountItemInfo Clone()
    {
        return new RoleCountItemInfo
        {
            Type = Type,
            Id = Id,
            Count = Count,
            LastRefreshDate = LastRefreshDate
        };
    }
    
}

/// <summary>
/// 普通布尔开关：Set 可识别是否重复置位，Reset 清空；这里没有自旋、锁或线程同步。
/// </summary>
public class Switch
{
    /// <summary>内部标志位，false 表示未置位。</summary>
 public bool _flag=false;

 #region 构造与初始化
    /// <summary>默认构造，标志初始为 false。</summary>
    public Switch() { }

    /// <summary>空终结器，保留以与历史代码一致。</summary>
    ~Switch() { }

    /// <summary>手动复位标志。</summary>
 public void Reset() { _flag = false; }
 #endregion

 #region 核心逻辑
    /// <summary>置位成功返回 true；已置位则返回 false。</summary>
 public bool Set()
{ 
    if (_flag)
    {
        return false;
    }
    else
    {
        _flag = true; 
        return true;
    }
}

    /// <summary>读取标志后立即复位；原为 true 返回 true。</summary>
 public bool TestAndReset()
 {
    if (!_flag)
    {
        return false;
    }
    _flag = false;
    return true;
 }

    /// <summary>只读测试当前标志，不复位。</summary>
 public bool Test() { return _flag; }
 #endregion
};
