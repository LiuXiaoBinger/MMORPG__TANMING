using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;

/// <summary>执行第18波实际导出的 StandBy 主树与它引用的追近子树。树定义共享，黑板仅属于一只兽人。</summary>
internal sealed class OrcStandByTree
{
    /// <summary>实体表索引 1 所指向的 RO 服务端主树文件名。</summary>
    internal const string MainName = "Dungeons_DaNaTuoSiZhiZhan_StandBy";
    /// <summary>主树的入口、节点参数和 UnityObjects 引用表。</summary>
    private static readonly XElement main = Load(MainName);
    /// <summary>主树的外部引用表；指针次序就是 BehaviorTreeReference 的 externalBehaviors 下标。</summary>
    private static readonly XElement[] subtrees = main.Element("UnityObjects").Elements("pointer")
        .Select(pointer => Load((string)pointer.Attribute("fileName"))).ToArray();
    /// <summary>本次决策操作的权威怪物。</summary>
    private readonly MoshentaOrc actor;
    /// <summary>当前实体行为状态的 SharedString 槽。</summary>
    private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
    /// <summary>距离、技能射程、接近容差等 SharedFloat 槽，单位米。</summary>
    private readonly Dictionary<string, float> numbers = new Dictionary<string, float>();
    /// <summary>仇恨节点取得的同实例在线玩家。</summary>
    private readonly Dictionary<string, OnlineRole> targets = new Dictionary<string, OnlineRole>();
    /// <summary>从目标槽取得的玩家编号或宿主怪物 UID。</summary>
    private readonly Dictionary<string, ulong> ids = new Dictionary<string, ulong>();
    /// <summary>每个技能组的筛选结果，5515 只有组 0 有一个候选。</summary>
    private readonly Dictionary<string, int> skills = new Dictionary<string, int>();
    /// <summary>追击子树写入的站位或目标位置，世界米制坐标。</summary>
    private readonly Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();

    /// <summary>准备实例黑板，运行时攻击距离来自这只怪的普攻。</summary>
    /// <param name="owner">绑定的第18波怪物。</param>
    internal OrcStandByTree(MoshentaOrc owner)
    {
        actor = owner;
        numbers["min_attack_range"] = MoshentaOrcDefinition.Range;
        numbers["tolerance_range"] = 0.5f;
    }

    /// <summary>从游戏服程序集读取原样发布的行为树 XML。</summary>
    /// <param name="name">RO 导出树的文件主名。</param>
    /// <returns>包含 EntryTask 和引用表的 XML 根。</returns>
    private static XElement Load(string name)
    {
        using (Stream xml = typeof(OrcStandByTree).Assembly.GetManifestResourceStream("Moshenta.AI." + name + ".xml"))
            return XElement.Load(xml);
    }

    /// <summary>按 XML 中声明的父子关系和兄弟顺序跑一次主树。</summary>
    /// <param name="notice">当前实例待广播的本次动作。</param>
    internal void Tick(MonsterAttrUpdateNtf notice)
    {
        Run(main.Element("EntryTask"), notice);
    }

    /// <summary>获取 Shared 参数绑定的黑板键；空键表示使用节点常量。</summary>
    /// <param name="node">当前 XML 节点。</param>
    /// <param name="field">导出参数名。</param>
    private static string Key(XElement node, string field)
    {
        return (string)node.Attribute("Shared_" + field + "Name");
    }

    /// <summary>读取节点导出的字面常量。</summary>
    /// <param name="node">当前 XML 节点。</param>
    /// <param name="field">导出参数名。</param>
    private static string Literal(XElement node, string field)
    {
        return (string)node.Attribute("Shared_" + field + "mValue");
    }

