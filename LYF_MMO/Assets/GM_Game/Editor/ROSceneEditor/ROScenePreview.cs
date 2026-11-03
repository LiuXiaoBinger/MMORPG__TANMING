using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>复用原波次/触发器配置的场景编辑预览；临时对象不保存进正式场景，不执行服务器战斗或外部 AI。</summary>
public sealed class ROScenePreview : IDisposable
{
    /// <summary>当前预览所使用的原编辑配置。</summary>
    private readonly MSceneEditorConfig config;
    /// <summary>已经迁入主工程的模型和动作映射。</summary>
    private readonly ROSceneCreatureCatalog catalog;
    /// <summary>临时模型的共同父节点，停止预览时整体销毁。</summary>
    private GameObject root;
    /// <summary>各波次在本轮预览中的创建与完成状态。</summary>
    public readonly List<MSceneWavePreview> Waves = new List<MSceneWavePreview>();
    /// <summary>实际触发过程的可见记录，保留最近 80 条。</summary>
    public readonly List<string> Events = new List<string>();
    /// <summary>需要等原 Timer 毫秒延迟后再执行的结果。</summary>
    private readonly List<MScenePreviewResult> timedResults = new List<MScenePreviewResult>();
    /// <summary>预览中被创建的区域或空气墙对象编号。</summary>
    public readonly HashSet<int> VisibleObjects = new HashSet<int>();
    /// <summary>预览角色当前所在位置，用于区域进入和传送验证。</summary>
    public Vector3 PlayerPosition = new Vector3(119.5f, 62f, 118.3f);
    /// <summary>上一帧已经进入的区域编号，用于检测进入/离开事件。</summary>
    private readonly HashSet<int> insideObjects = new HashSet<int>();
    /// <summary>是否正在推进编辑预览时间。</summary>
    public bool Running { get; private set; }
    /// <summary>波次、区域或事件变化时通知编辑窗口重绘。</summary>
    public Action Changed;

    /// <summary>绑定当前编辑文档和已迁移资源目录。</summary>
    /// <param name="document">当前打开的 RO 编辑配置。</param>
    /// <param name="creatures">本项目的怪物模型与动作映射。</param>
    public ROScenePreview(MSceneEditorConfig document, ROSceneCreatureCatalog creatures)
    {
        config = document;
        catalog = creatures;
    }

    /// <summary>按原自动波次条件开始预览，创建临时表现根节点。</summary>
    public void Start()
    {
        Dispose();
        config.SynchronizePreWaves();
        root = new GameObject("[RO 场景编辑预览]");
        root.hideFlags = HideFlags.DontSave;
        Events.Clear();
        Waves.Clear();
        VisibleObjects.Clear();
        insideObjects.Clear();
        for (int i = 0; i < config.Nodes.Count; i++) if (config.Nodes[i].IsWave) Waves.Add(new MSceneWavePreview { Node = config.Nodes[i] });
        for (int i = 0; i < config.TriggerObjects.Length; i++)
        {
            // ignore_init 表示等待机制显式创建该区域或空气墙。
            XElement data = config.TriggerObjects[i];
            if (ROSceneXml.Text(data, "ignore_init") != "true") VisibleObjects.Add(ROSceneXml.Int(data, "id"));
        }
        Running = true;
        EditorApplication.update += Tick;
        Log("开始编辑预览；波次、区域和事件使用当前配置。AI/Lua 调用保留为事件记录。");
        TriggerTiming("SSttimingTypeInit", -1);
        TriggerTiming("SSttimingTypeEnterDungeons", -1);
        Tick();
    }

    /// <summary>只查看一个指定波次，不要求先运行整个副本流程。</summary>
    /// <param name="node">选中的波次节点。</param>
    public void ShowWave(MSceneEditorNode node)
    {
        if (!Running) Start();
        // 同一个波次只保留一份预览表现。
        MSceneWavePreview wave = FindWave(node.WaveId);
        ClearInstances(wave);
        wave.Completed = false;
        wave.AlivePercent = 100;
        wave.HealthPercent = 100;
        wave.Requested = true;
        SpawnWave(wave, true);
    }

