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

public class Switch
{
    public Switch() { }
    ~Switch() { } 
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
 public bool TestAndReset()
 {
    if (!_flag)
    {
        return false;
    }
    _flag = false;
    return true;
 }
 public bool Test() { return _flag; }
 public void Reset() { _flag = false; }


 public bool _flag=false;
};