    /// <summary>按节点类型执行真实叶子，组合节点使用布尔返回值短路。</summary>
    /// <param name="node">本次递归执行的树节点。</param>
    /// <param name="notice">该逻辑步待下发的动作。</param>
    /// <returns>与 RO 节点 Update 一致的成功或失败结果。</returns>
    private bool Run(XElement node, MonsterAttrUpdateNtf notice)
    {
        switch (node.Name.LocalName)
        {
            case "EntryTask":
            case "Sequence":
                // 当前顺序节点的子项，首个失败即终止序列。
                for (XElement child = node.FirstNode as XElement; child != null; child = child.NextNode as XElement)
                    if (!Run(child, notice)) return false;
                return true;
            case "Selector":
                // 当前选择节点的子项，首个成功即停止选择。
                for (XElement child = node.FirstNode as XElement; child != null; child = child.NextNode as XElement)
                    if (Run(child, notice)) return true;
                return false;
            case "ReturnSuccess":
                Run((XElement)node.FirstNode, notice);
                return true;
            case "BehaviorTreeReference":
                return Run(subtrees[(int)node.Attribute("externalBehaviors")].Element("EntryTask"), notice);
            case "AIGetCurrentState":
                strings[Key(node, "store_result")] = actor.CurrentState;
                return true;
            case "AIStringEqual":
                return strings[Key(node, "string1")] == Literal(node, "string2");
            case "AIPatrolWhthPath":
                return actor.Patrol(notice);
            case "AIFindTargetByHatred":
                if (actor.Target == null) return false;
                targets[Key(node, "store_target")] = actor.Target;
                numbers[Key(node, "store_target_distance")] = actor.DistanceTo(actor.Target);
                return true;
            case "AIGetTargetUUID":
                // 与 RO 一致，直接目标为空时取宿主的 UID；正常无仇恨时上游节点已先行退出。
                OnlineRole direct;
                // 读取有效玩家时覆盖为该玩家身份，否则保留宿主 UID。
                ulong uuid = actor.UnitId;
                if (targets.TryGetValue(Key(node, "direct_target"), out direct) && direct != null) uuid = direct.GetID();
                ids[Key(node, "store_uuid")] = uuid;
                return true;
            case "AIFilterGroupSkill":
                // 兽人在实体表中仅配置一个组0普攻。另一分支的组1照原树返回失败。
                int group = int.Parse(Literal(node, "group_id"), CultureInfo.InvariantCulture);
                // 本次筛选出的唯一技能编号，零表示该组没有可用候选。
                int candidate = actor.FilterSkill(group, ids[Key(node, "target_uuid")]);
                skills[Key(node, "store_skills")] = candidate;
                return candidate != 0;
            case "AICastSkillByList":
                return actor.Attack(skills[Key(node, "store_skills")], ids[Key(node, "target_uuid")], notice);
            case "AIFloatComparison" when (string)node.Attribute("cmp_type") == "kCmpTypeGreaterTo":
                return numbers[Key(node, "float1")] > numbers[Key(node, "float2")] + 0.000001f;
            case "AIFloatOperator" when (string)node.Attribute("oper_type") == "kOperTypeSubtract":
                numbers[Key(node, "store_result")] = numbers[Key(node, "float1")] - numbers[Key(node, "float2")];
                return true;
            case "AIGetTargetFightPosition":
                // 有效的近战站位，无法寻路时让选择节点改走玩家当前位置。
                Vector3 dest;
                if (!actor.FightPosition(targets[Key(node, "fight_target")], numbers[Key(node, "distance")], out dest)) return false;
                positions[Key(node, "store_pos")] = dest;
                return true;
            case "AIGetPostion":
                if (Literal(node, "is_self") == "1") positions[Key(node, "store_postion")] = actor.Position;
                else positions[Key(node, "store_postion")] = targets[Key(node, "target")].mainRoleInfo.BaseInfo.Pos.ToVector3();
                return true;
            case "AIActionMove":
                return actor.Chase(positions[Key(node, "move_dest")], notice);
            default:
                throw new InvalidDataException("第18波不支持的 RO 行为树节点 " + node.Name.LocalName);
        }
    }
}
