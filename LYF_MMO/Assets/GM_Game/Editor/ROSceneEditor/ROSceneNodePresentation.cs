using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

/// <summary>只读解释原节点与连线，供列表、画布和地图定位共用，不改变 XML 或执行规则。</summary>
public static class ROSceneNodePresentation
{
    /// <summary>为原端口补充中文用途，保留括号内英文名供连线数据对照。</summary>
    /// <param name="port">仅用于展示的原端口，不修改名称或连接。</param>
    public static string PortLabel(MSceneEditorPort port)
    {
        // 中文只作用于界面，序列化仍使用 port.Name。
        string label;
        switch (port.Name)
        {
            case "inKnob": label = "上游输入"; break;
            case "borntimingKnob": label = "出生启用时机"; break;
            case "timingKnob": label = "触发时机"; break;
            case "resultKnob": label = "执行结果"; break;
            case "conditionKnob": label = "判断条件"; break;
            case "fromClientKnob": label = "客户端事件"; break;
            case "fromServerKnob": label = "服务端事件"; break;
            case "triggerKnob": label = "组内触发器"; break;
            case "groupKnob": label = "目标触发组"; break;
            case "prevInKnob": label = "前置波次"; break;
            case "prevOutKnob": label = "后续波次"; break;
            case "followInKnob": label = "跟随目标"; break;
            case "followOutKnob": label = "跟随者"; break;
            default:
                label = "输出端口";
                if (port.IsInput) label = "输入端口";
                break;
        }
        return label + "（" + port.Name + "）";
    }

    /// <summary>按原节点保存及执行语义解释端口，区分启用、触发、前置波次和跟随关系。</summary>
    /// <param name="port">当前悬停或显示相邻连接的端口。</param>
    public static string PortDescription(MSceneEditorPort port)
    {
        switch (port.Name)
        {
            case "inKnob":
                if (IsTrigger(port.Owner)) return "连接所属触发器组，由组内管理此触发器。";
                if (HasField(port.Owner, "timing_type")) return "连接所属触发器的出生启用时机或触发时机端口，具体作用由连到的端口决定。";
                if (HasField(port.Owner, "result_type")) return "连接所属触发器的执行结果端口，触发后执行本结果。";
                if (HasField(port.Owner, "condition_type")) return "连接需要本条件的时机节点，条件参与该时机的判断。";
                if (port.Owner.Data != null && port.Owner.Data.Name.LocalName.EndsWith("SceneTriggerGroupData", System.StringComparison.Ordinal)) return "接收启用或关闭此触发器组的控制结果连接。";
                return "连接上游节点，具体用途由当前节点类型和对端端口决定。";
            case "borntimingKnob": return "连接触发器的出生启用时机（opentimings）；默认未启用时等待该时机，满足后启用触发器。";
            case "timingKnob": return "连接触发器启用后监听的时机；时机及其条件满足后，执行相连结果。";
            case "resultKnob": return "连接触发后要执行的结果，执行顺序由结果节点的 result_index 配置决定。";
            case "conditionKnob": return "连接当前时机需要判断的条件，组合方式使用该时机的原配置。";
            case "fromClientKnob": return "接收客户端“触发服务端组”结果，向本服务端触发器组发送事件。";
            case "fromServerKnob": return "接收服务端“触发客户端组”结果，向本客户端触发器组发送事件。";
            case "triggerKnob": return "连接本触发器组包含的触发器，各触发器按自己的时机、条件和结果执行。";
            case "groupKnob": return "指定当前结果作用的触发器组，用于发送组事件或启用、关闭目标组，具体操作由结果类型决定。";
            case "prevInKnob": return "连接本波次等待的前置波次，血量、剩余数量阈值及组合方式使用前置波次配置。";
            case "prevOutKnob": return "连接以本波次作为前置条件的后续波次，是否开始由后续波次的前置条件配置决定。";
            case "followInKnob": return "连接本波次实体要跟随的目标波次，对应巡逻跟随配置 follow_wave_id。";
            case "followOutKnob": return "连接跟随本波次实体的其他波次；这是移动跟随关系，不是刷怪先后顺序。";
            default: return "原端口名：" + port.Name + "。保留原连接语义，请结合所属节点和相邻节点查看。";
        }
    }

