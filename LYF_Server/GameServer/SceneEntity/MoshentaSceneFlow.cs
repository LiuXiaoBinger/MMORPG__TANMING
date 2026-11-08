using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;

/// <summary>每个副本独立消费原 RO 场景图中的区域、波次、物件及任务节点。</summary>
public sealed class MoshentaSceneFlow
{
    /// <summary>正式 XML 同时嵌入游戏服并供客户端与编辑器读取。</summary>
    internal readonly XDocument Config;
    /// <summary>波次定义按原编号定位，不另存出生配置。</summary>
    private readonly Dictionary<int, XElement> Waves;
    /// <summary>触发物件的位置、朝向与碰撞盒均来自原 XML。</summary>
    private readonly Dictionary<int, XElement> Objects;
    /// <summary>画布数据引用表，仅用于原 XML 反序列化边界。</summary>
    private readonly Dictionary<string, XElement> Data;
    /// <summary>画布节点与端口，按原连接顺序运行结果。</summary>
    private readonly XElement[] Nodes;
    /// <summary>每个端口所属的原画布节点。</summary>
    private readonly Dictionary<string, XElement> Ports;
    /// <summary>原节点之间的连线，不建立另一套手写图。</summary>
    private readonly XElement[] Connections;
    /// <summary>当前副本所选Luban目标阶段和每个目标的实例计数。</summary>
    private readonly DungeonTargetProgress targets;
    /// <summary>原SceneAI转发规则；字段从该场景的原AI文件逐条提取。</summary>
    private readonly XElement[] sceneEvents;
    /// <summary>仅保存所移植分支实际使用的原AI布尔变量；没有原条件的分支不增加一次性状态。</summary>
    private readonly Dictionary<string, bool> sceneAiFlags;
    /// <summary>拥有实体世界的实例宿主。</summary>
    private readonly SceneWaveSpawner owner;
    /// <summary>已激活波次，保留到重置用于去重及晚加入快照。</summary>
    private readonly HashSet<int> active = new HashSet<int>();
    /// <summary>已经清空或采集完成的波次。</summary>
    private readonly HashSet<int> complete = new HashSet<int>();
    /// <summary>实际完成事实保留到实例重置，供尸体移除后的原清波节点查询。</summary><param name="wave">原波次号。</param>
    internal bool IsComplete(int wave) { return complete.Contains(wave); }
    /// <summary>已发生的波次事件，只用于出生依赖，不缓存副本目标事件。</summary>
    private readonly HashSet<string> events = new HashSet<string>();
    /// <summary>当前实际存在的场景物件。</summary>
    private readonly HashSet<int> objects = new HashSet<int>();
    /// <summary>当前需要验证移动线段的空气墙。</summary>
    private readonly HashSet<int> walls = new HashSet<int>();
    /// <summary>区域触发器的画布节点身份，同号旧触发器不会相互覆盖。</summary>
    private readonly HashSet<string> entered = new HashSet<string>();
    /// <summary>是否存在尚未广播的流程变化。</summary>
    internal bool Dirty { get; private set; }

