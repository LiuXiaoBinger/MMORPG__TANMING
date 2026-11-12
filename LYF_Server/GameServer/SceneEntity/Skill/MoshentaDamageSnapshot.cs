using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using MMoRpgCommon.SceneEntity;

/// <summary>原RO伤害边界的具名属性快照；怪物起手冻结，玩家在实际结果点重新读取。</summary>
internal sealed class MoshentaDamageSnapshot
{
    /// <summary>原AttrType字段名到真实数值；本项目没有的RO装备属性保持原缺省0。</summary>
    internal readonly Dictionary<string, double> Attributes = new Dictionary<string, double>();
    /// <summary>原Buff家族及等级，供原Lua判断现有状态。</summary>
    internal readonly Dictionary<int, int> Buffs = new Dictionary<int, int>();
    /// <summary>当前水平位置，单位米。</summary>
    internal Vector3 Position;
    /// <summary>已接收的水平朝向，单位度。</summary>
    internal float Facing;
    /// <summary>是否本项目真实玩家，决定RO玩家PvE防御分支。</summary>
    internal bool IsPlayer;
    /// <summary>当前围攻数量，供RO普攻/物理技能闪避折减。</summary>
    internal int AttackerCount;
    /// <summary>现有基因容器给出的眩晕状态。</summary>
    internal bool Stunned;

