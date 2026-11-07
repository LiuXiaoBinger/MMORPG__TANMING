using System;
using System.Collections.Generic;
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
    /// <summary>从正式 Luban 二进制读取的目标表，加载场景或手动刷新时更新。</summary>
    private TbDungeonTargetTable dungeonTargetTable;
    /// <summary>完整副本表，场景目标的顺序和分组以该表为准。</summary>
    private TbDungeonsTable dungeonsTable;
    /// <summary>分支模式引用的目标组定义，组内目标同时展示。</summary>
    private TbDungeonTargetGroupTable dungeonTargetGroupTable;
    /// <summary>目标面板当前查看的 DungeonsID，切换查看不写回场景 XML。</summary>
    [SerializeField] private int selectedDungeonId = 4060;
    /// <summary>副本下拉框的真实编号，顺序与生成表一致。</summary>
    private int[] dungeonIds = new int[0];
    /// <summary>副本下拉框使用编号和原表名称，便于区分同名难度。</summary>
    private string[] dungeonNames = new string[0];
    /// <summary>当前打开的 XML 所属资源登记，未登记时不借用其他场景预览。</summary>
    private DungeonSceneResource sceneResource;
    /// <summary>当前 XML 对应的已迁入 Unity 场景路径；缺场景时为空。</summary>
    private string CurrentScenePath
    {
        get
        {
            if (sceneResource == null || string.IsNullOrEmpty(sceneResource.sceneLocation)) return string.Empty;
            return sceneResource.sceneLocation + ".unity";
        }
    }
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
    /// <summary>全局出生点和目标表默认折叠，优先展示当前选择。</summary>
    private bool showGlobalSettings;
    /// <summary>原 XML 编辑字段按需展开，不挤占所选节点摘要和动作。</summary>
    private bool showRawFields;
    /// <summary>最近一次明确定位的地图场景路径，跨场景传送也按目标资源登记显示。</summary>
    private string focusedMapPath = "";
    /// <summary>最近一次地图定位的原世界坐标，单位为米。</summary>
    private Vector3 focusedMapPosition;
    /// <summary>最近一次地图定位标记的具体对象说明。</summary>
    private string focusedMapLabel = "";
    /// <summary>主入口默认展示配置阶段；值为 1 时保留原始节点编辑器。</summary>
    [SerializeField] private int editorView;
    /// <summary>当前查看的配置阶段键，不表示玩家的运行进度。</summary>
    [SerializeField] private string selectedStageKey = "entry";
    /// <summary>由当前 XML 与正式目标表实时解释的只读阶段集合。</summary>
    private List<ROSceneStage> stages = new List<ROSceneStage>();
    /// <summary>配置、目标表或原图编辑变化后重建一次阶段，避免每次重绘重复解析全图。</summary>
    private bool stageFlowNeedsRefresh = true;
    /// <summary>左侧完整阶段导航的滚动位置。</summary>
    private Vector2 stageListScroll;
    /// <summary>当前阶段行动与关联说明的滚动位置。</summary>
    private Vector2 stageContentScroll;
    /// <summary>是否展开当前阶段全部关联证据，不影响配置数据。</summary>
    private bool showStageNotes;
    /// <summary>是否展示高级排查入口；未归属节点不会混入正常阶段目录。</summary>
    private bool showStageAdvanced;

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
        window.editorView = 0;
        window.SelectStage("entry");
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
        LoadDungeonTargets();
        BindSceneResource();
        selected = null;
        selectedArea = null;
        focusedMapPath = "";
        editorView = 0;
        SelectStage("entry");
        stageListScroll = Vector2.zero;
        inspectorScroll = Vector2.zero;
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
        stageFlowNeedsRefresh = true;
        base.SaveChanges();
    }

    /// <summary>绘制工具栏、节点列表、原画布和属性面板。</summary>
    private void OnGUI()
    {
        DrawToolbar();
        if (config == null) return;
        if (editorView == 0)
        {
            // 仅在配置变化后的布局阶段重建；滚动和重绘不重复解析原图。
            if (stageFlowNeedsRefresh && Event.current.type == EventType.Layout) RefreshStageFlow();
            DrawStageFlow();
            hasUnsavedChanges = config.IsDirty;
            return;
        }
        // 原始视图保留全部编辑入口，返回流程时统一重新解释最新配置。
        stageFlowNeedsRefresh = true;
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
        editorView = GUILayout.Toolbar(editorView, new[] { "阶段流程", "高级：节点编辑" }, EditorStyles.toolbarButton, GUILayout.Width(220));
        if (GUILayout.Button("打开配置", EditorStyles.toolbarButton, GUILayout.Width(75)))
        {
            // Unity 原生文件框只负责选择待编辑文件。
            string path = EditorUtility.OpenFilePanel("打开 RO 场景编辑配置", Path.GetDirectoryName(DefaultConfigPath), "txt");
            if (path.Length > 0 && ConfirmDiscard()) LoadConfig(path);
        }
        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50))) SaveChanges();
        if (GUILayout.Button("打开当前场景", EditorStyles.toolbarButton, GUILayout.Width(95))) OpenDungeonScene();
        if (editorView == 1)
        {
            if (GUILayout.Button("适配画布", EditorStyles.toolbarButton, GUILayout.Width(75))) FitGraph();
            GUILayout.Space(12);
            if (GUILayout.Button("开始流程预览", EditorStyles.toolbarButton, GUILayout.Width(95))) StartPreview();
            if (GUILayout.Button("停止预览", EditorStyles.toolbarButton, GUILayout.Width(75))) StopPreview();
            showAreas = GUILayout.Toggle(showAreas, "触发区域", EditorStyles.toolbarButton, GUILayout.Width(75));
        }
        else if (GUILayout.Button("查看入口", EditorStyles.toolbarButton, GUILayout.Width(75)))
        {
            SelectStage("entry");
        }
        GUILayout.FlexibleSpace();
        if (config != null && editorView == 1) GUILayout.Label(config.Nodes.Count + " 节点 / " + config.Connections.Count + " 连线 / " + config.TriggerObjects.Length + " 区域");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (editorView == 0)
        {
            GUILayout.Label("当前副本", EditorStyles.boldLabel, GUILayout.Width(65));
            // 选择其他副本只读其目标表，不把当前 XML 或地图套到该副本上。
            EditorGUI.BeginChangeCheck();
            if (dungeonsTable != null)
                selectedDungeonId = EditorGUILayout.IntPopup(selectedDungeonId, dungeonNames, dungeonIds, GUILayout.Width(280));
            if (EditorGUI.EndChangeCheck())
            {
                stageFlowNeedsRefresh = true;
                SelectStage("entry");
            }
            if (GUILayout.Button("重读目标表", EditorStyles.toolbarButton, GUILayout.Width(80))) LoadDungeonTargets();
            GUILayout.Label("进入 → 查看当前阶段 → 按实际出口继续（配置查看）", EditorStyles.miniLabel);
        }
        else
        {
            GUILayout.Label("事件", GUILayout.Width(30));
            eventName = GUILayout.TextField(eventName, GUILayout.MinWidth(120));
            if (GUILayout.Button("发送到预览", EditorStyles.toolbarButton, GUILayout.Width(90))) { EnsurePreview(); if (preview != null) preview.SendEvent(eventName); }
            GUILayout.Label("滚轮缩放 · 背景拖动 · 选节点编辑 · 点两个端口连线", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>直接从最新配置生成阶段；查看其他副本时只读取其目标表，不关联当前地图。</summary>
    private void RefreshStageFlow()
    {
        stageFlowNeedsRefresh = false;
        stages.Clear();
        if (dungeonsTable == null || dungeonTargetTable == null) return;
        // 当前下拉框是只读查看对象，资源登记仍由已打开的 XML 决定。
        DungeonsTable dungeon = dungeonsTable.GetOrDefault(selectedDungeonId);
        if (dungeon == null) return;
        // 不匹配时不传入当前 XML，防止相同 Wave ID 被错误关联。
        MSceneEditorConfig stageConfig = null;
        if (StageMatchesMap(dungeon)) stageConfig = config;
        stages = ROSceneStageFlow.Build(stageConfig, dungeon, dungeonTargetTable, dungeonTargetGroupTable, catalog);
        if (FindStage(selectedStageKey) == null) selectedStageKey = "entry";
    }

    /// <summary>确认目标表所用地图与已按真实路径绑定的 XML 资源一致。</summary>
    /// <param name="dungeon">当前查看的副本表行。</param>
    private bool StageMatchesMap(DungeonsTable dungeon)
    {
        return sceneResource != null && dungeon.SceneID == sceneResource.sceneId;
    }

    /// <summary>按解释器提供的稳定键查找阶段，分支组不会按显示顺序误连。</summary>
    /// <param name="key">入口、阶段、组或结束项的标识。</param>
    private ROSceneStage FindStage(string key)
    {
        // 阶段集合很短，直接查找可保持与当前布局使用同一份结果。
        for (int index = 0; index < stages.Count; index++)
            if (stages[index].Key == key) return stages[index];
        return null;
    }

    /// <summary>选择配置阶段，只改变编辑器查看位置，不提交或模拟游戏进度。</summary>
    /// <param name="key">用户选择的真实阶段键。</param>
    private void SelectStage(string key)
    {
        selectedStageKey = key;
        stageContentScroll = Vector2.zero;
        showStageNotes = false;
        Repaint();
    }

    /// <summary>固定阶段目录与完成出口，只让当前阶段正文纵向滚动。</summary>
    private void DrawStageFlow()
    {
        if (stages.Count == 0)
        {
            EditorGUILayout.HelpBox("尚未读取到副本目标定义。可重读目标表，或进入高级节点编辑查看当前 XML。", MessageType.Warning);
            return;
        }
        // 两行工具栏下直接进入工作台，不再占用顶部空间绘制全程连线。
        float bodyTop = 46f;
        // 正文随窗口高度伸缩，1100×650 的最小窗口仍保留约 600 像素。
        float bodyHeight = position.height - bodyTop;
        // 导航宽度容纳完整阶段编号与两行目标名，中央保留至少 840 像素。
        float navigationWidth = 244f;
        GUILayout.BeginArea(new Rect(0f, bodyTop, navigationWidth, bodyHeight), EditorStyles.helpBox);
        DrawStageNavigation();
        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(navigationWidth + 6f, bodyTop, position.width - navigationWidth - 10f, bodyHeight), EditorStyles.helpBox);
        // 当前阶段对象来自本次布局，不保留失效的旧节点引用。
        ROSceneStage stage = FindStage(selectedStageKey);
        if (stage != null) DrawCurrentStage(stage);
        GUILayout.EndArea();
    }

    /// <summary>固定进入与结束入口，阶段目录保留真实编号，技术排查默认折叠。</summary>
    private void DrawStageNavigation()
    {
        GUILayout.Space(8f);
        GUILayout.Label("副本阶段", StageTextStyle(16, true));
        GUILayout.Label("点击阶段查看；出口决定下一步", StageTextStyle(12, false));
        GUILayout.Space(8f);
        DrawStageNavigationItem(FindStage("entry"));
        stageListScroll = EditorGUILayout.BeginScrollView(stageListScroll);
        // 只列正式目标阶段，不把未归属节点或原图节点当作玩家任务。
        for (int index = 0; index < stages.Count; index++)
        {
            if (stages[index].IsEntry || stages[index].IsFinish || stages[index].Key == "unresolved") continue;
            DrawStageNavigationItem(stages[index]);
        }
        EditorGUILayout.EndScrollView();
        DrawStageNavigationItem(FindStage("finish"));
        GUILayout.Space(8f);
        showStageAdvanced = EditorGUILayout.Foldout(showStageAdvanced, "高级：关联排查", true);
        if (showStageAdvanced)
        {
            // 未建立阶段关系的节点保留单独入口，不暗示它们属于通关流程。
            ROSceneStage unresolved = FindStage("unresolved");
            if (unresolved != null)
                if (GUILayout.Button("未归属节点（" + (unresolved.Waves.Count + unresolved.Mechanisms.Count) + "）", GUILayout.Height(26f))) SelectStage("unresolved");
            if (GUILayout.Button("高级：节点编辑", GUILayout.Height(26f))) editorView = 1;
        }
        GUILayout.Space(6f);
    }

    /// <summary>绘制可换行的阶段入口，选中底色仅表示当前查看项。</summary>
    /// <param name="stage">来自真实阶段集合的目录项。</param>
    private void DrawStageNavigationItem(ROSceneStage stage)
    {
        if (stage == null) return;
        // 导航文字使用正常字号并自动换行，不把长目标缩成无法识别的小按钮。
        GUIStyle itemStyle = new GUIStyle(GUI.skin.button);
        itemStyle.fontSize = 13;
        itemStyle.wordWrap = true;
        itemStyle.alignment = TextAnchor.MiddleLeft;
        itemStyle.padding = new RectOffset(10, 10, 8, 8);
        // 目录保持至少两行的点击面积，长名称继续按内容扩高。
        float itemHeight = Mathf.Max(48f, itemStyle.CalcHeight(new GUIContent(StageNavigationTitle(stage)), 218f));
        // 恢复原背景色以免当前选择影响其他窗口控件。
        Color oldColor = GUI.backgroundColor;
        if (stage.Key == selectedStageKey) GUI.backgroundColor = new Color(0.35f, 0.75f, 1f);
        if (GUILayout.Button(new GUIContent(StageNavigationTitle(stage), stage.Title), itemStyle, GUILayout.Height(itemHeight))) SelectStage(stage.Key);
        GUI.backgroundColor = oldColor;
        GUILayout.Space(4f);
    }

    /// <summary>保留真实阶段或分支组名称，开始和结束采用玩家流程文案。</summary>
    /// <param name="stage">需要显示编号和任务的阶段。</param>
    private static string StageNavigationTitle(ROSceneStage stage)
    {
        if (stage.IsEntry) return "进入 · 角色进入副本";
        if (stage.IsFinish) return "结束 · 完成与离开";
        if (stage.Title.Length > 42) return stage.Title.Substring(0, 41) + "…";
        return stage.Title;
    }

    /// <summary>构造当前工作台使用的可换行文字样式，避免继承编辑器微型字号。</summary>
    /// <param name="fontSize">文字字号，单位为屏幕逻辑像素。</param>
    /// <param name="bold">是否突出标题，不改变正文内容。</param>
    private static GUIStyle StageTextStyle(int fontSize, bool bold)
    {
        // 每个样式只影响当前控件，不修改 Unity 的共享 EditorStyles。
        GUIStyle style = new GUIStyle(EditorStyles.wordWrappedLabel);
        style.fontSize = fontSize;
        style.wordWrap = true;
        if (bold) style.fontStyle = FontStyle.Bold;
        return style;
    }

    /// <summary>按玩家行动、怪物、机制和完成出口组织四块内容。</summary>
    /// <param name="stage">本次布局读取的当前阶段。</param>
    private void DrawCurrentStage(ROSceneStage stage)
    {
        GUILayout.Space(8f);
        GUILayout.Label(new GUIContent(StageNavigationTitle(stage), stage.Title), StageTextStyle(20, true));
        GUILayout.Space(8f);
        stageContentScroll = EditorGUILayout.BeginScrollView(stageContentScroll);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("① 这一步做什么", StageTextStyle(15, true));
        GUILayout.Space(5f);
        // 先展示玩家当前目标，再说明进入条件，让第一眼就能读到行动要求。
        for (int index = 0; index < stage.TargetTexts.Count; index++)
            GUILayout.Label("• " + stage.TargetTexts[index], StageTextStyle(14, true));
        GUILayout.Label(stage.EntryText, StageTextStyle(13, false));
        if (stage.IsEntry) DrawEntryLocation();
        GUILayout.Space(5f);
        EditorGUILayout.EndVertical();
        GUILayout.Space(8f);
        // 仅展示真实触发器摘要；同链的时机、条件与结果不会再生成重复机制卡片。
        List<MSceneEditorNode> mechanisms = StageMechanismCards(stage);
        // 两列共用外层唯一正文滚动，既能对照怪物与机制，又不产生细小内层滚动窗。
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width((position.width - 284f) * 0.45f));
        GUILayout.Label("② 本阶段波次 · " + stage.Waves.Count + " 组", StageTextStyle(15, true));
        GUILayout.Space(6f);
        if (stage.Waves.Count == 0) GUILayout.Label("配置未确认此阶段的关联波次。", StageTextStyle(13, false));
        // 每个已关联波次保留名称、数量和地图入口，不按节点数任意截断。
        for (int index = 0; index < stage.Waves.Count; index++) DrawStageWave(stage.Waves[index]);
        EditorGUILayout.EndVertical();
        GUILayout.Space(8f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("③ 本阶段机制 · " + mechanisms.Count + " 项", StageTextStyle(15, true));
        GUILayout.Space(6f);
        if (mechanisms.Count == 0) GUILayout.Label("配置未确认此阶段的独立触发机制。", StageTextStyle(13, false));
        // 一张卡片对应一个真实触发器；共享结果不合并不同触发器的条件。
        for (int index = 0; index < mechanisms.Count; index++) DrawStageMechanism(mechanisms[index]);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10f);
        showStageNotes = EditorGUILayout.Foldout(showStageNotes, "高级：关联依据与原始节点（" + stage.Notes.Count + " 条说明）", true);
        if (showStageNotes)
        {
            // 技术证据只在用户展开后显示，避免原图节点挤占默认玩家流程。
            for (int index = 0; index < stage.Notes.Count; index++) GUILayout.Label("• " + stage.Notes[index], StageTextStyle(12, false));
            // 所有原始关联节点均有入口，包含未能归入某个触发器的节点。
            for (int index = 0; index < stage.Mechanisms.Count; index++)
                if (GUILayout.Button(ROSceneNodePresentation.Title(stage.Mechanisms[index], catalog), StageNodeButtonStyle())) OpenStageNode(stage.Mechanisms[index]);
        }
        GUILayout.Space(8f);
        EditorGUILayout.EndScrollView();
        DrawStageCompletion(stage);
    }

    /// <summary>从真实触发上下文收集机制卡片，同一链只显示触发器一次。</summary>
    /// <param name="stage">包含解释器已确认关联节点的当前阶段。</param>
    private List<MSceneEditorNode> StageMechanismCards(ROSceneStage stage)
    {
        // 只改变展示去重，不修改原阶段关联集合、节点或连线。
        List<MSceneEditorNode> cards = new List<MSceneEditorNode>();
        // 原数据中的时机和结果会通过已有 FlowNodes 找到所属触发器。
        for (int index = 0; index < stage.Mechanisms.Count; index++)
        {
            // 当前节点的一跳触发链保持原 XML 的归属，不扩大到整个触发器组。
            List<MSceneEditorNode> related = ROSceneNodePresentation.FlowNodes(config, stage.Mechanisms[index]);
            // 同一个触发器可能由多个时机、条件和结果引用，只建立一张机制卡。
            for (int relatedIndex = 0; relatedIndex < related.Count; relatedIndex++)
            {
                if (!ROSceneNodePresentation.IsTrigger(related[relatedIndex])) continue;
                if (!cards.Contains(related[relatedIndex])) cards.Add(related[relatedIndex]);
            }
        }
        return cards;
    }

    /// <summary>固定完成条件和真实后继入口，超长条件可独立查看完整原文。</summary>
    /// <param name="stage">提供完成说明与真实 NextKeys 的当前阶段。</param>
    private void DrawStageCompletion(ROSceneStage stage)
    {
        GUILayout.Space(6f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("④ 怎样完成 / 接下来去哪", StageTextStyle(15, true));
        GUILayout.Space(4f);
        // 固定区显示前三行核心条件，避免少数超长目标挤掉当前阶段全部正文。
        string[] completionLines = stage.CompletionText.Split('\n');
        // 未截断时与原文完全一致；被省略的内容通过按钮保留完整查看入口。
        string completion = stage.CompletionText;
        if (completionLines.Length > 3) completion = string.Join("\n", completionLines, 0, 3) + "…";
        if (completion.Length > 180) completion = completion.Substring(0, 180) + "…";
        GUILayout.Label(new GUIContent(completion, stage.CompletionText), StageTextStyle(13, false));
        if (completion != stage.CompletionText)
            if (GUILayout.Button("查看全部完成条件", GUILayout.Height(24f))) EditorUtility.DisplayDialog(stage.Title + " · 完成条件", stage.CompletionText, "返回阶段");
        GUILayout.Space(5f);
        if (stage.NextKeys.Count == 1)
        {
            // 单出口严格使用当前阶段配置的后继键，不使用目录的下一行。
            ROSceneStage next = FindStage(stage.NextKeys[0]);
            if (next != null)
                if (GUILayout.Button("下一步 → " + StageNavigationTitle(next), StageNodeButtonStyle())) SelectStage(next.Key);
        }
        else if (stage.NextKeys.Count > 1)
        {
            GUILayout.Label("存在 " + stage.NextKeys.Count + " 条真实后继；完成后按配置选择路线。", StageTextStyle(12, false));
            if (GUILayout.Button("查看下一阶段 / 选择分支 →", StageNodeButtonStyle()))
            {
                // 分支菜单容纳全部真实出口，不因窗口宽度隐藏或挤压某个分支。
                GenericMenu menu = new GenericMenu();
                // 菜单项仅导航，不触发游戏中的 NPC 选支或目标完成。
                for (int index = 0; index < stage.NextKeys.Count; index++)
                {
                    // 每个回调捕获本次循环对应的真实后继对象。
                    ROSceneStage next = FindStage(stage.NextKeys[index]);
                    if (next != null) menu.AddItem(new GUIContent(StageNavigationTitle(next).Replace("/", "／")), false, () => SelectStage(next.Key));
                }
                menu.ShowAsContext();
            }
        }
        else GUILayout.Label("此处没有配置后继阶段。", StageTextStyle(12, false));
        GUILayout.Space(4f);
        EditorGUILayout.EndVertical();
    }

    /// <summary>为地图、原节点及下一步提供可换行的正常大小按钮。</summary>
    private static GUIStyle StageNodeButtonStyle()
    {
        // 使用正常按钮作为基底，长名称按可用宽度换行而不是横向溢出。
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 12;
        style.wordWrap = true;
        style.padding = new RectOffset(8, 8, 6, 6);
        return style;
    }

    /// <summary>提供原始 player_spawn 的地图定位，不从任务引导坐标推测入口。</summary>
    private void DrawEntryLocation()
    {
        // 当前 XML 是否属于下拉查看的副本在此再次明确显示，防止误定位。
        DungeonsTable dungeon = dungeonsTable.GetOrDefault(selectedDungeonId);
        if (!StageMatchesMap(dungeon))
        {
            EditorGUILayout.HelpBox("当前查看的副本未绑定此 XML；目标可查看，怪物、机制与地图入口暂不关联。", MessageType.Warning);
            return;
        }
        // 正式出生点只从当前原配置读取，缺失时不回退到零坐标。
        XElement spawn = ROSceneXml.Find(config.Document.Root, "data/player_spawn");
        if (spawn == null || ROSceneXml.Find(spawn, "position") == null)
        {
            GUILayout.Label("当前 XML 未提供 player_spawn/position，无法定位真实入口。", StageTextStyle(13, false));
            return;
        }
        GUILayout.Label("正式出生点 " + ROSceneNodePresentation.PositionText(spawn, "position"), StageTextStyle(13, false));
        if (GUILayout.Button("地图定位：玩家进入位置", StageNodeButtonStyle())) FramePosition(ROSceneXml.Vector(spawn, "position"), CurrentScenePath, "玩家进入副本 · player_spawn");
    }

    /// <summary>绘制一个真实波次的名称、数量和原图/地图入口。</summary>
    /// <param name="wave">已由阶段解释器关联的原波次。</param>
    private void DrawStageWave(MSceneEditorNode wave)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("波次 " + wave.WaveId + " · " + ROSceneNodePresentation.CreatureLabel(wave, catalog), StageTextStyle(14, true));
        GUILayout.Label(ROSceneNodePresentation.WaveDetails(wave), StageTextStyle(13, false));
        GUILayout.Space(5f);
        EditorGUILayout.BeginHorizontal();
        // 逻辑波次不显示无效的地图按钮。
        if (ROSceneXml.SpawnPoints(wave.Data).Length > 0)
            if (GUILayout.Button("地图定位", StageNodeButtonStyle())) { SelectNode(wave); FrameAllPoints(); }
        if (GUILayout.Button("编辑波次", StageNodeButtonStyle())) OpenStageNode(wave);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.Space(6f);
    }

    /// <summary>把机制触发与结果连在同一张卡片，地图动作沿用真实来源/目的地定位。</summary>
    /// <param name="node">与本阶段存在证据关联的原机制节点。</param>
    private void DrawStageMechanism(MSceneEditorNode node)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label(ROSceneNodePresentation.Title(node, catalog), StageTextStyle(14, true));
        // 触发器初始启用状态来自原 enable 字段，不把配置显示成当前运行状态。
        string enabled = ROSceneXml.Text(node.Data, "enable");
        if (enabled == "true") GUILayout.Label("启用：初始开启", StageTextStyle(12, false));
        else if (enabled == "false") GUILayout.Label("启用：初始关闭", StageTextStyle(12, false));
        else GUILayout.Label("启用：原字段未明确（" + enabled + "）", StageTextStyle(12, false));
        GUILayout.Space(5f);
        GUILayout.Label(ROSceneNodePresentation.FlowSummary(config, node), StageTextStyle(13, false));
        GUILayout.Space(5f);
        // 只显示此触发器上下文中的真实来源区域或传送目的地。
        List<MSceneEditorNode> related = ROSceneNodePresentation.FlowNodes(config, node);
        // 每个关联位置入口对应原触发器中的时机或执行结果。
        for (int index = 0; index < related.Count; index++)
        {
            // 每个节点最多提供一个来源区域和一个传送结果。
            MSceneEditorNode location = related[index];
            if (ROSceneNodePresentation.HasSourceArea(location))
            {
                // 区域 ID 必须能在当前 XML 中找到真实对象才允许定位。
                XElement area = ROSceneNodePresentation.FindArea(config, ROSceneXml.Int(location.Data, "scene_trigger_obj_id"));
                if (area != null && GUILayout.Button("地图定位：来源区域 " + ROSceneXml.Int(area, "id"), StageNodeButtonStyle())) { SelectNode(node); FrameArea(area); }
            }
            if (ROSceneXml.Find(location.Data, "trans_position_info/position") != null)
                if (GUILayout.Button("地图定位：传送终点", StageNodeButtonStyle())) { SelectNode(node); FrameDestination(location); }
        }
        if (GUILayout.Button("编辑此机制", StageNodeButtonStyle())) OpenStageNode(node);
        EditorGUILayout.EndVertical();
        GUILayout.Space(6f);
    }

    /// <summary>从阶段卡片切换至原画布并沿用现有节点选择与编辑能力。</summary>
    /// <param name="node">阶段关联的原节点引用。</param>
    private void OpenStageNode(MSceneEditorNode node)
    {
        editorView = 1;
        // 原画布布局尚未重绘时先设置宽度，确保首次定位在实际中心区域。
        graphRect = new Rect(230f, 50f, position.width - 580f, position.height - 50f);
        SelectNode(node);
    }

    /// <summary>显示波次、机制和区域的可搜索列表。</summary>
    private void DrawList()
    {
        tab = GUILayout.Toolbar(tab, new[] { "波次", "机制", "区域" });
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        // 列表名称允许多行，不截断怪物名称、编号和刷点数量。
        GUIStyle listStyle = new GUIStyle(EditorStyles.miniButton);
        listStyle.fixedHeight = 0f;
        listStyle.wordWrap = true;
        listStyle.alignment = TextAnchor.MiddleLeft;
        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        if (tab == 2)
        {
            for (int i = 0; i < config.TriggerObjects.Length; i++)
            {
                // 原区域标题和 ID 便于与机制结果关联。
                XElement area = config.TriggerObjects[i];
                string title = "区域 " + ROSceneXml.Int(area, "id") + " · " + ROSceneXml.Text(area, "box/title");
                if (!Matches(title)) continue;
                if (GUILayout.Button(title, listStyle)) SelectArea(area);
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
                // 标题与数量来自当前 XML 和本场景实体目录，搜索同步包含真实怪名和 ID。
                string label = NodeLabel(node);
                if (!Matches(label + " " + node.Content)) continue;
                if (GUILayout.Button(label, listStyle)) SelectNode(node);
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
        inspectorScroll = Vector2.zero;
        focusedMapPath = "";
        zoom = Mathf.Max(zoom, 1f);
        pan = new Vector2(Mathf.Max(160f, graphRect.width) * 0.5f, 180f) - (node.Position + new Vector2(105f, 40f)) * zoom;
        Repaint();
        SceneView.RepaintAll();
    }

    /// <summary>选择区域只更新属性面板；地图切换由明确的定位按钮发起。</summary>
    /// <param name="area">原 XML 区域对象。</param>
    private void SelectArea(XElement area)
    {
        selectedArea = area;
        selected = null;
        inspectorScroll = Vector2.zero;
        focusedMapPath = "";
        Repaint();
        SceneView.RepaintAll();
    }

    /// <summary>生成列表和画布共用的多行摘要，标题保留真实名称和编号。</summary>
    /// <param name="node">当前显示节点。</param>
    private string NodeLabel(MSceneEditorNode node)
    {
        if (node.IsWave) return ROSceneNodePresentation.Title(node, catalog) + "\n" + ROSceneNodePresentation.WaveDetails(node);
        return ROSceneNodePresentation.Title(node, catalog);
    }

    /// <summary>取得当前缩放下实际绘制的节点字体，供文字测量和显示共同使用。</summary>
    private GUIStyle NodeTextStyle()
    {
        // 小缩放保留最小可读字号，布局使用相同字号计算换行。
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.fixedHeight = 0f;
        style.normal.textColor = Color.white;
        style.fontSize = Mathf.Max(7, Mathf.RoundToInt(11f * zoom));
        style.wordWrap = true;
        return style;
    }

    /// <summary>按文字实际换行计算未缩放标题高度，为下方端口留出空间。</summary>
    /// <param name="node">当前显示节点。</param>
    private float NodeHeaderHeight(MSceneEditorNode node)
    {
        // 使用原尺寸字体计算后整体缩放，避免长名称被固定行高裁切。
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.fontSize = 11;
        style.fixedHeight = 0f;
        style.wordWrap = true;
        return Mathf.Max(48f, style.CalcHeight(new GUIContent(NodeLabel(node)), 206f) + 12f);
    }

    /// <summary>按最长中英端口标签计算统一行高，输入输出各占完整一行且允许换行。</summary>
    /// <param name="node">需要测量端口文字的节点。</param>
    private float NodePortRowHeight(MSceneEditorNode node)
    {
        // 返回原画布单位的行高，绘制时再应用当前缩放。
        float height = 20f;
        // 隐藏文字的远景缩放保持紧凑轮廓，不为不可见标签增加高度。
        if (zoom <= 0.45f) return height;
        // 测量字号与实际绘制完全相同，避免低缩放的最小字号挤出行高。
        GUIStyle style = NodeTextStyle();
        // 原端口顺序同时决定标签和连线锚点的行号。
        for (int index = 0; index < node.Ports.Count; index++)
        {
            height = Mathf.Max(height, style.CalcHeight(new GUIContent(ROSceneNodePresentation.PortLabel(node.Ports[index])), 196f * zoom) / zoom + 4f);
        }
        return height;
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
        // 颜色区分普通逻辑、波次和当前选择。
        Color color = new Color(0.18f, 0.22f, 0.28f);
        if (node.IsWave) color = new Color(0.18f, 0.32f, 0.27f);
        if (selected == node) color = new Color(0.26f, 0.44f, 0.62f);
        EditorGUI.DrawRect(rect, color);
        // 小缩放时保留节点轮廓，大缩放时显示完整可读信息。
        GUIStyle style = NodeTextStyle();
        GUI.Label(new Rect(rect.x + 7f * zoom, rect.y + 3f * zoom, rect.width - 14f * zoom, NodeHeaderHeight(node) * zoom), new GUIContent(NodeLabel(node), node.Content), style);
        // 每个端口独占节点宽度，行高容纳当前缩放下的完整中英文字。
        float rowHeight = NodePortRowHeight(node) * zoom;
        // 端口仍按原文件顺序排列，不改变持久化端口或节点位置。
        for (int i = 0; i < node.Ports.Count; i++)
        {
            // 中文说明仅用于界面，英文原名仍与 XML 完全一致。
            MSceneEditorPort port = node.Ports[i];
            // 端口坐标随标题实际高度下移。
            Vector2 position = PortPosition(port);
            // 连接点击区保持窗口像素大小，方便低缩放时操作。
            Rect handle = new Rect(position.x - 5f, position.y - 5f, 10f, 10f);
            EditorGUI.DrawRect(handle, new Color(0.75f, 0.85f, 0.96f));
            // 端口圆点和中英标签提供相同的真实用途说明。
            GUIContent label = new GUIContent(ROSceneNodePresentation.PortLabel(port), ROSceneNodePresentation.PortDescription(port));
            GUI.Label(handle, new GUIContent("", label.tooltip));
            if (zoom > 0.45f)
            {
                // 两侧端口均使用整行，靠各自连接锚点对齐，长标签按行高换行。
                style.alignment = TextAnchor.MiddleRight;
                if (port.IsInput) style.alignment = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(rect.x + 12f * zoom, position.y - rowHeight * 0.5f, rect.width - 24f * zoom, rowHeight), label, style);
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
        return new Rect(node.Position * zoom + pan, new Vector2(220f, NodeHeaderHeight(node) + 10f + node.Ports.Count * NodePortRowHeight(node)) * zoom);
    }

    /// <summary>计算一个原端口在当前画布中的位置。</summary>
    /// <param name="port">要定位的端口。</param>
    private Vector2 PortPosition(MSceneEditorPort port)
    {
        // 同节点端口按文件顺序排列。
        Rect rect = NodeRect(port.Owner);
        // 输入和输出分别位于节点边框左右。
        float x = rect.xMax;
        if (port.IsInput) x = rect.xMin;
        return new Vector2(x, rect.y + (NodeHeaderHeight(port.Owner) + (port.Owner.Ports.IndexOf(port) + 0.5f) * NodePortRowHeight(port.Owner)) * zoom);
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
                    inspectorScroll = Vector2.zero;
                    focusedMapPath = "";
                    dragging = selected;
                    input.Use();
                    Repaint();
                    SceneView.RepaintAll();
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
        if (selectedArea != null)
        {
            GUILayout.Label("区域 " + ROSceneXml.Int(selectedArea, "id") + " · " + ROSceneXml.Text(selectedArea, "box/title"), EditorStyles.wordWrappedLabel);
            GUILayout.Label("世界坐标 " + ROSceneNodePresentation.PositionText(selectedArea, "position"), EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("定位地图区域（Scene 视图）")) FrameArea(selectedArea);
            EditorGUI.BeginChangeCheck();
            fields.Draw(selectedArea);
            if (EditorGUI.EndChangeCheck()) { config.IsDirty = true; SceneView.RepaintAll(); }
        }
        else if (selected != null)
        {
            GUILayout.Label(NodeLabel(selected), EditorStyles.wordWrappedLabel);
            GUILayout.Label("点选仅定位流程画布；地图按钮打开对应场景并定位 Scene 视图。", EditorStyles.wordWrappedMiniLabel);
            if (selected.IsWave) DrawWaveTools();
            else
            {
                DrawMechanismSummary();
                if (GUILayout.Button("预览所选机制")) { EnsurePreview(); if (preview != null) preview.ExecuteNode(selected); }
            }
            DrawConnections();
            showRawFields = EditorGUILayout.Foldout(showRawFields, "编辑原始字段与备注", true);
            if (showRawFields)
            {
                EditorGUILayout.LabelField("原节点类型", selected.TypeName, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("画布节点 ID", selected.Id, EditorStyles.wordWrappedMiniLabel);
                EditorGUI.BeginChangeCheck();
                // 备注属于原画布数据，保存时仍可回到 RO 编辑器查看。
                string content = EditorGUILayout.TextField("备注", selected.Content);
                if (EditorGUI.EndChangeCheck()) { selected.Content = content; config.IsDirty = true; }
                EditorGUI.BeginChangeCheck();
                fields.Draw(selected.Data);
                if (EditorGUI.EndChangeCheck()) { config.IsDirty = true; SceneView.RepaintAll(); }
            }
        }
        else GUILayout.Label("选择一个波次、机制节点或区域", EditorStyles.boldLabel);
        DrawPreviewLog();
        EditorGUILayout.Space();
        showGlobalSettings = EditorGUILayout.Foldout(showGlobalSettings, "全局配置：出生点 / 副本全部目标", true);
        if (!showGlobalSettings) return;
        GUILayout.Label("正式出生点（客户端与服务端共用）", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        fields.Draw(config.Document.Root.Element("data").Element("player_spawn"));
        if (EditorGUI.EndChangeCheck()) config.IsDirty = true;
        DrawDungeonTargets();
    }

    /// <summary>读取副本、目标与目标组生成表，不初始化编辑模式下无关的运行时模块。</summary>
    private void LoadDungeonTargets()
    {
        stageFlowNeedsRefresh = true;
        // 正式表由项目 Luban 生成流程复制到共享 Network 配置目录。
        string dataPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../LYF_Server/Network/LubanManage/Tb"));
        dungeonTargetTable = null;
        dungeonsTable = null;
        dungeonTargetGroupTable = null;
        if (!File.Exists(Path.Combine(dataPath, "tbdungeonstable.bytes"))) return;
        dungeonsTable = new TbDungeonsTable(new Luban.ByteBuf(File.ReadAllBytes(Path.Combine(dataPath, "tbdungeonstable.bytes"))));
        dungeonTargetTable = new TbDungeonTargetTable(new Luban.ByteBuf(File.ReadAllBytes(Path.Combine(dataPath, "tbdungeontargettable.bytes"))));
        dungeonTargetGroupTable = new TbDungeonTargetGroupTable(new Luban.ByteBuf(File.ReadAllBytes(Path.Combine(dataPath, "tbdungeontargetgrouptable.bytes"))));
        dungeonIds = new int[dungeonsTable.DataList.Count];
        dungeonNames = new string[dungeonIds.Length];
        // 下拉框保留全部表行，不局限于已迁入美术资源的副本。
        for (int index = 0; index < dungeonIds.Length; index++)
        {
            // 当前表行提供唯一编号与显示名称。
            DungeonsTable dungeon = dungeonsTable.DataList[index];
            dungeonIds[index] = dungeon.DungeonsID;
            dungeonNames[index] = dungeon.DungeonsID + " · " + dungeon.DungeonsName;
        }
    }

    /// <summary>按 XML 资源路径匹配场景登记，打开北森配置时不误用魔神塔地图或模型。</summary>
    private void BindSceneResource()
    {
        sceneResource = null;
        catalog = null;
        if (dungeonsTable == null) return;
        // 按表行匹配已登记的场景，不从 XML 内维护第二套目标编号。
        for (int index = 0; index < dungeonsTable.DataList.Count; index++)
        {
            // 当前副本可能与同主题的其他难度共用 SceneID。
            DungeonsTable dungeon = dungeonsTable.DataList[index];
            // 只检查已有资源登记，未迁入的场景没有默认回退。
            DungeonSceneResource resource = DungeonSceneResource.Load(dungeon.SceneID);
            if (resource == null) continue;
            // 原 XML 文件地址与 Resources 相对路径的实际映射。
            string path = "Assets/GM_Game/Resources/" + resource.flowResource + ".txt";
            if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(currentPath), StringComparison.OrdinalIgnoreCase)) continue;
            sceneResource = resource;
            selectedDungeonId = dungeon.DungeonsID;
            catalog = ROSceneCreatureCatalog.Load(resource.creatureResource);
            break;
        }
    }

    /// <summary>完整展示所选副本的全部阶段及组分支，源表是目标定义的唯一编辑入口。</summary>
    private void DrawDungeonTargets()
    {
        GUILayout.Label("副本目标（Luban）", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("打开副本表"))
            EditorUtility.OpenWithDefaultApp(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Luaban/MiniTemplate/Datas/#DungeonsTable.xlsx")));
        if (GUILayout.Button("打开目标配置表"))
        {
            // 打开项目正式源表；本窗口不把文案或目标条件写回场景 XML。
            string sourcePath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../Luaban/MiniTemplate/Datas/#DungeonTargetTable.xlsx"));
            EditorUtility.OpenWithDefaultApp(sourcePath);
        }
        if (GUILayout.Button("重新读取")) LoadDungeonTargets();
        EditorGUILayout.EndHorizontal();
        if (dungeonTargetTable == null || dungeonsTable == null)
        {
            EditorGUILayout.HelpBox("未找到副本目标生成表，请先运行项目 Luban 导出。", MessageType.Warning);
            return;
        }
        selectedDungeonId = EditorGUILayout.IntPopup("查看副本目标", selectedDungeonId, dungeonNames, dungeonIds);
        // 目标面板可查看尚未迁入场景的表行，不会改变当前打开 XML 的波次或地图。
        DungeonsTable dungeon = dungeonsTable.GetOrDefault(selectedDungeonId);
        if (dungeon == null) return;
        EditorGUILayout.LabelField("场景编号 / 目标模式", dungeon.SceneID + " / " + dungeon.DungeonTargetType);
        if (string.IsNullOrEmpty(dungeon.DungeonTarget))
        {
            EditorGUILayout.HelpBox("该副本未配置目标。", MessageType.Info);
            return;
        }
        if (dungeon.DungeonTargetType == 2)
        {
            DrawDungeonTargetGroups(dungeon.DungeonTarget);
            return;
        }
        // 原配置用 | 分阶段，用 = 表示同阶段同时存在的目标。
        string[] sections = dungeon.DungeonTarget.Split('|');
        for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            EditorGUILayout.LabelField("阶段 " + (sectionIndex + 1), EditorStyles.boldLabel);
            // 当前阶段的原始目标编号顺序。
            string[] targetIds = sections[sectionIndex].Split('=');
            for (int index = 0; index < targetIds.Length; index++)
                DrawDungeonTarget(int.Parse(targetIds[index], CultureInfo.InvariantCulture));
        }
    }

    /// <summary>展示原组有向边及各组内全部目标，分支不会被误当成线性阶段。</summary>
    /// <param name="definition">DungeonsTable 原始分支串，逗号分边，等号左侧为父组。</param>
    private void DrawDungeonTargetGroups(string definition)
    {
        // 已经显示的组仅去重正文，边仍完整保留，汇合节点不会丢失关系。
        System.Collections.Generic.HashSet<int> displayedGroups = new System.Collections.Generic.HashSet<int>();
        // 每条边包含一个父组和一个或多个可选后继组。
        string[] edges = definition.Split(',');
        for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++)
        {
            EditorGUILayout.LabelField("分支 " + edges[edgeIndex].Replace("=", " → ").Replace("|", " / "), EditorStyles.wordWrappedLabel);
            // 关系中的编号都是组编号，组内 Targets 才是真正的目标编号。
            string[] groupIds = edges[edgeIndex].Split('=', '|');
            for (int index = 0; index < groupIds.Length; index++)
            {
                // 当前关系引用的组编号。
                int groupId = int.Parse(groupIds[index], CultureInfo.InvariantCulture);
                if (!displayedGroups.Add(groupId)) continue;
                // 原组定义按自身数组顺序展示同组目标。
                DungeonTargetGroupTable group = dungeonTargetGroupTable.GetOrDefault(groupId);
                EditorGUILayout.LabelField("组 " + group.ID + " · " + group.Des, EditorStyles.boldLabel);
                for (int targetIndex = 0; targetIndex < group.Targets.Length; targetIndex++)
                    DrawDungeonTarget(group.Targets[targetIndex]);
            }
        }
    }

    /// <summary>显示一条正式目标定义，不把目标条件或文案复制回场景 XML。</summary>
    /// <param name="targetId">DungeonTargetTable 的原始目标编号。</param>
    private void DrawDungeonTarget(int targetId)
    {
        // 目标定义只从共享 Luban 生成表读取。
        DungeonTargetTable target = dungeonTargetTable.GetOrDefault(targetId);
        EditorGUILayout.LabelField(target.ID + "  " + target.TargetDes, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("目标参数", string.Join("|", target.Target));
        EditorGUILayout.LabelField("引导坐标", string.Join("=", target.GuidePos));
    }

    /// <summary>给波次提供直观的刷点与出场预览操作。</summary>
    private void DrawWaveTools()
    {
        // 地图定位独立于模型生成，名称未登记也可查看真实坐标。
        XElement[] points = ROSceneXml.SpawnPoints(selected.Data);
        if (points.Length > 0)
        {
            pointIndex = Mathf.Clamp(EditorGUILayout.IntSlider("当前刷点（从 1 开始）", pointIndex + 1, 1, points.Length) - 1, 0, points.Length - 1);
            GUILayout.Label("世界坐标 " + ROSceneNodePresentation.PositionText(points[pointIndex], "position"), EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("定位地图刷点")) FrameSelectedPoint();
            if (GUILayout.Button("定位全部刷点")) FrameAllPoints();
            EditorGUILayout.EndHorizontal();
        }
        else EditorGUILayout.HelpBox("此波为逻辑节点，没有地图刷点。可通过下方相邻节点查看关联对象。", MessageType.Info);
        // 现有模型预览仅实现 monster_info，NPC 和采集物仍可独立定位原刷点。
        bool supportsModelPreview = ROSceneNodePresentation.HasField(selected, "monster_info");
        if (!supportsModelPreview) EditorGUILayout.HelpBox("此类型当前只支持地图刷点查看，模型预览尚未实现。", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(!supportsModelPreview))
        {
            if (GUILayout.Button("预览此波怪物")) { EnsurePreview(); if (preview != null) { preview.ShowWave(selected); FrameSelectedPoint(); } }
        }
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
        int id = ROSceneNodePresentation.EntityId(selected);
        // 未登记模型不妨碍刷点定位，只隐藏不存在的动作目录。
        ROSceneCreatureEntity entity = null;
        if (catalog != null && supportsModelPreview) entity = catalog.FindEntity(id);
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
                    menu.AddItem(new GUIContent(animation.displayName), false, () => { EnsurePreview(); if (preview == null) return; preview.ShowWave(selected); preview.PlayAnimation(selected.WaveId, AssetDatabase.LoadAssetAtPath<AnimationClip>(animation.clipPath)); FrameSelectedPoint(); });
                }
                menu.ShowAsContext();
            }
        }
        if (points.Length == 0) return;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("复制当前刷点"))
        {
            // 怪物的列表刷点允许新增，NPC 单点保持原结构。
            if (points[pointIndex].Name.LocalName == "MScenePositionData") { points[pointIndex].Parent.Add(new XElement(points[pointIndex])); config.IsDirty = true; }
        }
        if (points.Length > 1 && GUILayout.Button("删除当前刷点")) { points[pointIndex].Remove(); pointIndex = 0; config.IsDirty = true; }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>先解释触发语义，再提供原区域和传送目的地的真实地图入口。</summary>
    private void DrawMechanismSummary()
    {
        EditorGUILayout.HelpBox(ROSceneNodePresentation.FlowSummary(config, selected), MessageType.None);
        // 只检查当前触发器的真实时机与结果，不遍历整个触发组。
        List<MSceneEditorNode> nodes = ROSceneNodePresentation.FlowNodes(config, selected);
        // 记录是否存在可定位对象，纯事件节点只显示逻辑提示。
        bool hasLocation = false;
        for (int index = 0; index < nodes.Count; index++)
        {
            // 当前节点可能提供来源区域或传送坐标。
            MSceneEditorNode node = nodes[index];
            if (ROSceneNodePresentation.HasSourceArea(node))
            {
                // 原时机的业务编号用于关联区域对象。
                int areaId = ROSceneXml.Int(node.Data, "scene_trigger_obj_id");
                // 未找到原对象时只报告缺失，不默认为零坐标。
                XElement area = ROSceneNodePresentation.FindArea(config, areaId);
                if (area == null) EditorGUILayout.HelpBox("来源区域 " + areaId + " 未在当前 XML 中找到。", MessageType.Warning);
                else
                {
                    hasLocation = true;
                    GUILayout.Label("来源区域 " + areaId + " · " + ROSceneXml.Text(area, "box/title") + "\n" + ROSceneNodePresentation.PositionText(area, "position"), EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("定位来源区域 " + areaId)) FrameArea(area);
                }
            }
            if (ROSceneXml.Find(node.Data, "trans_position_info/position") == null) continue;
            hasLocation = true;
            GUILayout.Label("传送终点 " + ROSceneNodePresentation.PositionText(node.Data, "trans_position_info/position") + "\n目标场景 " + ROSceneXml.Text(node.Data, "trans_position_info/scene_id"), EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("定位传送终点")) FrameDestination(node);
        }
        if (!hasLocation) EditorGUILayout.HelpBox("这是逻辑节点，本身没有可定位的地图坐标。下方列出真实连接的关联节点，可跳转继续查看。", MessageType.Info);
    }

    /// <summary>列出所选节点的真实连接，支持移除后重新接线。</summary>
    private void DrawConnections()
    {
        GUILayout.Label("相邻节点（仅定位画布）", EditorStyles.boldLabel);
        for (int i = 0; i < config.Connections.Count; i++)
        {
            // 当前节点的数据元素在 Rebuild 后仍是同一 XML 对象。
            MSceneEditorConnection connection = config.Connections[i];
            // 当前节点一侧的端口显示中文用途并保留真实英文名。
            MSceneEditorPort local = connection.First;
            // 另一端用于显示具体行为和画布跳转。
            MSceneEditorPort remote = connection.Second;
            if (remote.Owner.Element == selected.Element) { local = connection.Second; remote = connection.First; }
            if (local.Owner.Element != selected.Element) continue;
            EditorGUILayout.BeginHorizontal();
            // 两端均说明用途，避免只看到相邻节点标题却无法辨认连接关系。
            GUIContent connectionLabel = new GUIContent(ROSceneNodePresentation.PortLabel(local) + " ↔ " + ROSceneNodePresentation.Title(remote.Owner, catalog) + "\n" + ROSceneNodePresentation.PortLabel(remote), ROSceneNodePresentation.PortDescription(local) + "\n" + ROSceneNodePresentation.PortDescription(remote));
            GUILayout.Label(connectionLabel, EditorStyles.wordWrappedMiniLabel);
            // 跳转仅改变当前选择，不会移动地图或修改连线。
            bool jump = GUILayout.Button("画布", GUILayout.Width(45));
            // 移除后立即结束当前遍历，下一帧读取更新后的连接列表。
            bool remove = GUILayout.Button("断开", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();
            if (jump) { SelectNode(remote.Owner); break; }
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
        if (config == null) return;
        // 最近一次定位标记只在其明确登记的目标地图绘制，包括跨场景传送目的地。
        if (focusedMapPath.Length > 0 && SceneManager.GetActiveScene().path == focusedMapPath)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(focusedMapPosition, Vector3.up, 1f);
            Handles.DrawLine(focusedMapPosition, focusedMapPosition + Vector3.up * 3f);
            Handles.Label(focusedMapPosition + Vector3.up * 3f, focusedMapLabel);
        }
        if (string.IsNullOrEmpty(CurrentScenePath) || SceneManager.GetActiveScene().path != CurrentScenePath) return;
        if (showAreas)
        {
            for (int i = 0; i < config.TriggerObjects.Length; i++)
            {
                // 原区域的偏移在朝向旋转之后应用。
                XElement area = config.TriggerObjects[i];
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
                if (preview != null && !preview.VisibleObjects.Contains(ROSceneXml.Int(area, "id"))) Handles.color = Color.gray;
                DrawAreaMarker(area, "区域");
            }
        }
        if (selectedArea != null)
        {
            Handles.color = Color.yellow;
            DrawAreaMarker(selectedArea, "所选区域");
            EditorGUI.BeginChangeCheck();
            // 场景平移工具直接写回当前区域坐标。
            Vector3 moved = Handles.PositionHandle(ROSceneXml.Vector(selectedArea, "position"), Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) { ROSceneXml.SetVector(selectedArea, "position", moved); config.IsDirty = true; stageFlowNeedsRefresh = true; Repaint(); }
        }
        if (selected != null && selected.IsWave) DrawSpawnHandles();
        if (selected != null && !selected.IsWave) DrawMechanismMarkers();
        if (preview != null && preview.Running)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(preview.PlayerPosition, Vector3.up, 0.7f);
            Handles.Label(preview.PlayerPosition + Vector3.up, "预览角色（拖动可触发区域）");
            preview.PlayerPosition = Handles.PositionHandle(preview.PlayerPosition, Quaternion.identity);
        }
    }

    /// <summary>绘制区域原始旋转盒体和具体说明，即使未生成预览物件也可识别。</summary>
    /// <param name="area">原区域配置。</param>
    /// <param name="prefix">当前标记是普通区域、所选区域还是触发来源。</param>
    private static void DrawAreaMarker(XElement area, string prefix)
    {
        // 原区域位置与朝向共同决定碰撞盒的世界变换。
        Vector3 position = ROSceneXml.Vector(area, "position");
        // face 使用角度制，绕世界 Y 轴旋转。
        Quaternion rotation = Quaternion.Euler(0, ROSceneXml.Float(area, "face"), 0);
        using (new Handles.DrawingScope(Matrix4x4.TRS(position, rotation, Vector3.one))) Handles.DrawWireCube(ROSceneXml.Vector(area, "box/offset"), ROSceneXml.Vector(area, "box/scale"));
        Handles.Label(position + Vector3.up, prefix + " " + ROSceneXml.Int(area, "id") + " · " + ROSceneXml.Text(area, "box/title") + "\n" + ROSceneNodePresentation.PositionText(area, "position"));
    }

    /// <summary>标注所选机制真实连接的来源和传送目的地，不使用画布坐标。</summary>
    private void DrawMechanismMarkers()
    {
        // 当前触发器上下文与右侧摘要保持同一份连接解释。
        List<MSceneEditorNode> nodes = ROSceneNodePresentation.FlowNodes(config, selected);
        for (int index = 0; index < nodes.Count; index++)
        {
            // 每个关联节点至多提供一个来源区域或传送目的地。
            MSceneEditorNode node = nodes[index];
            if (ROSceneNodePresentation.HasSourceArea(node))
            {
                // 未解析的区域不画零坐标。
                XElement area = ROSceneNodePresentation.FindArea(config, ROSceneXml.Int(node.Data, "scene_trigger_obj_id"));
                if (area != null) { Handles.color = Color.yellow; DrawAreaMarker(area, "触发来源"); }
            }
            if (ROSceneXml.Find(node.Data, "trans_position_info/position") == null) continue;
            if (DestinationScenePath(node) != CurrentScenePath) continue;
            // 只在目的地所属地图绘制传送点，不把跨场景坐标画在来源地图。
            Vector3 position = ROSceneXml.Vector(node.Data, "trans_position_info/position");
            Handles.color = Color.magenta;
            Handles.DrawWireDisc(position, Vector3.up, 1f);
            Handles.DrawLine(position, position + Vector3.up * 2f);
            Handles.Label(position + Vector3.up * 2f, "传送终点 " + ROSceneNodePresentation.PositionText(node.Data, "trans_position_info/position"));
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
            if (i == pointIndex) Handles.color = Color.yellow;
            Handles.DrawLine(position, position + Vector3.up * 2f);
            Handles.Label(position + Vector3.up * 2f, ROSceneNodePresentation.Title(selected, catalog) + "\n刷点 " + (i + 1) + "/" + points.Length + " ×" + ROSceneXml.Int(points[i], "count", 1) + " " + ROSceneNodePresentation.PositionText(points[i], "position"));
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
                stageFlowNeedsRefresh = true;
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
        FramePosition(ROSceneXml.Vector(points[pointIndex], "position"), CurrentScenePath, ROSceneNodePresentation.Title(selected, catalog) + " · 刷点 " + (pointIndex + 1));
    }

    /// <summary>框选当前波次所有原刷点及出生半径，无需生成怪物模型。</summary>
    private void FrameAllPoints()
    {
        // 按原刷点集合计算范围，不修改坐标或数量。
        XElement[] points = ROSceneXml.SpawnPoints(selected.Data);
        if (points.Length == 0) return;
        // 边界至少包含第一个真实点。
        Bounds bounds = new Bounds(ROSceneXml.Vector(points[0], "position"), Vector3.one);
        for (int index = 0; index < points.Length; index++)
        {
            // 每个点的出生半径也计入观察范围。
            float radius = Mathf.Max(0.4f, ROSceneXml.Float(points[index], "born_radius"));
            bounds.Encapsulate(new Bounds(ROSceneXml.Vector(points[index], "position"), Vector3.one * radius * 2f));
        }
        FramePosition(bounds.center, CurrentScenePath, ROSceneNodePresentation.Title(selected, catalog) + " · 全部 " + points.Length + " 个刷点", Mathf.Max(12f, bounds.extents.magnitude * 1.3f));
    }

    /// <summary>定位来源区域时保持所选机制，便于继续查看其结果。</summary>
    /// <param name="area">原 XML 中的真实区域。</param>
    private void FrameArea(XElement area)
    {
        if (ROSceneXml.Find(area, "position") == null) return;
        FramePosition(ROSceneXml.Vector(area, "position"), CurrentScenePath, "区域 " + ROSceneXml.Int(area, "id") + " · " + ROSceneXml.Text(area, "box/title"));
    }

    /// <summary>按传送结果中的场景编号解析目标地图；未登记时不借用当前场景。</summary>
    /// <param name="node">包含 trans_position_info 的结果节点。</param>
    private string DestinationScenePath(MSceneEditorNode node)
    {
        // 目标场景编号来自传送结果，不取当前下拉查看的副本目标编号。
        int sceneId = ROSceneXml.Int(node.Data, "trans_position_info/scene_id");
        if (sceneResource != null && sceneResource.sceneId == sceneId) return CurrentScenePath;
        // 跨场景目的地必须有独立资源登记。
        DungeonSceneResource destination = DungeonSceneResource.Load(sceneId);
        if (destination == null || string.IsNullOrEmpty(destination.sceneLocation)) return "";
        return destination.sceneLocation + ".unity";
    }

    /// <summary>定位传送结果的原坐标，允许切换到明确登记的目的地地图。</summary>
    /// <param name="node">含真实传送坐标的结果节点。</param>
    private void FrameDestination(MSceneEditorNode node)
    {
        FramePosition(ROSceneXml.Vector(node.Data, "trans_position_info/position"), DestinationScenePath(node), "传送终点 · 场景 " + ROSceneXml.Text(node.Data, "trans_position_info/scene_id"));
    }

    /// <summary>先确认正确地图再让 Scene View 观察原坐标；取消打开不继续定位。</summary>
    /// <param name="position">需要查看的世界坐标。</param>
    /// <param name="scenePath">坐标所属的资源登记地图。</param>
    /// <param name="label">标记的对象名称与用途。</param>
    /// <param name="size">观察范围，单位为场景米。</param>
    private void FramePosition(Vector3 position, string scenePath, string label, float size = 12f)
    {
        if (!OpenMap(scenePath)) return;
        focusedMapPath = scenePath;
        focusedMapPosition = position;
        focusedMapLabel = label;
        // Scene View 由 Unity 管理，不改动正式场景相机。
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) view = GetWindow<SceneView>();
        view.LookAt(position + Vector3.up, Quaternion.Euler(35f, 200f, 0f), size);
        view.Focus();
        SceneView.RepaintAll();
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
    private void OpenDungeonScene()
    {
        OpenMap(CurrentScenePath);
    }

    /// <summary>仅打开已登记且存在的目标地图，保留 Unity 原生未保存场景确认。</summary>
    /// <param name="scenePath">待打开的真实 Unity 场景资源路径。</param>
    private bool OpenMap(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
        {
            ShowNotification(new GUIContent("该坐标所属地图未登记或尚未迁入，无法定位"));
            return false;
        }
        if (SceneManager.GetActiveScene().path == scenePath) return true;
        if (EditorApplication.isPlaying)
        {
            ShowNotification(new GUIContent("请退出运行模式后切换地图"));
            return false;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        StopPreview();
        EditorSceneManager.OpenScene(scenePath);
        return SceneManager.GetActiveScene().path == scenePath;
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
        if (string.IsNullOrEmpty(CurrentScenePath) || SceneManager.GetActiveScene().path != CurrentScenePath) return;
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
