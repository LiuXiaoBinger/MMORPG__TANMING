using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>RO 场景/副本编辑器的 LYF 接入窗口，沿用原画布与 XML 字段，提供刷点编辑和机制预览。</summary>
public sealed class ROSceneEditorWindow : EditorWindow
{
    /// <summary>本次迁入并配置的魔神塔简单难度编辑文件。</summary>
    public const string DefaultConfigPath = "Assets/GM_Game/Resources/Dungeons/Moshenta/RO_DaNaTuoSiZhiZhan_EZ.txt";
    /// <summary>已存在于主工程的魔神塔正式场景。</summary>
    public const string ScenePath = "Assets/GM_Game/Scenes/RODungeon_Moshenta/sc_dungeon_moshenta_001.unity";
    /// <summary>当前文件路径，窗口重载后恢复。</summary>
    [SerializeField] private string currentPath = DefaultConfigPath;
    /// <summary>左侧列表与画布使用的类别：波次、机制、区域。</summary>
    [SerializeField] private int tab;
    /// <summary>当前列表的节点名称或编号筛选文本。</summary>
    [SerializeField] private string search = "";
    /// <summary>画布平移量，单位为窗口像素。</summary>
    [SerializeField] private Vector2 pan;
    /// <summary>原画布到窗口像素的缩放比例。</summary>
    [SerializeField] private float zoom = 0.5f;
    /// <summary>左侧节点列表滚动位置。</summary>
    private Vector2 listScroll;
    /// <summary>右侧属性面板滚动位置。</summary>
    private Vector2 inspectorScroll;
    /// <summary>预览记录面板的滚动位置。</summary>
    private Vector2 eventScroll;
    /// <summary>当前完整 RO 编辑配置。</summary>
    private MSceneEditorConfig config;
    /// <summary>当前选中的原图节点。</summary>
    private MSceneEditorNode selected;
    /// <summary>当前选中的场景区域原数据。</summary>
    private XElement selectedArea;
    /// <summary>所选波次中正通过场景手柄编辑的刷点索引。</summary>
    private int pointIndex;
    /// <summary>怪物模型与动作资源目录。</summary>
    private ROSceneCreatureCatalog catalog;
    /// <summary>原 XML 属性控件及其展开状态。</summary>
    private readonly ROSceneFieldGUI fields = new ROSceneFieldGUI();
    /// <summary>当前窗口拥有的临时场景预览。</summary>
    private ROScenePreview preview;
    /// <summary>用户首先点选、等待接入另一个端口的起点。</summary>
    private MSceneEditorPort pendingPort;
    /// <summary>鼠标正在拖动的节点。</summary>
    private MSceneEditorNode dragging;
    /// <summary>当前是否通过中键或背景拖动画布。</summary>
    private bool panning;
    /// <summary>预览输入的原 AI/波次事件名。</summary>
    private string eventName = "SummonDemon";
    /// <summary>当前画布在窗口中的矩形。</summary>
    private Rect graphRect;
    /// <summary>是否绘制全部原始区域轮廓。</summary>
    private bool showAreas = true;

    /// <summary>保留两个 RO 原菜单入口并添加本项目中文入口。</summary>
    [MenuItem("TreeEditor/OpenSceneEditor %l")]
    [MenuItem("ROTools/OpenSceneEditor")]
    [MenuItem("LYF/副本/RO场景编辑器")]
    public static void OpenSceneEditor()
    {
        // 窗口由 Unity 管理生命周期，所有预览对象由该窗口清理。
        ROSceneEditorWindow window = GetWindow<ROSceneEditorWindow>();
        window.titleContent = new GUIContent("RO 副本编辑器");
        window.minSize = new Vector2(1100f, 650f);
        window.Show();
    }

