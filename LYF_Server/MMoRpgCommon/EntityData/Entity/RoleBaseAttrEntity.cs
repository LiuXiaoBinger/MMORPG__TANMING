/// <summary>
/// 角色基础战斗属性数据。
/// </summary>
public class RoleBaseAttrEntity
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    public int RoleID { get; set; }

    /// <summary>
    /// 角色修为。
    /// </summary>
    public int XiuWei { get; set; }

    /// <summary>
    /// 最大生命值。
    /// </summary>
    public int MaxHP { get; set; }

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public int CurrHP { get; set; }

    /// <summary>
    /// 最大法力值。
    /// </summary>
    public int MaxMP { get; set; }

    /// <summary>
    /// 当前法力值。
    /// </summary>
    public int CurrMP { get; set; }

    /// <summary>
    /// 外功攻击最小值。
    /// </summary>
    public int AtkExternalMin { get; set; }

    /// <summary>
    /// 外功攻击最大值。
    /// </summary>
    public int AtkExternalMax { get; set; }

    /// <summary>
    /// 内功攻击最小值。
    /// </summary>
    public int AtkInternalMin { get; set; }

    /// <summary>
    /// 内功攻击最大值。
    /// </summary>
    public int AtkInternalMax { get; set; }

    /// <summary>
    /// 外功防御值。
    /// </summary>
    public int DefExternal { get; set; }

    /// <summary>
    /// 外功防御变动值。
    /// </summary>
    public int DefExternalDelta { get; set; }

    /// <summary>
    /// 内功防御值。
    /// </summary>
    public int DefInternal { get; set; }

    /// <summary>
    /// 内功防御变动值。
    /// </summary>
    public int DefInternalDelta { get; set; }

    /// <summary>
    /// 命中属性值。
    /// </summary>
    public int HitPoint { get; set; }

    /// <summary>
    /// 格挡属性值。
    /// </summary>
    public int BlockPoint { get; set; }

    /// <summary>
    /// 振击属性值。
    /// </summary>
    public int ZhenJi { get; set; }

    /// <summary>
    /// 无咎属性值。
    /// </summary>
    public int WuGu { get; set; }

    /// <summary>
    /// 暴击属性值。
    /// </summary>
    public int CritPoint { get; set; }

    /// <summary>
    /// 暴击抵抗属性值。
    /// </summary>
    public int CritResistPoint { get; set; }

    /// <summary>
    /// 暴击伤害百分比。
    /// </summary>
    public float CritDamage { get; set; }

    /// <summary>
    /// 暴击伤害减免百分比。
    /// </summary>
    public float CritDamageReduce { get; set; }

    /// <summary>
    /// 治疗强度。
    /// </summary>
    public int HealStrength { get; set; }

    /// <summary>
    /// 治疗增益百分比。
    /// </summary>
    public float HealBonus { get; set; }

    /// <summary>
    /// 诛心属性，PVP 增伤。
    /// </summary>
    public int ZhuXin { get; set; }

    /// <summary>
    /// 坚韧属性，PVP 减伤。
    /// </summary>
    public int JianRen { get; set; }
}
