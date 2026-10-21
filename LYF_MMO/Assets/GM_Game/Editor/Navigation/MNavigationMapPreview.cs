using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>反读公共地图文件，在当前Scene相机自绘浅蓝可走面和三角边线，不调用原生导航绘制。</summary>
public sealed class MNavigationMapPreview
{
    /// <summary>仅用于预览期间暂停原生蓝面，避免两份网格叠色；隐藏时恢复。</summary>
    private static readonly PropertyInfo _showNavMeshProperty = typeof(NavMeshVisualizationSettings).GetProperty("showNavMesh", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

    /// <summary>公共文件原始世界坐标组成的32位面网格，隐藏时销毁。</summary>
    private Mesh _mesh;

    /// <summary>按完全相同XYZ去重后的绘制边网格，子网格0为内部边、1为外轮廓，不修改文件。</summary>
    private Mesh _lineMesh;

    /// <summary>蓝面、淡内部线和深色外轮廓三种临时材质，不绘制被遮挡的后方网格。</summary>
    private Material[] _materials;

    /// <summary>当前Scene相机正常绘制时执行的预览指令，隐藏时解绑并释放。</summary>
    private CommandBuffer _commands;

    /// <summary>本次预览所属的真实Scene相机，不影响Game相机或正式场景对象。</summary>
    private Camera _camera;

    /// <summary>Scene相机在预览前的深度纹理模式，隐藏时恢复，不影响用户后续观察。</summary>
    private DepthTextureMode _previousDepthTextureMode;

    /// <summary>当前Terrain世界高度的临时RFloat快照，只用于判断遮挡点是否为地表，隐藏时销毁。</summary>
    private Texture2D _terrainHeights;

    /// <summary>当前高度快照的Terrain世界原点，不用于移动或修改导航顶点。</summary>
    private Vector3 _terrainOrigin;

    /// <summary>Terrain世界尺寸及高度图分辨率，W分量为边长像素数，用于正确采样像素中心。</summary>
    private Vector4 _terrainSize;

    /// <summary>显示前的原生网格开关，结束、关闭或域重载时恢复。</summary>
    private bool _previousShowNavMesh;

    /// <summary>只有实际接管过原生开关时才恢复，空预览隐藏不改变编辑器设置。</summary>
    private bool _restoreShowNavMesh;

    /// <summary>文件世界坐标包围盒，仅用于用户主动点击定位。</summary>
    private Bounds _bounds;

    /// <summary>是否已缓存公共文件并将自绘指令接入Scene相机。</summary>
    public bool IsVisible => _mesh != null;

    /// <summary>读取公共bytes并向当前Scene相机添加自绘指令；保持用户当前近景镜头。</summary>
    /// <param name="filePath">项目根NavigationData内所选地图的公共网格绝对路径。</param>
    /// <returns>本次从磁盘实际读取的网格，供窗口显示数量及来源。</returns>
    public MNavigationMapData Show(string filePath)
    {
        // 先读取文件，读取失败不会清掉当前正在观察的网格。
        MNavigationMapData mapData = MNavigationMapSerializer.Load(filePath);
        Hide();
        // 文件XYZ保持原值，预览不重新吸附、收缩或筛掉地图层。
        Vector3[] vertices = new Vector3[mapData.Vertices.Length];
        for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            // 当前顶点为公共文件世界坐标，单位沿用Unity地图。
            MNavigationPoint point = mapData.Vertices[vertexIndex];
            vertices[vertexIndex] = new Vector3(point.X, point.Y, point.Z);
        }
        _mesh = new Mesh();
        _mesh.name = "公共导航可走面预览";
        _mesh.hideFlags = HideFlags.HideAndDontSave;
        _mesh.indexFormat = IndexFormat.UInt32;
        _mesh.vertices = vertices;
        _mesh.triangles = mapData.Indices;
        _mesh.RecalculateBounds();
        _bounds = _mesh.bounds;

        _lineMesh = CreateLineMesh(vertices, mapData.Indices);
        CreateTerrainHeightTexture();

        // 面写入深度，后方导航层不再穿透叠色；内部线淡显，障碍和外轮廓深显。
        _materials = new Material[3];
        _materials[0] = CreateMaterial(new Color(0f, 0.76f, 1f, 0.5f), true, 0f);
        _materials[1] = CreateMaterial(new Color(0f, 0.12f, 0.18f, 0.26f), false, 0.02f);
        _materials[2] = CreateMaterial(new Color(0f, 0.10f, 0.15f, 0.85f), false, 0.02f);
        // 复用当前Scene窗口，只有用户主动点定位才改变观察范围。
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            sceneView = EditorWindow.GetWindow<SceneView>();
        }
        _camera = sceneView.camera;
        _previousDepthTextureMode = _camera.depthTextureMode;
        _camera.depthTextureMode |= DepthTextureMode.Depth;
        _commands = new CommandBuffer();
        _commands.name = "公共文件导航网格自绘";
        _commands.DrawMesh(_mesh, Matrix4x4.identity, _materials[0]);
        _commands.DrawMesh(_lineMesh, Matrix4x4.identity, _materials[1], 0);
        _commands.DrawMesh(_lineMesh, Matrix4x4.identity, _materials[2], 1);
        _camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _commands);
        _previousShowNavMesh = (bool)_showNavMeshProperty.GetValue(null, null);
        _restoreShowNavMesh = true;
        _showNavMeshProperty.SetValue(null, false, null);
        AssemblyReloadEvents.beforeAssemblyReload += Hide;
        sceneView.Focus();
        SceneView.RepaintAll();
        return mapData;
    }

    /// <summary>创建面或线的透明预览材质，仅对实际Terrain遮挡点允许有界贴地显示。</summary>
    /// <param name="color">面或边线的RGB及透明度，保留地表纹理并区分内部线和外轮廓。</param>
    /// <param name="writeDepth">蓝面写入深度遮住后方导航层，边线只读深度。</param>
    /// <param name="viewOffset">线条向相机偏移的米数，面为0，细线为0.02以避免共面遮线。</param>
    /// <returns>隐藏预览时统一销毁的临时材质。</returns>
    private Material CreateMaterial(Color color, bool writeDepth, float viewOffset)
    {
        // 专用Shader保持正常深度测试，只有地表高度吻合且偏差不超过1.65米时修正显示深度。
        Material material = new Material(Shader.Find("Hidden/LYF/NavigationPreview"));
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetColor("_Color", color);
        material.SetInt("_ZWrite", 0);
        if (writeDepth)
        {
            material.SetInt("_ZWrite", 1);
        }
        material.SetFloat("_ViewOffset", viewOffset);
        material.SetFloat("_TerrainEnabled", 0f);
        if (_terrainHeights != null)
        {
            material.SetFloat("_TerrainEnabled", 1f);
            material.SetTexture("_TerrainHeights", _terrainHeights);
            material.SetVector("_TerrainOrigin", _terrainOrigin);
            material.SetVector("_TerrainSize", _terrainSize);
            // 主城三角52401的861点采样最大误差1.590706米，仅预览允许1.65米上限。
            material.SetFloat("_TerrainTolerance", 1.65f);
        }
        return material;
    }

    /// <summary>读取当前主城Terrain的高度快照，不重烘焙；没有Terrain的地图沿用正常深度显示。</summary>
    private void CreateTerrainHeightTexture()
    {
        // 当前地图只有一块Terrain，按现有实际场景取数据，不建立额外地形管理层。
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            return;
        }
        // 高度图采样边长来自实际地形资源，主城为513，包含边界两端。
        int resolution = terrain.terrainData.heightmapResolution;
        // Unity返回归一化高度，第一维为Z，第二维为X。
        float[,] heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
        // RFloat保存世界Y，不使用颜色空间转换，避免与真实地表高度比较时发生偏差。
        float[] worldHeights = new float[resolution * resolution];
        _terrainOrigin = terrain.transform.position;
        _terrainSize = terrain.terrainData.size;
        _terrainSize.w = resolution;
        // 高度图Z行序号，对应Terrain局部Z方向从起点到终点。
        for (int z = 0; z < resolution; z++)
        {
            // 当前行X列序号，对应Terrain局部X方向从起点到终点。
            for (int x = 0; x < resolution; x++)
            {
                worldHeights[z * resolution + x] = heights[z, x] * _terrainSize.y + _terrainOrigin.y;
            }
        }
        _terrainHeights = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true);
        _terrainHeights.name = "公共导航Terrain高度快照";
        _terrainHeights.hideFlags = HideFlags.HideAndDontSave;
        _terrainHeights.wrapMode = TextureWrapMode.Clamp;
        _terrainHeights.filterMode = FilterMode.Bilinear;
        _terrainHeights.SetPixelData(worldHeights, 0);
        _terrainHeights.Apply();
    }

    /// <summary>建立仅供显示的去重边，完全相同XYZ的端点视为同一点，保留不同高度的楼层。</summary>
    /// <param name="vertices">公共文件原始世界坐标，不修改任何元素。</param>
    /// <param name="indices">每三个索引构成原始三角形，不修改绕序或连接。</param>
    /// <returns>两个线子网格，分别存储内部边和只被一个三角形使用的轮廓边。</returns>
    private Mesh CreateLineMesh(Vector3[] vertices, int[] indices)
    {
        // 相同XYZ可能使用不同原始索引，统一到首次出现的顶点索引，仅用于边统计。
        Dictionary<Vector3, int> positions = new Dictionary<Vector3, int>();
        // 每个原始顶点对应的相同坐标首索引，不对位置做容差合并。
        int[] canonicalIndices = new int[vertices.Length];
        for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            // 当前坐标首次出现的位置，相同高度及XYZ才复用。
            int canonicalIndex;
            if (!positions.TryGetValue(vertices[vertexIndex], out canonicalIndex))
            {
                canonicalIndex = vertexIndex;
                positions.Add(vertices[vertexIndex], canonicalIndex);
            }
            canonicalIndices[vertexIndex] = canonicalIndex;
        }
        // 无向边对应使用它的三角形数量，端点顺序不影响统计。
        Dictionary<Vector2Int, int> edgeCounts = new Dictionary<Vector2Int, int>();
        for (int index = 0; index < indices.Length; index += 3)
        {
            AddEdge(edgeCounts, canonicalIndices[indices[index]], canonicalIndices[indices[index + 1]]);
            AddEdge(edgeCounts, canonicalIndices[indices[index + 1]], canonicalIndices[indices[index + 2]]);
            AddEdge(edgeCounts, canonicalIndices[indices[index + 2]], canonicalIndices[indices[index]]);
        }
        // 可按索引遍历的边键快照，不在遍历期间改写字典。
        List<Vector2Int> edgeKeys = new List<Vector2Int>(edgeCounts.Keys);
        // 共享边只画一次并采用淡线，避免重复透明混合加黑。
        List<int> innerEdges = new List<int>();
        // 只属于一个三角形的边以深色突出地图和障碍轮廓。
        List<int> boundaryEdges = new List<int>();
        for (int edgeIndex = 0; edgeIndex < edgeKeys.Count; edgeIndex++)
        {
            // XY分别存放已经排序的两个端点索引，不表示地图平面坐标。
            Vector2Int edgeKey = edgeKeys[edgeIndex];
            // 默认存入内部边；单侧边切换到轮廓集合。
            List<int> targetEdges = innerEdges;
            if (edgeCounts[edgeKey] == 1)
            {
                targetEdges = boundaryEdges;
            }
            targetEdges.Add(edgeKey.x);
            targetEdges.Add(edgeKey.y);
        }
        // 顶点仍是原始XYZ；仅边去重及分类，不生成场景对象或改写导航数据。
        Mesh lineMesh = new Mesh();
        lineMesh.name = "公共导航三角边线预览";
        lineMesh.hideFlags = HideFlags.HideAndDontSave;
        lineMesh.indexFormat = IndexFormat.UInt32;
        lineMesh.vertices = vertices;
        lineMesh.subMeshCount = 2;
        lineMesh.SetIndices(innerEdges.ToArray(), MeshTopology.Lines, 0);
        lineMesh.SetIndices(boundaryEdges.ToArray(), MeshTopology.Lines, 1);
        lineMesh.bounds = _bounds;
        return lineMesh;
    }

    /// <summary>按排序后的端点累计无向边，避免共享三角边被重复画深。</summary>
    /// <param name="edgeCounts">本次读取期间的绘制边统计，结束后不保留。</param>
    /// <param name="startIndex">相同XYZ规范化后的第一端点原始索引。</param>
    /// <param name="endIndex">相同XYZ规范化后的第二端点原始索引。</param>
    private void AddEdge(Dictionary<Vector2Int, int> edgeCounts, int startIndex, int endIndex)
    {
        // 较小索引固定放X，使正反方向共享键；使用双整数哈希避免打包ulong异或碰撞。
        Vector2Int edgeKey = new Vector2Int(Mathf.Min(startIndex, endIndex), Mathf.Max(startIndex, endIndex));
        // 已记录该边的三角形数量，首次出现从零增加。
        int count;
        edgeCounts.TryGetValue(edgeKey, out count);
        edgeCounts[edgeKey] = count + 1;
    }

    /// <summary>用户主动定位时将Scene镜头移到公共文件包围盒，不改场景内相机。</summary>
    public void Frame()
    {
        // 预览相机所属窗口仍是当前实际观察的Scene窗口。
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            sceneView = EditorWindow.GetWindow<SceneView>();
        }
        sceneView.Frame(_bounds, true);
    }

    /// <summary>移除Scene自绘、销毁临时网格材质并恢复原生开关；不保存或修改地图。</summary>
    public void Hide()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= Hide;
        if (_commands != null)
        {
            if (_camera != null)
            {
                _camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _commands);
            }
            _commands.Release();
            _commands = null;
        }
        if (_camera != null)
        {
            _camera.depthTextureMode = _previousDepthTextureMode;
        }
        _camera = null;
        if (_mesh != null)
        {
            Object.DestroyImmediate(_mesh);
            _mesh = null;
        }
        if (_lineMesh != null)
        {
            Object.DestroyImmediate(_lineMesh);
            _lineMesh = null;
        }
        if (_materials != null)
        {
            for (int materialIndex = 0; materialIndex < _materials.Length; materialIndex++)
            {
                Object.DestroyImmediate(_materials[materialIndex]);
            }
            _materials = null;
        }
        if (_terrainHeights != null)
        {
            Object.DestroyImmediate(_terrainHeights);
            _terrainHeights = null;
        }
        if (_restoreShowNavMesh)
        {
            _showNavMeshProperty.SetValue(null, _previousShowNavMesh, null);
            _restoreShowNavMesh = false;
        }
        SceneView.RepaintAll();
    }
}
