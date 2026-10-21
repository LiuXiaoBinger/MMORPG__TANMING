using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>主城与副本共用的地图导航导出窗口，选择场景及其烘焙资产后生成一份公共文件。</summary>
public sealed class MNavigationExportWindow : EditorWindow
{
    /// <summary>仅用于命名和标识地图的场景资产；导出过程不会打开或保存该场景。</summary>
    [SerializeField] private SceneAsset _sceneAsset;

    /// <summary>该地图已经烘焙的导航数据，保留用户原资产及其世界坐标。</summary>
    [SerializeField] private NavMeshData _navMeshData;

    /// <summary>显示最近一次导出的文件路径、顶点和三角形数量，不参与地图运行逻辑。</summary>
    private string _exportResult = string.Empty;

    /// <summary>反读公共文件并自绘可走面及网格线，窗口关闭释放临时绘制资源。</summary>
    private readonly MNavigationMapPreview _preview = new MNavigationMapPreview();

    /// <summary>当前公共文件预览的来源及实际顶点、三角形数量。</summary>
    private string _previewResult = string.Empty;

    /// <summary>保存窗口纵向滚动位置，窄窗口或原有较矮窗口也能访问底部预览按钮。</summary>
    private Vector2 _scrollPosition;

    /// <summary>从Unity菜单打开所有地图复用的导出窗口。</summary>
    [MenuItem("工具/导航/导出公共地图网格")]
    public static void OpenWindow()
    {
        GetWindow<MNavigationExportWindow>("公共导航导出");
    }

    /// <summary>绘制来源选择和导出按钮；数据始终写入项目根NavigationData目录。</summary>
    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        EditorGUILayout.HelpBox("烘焙仍使用Unity AI Navigation；本工具负责导出公共文件，并反读文件自行绘制可走面和网格线。导出时先打开Boot或空场景，查看时打开对应地图。", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        _sceneAsset = (SceneAsset)EditorGUILayout.ObjectField("地图场景", _sceneAsset, typeof(SceneAsset), false);
        _navMeshData = (NavMeshData)EditorGUILayout.ObjectField("已烘焙导航数据", _navMeshData, typeof(NavMeshData), false);
        if (EditorGUI.EndChangeCheck())
        {
            _preview.Hide();
            _previewResult = string.Empty;
        }
        EditorGUILayout.HelpBox("每张地图生成NavigationData/<场景名>.navmesh.bytes。客户端和服务器读取同一文件；文件保留三维顶点、三角形及区域，不包含跳跃链接或动态障碍。", MessageType.None);

        // 来源未选齐时按钮不可用，避免生成无法标识地图的文件。
        using (new EditorGUI.DisabledScope(_sceneAsset == null || _navMeshData == null))
        {
            if (GUILayout.Button("导出公共导航网格"))
            {
                ExportSelectedMap();
            }
        }

        if (!string.IsNullOrEmpty(_exportResult))
        {
            EditorGUILayout.HelpBox(_exportResult, MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("公共文件寻路网格预览", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("从公共bytes读取三角形，自绘蓝色行走面，内部网格淡显，障碍及外轮廓深显；不透画后方网格，不依赖场景中存在Surface。预览期间暂停原生蓝面，隐藏或关闭恢复。", MessageType.None);
        using (new EditorGUI.DisabledScope(_sceneAsset == null))
        {
            if (GUILayout.Button("显示 / 重新读取网格"))
            {
                ShowSelectedMap();
            }
        }

        using (new EditorGUI.DisabledScope(!_preview.IsVisible))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("定位网格"))
            {
                _preview.Frame();
            }
            if (GUILayout.Button("隐藏网格"))
            {
                _preview.Hide();
                _previewResult = string.Empty;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (!string.IsNullOrEmpty(_previewResult))
        {
            EditorGUILayout.HelpBox(_previewResult, MessageType.Info);
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>按场景名反读已导出的公共文件，不加载烘焙资产或修改导航世界。</summary>
    private void ShowSelectedMap()
    {
        // 场景选择只决定文件路径，查看已有文件不要求提供NavMeshData。
        string filePath = GetOutputPath();
        if (!File.Exists(filePath))
        {
            _previewResult = "尚未找到公共文件，请先导出：" + filePath;
            return;
        }
        // 数量来源为本次磁盘读取结果，不读取Unity全局导航三角网。
        MNavigationMapData mapData = _preview.Show(filePath);
        _previewResult = "自绘来源：" + filePath + "\n顶点：" + mapData.Vertices.Length + "，三角形：" + mapData.Areas.Length + "\nAgentTypeID：" + mapData.AgentTypeID;
    }

    /// <summary>窗口关闭或域重载时清理自绘缓存，并恢复预览前的原生显示开关。</summary>
    private void OnDisable()
    {
        _preview.Hide();
    }

    /// <summary>取得所选地图导出文件的绝对路径，主城和副本分别按场景名保存。</summary>
    private string GetOutputPath()
    {
        // Unity工程位于根目录LYF_MMO下，两端公共文件统一放在根NavigationData目录。
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
        return Path.Combine(projectRoot, "NavigationData", _sceneAsset.name + ".navmesh.bytes");
    }

    /// <summary>将当前选择写入根目录公共资源源文件；同名地图再次导出覆盖其旧版本。</summary>
    private void ExportSelectedMap()
    {
        // 每张地图独立命名；主城和副本切换选择后复用同一个导出入口。
        string outputPath = GetOutputPath();
        // 使用两端共用结构写出，并显示本次实际导出的网格数量。
        MNavigationMapData mapData = MNavigationMapExporter.Export(_sceneAsset.name, _navMeshData, outputPath);
        _exportResult = "导出完成：" + outputPath + "\n顶点：" + mapData.Vertices.Length + "，三角形：" + mapData.Areas.Length;
        Debug.Log(_exportResult);
    }
}
