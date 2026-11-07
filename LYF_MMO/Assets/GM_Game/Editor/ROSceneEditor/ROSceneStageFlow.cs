using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

/// <summary>按正式目标表建立阶段并读取显式节点归属；旧文件仅为首次初始化解释原图证据。</summary>
public static class ROSceneStageFlow
{
    /// <summary>按DungeonTargetProgress的阶段定义与原事件顺序建立玩家流程。</summary>
    /// <param name="config">与所选副本匹配的原XML；为空时仅展示正式目标表。</param>
    /// <param name="dungeon">正在查看的正式副本表行。</param>
    /// <param name="targetTable">原目标定义表。</param>
    /// <param name="groupTable">分支模式引用的原目标组表。</param>
    /// <param name="catalog">当前场景实体名称目录，用于关联证据中的怪物名称。</param>
    public static List<ROSceneStage> Build(MSceneEditorConfig config, DungeonsTable dungeon,
        TbDungeonTargetTable targetTable, TbDungeonTargetGroupTable groupTable, ROSceneCreatureCatalog catalog)
    {
        // 所有展示对象都独立于XML；节点列表仅保存现有节点的引用。
        List<ROSceneStage> stages = BuildSections(dungeon, groupTable);
        // 每阶段先解释原表条件，缺XML时也保留完整任务顺序与分支。
        for (int index = 0; index < stages.Count; index++) DescribeTargets(stages[index], targetTable);
        if (config == null)
        {
            for (int index = 0; index < stages.Count; index++)
                stages[index].Notes.Add("当前XML未匹配该副本；怪物、机制、出生点及地图资源未解析，不使用其他副本的数据。");
            return stages;
        }
        // 规则保持原Selector先后顺序；本解析器仅陈列条件，不假装运行布尔黑板。
        XElement eventRoot = ROSceneXml.Find(config.Document.Root, "data/scene_ai_events");
        XElement[] rules = new XElement[0];
        if (eventRoot != null) rules = eventRoot.Elements("event").ToArray();
        stages[0].EntryText = "角色进入 " + dungeon.DungeonsName + "（场景 " + dungeon.SceneID + "）\n出生点 "
            + ROSceneNodePresentation.PositionText(config.Document.Root, "data/player_spawn/position");
        stages[0].Notes.Add("阶段图来源：DungeonsTable.DungeonTarget；当前阶段才接收目标事件。图中是配置流程，不是实时通关进度。");
        if (ROSceneStageBindings.HasConfiguration(config, dungeon.DungeonsID))
        {
            // 完成条件仍解释正式目标及真实事件，但不再据此增减波次/机制归属。
            for (int index = 1; index < stages.Count - 1; index++) DescribeConfiguredCompletion(config, stages[index], targetTable, rules, catalog);
            ROSceneStageBindings.Apply(config, dungeon.DungeonsID, stages);
            return stages;
        }
        // 目标本身的清波、实体或区域条件是最直接的归属证据。
        for (int index = 1; index < stages.Count - 1; index++)
            AttachTargets(config, stages[index], targetTable, rules, catalog);
        AttachRuleWaveResults(config, stages, rules);
        // 已证明的波次先关联同启动事件、前置波次、采集结束事件以及WaveEvent触发组。
        for (int index = 1; index < stages.Count - 1; index++) ExpandWaveEvidence(config, stages[index]);
        // 入场自动出生和由目标事件推进后出生的波次分别归入进入节点或真实后继。
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 禁止自动创建的波次不能仅凭None前置判定为入场出生。
            MSceneEditorNode wave = config.Nodes[index];
            if (!wave.IsWave || ROSceneXml.Text(wave.Data, "base_info/not_active_create") == "true") continue;
            // 原前置枚举决定该波是否有明确的入场或事件启动证据。
            string option = ROSceneXml.Text(wave.Data, "base_info/pre_wave_option");
            if (option == "PreWaveOptionTypeNone") AddWave(stages[0], wave, "入场自动出生：pre_wave_option=None。");
            if (option != "PreWaveOptionTypeWaitEvent") continue;
            // 先处理目标通知后发出的同名事件，避免把137之后的战斗放在137之前。
            string eventName = ROSceneXml.Text(wave.Data, "base_info/pre_scene_ai_event");
            List<ROSceneStage> owners = EventSuccessors(stages, rules, eventName);
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
                AddWave(owners[ownerIndex], wave, "启动事件 " + eventName + " 先通知前一目标，再由Advance检查出生；归入通知后的阶段，存活实体可持续到后续阶段。");
        }
        // 扩展只沿已有证据，不能按画布位置或波次编号猜阶段。
        for (int index = 0; index < stages.Count; index++)
        {
            AttachMechanismWaves(config, stages, stages[index], rules);
            ExpandWaveEvidence(config, stages[index]);
            AttachCreatedInteractions(config, stages, stages[index]);
        }
        // 初始化机制与正式结束按钮具有明确的生命周期语义。
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 原节点可能只有时机或结果，统一沿原端口找到所属触发器。
            MSceneEditorNode node = config.Nodes[index];
            string timing = ROSceneXml.Text(node.Data, "timing_type");
            if (timing == "SSttimingTypeInit" || timing == "SSttimingTypeEnterDungeons") AddTriggerOwners(config, stages[0], node);
            if (ROSceneXml.Text(node.Data, "result_type") == "SSTResultTypeDungeonsEnd") AddTriggerOwners(config, stages[stages.Count - 1], node);
        }
        // 未证明归属的节点保留独立入口，不放进通关箭头。
        ROSceneStage unresolved = new ROSceneStage { Key = "unresolved", Title = "未解析归属（不是通关阶段）",
            EntryText = "原图保留的节点尚无足够目标、事件或端口证据确定阶段。", CompletionText = "不据此推断任务完成或执行先后。" };
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 只展示波次与完整触发器，条件和结果由其原连线详情继续展开。
            MSceneEditorNode node = config.Nodes[index];
            if (node.IsWave && !HasWave(stages, node)) unresolved.Waves.Add(node);
            if (ROSceneNodePresentation.IsTrigger(node) && !HasMechanism(stages, node)) unresolved.Mechanisms.Add(node);
        }
        if (unresolved.Waves.Count > 0 || unresolved.Mechanisms.Count > 0)
        {
            unresolved.Notes.Add("例如无事件名的外部AI时机、未接入NPC脚本或独立传送，不能仅按名称、距离或编号归入某个任务。");
            stages.Add(unresolved);
        }
        return stages;
    }

    /// <summary>为显式绑定阶段说明正式完成条件；本方法不添加或移除任何节点关联。</summary>
    /// <param name="config">供事件动作说明读取的当前场景配置。</param>
    /// <param name="stage">正式目标定义中的当前阶段。</param>
    /// <param name="targets">只读正式目标表。</param>
    /// <param name="rules">原场景事件规则，顺序与运行配置一致。</param>
    /// <param name="catalog">当前场景的怪物名称目录。</param>
    private static void DescribeConfiguredCompletion(MSceneEditorConfig config, ROSceneStage stage, TbDungeonTargetTable targets, XElement[] rules, ROSceneCreatureCatalog catalog)
    {
        // 完成语义由目标表与事件决定，不能因用户移出一个关联卡片就视为目标已完成。
        List<string> completion = new List<string>();
        // 同阶段多个目标仍要求全部完成。
        for (int index = 0; index < stage.TargetIds.Count; index++)
        {
            // 原表缺失时保留具体目标编号供修正。
            DungeonTargetTable target = targets.GetOrDefault(stage.TargetIds[index]);
            if (target == null)
            {
                completion.Add("目标 " + stage.TargetIds[index] + " 完成条件未解析");
                continue;
            }
            if ((int)target.Target[0] != 7)
            {
                completion.Add(TargetCondition(target));
                continue;
            }
            // 事件目标可以由多个原分支完成，逐项保留真实条件。
            List<string> actions = new List<string>();
            // 这里只翻译完成动作，不调用 AttachTargets 或任何归属扩展。
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                if (!RuleTargets(rules[ruleIndex]).Contains(target.ID)) continue;
                actions.Add(PlayerAction(config, rules[ruleIndex], catalog));
                stage.Notes.Add("目标 " + target.ID + " 的完成事件 " + (string)rules[ruleIndex].Attribute("name") + "；原分支 " + (string)rules[ruleIndex].Attribute("source_path"));
            }
            if (actions.Count == 0) actions.Add(TargetCondition(target));
            // 次数仍取正式目标参数，不随阶段显示关联改变。
            string action = string.Join("；或：", actions);
            if (target.Target[2] > 1) action += "（需完成 " + target.Target[2] + " 次）";
            completion.Add(action);
        }
        stage.CompletionText = CompletionSummary(stage, completion);
    }

    /// <summary>沿正式模式创建阶段，分支起点与服务端一样取最小组号。</summary>
    /// <param name="dungeon">包含原目标串的副本行。</param>
    /// <param name="groups">分支组的原目标数组。</param>
    private static List<ROSceneStage> BuildSections(DungeonsTable dungeon, TbDungeonTargetGroupTable groups)
    {
        // 入场与结束是独立节点，避免把进入角色状态当作第一场战斗。
        List<ROSceneStage> result = new List<ROSceneStage>();
        ROSceneStage entry = new ROSceneStage { Key = "entry", Title = "进入副本", EntryText = "角色进入 " + dungeon.DungeonsName,
            CompletionText = "进入后开始第一个任务阶段。" };
        result.Add(entry);
        // 空目标表只说明未配置，不凭场景节点制造任务。
        if (!string.IsNullOrWhiteSpace(dungeon.DungeonTarget) && dungeon.DungeonTargetType == 1)
        {
            // |是严格顺序，=是同阶段全部目标。
            string[] sections = dungeon.DungeonTarget.Split('|');
            for (int index = 0; index < sections.Length; index++)
            {
                // 键使用实际Section序号，避免依赖目标编号的大小。
                ROSceneStage stage = new ROSceneStage { Key = "section-" + (index + 1), Title = "阶段 " + (index + 1) };
                stage.TargetIds.AddRange(ParseIds(sections[index], '='));
                stage.EntryText = "上一阶段全部目标完成后自动进入。";
                result[result.Count - 1].NextKeys.Add(stage.Key);
                result.Add(stage);
            }
        }
        else if (!string.IsNullOrWhiteSpace(dungeon.DungeonTarget) && dungeon.DungeonTargetType == 2)
        {
            // 原服务端使用SortedDictionary，起始组不是逗号串中第一个父组。
            SortedDictionary<int, ROSceneStage> graph = new SortedDictionary<int, ROSceneStage>();
            string[] edges = dungeon.DungeonTarget.Split(',');
            for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++)
            {
                // 每行第一项是父组，后续项是互斥候选后继。
                int[] ids = ParseIds(edges[edgeIndex], '=', '|');
                for (int index = 0; index < ids.Length; index++)
                {
                    if (graph.ContainsKey(ids[index])) continue;
                    // 缺组行必须保留该图节点，不能悄悄删除原边。
                    DungeonTargetGroupTable group = groups.GetOrDefault(ids[index]);
                    ROSceneStage stage = new ROSceneStage { Key = "group-" + ids[index], Title = "分支组 " + ids[index],
                        EntryText = "上一阶段完成后自动进入；遇到分支时，等待NPC选择当前路线。" };
                    if (group != null)
                    {
                        stage.TargetIds.AddRange(group.Targets);
                        stage.Title += " · " + group.Des;
                    }
                    else stage.Notes.Add("原DungeonTargetGroupTable缺少组 " + ids[index] + "，目标未解析。");
                    graph.Add(ids[index], stage);
                }
                for (int index = 1; index < ids.Length; index++) graph[ids[0]].NextKeys.Add(graph[ids[index]].Key);
            }
            // Values是实际按组号升序排列的可索引快照。
            List<ROSceneStage> ordered = graph.Values.ToList();
            if (ordered.Count > 0) entry.NextKeys.Add(ordered[0].Key);
            result.AddRange(ordered);
        }
        else entry.Notes.Add("未配置目标或目标模式尚未支持：" + dungeon.DungeonTargetType + "。");
        // 只有目标图终点连到结束；多后继从不按列表顺序连线。
        for (int index = 1; index < result.Count; index++)
            if (result[index].NextKeys.Count == 0) result[index].NextKeys.Add("finish");
        if (result.Count == 1) entry.NextKeys.Add("finish");
        result.Add(new ROSceneStage { Key = "finish", Title = "目标完成 / 离开副本",
            EntryText = "当前分支末阶段的全部目标已完成；不会继续执行其他分支。",
            CompletionText = "完成全部任务后，与已开放的出口交互离开副本；具体出口见本阶段关联机制。" });
        return result;
    }

    /// <summary>转换原整数串，不改变原数组顺序。</summary>
    /// <param name="value">一个目标阶段或分支边。</param>
    /// <param name="separators">当前模式的正式分隔符。</param>
    private static int[] ParseIds(string value, params char[] separators)
    {
        // 参数片段均来自正式Luban表，按原运行时整数语义转换。
        string[] parts = value.Split(separators);
        int[] ids = new int[parts.Length];
        for (int index = 0; index < parts.Length; index++) ids[index] = int.Parse(parts[index], CultureInfo.InvariantCulture);
        return ids;
    }

    /// <summary>解释当前阶段同时生效的全部目标，完成条件按原处理器类型区分。</summary>
    /// <param name="stage">待补充文案的阶段。</param>
    /// <param name="table">原目标表。</param>
    private static void DescribeTargets(ROSceneStage stage, TbDungeonTargetTable table)
    {
        // 每个目标保持独立完成条件，不把同时目标当作任选其一。
        List<string> conditions = new List<string>();
        for (int index = 0; index < stage.TargetIds.Count; index++)
        {
            // 缺失定义仍显示原引用编号，供策划回到源表修复。
            DungeonTargetTable row = table.GetOrDefault(stage.TargetIds[index]);
            if (row == null)
            {
                stage.TargetTexts.Add("目标 " + stage.TargetIds[index] + "：原表缺失");
                conditions.Add("目标 " + stage.TargetIds[index] + " 完成条件未解析");
                continue;
            }
            stage.TargetTexts.Add(row.TargetDes);
            conditions.Add(TargetCondition(row));
            if ((int)row.Target[0] == 1) stage.Notes.Add("目标 " + row.ID + " 由dungeontargetnpctalk通知；当前项目尚无NPC脚本执行器。");
            if ((int)row.Target[0] == 7) stage.Notes.Add("目标 " + row.ID + " 只在当前阶段接收dungeonfinishtarget(" + row.ID + ")，累计要求 " + row.Target[2] + " 次。");
        }
        if (stage.TargetTexts.Count == 0) return;
        stage.Title += " · " + string.Join(" / ", stage.TargetTexts);
        stage.CompletionText = CompletionSummary(stage, conditions);
        if (stage.NextKeys.Count > 1) stage.Notes.Add("多个后继由NPC脚本selecttargetbranch显式选择；不会自动顺序走完所有分支。");
    }

    /// <summary>统一展示全部目标的完成关系，并用玩家能理解的语言说明选支。</summary>
    /// <param name="stage">当前阶段的真实后继关系。</param>
    /// <param name="conditions">每个当前目标各自需要达成的玩家动作。</param>
    private static string CompletionSummary(ROSceneStage stage, List<string> conditions)
    {
        // 同时目标必须全部满足，不因文案简化改成任选一个。
        string result = "完成以下全部目标：\n" + string.Join("\n", conditions);
        if (stage.NextKeys.Count > 1) result += "\n全部完成后等待NPC选择分支，再继续对应路线。";
        return result;
    }

    /// <summary>使用DungeonTargetProgress的类型语义生成完成说明。</summary>
    /// <param name="row">一条正式目标定义。</param>
    private static string TargetCondition(DungeonTargetTable row)
    {
        // 原数组首项是目标类型，第二项的含义由具体类型决定。
        int type = (int)row.Target[0];
        int subject = (int)row.Target[1];
        switch (type)
        {
            case 1: return "完成与NPC " + subject + " 的对话";
            case 2: return "击杀实体 " + subject + " × " + row.Target[2];
            case 3: return "成功采集 " + subject + " × " + row.Target[2];
            case 5: return "进入场景 " + subject + " 的区域 " + row.Target[2];
            case 7: return "完成配置指定的剧情或机制，具体来源未解析";
            case 8: return "实际激活并完成全部Wave " + string.Join(" / ", row.Target.Skip(1));
            default: return "未解析目标类型 " + type + "，原参数 " + string.Join("|", row.Target);
        }
    }

    /// <summary>依据直接目标条件与原事件分支补出本阶段波次和机制。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stage">当前目标阶段。</param>
    /// <param name="table">正式目标定义。</param>
    /// <param name="rules">按原Selector顺序保存的事件分支。</param>
    /// <param name="catalog">怪物名称目录。</param>
    private static void AttachTargets(MSceneEditorConfig config, ROSceneStage stage, TbDungeonTargetTable table, XElement[] rules, ROSceneCreatureCatalog catalog)
    {
        // 玩家说明单独投影，波次与机制归属仍按下方原关联逻辑处理。
        List<string> completion = new List<string>();
        for (int targetIndex = 0; targetIndex < stage.TargetIds.Count; targetIndex++)
        {
            // 当前目标的类型决定关联证据，不能只比较数字碰巧相同。
            DungeonTargetTable row = table.GetOrDefault(stage.TargetIds[targetIndex]);
            if (row == null)
            {
                completion.Add("目标 " + stage.TargetIds[targetIndex] + " 完成条件未解析");
                continue;
            }
            int type = (int)row.Target[0];
            for (int index = 0; index < config.Nodes.Count; index++)
            {
                // 目标实体关联是候选出生来源，不代表它会在本阶段重新出生。
                MSceneEditorNode node = config.Nodes[index];
                bool matches = false;
                if (type == 1) matches = ROSceneXml.Int(node.Data, "npc_info/npc_id", -1) == (int)row.Target[1];
                if (type == 2) matches = ROSceneXml.Int(node.Data, "monster_info/entity_id", -1) == (int)row.Target[1];
                if (type == 3) matches = ROSceneXml.Int(node.Data, "collection_info/collection_id", -1) == (int)row.Target[1];
                if (type == 8 && node.IsWave) matches = Array.IndexOf(row.Target, (double)node.WaveId, 1) >= 1;
                if (matches) AddWave(stage, node, "目标 " + row.ID + " 直接引用：" + ROSceneNodePresentation.CreatureLabel(node, catalog) + "；具体出生时机仍以波次前置为准。");
                if (type != 5 || !ROSceneNodePresentation.HasSourceArea(node)) continue;
                if (ROSceneXml.Int(node.Data, "scene_trigger_obj_id", -1) == (int)row.Target[2]) AddTriggerOwners(config, stage, node);
            }
            if (type != 7)
            {
                completion.Add(TargetCondition(row));
                continue;
            }
            // 同一目标可能存在多个原事件分支，逐项展示条件与来源。
            bool found = false;
            // 同一目标的不同事件来源是候选完成方式，保留原条件限制。
            List<string> actions = new List<string>();
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                // 只按target结果引用建立映射，不按事件名字猜任务文案。
                XElement rule = rules[ruleIndex];
                if (!RuleTargets(rule).Contains(row.ID)) continue;
                found = true;
                string eventName = (string)rule.Attribute("name");
                string evidence = "目标 " + row.ID + " ← 事件 " + eventName;
                XElement[] required = rule.Elements("require_wave").ToArray();
                for (int waveIndex = 0; waveIndex < required.Length; waveIndex++)
                {
                    // require_wave必须全部完成，原数组为空时不凭空增加清怪条件。
                    int waveId = (int)required[waveIndex].Attribute("id");
                    MSceneEditorNode wave = FindWave(config, waveId);
                    if (wave != null)
                    {
                        AddWave(stage, wave, "目标 " + row.ID + " 的require_wave明确要求完成Wave " + waveId + "。");
                        AddGatedWaves(config, stage, ROSceneXml.Text(wave.Data, "base_info/pre_scene_ai_event"),
                            "与必清Wave " + waveId + " 同启动条件；不是额外必杀目标。");
                    }
                    else stage.Notes.Add("目标 " + row.ID + " 引用的Wave " + waveId + " 在当前XML中缺失。");
                }
                if (required.Length > 0) evidence += "，全部清波 " + string.Join(" / ", required.Select(item => (string)item.Attribute("id")));
                // 原布尔前置与Selector序号只作条件展示，不模拟或预先满足。
                XElement[] flags = rule.Elements("require_bool").ToArray();
                for (int flagIndex = 0; flagIndex < flags.Length; flagIndex++)
                    evidence += "，" + (string)flags[flagIndex].Attribute("name") + "=" + (string)flags[flagIndex].Attribute("value");
                stage.Notes.Add(evidence);
                stage.Notes.Add("scene_ai_events第 " + (ruleIndex + 1) + " 分支（首个成功分支优先），源 " + (string)rule.Attribute("source_path"));
                actions.Add(PlayerAction(config, rule, catalog));
                AddEventProducers(config, stage, eventName);
            }
            if (!found) stage.Notes.Add("目标 " + row.ID + " 的完成事件在当前scene_ai_events中未解析。");
            if (actions.Count == 0) actions.Add(TargetCondition(row));
            // 次数仍来自原目标表，不因默认文案隐藏事件名而丢失累计要求。
            string action = string.Join("；或：", actions);
            if (row.Target[2] > 1) action += "（需完成 " + row.Target[2] + " 次）";
            completion.Add(action);
        }
        stage.CompletionText = CompletionSummary(stage, completion);
    }

    /// <summary>只把真实清波条件或事件来源时机翻译为玩家动作，不推测外部剧情。</summary>
    /// <param name="config">包含原事件来源端口和区域定义的配置。</param>
    /// <param name="rule">通知当前目标的SceneAI原分支。</param>
    /// <param name="catalog">当前场景怪物名称目录。</param>
    private static string PlayerAction(MSceneEditorConfig config, XElement rule, ROSceneCreatureCatalog catalog)
    {
        // 清波编号来自require_wave，必须全部满足；采集动作不能替代该条件。
        XElement[] required = rule.Elements("require_wave").ToArray();
        List<string> actions = new List<string>();
        for (int index = 0; index < required.Length; index++)
        {
            // 目录名称只是补充辨识，波次号保留原配置以便定位。
            int id = (int)required[index].Attribute("id");
            string waveText = "波次 " + id;
            MSceneEditorNode wave = FindWave(config, id);
            if (wave != null) waveText += "（" + ROSceneNodePresentation.CreatureLabel(wave, catalog) + "）";
            actions.Add(waveText);
        }
        // 黑板或触发条件必须显式提示，详细原字段留在关联说明与原机制卡片。
        bool hasExtraConditions = rule.Elements("require_bool").Any();
        string action = "完成配置指定的剧情或机制，具体来源未解析";
        if (actions.Count > 0) action = "清空全部" + string.Join("、", actions);
        else
        {
            // 无清波条件时，只从明确发送该事件的结果追到原触发时机。
            string eventName = (string)rule.Attribute("name");
            for (int index = 0; index < config.Nodes.Count; index++)
            {
                // 发送节点与实际关联算法使用同一对原字段，不通过文案或坐标猜测。
                MSceneEditorNode node = config.Nodes[index];
                bool sceneEvent = ROSceneXml.Text(node.Data, "scene_ai_event/ai_event") == eventName;
                bool waveEvent = ROSceneXml.Text(node.Data, "wave_event_info/wave_event") == eventName;
                if (!sceneEvent && !waveEvent) continue;
                List<MSceneEditorNode> triggers = Linked(config, node, "inKnob");
                for (int triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
                {
                    // 一个事件可能有多条真实来源，展示候选动作而不指定唯一外部脚本。
                    List<MSceneEditorNode> timings = Linked(config, triggers[triggerIndex], "timingKnob");
                    for (int timingIndex = 0; timingIndex < timings.Count; timingIndex++)
                    {
                        // 只有已知玩家区域时机才转换为进入、离开或交互。
                        MSceneEditorNode timing = timings[timingIndex];
                        string kind = ROSceneXml.Text(timing.Data, "timing_type");
                        string verb = "";
                        if (kind == "SSttimingTypeServerEnter") verb = "进入区域 ";
                        if (kind == "SSttimingTypeServerLeave") verb = "离开区域 ";
                        if (kind == "SSttimingTypePlayerButton") verb = "与区域物件交互：";
                        if (verb.Length == 0) continue;
                        // 区域标题来自原配置；没有标题时只保留真实编号。
                        int areaId = ROSceneXml.Int(timing.Data, "scene_trigger_obj_id");
                        XElement area = ROSceneNodePresentation.FindArea(config, areaId);
                        string areaTitle = ROSceneXml.Text(area, "box/title");
                        string text = verb + areaId;
                        if (!string.IsNullOrWhiteSpace(areaTitle)) text += "（" + areaTitle + "）";
                        if (!actions.Contains(text)) actions.Add(text);
                        if (Linked(config, timing, "conditionKnob").Count > 0) hasExtraConditions = true;
                    }
                }
            }
            if (actions.Count > 0) action = string.Join("，或", actions);
        }
        if (hasExtraConditions) action += "；需满足额外场景条件，见关联说明";
        return action;
    }

    /// <summary>取得一个事件分支明确通知的全部目标行号。</summary>
    /// <param name="rule">原SceneAI分支。</param>
    private static List<int> RuleTargets(XElement rule)
    {
        // 原target顺序有执行含义，返回结果保留其顺序。
        XElement[] rows = rule.Elements("target").ToArray();
        List<int> result = new List<int>();
        for (int index = 0; index < rows.Length; index++) result.Add((int)rows[index].Attribute("id"));
        return result;
    }

    /// <summary>当同名WaveEvent先完成阶段再调用Advance时，关联到真实后继；条件分支保留说明。</summary>
    /// <param name="stages">已建立的目标图。</param>
    /// <param name="rules">原事件分支。</param>
    /// <param name="eventName">波次等待的原事件名。</param>
    private static List<ROSceneStage> EventSuccessors(List<ROSceneStage> stages, XElement[] rules, string eventName)
    {
        // 该列表描述可能的已证实目标后继，不把分支当作全部都会执行。
        List<ROSceneStage> owners = new List<ROSceneStage>();
        for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
        {
            if ((string)rules[ruleIndex].Attribute("name") != eventName) continue;
            // 只有同一规则包含当前阶段全部目标，才能证明该事件能够推进阶段。
            List<int> ids = RuleTargets(rules[ruleIndex]);
            for (int index = 1; index < stages.Count - 1; index++)
            {
                // 同时目标和多分支保持未完成或待选支阶段，不推测NPC选择。
                ROSceneStage stage = stages[index];
                if (stage.TargetIds.Count == 0) continue;
                bool complete = true;
                for (int targetIndex = 0; targetIndex < stage.TargetIds.Count; targetIndex++)
                    if (!ids.Contains(stage.TargetIds[targetIndex])) complete = false;
                if (!complete) continue;
                if (stage.NextKeys.Count != 1)
                {
                    if (!owners.Contains(stage)) owners.Add(stage);
                    stage.Notes.Add("事件 " + eventName + " 完成目标后仍等待分支选择；其波次不能分配给尚未选定的后继。");
                    continue;
                }
                // 唯一后继按实际键查找，不依赖返回列表的视觉排序。
                ROSceneStage next = stages.Find(item => item.Key == stage.NextKeys[0]);
                if (next != null && !owners.Contains(next)) owners.Add(next);
            }
        }
        return owners;
    }

    /// <summary>只沿原波次依赖、采集结束事件和触发组扩展关联，遇到外部AI保留未解析。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stage">具有直接波次证据的阶段。</param>
    private static void ExpandWaveEvidence(MSceneEditorConfig config, ROSceneStage stage)
    {
        // 列表仅追加有限原节点，每个节点展开一次；没有运行队列、事件重放或游戏状态。
        for (int index = 0; index < stage.Waves.Count; index++)
        {
            // 当前已证明波次的启动事件与前置波次。
            MSceneEditorNode wave = stage.Waves[index];
            string gate = ROSceneXml.Text(wave.Data, "base_info/pre_scene_ai_event");
            XElement[] previous = new XElement[0];
            XElement list = ROSceneXml.Find(wave.Data, "base_info/pre_wave_list");
            if (list != null) previous = list.Elements().ToArray();
            for (int previousIndex = 0; previousIndex < previous.Length; previousIndex++)
            {
                // 前置链按XML明确编号追溯，未知枚举仍保留原字段说明。
                MSceneEditorNode parent = FindWave(config, ROSceneXml.Int(previous[previousIndex], "pre_wave_id", -1));
                if (parent != null) AddWave(stage, parent, "Wave " + wave.WaveId + " 的pre_wave_list前置；不表示该波在本阶段重新出生。");
            }
            if (gate.Length > 0) AddEventProducers(config, stage, gate);
            for (int otherIndex = 0; otherIndex < config.Nodes.Count; otherIndex++)
            {
                // 只按明确相同事件、采集发出的事件或全部前置关联逻辑后继。
                MSceneEditorNode other = config.Nodes[otherIndex];
                if (!other.IsWave || stage.Waves.Contains(other)) continue;
                if (ROSceneXml.Text(other.Data, "base_info/not_active_create") == "true") continue;
                // 阶段内新增/复制波次沿用完全相同的非空前置条件时，与原波次并行出生。
                if (ROSceneStageEditing.SamePreWaveGate(wave, other))
                    AddWave(stage, other, "与Wave " + wave.WaveId + " 使用相同的非空前置波次和阈值，并行等待这些条件满足。");
                if (gate.Length > 0)
                {
                    if (ROSceneXml.Text(other.Data, "collection_info/collect_end_ai_event") == gate)
                        AddWave(stage, other, "采集Wave " + other.WaveId + " 发出 " + gate + "，是Wave " + wave.WaveId + " 的启动来源；采集本身不替代后续清波目标。");
                }
                if (ROSceneXml.Find(other.Data, "trigger_group_event_info") == null) continue;
                // 仅清波触发组可顺向扩展，不把其他普通战斗串入未经证实的阶段。
                XElement requirements = ROSceneXml.Find(other.Data, "base_info/pre_wave_list");
                if (requirements == null || !requirements.HasElements) continue;
                bool allPresent = true;
                XElement[] rows = requirements.Elements().ToArray();
                for (int requirementIndex = 0; requirementIndex < rows.Length; requirementIndex++)
                {
                    // 当前阶段必须已有所有引用波次才展示其解锁结果。
                    int requiredId = ROSceneXml.Int(rows[requirementIndex], "pre_wave_id", -1);
                    if (!stage.Waves.Exists(item => item.WaveId == requiredId)) allPresent = false;
                }
                if (allPresent) AddWave(stage, other, "全部前置波完成后执行逻辑波Wave " + other.WaveId + " 的触发组。");
            }
            // 逻辑波只执行原组中的WaveEvent时机，不误把AIEvent或PlayerButton当自动结果。
            int groupId = ROSceneXml.Int(wave.Data, "trigger_group_event_info/group_id", -1);
            if (groupId >= 0) AddWaveGroup(config, stage, groupId);
        }
    }

    /// <summary>读取SceneAI结果的真实顺序，明确波次事件发生在目标通知之前还是之后。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stages">正式目标阶段图。</param>
    /// <param name="rules">原事件分支，保持Selector顺序。</param>
    private static void AttachRuleWaveResults(MSceneEditorConfig config, List<ROSceneStage> stages, XElement[] rules)
    {
        for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
        {
            // 结果数组包含条件字段，只有target和wave参与通知先后说明。
            XElement[] results = rules[ruleIndex].Elements().ToArray();
            List<int> priorTargets = new List<int>();
            List<int> allTargets = RuleTargets(rules[ruleIndex]);
            for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
            {
                if (results[resultIndex].Name.LocalName == "target") priorTargets.Add((int)results[resultIndex].Attribute("id"));
                if (results[resultIndex].Name.LocalName != "wave") continue;
                // 无目标结果的规则不能仅凭事件名称猜定任务阶段。
                string eventName = (string)results[resultIndex].Attribute("name");
                for (int stageIndex = 1; stageIndex < stages.Count - 1; stageIndex++)
                {
                    // 只有该规则确实通知本阶段目标才有明确的通知前后边界。
                    ROSceneStage stage = stages[stageIndex];
                    if (!stage.TargetIds.Exists(id => allTargets.Contains(id))) continue;
                    bool completedBefore = stage.TargetIds.Count > 0;
                    for (int targetIndex = 0; targetIndex < stage.TargetIds.Count; targetIndex++)
                        if (!priorTargets.Contains(stage.TargetIds[targetIndex])) completedBefore = false;
                    // 多后继仍等待显式选支，不能给某个未选分支预分配波次。
                    ROSceneStage owner = stage;
                    string order = "在本阶段目标通知前或同时目标尚未全部完成时发送";
                    if (completedBefore && stage.NextKeys.Count == 1)
                    {
                        owner = stages.Find(item => item.Key == stage.NextKeys[0]);
                        order = "在本阶段全部目标通知后发送，归入真实唯一后继";
                    }
                    if (owner == null) continue;
                    AddGatedWaves(config, owner, eventName, "scene_ai_events第 " + (ruleIndex + 1) + " 分支按原结果顺序" + order + "；仍受原require_bool/require_wave和首成功分支限制。");
                }
            }
        }
    }

    /// <summary>将已关联触发器真实发出的WaveEvent接到对应波次，先处理目标推进再处理出生。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stages">全部目标阶段。</param>
    /// <param name="stage">已有触发器证据的阶段。</param>
    /// <param name="rules">同名事件的原SceneAI目标规则。</param>
    private static void AttachMechanismWaves(MSceneEditorConfig config, List<ROSceneStage> stages, ROSceneStage stage, XElement[] rules)
    {
        // 仅消费进入方法前已证明的机制，不用未知外部AI反推整个副本图。
        int count = stage.Mechanisms.Count;
        for (int index = 0; index < count; index++)
        {
            // 结果连线顺序来自服务器相同的resultKnob读取方式。
            List<MSceneEditorNode> results = Linked(config, stage.Mechanisms[index], "resultKnob");
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                if (ROSceneXml.Text(results[resultIndex].Data, "result_type") != "SSTResultTypeWaveEvent") continue;
                // Event会先调用SceneAIEvent，因此有目标映射时必须使用目标之后的阶段。
                string name = ROSceneXml.Text(results[resultIndex].Data, "wave_event_info/wave_event");
                List<ROSceneStage> owners = EventSuccessors(stages, rules, name);
                if (owners.Count == 0) owners.Add(stage);
                for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
                    AddGatedWaves(config, owners[ownerIndex], name, "原触发器结果发送 " + name + "；该机制的原时机和条件仍须满足。");
            }
        }
    }

    /// <summary>关联明确等待指定事件的自动波次，空事件或禁用出生均不关联。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stage">接收波次的已证明阶段。</param>
    /// <param name="eventName">原WaveEvent名称。</param>
    /// <param name="reason">事件与当前阶段的直接证据。</param>
    private static void AddGatedWaves(MSceneEditorConfig config, ROSceneStage stage, string eventName, string reason)
    {
        if (string.IsNullOrEmpty(eventName)) return;
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 仅WaitEvent消费该字段，其他前置模式中的残留字符串无效。
            MSceneEditorNode wave = config.Nodes[index];
            if (!wave.IsWave || ROSceneXml.Text(wave.Data, "base_info/not_active_create") == "true") continue;
            if (ROSceneXml.Text(wave.Data, "base_info/pre_wave_option") != "PreWaveOptionTypeWaitEvent") continue;
            if (ROSceneXml.Text(wave.Data, "base_info/pre_scene_ai_event") == eventName) AddWave(stage, wave, reason);
        }
    }

    /// <summary>展示当前组中由波次明确启动的触发器。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stage">波次所属阶段。</param>
    /// <param name="groupId">逻辑波引用的服务端触发组号。</param>
    private static void AddWaveGroup(MSceneEditorConfig config, ROSceneStage stage, int groupId)
    {
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 仅服务端组被MoshentaSceneFlow.RunGroup消费。
            MSceneEditorNode group = config.Nodes[index];
            if (group.Data == null || group.Data.Name.LocalName != "MSSceneTriggerGroupData") continue;
            if (ROSceneXml.Int(group.Data, "id") != groupId) continue;
            // 组输出按原连线定位触发器，不能按近邻节点或标题匹配。
            List<MSceneEditorNode> triggers = ROSceneNodePresentation.Adjacent(config, group);
            for (int triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
            {
                // 组内按钮不在清波时执行，等后续创建物件关联再显示。
                List<MSceneEditorNode> timings = Linked(config, triggers[triggerIndex], "timingKnob");
                for (int timingIndex = 0; timingIndex < timings.Count; timingIndex++)
                    if (ROSceneXml.Text(timings[timingIndex].Data, "timing_type") == "SSttimingTypeWaveEvent") AddMechanism(stage, triggers[triggerIndex]);
            }
        }
    }

    /// <summary>把创建物件后的真实交互入口关联至解锁阶段和后继，保留跨阶段使用说明。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stages">全部阶段，供唯一后继查询。</param>
    /// <param name="stage">包含创建物件结果的阶段。</param>
    private static void AttachCreatedInteractions(MSceneEditorConfig config, List<ROSceneStage> stages, ROSceneStage stage)
    {
        // 只遍历进入本方法时已证明的机制，避免由交互链扩大到整张图。
        int count = stage.Mechanisms.Count;
        for (int index = 0; index < count; index++)
        {
            // 原结果端口保持服务器RunResults实际顺序。
            List<MSceneEditorNode> results = Linked(config, stage.Mechanisms[index], "resultKnob");
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                if (ROSceneXml.Text(results[resultIndex].Data, "result_type") != "SSTResultTypeCreateObj") continue;
                // 新创建物件号与PlayerButton时机使用同一scene_trigger_obj_id。
                int area = ROSceneXml.Int(results[resultIndex].Data, "scene_trigger_obj_id");
                for (int nodeIndex = 0; nodeIndex < config.Nodes.Count; nodeIndex++)
                {
                    // 消费者只接受玩家按钮，不把区域进入看成传送按钮。
                    MSceneEditorNode timing = config.Nodes[nodeIndex];
                    if (ROSceneXml.Text(timing.Data, "timing_type") != "SSttimingTypePlayerButton") continue;
                    if (ROSceneXml.Int(timing.Data, "scene_trigger_obj_id") != area) continue;
                    AddTriggerOwners(config, stage, timing);
                    if (stage.NextKeys.Count != 1) continue;
                    // 该入口解锁后可在后继使用，但不意味着传送直接完成后继目标。
                    ROSceneStage next = stages.Find(item => item.Key == stage.NextKeys[0]);
                    if (next == null) continue;
                    AddTriggerOwners(config, next, timing);
                    AddNote(next, "上一阶段创建物件 " + area + "，解锁其玩家交互；是否完成本阶段仍以正式目标条件为准。");
                }
            }
        }
    }

    /// <summary>找到明确发送事件的结果，并沿原结果端口关联整个触发器。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stage">事件涉及的阶段。</param>
    /// <param name="eventName">SceneAI或WaveEvent的原事件名。</param>
    private static void AddEventProducers(MSceneEditorConfig config, ROSceneStage stage, string eventName)
    {
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 默认空字段不能匹配为空的事件名，调用处只传已知名称。
            MSceneEditorNode node = config.Nodes[index];
            bool sceneEvent = ROSceneXml.Text(node.Data, "scene_ai_event/ai_event") == eventName;
            bool waveEvent = ROSceneXml.Text(node.Data, "wave_event_info/wave_event") == eventName;
            if (sceneEvent || waveEvent) AddTriggerOwners(config, stage, node);
        }
    }

    /// <summary>根据原inKnob端口找到时机或结果所属触发器。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="stage">接收关联的阶段。</param>
    /// <param name="node">原时机或结果节点。</param>
    private static void AddTriggerOwners(MSceneEditorConfig config, ROSceneStage stage, MSceneEditorNode node)
    {
        // 只有明确触发器类型才可作为机制卡片，避免展示孤立结果伪装完整流程。
        List<MSceneEditorNode> owners = Linked(config, node, "inKnob");
        for (int index = 0; index < owners.Count; index++)
            if (ROSceneNodePresentation.IsTrigger(owners[index])) AddMechanism(stage, owners[index]);
    }

    /// <summary>沿指定原端口按连接文件顺序取节点，与服务端Linked保持一致。</summary>
    /// <param name="config">包含真实连线的配置。</param>
    /// <param name="node">端口宿主节点。</param>
    /// <param name="name">原端口名称。</param>
    private static List<MSceneEditorNode> Linked(MSceneEditorConfig config, MSceneEditorNode node, string name)
    {
        // 不按端口方向猜测，原连接两端保存顺序并不固定。
        List<MSceneEditorNode> result = new List<MSceneEditorNode>();
        for (int index = 0; index < config.Connections.Count; index++)
        {
            // 原端口含Owner和Name，是连接关系的唯一依据。
            MSceneEditorConnection connection = config.Connections[index];
            if (connection.First.Owner == node && connection.First.Name == name) result.Add(connection.Second.Owner);
            else if (connection.Second.Owner == node && connection.Second.Name == name) result.Add(connection.First.Owner);
        }
        return result;
    }

    /// <summary>按原业务波次号查找当前配置中的节点。</summary>
    /// <param name="config">当前原图。</param>
    /// <param name="id">原Wave编号，零是合法编号。</param>
    private static MSceneEditorNode FindWave(MSceneEditorConfig config, int id)
    {
        for (int index = 0; index < config.Nodes.Count; index++)
            if (config.Nodes[index].IsWave && config.Nodes[index].WaveId == id) return config.Nodes[index];
        return null;
    }

    /// <summary>追加原波次引用并保留首次关联的证据。</summary>
    /// <param name="stage">目标阶段。</param>
    /// <param name="wave">当前原波次节点。</param>
    /// <param name="reason">这条关联的直接来源。</param>
    private static void AddWave(ROSceneStage stage, MSceneEditorNode wave, string reason)
    {
        if (stage.Waves.Contains(wave)) return;
        stage.Waves.Add(wave);
        AddNote(stage, "Wave " + wave.WaveId + "：" + reason);
    }

    /// <summary>机制卡片只保留原触发器引用，详情仍由原节点展示帮助类读取。</summary>
    /// <param name="stage">目标阶段。</param>
    /// <param name="node">原触发器节点。</param>
    private static void AddMechanism(ROSceneStage stage, MSceneEditorNode node)
    {
        if (!stage.Mechanisms.Contains(node)) stage.Mechanisms.Add(node);
    }

    /// <summary>去重关联说明，重复扩展不重复堆积相同文字。</summary>
    /// <param name="stage">目标阶段。</param>
    /// <param name="note">可追溯的证据或边界说明。</param>
    private static void AddNote(ROSceneStage stage, string note)
    {
        if (!stage.Notes.Contains(note)) stage.Notes.Add(note);
    }

    /// <summary>判断波次是否已有任一阶段证据。</summary>
    /// <param name="stages">当前全部流程节点。</param>
    /// <param name="node">待查原波次。</param>
    private static bool HasWave(List<ROSceneStage> stages, MSceneEditorNode node)
    {
        for (int index = 0; index < stages.Count; index++) if (stages[index].Waves.Contains(node)) return true;
        return false;
    }

    /// <summary>判断完整触发器是否已有任一阶段证据。</summary>
    /// <param name="stages">当前全部流程节点。</param>
    /// <param name="node">待查原触发器。</param>
    private static bool HasMechanism(List<ROSceneStage> stages, MSceneEditorNode node)
    {
        for (int index = 0; index < stages.Count; index++) if (stages[index].Mechanisms.Contains(node)) return true;
        return false;
    }
}
