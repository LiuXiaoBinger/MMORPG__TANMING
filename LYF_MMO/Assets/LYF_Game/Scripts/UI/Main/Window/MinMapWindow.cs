using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/**
* Title:
* Descrpiton:
*/

/// <summary>跟随当前场景小地图辅助点展示角色位置；未配置辅助点的场景隐藏小地图。</summary>
public class MinMapWindow : WindowBase
{

   #region 字段
   /// <summary>小地图左上侧的分线下拉框，由区服返回可用线路后显示。</summary>
   [SerializeField] private SceneLineDropdown _lineSelector;
   /// <summary>线路入口的折叠箭头，列表打开时朝上，收起时朝下。</summary>
   [SerializeField] private RectTransform _lineArrow;
   /// <summary>下拉索引对应的真实线路编号，有空号时仍能请求正确目标。</summary>
   private readonly List<int> _lineIds = new List<int>();
   /// <summary>上次服务端确认的线路索引，等待切线回包期间入口仍显示实际所在的线。</summary>
   private int _currentLineIndex;
   /// <summary>预制体配置的主城底图，从天空中之城返回时恢复。</summary>
   private Sprite _defaultMapSprite;
   /// <summary>玩家选择新线路时通知主界面控制器发送请求。</summary>
   public event System.Action<int> LineSelected;
   /// <summary>把线路入口的展开请求交给主控，以当前玩家的真实地图查询目录。</summary>
   public event System.Action LinesRequested
   {
      add { if (_lineSelector != null) _lineSelector.LinesRequested += value; }
      remove { if (_lineSelector != null) _lineSelector.LinesRequested -= value; }
   }
   /// <summary>小地图底图，通过尺寸和偏移显示角色附近区域。</summary>
   [SerializeField,Header("小地图背景图片")]private Image _imgMinMap;

   /// <summary>本地角色朝向箭头，随角色绕 Y 轴的方向旋转。</summary>
   [SerializeField,Header("小地图箭头图片")]private Image _imgArrow;

   /// <summary>点地图本身通知主控打开当前区域。</summary>
   public event System.Action RegionRequested;
   /// <summary>地图中央的透明热区，与原有缩放和切线按钮独立。</summary>
   [SerializeField] private Button _openRegion;
   /// <summary>位于地图真实位置的城际传送标记。</summary>
   [SerializeField] private Button _portalMark;
   /// <summary>RO 寻路方向标的预制样板。</summary>
   [SerializeField] private Image _routeTemplate;
   /// <summary>当前路径终点的 RO 定位针。</summary>
   [SerializeField] private Image _routeGoal;
   /// <summary>羊皮纸地图名下单独显示坐标。</summary>
   [SerializeField] private TMP_Text _positionText;
   /// <summary>本场景显示在底图上的门；副本中为空。</summary>
   private PublicCityPortal _portal;
   /// <summary>同一张公共地图上可供标记指引的门，随场景绑定一次。</summary>
   private IReadOnlyList<PublicCityPortal> _mapDoors;
   /// <summary>城门在当前底图上的归一化坐标，缩放时按画布像素重算。</summary>
   private Vector2 _portalPoint;
   /// <summary>当前窗口拥有的可复用路径标记。</summary>
   private MinimapRouteTrail _route;
   /// <summary>玩家点选城门，希望当前角色步行到此处。</summary>
   public event System.Action<Vector3> PortalSelected;
   /// <summary>当前小地图底图宽高比，用来让矩形区域图在 HUD 中保持等比例。</summary>
   private float _mapAspect = 1f;
   /// <summary>常驻地图上的场景名和主角实时坐标。</summary>
   [SerializeField] private TMP_Text _mapCaption;
   /// <summary>背景下面用于复制附近 NPC、怪物和玩家图标的样板。</summary>
   [SerializeField] private Image _entityTemplate;
   /// <summary>该 HUD 独有的实体标记列表。</summary>
   private MapEntityMarkers _markers;
   /// <summary>下次更新附近实体的未缩放时间，单位秒。</summary>
   private float _refreshAt;
   /// <summary>羊皮标题下的世界 X、Z 坐标格式，按整米显示。</summary>
   private const string CoordinateFormat = "{0} , {1}";
   /// <summary>当前本地玩家实体；模型异步加载完成后从实体中读取真实 Transform。</summary>
   private PlayerEntity _mainRole;