    /// <summary>捕获真实怪物实体与原表战斗属性。</summary>
    /// <param name="actor">同实例中真实怪物宿主。</param>
    /// <param name="forSummon">原施加者快照召唤时为真，只排除父怪难度Gene，仍继承原Buff属性。</param>
    internal static MoshentaDamageSnapshot Capture(MoshentaActor actor, bool forSummon = false)
    {
        // EntityTable基础属性不是已计算的最终攻击。
        XElement row = actor.Owner.Battle.Catalog.Entity(actor.EntityId);
        // 此对象从起手保留到该技能全部结果结束。
        MoshentaDamageSnapshot result = new MoshentaDamageSnapshot();
        // 原六维与等级，按attribute_calculator.lua的怪物分支还原。
        double strength = MoshentaSkillCatalog.I(row, "STR");
        // 灵巧影响命中及素质攻击。
        double dexterity = MoshentaSkillCatalog.I(row, "DEX");
        // 幸运影响暴击与素质魔攻。
        double luck = MoshentaSkillCatalog.I(row, "LUK");
        // 智力影响素质魔攻、魔防。
        double intelligence = MoshentaSkillCatalog.I(row, "INT");
        // 体质影响素质防御。
        double vitality = MoshentaSkillCatalog.I(row, "VIT");
        // 敏捷影响闪避与素质物防。
        double agility = MoshentaSkillCatalog.I(row, "AGI");
        // 怪物表中基础等级。
        int level = MoshentaSkillCatalog.I(row, "UnitLevel");
        result.Set("ATTR_SPECIAL_BASE_LV", level);
        result.Set("ATTR_BASIC_DEX", dexterity);
        result.Set("ATTR_SPECIAL_VIT", vitality);
        result.Set("ATTR_SPECIAL_INT", intelligence);
        result.Set("ATTR_SPECIAL_LUK", luck);
        result.Set("ATTR_BASIC_BASE_ATK_FINAL", (int)(strength + dexterity / 5 + luck / 5 + level / 4.0 + MoshentaSkillCatalog.I(row, "BaseATK")));
        result.Set("ATTR_BASIC_BASE_MATK_FINAL", (int)(intelligence * 1.5 + dexterity / 5 + luck / 3 + level / 4.0 + MoshentaSkillCatalog.I(row, "BaseMATK")));
        result.Set("ATTR_BASIC_BASE_DEF_FINAL", (int)((level + vitality) / 2 + agility / 5 + MoshentaSkillCatalog.I(row, "BaseDEF")));
        result.Set("ATTR_BASIC_BASE_MDEF_FINAL", (int)(level / 4.0 + intelligence + vitality / 5 + dexterity / 5 + MoshentaSkillCatalog.I(row, "BaseMDEF")));
        result.Set("ATTR_BASIC_WEAPON_ATK_FINAL", MoshentaSkillCatalog.I(row, "WEAPON_ATK"));
        result.Set("ATTR_BASIC_WEAPON_MATK_FINAL", MoshentaSkillCatalog.I(row, "WEAPON_MATK"));
        result.Set("ATTR_BASIC_IGNORE_DEF", MoshentaSkillCatalog.I(row, "DEF_IGNORE"));
        result.Set("ATTR_BASIC_IGNORE_MDEF", MoshentaSkillCatalog.I(row, "MDEF_IGNORE"));
        result.Set("ATTR_BASIC_HIT_FINAL", MoshentaSkillCatalog.I(row, "HIT") + dexterity + 80);
        result.Set("ATTR_BASIC_FLEE_FINAL", Math.Max(0, MoshentaSkillCatalog.I(row, "FLEE") + agility));
        result.Set("ATTR_BASIC_CRI_FINAL", (int)(MoshentaSkillCatalog.I(row, "CRI") + luck / 3));
        result.Set("ATTR_BASIC_CRI_RES_FINAL", (int)(MoshentaSkillCatalog.I(row, "CRI_RES") + luck / 5));
        result.Set("ATTR_SPECIAL_SIZE", MoshentaSkillCatalog.I(row, "UnitSize"));
        result.Set("ATTR_SPECIAL_RACE", MoshentaSkillCatalog.I(row, "UnitRace"));
        result.Set("ATTR_SPECIAL_DEF_ATTR", MoshentaSkillCatalog.I(row, "UnitAttrType"));
        result.Set("ATTR_BASIC_MAX_HP_FINAL", actor.MaxHp);
        result.Set("ATTR_BASIC_DEF", MoshentaSkillCatalog.I(row, "DEF"));
        result.Set("ATTR_BASIC_MDEF", MoshentaSkillCatalog.I(row, "MDEF"));
        if (actor.SummonSnapshot != null)
        {
            // 原召唤属性先作为基底；其中保留施加者Buff，已排除施加者的难度Gene。
            List<string> keys = new List<string>(actor.SummonSnapshot.Attributes.Keys);
            for (int index = 0; index < keys.Count; index++) result.Set(keys[index], actor.SummonSnapshot.Attributes[keys[index]]);
        }
        // Gene按当前召唤物或固定怪物自己的容器应用，不能被继承快照覆盖。
        GeneRuntime genes = actor.Monster.Components.Get<GeneComponent>(EntityComponentType.Gene).Runtime;
        ApplyAttackRangeAsRoScalar(result, actor, genes, GeneAttributeType.AtkExternalMin, GeneAttributeType.AtkExternalMax,
            "ATTR_BASIC_BASE_ATK_FINAL", "ATTR_BASIC_WEAPON_ATK_FINAL", forSummon);
        ApplyAttackRangeAsRoScalar(result, actor, genes, GeneAttributeType.AtkInternalMin, GeneAttributeType.AtkInternalMax,
            "ATTR_BASIC_BASE_MATK_FINAL", "ATTR_BASIC_WEAPON_MATK_FINAL", forSummon);
        result.Set("ATTR_BASIC_DEF", ReadGeneAttribute(actor, genes, GeneAttributeType.DefExternal, result.Get("ATTR_BASIC_DEF"), forSummon));
        result.Set("ATTR_BASIC_MDEF", ReadGeneAttribute(actor, genes, GeneAttributeType.DefInternal, result.Get("ATTR_BASIC_MDEF"), forSummon));
        result.Set("ATTR_BASIC_HIT_FINAL", ReadGeneAttribute(actor, genes, GeneAttributeType.HitPoint, result.Get("ATTR_BASIC_HIT_FINAL"), forSummon));
        result.Set("ATTR_BASIC_CRI_FINAL", ReadGeneAttribute(actor, genes, GeneAttributeType.CritPoint, result.Get("ATTR_BASIC_CRI_FINAL"), forSummon));
        result.Set("ATTR_BASIC_CRI_RES_FINAL", ReadGeneAttribute(actor, genes, GeneAttributeType.CritResistPoint, result.Get("ATTR_BASIC_CRI_RES_FINAL"), forSummon));
        // 生命快照始终来自当前真实实体，不用父怪生命上限替换召唤模板。
        result.Set("ATTR_BASIC_MAX_HP_FINAL", actor.MaxHp);
        result.Stunned = (genes.GetStates() & GeneState.Stun) != 0;
        result.Position = actor.Position;
        result.Facing = actor.Facing;
        actor.Skills.Buffs.Snapshot(result);
        return result;
    }

    /// <summary>将本项目攻击区间映射到RO原标量：取上下限修正的平均，不额外增加一次随机掷点。</summary>
    /// <param name="result">沿用原Lua字段的本次快照。</param>
    /// <param name="actor">当前实体，提供难度Gene来源。</param>
    /// <param name="genes">当前真实Gene容器。</param>
    /// <param name="minimum">外功或内功下限属性。</param>
    /// <param name="maximum">对应的上限属性。</param>
    /// <param name="baseKey">原RO素质攻击字段。</param>
    /// <param name="weaponKey">原RO武器攻击字段。</param>
    /// <param name="forSummon">是否只读取供子怪继承的非难度属性。</param>
    private static void ApplyAttackRangeAsRoScalar(MoshentaDamageSnapshot result, MoshentaActor actor, GeneRuntime genes,
        GeneAttributeType minimum, GeneAttributeType maximum, string baseKey, string weaponKey, bool forSummon)
    {
        // 以0为基底读出固定项，平均上下限代表同一区间的中心攻击标量。
        double flat = (ReadGeneAttribute(actor, genes, minimum, 0, forSummon) + ReadGeneAttribute(actor, genes, maximum, 0, forSummon)) / 2;
        // 以1为基底减去固定项，得到百分比倍率；避免将固定加值应用到两个组成各一次。
        double scale = (ReadGeneAttribute(actor, genes, minimum, 1, forSummon) + ReadGeneAttribute(actor, genes, maximum, 1, forSummon)) / 2 - flat;
        result.Set(baseKey, result.Get(baseKey) * scale);
        result.Set(weaponKey, result.Get(weaponKey) * scale + flat);
    }

