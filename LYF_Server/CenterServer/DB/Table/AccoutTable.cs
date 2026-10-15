


using System;
using SqlSugar;

/// <summary>
/// 账号表，保存注册账号的用户名、密码、邮箱验证码校验所需邮箱及最近登录服务器。
/// </summary>

[SugarTable("accout",TableDescription = "用户表")]
public class AccoutTable
{
    /// <summary>账号主键，自增。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增  IsPrimaryKey是否是主键 IsIdentity是否自增长
    public int Id { get; set; }
    /// <summary>账号状态：1 表示正常可登录，其他值表示禁用。</summary>
    [SugarColumn(DefaultValue = "1",IsOnlyIgnoreInsert =  true)] //IsOnlyIgnoreInsert插入时可以忽略这个字段
    public byte State { get; set; }

    /// <summary>登录用户名，唯一，长度 30。</summary>
    [SugarColumn(Length = 30)]
    public string UserName { get; set; }

    /// <summary>注册邮箱，用于接收与校验注册验证码。</summary>
    [SugarColumn(Length = 300)]
    public string Email { get; set; }

    /// <summary>登录密码（当前明文存储，长度 30）。</summary>
    [SugarColumn(Length = 30)]
    public string Passwrod { get; set; }

    /// <summary>用户最后一次登录并选中的游戏服务器 ID。</summary>
    public int LastLoginServerId{ get; set; }

    /// <summary>账号记录创建时间。</summary>
    public DateTime CreateDate { get; set; }
    /// <summary>账号记录最近更新时间。</summary>
    public DateTime UpdateDate { get; set; }
}