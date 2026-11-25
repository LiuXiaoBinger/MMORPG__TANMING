using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>从主界面小地图打开的区域图和世界图，显示角色、真实寻路目标和当前区域的活动实体。</summary>
public sealed class AreaMapWindow : WindowBase
{
    /// <summary>一座已制作地图的公开城市，地标和世界入口都由预制体绑定。</summary>
    [Serializable]
    private sealed class Region
    {
        /// <summary>对应 Unity 场景原名，用于识别玩家当前所在的区域。</summary>
        public string scene;
        /// <summary>显示在地图标题与坐标栏中的地名。</summary>
        public string title;
        /// <summary>按本场景辅助点范围正交绘制的北向俯视图。</summary>
        public Sprite terrain;
        /// <summary>世界总图上该城市的可点击地名签。</summary>
        public Button banner;
        /// <summary>世界图地图签上方的红水晶，只点亮玩家实际所在的城市。</summary>
        public GameObject here;
        /// <summary>世界地点检索中对应本城的入口。</summary>
        public Button worldRow;
        /// <summary>记录在本地底图上的固定地点按钮组。</summary>
        public RectTransform landmarks;
        /// <summary>搜索抽屉内该区域的地点列表。</summary>
        public GameObject directory;
        /// <summary>底图上的地点标记，顺序与地点列表一致。</summary>
        public Button[] places;
        /// <summary>点击后定位到对应标记的搜索结果行。</summary>
        public Button[] rows;
    }

    /// <summary>已经发布地区的底图、名称与 UI 控件。</summary>
    [SerializeField] private Region[] _regions;
    /// <summary>区域层、世界层和地标搜索抽屉。</summary>
    [SerializeField] private GameObject _regional, _world, _search;
    /// <summary>世界图地点抽屉内的城市列表。</summary>
    [SerializeField] private GameObject _worldDirectory;
    /// <summary>关闭、切换世界/区域、搜索、终止导航四个入口。</summary>
    [SerializeField] private Button _close, _switch, _find, _stop;
    /// <summary>显示地图名称、切换目标、当前位置和寻路目的地。</summary>
    [SerializeField] private TMP_Text _title, _switchText, _currentText, _targetText;
    /// <summary>拖动边界、地图画布和滚动视图。</summary>
    [SerializeField] private RectTransform _viewport, _sheet;
    /// <summary>拖动移动整张地图，显示另一城市时重置滚动。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>接收区域图底图的点击以及滚轮。</summary>
    [SerializeField] private MapSurfaceInput _input;
    /// <summary>显示实景俯拍地图的图形。</summary>
    [SerializeField] private Image _terrain;
    /// <summary>世界与区域切换按钮，由当前页面选择 RO 地球或区域图标。</summary>
    [SerializeField] private Image _switchSymbol;
    /// <summary>进入世界图的地球图标。</summary>
    [SerializeField] private Sprite _worldSymbol;
    /// <summary>返回地区浏览的地图图标。</summary>
    [SerializeField] private Sprite _areaSymbol;
    /// <summary>分别表示自己的朝向、当前导航目的地和活动实体模板。</summary>
    [SerializeField] private Image _player, _goal, _dot;
    /// <summary>点击当前区域地图时将场景米制坐标交由主控导航。</summary>
    public event Action<Vector3> TravelRequested;
    /// <summary>停止当前寻路由主控通知角色组件。</summary>
    public event Action StopRequested;
    /// <summary>当前打开窗口时绑定的场景辅助点。</summary>
    private MinMapHelp _localMap;
    /// <summary>本次显示的区域下标。</summary>
    private int _index;
    /// <summary>相对于完整底图的显示比例。</summary>
    private float _zoom = 1f;
    /// <summary>上次视口尺寸，窗口在拖动中改变分辨率时重新适配。</summary>
    private Vector2 _lastViewport;
    /// <summary>具名地标以外的活动实体。</summary>
    private MapEntityMarkers _markers;
    /// <summary>下一次刷新附近实体与坐标文字的未缩放时间。</summary>
    private float _nextRefresh;
    /// <summary>正在追踪的本地角色，由主控按登录生命周期更新。</summary>
    private PlayerEntity _role;
    /// <summary>最近一次点到了不可走地形时给出的文字，下一次选点同样可以继续操作。</summary>
    private string _routeMessage;
    /// <summary>两张区域图都在相同的宽幅画面内俯拍，纵向对应场景基准。</summary>
    private const float MapAspect = 16f / 9f;