    /// <summary>取得怪物或 NPC 的原表编号；非实体波次返回 -1。</summary>
    /// <param name="node">当前波次节点。</param>
    public static int EntityId(MSceneEditorNode node)
    {
        if (ROSceneXml.Find(node.Data, "monster_info/entity_id") != null) return ROSceneXml.Int(node.Data, "monster_info/entity_id");
        return ROSceneXml.Int(node.Data, "npc_info/npc_id", -1);
    }

    /// <summary>以真实目录名称和原表编号标识怪物或 NPC，缺少目录时保留可追溯编号。</summary>
    /// <param name="node">当前波次节点。</param>
    /// <param name="catalog">当前 XML 登记的实体资源目录，可为空。</param>
    public static string CreatureLabel(MSceneEditorNode node, ROSceneCreatureCatalog catalog)
    {
        // 两类实体使用原 XML 的独立编号字段。
        int id = EntityId(node);
        if (id < 0)
        {
            if (HasField(node, "collection_info")) return "采集物 [" + ROSceneXml.Text(node.Data, "collection_info/collection_id") + "]";
            if (HasField(node, "trigger_group_event_info")) return "触发组事件";
            return ROSceneXml.Text(node.Data, "base_info/spawn_type").Replace("SceneSpawnType_Spawn_Source_", "");
        }
        // 不使用其他场景目录补出未经证实的名称。
        ROSceneCreatureEntity entity = null;
        if (catalog != null) entity = catalog.FindEntity(id);
        // 未登记的实体仍可按编号搜索和定位原刷点。
        string name = "名称未登记";
        if (entity != null) name = entity.displayName;
        if (HasField(node, "npc_info")) return "NPC " + name + " [" + id + "]";
        return "怪物 " + name + " [" + id + "]";
    }

    /// <summary>生成短标题；机制优先使用作者备注，并保留原节点 ID 区分同名逻辑。</summary>
    /// <param name="node">需要显示的节点。</param>
    /// <param name="catalog">当前实体目录。</param>
    public static string Title(MSceneEditorNode node, ROSceneCreatureCatalog catalog)
    {
        if (node.IsWave) return "Wave " + node.WaveId + " · " + CreatureLabel(node, catalog);
        if (!string.IsNullOrWhiteSpace(node.Content) && node.Content != "备注") return node.Content;
        // 无有效备注时显示中文行为，不把重复的业务 id=0 当作唯一身份。
        string behavior = Behavior(node);
        if (behavior.Length > 0) return behavior;
        return node.TypeName + " · 节点 " + node.Id;
    }

    /// <summary>显示实际创建上限、刷点数和与上限不一致的点位数量。</summary>
    /// <param name="node">含原位置配置的波次。</param>
    public static string WaveDetails(MSceneEditorNode node)
    {
        // 同一坐标的 count 代表该点的实体数量，不等同于刷点个数。
        XElement[] points = ROSceneXml.SpawnPoints(node.Data);
        if (EntityId(node) < 0)
        {
            // 触发组事件波次没有实体总数，保留实际组编号和等待事件。
            string logic = "逻辑波次";
            if (HasField(node, "trigger_group_event_info")) logic += " · 触发组 " + ROSceneXml.Text(node.Data, "trigger_group_event_info/group_id");
            if (points.Length > 0) logic = "刷点 " + points.Length;
            // 波次等待的原场景 AI 事件只作说明，不推测其运行状态。
            string eventName = ROSceneXml.Text(node.Data, "base_info/pre_scene_ai_event");
            if (eventName.Length > 0) logic += " · 等待事件 " + eventName;
            return logic;
        }
        // 点位总数与预览使用相同的默认数量。
        int available = 0;
        for (int index = 0; index < points.Length; index++) available += ROSceneXml.Int(points[index], "count", 1);
        // 原 real_num 是创建上限，未配置时采用全部点位数量。
        int limit = ROSceneXml.Int(node.Data, "monster_info/monster_base_data/entity_info/real_num", available);
        // 超出点位数量的上限不会凭空增加刷点。
        int total = System.Math.Max(0, System.Math.Min(limit, available));
        // 非怪物逻辑波次没有刷点时明确显示零，不推测空间位置。
        string text = "总数 " + total + " · 刷点 " + points.Length;
        if (available != total) text += "（点位合计 " + available + "）";
        return text;
    }