    /// <summary>从事件输入框发送原 AI/触发器事件，激活等待该事件的波次。</summary>
    /// <param name="eventName">原 pre_scene_ai_event 使用的精确事件名。</param>
    public void SendEvent(string eventName)
    {
        if (!Running) Start();
        Log("事件：" + eventName);
        for (int i = 0; i < Waves.Count; i++)
        {
            // AI 与触发器事件都按原配置中的名称精确匹配。
            MSceneWavePreview wave = Waves[i];
            string option = ROSceneXml.Text(wave.Node.Data, "base_info/pre_wave_option");
            if (option != "PreWaveOptionTypeWaitEvent" && option != "PreWaveOptionTypeTriggerEvent") continue;
            if (ROSceneXml.Text(wave.Node.Data, "base_info/pre_scene_ai_event") == eventName) RequestWave(wave);
        }
    }

    /// <summary>标记选中波次的预览完成，以检查后续波次的连线和延迟。</summary>
    /// <param name="id">要完成的波次业务编号。</param>
    public void CompleteWave(int id)
    {
        if (!Running) Start();
        // 该状态只属于编辑器预览，不产生战斗伤害或游戏奖励。
        MSceneWavePreview wave = FindWave(id);
        if (wave == null) return;
        wave.Completed = true;
        wave.Spawning = false;
        wave.AlivePercent = 0;
        wave.HealthPercent = 0;
        ClearInstances(wave);
        Log("Wave " + id + "：预览完成");
        // 沿用原 Wave 结束后的场景 AI 事件字段。
        string eventName = ROSceneXml.Text(wave.Node.Data, "base_info/end_scene_ai_event");
        if (eventName.Length > 0) SendEvent(eventName);
    }

    /// <summary>按连接预览一个触发器、触发组或单独的结果节点。</summary>
    /// <param name="node">用户选中模拟触发的机制节点。</param>
    public void ExecuteNode(MSceneEditorNode node)
    {
        if (!Running) Start();
        if (node.Data == null) return;
        if (node.Data.Element("result_type") != null) { ExecuteResult(node); return; }
        if (node.Data.Element("timing_type") != null) { ExecuteTiming(node); return; }
        // 触发器组只调用其直接关联的触发器，不遍历任意图路径。
        List<MSceneEditorNode> triggers = Linked(node, "triggerKnob");
        for (int i = 0; i < triggers.Count; i++) ExecuteTrigger(triggers[i]);
        if (triggers.Count == 0) ExecuteTrigger(node);
    }

    /// <summary>播放所选波次模型的原版动作。</summary>
    /// <param name="waveId">波次业务编号。</param>
    /// <param name="clip">已经迁入主工程的动画片段。</param>
    public void PlayAnimation(int waveId, AnimationClip clip)
    {
        // 动作采样仅作用于临时实例。
        MSceneWavePreview wave = FindWave(waveId);
        if (wave == null) return;
        for (int i = 0; i < wave.Instances.Count; i++)
        {
            wave.Instances[i].Clip = clip;
            wave.Instances[i].StartedAt = EditorApplication.timeSinceStartup;
        }
    }

    /// <summary>按波次编号查询本轮预览状态。</summary>
    /// <param name="id">原波次编号。</param>
    public MSceneWavePreview FindWave(int id)
    {
        for (int i = 0; i < Waves.Count; i++) if (Waves[i].Node.WaveId == id) return Waves[i];
        return null;
    }

    /// <summary>每个编辑器更新周期推进延迟、模型动作和区域触发。</summary>
    private void Tick()
    {
        if (!Running) return;
        // 当前编辑器时间用于毫秒延迟转换和原动画采样。
        double now = EditorApplication.timeSinceStartup;
        for (int i = 0; i < Waves.Count; i++)
        {
            // 当前波次的预览状态由原前置条件决定。
            MSceneWavePreview wave = Waves[i];
            if (!wave.Requested && CanStart(wave)) RequestWave(wave);
            if (wave.Requested && !wave.Spawned && now >= wave.DueTime) SpawnWave(wave);
            else if (wave.Spawning && now >= wave.NextSpawnTime) SpawnWave(wave);
            for (int p = 0; p < wave.Instances.Count; p++)
            {
                // 动作来自复制的原 AnimationClip，没有替代模型或重定向骨骼。
                MScenePreviewInstance instance = wave.Instances[p];
                if (instance.Clip == null || instance.GameObject == null) continue;
                instance.Clip.SampleAnimation(instance.GameObject, (float)(now - instance.StartedAt) % Mathf.Max(0.01f, instance.Clip.length));
            }
        }
        for (int i = timedResults.Count - 1; i >= 0; i--)
        {
            if (timedResults[i].DueTime > now) continue;
            // 先移除到期动作，再执行它的机制结果。
            MSceneEditorNode node = timedResults[i].Node;
            timedResults.RemoveAt(i);
            ExecuteResult(node);
        }
        CheckRegions();
        SceneView.RepaintAll();
    }

