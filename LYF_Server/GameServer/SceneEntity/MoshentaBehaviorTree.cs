using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;

/// <summary>执行魔神塔实际12棵RO导出树；原C++ Update只有bool，跨帧动作由宿主负责。</summary>
internal sealed class MoshentaBehaviorTree
{
    /// <summary>进程共享的只读XML树，实体黑板绝不放入此缓存。</summary>
    private static readonly Dictionary<string, XElement> definitions = new Dictionary<string, XElement>();
    /// <summary>当前副本的场景变量、事件和真实实体出口。</summary>
    private readonly SceneWaveSpawner scene;
    /// <summary>实体树宿主；SceneAI使用null，场景状态仍保存在本树。</summary>
    private readonly MoshentaActor actor;
    /// <summary>当前主树完整XML根。</summary>
    private readonly XElement root;
    /// <summary>字符串黑板，包括AIEvent和动作状态。</summary>
    private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
    /// <summary>原SharedFloat槽，运算按32位浮点。</summary>
    private readonly Dictionary<string, float> floats = new Dictionary<string, float>();
    /// <summary>原SharedInt槽，保存人格状态等整数。</summary>
    private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
    /// <summary>原SharedBool槽，保存每实体初始化和冷却标志。</summary>
    private readonly Dictionary<string, bool> bools = new Dictionary<string, bool>();
    /// <summary>原Transform与UUID槽均存真实实体身份，并在同实例解析。</summary>
    private readonly Dictionary<string, ulong> ids = new Dictionary<string, ulong>();
    /// <summary>世界坐标槽，单位米。</summary>
    private readonly Dictionary<string, Vector3> vectors = new Dictionary<string, Vector3>();
    /// <summary>原群体查找的UUID集合，允许节点原地过滤。</summary>
    private readonly Dictionary<string, List<ulong>> groups = new Dictionary<string, List<ulong>>();
    /// <summary>原技能筛选结果列表。</summary>
    private readonly Dictionary<string, List<int>> skillLists = new Dictionary<string, List<int>>();
    /// <summary>Common_Monster_Summon等旧技能节点共享的筛选候选。</summary>
    private List<int> canCast = new List<int>();
    /// <summary>当前待消费事件，ResetEvent显式清除。</summary>
    internal MoshentaAiEvent Event { get; set; } = new MoshentaAiEvent();
    /// <summary>原公共tick_count，影响左右交替站位。</summary>
    private int ticks;

