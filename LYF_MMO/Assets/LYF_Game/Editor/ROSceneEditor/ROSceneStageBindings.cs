using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

/// <summary>同一场景 XML 中的显式阶段归属；仅管理编辑分类，不改变波次出生或机制执行条件。</summary>
public static class ROSceneStageBindings
{
    /// <summary>按正式目标身份生成稳定键，目标顺序调整不会把绑定套到其他任务上。</summary>
    /// <param name="stage">来自三张正式目标表的阶段。</param>
    public static string StageKey(ROSceneStage stage)
    {
        if (stage.IsEntry || stage.IsFinish) return stage.Key;
        if (stage.Key.StartsWith("group-", StringComparison.Ordinal)) return stage.Key;
        // 同阶段目标为同时满足关系，排序只规范持久身份，不改变目标显示或执行顺序。
        List<int> ids = new List<int>(stage.TargetIds);
        ids.Sort();
        return "targets:" + string.Join(",", ids);
    }

    /// <summary>查询当前副本是否已有显式配置，空的绑定段同样表示已配置。</summary>
    /// <param name="config">包含原 XML 的场景配置。</param>
    /// <param name="dungeonId">正式副本 ID，不能用场景 ID 代替。</param>
    public static bool HasConfiguration(MSceneEditorConfig config, int dungeonId)
    {
        return DungeonElement(config, dungeonId) != null;
    }

    /// <summary>仅为尚无配置的旧文件生成一次初始绑定；已有空配置或手工修改均不覆盖。</summary>
    /// <param name="config">当前匹配副本的场景 XML。</param>
    /// <param name="dungeonId">当前正式副本编号。</param>
    /// <param name="stages">旧文件首次解析所得阶段，未归属项不会变成正式阶段。</param>
    public static bool Initialize(MSceneEditorConfig config, int dungeonId, List<ROSceneStage> stages)
    {
        if (HasConfiguration(config, dungeonId)) return false;
        // 独立块只保存引用，不复制运行配置或把编辑器状态写入业务字段。
        XElement root = config.Document.Root.Element("editor_stage_bindings");
        if (root == null)
        {
            root = new XElement("editor_stage_bindings");
            config.Document.Root.Add(root);
        }
        // 同一地图可能对应多个正式副本，分别保存其目标阶段绑定。
        XElement dungeon = new XElement("dungeon", new XAttribute("id", dungeonId));
        root.Add(dungeon);
        // 每个正式阶段都保存空或非空段，空关联不能触发再次推导。
        for (int index = 0; index < stages.Count; index++)
        {
            // 未归属只是编辑器排查入口，不持久化为玩家任务。
            ROSceneStage stage = stages[index];
            if (stage.Key == "unresolved") continue;
            // 固定生命周期、目标集合或分支组 ID 是持久身份。
            XElement binding = new XElement("stage", new XAttribute("key", StageKey(stage)));
            dungeon.Add(binding);
            // 波次业务 ID 与运行 XML 保持一致。
            for (int waveIndex = 0; waveIndex < stage.Waves.Count; waveIndex++) binding.Add(Reference(stage.Waves[waveIndex]));
            // 机制使用画布节点 ID，不能使用大量重复的触发器业务 id=0。
            for (int mechanismIndex = 0; mechanismIndex < stage.Mechanisms.Count; mechanismIndex++) binding.Add(Reference(stage.Mechanisms[mechanismIndex]));
        }
        config.IsDirty = true;
        return true;
    }