    /// <summary>复用 RO None/And/Or 前置语义，事件型波次等待显式事件输入。</summary>
    /// <param name="wave">尚未请求创建的波次。</param>
    private bool CanStart(MSceneWavePreview wave)
    {
        if (ROSceneXml.Text(wave.Node.Data, "base_info/not_active_create") == "true") return false;
        // 前置类型是原 XML 枚举文本。
        string option = ROSceneXml.Text(wave.Node.Data, "base_info/pre_wave_option");
        if (option == "PreWaveOptionTypeNone") return true;
        if (option != "PreWaveOptionTypeAnd" && option != "PreWaveOptionTypeOr") return false;
        // 前置列表由原端口连接同步，空列表不构造额外等待条件。
        XElement[] previous = wave.Node.Data.Element("base_info").Element("pre_wave_list").Elements().ToArray();
        bool any = false;
        for (int i = 0; i < previous.Length; i++)
        {
            // 沿用 RO CheckWaveProcess：-1 忽略该项，其他阈值比较剩余数量与血量百分比。
            MSceneWavePreview dependency = FindWave(ROSceneXml.Int(previous[i], "pre_wave_id"));
            bool fulfilled = dependency != null && dependency.Spawned;
            // 两个百分比条件分别保留原短路顺序。
            int countLimit = ROSceneXml.Int(previous[i], "pre_wave_num", -1);
            int healthLimit = ROSceneXml.Int(previous[i], "pre_wave_blood", -1);
            if (fulfilled && countLimit != -1) fulfilled = dependency.AlivePercent <= countLimit;
            if (fulfilled && healthLimit != -1) fulfilled = dependency.HealthPercent <= healthLimit;
            if (option == "PreWaveOptionTypeAnd" && !fulfilled) return false;
            any = any || fulfilled;
        }
        if (option == "PreWaveOptionTypeAnd") return true;
        return any;
    }

    /// <summary>按原毫秒延迟登记一次波次创建。</summary>
    /// <param name="wave">已满足前置条件的波次。</param>
    private void RequestWave(MSceneWavePreview wave)
    {
        if (wave.Requested) return;
        wave.Requested = true;
        wave.DueTime = EditorApplication.timeSinceStartup + ROSceneXml.Int(wave.Node.Data, "base_info/delay_create_time") * 0.001;
    }

