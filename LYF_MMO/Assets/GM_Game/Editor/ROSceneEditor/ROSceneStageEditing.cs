using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

/// <summary>阶段工作台的原 XML 编辑操作；复用已有出生条件和触发链，不执行游戏逻辑。</summary>
public static class ROSceneStageEditing
{
    /// <summary>按稳定画布编号取得重建后的节点，避免复制或连线后继续使用旧宿主引用。</summary>
    /// <param name="config">当前正在编辑的原配置。</param>
    /// <param name="id">原画布 Node 的 ID。</param>
    public static MSceneEditorNode FindNode(MSceneEditorConfig config, string id)
    {
        // 每次 Rebuild 都重新创建宿主，XML 编号保持不变。
        for (int index = 0; index < config.Nodes.Count; index++)
            if (config.Nodes[index].Id == id) return config.Nodes[index];
        return null;
    }

    /// <summary>说明真实波次的出生门槛，用于让策划选择当前阶段已有启动来源。</summary>
    /// <param name="wave">提供 base_info 的原波次。</param>
    public static string SpawnDescription(MSceneEditorNode wave)
    {
        // 保留原枚举对应的实际语义，不把未激活波次说成自动出生。
        string option = ROSceneXml.Text(wave.Data, "base_info/pre_wave_option");
        // 当前波次是否由其他显式创建入口控制。
        string prefix = "";
        if (ROSceneXml.Text(wave.Data, "base_info/not_active_create") == "true") prefix = "禁止自动创建；";
        if (option == "PreWaveOptionTypeNone") return prefix + "角色进入时出现";
        if (option == "PreWaveOptionTypeWaitEvent") return prefix + "等待波次事件：" + ROSceneXml.Text(wave.Data, "base_info/pre_scene_ai_event");
        // 列表中的比例阈值必须与原连接同时保留。
        XElement list = ROSceneXml.Find(wave.Data, "base_info/pre_wave_list");
        // 文案只列真实编号，详细阈值在原字段面板中保留。
        List<string> ids = new List<string>();
        if (list != null)
        {
            // XML 数组允许后续编辑仍引用原对象。
            XElement[] rows = list.Elements().ToArray();
            // 原前置列表顺序与连接顺序一致。
            for (int index = 0; index < rows.Length; index++) ids.Add(ROSceneXml.Text(rows[index], "pre_wave_id"));
        }
        if (option == "PreWaveOptionTypeAnd") return prefix + "全部前置波次满足：" + string.Join("、", ids);
        if (option == "PreWaveOptionTypeOr") return prefix + "任一前置波次满足：" + string.Join("、", ids);
        return prefix + "原出生条件：" + option;
    }

    /// <summary>复制波次及其真实输入连接，保持事件、前置阈值和未知字段。</summary>
    /// <param name="config">接收新波次的当前配置。</param>
    /// <param name="source">已确认归属当前阶段的波次模板。</param>
    public static MSceneEditorNode DuplicateWave(MSceneEditorConfig config, MSceneEditorNode source)
    {
        // 原连接在 Rebuild 后仍可按端口 XML 编号追溯。
        List<MSceneEditorConnection> links = new List<MSceneEditorConnection>(config.Connections);
        // DuplicateNode 会清空前置字段，因此先保留原比例阈值。
        XElement previous = new XElement(ROSceneXml.Find(source.Data, "base_info/pre_wave_list"));
        // 复制原怪物、刷点、出生设置及未知字段，只重新分配节点和波次身份。
        MSceneEditorNode copy = config.DuplicateNode(source);
        // 原节点到副本的编号映射用于重建真实输入连接。
        Dictionary<string, string> copies = new Dictionary<string, string> { { source.Id, copy.Id } };
        CopyConnections(config, links, copies, source.Id);
        copy = FindNode(config, copy.Id);
        ROSceneXml.Find(copy.Data, "base_info/pre_wave_list").ReplaceWith(previous);
        copy.Content = source.Content + "（复制）";
        config.IsDirty = true;
        return copy;
    }

