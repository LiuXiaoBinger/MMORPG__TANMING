using System;

/// <summary>角色单条业务次数及其刷新时间。</summary>
public class RoleCountItemInfo
{
    /// <summary>CountInfo.Action 行为类型。</summary>
    public long Type;
    /// <summary>CountInfo.Key 业务对象标识。</summary>
    public long Id;
    /// <summary>当前已经使用的次数。</summary>
    public long Count;
    /// <summary>上次执行周期刷新的时间。</summary>
    public DateTime LastRefreshDate;

    public RoleCountItemInfo() { }
    public RoleCountItemInfo(long type, long id, long count)
    {
        Type = type;
        Id = id;
        Count = count;
        LastRefreshDate = DateTime.MinValue;
    }

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
/// 轻量一次性自旋标志：用于标记某段逻辑是否已进入/已处理，保证同一周期内不重复执行。
/// </summary>
public class Switch
{
    /// <summary>默认构造，标志初始为 false。</summary>
    public Switch() { }
    /// <summary>空终结器，保留以与历史代码一致。</summary>
    ~Switch() { }
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
    /// <summary>手动复位标志。</summary>
 public void Reset() { _flag = false; }


    /// <summary>内部标志位，false 表示未置位。</summary>
 public bool _flag=false;
};
