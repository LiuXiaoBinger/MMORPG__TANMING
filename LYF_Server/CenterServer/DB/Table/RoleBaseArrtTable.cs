using SqlSugar;
using System;

[SugarTable("RoleBaseArrt", TableDescription = "角色表")]
public class RoleBaseArrtTable
{
    /// <summary>
    /// 角色ID，同时作为角色属性表主键。
    /// 一个角色只能有一条基础属性记录。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int Id { get; set; }
    public int roleID { get; set; }
     /// <summary>
    /// 角色修为   
    /// </summary>
    public int XiuWei  { get; set; } =100;

    //最大生命值
    public int MaxHP { get; set; } = 1000;


    //当前生命值
    public int CurrHP { get; set; }= 1000;

    //最大法力值
    public int MaxMP { get; set; }= 1000;

    //当前的法力值
    public int CurrMP { get; set; } = 1000;
    #region 攻击属性
    /// <summary>
    /// 外功攻击最小值
    /// </summary>
    public int AtkExternalMin { get; set; }= 2510;
    /// <summary>
    /// 外功攻击最大值
    /// </summary>
    public int AtkExternalMax { get; set; }= 3937;
    /// <summary>
    /// 内功攻击最小值
    /// </summary>
    public int AtkInternalMin { get; set; }= 400;
    /// <summary>
    /// 内功攻击最大值
    /// </summary>
    public int AtkInternalMax { get; set; }= 773;
    #endregion

    #region 防御属性
    /// <summary>
    /// 外功防御
    /// </summary>
    public int DefExternal { get; set; }= 1324;
    /// <summary>
    /// 外功防御变动值(面板括号内) -37
    /// </summary>
    public int DefExternalDelta { get; set; }= -37;
    /// <summary>
    /// 内功防御
    /// </summary>
    public int DefInternal { get; set; }= 1132;
    /// <summary>
    /// 内功防御变动值(面板括号内) -32
    /// </summary>
    public int DefInternalDelta { get; set; }= -32;
    #endregion

    #region 命中格挡
    /// <summary>
    /// 命中属性值
    /// </summary>
    public int HitPoint { get; set; }= 2645;
    /// <summary>
    /// 格挡属性值
    /// </summary>
    public int BlockPoint { get; set; }= 2201;
    #endregion

    #region 振击、无咎
    /// <summary>
    /// 振击属性
    /// </summary>
    public int ZhenJi { get; set; }= 811;
    /// <summary>
    /// 无咎属性
    /// </summary>
    public int WuGu { get; set; }= 748;
    #endregion

    #region 暴击相关
    /// <summary>
    /// 暴击属性值
    /// </summary>
    public int CritPoint { get; set; }= 3370;
    /// <summary>
    /// 暴抗属性值
    /// </summary>
    public int CritResistPoint { get; set; }= 2598;
    /// <summary>
    /// 暴击伤害(百分比原始数值 151代表151%)
    /// </summary>
    public float CritDamage { get; set; }= 151f;
    /// <summary>
    /// 暴击伤害减免(百分比原始数值 0代表0%)
    /// </summary>
    public float CritDamageReduce { get; set; }= 0f;
    #endregion

    #region 治疗属性
    /// <summary>
    /// 治疗强度
    /// </summary>
    public int HealStrength { get; set; }= 103;
    /// <summary>
    /// 治疗增益(百分比原始数值 1代表1%)
    /// </summary>
    public float HealBonus { get; set; }= 1f;
    #endregion

    #region PVP对抗属性
    /// <summary>
    /// 诛心(PVP增伤)
    /// </summary>
    public int ZhuXin { get; set; }= 324;
    /// <summary>
    /// 坚韧(PVP减伤)
    /// </summary>
    public int JianRen { get; set; }= 157;
    #endregion
        
}