    /// <summary>把已有波次改接到用户选择的真实出生来源，同时保留其怪物、刷点和输出行为。</summary>
    /// <param name="config">当前正在编辑的配置。</param>
    /// <param name="wave">需要改变出生条件的现有波次。</param>
    /// <param name="source">提供真实事件或前置门槛的波次。</param>
    public static MSceneEditorNode UseWaveStart(MSceneEditorConfig config, MSceneEditorNode wave, MSceneEditorNode source)
    {
        if (wave.Id == source.Id) return wave;
        // 原连接接口拒绝自连，提前拒绝此真实无效来源，避免保存时静默丢失其中一项条件。
        string blocked = WaveStartBlockReason(wave, source);
        if (blocked.Length > 0) throw new InvalidOperationException(blocked);
        // 源前置及其阈值在任何连接编辑前保存，避免同步列表改变待拷贝值。
        XElement previous = new XElement(ROSceneXml.Find(source.Data, "base_info/pre_wave_list"));
        // 仅复制来源的前置波次输入，不复制其下游或跟随行为。
        List<MSceneEditorConnection> inputs = new List<MSceneEditorConnection>();
        // 当前波次原前置连接由用户明确选择新来源后移除。
        List<MSceneEditorConnection> old = new List<MSceneEditorConnection>();
        // 原连线可能以任一端保存输入，分别检查以保持条件清楚。
        for (int index = 0; index < config.Connections.Count; index++)
        {
            // 当前边只可能对应一条前置波次依赖。
            MSceneEditorConnection link = config.Connections[index];
            if (link.First.Owner == source && link.First.Name == "prevInKnob") inputs.Add(link);
            if (link.Second.Owner == source && link.Second.Name == "prevInKnob") inputs.Add(link);
            if (link.First.Owner == wave && link.First.Name == "prevInKnob") old.Add(link);
            if (link.Second.Owner == wave && link.Second.Name == "prevInKnob") old.Add(link);
        }
        // Disconnect 会同步字段但不改节点身份。
        for (int index = 0; index < old.Count; index++) config.Disconnect(old[index]);
        ROSceneXml.Set(wave.Data, "base_info/pre_wave_option", ROSceneXml.Text(source.Data, "base_info/pre_wave_option"));
        ROSceneXml.Set(wave.Data, "base_info/pre_scene_ai_event", ROSceneXml.Text(source.Data, "base_info/pre_scene_ai_event"));
        ROSceneXml.Set(wave.Data, "base_info/not_active_create", ROSceneXml.Text(source.Data, "base_info/not_active_create"));
        // 真实连接按原门槛的端口重建，随后恢复原百分比阈值。
        Dictionary<string, string> copies = new Dictionary<string, string> { { source.Id, wave.Id } };
        CopyConnections(config, inputs, copies, source.Id);
        wave = FindNode(config, wave.Id);
        ROSceneXml.Find(wave.Data, "base_info/pre_wave_list").ReplaceWith(previous);
        config.IsDirty = true;
        return wave;
    }

    /// <summary>检查来源是否直接要求当前波次自身完成；不推断其他未证实的图循环。</summary>
    /// <param name="wave">用户正在修改出生条件的波次。</param>
    /// <param name="source">用户准备沿用前置列表的真实来源波次。</param>
    public static string WaveStartBlockReason(MSceneEditorNode wave, MSceneEditorNode source)
    {
        if (wave.Id == source.Id) return "";
        // Config.Connect 无法建立自身的前置连接，原列表中的该项必须在修改前明确拒绝。
        XElement previous = ROSceneXml.Find(source.Data, "base_info/pre_wave_list");
        if (previous == null) return "";
        // 每项保留原 Wave ID 语义，不能按相似事件名猜测依赖。
        XElement[] rows = previous.Elements().ToArray();
        // 只检查已复现的直接自依赖，不新增全图状态或环路框架。
        for (int index = 0; index < rows.Length; index++)
            if (ROSceneXml.Int(rows[index], "pre_wave_id", -1) == wave.WaveId)
                return "波次 " + source.WaveId + " 的前置条件包含当前波次 " + wave.WaveId + "，不能让本波次等待自身完成；请选择其他出生来源。";
        return "";
    }