    /// <summary>取得直接相邻节点，保持原连线顺序并去重。</summary>
    /// <param name="config">当前原图配置。</param>
    /// <param name="node">要查询的节点。</param>
    public static List<MSceneEditorNode> Adjacent(MSceneEditorConfig config, MSceneEditorNode node)
    {
        // 仅返回真实连线，不按距离或相同业务编号猜测关系。
        List<MSceneEditorNode> neighbors = new List<MSceneEditorNode>();
        for (int index = 0; index < config.Connections.Count; index++)
        {
            // 连线两端的 Owner 来自原端口引用。
            MSceneEditorConnection connection = config.Connections[index];
            // 原画布保存方向不固定，因此检查两个端点。
            MSceneEditorNode remote = null;
            if (connection.First.Owner == node) remote = connection.Second.Owner;
            if (connection.Second.Owner == node) remote = connection.First.Owner;
            if (remote != null && !neighbors.Contains(remote)) neighbors.Add(remote);
        }
        return neighbors;
    }

    /// <summary>限定到当前触发器及其时机、条件、结果；不穿过触发器组扩大为全图。</summary>
    /// <param name="config">当前原图配置。</param>
    /// <param name="node">触发器本身或其直接相连的时机、结果节点。</param>
    public static List<MSceneEditorNode> FlowNodes(MSceneEditorConfig config, MSceneEditorNode node)
    {
        // 时机和结果从一跳相邻关系找所属触发器，没有父触发器则只解释自身。
        MSceneEditorNode trigger = node;
        // 当前节点直接相连的真实对象集合。
        List<MSceneEditorNode> neighbors = Adjacent(config, node);
        if (HasField(node, "timing_type") || HasField(node, "result_type"))
        {
            for (int index = 0; index < neighbors.Count; index++)
            {
                if (!IsTrigger(neighbors[index])) continue;
                trigger = neighbors[index];
                break;
            }
        }
        // 当前触发器上下文仅包含可解释的执行节点。
        List<MSceneEditorNode> result = new List<MSceneEditorNode> { trigger };
        if (IsTrigger(trigger))
        {
            neighbors = Adjacent(config, trigger);
            for (int index = 0; index < neighbors.Count; index++)
            {
                if (HasField(neighbors[index], "timing_type") || HasField(neighbors[index], "result_type")) result.Add(neighbors[index]);
            }
        }
        // 条件只从本上下文的时机节点延伸一跳，避免连到其他组。
        int timingCount = result.Count;
        for (int index = 0; index < timingCount; index++)
        {
            if (!HasField(result[index], "timing_type")) continue;
            neighbors = Adjacent(config, result[index]);
            for (int adjacentIndex = 0; adjacentIndex < neighbors.Count; adjacentIndex++)
            {
                if (HasField(neighbors[adjacentIndex], "condition_type") && !result.Contains(neighbors[adjacentIndex])) result.Add(neighbors[adjacentIndex]);
            }
        }
        return result;
    }

    /// <summary>判断原数据是否为触发器，不混淆触发器组或时机。</summary>
    /// <param name="node">待检查节点。</param>
    public static bool IsTrigger(MSceneEditorNode node)
    {
        if (node.Data == null) return false;
        return node.Data.Name.LocalName == "MSSceneTriggerData" || node.Data.Name.LocalName == "MCSceneTriggerData";
    }

    /// <summary>判断节点是否拥有指定执行字段。</summary>
    /// <param name="node">待检查节点。</param>
    /// <param name="field">时机、条件或结果的原字段名。</param>
    public static bool HasField(MSceneEditorNode node, string field)
    {
        return ROSceneXml.Find(node.Data, field) != null;
    }

