using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/// <summary>魔神塔实际服务端表与技能XML切片；XElement只用于原配置的序列化边界。</summary>
internal sealed class MoshentaSkillCatalog
{
    /// <summary>随游戏服部署的本副本配置目录。</summary>
    internal readonly string DirectoryPath;
    /// <summary>从RO部署表精确导出的五张表，运行中不修改。</summary>
    private readonly XElement tables;
    /// <summary>已解析的40项本副本动作，按技能编号缓存。</summary>
    private readonly Dictionary<int, MoshentaSkillDefinition> skills = new Dictionary<int, MoshentaSkillDefinition>();

    /// <summary>只装载当前副本所选数据目录，同编号技能不会借用其他目录的缓存。</summary>
    /// <param name="directory">原表、SkillData、Lua和Curve所在绝对目录。</param>
    internal MoshentaSkillCatalog(string directory)
    {
        DirectoryPath = directory;
        tables = XElement.Load(Path.Combine(DirectoryPath, "Tables.xml"));
    }

    /// <summary>取得本副本实体的完整原始字段。</summary>
    /// <param name="id">EntityTable编号。</param>
    internal XElement Entity(int id) { return Row("EntityTable", id, "Id"); }
    /// <summary>取得技能一级效果；本副本闭包全部使用一级。</summary>
    /// <param name="id">SkillEffectTable编号。</param>
    internal XElement Effect(int id) { return Row("SkillEffectTable", id, "Id"); }
    /// <summary>取得原Buff具体等级行。</summary>
    /// <param name="id">包含末两位等级的原Buff行号。</param>
    internal XElement Buff(int id) { return Row("BuffTable", id, "Id"); }
    /// <summary>取得Buff叶子效果行。</summary>
    /// <param name="id">BuffEffectTable.EffectId。</param>
    internal XElement BuffEffect(int id) { return Row("BuffEffectTable", id, "EffectId"); }

    /// <summary>取得技能及其原动作，不重新拼装或覆盖原始时点。</summary>
    /// <param name="id">SkillTable编号。</param>
    internal MoshentaSkillDefinition Skill(int id)
    {
        // 同一副本实例的每项技能配置仅初始化一次。
        MoshentaSkillDefinition result;
        if (!skills.TryGetValue(id, out result))
        {
            result = new MoshentaSkillDefinition(Row("SkillTable", id, "Id"), Effect(id),
                XElement.Load(Path.Combine(DirectoryPath, "SkillData", id.ToString(CultureInfo.InvariantCulture) + ".xml")));
            skills.Add(id, result);
        }
        return result;
    }

    /// <summary>按原表主键找行；配置缺失直接暴露，不替换为别的怪物或技能。</summary>
    /// <param name="table">原表名。</param>
    /// <param name="id">原主键值。</param>
    /// <param name="key">主键字段名。</param>
    private XElement Row(string table, int id, string key)
    {
        // XElement是配置输入，筛选结果仍为原配置对象。
        return tables.Element(table).Elements().First(row => I(row, key) == id);
    }

    /// <summary>读取表属性或原XML叶子文本，空字段沿原协议默认值。</summary>
    /// <param name="row">正在读取的原配置节点。</param>
    /// <param name="name">原字段名，区分大小写。</param>
    internal static string S(XElement row, string name)
    {
        // 表字段为属性，SkillData字段为子元素。
        XAttribute attr = row.Attribute(name);
        if (attr != null) return attr.Value;
        // 未导出的默认字段按空文本表示。
        XElement element = row.Element(name);
        if (element != null) return element.Value;
        return string.Empty;
    }
    /// <summary>按原整数或布尔字段读取，空字段为0。</summary>
    /// <param name="row">原配置节点。</param>
    /// <param name="name">原字段名。</param>
    internal static int I(XElement row, string name)
    {
        // 原运行表的bool按true/false导出，不误当缺失值。
        string value = S(row, name);
        if (value == "true") return 1;
        if (value == "false" || value.Length == 0) return 0;
        return int.Parse(value, CultureInfo.InvariantCulture);
    }
    /// <summary>按原浮点字段读取，单位由对应RO字段定义。</summary>
    /// <param name="row">原配置节点。</param>
    /// <param name="name">原字段名。</param>
    internal static float F(XElement row, string name)
    {
        // 固定InvariantCulture避免系统小数点设置改变配置值。
        string value = S(row, name);
        if (value.Length == 0) return 0;
        return float.Parse(value, CultureInfo.InvariantCulture);
    }
    /// <summary>读取按结果点配置的数组；缺少该索引时使用对应RO调用点的默认值。</summary>
    /// <param name="row">原表行。</param>
    /// <param name="name">数组字段名。</param>
    /// <param name="index">原ResultList零基索引。</param>
    /// <param name="fallback">目标数默认0，伤害倍率默认10000，固定伤害默认1。</param>
    internal static float Indexed(XElement row, string name, int index, float fallback = 0)
    {
        // 原伤害系数与目标数不能将第一项擅自广播到所有结果点。
        string[] values = S(row, name).Split('|');
        if (index >= values.Length || values[index].Length == 0) return fallback;
        return float.Parse(values[index], CultureInfo.InvariantCulture);
    }
}
