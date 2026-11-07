using System;
using System.Xml.Linq;

/// <summary>魔神塔邪骸兽人的一级物理普攻，按 RO 的怪物属性、玩家 PvE 防御与 CommonSkillCritCalc 结算。</summary>
public static class OrcBasicAttackDamage
{
    /// <summary>游戏服在单一逻辑线程推进怪物攻击，连续命中共用该随机源。</summary>
    private static readonly Random combatRandom = new Random();
    /// <summary>原 Lua 把素质物攻写回整型属性时的结果，单位攻击点。</summary>
    private static readonly int statusAttack;
    /// <summary>怪物装备物攻，来自 EntityTable.WEAPON_ATK。</summary>
    private static readonly int weaponAttack;
    /// <summary>普通攻击忽略的装备物防点数，不抵消玩家的素质防御。</summary>
    private static readonly int ignoreDefense;
    /// <summary>由原表、灵巧以及 RO 命中基数得到的最终命中点。</summary>
    private static readonly int hitPoints;
    /// <summary>由原表和幸运得到的最终暴击点，每点对应一个百分点。</summary>
    private static readonly int criticalPoints;
    /// <summary>本级技能的 PvE 伤害比例，原表以万分数存储。</summary>
    private static readonly double skillRate;
    /// <summary>本级技能每个结果点的 PvE 固定附加伤害。</summary>
    private static readonly int skillFlatDamage;
    /// <summary>RO 对普通命中设置的最低总伤害，等于本结果的攻击段数。</summary>
    private static readonly int hitCount;

    /// <summary>从嵌入的原表字段一次性还原这只近战怪物的战斗属性。</summary>
    static OrcBasicAttackDamage()
    {
        // 行为树和结算共享 5515 的实体表行。
        XElement monster = MoshentaOrcDefinition.Entity;
        // 普攻 1000118401 的一级效果表行，包含结果段数、比例与固定附伤。
        XElement skill = MoshentaOrcDefinition.Effect;
        // 兽人灵巧参与素质物攻及命中计算。
        int dexterity = (int)monster.Attribute("DEX");
        // 幸运每三点提供一点暴击，也是素质物攻的一部分。
        int luck = (int)monster.Attribute("LUK");
        // 力量是邪骸兽人近战素质物攻的主属性。
        int strength = (int)monster.Attribute("STR");
        // 怪物基础等级按四分之一计入素质攻击。
        int level = (int)monster.Attribute("UnitLevel");
        statusAttack = (int)(strength + dexterity / 5.0 + luck / 5.0 + level / 4.0 + (int)monster.Attribute("BaseATK"));
        weaponAttack = (int)monster.Attribute("WEAPON_ATK");
        ignoreDefense = (int)monster.Attribute("DEF_IGNORE");
        hitPoints = (int)monster.Attribute("HIT") + dexterity + 80;
        criticalPoints = (int)((int)monster.Attribute("CRI") + luck / 3.0);
        skillRate = (int)skill.Attribute("PVEDamageRadio") / 10000.0;
        skillFlatDamage = (int)skill.Attribute("PVEStaticDam");
        hitCount = (int)skill.Attribute("AttackCount");
    }

    /// <summary>在服务器普攻结果点取玩家此刻的防御属性，计算本刀的伤害点数。</summary>
    /// <param name="target">仍在本副本中的受击角色。</param>
    /// <returns>闪避为零，命中后由调用方按剩余生命裁切实际扣血量。</returns>
    public static int Calculate(OnlineRole target)
    {
        // 兼容原接口，暴击标记仍由同一次随机判定生成。
        bool critical;
        return Calculate(target, out critical);
    }

    /// <summary>同时返回本刀伤害与实际暴击结果，不在事件层重新抽样。</summary>
    /// <param name="target">读取最终装备属性的受击在线角色。</param>
    /// <param name="critical">本刀权威暴击结果。</param>
    /// <returns>未按剩余生命裁切的整点伤害。</returns>
    public static int Calculate(OnlineRole target, out bool critical)
    {
        return Calculate(target.GetComponent<RoleEquipComponent>().CalculateAttribute(), combatRandom, out critical);
    }

    /// <summary>按 RO 的判定次序和 PvE 玩家分支，用指定随机源一次结算无属性普通攻击。</summary>
    /// <param name="target">本项目玩家面板，外防为最终面板外防，括号内变动值已包含在面板值中。</param>
    /// <param name="rolls">本次暴击、命中及伤害浮动使用的随机源。</param>
    /// <returns>结果点应造成的整点伤害，不直接修改玩家属性。</returns>
    public static int Calculate(RoleBaseInfo target, Random rolls)
    {
        // 兼容已有只需要伤害的独立测试接口。
        bool critical;
        return Calculate(target, rolls, out critical);
    }

    /// <summary>沿原判定次序计算伤害并保留暴击结果供事件使用。</summary>
    /// <param name="target">已经合成装备与临时效果的玩家面板。</param>
    /// <param name="rolls">本刀唯一随机源。</param>
    /// <param name="critical">实际暴击成功为 true，闪避必为 false。</param>
    /// <returns>未按剩余生命裁切的整点伤害。</returns>
    public static int Calculate(RoleBaseInfo target, Random rolls, out bool critical)
    {
        // 当前职业没有 RO 的格挡技能状态和卡片概率，BlockPoint 是面板点数，不能当万分比使用。
        // RO 普攻先判暴击，成功的暴击绕过普通命中判定。暴抗来自玩家实际面板。
        double criticalChance = Math.Min(1.0, Math.Max(0.0, (criticalPoints - target.CritResistPoint) / 100.0));
        critical = criticalChance > 0.0 && rolls.NextDouble() <= criticalChance;
        if (!critical)
        {
            // RO 玩家基础闪避等于等级。LYF 没有独立 AGI、FLEE 加成，本副本当前仅这一只攻击者。
            double hitChance = Math.Max(0.05, Math.Min(1.0, (hitPoints - target.Level) / 100.0));
            if (rolls.NextDouble() > hitChance) return 0;
        }

        // damage_def.cpp 的玩家 PvE 装备防御。LYF 用外防作物防，无独立盾防、精炼减伤词条。
        double equipmentDefense = Math.Max(0.0, (double)target.DefExternal - ignoreDefense);
        double defenseRate = Math.Max((equipmentDefense + 4000.0) / (equipmentDefense * 10.0 + 4000.0), 0.1) * 0.95;
        // RO 素质防御整点为 (等级 + VIT)/2 + AGI/5，当前玩家无额外 VIT、AGI 输入。
        int statusDefense = Math.Max(0, target.Level) / 2;
        // 5515 是无独立武器类型的怪物，技能攻击属性为无，玩家无属性抗性、种族和体型修正均为 1。
        double physical = (2.0 * statusAttack + weaponAttack) * skillRate;
        // 原 C++ 先生成 97% 至 103% 的浮动，再由 Lua 对素质减防后的伤害和固定附加值应用。
        double variation = (float)(0.97 + rolls.NextDouble() * 0.06);
        double damage = Math.Max(hitCount, (Math.Max(0.0, physical * defenseRate - statusDefense) + skillFlatDamage) * variation);
        if (critical)
        {
            // RO 普攻暴击基础 150%，LYF 暴伤减免面板的 1 表示 1%。
            damage *= 1.5 * (1.0 - target.CritDamageReduce / 100.0);
        }
        return Math.Max(0, (int)damage);
    }
}
