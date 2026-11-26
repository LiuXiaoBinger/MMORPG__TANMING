using SqlSugar;

/// <summary>RO 主动添加好友的一条有向关系；反向记录存在时双方互为好友。</summary>
[SugarTable("role_friend")]
public sealed class RoleFriendTable
{
    /// <summary>主动添加好友的角色主键。</summary>
    [SugarColumn(IsPrimaryKey = true)]
    public int RoleId { get; set; }
    /// <summary>被添加的同区角色主键。</summary>
    [SugarColumn(IsPrimaryKey = true)]
    public int FriendId { get; set; }
}