   /// <summary>当前场景的坐标换算辅助点，每次场景加载完成后重新绑定。</summary>
   private MinMapHelp _mapHelp;

   /// <summary>小地图图片当前边长，单位为 UI 像素；缩放按钮限制在 512 至 2048。</summary>
   private float _mapsize;
   #endregion

   #region 生命周期
   /// <summary>初始化图片边长并订阅场景切换，保持常驻窗口与场景辅助点的生命周期一致。</summary>
   private void Start()
   {
      if (_lineSelector != null) _lineSelector.onValueChanged.AddListener(SelectLine);
      _defaultMapSprite = _imgMinMap.sprite;
      _mapsize = _imgMinMap.rectTransform.rect.height;
      _openRegion.onClick.AddListener(OpenRegion);
      _markers = new MapEntityMarkers(_entityTemplate);
      _route = new MinimapRouteTrail(_routeTemplate);
      _portalMark.onClick.AddListener(SelectPortal);
      SceneManager.sceneLoaded += OnSceneLoaded;
      BindSceneMap();

   }

   /// <summary>用当前有效辅助点换算角色位置，场景卸载到重新绑定之间不访问旧节点。</summary>
   private void Update()
   {
      if (_mapHelp == null)
      {
         return;
      }
      if (Time.unscaledTime >= _refreshAt)
      {
         _refreshAt = Time.unscaledTime + 0.15f;
         if (_lineArrow != null && _lineSelector != null)
         {
            // RO 箭头原图朝下，菜单展开时在界面平面旋转半圈。
            float arrowAngle = 0f;
            if (_lineSelector.IsExpanded) arrowAngle = 180f;
            _lineArrow.localRotation = Quaternion.Euler(0f, 0f, arrowAngle);
         }
         // 模型进入或离开视野时，从现有场景世界刷新标记。
         SceneWorldSystem world = null;
         if (Global.Instance != null) world = Global.Instance.SceneWorld;
         _markers.Refresh(_mapHelp, world, _mapAspect);
         _route.Refresh(_mainRole, _mapHelp, _mapsize, ((RectTransform)_imgMinMap.transform.parent).rect.height * 0.5f);
         // 终点单独绘制，停止或走完后与路线一并消失。
         bool navigating = _mainRole != null && _mainRole.NavComp != null && _mainRole.NavComp.IsNavigation;
         _routeGoal.gameObject.SetActive(navigating);
         if (navigating) MinMapHelp.Place(_routeGoal.rectTransform, _mapHelp.WorldToMap(_mainRole.NavComp.Destination), _mapAspect);
      }
      if (_mainRole != null && _mainRole.Model != null &&
          _mainRole.Model.GameObject != null)
      {
         // 本地主角当前模型节点；未加载完成时为空并等待下一帧重新读取。
         Transform roleTransform = _mainRole.Model.GameObject.Transform;
         if (roleTransform == null)
         {
            return;
         }
         // 角色保持在圆心，底图与活动图标一起向相反方向平移。
         Vector3 position = roleTransform.position;
         // 角色距制图中心的像素偏移，每帧只进行一次投影。
         Vector2 playerPoint = _mapHelp.WorldToMap(position) * _mapsize;
         _imgMinMap.rectTransform.anchoredPosition = -playerPoint;
         if (_portal != null)
         {
            // 多入口时，复用原来的可点击标记指向最近的门，点选和实际踩门使用同一坐标。
            if (_mapDoors.Count > 1) SelectNearestPortal(position);
            // 门在视野外时停在对应方向的圆边，点击仍会前往真实门心。
            RectTransform marker = (RectTransform)_portalMark.transform;
            float radius = ((RectTransform)_imgMinMap.transform.parent).rect.height * 0.5f - marker.rect.width * 0.5f;
            marker.anchoredPosition = playerPoint + Vector2.ClampMagnitude(_portalPoint * _mapsize - playerPoint, radius);
         }
         _positionText.SetText(CoordinateFormat, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
         // RO 图标默认朝左，地图框的实际北向决定屏幕旋转角，箭头尖端固定在角色位置。
         Vector3 facing = _mapHelp.transform.parent.InverseTransformDirection(roleTransform.forward);
         _imgArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.z, facing.x) * Mathf.Rad2Deg - 180f);
      }
   }

   /// <summary>窗口销毁时释放场景订阅；仅隐藏时保留订阅，以便下一场景重新启用。</summary>
   private void OnDestroy()
   {
      if (_lineSelector != null) _lineSelector.onValueChanged.RemoveListener(SelectLine);
      if (_openRegion != null) _openRegion.onClick.RemoveListener(OpenRegion);
      if (_portalMark != null) _portalMark.onClick.RemoveListener(SelectPortal);
      SceneManager.sceneLoaded -= OnSceneLoaded;
   }
   #endregion

   #region 交互响应
   /// <summary>仅点圆形地图时打开区域图，原有切线控件继续独立响应。</summary>
   /// <summary>在建筑密集的街区显示距离角色最近的传送入口。</summary>
   /// <param name="position">本地主角当前世界坐标，单位米。</param>
   private void SelectNearestPortal(Vector3 position)
   {
      // 用平方距离找最近的门，避免逐门开平方。
      float best = float.PositiveInfinity;
      for (int index = 0; index < _mapDoors.Count; index++)
      {
         // 同图所有门的中心均来自服务端与客户端共用的文件。
         PublicCityPortal door = _mapDoors[index];
         Vector3 center = new Vector3(door.X, door.Y, door.Z);
         float distance = (center - position).sqrMagnitude;
         if (distance >= best) continue;
         best = distance;
         _portal = door;
      }
      _portalPoint = _mapHelp.WorldToMap(new Vector3(_portal.X, _portal.Y, _portal.Z));
   }

   /// <summary>把当前城门的世界位置交给主界面控制器寻路。</summary>
   private void SelectPortal()
   {
      if (_portal != null) PortalSelected?.Invoke(new Vector3(_portal.X, _portal.Y, _portal.Z));
   }

   private void OpenRegion()
   {
      RegionRequested?.Invoke();
   }

   /// <summary>根据服务器登记的线路和当前位置刷新小地图入口及选中行。</summary>
   /// <param name="result">本地图的实际线路列表；空值用于隐藏副本内的选择器。</param>
   public void ShowLines(SceneLineResult result)
   {
      if (_lineSelector == null) return;
      _lineIds.Clear();
      _lineSelector.ClearOptions();
      if (result == null || result.Lines.Count == 0)
      {
         _lineSelector.Hide();
         _lineSelector.gameObject.SetActive(false);
         return;
      }
      // 下拉显示顺序按区服目录；当前线的真实编号单独匹配。
      List<TMP_Dropdown.OptionData> labels = new List<TMP_Dropdown.OptionData>();
      int currentIndex = 0;
      for (int index = 0; index < result.Lines.Count; index++)
      {
         // 本条线路的可见编号与实时占用。
         PublicSceneLine line = result.Lines[index];
         _lineIds.Add(line.LineId);
         // 入口和高亮行都以服务端当前线为准，其他线路用低亮度圆点区分。
         Color32 statusColor = new Color32(125, 146, 165, 255);
         if (line.LineId == result.CurrentLine)
         {
            currentIndex = index;
            statusColor = new Color32(42, 167, 117, 255);
         }
         // 满员线路使用 RO 线路列表的红字；真实可切状态仍由服务器判定。
         string lineLabel = line.LineId + "线";
         if (line.Capacity > 0 && line.Population >= line.Capacity)
            lineLabel = "<color=#A83D40>" + lineLabel + "</color>";
         labels.Add(new TMP_Dropdown.OptionData(lineLabel, _lineSelector.itemImage.sprite, statusColor));
      }
      _lineSelector.AddOptions(labels);
      _currentLineIndex = currentIndex;
      _lineSelector.SetValueWithoutNotify(currentIndex);
      _lineSelector.RefreshShownValue();
      _lineSelector.gameObject.SetActive(true);
      _lineSelector.ShowRequestedLines();
   }

   /// <summary>把控件选中行转换为地图线路编号。</summary><param name="index">下拉框的零基行号。</param>
   private void SelectLine(int index)
   {
      if (index < 0 || index >= _lineIds.Count || index == _currentLineIndex) return;
      _lineSelector.SetValueWithoutNotify(_currentLineIndex);
      LineSelected?.Invoke(_lineIds[index]);
   }
   /// <summary>将地图边长放大为当前的 1.1 倍，并限制在 512 到 2048。</summary>
   public void OnPluseBtnClicked()
   {
      Debug.Log("OnPluseBtnClicked");
      _mapsize = Mathf.Clamp(_mapsize * 1.1f,512,2048);
      _imgMinMap.rectTransform.sizeDelta = new Vector2(_mapsize * _mapAspect, _mapsize);
   }

   /// <summary>将地图边长缩小为当前的 1/1.1，并限制在 512 到 2048。</summary>
   public void OnDecBtnClicked()
   {
      Debug.Log("OnDecBtnClicked");
      _mapsize = Mathf.Clamp(_mapsize / 1.1f,512,2048);
      _imgMinMap.rectTransform.sizeDelta = new Vector2(_mapsize * _mapAspect, _mapsize);
   }
   #endregion

   /// <summary>场景对象完成 Awake 后取新辅助点，不沿用主城已销毁的缓存。</summary>
   /// <param name="scene">本次完成加载的场景；辅助点由该场景自身发布。</param>
   /// <param name="mode">Unity 本次场景加载模式，不改变既有加载流程。</param>
   private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
   {
      BindSceneMap();
   }

   #region 核心逻辑
   /// <summary>绑定当前场景辅助点；没有小地图配置时隐藏整块 UI，回到有配置场景时恢复。</summary>
   private void BindSceneMap()
   {
      _mapHelp = MinMapHelp.instance;
      if (_mapHelp != null)
      {
         // 按场景切图，同时保留玩家设置的缩放倍率。
         _mapCaption.text = _mapHelp.AreaName;
         _imgMinMap.sprite = _mapHelp.MapSprite;
         if (_imgMinMap.sprite == null) _imgMinMap.sprite = _defaultMapSprite;
         _mapAspect = _imgMinMap.sprite.rect.width / _imgMinMap.sprite.rect.height;
         _mapDoors = PublicCityPortal.InScene(_mapHelp.gameObject.scene.name);
         _portal = MinMapHelp.CityPortal(_mapHelp.gameObject.scene.name);
         _portalMark.gameObject.SetActive(_portal != null);
         if (_portal != null)
         {
            // 与场景特效、服务端触发范围使用同一门心。
            Vector3 center = new Vector3(_portal.X, _portal.Y, _portal.Z);
            _portalPoint = _mapHelp.WorldToMap(center);
         }
         _imgMinMap.rectTransform.sizeDelta = new Vector2(_mapsize * _mapAspect, _mapsize);
      }
      gameObject.SetActive(_mapHelp != null);
   }

   /// <summary>由主界面控制器设置需要跟踪的本地玩家实体。</summary>
   /// <param name="role">场景世界持有的本地玩家；断线清理时可为空。</param>
   public void SetTrackedRole(PlayerEntity role)
   {
      _mainRole = role;
   }
   #endregion
}
