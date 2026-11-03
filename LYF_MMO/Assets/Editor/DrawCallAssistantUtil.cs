using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

/// <summary>
/// 根据 UI 矩形重叠、组件类型和材质，估算选中 Canvas 下可以合并绘制的批次。
/// 当前只识别 Image、RawImage 和旧 Text；估算假设文字占满矩形、Z 为 0 且没有旋转。
/// 多 Canvas 和跨层合批尚未处理，显示的数量不能代替 Unity 实际渲染统计。
/// </summary>
public class DrawCallAssistantUtil : EditorWindow
{

    #region 字段
    // 当前分析范围的节点；筛选后只保留符合本工具渲染条件的节点。
    private static List<AssetNode> _assetNodeList = new List<AssetNode>();

    // 局部坐标 Z 不为 0 的节点，单独列出以提示估算条件不满足。
    private static List<AssetNode> _zValueList = new List<AssetNode>();

    // 局部旋转不为单位旋转的节点，矩形估算可能不准确。
    private static List<AssetNode> _rotNodeList = new List<AssetNode>();

    // 图形颜色透明度为 0 的节点，供人工检查。
    private static List<AssetNode> _colorAlphaList = new List<AssetNode>();

    // 本次分析使用的 Canvas，只在选中对象及其直接父节点中查找。
    private static Canvas _canvas = null;

    // 遍历 UI 层级的起点，即找到 Canvas 的对象。
    private static GameObject _target = null;

    // 当前在结果列表中选中的节点，用于定位 Hierarchy 中的原对象。
    private static AssetNode _curSelectNode = null;

    // 是否把禁用组件和未激活节点也纳入分析；改变后需刷新结果。
    private static bool _incluedDisable = true;

    // 本工具按类型和材质估算出的绘制批次数。
    private static int _drawCall = 0;

    // 估算渲染层号到节点列表的映射，由矩形重叠关系计算。
    private static Dictionary<int, List<AssetNode>> _renderLayerDict = new Dictionary<int, List<AssetNode>>();

    // 预留的批次结果字典，当前只会在刷新时清空。
    private static Dictionary<int, List<AssetNode>> _batchDict = new Dictionary<int, List<AssetNode>>();

    // 按 MatName 统计引用次数；实际键是材质名或 Text 标签，不是图集资源 ID。
    private static Dictionary<string, int> _atlasRefDict = new Dictionary<string, int>();

    // 结果列表的滚动位置，单位为编辑器界面坐标。
    private static Vector2 _scrollPosition;

    // 已筛选节点在 Hierarchy 中的最大深度，用于对齐结果列。
    private static int _mostDepth = 0;
    #endregion

