using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

/// <summary>用真正Lua5.1执行原DamageCalculator，只适配其原C++数值输入边界。</summary>
internal sealed class MoshentaDamageCalculator
{
    /// <summary>独立公式Lua状态，使用与时间轴相同的真实Lua5.1绑定。</summary>
    private readonly Lua51State lua = new Lua51State();
    /// <summary>服务端伤害、命中、暴击共用随机源。</summary>
    private readonly Random random;

    /// <summary>载入未修改的原伤害及Buff概率/寿命脚本。</summary>
    /// <param name="randomSource">本副本随机源；隔离验证可传入固定种子。</param>
    /// <param name="catalog">本副本选择的公式脚本目录，不使用进程全局目录。</param>
    internal MoshentaDamageCalculator(Random randomSource, MoshentaSkillCatalog catalog)
    {
        random = randomSource;
        lua.Register("moshenta_random", RandomValue);
        lua.Register("moshenta_float", FloatValue);
        lua.Execute(File.ReadAllText(Path.Combine(catalog.DirectoryPath, "Lua", "moshenta_formula_bridge.lua")), "moshenta_formula_bridge.lua");
        lua.Execute(File.ReadAllText(Path.Combine(catalog.DirectoryPath, "Lua", "skill", "skill_trigger_moudle.lua")), "skill/skill_trigger_moudle.lua");
        lua.Execute("package.loaded['skill.skill_trigger_moudle']=SkillEffectIDMoudle");
        lua.Execute(File.ReadAllText(Path.Combine(catalog.DirectoryPath, "Lua", "damage_calculator.lua")), "damage_calculator.lua");
        lua.Execute(File.ReadAllText(Path.Combine(catalog.DirectoryPath, "Lua", "buff_add_condition.lua")), "buff_add_condition.lua");
        lua.Execute(File.ReadAllText(Path.Combine(catalog.DirectoryPath, "Lua", "buff_time_calculator.lua")), "buff_time_calculator.lua");
        lua.Execute("package.loaded['Table/EntityTable']={}");
        lua.Execute(File.ReadAllText(Path.Combine(catalog.DirectoryPath, "Lua", "buff_trigger_condition.lua")), "buff_trigger_condition.lua");
        lua.Execute(File.ReadAllText(Path.Combine(catalog.DirectoryPath, "Lua", "buff_trigger_result.lua")), "buff_trigger_result.lua");
    }
    /// <summary>Lua随机接口边界；同一结果的所有抽样保留原脚本调用次序。</summary>
    /// <param name="state">发起本次原生回调的Lua状态。</param>
    private int RandomValue(IntPtr state)
    {
        Lua51Native.lua_pushnumber(state, random.NextDouble());
        return 1;
    }
    /// <summary>原LuaCalculateData的float字段先经C++单精度写回再暴露给Lua number。</summary><param name="state">原生Lua调用状态。</param>
    private int FloatValue(IntPtr state)
    {
        Lua51Native.lua_pushnumber(state, (float)Lua51Native.lua_tonumber(state, 1)); return 1;
    }
    /// <summary>释放本副本公式环境，与场景生命周期同步。</summary>
    internal void Dispose() { lua.Dispose(); }

    /// <summary>在单个实际结果点运行配置指定的原Lua公式。</summary>
    /// <param name="skill">权威部署的技能定义。</param>
    /// <param name="index">原ResultList零基下标。</param>
    /// <param name="caster">施法开始冻结的攻击者属性。</param>
    /// <param name="target">命中时的实际目标属性。</param>
    /// <param name="targetIndex">本结果中的目标顺序，从1开始。</param>
    /// <param name="opponents">本结果实际目标数量。</param>
    internal MoshentaDamageResult Calculate(MoshentaSkillDefinition skill, int index, MoshentaDamageSnapshot caster,
        MoshentaDamageSnapshot target, int targetIndex, int opponents)
    {
        // Damage=false仍会进入后续结果Buff，但原C++不调用伤害Lua。
        XElement point = skill.Results[index];
        if (MoshentaSkillCatalog.I(point, "Damage") == 0) return new MoshentaDamageResult { Type = 1, Count = 1 };
        // Loop决定跳字段数和最低伤害，不重新执行结果。
        int count = 1;
        if (MoshentaSkillCatalog.I(point, "Loop") != 0) count = Math.Max(1, MoshentaSkillCatalog.I(point, "LoopCount"));
        // C++ DamageLuaArgs::CalculatorProcess所需的具名计算字段。
        Dictionary<string, double> calculated = Calculated(skill, index, caster, target);
        // 只把数字和固定字段名写到Lua，不将业务object转换为动态载荷。
        StringBuilder input = new StringBuilder("{caster=").Append(EntityLua(caster)).Append(",target=").Append(EntityLua(target));
        input.Append(",skillId=").Append(skill.Id).Append(",resultIndex=").Append(index).Append(",targetIndex=").Append(targetIndex);
        input.Append(",opponents=").Append(opponents).Append(",distance=").Append(Number(System.Numerics.Vector3.Distance(caster.Position, target.Position)));
        input.Append(",count=").Append(count).Append(",damageType=").Append(MoshentaSkillCatalog.I(skill.Table, "SkillDamType"));
        input.Append(",attackType=").Append(MoshentaSkillCatalog.I(skill.Table, "SkillAttr"));
        input.Append(",defenseType=").Append(Number(target.Get("ATTR_SPECIAL_DEF_ATTR"))).Append(",suppress=0");
        input.Append(",formula='").Append(MoshentaSkillCatalog.S(skill.Effect, "DamageScript")).Append("',calculated=").Append(MapLua(calculated)).Append('}');
        // 一次调用返回同一次计算的伤害与判定，后续网络层不重新抽样。
        string[] values = lua.EvaluateString("MoshentaCalculate(" + input + ")").Split(',');
        return new MoshentaDamageResult { Damage = int.Parse(values[0], CultureInfo.InvariantCulture),
            Type = int.Parse(values[1], CultureInfo.InvariantCulture), Count = int.Parse(values[3], CultureInfo.InvariantCulture) };
    }

