using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

/// <summary>一项原RO技能的服务端主表、一级效果与完整动作配置。</summary>
internal sealed class MoshentaSkillDefinition
{
    /// <summary>原SkillTable部署行，决定选人、伤害类别和攻速影响。</summary>
    internal readonly XElement Table;
    /// <summary>原SkillEffectTable一级行，保存CD、吟唱、伤害数组与结果Buff。</summary>
    internal readonly XElement Effect;
    /// <summary>与原二进制逐字段核对一致的MSkillData。</summary>
    internal readonly XElement Data;
    /// <summary>ResultList原顺序，数组索引就是结果编号。</summary>
    internal readonly List<XElement> Results;
    /// <summary>原技能编号。</summary>
    internal int Id { get { return MoshentaSkillCatalog.I(Table, "Id"); } }
    /// <summary>是否需要直接目标；方向/位置/自身技能由施法点决定范围。</summary>
    internal bool NeedTarget { get { return MoshentaSkillCatalog.I(Table, "SkillTargetRangeType") == 0; } }
    /// <summary>本副本PvE固定与可变吟唱之和，单位秒；怪物没有角色装备吟唱缩减。</summary>
    internal float Singing { get { return MoshentaSkillCatalog.F(Effect, "PVEFixSingingTime") + MoshentaSkillCatalog.F(Effect, "PVEFloatSingingTime"); } }

    /// <summary>保留原表和动作对象，不把多结果压平为一次普攻。</summary>
    /// <param name="table">已经取权威部署值的主表。</param>
    /// <param name="effect">对应一级效果表。</param>
    /// <param name="data">原动作XML。</param>
    internal MoshentaSkillDefinition(XElement table, XElement effect, XElement data)
    {
        Table = table;
        Effect = effect;
        Data = data;
        Results = new List<XElement>();
        if (data.Element("ResultList") != null) Results = data.Element("ResultList").Elements().ToList();
    }
}