    /// <summary>准备主树及引用树的实例黑板。</summary><param name="owner">所属场景。</param><param name="host">实体宿主，SceneAI为空。</param><param name="name">原树文件主名。</param>
    internal MoshentaBehaviorTree(SceneWaveSpawner owner, MoshentaActor host, string name)
    {
        scene = owner; actor = host; root = Load(name); Initialize(root);
        if (actor != null) { floats["min_attack_range"] = actor.AttackRange; vectors["out_fight_pos"] = actor.BornPosition; vectors["born_pos"] = actor.BornPosition; }
        floats["tolerance_range"] = 0.5f;
    }
    /// <summary>读取嵌入的原始导出XML，未知树直接失败。</summary><param name="name">原树名。</param>
    private static XElement Load(string name)
    {
        // 同一逻辑线程首次装载后复用只读定义。
        XElement value;
        if (definitions.TryGetValue(name, out value)) return value;
        using (Stream source = typeof(MoshentaBehaviorTree).Assembly.GetManifestResourceStream("Moshenta.AI." + name + ".xml"))
        {
            if (source == null) throw new FileNotFoundException("未部署RO AI树：" + name);
            value = XElement.Load(source);
        }
        definitions.Add(name, value); return value;
    }
    /// <summary>按EntryTask类型前缀建立槽；子树合并不覆盖主树已有值。</summary><param name="tree">当前树根。</param>
    private void Initialize(XElement tree)
    {
        // XML属性顺序不决定行为，仅提供默认值。
        XAttribute[] attributes = tree.Element("EntryTask").Attributes().ToArray();
        for (int index = 0; index < attributes.Length; index++)
        {
            // 首个下划线分隔RO类型前缀和黑板名。
            string field = attributes[index].Name.LocalName;
            int separator = field.IndexOf('_');
            if (separator < 0) continue;
            string type = field.Substring(0, separator);
            string key = field.Substring(separator + 1);
            string value = attributes[index].Value;
            switch (type)
            {
                case "S": if (!strings.ContainsKey(key)) strings[key] = value; break;
                case "F": if (!floats.ContainsKey(key)) floats[key] = Number(value); break;
                case "I": if (!ints.ContainsKey(key)) ints[key] = (int)Number(value); break;
                case "B": if (!bools.ContainsKey(key)) bools[key] = value == "1"; break;
                case "T": case "L": if (!ids.ContainsKey(key)) ids[key] = 0; break;
                case "V": if (!vectors.ContainsKey(key)) vectors[key] = Vector(value); break;
                case "LL": if (!groups.ContainsKey(key)) groups[key] = new List<ulong>(); break;
                case "IL": if (!skillLists.ContainsKey(key)) skillLists[key] = new List<int>(); break;
            }
        }
        // BehaviorTreeReference沿本树的UnityObjects索引解析真正子树。
        XElement[] pointers = tree.Element("UnityObjects").Elements("pointer").ToArray();
        for (int index = 0; index < pointers.Length; index++) Initialize(Load((string)pointers[index].Attribute("fileName")));
    }
    /// <summary>执行一次原树，不把失败前已写黑板回滚。</summary>
    internal bool Tick() { ticks++; return Run(root.Element("EntryTask"), root); }
    /// <summary>原Shared字段的槽名，空名表示读取字面参数。</summary><param name="node">节点。</param><param name="field">原字段。</param>
    private static string Key(XElement node, string field) { return (string)node.Attribute("Shared_" + field + "Name") ?? ""; }
    /// <summary>原Shared字段字面值。</summary><param name="node">节点。</param><param name="field">原字段。</param>
    private static string Literal(XElement node, string field) { return (string)node.Attribute("Shared_" + field + "mValue") ?? ""; }
    /// <summary>以不受系统区域影响的原格式解析浮点。</summary><param name="text">原数字文本，可空。</param>
    private static float Number(string text) { if (text.Length == 0) return 0; return float.Parse(text, CultureInfo.InvariantCulture); }
    /// <summary>解析原XML冒号分隔坐标。</summary><param name="text">x:y:z文本。</param>
    private static Vector3 Vector(string text) { if (text.Length == 0) return Vector3.Zero; string[] fields = text.Split(':'); return new Vector3(Number(fields[0]), Number(fields[1]), Number(fields[2])); }
    /// <summary>读字符串Shared槽或常量。</summary><param name="node">节点。</param><param name="field">字段。</param>
    private string S(XElement node, string field) { string value; if (Key(node, field).Length > 0 && strings.TryGetValue(Key(node, field), out value)) return value; return Literal(node, field); }
    /// <summary>读浮点Shared槽或常量。</summary><param name="node">节点。</param><param name="field">字段。</param>
    private float F(XElement node, string field) { float value; if (Key(node, field).Length > 0 && floats.TryGetValue(Key(node, field), out value)) return value; return Number(Literal(node, field)); }
    /// <summary>读整数Shared槽或常量。</summary><param name="node">节点。</param><param name="field">字段。</param>
    private int I(XElement node, string field) { int value; if (Key(node, field).Length > 0 && ints.TryGetValue(Key(node, field), out value)) return value; return (int)Number(Literal(node, field)); }
    /// <summary>读布尔Shared槽或常量。</summary><param name="node">节点。</param><param name="field">字段。</param>
    private bool B(XElement node, string field) { bool value; if (Key(node, field).Length > 0 && bools.TryGetValue(Key(node, field), out value)) return value; return Literal(node, field) == "1"; }
    /// <summary>读引用/UUID槽，空引用保持0，不制造实体。</summary><param name="node">节点。</param><param name="field">字段。</param>
    private ulong Id(XElement node, string field) { ulong value; if (Key(node, field).Length > 0 && ids.TryGetValue(Key(node, field), out value)) return value; if (ulong.TryParse(Literal(node, field), out value)) return value; return 0; }
    /// <summary>读坐标Shared槽或常量。</summary><param name="node">节点。</param><param name="field">字段。</param>
    private Vector3 V(XElement node, string field) { Vector3 value; if (Key(node, field).Length > 0 && vectors.TryGetValue(Key(node, field), out value)) return value; return Vector(Literal(node, field)); }
    /// <summary>取得原地可过滤的UUID列表。</summary><param name="node">节点。</param><param name="field">列表字段。</param>
    private List<ulong> G(XElement node, string field) { List<ulong> value; if (Key(node, field).Length > 0 && groups.TryGetValue(Key(node, field), out value)) return value; return new List<ulong>(); }
    /// <summary>原比较枚举与浮点容差。</summary><param name="type">cmp_type。</param><param name="first">左值。</param><param name="second">右值。</param><param name="epsilon">整数0、浮点原容差。</param>
    private static bool Compare(string type, double first, double second, double epsilon)
    {
        switch (type)
        {
            case "kCmpTypeLessThan": return first < second - epsilon;
            case "kCmpTypeLessOrEqualTo": return first <= second + epsilon;
            case "kCmpTypeEqualTo": if (epsilon == 0) return first == second; return Math.Abs(first - second) < epsilon;
            case "kCmpTypeNotEqualTo": if (epsilon == 0) return first != second; return Math.Abs(first - second) >= epsilon;
            case "kCmpTypeGreaterOrEqualTo": return first >= second - epsilon;
            case "kCmpTypeGreaterTo": return first > second + epsilon;
            default: throw new InvalidDataException("原AI未知比较类型 " + type);
        }
    }
    /// <summary>递归执行原节点种类、父子结构和先后次序。</summary><param name="node">实际节点。</param><param name="tree">当前引用表所属树。</param>
    private bool Run(XElement node, XElement tree)
    {
        // 原XML运行时边界，以具名节点分类，不以未知节点成功兜底。
        string kind = node.Name.LocalName;
        switch (kind)
        {
            case "EntryTask": return Run(node.Elements().First(), tree);
            case "Sequence":
                for (XElement child = node.Elements().FirstOrDefault(); child != null; child = child.NextNode as XElement) if (!Run(child, tree)) return false;
                return true;
            case "Selector":
                for (XElement child = node.Elements().FirstOrDefault(); child != null; child = child.NextNode as XElement) if (Run(child, tree)) return true;
                return false;
            case "Parallel":
                // 原C++按位AND，必须执行每个child的副作用。
                bool success = true;
                for (XElement child = node.Elements().FirstOrDefault(); child != null; child = child.NextNode as XElement) success &= Run(child, tree);
                return success;
            case "ReturnSuccess": Run(node.Elements().First(), tree); return true;
            case "ReturnFailure": Run(node.Elements().First(), tree); return false;
            case "BehaviorTreeReference":
                // 原externalBehaviors是当前UnityObjects数组下标。
                XElement pointer = tree.Element("UnityObjects").Elements("pointer").ElementAt((int)node.Attribute("externalBehaviors"));
                XElement referenced = Load((string)pointer.Attribute("fileName"));
                return Run(referenced.Element("EntryTask"), referenced);
            case "AIGetCurrentState": strings[Key(node, "store_result")] = actor.CurrentState; return true;
            case "AIStringEqual": return S(node, "string1") == S(node, "string2");
            case "AIBoolComparison": return B(node, "bool1") == B(node, "bool2");
            case "AIIntComparison": return Compare((string)node.Attribute("cmp_type"), I(node, "int1"), I(node, "int2"), 0);
            case "AIFloatComparison": return Compare((string)node.Attribute("cmp_type"), F(node, "float1"), F(node, "float2"), 0.000001);
            case "AIFloatOperator": floats[Key(node, "store_result")] = F(node, "float1") - F(node, "float2"); return true;
            case "AISetBool": bools[Key(node, "store_result")] = B(node, "value"); return true;
            case "AISetFloat": floats[Key(node, "store_result")] = F(node, "value"); return true;
            case "AIRandomFloat": floats[Key(node, "store_result")] = F(node, "min") + (float)scene.Random.NextDouble() * (F(node, "max") - F(node, "min")); return true;
            case "AIRandomProbability": return scene.Random.NextDouble() <= F(node, "probability");
            case "AINewCooldown":
                // 原秒制时间戳初值0时只初始化而不触发。
                float last = F(node, "last_check_time");
                float now = scene.Now / 1000f;
                if (last == 0) { floats[Key(node, "last_check_time")] = now; return false; }
                if (now < last || now - last < F(node, "cd_time")) return false;
                floats[Key(node, "last_check_time")] = now; return true;
            case "AIFindTargetByHatred":
                if (actor.Target == null) return false;
                ids[Key(node, "store_target")] = actor.Target.GetID();
                floats[Key(node, "store_target_distance")] = actor.DistanceTo(actor.Target.mainRoleInfo.BaseInfo.Pos.ToVector3()); return true;
            case "AIGetTargetUUID":
                // 空直接目标回退宿主，与原C++相同。
                ulong target = Id(node, "direct_target");
                if (target == 0) target = actor.UnitId;
                ids[Key(node, "store_uuid")] = target; return true;
            case "AIGetTargetIsDead":
                if (!scene.ContainsEntity(Id(node, "target"))) return false;
                bools[Key(node, "store_is_dead")] = scene.IsEntityDead(Id(node, "target")); return true;
            case "AIGetPostion":
                if (B(node, "is_self")) vectors[Key(node, "store_postion")] = actor.Position;
                else { if (!scene.ContainsEntity(Id(node, "target"))) return false; vectors[Key(node, "store_postion")] = scene.EntityPosition(Id(node, "target")); }
                return true;
            case "AIGetTargetFightPosition":
                // 站位失败后原Selector继续目标当前位置分支。
                Vector3 fight;
                if (!actor.FightPosition(scene.EntityPosition(Id(node, "fight_target")), F(node, "distance"), out fight)) return false;
                vectors[Key(node, "store_pos")] = fight; return true;
            case "AICalDistance": floats[Key(node, "store_result")] = HorizontalDistance(PositionInput(node, "target1", "dest1"), PositionInput(node, "target2", "dest2")); return true;
            case "AIDistanceTo": floats[Key(node, "store_result")] = HorizontalDistance(actor.Position, PositionInput(node, "target", "dest")); return true;
            case "AISetDest": return SetDestination(node);
            case "AIActionMove": return actor.Move(V(node, "move_dest"));
            case "AIPatrolWhthPath": return actor.Patrol();
            case "AIFilterGroupSkill": skillLists[Key(node, "store_skills")] = actor.Skills.FilterGroup(I(node, "group_id"), Id(node, "target_uuid")); return skillLists[Key(node, "store_skills")].Count > 0;
            case "AICastSkillByList": return CastList(skillLists[Key(node, "store_skills")], Id(node, "target_uuid"));
            case "AIFilterSkill": return FilterSkills(node);
            case "AIDoCastSkill": return CastList(canCast, Id(node, "target"));
            case "AIDoCastSkillByIDLevel": return actor.Skills.Cast(int.Parse(S(node, "skill_id"), CultureInfo.InvariantCulture), I(node, "skill_level"), Id(node, "target"), CastPosition(Id(node, "target")));
            case "AIActivePassive": return ActivePassive(node);
            case "AIBuffOperator": return Buff(node);
            case "AIGroupBuffOperator": return GroupBuff(node);
            case "AIGroupFindAllPlayer": groups[Key(node, "players")] = scene.PlayerIds(); return true;
            case "AINewGroupFindByEntityType": return FindPlayers(node);
            case "AINewGroupFindByWave":
                // 波号常量按原逗号格式读取，结果保留原场景登记顺序。
                List<ulong> waveEntities = scene.WaveEntities(S(node, "wave_ids"));
                if (waveEntities.Count == 0) return false;
                groups[Key(node, "store_entitys")] = waveEntities; return true;
            case "AINewGroupFilterDistance": return FilterDistance(node);
            case "AINewGroupFilterBuff": return FilterBuff(node);
            case "AINewGroupFindEnd": return FindEnd(node);
            case "AINewGroupUUIDtoTransform": if (!scene.ContainsEntity(Id(node, "uuid"))) return false; ids[Key(node, "store_transform")] = Id(node, "uuid"); return true;
            case "AIGetSceneVar": return ReadSceneVariable(node);
            case "AISetSceneVar": scene.SetSceneVariable(S(node, "key"), S(node, "value"), (string)node.Attribute("var_type")); return true;
            case "AINewSetSceneVar": return WriteSceneVariable(node);
            case "AIReceiveEvent": return ReceiveEvent(node);
            case "AIResetEvent": Event = new MoshentaAiEvent(); strings[Key(node, "event_name")] = ""; return true;
            case "AISendSceneEvent": scene.ScheduleSceneEvent(S(node, "event_type"), I(node, "int_arg"), F(node, "float_arg"), Id(node, "entity_uuid"), (int)(F(node, "delay_time") * 1000)); return true;
            case "AISendEventToSceneTriggerGroup": return scene.Flow.SendTriggerGroup(I(node, "group_id"), I(node, "trigger_id"));
            case "AIWaveEvent": return scene.Flow.RecordWaveEvent(S(node, "event_name"));
            case "AICheckWaveMonsterAllDie": return scene.IsWaveDead(I(node, "wave_id"));
            case "AIShareHP": scene.ShareHp(S(node, "wave_ids"), B(node, "enable")); return true;
            case "AITimeAxisWithNode": if (actor.Timeline == null) return false; actor.Timeline.Update(scene.Now); return true;
            case "AISwitchAxis": return actor.Timeline != null && actor.Timeline.Switch(I(node, "time_axis"));
            case "AIShowTVNotice": scene.PublishAiNotice(1, I(node, "annouce_id"), actor, Id(node, "send_target")); return true;
            case "AIEmotion": scene.PublishAiNotice(2, I(node, "take_photo_status"), actor, Id(node, "role_see")); return true;
            case "AICallBlackCurtain": scene.PublishAiNotice(3, I(node, "id"), actor, Id(node, "send_target")); return true;
            default: throw new InvalidDataException("魔神塔未实现的原RO节点 " + kind);
        }
    }
    /// <summary>计算原水平距离。</summary><param name="first">起点。</param><param name="second">终点。</param>
    private static float HorizontalDistance(Vector3 first, Vector3 second) { Vector3 delta = first - second; return (float)Math.Sqrt(delta.X * delta.X + delta.Z * delta.Z); }
    /// <summary>目标绑定优先于位置常量。</summary><param name="node">节点。</param><param name="target">目标字段。</param><param name="position">坐标字段。</param>
    private Vector3 PositionInput(XElement node, string target, string position) { if (Key(node, target).Length > 0) return scene.EntityPosition(Id(node, target)); return V(node, position); }
    /// <summary>无目标技能以自身位置施法。</summary><param name="target">真实目标UID。</param>
    private Vector3 CastPosition(ulong target) { if (target == 0) return actor.Position; return scene.EntityPosition(target); }
    /// <summary>按原固定万分比权重抽签施法。</summary><param name="skills">已筛选技能列表。</param><param name="target">目标UID。</param>
    private bool CastList(List<int> skills, ulong target) { return actor.Skills.CastFromList(skills, target, CastPosition(target)); }
    /// <summary>召唤机制按原实体绑定表筛选技能，保留无目标施法。</summary><param name="node">原筛选节点。</param>
    private bool FilterSkills(XElement node)
    {
        canCast.Clear();
        // 原绑定仅在本场景闭包中读取，不扫描全服技能。
        string[] bindings = MoshentaSkillCatalog.S(MoshentaSkillCatalog.Entity(actor.EntityId), "UnitSkillID").Split('|');
        for (int index = 0; index < bindings.Length; index++)
        {
            // 原SkillTable编号为第一个等号字段。
            int id;
            if (!int.TryParse(bindings[index].Split('=')[0], out id)) continue;
            bool valid;
            if (B(node, "use_attack_field")) valid = actor.Skills.CanCast(id, 1, Id(node, "target"), CastPosition(Id(node, "target")));
            else valid = actor.Skills.CanCastIgnoringRange(id, 1, Id(node, "target"), CastPosition(Id(node, "target")));
            if (valid) canCast.Add(id);
        }
        return canCast.Count > 0;
    }
    /// <summary>原SetDest在当前三种实际参数组合中产生游走或逃离位置。</summary><param name="node">原节点。</param>
    private bool SetDestination(XElement node)
    {
        // 原set_dest_type决定基本目标，不改变后续随机分布。
        Vector3 position = actor.Position;
        string type = (string)node.Attribute("set_dest_type");
        if (type == "kTarget") { if (!scene.ContainsEntity(Id(node, "target"))) return false; position = scene.EntityPosition(Id(node, "target")); }
        else if (type == "kBornPos") position = actor.BornPosition;
        // 随机X/Z独立均匀，保持原方形分布而非圆形替代。
        float random = F(node, "random_max");
        if (random > 0) position += new Vector3((float)(scene.Random.NextDouble() * 2 - 1), 0, (float)(scene.Random.NextDouble() * 2 - 1)) * random;
        // 方向以自身朝向或目标外侧向量决定。
        Vector3 direction = new Vector3((float)Math.Sin(actor.Facing * Math.PI / 180), 0, (float)Math.Cos(actor.Facing * Math.PI / 180));
        if ((string)node.Attribute("adjust_dir_type") == "kTargetDir") direction = actor.Position - position;
        direction.Y = 0;
        // 原tick_count奇偶使调整角左右交替。
        float angle = (ticks % 2) * F(node, "adjust_angle") * 2 - F(node, "adjust_angle");
        direction = OrcNavigation.RotateDirection(direction, angle);
        if (direction.LengthSquared() > 0) position += Vector3.Normalize(direction) * F(node, "adjust_length");
        vectors[Key(node, "store_final_dest")] = position; return true;
    }
    /// <summary>对自身或原目标组切换主动仇恨门控。</summary><param name="node">节点。</param>
    private bool ActivePassive(XElement node)
    {
        if (B(node, "is_self")) { actor.Passive = B(node, "is_passive"); return true; }
        // 单目标及列表均可能配置，按原节点顺序全部处理。
        MoshentaActor target = scene.FindActor(Id(node, "target"));
        if (target != null) target.Passive = B(node, "is_passive");
        List<ulong> list = G(node, "targetlist");
        for (int index = 0; index < list.Count; index++) { target = scene.FindActor(list[index]); if (target != null) target.Passive = B(node, "is_passive"); }
        return true;
    }
    /// <summary>原单体Buff操作。</summary><param name="node">节点。</param>
    private bool Buff(XElement node)
    {
        // is_self优先，其次显式hoster，最后仇恨目标。
        ulong target = Id(node, "hoster_uuid");
        if (B(node, "is_self")) target = actor.UnitId;
        else if (target == 0 && actor.Target != null) target = actor.Target.GetID();
        if (!scene.ContainsEntity(target)) return false;
        if (I(node, "operator_id") == 0) scene.AddBuff(actor, target, int.Parse(S(node, "buff_id"), CultureInfo.InvariantCulture));
        else scene.RemoveBuff(target, int.Parse(S(node, "buff_id"), CultureInfo.InvariantCulture));
        return true;
    }
    /// <summary>原群体Buff对每个真实UUID处理，输入空返回false。</summary><param name="node">节点。</param>
    private bool GroupBuff(XElement node)
    {
        // 列表由之前查找节点写入，过滤后允许为空。
        List<ulong> list = G(node, "transforms");
        if (list.Count == 0) return false;
        for (int index = 0; index < list.Count; index++)
        {
            if (I(node, "opid") == 0) scene.AddBuff(actor, list[index], I(node, "buffid"));
            else scene.RemoveBuff(list[index], I(node, "buffid"));
        }
        return true;
    }
    /// <summary>按实际TYPE_ROLE/360度参数查找半径内玩家。</summary><param name="node">节点。</param>
    private bool FindPlayers(XElement node)
    {
        // 当前实际树只有角色圆形查询，未支持的类型必须显式失败。
        if ((string)node.Attribute("entity_type") != "TYPE_ROLE") throw new InvalidDataException("非魔神塔实际查找类型");
        List<ulong> result = scene.PlayerIds();
        for (int index = result.Count - 1; index >= 0; index--)
        {
            // 水平范围与原scope的近远闭区间一致。
            float distance = actor.DistanceTo(scene.EntityPosition(result[index]));
            if (distance < F(node, "distance_short") || distance > F(node, "distance_long")) result.RemoveAt(index);
        }
        if (result.Count == 0) return false;
        groups[Key(node, "store_entitys")] = result; return true;
    }
    /// <summary>在原列表上按敌友和水平距离过滤，过滤后空仍返回true。</summary><param name="node">节点。</param>
    private bool FilterDistance(XElement node)
    {
        // 输入为空时原节点尚未完成一次过滤。
        List<ulong> list = G(node, "store_entitys");
        if (list.Count == 0) return false;
        for (int index = list.Count - 1; index >= 0; index--)
        {
            // 本副本怪物与玩家相敌，怪物同阵营相友。
            bool player = scene.FindRole(list[index]) != null;
            float distance = actor.DistanceTo(scene.EntityPosition(list[index]));
            if (I(node, "enemy_friend") == 0 && !player) { list.RemoveAt(index); continue; }
            if (I(node, "enemy_friend") == 1 && player) { list.RemoveAt(index); continue; }
            if (distance < F(node, "short_distance") || distance > F(node, "far_distance")) list.RemoveAt(index);
        }
        return true;
    }
    /// <summary>原地保留有或没有指定Buff家族的实体。</summary><param name="node">节点。</param>
    private bool FilterBuff(XElement node)
    {
        // HasBuff使用基础Buff家族ID，与AddBuff完整ID不同。
        List<ulong> list = G(node, "store_entitys");
        if (list.Count == 0) return false;
        for (int index = list.Count - 1; index >= 0; index--) if (scene.HasBuff(list[index], I(node, "buff_id")) != B(node, "has_or_not")) list.RemoveAt(index);
        return true;
    }
    /// <summary>完成原群体选择，随机、截断和空气墙检查保持原先后。</summary><param name="node">节点。</param>
    private bool FindEnd(XElement node)
    {
        // 当前过滤结果原地保存，后续节点可继续消费同一列表。
        List<ulong> list = G(node, "store_entitys");
        if (list.Count == 0) return false;
        if (I(node, "random") == 1) scene.Shuffle(list);
        // 原maxnum=0兼容为1，负值不截断。
        int maximum = I(node, "maxnum");
        if (maximum == 0) maximum = 1;
        if (maximum >= 0 && maximum < list.Count) list.RemoveRange(maximum, list.Count - maximum);
        // 原默认index=0，即全部挡住时仍保留第一个结果。
        int selected = 0;
        for (int index = 0; index < list.Count; index++) if (!scene.Flow.Blocks(actor.Position, scene.EntityPosition(list[index]))) { selected = index; break; }
        if (selected < list.Count) ids[Key(node, "store_uuid")] = list[selected];
        return true;
    }
    /// <summary>读取场景共享变量到本实体对应类型槽。</summary><param name="node">节点。</param>
    private bool ReadSceneVariable(XElement node)
    {
        // 类型由场景存储决定，不能因目标槽名称推测。
        MoshentaSceneVariable value;
        if (!scene.TrySceneVariable(S(node, "key"), out value)) return false;
        if (value.Type == "kTypeInt") ints[Key(node, "int_value")] = value.IntValue;
        else if (value.Type == "kTypeFloat") floats[Key(node, "float_value")] = value.FloatValue;
        else strings[Key(node, "string_value")] = value.StringValue;
        return true;
    }
    /// <summary>把本节点配置的类型和值写入场景共享黑板。</summary><param name="node">节点。</param>
    private bool WriteSceneVariable(XElement node)
    {
        // 原AINewSetSceneVar的key使用字面args_.key。
        string type = (string)node.Attribute("var_type");
        string value = S(node, "string_value");
        if (type == "kTypeInt") value = I(node, "int_value").ToString(CultureInfo.InvariantCulture);
        else if (type == "kTypeFloat") value = F(node, "float_value").ToString(CultureInfo.InvariantCulture);
        scene.SetSceneVariable(Literal(node, "key"), value, type); return true;
    }
    /// <summary>接收不会自动消费事件，ResetEvent节点才清空。</summary><param name="node">节点。</param>
    private bool ReceiveEvent(XElement node)
    {
        if (Event.Name.Length == 0) return false;
        strings[Key(node, "store_event")] = Event.Name; ints[Key(node, "store_int_arg")] = Event.IntArg;
        floats[Key(node, "store_float_arg")] = Event.FloatArg; floats[Key(node, "store_face_degree")] = Event.Face;
        ids[Key(node, "store_uuid")] = Event.SenderId; ids[Key(node, "store_transform")] = Event.SenderId;
        vectors[Key(node, "store_position")] = Event.Position; return true;
    }
}
