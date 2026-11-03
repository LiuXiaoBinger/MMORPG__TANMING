using System.Collections.Generic;

/// <summary>
/// 将角色各组件产生的持久化字段汇总为一次 SaveRoleDataReq。
/// 把本次变化的完整背包列表和可选的完整次数列表装入同一角色版本；没有变化的背包无需重复提交。
/// </summary>
public sealed class RoleDataFieldManager
{
    /// <summary>本次角色存档的协议对象，组件依次把各自快照追加到其中。</summary>
    private readonly SaveRoleDataReq _request;

    /// <summary>建立一次角色保存请求的身份与版本。</summary>
    /// <param name="roleId">数据库角色编号。</param>
    /// <param name="version">这批组件快照共用的角色保存版本。</param>
    public RoleDataFieldManager(int roleId, long version)
    {
        _request = new SaveRoleDataReq
        {
            RoleId = roleId,
            Version = version
        };
    }

    /// <summary>追加一个发生变化的背包的完整物品列表。</summary>
    /// <param name="itemDataList">背包分类及该分类当前全部物品；空列表可表示清空背包。</param>
    public void SaveField(ItemDataList itemDataList)
    {
        if (itemDataList != null)
        {
            _request.ItemDataLists.Add(itemDataList);
        }
    }

    /// <summary>追加角色次数快照，供 CenterServer 延迟持久化。</summary>
    public void SaveRoleCounts(IEnumerable<RoleCountItemInfo> counts)
    {
        _request.RoleCountSnapshot = true;
        if (counts == null) return;
        foreach (RoleCountItemInfo value in counts)
        {
            if (value == null) continue;
            _request.RoleCountInfoList.Add(new RoleCountInfo
            {
                Action = (int)value.Type,
                CountKey = (int)value.Id,
                Count = value.Count,
                LastRefreshTime = value.LastRefreshDate.Ticks
            });
        }
    }

    /// <summary>返回已组装的同一个请求对象，交给 RoleNetSender 等待保存确认。</summary>
    public SaveRoleDataReq GetRequest()
    {
        return _request;
    }
}
