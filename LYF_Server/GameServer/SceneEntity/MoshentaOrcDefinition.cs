using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

/// <summary>第18波兽人的 RO 实体、技能和表现表切片，服务端决策和伤害共用同一次读取。</summary>
internal static class MoshentaOrcDefinition
{
    /// <summary>原 EntityTable 的 5515 行。</summary>
    internal static readonly XElement Entity;
    /// <summary>原 SkillEffectTable 的 1000118401 一级行。</summary>
    internal static readonly XElement Effect;
    /// <summary>实体表 AI 树名按原表顺序拆分，场景使用从零开始的下标。</summary>
    internal static readonly string[] Trees;
    /// <summary>实体技能槽实际配置的普攻号。</summary>
    internal static readonly int SkillId;
    /// <summary>实体技能槽的组号，此兽人没有其他候选。</summary>
    internal static readonly int SkillGroup;
    /// <summary>水平最大施法距离，单位米。</summary>
    internal static readonly float Range;
    /// <summary>巡逻和追击的移动速度，米/秒。</summary>
    internal static readonly float Speed;
    /// <summary>从出生到首次思考的毫秒。</summary>
    internal static readonly int FirstThink;
    /// <summary>空闲状态决策间隔，毫秒。</summary>
    internal static readonly int IdleThink;
    /// <summary>战斗状态决策间隔，毫秒。</summary>
    internal static readonly int CombatThink;
    /// <summary>当前攻速下一刀动作持续的毫秒。</summary>
    internal static readonly int ActionDuration;
    /// <summary>当前攻速下本次技能自身 PvE 冷却的毫秒。</summary>
    internal static readonly int Cooldown;
    /// <summary>当前攻速下从起手到伤害结果点的毫秒。</summary>
    internal static readonly int ResultOffset;

    /// <summary>读取随游戏服部署的原表字段，按 RO 怪物攻速公式缩放受攻速影响的普攻。</summary>
    static MoshentaOrcDefinition()
    {
        // 原配置资源，分别包含实体、技能定义、等级效果和攻击时刻。
        XElement config;
        using (Stream stream = typeof(MoshentaOrcDefinition).Assembly.GetManifestResourceStream("Moshenta.OrcBasicAttack.xml"))
            config = XElement.Load(stream);
        Entity = config.Element("EntityTable");
        Effect = config.Element("SkillEffectTable");
        Trees = ((string)Entity.Attribute("AI")).Split('|');
        // UnitSkillID 四元组依次为技能 ID、等级、组号和权重。
        string[] slot = ((string)Entity.Attribute("UnitSkillID")).Split('=');
        SkillId = int.Parse(slot[0], CultureInfo.InvariantCulture);
        SkillGroup = int.Parse(slot[2], CultureInfo.InvariantCulture);
        Range = (float)Effect.Attribute("FarRange");
        Speed = (float)Entity.Attribute("MoveSpeed");
        FirstThink = (int)Math.Round(1000 * (double)Entity.Attribute("AIStartTime"));
        IdleThink = (int)Math.Round(1000 * (double)Entity.Attribute("AIHeartInterval"));
        CombatThink = (int)Math.Round(1000 * (double)Entity.Attribute("AIHeartIntervalCombat"));
        // attribute_calculator.lua 的怪物公式为 50/(200-基础ASPD)，此兽人无装备攻速加成。
        double attacksPerSecond = 1;
        if ((int)config.Element("SkillTable").Attribute("IsAffectedASPD") != 0)
            attacksPerSecond = 50 / (200 - (double)Entity.Attribute("ASPD"));
        // 原动作和 CD 分别换算，结果向上取到不早于原 At 的第一个毫秒。
        XElement action = config.Element("MSkillData");
        ActionDuration = (int)Math.Round(1000 * (double)action.Element("Time") / attacksPerSecond);
        Cooldown = (int)Math.Round(1000 * (double)Effect.Attribute("PVECoolTime") / attacksPerSecond);
        ResultOffset = (int)Math.Ceiling(1000 * (double)action.Element("ResultList").Element("MSkillResultData").Element("At") / attacksPerSecond);
    }
}
