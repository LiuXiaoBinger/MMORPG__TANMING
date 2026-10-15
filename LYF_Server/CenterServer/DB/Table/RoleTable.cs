using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// 角色主表，一个账号在一个服务器下可拥有的角色基础信息：归属账号、昵称、职业、等级、
/// 经验、技能点、出生位置、所在服务器与地图，以及物品保存版本。
/// </summary>
[SugarTable("role", TableDescription = "角色表")]

internal class RoleTable
{

    /// <summary>角色主键，自增。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增 IsPrimaryKey:表示是否是主键，IsIdentity:表示是否自增长
    public int Id { get; set; }

    /// <summary>角色启用状态：1 正常，其他值停用（遗留字段，当前业务未读取）。</summary>
    [SugarColumn(DefaultValue = "1", IsOnlyIgnoreInsert = true)]
    public byte State { get; set; }

    /// <summary>所属账号 ID，对应 AccoutTable.Id。</summary>
    public int AccountID { get; set; }

    /// <summary>角色昵称，同服务器下唯一。</summary>
    [SugarColumn(Length = 30)]
    public string Nickname { get; set; }

    /// <summary>职业 ID，对应 Luban 职业配置。</summary>
    public int JobID { get; set; }

    /// <summary>角色等级。</summary>
    public int Level { get; set; }

    /// <summary>当前经验值。</summary>
    public int Exp { get; set; }

    /// <summary>可分配的技能升级点数。</summary>
    public int SkillUpPoint { get; set; }

    /// <summary>遗留角色 itemUid 字段，当前业务代码未读取。</summary>
    public long itemUid { get; set; }

    // CenterServer 已提交的角色物品快照版本，用于保存请求幂等校验。
    public long ItemSaveVersion { get; set; }


    /// <summary>角色当前所在位置坐标字符串。</summary>
    [SugarColumn(Length = 50)]
    public string Pos { get; set; }

    /// <summary>角色所属游戏服务器 ID。</summary>
    public int ServerId { get; set; }

    /// <summary>角色相机偏移字符串。</summary>
    [SugarColumn(Length = 50)]
    public string CameraOffset { get; set; }

    /// <summary>角色当前所在地图 ID。</summary>
    public int MapId { get; set; }


    /// <summary>角色记录创建时间。</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>角色记录最近更新时间。</summary>
    public DateTime UpdateDate { get; set; }


}
