using System.Collections.Generic;

/// <summary>
/// 将角色各组件产生的持久化字段汇总为一次 SaveRoleDataReq。
/// 当前接入全部物品容器（包括不占格子的虚拟物品背包），后续角色模块可继续通过 SaveField 添加独立字段。
/// </summary>
public sealed class RoleDataFieldManager
{
    private readonly SaveRoleDataReq _request;

    public RoleDataFieldManager(int roleId, long version)
    {
        _request = new SaveRoleDataReq
        {
            RoleId = roleId,
            Version = version
        };
    }

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

    public SaveRoleDataReq GetRequest()
    {
        return _request;
    }
}