    #region 生命周期
    // 绘制刷新入口和分析列表；刷新时重新计算，列表点击只定位原 UI 对象。
    void OnGUI()
    {
        Color previousContentColor = GUI.contentColor;
        Color previousLabelColor = GUI.skin.label.normal.textColor;
        Color previousButtonColor = GUI.skin.button.normal.textColor;
        Color previousToggleColor = GUI.skin.toggle.normal.textColor;
        GUI.contentColor = Color.white;
        GUI.skin.label.normal.textColor = Color.white;
        GUI.skin.button.normal.textColor = Color.white;
        GUI.skin.toggle.normal.textColor = Color.white;

        GameObject go = Selection.activeGameObject;

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新", GUILayout.Width(60)))
        {
            if (go != null)
            {
                OnInit();
                bool exitcanvase = FindRootCanvas(go);
                if (exitcanvase && _target != null)
                {
                    //创建根节点;
                    AssetNode rootNode = new AssetNode(_target);
                    //根节点在最底层;
                    _assetNodeList.Add(rootNode);
                    //递归创建节点;
                    RecursiveCreateNode(_target);
                    //找出需要渲染的节点,并且找出错误节点;
                    FindDirtyNode();
                    //重叠检测;
                    OverlapDetection();
                    _drawCall = 0;
                    foreach (var target in _renderLayerDict)
                    {
                        _drawCall = RecursiveCombineBatch(_drawCall, target.Value);
                    }
                }
            }
        }
        GUILayout.Space(30);
        if (GUILayout.Toggle(_incluedDisable, "", GUILayout.Width(10)))
        {
            _incluedDisable = true;
        }
        else
        {
            _incluedDisable = false;
        }
        GUILayout.Label("是否包含DeActive和Disable的组件?", GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();
        if (_assetNodeList.Count > 0)
        {
            //DrawRect();
            GUIStyle style = new GUIStyle();
            style.fontSize = 12;
            GUILayout.Space(10);
            GUILayout.Label(">>>>>>>>>>史上最华丽的分割线>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>", style);
            GUILayout.Space(10);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Label(">>>>>NO.1:详情", style);
            GUILayout.Space(10);
            GUILayout.Label("   DrawCall：" + _drawCall, style);
            foreach (var temp in _atlasRefDict)
            {
                GUILayout.Label("   图集：" + temp.Key + "  引用次数：" + temp.Value, style);
            }
            GUILayout.Space(10);
            GUILayout.Label(">>>>>NO.2:以下节点存在 [z值不为0] [旋转] [颜色的alpha值为0] 等问题，请检查", style);
            GUILayout.Space(10);
            for (int i = 0; i < _zValueList.Count; i++)
            {
                GUILayout.BeginHorizontal();

                AssetNode node = _zValueList[i];
                bool state = node == _curSelectNode;
                for (int j = 0; j < node.LocalDepth; j++)
                    GUILayout.Label(" ", GUILayout.ExpandWidth(false));
                if (GUILayout.Toggle(state, "", GUILayout.Width(10)))
                {
                    if (_curSelectNode != node)
                        EditorGUIUtility.PingObject(node.gameObject);
                    _curSelectNode = node;
                }
                GUILayout.Label(node.Name + "(z != 0)", style, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }
            for (int i = 0; i < _rotNodeList.Count; i++)
            {
                GUILayout.BeginHorizontal();

                AssetNode node = _rotNodeList[i];
                bool state = node == _curSelectNode;
                for (int j = 0; j < node.LocalDepth; j++)
                    GUILayout.Label(" ", GUILayout.ExpandWidth(false));
                if (GUILayout.Toggle(state, "", GUILayout.Width(10)))
                {
                    if (_curSelectNode != node)
                        EditorGUIUtility.PingObject(node.gameObject);
                    _curSelectNode = node;
                }
                GUILayout.Label(node.Name + "(rotation != (0,0,0)", style, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }
            for (int i = 0; i < _colorAlphaList.Count; i++)
            {
                GUILayout.BeginHorizontal();

                AssetNode node = _colorAlphaList[i];
                bool state = node == _curSelectNode;
                for (int j = 0; j < node.LocalDepth; j++)
                    GUILayout.Label(" ", GUILayout.ExpandWidth(false));
                if (GUILayout.Toggle(state, "", GUILayout.Width(10)))
                {
                    if (_curSelectNode != node)
                        EditorGUIUtility.PingObject(node.gameObject);
                    _curSelectNode = node;
                }
                GUILayout.Label(node.Name + "(color.a == 0)", style, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
            GUILayout.Label(">>>>>NO.3:层级与Batch显示", style);
            GUILayout.Space(10);
            style.normal.textColor = Color.white;
            for (int i = 0; i < _assetNodeList.Count; i++)
            {
                GUILayout.BeginHorizontal();

                AssetNode node = _assetNodeList[i];
                bool state = node == _curSelectNode;
                style.normal.textColor = node.ColorValue;
                //if (node.RenderLayer > 10)
                //{
                //    style.normal.textColor = Color.red;
                //}
                for (int j = 0; j < node.LocalDepth; j++)
                    GUILayout.Label(" ", GUILayout.ExpandWidth(false));
                string matName;
                if (GUILayout.Toggle(state, "", GUILayout.Width(10)))
                {
                    if (_curSelectNode != node)
                        EditorGUIUtility.PingObject(node.gameObject);
                    _curSelectNode = node;
                }
                if (node.RenderType == RenderTypeEnum.Image)
                {
                    matName = node.MatName;
                    GUILayout.Label(node.Name, style, GUILayout.Width(100));
                    for (int j = 0; j < _mostDepth + 1 - node.LocalDepth; j++)
                        GUILayout.Label(" ", GUILayout.ExpandWidth(false));
                    GUILayout.Label("[Mat:" + matName + "]", style, GUILayout.Width(170));
                    GUILayout.Label("[Layer:" + node.RenderLayer + "]", style, GUILayout.Width(80));
                    GUILayout.Label("[Batch:" + node.DrawCallBatch + "]", style, GUILayout.Width(100));
                }
                else
                {
                    GUILayout.Label(node.Name, style, GUILayout.Width(100));
                    for (int j = 0; j < _mostDepth + 1 - node.LocalDepth; j++)
                        GUILayout.Label(" ", GUILayout.ExpandWidth(false));
                    GUILayout.Label("[Mat:" + "Text" + "]", style, GUILayout.Width(170));
                    GUILayout.Label("[Layer:" + node.RenderLayer + "]", style, GUILayout.Width(80));
                    GUILayout.Label("[Batch:" + node.DrawCallBatch + "]", style, GUILayout.Width(100));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
        GUILayout.EndVertical();

        GUI.contentColor = previousContentColor;
        GUI.skin.label.normal.textColor = previousLabelColor;
        GUI.skin.button.normal.textColor = previousButtonColor;
        GUI.skin.toggle.normal.textColor = previousToggleColor;
    }

    /// <summary>
    /// 清空上一次分析的节点、警示和估算批次，为重新刷新作准备。
    /// </summary>
    private static void OnInit()
    {
        _assetNodeList.Clear();
        _zValueList.Clear();
        _rotNodeList.Clear();
        _colorAlphaList.Clear();
        _canvas = null;
        _target = null;
        _curSelectNode = null;
        _drawCall = 0;
        _renderLayerDict.Clear();
        _batchDict.Clear();
        _atlasRefDict.Clear();
        _mostDepth = 0;
    }
    #endregion

    /// <summary>
    /// 用红线画出本工具估算的节点矩形；当前 OnGUI 中的调用已注释。
    /// </summary>
    private void DrawRect()
    {
        for (int i = 0; i < _assetNodeList.Count; i++)
        {
            AssetNode node = _assetNodeList[i];
            Rect rect = node.TransRect;
            Debug.DrawLine(new Vector3(rect.xMax, rect.yMax, 0), new Vector3(rect.xMin, rect.yMax, 0), Color.red);
            Debug.DrawLine(new Vector3(rect.xMax, rect.yMax, 0), new Vector3(rect.xMax, rect.yMin, 0), Color.red);
            Debug.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMin, rect.yMax, 0), Color.red);
            Debug.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMax, rect.yMin, 0), Color.red);
        }
    }

    #region 查询与判定
    /// <summary>
    /// 只检查 go 自身及其直接父节点是否带 Canvas，成功后记录分析根对象；不会继续向祖先查找。
    /// </summary>
    /// <param name="go">本次在编辑器中选中的对象。</param>
    public static bool FindRootCanvas(GameObject go)
    {
        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas)
        {
            _canvas = canvas;
            _target = go;
            return true;
        }
        else
        {
            Transform parent = go.transform.parent;
            if (parent == null)
            {
                EditorUtility.DisplayDialog("提示", "请选中Prefab的根目录！", "确认");
                return false;
            }
            canvas = parent.GetComponent<Canvas>();
            if (canvas)
            {
                _canvas = canvas;
                _target = parent.gameObject;
                return true;
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请选中Prefab的根目录！", "确认");
                return false;
            }
        }
    }

    /// <summary>
    /// 记录 Z、旋转和透明度提示，按开关筛掉禁用图形，并统计留下的节点及材质引用。
    /// </summary>
    private static void FindDirtyNode()
    {
        //会被渲染的队列;
        List<AssetNode> renderList = new List<AssetNode>();
        foreach (AssetNode node in _assetNodeList)
        {
            bool isDirty = false;
            if (node.Position.z != 0)
            {
                _zValueList.Add(node);
            }
            if (node.Rotation != Quaternion.identity)
            {
                _rotNodeList.Add(node);
            }
            if (node.ColorAlpha == 0)
            {
                _colorAlphaList.Add(node);
            }
            if (!isDirty && node.graphic)
            {
                isDirty = node.graphic.enabled == false;
            }
            if (!_incluedDisable && isDirty)
            {
                continue;
            }
            if (node.IsRender)
            {
                _mostDepth = node.LocalDepth > _mostDepth ? node.LocalDepth : _mostDepth;
                renderList.Add(node);
                string matName = node.MatName;
                if (!_atlasRefDict.ContainsKey(matName))
                {
                    _atlasRefDict[matName] = 0;
                }
                _atlasRefDict[matName]++;
            }
        }
        _assetNodeList = renderList;
    }
    #endregion

    #region 核心逻辑
    // 从 GameObject 菜单打开分析窗口并清空上次结果，不修改被分析的场景对象。
    [MenuItem("GameObject/UGUI DrawCall小助手", false, 0)]
    public static void StartAssistant()
    {
        OnInit();
        _incluedDisable = true;
        var window = GetWindow<DrawCallAssistantUtil>(false, "DrawCall小助手");
        window.Show();
    }

    /// <summary>
    /// 按层级顺序收集 go 的子节点；关闭包含禁用项时，跳过未激活的节点。
    /// </summary>
    /// <param name="go">当前需要继续收集子节点的对象。</param>
    private static void RecursiveCreateNode(GameObject go)
    {
        if (!_incluedDisable && go.activeSelf == false)
            return;
        foreach (Transform child in go.transform)
        {
            if (!_incluedDisable && child.gameObject.activeSelf == false)
                continue;
            AssetNode childNode = new AssetNode(child.gameObject);
            _assetNodeList.Add(childNode);
            RecursiveCreateNode(child.gameObject);
        }
    }

    /// <summary>
    /// 按节点顺序比较矩形：与前面节点重叠时放到更高估算层，随后按层收集节点。
    /// </summary>
    private static void OverlapDetection()
    {
        for (int i = 0; i < _assetNodeList.Count; i++)
        {
            AssetNode node = _assetNodeList[i];
            if (i == 0)
            {
                node.RenderLayer = 1;
                continue;
            }
            int maxRenderLayer = 1;
            for (int j = i - 1; j >= 0; j--)
            {
                if (node.TransRect.Overlaps(_assetNodeList[j].TransRect))
                {
                    int targetLayer = _assetNodeList[j].RenderLayer + 1;
                    maxRenderLayer = targetLayer > maxRenderLayer ? targetLayer : maxRenderLayer;
                }
            }
            node.RenderLayer = maxRenderLayer;
        }
        //分层;
        foreach (AssetNode node in _assetNodeList)
        {
            int layer = node.RenderLayer;
            if (!_renderLayerDict.ContainsKey(node.RenderLayer))
            {
                _renderLayerDict[layer] = new List<AssetNode>();
            }
            _renderLayerDict[layer].Add(node);
        }
    }

    /// <summary>
    /// 在当前层中把组件类型和材质相同的节点放入同一批，再递归处理剩余节点。
    /// </summary>
    /// <param name="preBatch">前面各层已经分配的最大批次号，当前批次从它加一开始。</param>
    /// <param name="combineNodeList">当前层尚未分配批次的节点，保存原节点引用。</param>
    private static int RecursiveCombineBatch(int preBatch, List<AssetNode> combineNodeList)
    {
        if (combineNodeList.Count == 0)
            return preBatch;
        int batch = preBatch + 1;
        AssetNode baseNode = combineNodeList[0];
        int count = 1;
        List<AssetNode> remainNode = new List<AssetNode>();
        for (int i = 0; i < combineNodeList.Count; i++)
        {
            AssetNode node = combineNodeList[i];
            if (i == 0)
            {
                baseNode = node;
                node.DrawCallBatch = batch;
                continue;
            }
            bool isCanBatch = baseNode.RenderType == node.RenderType;
            if (isCanBatch)
            {
                isCanBatch = baseNode.material == node.material;
                if (isCanBatch)
                {
                    node.DrawCallBatch = batch;
                    count++;
                    continue;
                }
            }
            //无法合批;
            remainNode.Add(node);
        }
        if (count < 2)
        {
            baseNode.ColorValue = Color.red;
        }
        return RecursiveCombineBatch(batch, remainNode);
    }
    #endregion
}

/// <summary>
/// 一次分析采集的 UI 节点信息，保存原 GameObject 引用及用于估算的矩形、材质和层号。
/// </summary>
public class AssetNode
{
    // 从 UI 对象 go 采集局部变换、矩形和图形组件；本对象只是分析数据，不会改动原控件。
    public AssetNode(GameObject go)
    {
        _go = go;
        _name = go.name;
        _localDepth = 0;
        _localpath = GetAssetLoaclPath(go);
        _renderType = RenderTypeEnum.Non;
        _material = null;
        _matName = "NULL";
        _graphic = null;
        _isRender = false;
        _renderLayer = -1;
        _drawCallBatch = -1;
        _position = go.transform.localPosition;
        _scale = go.transform.localScale;
        _rotation = go.transform.localRotation;
        _colorAlpha = -1f;
        _color = Color.white;
        RectTransform trans = go.transform as RectTransform;
        _rect = Rect.zero;
        if (trans)
        {
            float w = Mathf.Abs(trans.sizeDelta.x);
            float h = Mathf.Abs(trans.sizeDelta.y);
            float x = trans.localPosition.x;
            float y = trans.localPosition.y;
            _rect = new Rect(Rect.zero);
            _rect.xMax = x + w / 2;
            _rect.xMin = x - w / 2;
            _rect.yMax = y + h / 2;
            _rect.yMin = y - h / 2;

            Transform t = go.transform.parent;
            if (t)
            {
                Vector3 _leftTop = t.TransformPoint(new Vector3(_rect.xMin, _rect.yMax, 0));
                Vector3 _rightTop = t.TransformPoint(new Vector3(_rect.xMax, _rect.yMax, 0));
                Vector3 _leftDown = t.TransformPoint(new Vector3(_rect.xMin, _rect.yMin, 0));
                Vector3 _rightDown = t.TransformPoint(new Vector3(_rect.xMax, _rect.yMin, 0));
                _rect.xMin = _leftTop.x;
                _rect.xMax = _rightTop.x;
                _rect.yMin = _leftDown.y;
                _rect.yMax = _rightTop.y;
            }
        }
        Image image = go.GetComponent<Image>();
        if (image)
        {
            _renderType = RenderTypeEnum.Image;
            _material = image.material;
            _matName = (_material == null) ? "NULL" : _material.name;
            _colorAlpha = image.color.a;
            _graphic = image;
        }
        Text text = go.GetComponent<Text>();
        if (text)
        {
            _renderType = RenderTypeEnum.Text;
            _material = text.material;
            _matName = "Text";
            _colorAlpha = text.color.a;
            _graphic = text;
        }
        RawImage rawImage = go.GetComponent<RawImage>();
        if (rawImage)
        {
            _renderType = RenderTypeEnum.Image;
            _material = rawImage.material;
            _matName = (_material == null) ? "NULL" : _material.name;
            _colorAlpha = rawImage.color.a;
            _graphic = rawImage;
        }
        // 已识别出可渲染图形类型，且矩形宽高均不为零。
        bool hasRenderTypeAndSize = _renderType != RenderTypeEnum.Non && _rect.width != 0 && _rect.height != 0;
        // 缩放不为零才参与渲染。
        bool hasNonZeroScale = _scale != Vector3.zero;
        if (hasRenderTypeAndSize && hasNonZeroScale)//if(w==0||h==0) 不参与渲染,scala == 0 不参与渲染;
        {
            _isRender = true;
        }
    }

    private string _name;                 //节点名字;

    private string _localpath;            //hierarchy路径;

    private int _localDepth;              //hierarchy层级深度;

    // 分析结果对应的原场景对象，用于点击列表时定位。
    private GameObject _go;

    private RenderTypeEnum _renderType;   //参与渲染的组件类型;

    private Material _material;           //材质;

    private string _matName;              //材质名;

    // 识别出的 Image、RawImage 或旧 Text 组件，供禁用状态筛选。
    private Graphic _graphic;

    private bool _isRender;               //是否参与渲染;

    private int _renderLayer;             //渲染时所在的层数;

    private int _drawCallBatch;           //渲染时所在的批次;

    private Vector3 _position;             //postion;

    private Vector3 _scale;                //scale;

    private Quaternion _rotation;          //rotation

    private Rect _rect;                //大小;

    private float _colorAlpha;               //颜色的alpha值;

    private Color _color;                  //显示颜色;

    // 节点名称，用于结果列表显示。
    public string Name { get { return _name; } }

    // 节点完整层级路径，保留原属性名的拼写。
    public string LoaclPath { get { return _localpath; } }

    // 从根对象向下计算的层级深度，用于列表缩进。
    public int LocalDepth { get { return _localDepth; } }

    // 分析时保存的原 GameObject 引用。
    public GameObject gameObject { get { return _go; } }

    // 本工具识别到的图形类别，参与合批比较。
    public RenderTypeEnum RenderType { get { return _renderType; } }

    // 图形实际引用的材质对象，合批比较使用引用是否相同。
    public Material material { get { return _material; } }

    // 列表显示和引用统计使用的名称，旧 Text 固定显示 Text。
    public string MatName { get { return _matName; } }

    // 原图形组件，用于读取 enabled 状态。
    public Graphic graphic { get { return _graphic; } }

    // 当前节点是否满足本工具的类型、尺寸和缩放条件，不等同于 Unity 最终是否绘制。
    public bool IsRender { get { return _isRender; } }

    // 矩形重叠算法分配的估算层号。
    public int RenderLayer { get { return _renderLayer; } set { _renderLayer = value; } }

    // 同层合批算法分配的批次号。
    public int DrawCallBatch { get { return _drawCallBatch; } set { _drawCallBatch = value; } }

    // 采集时的局部位置；Z 分量用于提示非平面 UI。
    public Vector3 Position { get { return _position; } }

    // 采集时的局部缩放。
    public Vector3 Scale { get { return _scale; } }

    // 采集时的局部旋转，用于提示估算误差。
    public Quaternion Rotation { get { return _rotation; } }

    // 由局部尺寸和父节点变换估算出的二维矩形，用于重叠检测。
    public Rect TransRect { get { return _rect; } }

    // 图形颜色的透明度，通常为 0 到 1；未识别图形时为 -1。
    public float ColorAlpha { get { return _colorAlpha; } }

    // 结果列表的文字颜色；独占一个批次的节点会被标红。
    public Color ColorValue { get { return _color; } set { _color = value; } }

    /// <summary>
    /// 从 go 向父节点递归拼接 Hierarchy 路径，同时累计当前节点深度。
    /// </summary>
    /// <param name="go">当前需要拼接名字并继续向父节点查找的对象。</param>
    /// <returns>由父子对象名组成、以斜杠分隔的路径。</returns>
    private string GetAssetLoaclPath(GameObject go)
    {
        string localName = string.Empty;
        if (go.transform.parent == null)
        {
            localName = go.name;
        }
        else
        {
            _localDepth++;
            localName = GetAssetLoaclPath(go.transform.parent.gameObject) + "/" + go.name;
        }
        return localName;
    }
}

/// <summary>
/// 本工具能识别的 UI 图形类别；RawImage 与 Image 归为同一类。
/// </summary>
public enum RenderTypeEnum : int
{
    // 没有识别到本工具支持的 UI 图形。
    Non = 0,
    // Image 或 RawImage 图形。
    Image = 1,
    // Unity 旧版 UGUI Text 文本。
    Text = 2,
}