    /// <summary>注册正式预制体内的地图交互与各区域地点。</summary>
    public override void InitWindow()
    {
        if (_markers != null) return;
        _markers = new MapEntityMarkers(_dot);
        _close.onClick.AddListener(CloseWindow);
        _switch.onClick.AddListener(SwitchMap);
        _find.onClick.AddListener(ToggleSearch);
        _stop.onClick.AddListener(StopRoute);
        _input.PointClicked += PickPoint;
        _input.ZoomRequested += Zoom;
        for (int i = 0; i < _regions.Length; i++)
        {
            // 捕获各自的城市索引，使每个城市的签都能进入正确的预览。
            int regionIndex = i;
            // 当前地区的搜索行和图上的点是一一对应的。
            Region region = _regions[i];
            region.banner.onClick.AddListener(() => ShowRegion(regionIndex));
            region.worldRow.onClick.AddListener(() => ShowRegion(regionIndex));
            for (int j = 0; j < region.places.Length; j++)
            {
                // 同一个地标同时被底图按钮与搜索列表引用。
                RectTransform place = (RectTransform)region.places[j].transform;
                region.places[j].onClick.AddListener(() => ChoosePlace(place));
                region.rows[j].onClick.AddListener(() => ChoosePlace(place));
            }
        }
    }

    /// <summary>根据场景取得与区域图共用的道路底图，供 HUD 根据相同比例尺显示角色周边。</summary>
    /// <param name="sceneName">游戏已加载场景的资源名称。</param>
    public Sprite TerrainForScene(string sceneName)
    {
        // 按预制体内的地理目录查图，未配置的场景继续使用自身小地图资源。
        for (int i = 0; i < _regions.Length; i++)
        {
            if (_regions[i].scene == sceneName) return _regions[i].terrain;
        }
        return null;
    }

    /// <summary>主控把本次进入场景的角色交给区域地图，角色释放时传空。</summary>
    /// <param name="role">本地玩家实体。</param>
    public void Track(PlayerEntity role)
    {
        _role = role;
    }

    /// <summary>每次从 HUD 进入都打开玩家当前的区域。</summary>
    private void OnEnable()
    {
        if (_regions == null || _regions.Length == 0) return;
        _localMap = MinMapHelp.instance;
        if (_localMap == null) return;
        for (int i = 0; i < _regions.Length; i++)
        {
            if (_regions[i].scene == _localMap.gameObject.scene.name)
            {
                _routeMessage = null;
                ShowRegion(i);
                return;
            }
        }
        CloseWindow();
    }

