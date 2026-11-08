/// <summary>基因修正的属性编号；1 至 23 保留现有装备词条和基因表的编号。</summary>
public enum GeneAttributeType
{
    /// <summary>非属性效果不指定属性。</summary>
    None = 0,
    /// <summary>生命上限，单位生命点。</summary>
    MaxHp = 1,
    /// <summary>法力上限，单位法力点。</summary>
    MaxMp = 2,
    /// <summary>外功攻击随机区间下限，单位攻击点。</summary>
    AtkExternalMin = 3,
    /// <summary>外功攻击随机区间上限，单位攻击点。</summary>
    AtkExternalMax = 4,
    /// <summary>内功攻击随机区间下限，单位攻击点。</summary>
    AtkInternalMin = 5,
    /// <summary>内功攻击随机区间上限，单位攻击点。</summary>
    AtkInternalMax = 6,
    /// <summary>外功防御面板点数。</summary>
    DefExternal = 7,
    /// <summary>外功防御变动值，可为负数。</summary>
    DefExternalDelta = 8,
    /// <summary>内功防御面板点数。</summary>
    DefInternal = 9,
    /// <summary>内功防御变动值，可为负数。</summary>
    DefInternalDelta = 10,
    /// <summary>命中面板点数，不是命中概率。</summary>
    HitPoint = 11,
    /// <summary>格挡面板点数，不是格挡概率。</summary>
    BlockPoint = 12,
    /// <summary>振击面板点数。</summary>
    ZhenJi = 13,
    /// <summary>无咎面板点数。</summary>
    WuGu = 14,
    /// <summary>暴击面板点数，不是暴击概率。</summary>
    CritPoint = 15,
    /// <summary>暴击抵抗面板点数。</summary>
    CritResistPoint = 16,
    /// <summary>暴击伤害百分数，151 表示 151%；固定值修正增加百分点。</summary>
    CritDamage = 17,
    /// <summary>暴击伤害减免百分数；固定值修正增加百分点。</summary>
    CritDamageReduce = 18,
    /// <summary>治疗强度面板点数。</summary>
    HealStrength = 19,
    /// <summary>治疗增益百分数；固定值修正增加百分点。</summary>
    HealBonus = 20,
    /// <summary>诛心面板点数，供 PvP 伤害公式使用。</summary>
    ZhuXin = 21,
    /// <summary>坚韧面板点数，供 PvP 减伤公式使用。</summary>
    JianRen = 22,
    /// <summary>原表的基础气属性，保留编号；不等同于法力或轻功气力。</summary>
    Qi = 23,
    /// <summary>移动速度，单位与调用方传入的基础速度一致。</summary>
    MoveSpeed = 24,
    /// <summary>承受伤害倍率的百分数，未修正时为 100。</summary>
    DamageTaken = 25,
    /// <summary>接受治疗倍率的百分数，未修正时为 100。</summary>
    HealingReceived = 26
}