    /// <summary>按区域业务编号查找 XML 原对象，找不到时不生成虚假零坐标。</summary>
    /// <param name="config">区域所属配置。</param>
    /// <param name="id">原 scene_trigger_obj_id。</param>
    public static XElement FindArea(MSceneEditorConfig config, int id)
    {
        for (int index = 0; index < config.TriggerObjects.Length; index++)
        {
            if (ROSceneXml.Int(config.TriggerObjects[index], "id") == id) return config.TriggerObjects[index];
        }
        return null;
    }

    /// <summary>只有空间时机才把 scene_trigger_obj_id 解释为触发来源。</summary>
    /// <param name="node">待解释的时机节点。</param>
    public static bool HasSourceArea(MSceneEditorNode node)
    {
        // 事件和初始化时机中的默认 0 不表示地图区域。
        string kind = ROSceneXml.Text(node.Data, "timing_type");
        return kind == "SSttimingTypePlayerButton" || kind == "SSttimingTypeServerEnter" || kind == "SSttimingTypeServerLeave";
    }

    /// <summary>格式化原世界坐标，缺失位置时明确说明不可定位。</summary>
    /// <param name="data">含位置字段的 XML 对象。</param>
    /// <param name="path">原位置字段路径。</param>
    public static string PositionText(XElement data, string path)
    {
        if (ROSceneXml.Find(data, path) == null) return "未配置坐标";
        return "(" + ROSceneXml.Text(data, path + "/x") + ", " + ROSceneXml.Text(data, path + "/y") + ", " + ROSceneXml.Text(data, path + "/z") + ")";
    }

    /// <summary>用中文描述实际枚举和关键参数，未知枚举保留原文以便追溯。</summary>
    /// <param name="node">当前时机、条件或结果节点。</param>
    public static string Behavior(MSceneEditorNode node)
    {
        // 同类节点仅有一种执行枚举，空字段继续检查下一类。
        string kind = ROSceneXml.Text(node.Data, "timing_type");
        if (kind.Length == 0) kind = ROSceneXml.Text(node.Data, "condition_type");
        if (kind.Length == 0) kind = ROSceneXml.Text(node.Data, "result_type");
        // 区域编号只在确有区域语义的分支中使用。
        string area = "区域 " + ROSceneXml.Int(node.Data, "scene_trigger_obj_id");
        switch (kind)
        {
            case "SSttimingTypePlayerButton": return "玩家点击 " + area;
            case "SSttimingTypeServerEnter": return "玩家进入 " + area;
            case "SSttimingTypeServerLeave": return "玩家离开 " + area;
            case "SSttimingTypeInit": return "场景初始化";
            case "SSttimingTypeEnterDungeons": return "进入副本";
            case "SSttimingTypeAIEvent": return "收到 AI 事件";
            case "SSttimingTypeWaveEvent": return "收到波次事件";
            case "SSttimingTypeFromSceneRecord": return "读取场景触发记录";
            case "CSttimingTypeFromServer": return "收到服务端通知";
            case "SSTConditionTypeIsInFight":
                // 原 valid_state=12 且不取反表示禁止战斗状态，与 MoshentaSceneFlow 的按钮判断一致。
                if (ROSceneXml.Int(node.Data, "is_in_fight/valid_state") == 12 && ROSceneXml.Text(node.Data, "opposite", "false") == "false") return "角色不在战斗中（valid_state=12）";
                return "检查战斗状态（valid_state=" + ROSceneXml.Text(node.Data, "is_in_fight/valid_state") + "，取反=" + ROSceneXml.Text(node.Data, "opposite") + "）";
            case "SSTConditionTypeCheckSpecialEntity": return "检查特殊实体 " + area + "（取反=" + ROSceneXml.Text(node.Data, "opposite") + "）";
            case "SSTResultTypeTransPosition": return "传送至 " + PositionText(node.Data, "trans_position_info/position") + " · 场景 " + ROSceneXml.Text(node.Data, "trans_position_info/scene_id");
            case "SSTResultTypeCreateObj": return "创建 " + area;
            case "SSTResultTypeDestroyObj": return "销毁 " + area;
            case "SSTResultTypeAirWall": return "设置空气墙 " + area;
            case "SSTResultTypeObjAnimation": return "播放物件动画 " + area;
            case "SSTResultTypeTimer": return "等待 " + ROSceneXml.Text(node.Data, "timer_info/delay_time") + " 毫秒";
            case "SSTResultTypeWaveEvent": return "发送波次事件 " + ROSceneXml.Text(node.Data, "wave_event_info/wave_event");
            case "SSTResultTypeSceneAIEvent": return "发送场景 AI 事件 " + ROSceneXml.Text(node.Data, "scene_ai_event/ai_event");
            case "SSTResultTypeToClientGroup": return "通知客户端触发组 " + ROSceneXml.Text(node.Data, "trigger_client_group_info/group_id");
            case "SSTResultTypeShowTVNotice": return "显示公告 " + ROSceneXml.Text(node.Data, "tv_notice_info/annouce_id");
            case "SSTResultTypeRecordSceneTriggerState": return "记录场景触发状态";
            case "SSTResultTypeRecordSceneTriggerStateIndividual": return "记录个人触发状态";
            case "SSTResultTypeFireSceneEvents": return "发送场景事件 " + ROSceneXml.Text(node.Data, "firer_scene_events/scene_event_type");
            case "SSTResultTypeResetDungeons": return "重置副本";
            case "SSTResultTypeDungeonsEnd": return "结束副本";
            case "CSTResultTypeClientLuaMessage": return "客户端 Lua 消息 " + ROSceneXml.Text(node.Data, "LuaMessageData/message_type");
            case "CSTResultTypeCommandScript": return "执行客户端脚本 " + ROSceneXml.Text(node.Data, "command_script/command_script_name");
            case "CSTResultTypePlayAudio": return "播放音频 " + ROSceneXml.Text(node.Data, "asset_path");
            default: return kind;
        }
    }