    /// <summary>在窗口和场景都有效时，从真实角色位置和导航组件绘制图标。</summary>
    private void LateUpdate()
    {
        if (_localMap == null || _localMap != MinMapHelp.instance)
        {
            CloseWindow();
            return;
        }
        if (!_regional.activeSelf) return;
        if (_lastViewport != _viewport.rect.size) ResizeMap(false);
        // 跨图查看时只显示当地的固定地标，玩家和目的地属于实际所在场景。
        bool here = IsLocalRegion();
        bool hasRole = _role != null;
        _player.gameObject.SetActive(here && hasRole);
        bool navigating = hasRole && _role.NavComp != null && _role.NavComp.IsNavigation;
        _goal.gameObject.SetActive(here && navigating);
        _stop.gameObject.SetActive(navigating);
        if (here && hasRole)
        {
            MinMapHelp.Place(_player.rectTransform, _localMap.WorldToMap(_role.Position), MapAspect);
            _player.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f - _role.Transform.Yaw);
            if (navigating) MinMapHelp.Place(_goal.rectTransform, _localMap.WorldToMap(_role.NavComp.Destination), MapAspect);
        }
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.15f;
        // 可见实体由场景世界管理，浏览外地时传空以清除上一张图的点。
        SceneWorldSystem world = null;
        if (here && Global.Instance != null) world = Global.Instance.SceneWorld;
        _markers?.Refresh(_localMap, world, MapAspect);
        if (hasRole)
        {
            _currentText.text = "当前坐标  " + Location(_role.Position);
        }
        else _currentText.text = "当前区域  " + _localMap.AreaName;
        if (navigating) _targetText.text = "前往坐标  " + Location(_role.NavComp.Destination);
        else if (!string.IsNullOrEmpty(_routeMessage)) _targetText.text = _routeMessage;
        else if (!here) _targetText.text = "正在查看  " + _regions[_index].title;
        else _targetText.text = "点击地图前往";
    }

    /// <summary>按当前所在区域名称和真实 X、Z 米数显示当前位置或目标。</summary>
    /// <param name="position">场景中的实际位置。</param>
    private string Location(Vector3 position)
    {
        return _localMap.AreaName + " (" + Mathf.RoundToInt(position.x) + ", " + Mathf.RoundToInt(position.z) + ")";
    }

    /// <summary>显示指定地区并重新定位底图。</summary>
    /// <param name="index">世界图中被点击的城市下标。</param>
    private void ShowRegion(int index)
    {
        _index = index;
        _zoom = 1.45f;
        _world.SetActive(false);
        _regional.SetActive(true);
        _search.SetActive(false);
        _find.gameObject.SetActive(true);
        _title.text = _regions[index].title;
        _terrain.sprite = _regions[index].terrain;
        _worldDirectory.SetActive(false);
        _switchText.text = "世界地图";
        _switchSymbol.sprite = _worldSymbol;
        for (int i = 0; i < _regions.Length; i++)
        {
            _regions[i].landmarks.gameObject.SetActive(i == index);
            _regions[i].directory.SetActive(i == index);
        }
        ResizeMap(true);
        if (IsLocalRegion() && _role != null) CenterOn(_localMap.WorldToMap(_role.Position));
        _nextRefresh = 0f;
        LateUpdate();
    }

    /// <summary>切换到世界图，或从世界图回到刚才查看的区域。</summary>
    private void SwitchMap()
    {
        if (_world.activeSelf)
        {
            ShowRegion(_index);
            return;
        }
        _regional.SetActive(false);
        _search.SetActive(false);
        _worldDirectory.SetActive(true);
        _regions[_index].directory.SetActive(false);
        _world.SetActive(true);
        _title.text = "世界地图";
        _switchText.text = "区域地图";
        _switchSymbol.sprite = _areaSymbol;
        for (int i = 0; i < _regions.Length; i++)
        {
            _regions[i].here.SetActive(_regions[i].scene == _localMap.gameObject.scene.name);
        }
    }

    /// <summary>展开或收起当前地区的地名检索。</summary>
    private void ToggleSearch()
    {
        _search.SetActive(!_search.activeSelf);
    }

    /// <summary>选择固定地点时居中显示，处于本地区时同时提交寻路。</summary>
    /// <param name="place">与地理位置绑定的地点签。</param>
    private void ChoosePlace(RectTransform place)
    {
        // 地标锚点换回以地图高度为一的坐标。
        Vector2 point = place.anchorMin - new Vector2(0.5f, 0.5f);
        point.x *= MapAspect;
        _search.SetActive(false);
        CenterOn(point);
        PickPoint(point);
    }

    /// <summary>当前是否在查看玩家本人所在地区。</summary>
    private bool IsLocalRegion()
    {
        return _localMap != null && _regions[_index].scene == _localMap.gameObject.scene.name;
    }

    /// <summary>将区域图点击位置交给主控，跨区浏览只改变视图。</summary>
    /// <param name="point">以区域底图中心为零的地图坐标。</param>
    private void PickPoint(Vector2 point)
    {
        if (!IsLocalRegion()) return;
        _routeMessage = null;
        TravelRequested?.Invoke(_localMap.MapToWorld(point));
        _nextRefresh = 0f;
    }

    /// <summary>把无法抵达的反馈放在目标坐标一栏。</summary>
    public void ShowUnreachable()
    {
        _routeMessage = "此处无法到达";
        _nextRefresh = 0f;
    }

    /// <summary>沿现有角色移动逻辑中断正在执行的路线。</summary>
    private void StopRoute()
    {
        StopRequested?.Invoke();
        _routeMessage = null;
        _nextRefresh = 0f;
    }

    /// <summary>滚轮按比例调整地图，在缩放前后保留当前浏览中心。</summary>
    /// <param name="delta">滚轮纵向增量，向上为正。</param>
    private void Zoom(float delta)
    {
        _zoom = Mathf.Clamp(_zoom * Mathf.Pow(1.2f, delta), 1f, 3f);
        ResizeMap(false);
    }

    /// <summary>按视口确保底图覆盖全屏，同时保持与场景投影相同的纵横比。</summary>
    /// <param name="reset">切换地区时回到图心，改变尺寸时保留原中心。</param>
    private void ResizeMap(bool reset)
    {
        // 原画面中心的地图坐标，分辨率或缩放级别改变后仍用它定位。
        Vector2 center = Vector2.zero;
        if (!reset && _sheet.rect.height > 0f) center = -_sheet.anchoredPosition / _sheet.rect.height;
        _lastViewport = _viewport.rect.size;
        // 横屏和宽屏都填满窗口，并保留拖动后仍能预览的地图范围。
        float height = Mathf.Max(_lastViewport.y, _lastViewport.x / MapAspect) * _zoom;
        _sheet.sizeDelta = new Vector2(height * MapAspect, height);
        CenterOn(center);
    }

    /// <summary>将指定地图坐标移至视口中心，并约束在底图四周范围内。</summary>
    /// <param name="point">需要居中显示的地图坐标。</param>
    private void CenterOn(Vector2 point)
    {
        _scroll.StopMovement();
        // 内容大于视口的半差就是可以平移的最大距离。
        Vector2 limit = Vector2.Max((_sheet.rect.size - _viewport.rect.size) * 0.5f, Vector2.zero);
        // 玩家或检索位置处于地图边缘时，用最接近的可见中心。
        Vector2 offset = -point * _sheet.rect.height;
        _sheet.anchoredPosition = new Vector2(Mathf.Clamp(offset.x, -limit.x, limit.x),
                                             Mathf.Clamp(offset.y, -limit.y, limit.y));
    }
}
