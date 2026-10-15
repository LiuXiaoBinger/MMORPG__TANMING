



using System;
using SqlSugar;

/// <summary>
/// 游戏服务器列表表，记录每个区服的名称、运行状态、新服标记与接入地址端口。
/// </summary>

[SugarTable("game_server",TableDescription = "服务器列表")]
public class GameServerTable
{
    /// <summary>服务器主键，自增。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增  IsPrimaryKey是否是主键 IsIdentity是否自增长
    public int Id { get; set; }
    /// <summary>服务器启用状态：1 表示启用，其他值停用。</summary>
    [SugarColumn(DefaultValue = "1",IsOnlyIgnoreInsert =  true)] //IsOnlyIgnoreInsert插入时可以忽略这个字段
    public byte State { get; set; }

    /// <summary>服务器显示名称，如“1区 五域大陆”。</summary>
    [SugarColumn(Length = 30)]
    public string ServerName { get; set; }

    /// <summary>运行状态：1 爆满、2 拥挤、3 正常。</summary>
    [SugarColumn(Length = 15)]
    public byte RunState { get; set; }

    /// <summary>是否新服：1 是新服，0 否。</summary>
    public int IsNew { get; set; }
    /// <summary>客户端接入地址（IP 或主机名）。</summary>
    [SugarColumn(Length = 30)]
    public string IpHost { get; set; }
    /// <summary>客户端接入端口。</summary>
    public int Port { get; set; }

    /// <summary>服务器记录创建时间。</summary>
    public DateTime CreateDate { get; set; }
    /// <summary>服务器记录最近更新时间。</summary>
    public DateTime UpdateDate { get; set; }
}