    /// <summary>为实例建立初始物件并只激活配置允许的初始波次。</summary>
    /// <param name="owner">实例真实怪物世界。</param>
    public MoshentaSceneFlow(SceneWaveSpawner owner)
    {
        this.owner = owner;
        Config = owner.Config;
        Waves = Config.Descendants("MSceneWaveData").ToDictionary(row => (int)row.Element("base_info").Element("id"));
        Objects = Config.Descendants("MSceneTriggerObjectData").ToDictionary(row => (int)row.Element("id"));
        Data = Config.Root.Element("canvas").Element("Objects").Elements().ToDictionary(row => (string)row.Attribute("refID"), row => row.Elements().First());
        Nodes = Config.Root.Element("canvas").Element("Nodes").Elements().ToArray();
        Ports = Nodes.SelectMany(node => node.Element("Ports").Elements().Select(port => new KeyValuePair<string, XElement>((string)port.Attribute("ID"), node))).ToDictionary(pair => pair.Key, pair => pair.Value);
        Connections = Config.Root.Element("canvas").Element("Connections").Elements().ToArray();
        targets = new DungeonTargetProgress(owner.Dungeon);
        sceneEvents = Config.Root.Element("data").Element("scene_ai_events").Elements("event").ToArray();
        sceneAiFlags = Config.Root.Element("data").Element("scene_ai_events").Element("variables").Elements("bool")
            .ToDictionary(row => (string)row.Attribute("name"), row => (bool)row.Attribute("value"));
        // 原对象列表中的默认出生标记决定初始存在状态。
        XElement[] rows = Objects.Values.ToArray();
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < rows.Length; index++)
            if (!(bool)rows[index].Element("ignore_init")) objects.Add((int)rows[index].Element("id"));
        Advance();
    }

    /// <summary>当前实例入口的米制向量，普通与困难配置各自提供出生点。</summary>
    public Vector3 SpawnPosition { get { return Position(Config.Root.Element("data").Element("player_spawn").Element("position")); } }

    /// <summary>将 XML 坐标转换为游戏服向量。</summary>
    /// <param name="row">包含 x/y/z 的 XML 坐标。</param>
    internal static Vector3 Position(XElement row)
    {
        return new Vector3((float)row.Element("x"), (float)row.Element("y"), (float)row.Element("z"));
    }

    /// <summary>跨端沿用角色位置下划线格式。</summary>
    /// <param name="position">世界坐标，米。</param>
    internal static string Format(Vector3 position)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", position.X, position.Y, position.Z);
    }

    /// <summary>按画布变量引用取得真实配置。</summary>
    /// <param name="node">原画布节点。</param>
    private XElement NodeData(XElement node)
    {
        // 实体波次使用 data，其余服务端触发节点使用 realData。
        XElement variable = node.Element("Variables").Elements("Variable").FirstOrDefault(row => (string)row.Attribute("name") == "realData");
        if (variable == null) return null;
        return Data[(string)variable.Attribute("refID")];
    }

    /// <summary>沿指定端口读取相邻节点，保留配置连线顺序。</summary>
    /// <param name="node">端口宿主节点。</param>
    /// <param name="name">原端口名。</param>
    private List<XElement> Linked(XElement node, string name)
    {
        // 每次仅展开需要消费的原端口。
        List<XElement> result = new List<XElement>();
        // 端口可能不存在，例如不带条件的结果节点。
        XElement port = node.Element("Ports").Elements().FirstOrDefault(row => (string)row.Attribute("name") == name);
        if (port == null) return result;
        // XML 的端口标识是字符串形式的有符号64位数字。
        string id = (string)port.Attribute("ID");
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < Connections.Length; index++)
        {
            // 同一条连接任意一端都能解析到另一端宿主。
            XElement link = Connections[index];
            if ((string)link.Attribute("port1ID") == id) result.Add(Ports[(string)link.Attribute("port2ID")]);
            else if ((string)link.Attribute("port2ID") == id) result.Add(Ports[(string)link.Attribute("port1ID")]);
        }
        return result;
    }

    /// <summary>事件仅首次推进波次，重复区域上报不会重复出生。</summary>
    /// <param name="name">原 WaveEvent 或波次结束事件。</param>
    internal void Event(string name)
    {
        if (string.IsNullOrEmpty(name) || !events.Add(name)) return;
        Dirty = true;
        SceneAIEvent(name);
        Advance();
    }

    /// <summary>原AIWaveEvent只进入波次系统，不递归重新触发SceneAI。</summary><param name="name">原波次事件名。</param>
    internal bool RecordWaveEvent(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (events.Add(name)) { Dirty = true; Advance(); }
        return true;
    }

    /// <summary>实际怪物全灭或采集完成后推进依赖。</summary>
    /// <param name="id">本实例已出生的波次编号。</param>
    internal void Complete(int id)
    {
        if (!active.Contains(id) || !complete.Add(id)) return;
        Dirty = true;
        targets.WaveFinished(id);
        Event((string)Waves[id].Element("base_info").Element("end_scene_ai_event"));
        Advance();
    }

    /// <summary>按所选场景原AI的事件名与清波条件转发目标和出生事件。</summary>
    /// <param name="name">服务端场景图或实体死亡产生的真实事件名。</param>
    private void SceneAIEvent(string name)
    {
        if (owner.IsMoshenta) { owner.SceneEvent(name); return; }
        // 原场景AI分支顺序保存在正式XML中；目标ID不再写入C#分支。
        for (int index = 0; index < sceneEvents.Length; index++)
        {
            // 当前原AI序列的输入事件与所需清空波次。
            XElement rule = sceneEvents[index];
            if ((string)rule.Attribute("name") != name) continue;
            // 清波分支的前置只接受已出生且真正完成的波次。
            XElement[] required = rule.Elements("require_wave").ToArray();
            // 只有本条规则引用的所有实际波次都完成才执行原AI结果。
            bool ready = true;
            for (int wave = 0; wave < required.Length; wave++)
                if (!complete.Contains((int)required[wave].Attribute("id"))) ready = false;
            // 北森的出生/完成标记按原AIBoolComparison判断，魔神塔143/141没有这些条件。
            XElement[] conditions = rule.Elements("require_bool").ToArray();
            // 当前规则的每个原布尔前置必须与该实例的黑板一致。
            for (int condition = 0; condition < conditions.Length; condition++)
                if (sceneAiFlags[(string)conditions[condition].Attribute("name")] != (bool)conditions[condition].Attribute("value")) ready = false;
            if (!ready) continue;
            // 按原Sequence顺序保留副本目标通知和WaveEvent，不移植无关Boss/对白AI。
            XElement[] results = rule.Elements().ToArray();
            for (int result = 0; result < results.Length; result++)
            {
                if (results[result].Name.LocalName == "target") DungeonTargetEvent("dungeonfinishtarget", (int)results[result].Attribute("id"));
                if (results[result].Name.LocalName == "wave") Event((string)results[result].Attribute("name"));
                if (results[result].Name.LocalName == "set_bool") sceneAiFlags[(string)results[result].Attribute("name")] = (bool)results[result].Attribute("value");
            }
            // 原Selector在首个成功分支结束；目标暂不属于当前Section也不继续执行后续分支。
            return;
        }
    }

    /// <summary>转发RO特殊目标事件，由当前实例Section按原目标ID匹配。</summary>
    /// <param name="name">原事件名。</param>
    /// <param name="targetId">原AI事件int_arg携带的目标表行号。</param>
    internal void DungeonTargetEvent(string name, int targetId)
    {
        if (targets.SceneEvent(name, targetId)) Dirty = true;
    }

    /// <summary>单个实体真实死亡时统计击杀目标，清波完成后再调用原SceneAI。</summary>
    /// <param name="entityId">死亡实体的原EntityTable编号。</param>
    internal void CountMonsterDeath(int entityId)
    {
        if (targets.MonsterDied(entityId)) Dirty = true;
    }

    /// <summary>真实死亡结算并完成清波后消费原monster_die场景AI事件。</summary>
    internal void MonsterDied() { SceneAIEvent("monster_die"); }

    /// <summary>保留原NPC脚本事件接入边界；本项目尚无NPC脚本执行器，不由客户端直接调用。</summary>
    /// <param name="name">RO对话或分支选择事件名。</param>
    /// <param name="npcId">执行脚本的NPC表编号。</param>
    /// <param name="branchId">原脚本选择的目标组编号。</param>
    internal void NpcScript(string name, int npcId, int branchId)
    {
        if (targets.NpcScript(name, npcId, branchId)) Dirty = true;
    }

    /// <summary>从原前置条件解析当前可出生波次；触发组波次立即执行其 WaveEvent 结果。</summary>
    private void Advance()
    {
        // 本轮检查的静态波次列表；激活集合变化不改变列表。
        XElement[] rows = Waves.Values.ToArray();
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < rows.Length; index++)
        {
            // 波次运行入口与前置条件均使用原配置。
            XElement wave = rows[index];
            // 本波的出生类型、前置事件与激活标记。
            XElement info = wave.Element("base_info");
            // 原波次编号，跨端用于实例内出生身份。
            int id = (int)info.Element("id");
            if (active.Contains(id) || (bool)info.Element("not_active_create")) continue;
            // None、WaitEvent、And 为本轮流程实际使用的前置类型。
            string option = (string)info.Element("pre_wave_option");
            // 本次检查是否已满足原波次前置条件。
            bool ready = option == "PreWaveOptionTypeNone";
            if (option == "PreWaveOptionTypeWaitEvent") ready = events.Contains((string)info.Element("pre_scene_ai_event"));
            if (option == "PreWaveOptionTypeAnd")
            {
                // 每个前置必须先实际激活并完成，空前置不自动通过。
                XElement[] requirements = info.Element("pre_wave_list").Elements().ToArray();
                ready = requirements.Length > 0;
                // 当前配置集合的零基下标，仅遍历本次快照长度。
                for (int item = 0; item < requirements.Length; item++)
                    if (!complete.Contains((int)requirements[item].Element("pre_wave_id"))) ready = false;
            }
            if (!ready) continue;
            active.Add(id);
            Dirty = true;
            owner.ActivateWave(id);
            if (wave.Element("trigger_group_event_info") != null)
            {
                RunGroup((int)wave.Element("trigger_group_event_info").Element("group_id"));
                Complete(id);
            }
        }
    }

    /// <summary>执行波次选中的服务端触发组，仅消费其中 WaveEvent 时机。</summary>
    /// <param name="id">原服务端触发组号。</param>
    private void RunGroup(int id) { SendTriggerGroup(id, -1); }

    /// <summary>原AI触发组按组号/触发器号派发AIEvent，波次入口只派WaveEvent。</summary>
    /// <param name="id">原组编号。</param><param name="triggerId">原触发器编号，波次调用为-1。</param>
    internal bool SendTriggerGroup(int id, int triggerId)
    {
        bool executed = false;
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < Nodes.Length; index++)
        {
            // 组数据和组端口来自原画布。
            XElement data = NodeData(Nodes[index]);
            if (data == null || data.Name.LocalName != "MSSceneTriggerGroupData") continue;
            if ((int)data.Element("id") != id) continue;
            // 组的输出端口名由原图定义。
            XElement[] ports = Nodes[index].Element("Ports").Elements().ToArray();
            // 当前配置集合的零基下标，仅遍历本次快照长度。
            for (int port = 0; port < ports.Length; port++)
            {
                // 连接到该组的触发器列表。
                List<XElement> triggers = Linked(Nodes[index], (string)ports[port].Attribute("name"));
                // 当前配置集合的零基下标，仅遍历本次快照长度。
                for (int trigger = 0; trigger < triggers.Count; trigger++)
                {
                    // 只执行有 WaveEvent 时机的触发器；PlayerButton 出口仍等待玩家点击。
                    List<XElement> timings = Linked(triggers[trigger], "timingKnob");
                    // 当前配置集合的零基下标，仅遍历本次快照长度。
                    for (int timing = 0; timing < timings.Count; timing++)
                    {
                        // 当前组内时机的原始 XML 数据。
                        XElement condition = NodeData(timings[timing]);
                        if (condition == null) continue;
                        string expected = "SSttimingTypeAIEvent";
                        if (triggerId < 0) expected = "SSttimingTypeWaveEvent";
                        if ((string)condition.Element("timing_type") != expected) continue;
                        XElement triggerData = NodeData(triggers[trigger]);
                        if (triggerId > 0 && (int?)triggerData?.Element("id") != triggerId) continue;
                        RunResults(triggers[trigger], null); executed = true;
                    }
                }
            }
        }
        return executed;
    }

    /// <summary>消费已确认时机连接的结果，本轮只执行原图已有的流程节点。</summary>
    /// <param name="trigger">原触发器节点。</param>
    /// <param name="reply">玩家交互时的位置结果，自动时机为 null。</param>
    private void RunResults(XElement trigger, DungeonInteractRet reply)
    {
        // 结果按原连线顺序执行，节点字段为强含义 XML 反序列化边界。
        List<XElement> results = Linked(trigger, "resultKnob");
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < results.Count; index++)
        {
            // 当前结果节点的原始字段。
            XElement row = NodeData(results[index]);
            if (row == null || row.Name.LocalName != "MSSceneTriggerResultData") continue;
            // 物件编号零也合法，不以默认值判定缺失。
            int id = (int?)row.Element("scene_trigger_obj_id") ?? 0;
            switch ((string)row.Element("result_type"))
            {
                case "SSTResultTypeWaveEvent": Event((string)row.Element("wave_event_info").Element("wave_event")); break;
                case "SSTResultTypeSceneAIEvent": SceneAIEvent((string)row.Element("scene_ai_event").Element("ai_event")); break;
                case "SSTResultTypeCreateObj": objects.Add(id); Dirty = true; break;
                case "SSTResultTypeDestroyObj": objects.Remove(id); walls.Remove(id); Dirty = true; break;
                case "SSTResultTypeAirWall": objects.Add(id); walls.Add(id); Dirty = true; break;
                case "SSTResultTypeTransPosition":
                    if (reply != null) reply.Pos = Format(Position(row.Element("trans_position_info").Element("position")));
                    break;
                case "SSTResultTypeResetDungeons":
                    // RO 此节点仅通知触发实体的 Boss AI 和同视层采集表现刷新，不重置整本。
                    // 本轮尚未移植 Boss AI/视层采集刷新组件；实例全灭由 TeamManager.Reset 独立处理。
                    break;
                case "SSTResultTypeDungeonsEnd":
                    if (reply != null) { reply.Exited = true; Event("DungeonsEnd"); }
                    break;
            }
        }
    }

    /// <summary>只有通过服务端位置验证的玩家能运行按钮结果或完成采集。</summary>
    /// <param name="request">原客户端对象/采集编号。</param>
    /// <param name="position">已保存的服务端米制位置。</param>
    /// <param name="inFight">角色是否仍被本实例怪物追击或正在施法。</param>
    public DungeonInteractRet Interact(DungeonInteractReq request, Vector3 position, bool inFight)
    {
        // 失败默认保留当前权威位置供客户端校正。
        DungeonInteractRet reply = new DungeonInteractRet { Pos = Format(position), Reason = "物件未解锁或距离过远" };
        if (request.WaveId > 0)
        {
            // 采集仅允许已激活且未完成的配置波次。
            XElement wave;
            if (!active.Contains(request.WaveId) || complete.Contains(request.WaveId)) return reply;
            if (!Waves.TryGetValue(request.WaveId, out wave) || wave.Element("collection_info") == null) return reply;
            // 采集目标和完成事件共用原波次配置。
            XElement collection = wave.Element("collection_info");
            // 本波采集点世界坐标，交互范围为四米。
            Vector3 target = Position(collection.Descendants("MScenePositionData").First().Element("position"));
            if (Vector3.DistanceSquared(target, position) > 16f) return reply;
            if (targets.Collected((int)collection.Element("collection_id"))) Dirty = true;
            Complete(request.WaveId);
            Event((string)collection.Element("collect_end_ai_event"));
            reply.Success = true;
        }
        else
        {
            // 原物件存在、玩家处于其交互盒附近，才检查按钮时机。
            if (!objects.Contains(request.ObjectId) || !Near(request.ObjectId, position, 2f)) return reply;
            reply.Success = Trigger("SSttimingTypePlayerButton", request.ObjectId, reply, inFight);
        }
        if (reply.Success) reply.Reason = "";
        return reply;
    }

    /// <summary>位置被正式移动组件接纳后检查区域时机。</summary>
    /// <param name="position">服务端实际米制位置。</param>
    internal void Enter(Vector3 position)
    {
        // 自动区域仅对存在的原物件执行，物件可能在结果执行时改变集合。
        int[] ids = objects.ToArray();
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < ids.Length; index++)
            if (Near(ids[index], position, 0.5f))
            {
                if (targets.EnterTrigger(owner.Dungeon.SceneID, ids[index])) Dirty = true;
                Trigger("SSttimingTypeServerEnter", ids[index], null, false);
            }
    }

    /// <summary>触发匹配物件的时机，并遵循触发次数与原战斗条件。</summary>
    /// <param name="kind">原服务端时机名称。</param>
    /// <param name="id">物件编号。</param>
    /// <param name="reply">按钮的传送结果，自动区域为空。</param>
    /// <param name="inFight">本实例已确认的战斗状态。</param>
    private bool Trigger(string kind, int id, DungeonInteractRet reply, bool inFight)
    {
        // 有实际支持的时机才将交互标记成功。
        bool executed = false;
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < Nodes.Length; index++)
        {
            // 当前画布节点的时机定义。
            XElement timing = NodeData(Nodes[index]);
            if (timing == null || (string)timing.Element("timing_type") != kind) continue;
            if ((int?)timing.Element("scene_trigger_obj_id") != id) continue;
            // 原战斗条件的 valid_state=12 表示禁止处于战斗，其他条件本轮不执行。
            List<XElement> conditions = Linked(Nodes[index], "conditionKnob");
            // 所有已连接条件通过时才可执行按钮。
            bool allowed = true;
            // 当前配置集合的零基下标，仅遍历本次快照长度。
            for (int item = 0; item < conditions.Count; item++)
            {
                // 原时机连接的单条条件定义。
                XElement condition = NodeData(conditions[item]);
                if ((string)condition.Element("condition_type") != "SSTConditionTypeIsInFight") allowed = false;
                else if (inFight) allowed = false;
            }
            if (!allowed) continue;
            // 匹配时机连接的触发器节点列表。
            List<XElement> triggers = Linked(Nodes[index], "inKnob");
            // 当前配置集合的零基下标，仅遍历本次快照长度。
            for (int item = 0; item < triggers.Count; item++)
            {
                // 触发器定义，用于过滤非服务端触发节点。
                XElement data = NodeData(triggers[item]);
                if (data == null || data.Name.LocalName != "MSSceneTriggerData") continue;
                // 区域事件只执行一次；PlayerButton 按配置允许重复传送。
                string identity = (string)triggers[item].Attribute("ID");
                if (kind == "SSttimingTypeServerEnter" && !entered.Add(identity)) continue;
                RunResults(triggers[item], reply);
                executed = true;
            }
        }
        return executed;
    }

    /// <summary>把世界坐标转为原物件旋转盒的局部坐标。</summary>
    /// <param name="row">原场景物件。</param>
    /// <param name="position">世界坐标，米。</param>
    private static Vector3 Local(XElement row, Vector3 position)
    {
        // 绕Y轴的反向旋转，与Unity BoxCollider一致。
        float angle = -(float)row.Element("face") * (float)Math.PI / 180f;
        // 从物件原点到玩家的世界位移，米。
        Vector3 delta = position - Position(row.Element("position"));
        return Vector3.Transform(delta, Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle)) - Position(row.Element("box").Element("offset"));
    }

    /// <summary>交互使用物件原始盒体及少量角色半径容差。</summary>
    /// <param name="id">物件配置号。</param>
    /// <param name="position">权威位置。</param>
    /// <param name="margin">距离容差，米。</param>
    private bool Near(int id, Vector3 position, float margin)
    {
        // 当前物件的原碰撞盒配置。
        XElement row = Objects[id];
        if (row.Element("box") == null) return false;
        // 玩家在物件局部坐标的绝对距离。
        Vector3 local = Vector3.Abs(Local(row, position));
        // 交互盒的半尺寸，叠加角色操作容差。
        Vector3 half = Position(row.Element("box").Element("scale")) / 2f + new Vector3(margin);
        return local.X <= half.X && local.Y <= half.Y && local.Z <= half.Z;
    }

    /// <summary>验证整条移动线段与活动空气墙，阻止一次大步跨越薄墙。</summary>
    /// <param name="start">已保存的权威起点。</param>
    /// <param name="end">本次上报终点。</param>
    public bool Blocks(Vector3 start, Vector3 end)
    {
        // 墙编号快照，遍历期间不依赖集合枚举。
        int[] ids = walls.ToArray();
        // 当前配置集合的零基下标，仅遍历本次快照长度。
        for (int index = 0; index < ids.Length; index++)
        {
            // 当前活动墙的原形状与朝向。
            XElement row = Objects[ids[index]];
            // 移动起点在墙盒内的局部米制坐标。
            Vector3 from = Local(row, start);
            // 本次线段在墙盒局部坐标中的位移。
            Vector3 delta = Local(row, end) - from;
            // 墙盒半尺寸加角色半径，单位米。
            Vector3 half = Position(row.Element("box").Element("scale")) / 2f + new Vector3(0.3f);
            // slab交集参数限制于[0,1]，按三个轴逐一裁切。
            float lower = 0f;
            // 线段离开盒体的最大参数，初值为终点。
            float upper = 1f;
            if (!Slab(from.X, delta.X, half.X, ref lower, ref upper)) continue;
            if (!Slab(from.Y, delta.Y, half.Y, ref lower, ref upper)) continue;
            if (Slab(from.Z, delta.Z, half.Z, ref lower, ref upper)) return true;
        }
        return false;
    }

    /// <summary>裁切线段在单轴盒体内的参数区间。</summary>
    /// <param name="start">轴起点米数。</param>
    /// <param name="delta">轴位移米数。</param>
    /// <param name="half">轴半尺寸米数。</param>
    /// <param name="lower">进入参数。</param>
    /// <param name="upper">离开参数。</param>
    private static bool Slab(float start, float delta, float half, ref float lower, ref float upper)
    {
        if (Math.Abs(delta) < 0.00001f) return Math.Abs(start) <= half;
        // 线段与负向盒面的交点参数。
        float first = (-half - start) / delta;
        // 线段与正向盒面的交点参数。
        float last = (half - start) / delta;
        lower = Math.Max(lower, Math.Min(first, last));
        upper = Math.Min(upper, Math.Max(first, last));
        return lower <= upper;
    }

    /// <summary>生成完整实例快照，目标只同步RO当前Section的表定义与实例进度。</summary>
    public DungeonFlowState Snapshot()
    {
        // 对外传输的独立快照，不暴露内部集合。
        bool finished = events.Contains("FinishDungeon");
        if (owner.IsMoshenta) finished = owner.Victory;
        DungeonFlowState result = new DungeonFlowState { Finished = finished };
        result.ActiveWaveIds.Add(active.OrderBy(id => id));
        result.CompletedWaveIds.Add(complete.OrderBy(id => id));
        result.ActiveObjectIds.Add(objects.OrderBy(id => id));
        result.ActiveWallIds.Add(walls.OrderBy(id => id));
        targets.FillSnapshot(result);
        return result;
    }

    /// <summary>最后成员离开或全灭重建前释放所有流程运行状态。</summary>
    internal void Clear()
    {
        active.Clear();
        complete.Clear();
        events.Clear();
        objects.Clear();
        walls.Clear();
        entered.Clear();
        sceneAiFlags.Clear();
        Dirty = true;
    }

    /// <summary>本次广播完成后清除变更标记。</summary>
    internal void Acknowledge() { Dirty = false; }
}