    /// <summary>用怪物结构模板创建入场波次；不会继承模板其他阶段的事件或前置关系。</summary>
    /// <param name="config">接收新节点的配置。</param>
    /// <param name="template">当前 XML 中的既有怪物结构模板。</param>
    public static MSceneEditorNode CreateEntryWave(MSceneEditorConfig config, MSceneEditorNode template)
    {
        // 原模板只提供怪物类型、数量和刷点，启动条件明确改为入场。
        MSceneEditorNode added = config.DuplicateNode(template);
        ROSceneXml.Set(added.Data, "base_info/pre_wave_option", "PreWaveOptionTypeNone");
        ROSceneXml.Set(added.Data, "base_info/pre_scene_ai_event", "");
        ROSceneXml.Set(added.Data, "base_info/end_scene_ai_event", "");
        ROSceneXml.Set(added.Data, "base_info/not_active_create", "false");
        added.Content = "新增入场怪物";
        return added;
    }

    /// <summary>在已确认的上一阶段完成规则后发出新波次事件，支持原本没有波次的阶段。</summary>
    /// <param name="config">接收波次和原规则结果的当前配置。</param>
    /// <param name="template">只用于复制怪物结构、数量和刷点的现有怪物。</param>
    /// <param name="rule">已由 EntryRules 确认会推进到所选阶段的原事件规则。</param>
    public static MSceneEditorNode CreateWaveAfterRule(MSceneEditorConfig config, MSceneEditorNode template, XElement rule)
    {
        // 新波次只复用怪物结构，不沿用模板的出生事件。
        MSceneEditorNode added = CreateEntryWave(config, template);
        // 波次编号形成可维护的出生信号，已有同名信号时加后缀避免碰撞。
        string signal = "StageEditorWave_" + added.WaveId;
        while (config.Document.Descendants().Any(item => (string)item.Attribute("name") == signal || item.Value == signal)) signal += "_new";
        ROSceneXml.Set(added.Data, "base_info/pre_wave_option", "PreWaveOptionTypeWaitEvent");
        ROSceneXml.Set(added.Data, "base_info/pre_scene_ai_event", signal);
        // 原运行器仍消费原 wave/name；额外属性仅记录编辑器创建的私有结果归属。
        rule.Add(new XElement("wave", new XAttribute("name", signal), new XAttribute("editor_wave_id", added.WaveId)));
        added.Content = "任务推进后新增怪物";
        config.IsDirty = true;
        return added;
    }