    /// <summary>按原Buff函数实际计算添加概率及秒制寿命。</summary>
    /// <param name="row">具体等级Buff原表行。</param>
    /// <param name="caster">施加者快照。</param>
    /// <param name="target">实际宿主快照。</param>
    /// <param name="duration">原DestroyTiming时间；小于0表示常驻。</param>
    internal double BuffDuration(XElement row, MoshentaDamageSnapshot caster, MoshentaDamageSnapshot target, double duration)
    {
        // 两个脚本共享原配置中的具名参数，时间脚本可覆盖最终寿命。
        Dictionary<string, double> parameters = new Dictionary<string, double>();
        // AddCondition原文本的第一段是函数名。
        string condition = ParseRule(MoshentaSkillCatalog.S(row, "AddCondition"), parameters);
        // DestroyLua控制BuffTime，不是固定销毁回调。
        string rule = ParseRule(MoshentaSkillCatalog.S(row, "DestroyLua"), parameters);
        if (condition.Length == 0 && rule.Length == 0) return duration;
        return lua.EvaluateNumber("MoshentaBuffRule({caster=" + EntityLua(caster) + ",target=" + EntityLua(target)
            + ",duration=" + Number(duration) + ",condition='" + condition + "',timeRule='" + rule + "',params=" + MapLua(parameters) + "})");
    }
    /// <summary>拆解原LuaScriptData的函数名及数值参数。</summary>
    /// <param name="source">函数名|键=数值形式的原配置。</param>
    /// <param name="parameters">本次Buff规则共享的数值表。</param>
    private static string ParseRule(string source, Dictionary<string, double> parameters)
    {
        // 空字段表示无需额外脚本。
        string[] segments = source.Split('|');
        for (int i = 1; i < segments.Length; i++)
        {
            // 原表规则中每段均为确定的数值键值对。
            string[] pair = segments[i].Split('=');
            parameters[pair[0]] = double.Parse(pair[1], CultureInfo.InvariantCulture);
        }
        return segments[0];
    }

    /// <summary>复用原damage_def.cpp的玩家PvE/怪物防御边界，不重新实现Lua伤害公式。</summary>
    /// <param name="skill">原技能表与等级效果。</param>
    /// <param name="index">当前结果点。</param>
    /// <param name="caster">冻结施法者属性。</param>
    /// <param name="target">当前受击者属性。</param>
    private static Dictionary<string, double> Calculated(MoshentaSkillDefinition skill, int index, MoshentaDamageSnapshot caster, MoshentaDamageSnapshot target)
    {
        // 当前双方真实装备防御，RO盾防/精炼缺失时保持0。
        float physical = (float)Math.Max(0, target.Get("ATTR_BASIC_DEF") - caster.Get("ATTR_BASIC_IGNORE_DEF"));
        // 魔防独立减去施法者原忽视魔防。
        float magical = (float)Math.Max(0, target.Get("ATTR_BASIC_MDEF") - caster.Get("ATTR_BASIC_IGNORE_MDEF"));
        // 原玩家分支最低保留10%，怪物分支为5%。
        float minimum = 0.05f;
        // 玩家未提供精炼减伤时，原公式仍有5%的基础修正。
        float refine = 1;
        if (target.IsPlayer) { minimum = 0.1f; refine = 0.95f; }
        // 只填本闭包原表实际具有的修正；不存在的装备词条为0。
        Dictionary<string, double> values = new Dictionary<string, double>();
        values["caster_weapon_type"] = 0; values["target_size"] = target.Get("ATTR_SPECIAL_SIZE");
        values["caster_race"] = caster.Get("ATTR_SPECIAL_RACE"); values["target_race"] = target.Get("ATTR_SPECIAL_RACE");
        values["skill_num"] = MoshentaSkillCatalog.Indexed(skill.Effect, "PVEStaticDam", index, 1);
        values["skill_per"] = MoshentaSkillCatalog.Indexed(skill.Effect, "PVEDamageRadio", index, 10000) / 10000f;
        values["atk"] = caster.Get("ATTR_BASIC_BASE_ATK_FINAL"); values["matk"] = caster.Get("ATTR_BASIC_BASE_MATK_FINAL");
        values["wuqi_atk"] = caster.Get("ATTR_BASIC_WEAPON_ATK_FINAL"); values["wuqi_matk"] = caster.Get("ATTR_BASIC_WEAPON_MATK_FINAL");
        values["def_suzhi"] = target.Get("ATTR_BASIC_BASE_DEF_FINAL"); values["mdef_suzhi"] = target.Get("ATTR_BASIC_BASE_MDEF_FINAL");
        values["def_jianshang"] = Math.Max((physical + 4000) / (physical * 10 + 4000), minimum) * refine;
        values["mdef_jianshang"] = Math.Max((magical + 1000) / (magical * 10 + 1000), minimum) * refine;
        values["yuansu"] = 1; values["npc_dam_final"] = 1;
        values["size_dam_per"] = 0; values["size_res_per"] = 0; values["race_dam_per"] = 0; values["race_res_per"] = 0;
        values["wuqi_atk_per"] = 0; values["wuqi_matk_per"] = 0; values["jinglian_atk"] = 0; values["jinglian_matk"] = 0;
        values["caster_atk_up"] = 0; values["caster_matk_up"] = 0;
        values["target_atk_down"] = target.Get("ATTR_PERCENT_DAM_REDUC");
        values["target_matk_down"] = target.Get("ATTR_PERCENT_MDAM_REDUC");
        return values;
    }