    /// <summary>按照原刷点位置、朝向、数量和模型比例生成临时模型。</summary>
    /// <param name="wave">需要展示的原波次。</param>
    /// <param name="forceVisible">用户单独查看该波次时显示原本不可见的真实模型。</param>
    private void SpawnWave(MSceneWavePreview wave, bool forceVisible = false)
    {
        wave.Spawned = true;
        wave.Spawning = false;
        if (wave.Node.Data.Element("trigger_group_event_info") != null)
        {
            // 原魔神塔 Wave 62 在前置怪物结束后向第 12 组发送波次事件。
            TriggerGroupTiming("MSSceneTriggerGroupData", ROSceneXml.Int(wave.Node.Data, "trigger_group_event_info/group_id"), "SSttimingTypeWaveEvent");
            wave.Completed = true;
            wave.AlivePercent = 0;
            wave.HealthPercent = 0;
            Log("Wave " + wave.Node.WaveId + "：已发送原触发器组波次事件");
            return;
        }
        if (!forceVisible && ROSceneXml.Text(wave.Node.Data, "base_info/init_invisible") == "true")
        {
            Log("Wave " + wave.Node.WaveId + "：按原配置保持出生不可见。");
            return;
        }
        // 此处只创建可见怪物；原不可见控制实体保留为节点状态。
        int entityId = ROSceneXml.Int(wave.Node.Data, "monster_info/entity_id", -1);
        ROSceneCreatureEntity entity = catalog.FindEntity(entityId);
        if (entity == null) { Log("Wave " + wave.Node.WaveId + "：配置点位预览（NPC/采集/事件节点）。"); return; }
        if (entity.modelName == "Monster_Empty") { Log("Wave " + wave.Node.WaveId + "：原版不可见控制实体 " + entity.displayName); return; }
        // 已迁入的模型必须存在；缺少的换装人物不以其他模型冒充。
        ROSceneCreatureModel model = catalog.FindModel(entity.modelName);
        if (model == null) { Log("Wave " + wave.Node.WaveId + "：" + entity.displayName + " 当前只有原刷点，测试工程未提供其换装模型。"); return; }
        // 从主工程直接实例化已复制的预制体。
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model.prefabPath);
        XElement[] points = ROSceneXml.SpawnPoints(wave.Node.Data);
        // real_num 由 RO 用于限制实际创建数量，未配置时使用全部刷点数量。
        int available = 0;
        for (int i = 0; i < points.Length; i++) available += ROSceneXml.Int(points[i], "count", 1);
        int limit = ROSceneXml.Int(wave.Node.Data, "monster_info/monster_base_data/entity_info/real_num", available);
        limit = Mathf.Clamp(limit, 0, available);
        // 原间隔刷怪配置以秒为单位；单波查看时立即显示所有配置模型。
        int batchEnd = limit;
        XElement rush = ROSceneXml.Find(wave.Node.Data, "base_info/interval_rush_data");
        if (rush != null && !forceVisible) batchEnd = Math.Min(limit, wave.Instances.Count + Mathf.Max(1, ROSceneXml.Int(rush, "count", 1)));
        // 全部刷点展开后的顺序索引，已生成的批次不再重复实例化。
        int spawnIndex = 0;
        int previousCount = wave.Instances.Count;
        for (int i = 0; i < points.Length; i++)
        {
            for (int count = 0; count < ROSceneXml.Int(points[i], "count", 1); count++)
            {
                if (wave.Instances.Count >= batchEnd) break;
                if (spawnIndex++ < previousCount) continue;
                // 位置仍使用原刷点；出生半径通过场景线框显示，预览保持确定性。
                GameObject instance = UnityEngine.Object.Instantiate(prefab, root.transform);
                instance.name = "Wave " + wave.Node.WaveId + " · " + entity.displayName;
                instance.transform.position = ROSceneXml.Vector(points[i], "position");
                instance.transform.rotation = Quaternion.Euler(0f, ROSceneXml.Float(points[i], "face"), 0f);
                instance.transform.localScale *= entity.scale;
                // 所有子物体都标记为不保存，保证预览不会污染正式场景。
                Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++) transforms[t].gameObject.hideFlags = HideFlags.DontSave;
                // 保留原 Animator 骨骼，由编辑器直接采样动画片段。
                AnimationClip clip = FindClip(model, entity.idleAnimation);
                wave.Instances.Add(new MScenePreviewInstance { GameObject = instance, Clip = clip, StartedAt = EditorApplication.timeSinceStartup });
            }
        }
        wave.Spawning = wave.Instances.Count < limit;
        if (wave.Spawning) wave.NextSpawnTime = EditorApplication.timeSinceStartup + Mathf.Max(0, ROSceneXml.Float(rush, "interval"));
        Log("Wave " + wave.Node.WaveId + "：已创建 " + wave.Instances.Count + "/" + limit + " 个 " + entity.displayName);
    }

    /// <summary>按 RO 动作文件名选取待机片段。</summary>
    /// <param name="model">当前模型目录条目。</param>
    /// <param name="name">PresentTable 中的动作名。</param>
    private static AnimationClip FindClip(ROSceneCreatureModel model, string name)
    {
        for (int i = 0; i < model.animations.Length; i++)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(model.animations[i].clipPath) == name) return AssetDatabase.LoadAssetAtPath<AnimationClip>(model.animations[i].clipPath);
        }
        return null;
    }

    /// <summary>检测预览角色进入/离开原箱形区域；旋转、偏移和尺寸都来自原配置。</summary>
    private void CheckRegions()
    {
        for (int i = 0; i < config.TriggerObjects.Length; i++)
        {
            // 区域对象保存原 ID 和空间参数。
            XElement area = config.TriggerObjects[i];
            int id = ROSceneXml.Int(area, "id");
            if (!VisibleObjects.Contains(id)) continue;
            Vector3 center = ROSceneXml.Vector(area, "position");
            Quaternion rotation = Quaternion.Euler(0f, ROSceneXml.Float(area, "face"), 0f);
            Vector3 local = Quaternion.Inverse(rotation) * (PlayerPosition - center) - ROSceneXml.Vector(area, "box/offset");
            Vector3 size = ROSceneXml.Vector(area, "box/scale");
            bool inside = Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f;
            if (inside && insideObjects.Add(id)) TriggerTiming("SSttimingTypeServerEnter", id);
            if (!inside && insideObjects.Remove(id)) TriggerTiming("SSttimingTypeServerLeave", id);
        }
    }

    /// <summary>从实际区域变化触发连接到相应时机的原触发器。</summary>
    /// <param name="type">原 timing_type 枚举名称。</param>
    /// <param name="objectId">区域 ID；负数表示无区域限制。</param>
    public void TriggerTiming(string type, int objectId)
    {
        for (int i = 0; i < config.Nodes.Count; i++)
        {
            // 只匹配原服务端时机字段，避免重复触发客户端表现节点。
            MSceneEditorNode node = config.Nodes[i];
            if (ROSceneXml.Text(node.Data, "timing_type") != type) continue;
            if (objectId >= 0 && ROSceneXml.Int(node.Data, "scene_trigger_obj_id") != objectId) continue;
            // 输入连线指向拥有该时机的触发器。
            ExecuteTiming(node);
        }
    }

    /// <summary>时机满足后检查其条件端口，再执行所属触发器。</summary>
    /// <param name="timing">RO 原时机节点；条件实际连接在该节点上。</param>
    private void ExecuteTiming(MSceneEditorNode timing)
    {
        // 本副本的战斗状态与实体检查条件依赖服务器，不能假定通过。
        if (Linked(timing, "conditionKnob").Count > 0)
        {
            Log(timing.Title + "：包含外部战斗条件，请选择结果节点单独预览。");
            return;
        }
        // 原 inKnob 连接拥有该时机的触发器。
        List<MSceneEditorNode> owners = Linked(timing, "inKnob");
        for (int i = 0; i < owners.Count; i++) ExecuteTrigger(owners[i]);
    }

    /// <summary>只触发原指定组内匹配的时机，不把同组其他时机当成已经发生。</summary>
    /// <param name="groupType">原客户端或服务端触发器组数据类型。</param>
    /// <param name="groupId">原触发器组编号。</param>
    /// <param name="timingType">这次派发的原时机枚举文本。</param>
    private void TriggerGroupTiming(string groupType, int groupId, string timingType)
    {
        for (int i = 0; i < config.Nodes.Count; i++)
        {
            // 两侧触发器组分别编号，必须先区分原数据类型。
            MSceneEditorNode group = config.Nodes[i];
            if (group.Data == null || group.Data.Name.LocalName != groupType) continue;
            if (ROSceneXml.Int(group.Data, "id") != groupId) continue;
            List<MSceneEditorNode> triggers = Linked(group, "triggerKnob");
            for (int t = 0; t < triggers.Count; t++)
            {
                // 原 timingKnob 只连接本触发器的时机节点。
                List<MSceneEditorNode> timings = Linked(triggers[t], "timingKnob");
                for (int n = 0; n < timings.Count; n++)
                {
                    if (ROSceneXml.Text(timings[n].Data, "timing_type") == timingType) ExecuteTiming(timings[n]);
                }
            }
        }
    }

    /// <summary>获取原端口直接相连的节点，保留连接方向的业务含义。</summary>
    /// <param name="node">起点节点。</param>
    /// <param name="portName">原端口的精确名称。</param>
    private List<MSceneEditorNode> Linked(MSceneEditorNode node, string portName)
    {
        // 仅收集一个端口的邻接节点，不运行通用图搜索。
        List<MSceneEditorNode> result = new List<MSceneEditorNode>();
        for (int i = 0; i < config.Connections.Count; i++)
        {
            // 两个端口在文件中的顺序不代表输入输出方向。
            MSceneEditorConnection connection = config.Connections[i];
            if (connection.First.Owner == node && connection.First.Name == portName) result.Add(connection.Second.Owner);
            if (connection.Second.Owner == node && connection.Second.Name == portName) result.Add(connection.First.Owner);
        }
        return result;
    }

    /// <summary>按原 result_index 顺序执行触发器结果，Timer 累积为真实预览延迟。</summary>
    /// <param name="trigger">原触发器节点。</param>
    private void ExecuteTrigger(MSceneEditorNode trigger)
    {
        if (ROSceneXml.Text(trigger.Data, "enable", "true") == "false") return;
        // 直接点击触发器是策划的手动触发，条件是否满足仍由各时机入口检查。
        List<MSceneEditorNode> results = Linked(trigger, "resultKnob");
        results.Sort((first, second) => ROSceneXml.Int(first.Data, "result_index").CompareTo(ROSceneXml.Int(second.Data, "result_index")));
        // 各 Timer 的毫秒延迟按原顺序作用于后续结果。
        double delay = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (ROSceneXml.Text(results[i].Data, "result_type") == "SSTResultTypeTimer") { delay += ROSceneXml.Int(results[i].Data, "timer_info/delay_time") * 0.001; continue; }
            if (delay > 0) timedResults.Add(new MScenePreviewResult { Node = results[i], DueTime = EditorApplication.timeSinceStartup + delay });
            else ExecuteResult(results[i]);
        }
    }

    /// <summary>预览本次副本使用的波次事件、区域开关和传送结果；外部业务保持可见记录。</summary>
    /// <param name="node">原触发结果节点。</param>
    private void ExecuteResult(MSceneEditorNode node)
    {
        // 枚举和目标编号直接读取当前可编辑配置。
        string type = ROSceneXml.Text(node.Data, "result_type");
        int objectId = ROSceneXml.Int(node.Data, "scene_trigger_obj_id");
        if (type == "SSTResultTypeDestroyObj") VisibleObjects.Remove(objectId);
        else if (type == "SSTResultTypeCreateObj" || type == "SSTResultTypeAirWall") VisibleObjects.Add(objectId);
        else if (type == "SSTResultTypeTransPosition") PlayerPosition = ROSceneXml.Vector(node.Data, "trans_position_info/position");
        else if (type == "SSTResultTypeWaveEvent") SendEvent(ROSceneXml.Text(node.Data, "wave_event_info/wave_event"));
        else if (type == "SSTResultTypeSceneAIEvent") Log("场景 AI 入口：" + ROSceneXml.Text(node.Data, "scene_ai_event/ai_event") + "（外部 AI 未在编辑预览执行）");
        else if (type == "SSTResultTypeToClientGroup")
        {
            // 服务端结果指定的客户端组用于相同机制的表现节点。
            int groupId = ROSceneXml.Int(node.Data, "trigger_client_group_info/group_id");
            TriggerGroupTiming("MCSceneTriggerGroupData", groupId, "CSttimingTypeFromServer");
        }
        else Log("机制配置：" + node.Title + "（当前预览保留调用记录）");
        Log("执行：" + node.Title + " / 区域 " + objectId);
    }

    /// <summary>释放指定波次的临时模型。</summary>
    /// <param name="wave">要重新预览或标记完成的波次。</param>
    private static void ClearInstances(MSceneWavePreview wave)
    {
        for (int i = 0; i < wave.Instances.Count; i++) if (wave.Instances[i].GameObject != null) UnityEngine.Object.DestroyImmediate(wave.Instances[i].GameObject);
        wave.Instances.Clear();
    }

    /// <summary>追加一条可见预览记录，并通知窗口重绘。</summary>
    /// <param name="message">实际执行或未执行的机制说明。</param>
    private void Log(string message)
    {
        Events.Add(message);
        if (Events.Count > 80) Events.RemoveAt(0);
        if (Changed != null) Changed();
    }

    /// <summary>停止更新并移除全部 DontSave 对象，正常场景内容保持原样。</summary>
    public void Dispose()
    {
        Running = false;
        EditorApplication.update -= Tick;
        timedResults.Clear();
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        root = null;
        for (int i = 0; i < Waves.Count; i++) Waves[i].Instances.Clear();
        SceneView.RepaintAll();
    }
}
