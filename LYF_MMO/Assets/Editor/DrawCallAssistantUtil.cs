using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

/// <summary>
/// DrawCall计算规则:1.默认Text Mesh与大小一致;2.默认z都为0;3.默认旋转为0;
/// TODO:1.多个canvas合批计算,canvas实现动静分离,会增加drawcall;2.不同层之间的DrawCall合并;
/// </summary>
public class DrawCallAssistantUtil : EditorWindow
{
    //全部节点;
    private static List<AssetNode> _assetNodeList = new List<AssetNode>();
    //z值不为0的的全部节点;
    private static List<AssetNode> _zValueList = new List<AssetNode>();
    //存在旋转的节点;
    private static List<AssetNode> _rotNodeList = new List<AssetNode>();
    //alpha 为0的节点;
    private static List<AssetNode> _colorAlphaList = new List<AssetNode>();
    //根canvas;
    private static Canvas _canvas = null;
    //分析对象;
    private static GameObject _target = null;
    //选中节点;
    private static AssetNode _curSelectNode = null;
    //Disable和DeActive是否包含;
    private static bool _incluedDisable = true;
    //合批次数;
    private static int _drawCall = 0;
    //节点分层;
    private static Dictionary<int, List<AssetNode>> _renderLayerDict = new Dictionary<int, List<AssetNode>>();
    //合批结果;
    private static Dictionary<int, List<AssetNode>> _batchDict = new Dictionary<int, List<AssetNode>>();
    //图集引用;
    private static Dictionary<string, int> _atlasRefDict = new Dictionary<string, int>();
    //滑动区域;
    private static Vector2 _scrollPosition;
    //最大深度;
    private static int _mostDepth = 0;

    [MenuItem("GameObject/UGUI DrawCall小助手", false, 0)]
    public static void StartAssistant()
    {
        OnInit();
        _incluedDisable = true;
        var window = GetWindow<DrawCallAssistantUtil>(false, "DrawCall小助手");
        window.Show();
    }

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
    /// 初始化;
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
    /// <summary>
    /// 查找canvas;
    /// </summary>
    /// <param name="go"></param>
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
    /// 创建节点;
    /// </summary>
    /// <param name="go"></param>
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
    /// 找出不符合规定的节点;
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
    /// <summary>
    /// 重叠检测,进行分层;
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
    /// 同层合批;
    /// </summary>
    /// <param name="preBatch">前一batch值;</param>
    /// <param name="combineNodeList"></param>
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
    /// <summary>
    /// 画出组件矩阵;
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
}

/// <summary>
/// 分析的资源节点;
/// </summary>
public class AssetNode
{
    private string _name;                 //节点名字;
    private string _localpath;            //hierarchy路径;
    private int _localDepth;              //hierarchy层级深度;
    private GameObject _go;
    private RenderTypeEnum _renderType;   //参与渲染的组件类型;
    private Material _material;           //材质;
    private string _matName;              //材质名;
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

    public string Name { get { return _name; } }
    public string LoaclPath { get { return _localpath; } }
    public int LocalDepth { get { return _localDepth; } }
    public GameObject gameObject { get { return _go; } }
    public RenderTypeEnum RenderType { get { return _renderType; } }
    public Material material { get { return _material; } }
    public string MatName { get { return _matName; } }
    public Graphic graphic { get { return _graphic; } }

    public bool IsRender { get { return _isRender; } }

    public int RenderLayer { get { return _renderLayer; } set { _renderLayer = value; } }
    public int DrawCallBatch { get { return _drawCallBatch; } set { _drawCallBatch = value; } }

    public Vector3 Position { get { return _position; } }
    public Vector3 Scale { get { return _scale; } }
    public Quaternion Rotation { get { return _rotation; } }

    public Rect TransRect { get { return _rect; } }
    public float ColorAlpha { get { return _colorAlpha; } }

    public Color ColorValue { get { return _color; } set { _color = value; } }

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
        if (_renderType != RenderTypeEnum.Non && _rect.width != 0 && _rect.height != 0 && _scale != Vector3.zero)//if(w==0||h==0) 不参与渲染,scala == 0 不参与渲染;
        {
            _isRender = true;
        }
    }
    /// <summary>
    /// 获取结构路径;
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
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
/// 参与渲染的组件类型;
/// </summary>
public enum RenderTypeEnum : int
{
    Non = 0,
    Image = 1,
    Text = 2,
}