    /// <summary>原光环条件Lua在当前实际角色符号上执行。</summary><param name="effect">原效果叶子。</param><param name="target">范围内角色快照。</param>
    internal bool BuffCondition(XElement effect, MoshentaDamageSnapshot target)
    {
        // 原LuaScriptData仅含数值参数。
        Dictionary<string, double> parameters = new Dictionary<string, double>();
        string name = ParseRule(MoshentaSkillCatalog.S(effect, "TriggerCondition"), parameters);
        if (name.Length == 0) return true;
        return lua.EvaluateNumber("MoshentaBuffCondition({target=" + EntityLua(target) + ",name='" + name + "',params=" + MapLua(parameters) + "})") != 0;
    }
    /// <summary>运行原触发结果Lua，并将其实际请求的动作交回场景宿主。</summary><param name="effect">原效果叶子。</param><param name="caster">来源快照。</param><param name="target">宿主快照。</param>
    internal string BuffResult(XElement effect, MoshentaDamageSnapshot caster, MoshentaDamageSnapshot target)
    {
        // 本闭包Lua结果只有Nil、Suicide和不可达的勋章条件分支。
        Dictionary<string, double> parameters = new Dictionary<string, double>();
        string name = ParseRule(MoshentaSkillCatalog.S(effect, "EffectParam"), parameters);
        return lua.EvaluateString("MoshentaBuffResult({caster=" + EntityLua(caster) + ",target=" + EntityLua(target) + ",name='" + name + "',params=" + MapLua(parameters) + "})");
    }
    /// <summary>构造原Lua对象所需的强类型数值表文本。</summary>
    /// <param name="snapshot">本次输入的真实属性快照。</param>
    private static string EntityLua(MoshentaDamageSnapshot snapshot)
    {
        // 姿态供MagicSkillCalcFace直接读取。
        StringBuilder text = new StringBuilder("{attrs=").Append(MapLua(snapshot.Attributes)).Append(",buffs={");
        // 按当前真实Buff家族写入等级，不合成不存在的装备Buff。
        List<int> keys = new List<int>(snapshot.Buffs.Keys);
        for (int i = 0; i < keys.Count; i++) text.Append('[').Append(keys[i]).Append("]=").Append(snapshot.Buffs[keys[i]]).Append(',');
        text.Append("},attackers=").Append(snapshot.AttackerCount).Append(",stunned=").Append(snapshot.Stunned.ToString().ToLowerInvariant());
        text.Append(",position={x=").Append(Number(snapshot.Position.X)).Append(",z=").Append(Number(snapshot.Position.Z)).Append("},face=").Append(Number(snapshot.Facing)).Append('}');
        return text.ToString();
    }
    /// <summary>序列化固定原属性名和纯数字；这里是Lua语言边界。</summary>
    /// <param name="values">强类型数值字典，不承载任意业务对象。</param>
    private static string MapLua(Dictionary<string, double> values)
    {
        // 原属性标识符来自固定代码或原表参数，不包含用户文本。
        StringBuilder text = new StringBuilder("{");
        // 索引列表保留原数字字典引用，不克隆领域数据。
        List<string> keys = new List<string>(values.Keys);
        for (int i = 0; i < keys.Count; i++) text.Append("['").Append(keys[i]).Append("']=").Append(Number(values[keys[i]])).Append(',');
        return text.Append('}').ToString();
    }
    /// <summary>Lua数字使用不受区域设置影响的往返表示。</summary>
    /// <param name="value">有限的配置或战斗数值。</param>
    private static string Number(double value) { return value.ToString("R", CultureInfo.InvariantCulture); }
}
