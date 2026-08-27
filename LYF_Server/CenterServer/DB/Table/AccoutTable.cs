


using System;
using SqlSugar;

/// <summary>
/// 用户表
/// </summary>

[SugarTable("accout",TableDescription = "用户表")]
public class AccoutTable
{
    //id
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增  IsPrimaryKey是否是主键 IsIdentity是否自增长
    public int Id { get; set; }
   //状态
    [SugarColumn(DefaultValue = "1",IsOnlyIgnoreInsert =  true)] //IsOnlyIgnoreInsert插入时可以忽略这个字段
    public byte State { get; set; }
    
    //用户名
    [SugarColumn(Length = 30)]
    public string UserName { get; set; }
    
    //手机号
    [SugarColumn(Length = 300)]
    public string Email { get; set; }
    
    //密码
    [SugarColumn(Length = 30)]
    public string Passwrod { get; set; }
    
    //用户最后登录服务器的id
    public int LastLoginServerId{ get; set; }
    
    //创建时间和更新时间
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}