    /// <summary>恢复配置并注册场景手柄绘制。</summary>
    private void OnEnable()
    {
        titleContent = new GUIContent("RO 副本编辑器");
        minSize = new Vector2(1100f, 650f);
        saveChangesMessage = "RO 副本配置有未保存修改，是否保存？";
        SceneView.duringSceneGui += DuringSceneGUI;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (File.Exists(currentPath)) LoadConfig(currentPath);
    }

    /// <summary>关闭窗口或重编译时释放所有预览对象和回调。</summary>
    private void OnDisable()
    {
        StopPreview();
        SceneView.duringSceneGui -= DuringSceneGUI;
        AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    /// <summary>正式运行模式开始前停止编辑器预览。</summary>
    /// <param name="state">Unity 当前运行模式切换阶段。</param>
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) StopPreview();
    }

    /// <summary>打开原 XML，重建画布但不重写文件。</summary>
    /// <param name="path">主工程配置文件。</param>
    public void LoadConfig(string path)
    {
        StopPreview();
        config = MSceneEditorConfig.LoadFromFile(path);
        currentPath = path;
        catalog = ROSceneCreatureCatalog.Load();
        selected = null;
        selectedArea = null;
        hasUnsavedChanges = false;
        FitGraph();
        Repaint();
    }

    /// <summary>保存原 XML 并让 Unity 导入新配置。</summary>
    public override void SaveChanges()
    {
        if (config != null)
        {
            config.SaveToFile(currentPath);
            AssetDatabase.ImportAsset(currentPath);
        }
        base.SaveChanges();
    }

    /// <summary>绘制工具栏、节点列表、原画布和属性面板。</summary>
    private void OnGUI()
    {
        DrawToolbar();
        if (config == null) return;
        // 两侧面板固定宽度，中央留给原节点图。
        float listWidth = 230f;
        float inspectorWidth = 350f;
        float top = 50f;
        float height = position.height - top;
        graphRect = new Rect(listWidth, top, position.width - listWidth - inspectorWidth, height);
        GUILayout.BeginArea(new Rect(0, top, listWidth, height), EditorStyles.helpBox);
        DrawList();
        GUILayout.EndArea();
        DrawGraph();
        GUILayout.BeginArea(new Rect(position.width - inspectorWidth, top, inspectorWidth, height), EditorStyles.helpBox);
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
        DrawInspector();
        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
        hasUnsavedChanges = config.IsDirty;
    }

    /// <summary>绘制打开、保存、场景和预览控制入口。</summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("打开配置", EditorStyles.toolbarButton, GUILayout.Width(75)))
        {
            // Unity 原生文件框只负责选择待编辑文件。
            string path = EditorUtility.OpenFilePanel("打开 RO 场景编辑配置", Path.GetDirectoryName(DefaultConfigPath), "txt");
            if (path.Length > 0 && ConfirmDiscard()) LoadConfig(path);
        }
        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50))) SaveChanges();
        if (GUILayout.Button("打开魔神塔", EditorStyles.toolbarButton, GUILayout.Width(85))) OpenDungeonScene();
        if (GUILayout.Button("适配画布", EditorStyles.toolbarButton, GUILayout.Width(75))) FitGraph();
        GUILayout.Space(12);
        if (GUILayout.Button("开始流程预览", EditorStyles.toolbarButton, GUILayout.Width(95))) StartPreview();
        if (GUILayout.Button("停止预览", EditorStyles.toolbarButton, GUILayout.Width(75))) StopPreview();
        showAreas = GUILayout.Toggle(showAreas, "触发区域", EditorStyles.toolbarButton, GUILayout.Width(75));
        GUILayout.FlexibleSpace();
        if (config != null) GUILayout.Label(config.Nodes.Count + " 节点 / " + config.Connections.Count + " 连线 / " + config.TriggerObjects.Length + " 区域");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("事件", GUILayout.Width(30));
        eventName = GUILayout.TextField(eventName, GUILayout.MinWidth(120));
        if (GUILayout.Button("发送到预览", EditorStyles.toolbarButton, GUILayout.Width(90))) { EnsurePreview(); if (preview != null) preview.SendEvent(eventName); }
        GUILayout.Label("滚轮缩放 · 背景拖动 · 选节点编辑 · 点两个端口连线", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>显示波次、机制和区域的可搜索列表。</summary>
    private void DrawList()
    {
        tab = GUILayout.Toolbar(tab, new[] { "波次", "机制", "区域" });
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        if (tab == 2)
        {
            for (int i = 0; i < config.TriggerObjects.Length; i++)
            {
                // 原区域标题和 ID 便于与机制结果关联。
                XElement area = config.TriggerObjects[i];
                string title = "区域 " + ROSceneXml.Int(area, "id") + " · " + ROSceneXml.Text(area, "box/title");
                if (!Matches(title)) continue;
                if (GUILayout.Button(title, EditorStyles.miniButton)) { selectedArea = area; selected = null; FramePosition(ROSceneXml.Vector(area, "position")); }
            }
        }
        else
        {
            for (int i = 0; i < config.Nodes.Count; i++)
            {
                // 列表按原节点顺序展示，避免改变保存数据。
                MSceneEditorNode node = config.Nodes[i];
                if (tab == 0 && !node.IsWave) continue;
                if (tab == 1 && node.IsWave) continue;
                string label = node.Title;
                if (!Matches(label + " " + node.Content)) continue;
                if (GUILayout.Button(label, EditorStyles.miniButton)) SelectNode(node);
            }
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("新增怪物波次"))
        {
            StopPreview();
            // 沿用当前文档中的原怪物节点模板，不发明新的序列化格式。
            MSceneEditorNode template = config.Nodes.First(node => node.TypeName == "MSceneWaveMonsterNode");
            SelectNode(config.DuplicateNode(template));
        }
        if (selected != null && GUILayout.Button("复制所选节点")) { StopPreview(); SelectNode(config.DuplicateNode(selected)); }
        if (selected != null && GUILayout.Button("删除所选节点")) { StopPreview(); config.RemoveNode(selected); selected = null; }
        EditorGUILayout.HelpBox("模型来自原测试工程。完整 RO 节点与字段保留；流程预览不运行服务器战斗和外部 AI/Lua。", MessageType.Info);
    }

    /// <summary>筛选名称或备注中包含输入文本的条目。</summary>
    /// <param name="value">节点标题和备注。</param>
    private bool Matches(string value)
    {
        return value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>选中节点并将其移动到画布可见中心。</summary>
    /// <param name="node">所选原画布节点。</param>
    public void SelectNode(MSceneEditorNode node)
    {
        selected = node;
        selectedArea = null;
        pointIndex = 0;
        zoom = Mathf.Max(zoom, 0.55f);
        pan = new Vector2(Mathf.Max(160f, graphRect.width) * 0.5f, 180f) - (node.Position + new Vector2(105f, 40f)) * zoom;
        Repaint();
        SceneView.RepaintAll();
    }

    /// <summary>绘制原节点位置、端口和连线，并处理拖动交互。</summary>
    private void DrawGraph()
    {
        GUI.BeginClip(graphRect);
        EditorGUI.DrawRect(new Rect(0, 0, graphRect.width, graphRect.height), new Color(0.10f, 0.12f, 0.15f));
        Handles.BeginGUI();
        for (int i = 0; i < config.Connections.Count; i++)
        {
            // 端口连线使用原图的真实关系。
            MSceneEditorConnection connection = config.Connections[i];
            Vector2 first = PortPosition(connection.First);
            Vector2 second = PortPosition(connection.Second);
            Handles.DrawBezier(first, second, first + Vector2.right * 45f * zoom, second - Vector2.right * 45f * zoom, new Color(0.42f, 0.68f, 0.87f), null, 1.5f);
        }
        if (pendingPort != null) Handles.DrawLine(PortPosition(pendingPort), Event.current.mousePosition);
        Handles.EndGUI();
        for (int i = 0; i < config.Nodes.Count; i++) DrawNode(config.Nodes[i]);
        HandleGraphInput();
        GUI.EndClip();
    }

    /// <summary>绘制单个节点的标题、备注和可连接端口。</summary>
    /// <param name="node">原节点数据。</param>
    private void DrawNode(MSceneEditorNode node)
    {
        // 所有节点仍使用原坐标，缩放只影响编辑窗口显示。
        Rect rect = NodeRect(node);
        if (!rect.Overlaps(new Rect(0, 0, graphRect.width, graphRect.height))) return;
        Color color = new Color(0.18f, 0.22f, 0.28f);
        if (node.IsWave) color = new Color(0.18f, 0.32f, 0.27f);
        if (selected == node) color = new Color(0.26f, 0.44f, 0.62f);
        EditorGUI.DrawRect(rect, color);
        // 小缩放时保留节点轮廓，大缩放时显示完整可读信息。
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = Color.white;
        style.fontSize = Mathf.Max(7, Mathf.RoundToInt(11f * zoom));
        GUI.Label(new Rect(rect.x + 7f * zoom, rect.y + 3f * zoom, rect.width - 14f * zoom, 22f * zoom), node.Title, style);
        GUI.Label(new Rect(rect.x + 7f * zoom, rect.y + 25f * zoom, rect.width - 14f * zoom, 18f * zoom), node.Content, style);
        for (int i = 0; i < node.Ports.Count; i++)
        {
            // 原端口名用作连接语义提示，不重新命名序列化字段。
            MSceneEditorPort port = node.Ports[i];
            Vector2 position = PortPosition(port);
            Rect handle = new Rect(position.x - 5f, position.y - 5f, 10f, 10f);
            EditorGUI.DrawRect(handle, new Color(0.75f, 0.85f, 0.96f));
            if (zoom > 0.45f)
            {
                // 输入端口标签从左侧开始，输出标签靠右。
                float left = rect.x + 12f * zoom;
                if (!port.IsInput) left = rect.x + 80f * zoom;
                GUI.Label(new Rect(left, position.y - 8f * zoom, 135f * zoom, 18f * zoom), port.Name, style);
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && handle.Contains(Event.current.mousePosition))
            {
                if (pendingPort == null) pendingPort = port;
                else { StopPreview(); config.Connect(pendingPort, port); pendingPort = null; selected = null; }
                Event.current.Use();
                Repaint();
                return;
            }
        }
    }

    /// <summary>返回窗口坐标中的节点边框。</summary>
    /// <param name="node">原节点。</param>
    private Rect NodeRect(MSceneEditorNode node)
    {
        return new Rect(node.Position * zoom + pan, new Vector2(220f, 58f + node.Ports.Count * 18f) * zoom);
    }

    /// <summary>计算一个原端口在当前画布中的位置。</summary>
    /// <param name="port">要定位的端口。</param>
    private Vector2 PortPosition(MSceneEditorPort port)
    {
        // 同节点端口按文件顺序排列。
        Rect rect = NodeRect(port.Owner);
        float x = rect.xMax;
        if (port.IsInput) x = rect.xMin;
        return new Vector2(x, rect.y + (54f + port.Owner.Ports.IndexOf(port) * 18f) * zoom);
    }

    /// <summary>响应缩放、平移、选中和原节点坐标拖动。</summary>
    private void HandleGraphInput()
    {
        // 事件坐标已由 BeginClip 转换到画布局部空间。
        Event input = Event.current;
        // 背景交互仅消费画布内事件，侧栏滚动和文本编辑保持原 Unity 控件行为。
        bool inside = new Rect(0, 0, graphRect.width, graphRect.height).Contains(input.mousePosition);
        if (!inside && dragging == null && !panning) return;
        if (input.type == EventType.ScrollWheel)
        {
            // 以鼠标所在画布点为缩放中心。
            Vector2 world = (input.mousePosition - pan) / zoom;
            zoom = Mathf.Clamp(zoom * Mathf.Pow(1.1f, -input.delta.y), 0.05f, 1.5f);
            pan = input.mousePosition - world * zoom;
            input.Use();
            Repaint();
        }
        if (input.type == EventType.MouseDown)
        {
            panning = input.button == 2;
            if (input.button == 0)
            {
                for (int i = config.Nodes.Count - 1; i >= 0; i--)
                {
                    if (!NodeRect(config.Nodes[i]).Contains(input.mousePosition)) continue;
                    selected = config.Nodes[i];
                    selectedArea = null;
                    pointIndex = 0;
                    dragging = selected;
                    input.Use();
                    Repaint();
                    return;
                }
                panning = true;
                pendingPort = null;
            }
        }
        if (input.type == EventType.MouseDrag)
        {
            if (dragging != null) { dragging.Position += input.delta / zoom; config.IsDirty = true; }
            else if (panning) pan += input.delta;
            input.Use();
            Repaint();
        }
        if (input.type == EventType.MouseUp) { dragging = null; panning = false; }
    }

    /// <summary>显示当前节点字段、刷点和机制执行记录。</summary>
    private void DrawInspector()
    {
        GUILayout.Label("正式出生点（客户端与服务端共用）", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        fields.Draw(config.Document.Root.Element("data").Element("player_spawn"));
        GUILayout.Label("副本阶段目标", EditorStyles.boldLabel);
        fields.Draw(config.Document.Root.Element("data").Element("dungeon_targets"));
        if (EditorGUI.EndChangeCheck()) config.IsDirty = true;
        EditorGUILayout.Space();
        if (selectedArea != null)
        {
            GUILayout.Label("区域 " + ROSceneXml.Int(selectedArea, "id"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            fields.Draw(selectedArea);
            if (EditorGUI.EndChangeCheck()) { config.IsDirty = true; SceneView.RepaintAll(); }
            DrawPreviewLog();
            return;
        }
        if (selected == null) { GUILayout.Label("选择一个波次或机制节点", EditorStyles.boldLabel); DrawPreviewLog(); return; }
        GUILayout.Label(selected.Title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("原节点类型", selected.TypeName, EditorStyles.wordWrappedMiniLabel);
        EditorGUI.BeginChangeCheck();
        // 备注属于原画布数据，保存时仍可回到 RO 编辑器查看。
        string content = EditorGUILayout.TextField("备注", selected.Content);
        if (EditorGUI.EndChangeCheck()) { selected.Content = content; config.IsDirty = true; }
        if (selected.IsWave) DrawWaveTools();
        else if (GUILayout.Button("预览所选机制")) { EnsurePreview(); if (preview != null) preview.ExecuteNode(selected); }
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        fields.Draw(selected.Data);
        if (EditorGUI.EndChangeCheck()) { config.IsDirty = true; SceneView.RepaintAll(); }
        DrawConnections();
        DrawPreviewLog();
    }

    /// <summary>给波次提供直观的刷点与出场预览操作。</summary>
    private void DrawWaveTools()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("预览此波怪物")) { EnsurePreview(); if (preview != null) preview.ShowWave(selected); FrameSelectedPoint(); }
        if (GUILayout.Button("预览完成此波")) { EnsurePreview(); if (preview != null) preview.CompleteWave(selected.WaveId); }
        EditorGUILayout.EndHorizontal();
        if (preview != null)
        {
            // 百分比只改变预览条件，不向配置写入假战斗结果。
            MSceneWavePreview wave = preview.FindWave(selected.WaveId);
            if (wave != null && wave.Spawned)
            {
                wave.AlivePercent = EditorGUILayout.IntSlider("模拟剩余数量（%）", wave.AlivePercent, 0, 100);
                wave.HealthPercent = EditorGUILayout.IntSlider("模拟剩余血量（%）", wave.HealthPercent, 0, 100);
            }
        }
        // 原怪物编号决定真实模型目录映射。
        int id = ROSceneXml.Int(selected.Data, "monster_info/entity_id", -1);
        ROSceneCreatureEntity entity = catalog.FindEntity(id);
        if (entity != null)
        {
            EditorGUILayout.LabelField("怪物", id + " · " + entity.displayName);
            // 已迁入模型的原动作均可手动播放，便于检查出场表现。
            ROSceneCreatureModel model = catalog.FindModel(entity.modelName);
            if (model != null && GUILayout.Button("选择动作预览…"))
            {
                // 菜单项只捕获当前动作路径，不复制模型业务数据。
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < model.animations.Length; i++)
                {
                    // 此局部保存当前循环的动作条目供菜单回调使用。
                    ROSceneCreatureAnimation animation = model.animations[i];
                    menu.AddItem(new GUIContent(animation.displayName), false, () => { EnsurePreview(); preview.ShowWave(selected); preview.PlayAnimation(selected.WaveId, AssetDatabase.LoadAssetAtPath<AnimationClip>(animation.clipPath)); FrameSelectedPoint(); });
                }
                menu.ShowAsContext();
            }
        }
        // 刷点引用直接来自原波次 XML，不维护第二份位置数据。
        XElement[] points = ROSceneXml.SpawnPoints(selected.Data);
        if (points.Length == 0) return;
        pointIndex = Mathf.Clamp(EditorGUILayout.IntSlider("编辑刷点", pointIndex, 0, points.Length - 1), 0, points.Length - 1);
        if (GUILayout.Button("场景聚焦当前刷点")) FrameSelectedPoint();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("复制当前刷点"))
        {
            // 怪物的列表刷点允许新增，NPC 单点保持原结构。
            if (points[pointIndex].Name.LocalName == "MScenePositionData") { points[pointIndex].Parent.Add(new XElement(points[pointIndex])); config.IsDirty = true; }
        }
        if (points.Length > 1 && GUILayout.Button("删除当前刷点")) { points[pointIndex].Remove(); pointIndex = 0; config.IsDirty = true; }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>列出所选节点的真实连接，支持移除后重新接线。</summary>
    private void DrawConnections()
    {
        GUILayout.Label("节点连线", EditorStyles.boldLabel);
        for (int i = 0; i < config.Connections.Count; i++)
        {
            // 当前节点的数据元素在 Rebuild 后仍是同一 XML 对象。
            MSceneEditorConnection connection = config.Connections[i];
            MSceneEditorPort local = connection.First;
            MSceneEditorPort remote = connection.Second;
            if (remote.Owner.Element == selected.Element) { local = connection.Second; remote = connection.First; }
            if (local.Owner.Element != selected.Element) continue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(local.Name + " → " + remote.Owner.Title, EditorStyles.wordWrappedMiniLabel);
            // 移除后立即结束当前遍历，下一帧读取更新后的连接列表。
            bool remove = GUILayout.Button("断开", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();
            if (remove) { StopPreview(); config.Disconnect(connection); break; }
        }
    }

    /// <summary>展示本轮预览真实执行的事件和暂未运行的外部调用。</summary>
    private void DrawPreviewLog()
    {
        if (preview == null) return;
        EditorGUILayout.Space();
        GUILayout.Label("机制预览记录", EditorStyles.boldLabel);
        // 预览角色坐标也允许直接输入，以检查原传送点和区域。
        preview.PlayerPosition = EditorGUILayout.Vector3Field("预览角色位置", preview.PlayerPosition);
        // 区域索引与原 XML 顺序一致，不改变持久化对象编号。
        for (int i = 0; i < config.TriggerObjects.Length; i++)
        {
            // 只显示当前位置可操作对象；点击按原 PlayerButton 连线执行。
            XElement area = config.TriggerObjects[i];
            // 原对象编号与正式客户端、服务端保持一致。
            int objectId = ROSceneXml.Int(area, "id");
            if (!preview.CanPressObject(objectId)) continue;
            // option 是原区域支持的一个操作枚举，只有按钮操作才展示入口。
            if (!area.Descendants("SceneTriggerObjectOpType").Any(option => option.Value == "StoOpTypePlayerButton")) continue;
            if (GUILayout.Button(ROSceneXml.Text(area, "box/title", "交互") + " · " + objectId)) preview.PressObject(objectId);
        }
        eventScroll = EditorGUILayout.BeginScrollView(eventScroll, GUILayout.Height(170));
        for (int i = preview.Events.Count - 1; i >= 0; i--) EditorGUILayout.LabelField(preview.Events[i], EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
    }

    /// <summary>显示并编辑原刷点和触发区域，预览角色手柄可触发真实区域检测。</summary>
    /// <param name="view">当前 Scene View。</param>
    private void DuringSceneGUI(SceneView view)
    {
        if (config == null || SceneManager.GetActiveScene().path != ScenePath) return;
        if (showAreas)
        {
            for (int i = 0; i < config.TriggerObjects.Length; i++)
            {
                // 原区域的偏移在朝向旋转之后应用。
                XElement area = config.TriggerObjects[i];
                Vector3 position = ROSceneXml.Vector(area, "position");
                Quaternion rotation = Quaternion.Euler(0, ROSceneXml.Float(area, "face"), 0);
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
                if (preview != null && !preview.VisibleObjects.Contains(ROSceneXml.Int(area, "id"))) Handles.color = Color.gray;
                using (new Handles.DrawingScope(Matrix4x4.TRS(position, rotation, Vector3.one))) Handles.DrawWireCube(ROSceneXml.Vector(area, "box/offset"), ROSceneXml.Vector(area, "box/scale"));
                Handles.Label(position, "区域 " + ROSceneXml.Int(area, "id"));
            }
        }
        if (selectedArea != null)
        {
            EditorGUI.BeginChangeCheck();
            // 场景平移工具直接写回当前区域坐标。
            Vector3 moved = Handles.PositionHandle(ROSceneXml.Vector(selectedArea, "position"), Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) { ROSceneXml.SetVector(selectedArea, "position", moved); config.IsDirty = true; Repaint(); }
        }
        if (selected != null && selected.IsWave) DrawSpawnHandles();
        if (preview != null && preview.Running)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(preview.PlayerPosition, Vector3.up, 0.7f);
            Handles.Label(preview.PlayerPosition + Vector3.up, "预览角色（拖动可触发区域）");
            preview.PlayerPosition = Handles.PositionHandle(preview.PlayerPosition, Quaternion.identity);
        }
    }

    /// <summary>显示当前波次全部刷点，选中点提供位置和旋转手柄。</summary>
    private void DrawSpawnHandles()
    {
        // 场景工具始终引用原 XML 刷点集合。
        XElement[] points = ROSceneXml.SpawnPoints(selected.Data);
        for (int i = 0; i < points.Length; i++)
        {
            // 原刷点坐标及出生扩散范围，半径单位为米。
            Vector3 position = ROSceneXml.Vector(points[i], "position");
            Handles.color = Color.green;
            Handles.DrawWireDisc(position, Vector3.up, Mathf.Max(0.4f, ROSceneXml.Float(points[i], "born_radius")));
            Handles.Label(position, "Wave " + selected.WaveId + " / 点 " + i + " ×" + ROSceneXml.Int(points[i], "count", 1));
            if (i != pointIndex) continue;
            EditorGUI.BeginChangeCheck();
            // 选中刷点的平移和朝向独立于模型预览对象。
            Vector3 moved = Handles.PositionHandle(position, Quaternion.identity);
            Quaternion rotation = Quaternion.Euler(0, ROSceneXml.Float(points[i], "face"), 0);
            rotation = Handles.RotationHandle(rotation, moved);
            if (EditorGUI.EndChangeCheck())
            {
                ROSceneXml.SetVector(points[i], "position", moved);
                ROSceneXml.Set(points[i], "face", rotation.eulerAngles.y.ToString("R", CultureInfo.InvariantCulture));
                config.IsDirty = true;
                Repaint();
            }
        }
    }

    /// <summary>聚焦当前所选刷点，便于直接在地图中调整。</summary>
    private void FrameSelectedPoint()
    {
        if (selected == null) return;
        // 当前节点可能是没有空间点的事件节点。
        XElement[] points = ROSceneXml.SpawnPoints(selected.Data);
        if (points.Length == 0) return;
        pointIndex = Mathf.Clamp(pointIndex, 0, points.Length - 1);
        FramePosition(ROSceneXml.Vector(points[pointIndex], "position"));
    }

    /// <summary>让 Scene View 观察指定原场景位置。</summary>
    /// <param name="position">需要查看的世界坐标。</param>
    private static void FramePosition(Vector3 position)
    {
        // Scene View 由 Unity 管理，不改动正式场景相机。
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) view = GetWindow<SceneView>();
        view.LookAt(position + Vector3.up, Quaternion.Euler(35f, 200f, 0f), 12f);
        view.Repaint();
    }

    /// <summary>将全部原节点坐标适配到中央画布。</summary>
    private void FitGraph()
    {
        if (config == null || config.Nodes.Count == 0) return;
        // 边界只作用于显示，不更改原节点坐标。
        Vector2 min = config.Nodes[0].Position;
        Vector2 max = min;
        for (int i = 0; i < config.Nodes.Count; i++) { min = Vector2.Min(min, config.Nodes[i].Position); max = Vector2.Max(max, config.Nodes[i].Position + new Vector2(220f, 180f)); }
        Vector2 size = max - min;
        zoom = Mathf.Clamp(Mathf.Min(Mathf.Max(300f, position.width - 620f) / size.x, Mathf.Max(400f, position.height - 100f) / size.y), 0.05f, 1f);
        pan = new Vector2(20f, 20f) - min * zoom;
        Repaint();
    }

    /// <summary>打开已经迁入主工程的正式地图，保留 Unity 原生未保存场景提示。</summary>
    private static void OpenDungeonScene()
    {
        if (SceneManager.GetActiveScene().path == ScenePath) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
    }

    /// <summary>开始当前配置的完整波次预览。</summary>
    private void StartPreview()
    {
        EnsurePreview();
        if (preview != null) preview.Start();
    }

    /// <summary>为当前编辑配置创建一次场景预览对象。</summary>
    private void EnsurePreview()
    {
        if (EditorApplication.isPlaying) { ShowNotification(new GUIContent("请退出运行模式后使用编辑预览")); return; }
        OpenDungeonScene();
        if (SceneManager.GetActiveScene().path != ScenePath) return;
        if (preview == null) preview = new ROScenePreview(config, catalog);
        preview.Changed = Repaint;
    }

    /// <summary>停止并释放本窗口当前预览。</summary>
    private void StopPreview()
    {
        if (preview != null) preview.Dispose();
        preview = null;
        Repaint();
    }

    /// <summary>切换文件前保护未保存的真实配置编辑。</summary>
    private bool ConfirmDiscard()
    {
        if (config == null || !config.IsDirty) return true;
        // 只在确实存在未保存修改时询问如何处理。
        int choice = EditorUtility.DisplayDialogComplex("切换副本配置", "当前修改尚未保存。", "保存并打开", "取消", "放弃修改");
        if (choice == 1) return false;
        if (choice == 0) SaveChanges();
        return true;
    }
}