    /// <summary>正常施法读取完整属性；召唤继承仅扣父怪难度负槽，不改变Buff、层数或寿命。</summary>
    /// <param name="actor">拥有难度负槽的当前怪物。</param>
    /// <param name="genes">当前真实运行容器。</param>
    /// <param name="attribute">已有Gene属性类型，不按基因ID特判。</param>
    /// <param name="baseValue">原RO属性或继承的非难度基础值。</param>
    /// <param name="forSummon">是否排除难度槽以供子怪再次应用自身配置。</param>
    private static double ReadGeneAttribute(MoshentaActor actor, GeneRuntime genes, GeneAttributeType attribute, double baseValue, bool forSummon)
    {
        // 原完整属性是所有已生效来源之和，后续只扣明确属于难度的槽。
        double original = genes.CalculateAttribute(attribute, baseValue);
        if (!forSummon) return original;
        // 多个同属性Gene各扣一次，Buff来源槽保持原贡献。
        double result = original;
        for (int index = 0; index < actor.DifficultyGeneCount; index++)
            result -= original - genes.CalculateAttributeExcludingSource(attribute, baseValue, actor.UnitId, int.MinValue + index);
        return result;
    }

    /// <summary>读取玩家现有最终装备面板，不为RO补造装备或六维系统。</summary>
    /// <param name="role">实际受击在线角色。</param>
    /// <param name="attackerCount">当前同实例有效攻击者数量。</param>
    internal static MoshentaDamageSnapshot Capture(OnlineRole role, int attackerCount)
    {
        // CalculateAttribute已经含现有Gene修正，后续不能再次应用凝视减防。
        RoleBaseInfo data = role.GetComponent<RoleEquipComponent>().CalculateAttribute();
        // 玩家仅提供现有真实面板，缺失RO六维、闪避附加和精炼词条均不虚构。
        MoshentaDamageSnapshot result = new MoshentaDamageSnapshot();
        result.IsPlayer = true;
        result.AttackerCount = attackerCount;
        result.Position = role.mainRoleInfo.BaseInfo.Pos.ToVector3();
        result.Facing = role.GetComponent<ROMoveComponent>().Facing;
        result.Set("ATTR_SPECIAL_BASE_LV", data.Level);
        result.Set("ATTR_BASIC_MAX_HP_FINAL", data.MaxHp);
        result.Set("ATTR_BASIC_DEF", data.DefExternal);
        result.Set("ATTR_BASIC_MDEF", data.DefInternal);
        result.Set("ATTR_BASIC_BASE_DEF_FINAL", data.Level / 2);
        result.Set("ATTR_BASIC_BASE_MDEF_FINAL", data.Level / 4);
        result.Set("ATTR_BASIC_FLEE_FINAL", data.Level);
        result.Set("ATTR_BASIC_HIT_FINAL", data.HitPoint);
        result.Set("ATTR_BASIC_CRI_RES_FINAL", data.CritResistPoint);
        result.Set("ATTR_PERCENT_CRI_DEF", data.CritDamageReduce * 100);
        result.Set("ATTR_SPECIAL_RACE", 1);
        result.Set("ATTR_SPECIAL_SIZE", 0);
        result.Stunned = (role.GetComponent<RoleEquipComponent>().Runtime.GetStates() & GeneState.Stun) != 0;
        return result;
    }
    /// <summary>保存一项具名原属性，整型截断已在对应原计算写回点完成。</summary>
    /// <param name="name">原AttrType标识符。</param>
    /// <param name="value">该属性的当前真实数值。</param>
    internal void Set(string name, double value) { Attributes[name] = value; }
    /// <summary>读取原属性；没有对应输入的扩展装备属性返回原缺省0。</summary>
    /// <param name="name">原AttrType标识符。</param>
    internal double Get(string name)
    {
        // 所有返回值都是数值型领域属性，不使用object载荷。
        double value;
        if (Attributes.TryGetValue(name, out value)) return value;
        return 0;
    }
}