    /// <summary>为无事件名字段的时机补充真实所属组；波次事件仅反查当前 XML 的明确来源。</summary>
    /// <param name="config">当前原图配置。</param>
    /// <param name="node">要解释的时机节点。</param>
    public static string TimingDescription(MSceneEditorConfig config, MSceneEditorNode node)
    {
        // 其他时机的空间或初始化语义已由原枚举直接说明。
        string kind = ROSceneXml.Text(node.Data, "timing_type");
        if (kind != "SSttimingTypeAIEvent" && kind != "SSttimingTypeWaveEvent") return Behavior(node);
        // 按时机到触发器、触发器到组的两条原连线定位所属组。
        List<MSceneEditorNode> triggers = Adjacent(config, node).Where(IsTrigger).ToList();
        // 同一组可能经多个触发器关联，仅展示一次原组对象。
        List<MSceneEditorNode> groups = new List<MSceneEditorNode>();
        for (int index = 0; index < triggers.Count; index++)
        {
            // 组属于组织关系，不穿越到相邻组的其他触发器。
            List<MSceneEditorNode> neighbors = Adjacent(config, triggers[index]);
            for (int groupIndex = 0; groupIndex < neighbors.Count; groupIndex++)
            {
                if (neighbors[groupIndex].Data == null) continue;
                if (!neighbors[groupIndex].Data.Name.LocalName.EndsWith("SceneTriggerGroupData", System.StringComparison.Ordinal)) continue;
                if (!groups.Contains(neighbors[groupIndex])) groups.Add(neighbors[groupIndex]);
            }
        }
        // 组说明和波次来源均来自当前文档，不推测外部 AI 发送的事件名。
        List<string> descriptions = new List<string>();
        for (int index = 0; index < groups.Count; index++)
        {
            // 原业务组编号用于匹配波次的 trigger_group_event_info。
            int groupId = ROSceneXml.Int(groups[index].Data, "id");
            // 作者备注补充组用途，默认占位备注不展示。
            string groupText = "触发组 " + groupId;
            if (!string.IsNullOrWhiteSpace(groups[index].Content) && groups[index].Content != "备注") groupText += "（" + groups[index].Content + "）";
            // 只有明确配置目标组的事件波次才作为来源。
            List<string> waves = new List<string>();
            if (kind == "SSttimingTypeWaveEvent")
            {
                for (int waveIndex = 0; waveIndex < config.Nodes.Count; waveIndex++)
                {
                    if (!config.Nodes[waveIndex].IsWave) continue;
                    if (ROSceneXml.Int(config.Nodes[waveIndex].Data, "trigger_group_event_info/group_id", -1) != groupId) continue;
                    waves.Add("Wave " + config.Nodes[waveIndex].WaveId);
                }
            }
            if (waves.Count > 0) descriptions.Add("收到 " + string.Join(" / ", waves) + " 发往 " + groupText + " 的波次事件");
            else descriptions.Add(Behavior(node) + " · " + groupText + "（时机未配置事件名）");
        }
        if (descriptions.Count == 0) return Behavior(node) + "（未关联触发组，时机未配置事件名）";
        return string.Join("；", descriptions);
    }