    /// <summary>只返回完成全部前驱目标且唯一后继为当前阶段的原规则，不猜测 NPC 分支选择。</summary>
    /// <param name="config">包含真实 scene_ai_events 的配置。</param>
    /// <param name="stages">当前目标表解释出的阶段。</param>
    /// <param name="stage">用户要新增波次的阶段。</param>
    public static List<XElement> EntryRules(MSceneEditorConfig config, List<ROSceneStage> stages, ROSceneStage stage)
    {
        // 多条候选交给用户选择，不能静默认定所有前驱会同时完成。
        List<XElement> result = new List<XElement>();
        // 原规则顺序决定运行时首个成功分支，编辑器保留该顺序。
        XElement events = ROSceneXml.Find(config.Document.Root, "data/scene_ai_events");
        if (events == null) return result;
        // 规则数组只保存当前文档对象引用。
        XElement[] rules = events.Elements("event").ToArray();
        // 从真实后继关系反查前驱，不根据阶段显示编号减一。
        for (int index = 0; index < stages.Count; index++)
        {
            // 前驱必须有完整目标且不存在未决分支。
            ROSceneStage previous = stages[index];
            if (previous.NextKeys.Count != 1 || previous.TargetIds.Count == 0) continue;
            if (previous.NextKeys[0] != stage.Key) continue;
            // 仅可复用原规则中明确包含全部前驱目标的结果。
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                // 原 target/id 值沿用既有运行时格式。
                XElement results = rules[ruleIndex];
                // 当前规则明确完成的目标编号集合。
                List<int> completed = new List<int>();
                // 原 XML target 元素顺序仍由文档保留。
                XElement[] targets = results.Elements("target").ToArray();
                // 每个 target 对应一个完成通知。
                for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++) completed.Add((int)targets[targetIndex].Attribute("id"));
                // 当前全部前驱目标必须被同一真实分支完成。
                bool complete = true;
                // 按正式目标集合检查，不把其中一个目标当作整段完成。
                for (int targetIndex = 0; targetIndex < previous.TargetIds.Count; targetIndex++)
                    if (!completed.Contains(previous.TargetIds[targetIndex])) complete = false;
                if (complete && !result.Contains(rules[ruleIndex])) result.Add(rules[ruleIndex]);
            }
        }
        return result;
    }

    /// <summary>复制单个完整触发器上下文，公共组仍复用，时机/条件/结果使用新私有副本。</summary>
    /// <param name="config">接收机制副本的当前配置。</param>
    /// <param name="source">已确认属于当前阶段的真实触发器。</param>
    public static MSceneEditorNode DuplicateMechanism(MSceneEditorConfig config, MSceneEditorNode source)
    {
        // 只复制已有展示助手明确界定的触发链，不复制整张图或整个组。
        List<MSceneEditorNode> nodes = ROSceneNodePresentation.FlowNodes(config, source);
        // 保存原连接以便逐一映射内部边和真实组依赖。
        List<MSceneEditorConnection> links = new List<MSceneEditorConnection>(config.Connections);
        // 每个原节点身份只对应一个副本，保证共享条件在本次复制内不重复。
        Dictionary<string, string> copies = new Dictionary<string, string>();
        // 复制后原 XElement 仍有效，节点编号用于最终重新取得宿主。
        for (int index = 0; index < nodes.Count; index++) copies[nodes[index].Id] = config.DuplicateNode(nodes[index]).Id;
        CopyConnections(config, links, copies, source.Id);
        // 根触发器负责卡片身份，不把时机或结果作为独立机制返回。
        MSceneEditorNode copy = FindNode(config, copies[source.Id]);
        copy.Content = source.Content + "（复制）";
        return copy;
    }

    /// <summary>重建副本内部连接与外部依赖，不把共享节点的其他消费者连到副本。</summary>
    /// <param name="config">当前接收连接的配置。</param>
    /// <param name="links">复制前的真实连接快照。</param>
    /// <param name="copies">原节点 ID 到新节点 ID 的映射。</param>
    /// <param name="rootId">本次复制的波次或触发器根节点 ID。</param>
    private static void CopyConnections(MSceneEditorConfig config, List<MSceneEditorConnection> links, Dictionary<string, string> copies, string rootId)
    {
        // 每条原边最多写入一条对应副本边。
        for (int index = 0; index < links.Count; index++)
        {
            // 原连接的两个端点保持原字段和方向。
            MSceneEditorPort first = links[index].First;
            // 第二个端点可能属于共享的组或前置波次。
            MSceneEditorPort second = links[index].Second;
            // 两端是否都属于本次需要复制的触发链。
            bool firstCopied = copies.ContainsKey(first.Owner.Id);
            // 当前边与本次复制完全无关时跳过。
            bool secondCopied = copies.ContainsKey(second.Owner.Id);
            if (!firstCopied && !secondCopied) continue;
            if (!firstCopied || !secondCopied)
            {
                // 位于副本一侧的端口决定外部连接是否为依赖，而非共享消费者。
                MSceneEditorPort local = first;
                if (secondCopied) local = second;
                // 根节点输入保留原组/前置来源，结果的 groupKnob 保留真实作用目标。
                bool dependency = local.Owner.Id == rootId && local.IsInput;
                if (local.Name == "groupKnob") dependency = true;
                if (!dependency) continue;
            }
            // 每次 Connect 会重建宿主，因此使用当前节点查找对应名称的端口。
            string firstId = first.Owner.Id;
            // 未复制的一端保留原共享身份。
            string secondId = second.Owner.Id;
            if (firstCopied) firstId = copies[firstId];
            if (secondCopied) secondId = copies[secondId];
            config.Connect(Port(FindNode(config, firstId), first.Name), Port(FindNode(config, secondId), second.Name));
        }
    }

    /// <summary>按原端口名取得当前宿主端口，名称不参与任何新运行协议。</summary>
    /// <param name="node">重建后的节点。</param>
    /// <param name="name">原连接端点的端口名称。</param>
    private static MSceneEditorPort Port(MSceneEditorNode node, string name)
    {
        // 节点模板保留原端口列表，复制只替换数字身份。
        for (int index = 0; index < node.Ports.Count; index++) if (node.Ports[index].Name == name) return node.Ports[index];
        throw new InvalidOperationException("节点缺少原端口：" + name);
    }

    /// <summary>比较真实非空前置列表，供只读阶段解释器识别相同门槛并行出生的波次。</summary>
    /// <param name="first">已有阶段证据的波次。</param>
    /// <param name="second">待关联的普通或逻辑波次。</param>
    public static bool SamePreWaveGate(MSceneEditorNode first, MSceneEditorNode second)
    {
        // 仅比较已有实际执行语义的 And/Or 前置，不用空条件把全图合并。
        string option = ROSceneXml.Text(first.Data, "base_info/pre_wave_option");
        if (option != "PreWaveOptionTypeAnd" && option != "PreWaveOptionTypeOr") return false;
        if (ROSceneXml.Text(second.Data, "base_info/pre_wave_option") != option) return false;
        // 禁止自动创建与主动出生不是同一个门槛。
        if (ROSceneXml.Text(first.Data, "base_info/not_active_create") != ROSceneXml.Text(second.Data, "base_info/not_active_create")) return false;
        // 原顺序和阈值必须逐项一致，不能仅比较编号集合。
        XElement firstList = ROSceneXml.Find(first.Data, "base_info/pre_wave_list");
        // 第二个列表保留其原节点结构。
        XElement secondList = ROSceneXml.Find(second.Data, "base_info/pre_wave_list");
        return firstList != null && firstList.HasElements && XNode.DeepEquals(firstList, secondList);
    }

    /// <summary>计算机制删除范围，只移除没有其他消费者的私有时机、条件和结果。</summary>
    /// <param name="config">提供原连接的当前配置。</param>
    /// <param name="node">用户选中的波次或根触发器。</param>
    public static List<MSceneEditorNode> DeletionNodes(MSceneEditorConfig config, MSceneEditorNode node)
    {
        if (node.IsWave) return new List<MSceneEditorNode> { node };
        // 组、区域和其他机制不在 FlowNodes 的私有候选集合中。
        List<MSceneEditorNode> removed = ROSceneNodePresentation.FlowNodes(config, node);
        // 共享时机会使其条件也成为共享依赖，逐层保留直到集合稳定。
        bool changed = true;
        while (changed)
        {
            changed = false;
            // 逆序删除候选，保持其他候选对象的引用不变。
            for (int index = removed.Count - 1; index >= 0; index--)
            {
                if (removed[index] == node) continue;
                // 只检查 inKnob 上的消费者；结果输出到公共组不代表结果归公共组所有。
                for (int linkIndex = 0; linkIndex < config.Connections.Count; linkIndex++)
                {
                    // 当前连接可能从任一端保存，不能依赖 port1/port2 方向。
                    MSceneEditorConnection link = config.Connections[linkIndex];
                    // 外部消费者存在时，本节点和其后续依赖必须保留。
                    MSceneEditorNode consumer = null;
                    if (link.First.Owner == removed[index] && link.First.Name == "inKnob") consumer = link.Second.Owner;
                    if (link.Second.Owner == removed[index] && link.Second.Name == "inKnob") consumer = link.First.Owner;
                    if (consumer == null || removed.Contains(consumer)) continue;
                    removed.RemoveAt(index);
                    changed = true;
                    break;
                }
            }
        }
        return removed;
    }

    /// <summary>报告不能自动移除的数值引用，防止删除后留下不可达成的清波条件。</summary>
    /// <param name="config">包含场景事件规则的原配置。</param>
    /// <param name="node">待删除的波次或机制。</param>
    /// <param name="stages">当前副本的全部目标阶段。</param>
    /// <param name="targets">正式目标表，只读不修改。</param>
    public static string DeleteBlockReason(MSceneEditorConfig config, MSceneEditorNode node, List<ROSceneStage> stages, TbDungeonTargetTable targets)
    {
        if (!node.IsWave) return "";
        // 清波条件直接依赖 Wave ID，不能把删除当作自动完成。
        List<string> references = new List<string>();
        // 规则中所有 require_wave 保持其真实所属事件名。
        XElement[] required = config.Document.Descendants("require_wave").ToArray();
        // 此类引用不能通过断开画布连线自动修复。
        for (int index = 0; index < required.Length; index++)
            if ((int)required[index].Attribute("id") == node.WaveId) references.Add("事件 " + (string)required[index].Parent.Attribute("name") + " 的 require_wave");
        // 只检查当前副本正式阶段引用，避免把其他副本相同 Wave ID 当作关联。
        for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
        {
            // 正式清波目标以类型 8 的参数保存 Wave ID。
            for (int targetIndex = 0; targetIndex < stages[stageIndex].TargetIds.Count; targetIndex++)
            {
                // 原目标表缺失行由已有解析器负责说明。
                DungeonTargetTable target = targets.GetOrDefault(stages[stageIndex].TargetIds[targetIndex]);
                if (target == null || target.Target[0] != 8) continue;
                if (Array.IndexOf(target.Target, (double)node.WaveId, 1) >= 1) references.Add("正式目标 " + target.ID + "：" + target.TargetDes);
            }
        }
        // 采集/结束事件是其他波次真实出生来源，不能静默删除唯一生产者。
        List<string> producedSignals = ProducedSignals(node);
        // 同一采集波可同时发送采集和结束事件，两种真实依赖都要检查。
        for (int signalIndex = 0; signalIndex < producedSignals.Count; signalIndex++)
        {
            // 本轮只检查当前原事件，不能用其他事件的生产者代替。
            string produced = producedSignals[signalIndex];
            // 其他波次或机制若也发出该事件，则删除此节点不会留下唯一来源悬空。
            bool hasOtherProducer = false;
            // 当前场景中仍等待该信号的具体波次编号。
            List<string> waiting = new List<string>();
            // 只按原事件字段比较，不把任意相同字符串当作生产者。
            for (int index = 0; index < config.Nodes.Count; index++)
            {
                // 当前节点即将删除，不可把它自身算作备用来源。
                MSceneEditorNode other = config.Nodes[index];
                if (other == node) continue;
                if (ROSceneXml.Text(other.Data, "base_info/pre_scene_ai_event") == produced) waiting.Add(other.WaveId.ToString(CultureInfo.InvariantCulture));
                if (ROSceneXml.Text(other.Data, "collection_info/collect_end_ai_event") == produced) hasOtherProducer = true;
                if (ROSceneXml.Text(other.Data, "base_info/end_scene_ai_event") == produced) hasOtherProducer = true;
                if (ROSceneXml.Text(other.Data, "wave_event_info/wave_event") == produced) hasOtherProducer = true;
            }
            if (!hasOtherProducer && waiting.Count > 0)
                references.Add("事件 " + produced + " 是波次 " + string.Join("、", waiting) + " 的唯一已配置出生来源；请先修改这些波次的出生条件或明确删除依赖波次");
        }
        if (references.Count == 0) return "";
        return "波次 " + node.WaveId + " 仍被以下真实条件引用：\n" + string.Join("\n", references) + "\n请先调整这些真实条件再删除；本操作不会修改正式目标表或把缺失波次当作已完成。";
    }

    /// <summary>生成删除前的具体影响说明，公共组、区域和共享下游保留。</summary>
    /// <param name="config">提供原连线和规则的配置。</param>
    /// <param name="node">即将删除的卡片根节点。</param>
    public static string DeletionImpact(MSceneEditorConfig config, MSceneEditorNode node)
    {
        // 最终删除集合与实际执行使用同一计算，不单凭卡片数估计。
        List<MSceneEditorNode> removed = DeletionNodes(config, node);
        // 每个连接最多计数一次。
        int connectionCount = 0;
        // 原下游波次的前置变化逐项显示给用户。
        List<string> effects = new List<string>();
        // 当前连接的两端都可能保存为输入端。
        for (int index = 0; index < config.Connections.Count; index++)
        {
            // 当前边被删集合中的任意端点拥有时都会移除。
            MSceneEditorConnection link = config.Connections[index];
            if (!removed.Contains(link.First.Owner) && !removed.Contains(link.Second.Owner)) continue;
            connectionCount++;
            if (node.IsWave)
            {
                if (link.First.Name == "prevInKnob" && link.Second.Owner == node) effects.Add("波次 " + link.First.Owner.WaveId + " 将移除此一个前置条件");
                if (link.Second.Name == "prevInKnob" && link.First.Owner == node) effects.Add("波次 " + link.Second.Owner.WaveId + " 将移除此一个前置条件");
            }
        }
        if (node.IsWave)
        {
            // 同盟列表是独立于连线的真实数值引用，删除时将精确清理这一项。
            for (int index = 0; index < config.Nodes.Count; index++)
            {
                // 当前波次的同盟列表可能引用即将删除的 Wave ID。
                XElement allies = ROSceneXml.Find(config.Nodes[index].Data, "monster_info/monster_base_data/alliance_info/wave_ids");
                if (allies == null) continue;
                if (allies.Elements().Any(item => item.Value == node.WaveId.ToString(CultureInfo.InvariantCulture))) effects.Add("波次 " + config.Nodes[index].WaveId + " 的同盟列表将移除此波次编号");
            }
        }
        // 删除机制可能移除目标事件生产者，必须先说明真实事件名及原完成通知。
        for (int index = 0; index < removed.Count; index++)
        {
            // SceneAI 和 WaveEvent 是两种原通道，均可用于原规则的目标推进。
            List<string> signals = ProducedSignals(removed[index]);
            // 一个波次可以在采集和结束时分别发出事件，不能只展示其中第一个。
            for (int signalIndex = 0; signalIndex < signals.Count; signalIndex++)
            {
                // 本次影响逐个对应原事件名。
                string signal = signals[signalIndex];
                // 消费者即使还有其他生产者，也必须让用户看到本次会移除一个真实出生来源。
                for (int waveIndex = 0; waveIndex < config.Nodes.Count; waveIndex++)
                    if (ROSceneXml.Text(config.Nodes[waveIndex].Data, "base_info/pre_scene_ai_event") == signal)
                        effects.Add("波次 " + config.Nodes[waveIndex].WaveId + " 仍等待 " + signal + "；删除后将失去此事件生产者，请确认保留来源能在正确时机触发");
                // 原事件名匹配实际配置中的规则，而不是按文字相似猜测。
                XElement[] rules = config.Document.Descendants("event").Where(item => (string)item.Attribute("name") == signal).ToArray();
                // 同名规则可能有多个分支，都需要告知其目标影响。
                for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
                {
                    // 此规则的正式目标通知仍保持在原 XML 中，不随机制删除。
                    XElement[] completed = rules[ruleIndex].Descendants("target").ToArray();
                    // 每个通知独立列出，避免漏掉并行目标。
                    for (int targetIndex = 0; targetIndex < completed.Length; targetIndex++) effects.Add("目标 " + (string)completed[targetIndex].Attribute("id") + " 的事件 " + signal + " 将失去此生产者（目标定义保留）");
                }
            }
        }
        return "将删除 " + removed.Count + " 个节点及其私有数据、" + connectionCount + " 条连接。\n公共触发组、区域对象及其他机制共享的下游节点保留。\n" + string.Join("\n", effects);
    }

    /// <summary>列出直接引用本波次实体的正式目标，删除不会替策划降低目标要求。</summary>
    /// <param name="node">用户选择删除的原波次。</param>
    /// <param name="stages">当前副本真实目标阶段。</param>
    /// <param name="targets">只读正式目标表。</param>
    public static string EntityTargetImpact(MSceneEditorNode node, List<ROSceneStage> stages, TbDungeonTargetTable targets)
    {
        if (!node.IsWave) return "";
        // 同一目标只提示一次，避免分支中的重复引用堆叠文案。
        List<string> impacts = new List<string>();
        // 当前副本中实际存在的目标引用范围。
        for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
        {
            // 每个原目标独立判断其实体类型，不能用编号碰巧相同作为关联。
            for (int targetIndex = 0; targetIndex < stages[stageIndex].TargetIds.Count; targetIndex++)
            {
                // 原表中的目标参数决定 NPC、怪物或采集物身份。
                DungeonTargetTable target = targets.GetOrDefault(stages[stageIndex].TargetIds[targetIndex]);
                if (target == null) continue;
                // 本波次是否为该正式目标可消费的实体来源。
                bool referenced = false;
                if (target.Target[0] == 1) referenced = ROSceneXml.Int(node.Data, "npc_info/npc_id", -1) == (int)target.Target[1];
                if (target.Target[0] == 2) referenced = ROSceneXml.Int(node.Data, "monster_info/entity_id", -1) == (int)target.Target[1];
                if (target.Target[0] == 3) referenced = ROSceneXml.Int(node.Data, "collection_info/collection_id", -1) == (int)target.Target[1];
                if (!referenced) continue;
                // 原目标名称及 ID 明确提示删除不会同步修改三表。
                string text = "正式目标 " + target.ID + "：" + target.TargetDes + " 引用本波次实体；目标要求保持不变，请确认仍有其他可达成来源。";
                if (!impacts.Contains(text)) impacts.Add(text);
            }
        }
        return string.Join("\n", impacts);
    }

    /// <summary>收集节点配置的全部输出事件，避免采集事件与结束事件互相遮蔽。</summary>
    /// <param name="node">原波次或机制结果节点。</param>
    private static List<string> ProducedSignals(MSceneEditorNode node)
    {
        // 仅列现有运行配置中的明确事件字段，不按任意文本猜依赖。
        string[] paths = { "scene_ai_event/ai_event", "wave_event_info/wave_event", "base_info/end_scene_ai_event", "collection_info/collect_end_ai_event" };
        // 同名事件只报告一次，保持原字段顺序。
        List<string> signals = new List<string>();
        // 每个路径对应一类真实事件生产者。
        for (int index = 0; index < paths.Length; index++)
        {
            // 空字段不代表事件。
            string signal = ROSceneXml.Text(node.Data, paths[index]);
            if (signal.Length > 0 && !signals.Contains(signal)) signals.Add(signal);
        }
        return signals;
    }

    /// <summary>执行已确认的删除，清理仅被该新增波次消费的编辑器私有出生结果。</summary>
    /// <param name="config">当前可写配置。</param>
    /// <param name="node">用户已确认删除的卡片根节点。</param>
    public static void Delete(MSceneEditorConfig config, MSceneEditorNode node)
    {
        // 清理前保存根波次身份，节点删除后不能再靠当前位置寻找原对象。
        int waveId = node.WaveId;
        // 最后一个复制品删除时，也要检查它仍在消费的编辑器私有出生结果。
        string consumedSignal = ROSceneXml.Text(node.Data, "base_info/pre_scene_ai_event");
        // 仅私有链可删除，原组和共享结果不进入该集合。
        List<MSceneEditorNode> removed = DeletionNodes(config, node);
        // 每次 RemoveNode 会 Rebuild，因此按稳定编号取当前宿主。
        for (int index = 0; index < removed.Count; index++) config.RemoveNode(FindNode(config, removed[index].Id));
        if (waveId < 0) return;
        // 真实同盟波次列表独立于画布连线，精确移除该编号而不扫描无关整数。
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 原怪物同盟配置中可以引用本次删除的波次。
            XElement allies = ROSceneXml.Find(config.Nodes[index].Data, "monster_info/monster_base_data/alliance_info/wave_ids");
            if (allies == null) continue;
            // 仅遍历原列表项，其他同盟参数保持不变。
            XElement[] ids = allies.Elements().ToArray();
            // 被删波次从列表中移除，保留其余波次身份和顺序。
            for (int itemIndex = 0; itemIndex < ids.Length; itemIndex++) if (ids[itemIndex].Value == waveId.ToString(CultureInfo.InvariantCulture)) ids[itemIndex].Remove();
        }
        // 所有权属性只由 CreateWaveAfterRule 写入，不清理原作者的 wave 结果。
        XElement[] owned = config.Document.Descendants("wave").Where(item => item.Attribute("editor_wave_id") != null).ToArray();
        // 复制波次可能仍监听该出生信号，出现真实其他消费者时保留结果。
        for (int index = 0; index < owned.Length; index++)
        {
            // 当前私有结果原样保存的事件名。
            string signal = (string)owned[index].Attribute("name");
            if ((string)owned[index].Attribute("editor_wave_id") != waveId.ToString(CultureInfo.InvariantCulture) && signal != consumedSignal) continue;
            // 同名事件规则及其他波次都是实际消费者。
            bool shared = config.Nodes.Exists(item => ROSceneXml.Text(item.Data, "base_info/pre_scene_ai_event") == signal);
            if (config.Document.Descendants("event").Any(item => (string)item.Attribute("name") == signal)) shared = true;
            if (!shared) owned[index].Remove();
        }
        config.IsDirty = true;
    }
}
