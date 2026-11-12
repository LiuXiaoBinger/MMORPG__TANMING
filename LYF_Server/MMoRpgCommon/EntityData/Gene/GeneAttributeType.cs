/// <summary>基因修正的属性编号；0 至 26 保留现有装备词条和基因表的编号，101 至 144 为高级属性。</summary>
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
    HealingReceived = 26,

    // 高级属性：101 至 120 为五行属性攻击、穿透、抵抗与穿透上限，均为面板点数值。
    /// <summary>水属性攻击，单位面板点数。</summary>
    WaterAttack = 101,
    /// <summary>毒属性攻击，单位面板点数。</summary>
    PoisonAttack = 102,
    /// <summary>火属性攻击，单位面板点数。</summary>
    FireAttack = 103,
    /// <summary>风属性攻击，单位面板点数。</summary>
    WindAttack = 104,
    /// <summary>元属性攻击，单位面板点数。</summary>
    YuanAttack = 105,
    /// <summary>水属性穿透，单位面板点数。</summary>
    WaterPenetration = 106,
    /// <summary>毒属性穿透，单位面板点数。</summary>
    PoisonPenetration = 107,
    /// <summary>火属性穿透，单位面板点数。</summary>
    FirePenetration = 108,
    /// <summary>风属性穿透，单位面板点数。</summary>
    WindPenetration = 109,
    /// <summary>元属性穿透，单位面板点数。</summary>
    YuanPenetration = 110,
    /// <summary>水属性抵抗，单位面板点数。</summary>
    WaterResistance = 111,
    /// <summary>毒属性抵抗，单位面板点数。</summary>
    PoisonResistance = 112,
    /// <summary>火属性抵抗，单位面板点数。</summary>
    FireResistance = 113,
    /// <summary>风属性抵抗，单位面板点数。</summary>
    WindResistance = 114,
    /// <summary>元属性抵抗，单位面板点数。</summary>
    YuanResistance = 115,
    /// <summary>水穿透上限，单位面板点数。</summary>
    WaterPenetrationCap = 116,
    /// <summary>毒穿透上限，单位面板点数。</summary>
    PoisonPenetrationCap = 117,
    /// <summary>火穿透上限，单位面板点数。</summary>
    FirePenetrationCap = 118,
    /// <summary>风穿透上限，单位面板点数。</summary>
    WindPenetrationCap = 119,
    /// <summary>元穿透上限，单位面板点数。</summary>
    YuanPenetrationCap = 120,

    // 扬刃一脉十二项，均为百分数，10 表示 10%。
    /// <summary>扬刃·辟奕，单位百分点。</summary>
    YangrenPiyi = 121,
    /// <summary>扬刃·问禅，单位百分点。</summary>
    YangrenWenchan = 122,
    /// <summary>扬刃·无魇，单位百分点。</summary>
    YangrenWuyan = 123,
    /// <summary>扬刃·覆极，单位百分点。</summary>
    YangrenFuji = 124,
    /// <summary>扬刃·驱鸩，单位百分点。</summary>
    YangrenQuzhen = 125,
    /// <summary>扬刃·化丹，单位百分点。</summary>
    YangrenHuadan = 126,
    /// <summary>扬刃·灵卜，单位百分点。</summary>
    YangrenLingbu = 127,
    /// <summary>扬刃·涤尘，单位百分点。</summary>
    YangrenDichen = 128,
    /// <summary>扬刃·吟风，单位百分点。</summary>
    YangrenYinfeng = 129,
    /// <summary>扬刃·雁翎，单位百分点。</summary>
    YangrenYanling = 130,
    /// <summary>扬刃·衔命，单位百分点。</summary>
    YangrenXianming = 131,
    /// <summary>扬刃·烹香，单位百分点。</summary>
    YangrenPengxiang = 132,

    // 拒芒一脉十二项，均为百分数，10 表示 10%。
    /// <summary>拒芒·辟奕，单位百分点。</summary>
    JumangPiyi = 133,
    /// <summary>拒芒·问禅，单位百分点。</summary>
    JumangWenchan = 134,
    /// <summary>拒芒·无魇，单位百分点。</summary>
    JumangWuyan = 135,
    /// <summary>拒芒·覆极，单位百分点。</summary>
    JumangFuji = 136,
    /// <summary>拒芒·驱鸩，单位百分点。</summary>
    JumangQuzhen = 137,
    /// <summary>拒芒·化丹，单位百分点。</summary>
    JumangHuadan = 138,
    /// <summary>拒芒·灵卜，单位百分点。</summary>
    JumangLingbu = 139,
    /// <summary>拒芒·涤尘，单位百分点。</summary>
    JumangDichen = 140,
    /// <summary>拒芒·吟风，单位百分点。</summary>
    JumangYinfeng = 141,
    /// <summary>拒芒·雁翎，单位百分点。</summary>
    JumangYanling = 142,
    /// <summary>拒芒·衔命，单位百分点。</summary>
    JumangXianming = 143,
    /// <summary>拒芒·烹香，单位百分点。</summary>
    JumangPengxiang = 144
}