    /// <summary>根据真实连线分别展示时机、各时机条件和按 result_index 排序的结果。</summary>
    /// <param name="config">当前原图配置。</param>
    /// <param name="node">所选触发上下文。</param>
    public static string FlowSummary(MSceneEditorConfig config, MSceneEditorNode node)
    {
        if (node.Data != null && node.Data.Name.LocalName.EndsWith("SceneTriggerGroupData", System.StringComparison.Ordinal))
        {
            // 触发组组织触发器，不会直接拥有触发时机和结果端口。
            int triggerCount = Adjacent(config, node).Count(IsTrigger);
            return "触发器组 " + ROSceneXml.Text(node.Data, "id") + " · 直接关联 " + triggerCount + " 个触发器\n各触发器按自己的时机、条件和结果执行；下方列出真实关联节点，可跳转查看。";
        }
        if (HasField(node, "condition_type")) return "条件检查\n" + Behavior(node) + "\n条件随相邻时机参与判断；下方可跳转查看组合方式。";
        // 摘要只展示当前触发器上下文，不冒充运行时执行进度。
        List<MSceneEditorNode> nodes = FlowNodes(config, node);
        // 各时机独立列出其条件，避免把不同分支误合成一个条件。
        List<string> timings = new List<string>();
        for (int index = 0; index < nodes.Count; index++)
        {
            if (!HasField(nodes[index], "timing_type")) continue;
            // 条件来自当前时机的直接连接。
            List<MSceneEditorNode> conditions = Adjacent(config, nodes[index]).Where(item => HasField(item, "condition_type")).ToList();
            // 逻辑枚举决定当前时机条件的实际组合方式。
            string conditionText = "无附加条件";
            if (conditions.Count > 0)
            {
                // 未认识的逻辑枚举保留原值，避免默认解释为与条件。
                string logic = ROSceneXml.Text(nodes[index].Data, "condition_logic_type");
                if (logic == "SceneTriggerConditionLogicType_And") logic = "全部满足";
                else if (logic == "SceneTriggerConditionLogicType_Or") logic = "任一满足";
                conditionText = logic + "：" + string.Join("；", conditions.Select(Behavior));
            }
            timings.Add(TimingDescription(config, nodes[index]) + " → " + conditionText);
        }
        // 同序号保留原连线顺序，不发明未保存的执行次序。
        List<MSceneEditorNode> results = nodes.Where(item => HasField(item, "result_type")).OrderBy(item => ROSceneXml.Int(item.Data, "result_index")).ToList();
        // 缺少时机或结果明确描述为未连接，而非推断始终触发。
        string timingText = "未连接触发时机";
        if (timings.Count > 0) timingText = string.Join("\n", timings);
        // 结果字段显示配置含义，不声称客户端已实现全部枚举。
        string resultText = "未连接执行结果";
        if (results.Count > 0) resultText = string.Join(" → ", results.Select(Behavior));
        return "何时触发 → 条件\n" + timingText + "\n→ 执行什么\n" + resultText;
    }
}