    /// <summary>只按已保存引用装配阶段列表；缺失、空段和未归属节点均不回退到代码推导。</summary>
    /// <param name="config">当前原配置和全部节点。</param>
    /// <param name="dungeonId">正在查看的正式副本编号。</param>
    /// <param name="stages">仅由正式任务定义创建的阶段集合。</param>
    public static void Apply(MSceneEditorConfig config, int dungeonId, List<ROSceneStage> stages)
    {
        // 调用方已确认存在配置，原空段必须保留其明确语义。
        XElement dungeon = DungeonElement(config, dungeonId);
        // 用本次已关联身份生成未归属入口，允许一个节点被多个阶段引用。
        HashSet<string> assigned = new HashSet<string>();
        // 当前阶段键集合用于报告旧目标已从正式定义中移除的情况。
        HashSet<string> stageKeys = new HashSet<string>();
        // 逐个真实阶段读取独立显式关联。
        for (int index = 0; index < stages.Count; index++)
        {
            // 清除显示列表仅为了应用配置，不触及原节点或运行规则。
            ROSceneStage stage = stages[index];
            stage.Waves.Clear();
            stage.Mechanisms.Clear();
            // 稳定身份与 stage.Key 的临时显示编号分开。
            string key = StageKey(stage);
            stageKeys.Add(key);
            // 缺少当前目标段视为尚无关联，不能借用旧序号的节点。
            XElement binding = dungeon.Elements("stage").FirstOrDefault(item => (string)item.Attribute("key") == key);
            stage.Notes.Add("阶段归属读取当前 XML 的 editor_stage_bindings，键为 " + key + "；关联不改变出生或触发条件。");
            if (binding == null) continue;
            // 保存顺序用于卡片展示，不能被节点图推导重新排序。
            XElement[] references = binding.Elements().ToArray();
            // 每个引用只接受对应节点类别，错误引用保留在说明中供修正。
            for (int referenceIndex = 0; referenceIndex < references.Length; referenceIndex++)
            {
                // 原引用只能是 wave/id 或 mechanism/node_id。
                XElement reference = references[referenceIndex];
                // 未知扩展不删除，也不推测成某个现有节点。
                MSceneEditorNode node = Resolve(config, reference);
                if (node == null)
                {
                    stage.Notes.Add("显式关联未找到对应节点：" + reference.ToString(SaveOptions.DisableFormatting));
                    continue;
                }
                if (node.IsWave && !stage.Waves.Contains(node)) stage.Waves.Add(node);
                if (ROSceneNodePresentation.IsTrigger(node) && !stage.Mechanisms.Contains(node)) stage.Mechanisms.Add(node);
                assigned.Add(node.Id);
            }
        }
        // 原表删除目标时不把旧段套到其他阶段，保留原配置并提示其稳定键。
        XElement[] configured = dungeon.Elements("stage").ToArray();
        // 失配提示放在固定入口，用户仍能追溯被保留的配置。
        for (int index = 0; index < configured.Length; index++)
            if (!stageKeys.Contains((string)configured[index].Attribute("key"))) stages[0].Notes.Add("绑定段 " + (string)configured[index].Attribute("key") + " 暂无对应正式阶段，原绑定保留。" );
        // 没有显式引用的节点仍可被用户选择加入任一正式阶段。
        ROSceneStage unassigned = new ROSceneStage { Key = "unresolved", Title = "未关联节点（不是通关阶段）",
            EntryText = "这些现有节点尚未在当前副本的显式阶段配置中关联，可在正式阶段使用“关联已有”选择。",
            CompletionText = "未关联不代表不会运行；实际出生、事件和触发条件仍来自原节点配置。" };
        // 全图原节点保持不变，仅按引用集合分类。
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 同一节点只在当前副本完全未关联时进入此排查入口。
            MSceneEditorNode node = config.Nodes[index];
            if (assigned.Contains(node.Id)) continue;
            if (node.IsWave) unassigned.Waves.Add(node);
            if (ROSceneNodePresentation.IsTrigger(node)) unassigned.Mechanisms.Add(node);
        }
        if (unassigned.Waves.Count > 0 || unassigned.Mechanisms.Count > 0) stages.Add(unassigned);
    }

    /// <summary>为当前正式阶段增加或移除一个现有节点引用，不删除节点或改动其运行条件。</summary>
    /// <param name="config">包含已初始化显式关联的原配置。</param>
    /// <param name="dungeonId">当前正式副本 ID。</param>
    /// <param name="stage">关联操作的目标阶段。</param>
    /// <param name="node">现有波次或真实根触发器。</param>
    /// <param name="linked">为 true 时加入，为 false 时仅移出当前阶段。</param>
    public static void SetLinked(MSceneEditorConfig config, int dungeonId, ROSceneStage stage, MSceneEditorNode node, bool linked)
    {
        if (stage.Key == "unresolved") throw new InvalidOperationException("未关联列表不是正式阶段，请选择进入、目标阶段或结束。" );
        // 添加前必须由窗口按已匹配的目标表初始化，不能自行推测其他副本。
        XElement dungeon = DungeonElement(config, dungeonId);
        if (dungeon == null) throw new InvalidOperationException("当前副本尚未初始化显式阶段配置。" );
        // 稳定键避免目标段插入或重排造成归属漂移。
        string key = StageKey(stage);
        // 正式目标新增时允许按用户操作创建一个明确空段。
        XElement binding = dungeon.Elements("stage").FirstOrDefault(item => (string)item.Attribute("key") == key);
        if (binding == null)
        {
            if (!linked) return;
            binding = new XElement("stage", new XAttribute("key", key));
            dungeon.Add(binding);
        }
        // 同一阶段引用去重；其他阶段对同一机制的关联独立保留。
        XElement existing = binding.Elements().FirstOrDefault(item => RefersTo(item, node));
        if (linked)
        {
            if (existing != null) return;
            binding.Add(Reference(node));
        }
        else
        {
            if (existing == null) return;
            // 即使手工写入重复项也一次移出当前阶段，不影响其他段。
            XElement[] duplicates = binding.Elements().Where(item => RefersTo(item, node)).ToArray();
            // 数组保留原 XML 元素引用以安全移除。
            for (int index = 0; index < duplicates.Length; index++) duplicates[index].Remove();
        }
        config.IsDirty = true;
    }

    /// <summary>现有高级字段改波次编号后同步显式引用，不代改原运行规则中的其他编号。</summary>
    /// <param name="config">当前正在编辑的场景配置。</param>
    /// <param name="oldId">绘制字段前捕获的原 Wave ID。</param>
    /// <param name="newId">用户修改字段后的新 Wave ID。</param>
    public static void RenameWaveReferences(MSceneEditorConfig config, int oldId, int newId)
    {
        if (oldId == newId) return;
        // 未初始化旧文件不创建虚假的新关联，仅同步已存在配置。
        XElement root = config.Document.Root.Element("editor_stage_bindings");
        if (root == null) return;
        // 同一波次在多个副本/阶段中的已有引用必须保持指向同一实际节点。
        XElement[] references = root.Elements("dungeon").Elements("stage").Elements("wave").ToArray();
        // 只改显式 wave/id，不遍历或重写前置、同盟、事件等原业务数据。
        for (int index = 0; index < references.Length; index++)
        {
            if ((string)references[index].Attribute("id") != oldId.ToString(CultureInfo.InvariantCulture)) continue;
            references[index].SetAttributeValue("id", newId);
            config.IsDirty = true;
        }
    }

    /// <summary>真正删除节点时清理该场景所有副本和阶段的引用，保留空阶段段。</summary>
    /// <param name="config">即将删除原节点的配置。</param>
    /// <param name="node">实际被删除的波次或机制身份。</param>
    public static void RemoveNodeReferences(MSceneEditorConfig config, MSceneEditorNode node)
    {
        // 无绑定的旧文件仍沿用原节点删除，不创建任何新记录。
        XElement root = config.Document.Root.Element("editor_stage_bindings");
        if (root == null) return;
        // 只检查 stage 的直接引用元素，不删除其他扩展或目标段。
        XElement[] references = root.Elements("dungeon").Elements("stage").Elements().ToArray();
        // 一个节点可能被多个阶段共享，真正删除后所有这些引用都应移除。
        for (int index = 0; index < references.Length; index++)
            if (RefersTo(references[index], node)) references[index].Remove();
    }

    /// <summary>取得正式副本的独立绑定段，缺失不会隐式写入。</summary>
    /// <param name="config">当前原 XML 配置。</param>
    /// <param name="dungeonId">正式 DungeonsID。</param>
    private static XElement DungeonElement(MSceneEditorConfig config, int dungeonId)
    {
        // 原根节点保存独立编辑配置，不污染 canvas 或运行 data。
        XElement root = config.Document.Root.Element("editor_stage_bindings");
        if (root == null) return null;
        return root.Elements("dungeon").FirstOrDefault(item => (string)item.Attribute("id") == dungeonId.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>生成具名引用，波次与机制使用各自真实唯一身份。</summary>
    /// <param name="node">当前配置的波次或根触发器。</param>
    private static XElement Reference(MSceneEditorNode node)
    {
        if (node.IsWave) return new XElement("wave", new XAttribute("id", node.WaveId));
        return new XElement("mechanism", new XAttribute("node_id", node.Id));
    }

    /// <summary>判断引用是否指向被操作的同一节点，避免混淆业务编号和画布编号。</summary>
    /// <param name="reference">阶段配置中的一个引用元素。</param>
    /// <param name="node">真实节点。</param>
    private static bool RefersTo(XElement reference, MSceneEditorNode node)
    {
        if (node.IsWave) return reference.Name.LocalName == "wave" && (string)reference.Attribute("id") == node.WaveId.ToString(CultureInfo.InvariantCulture);
        return reference.Name.LocalName == "mechanism" && (string)reference.Attribute("node_id") == node.Id;
    }

    /// <summary>解析持久引用，缺失节点只由调用方报告，不按编号或相邻位置猜测替代对象。</summary>
    /// <param name="config">当前真实节点集合。</param>
    /// <param name="reference">原绑定元素。</param>
    private static MSceneEditorNode Resolve(MSceneEditorConfig config, XElement reference)
    {
        // 不复制节点数据，直接复用现有编辑器对象。
        for (int index = 0; index < config.Nodes.Count; index++)
        {
            // 机制引用必须对应真实触发器，不能把时机或结果伪装成独立机制。
            MSceneEditorNode node = config.Nodes[index];
            if (!node.IsWave && !ROSceneNodePresentation.IsTrigger(node)) continue;
            if (RefersTo(reference, node)) return node;
        }
        return null;
    }
}
