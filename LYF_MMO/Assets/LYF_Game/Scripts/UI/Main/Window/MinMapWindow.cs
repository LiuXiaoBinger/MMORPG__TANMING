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
   /// <summary>小地图下方的分线下拉框，由区服返回可用线路后显示。</summary>
   [SerializeField] private TMP_Dropdown _lineSelector;
   /// <summary>下拉索引对应的真实线路编号，有空号时仍能请求正确目标。</summary>
   private readonly List<int> _lineIds = new List<int>();
   /// <summary>上次服务端确认的线路索引，等待切线回包期间入口仍显示实际所在的线。</summary>
   private int _currentLineIndex;
   /// <summary>预制体配置的主城底图，从天空中之城返回时恢复。</summary>
   private Sprite _defaultMapSprite;
   /// <summary>玩家选择新线路时通知主界面控制器发送请求。</summary>
   public event System.Action<int> LineSelected;
   /// <summary>小地图底图，通过尺寸和偏移显示角色附近区域。</summary>
   [SerializeField,Header("小地图背景图片")]private Image _imgMinMap;

   /// <summary>本地角色朝向箭头，随角色绕 Y 轴的方向旋转。</summary>
   [SerializeField,Header("小地图箭头图片")]private Image _imgArrow;

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
      _mapsize = _defaultMapSprite.rect.width;
      Debug.Log("_mapsize:"+_mapsize);
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
      if (_mainRole != null && _mainRole.Model != null &&
          _mainRole.Model.GameObject != null)
      {
         // 本地主角当前模型节点；未加载完成时为空并等待下一帧重新读取。
         Transform roleTransform = _mainRole.Model.GameObject.Transform;
         if (roleTransform == null)
         {
            return;
         }
         // 把场景辅助点移到角色世界位置，再利用辅助点父节点的比例换算地图偏移。
         _mapHelp.transform.position = roleTransform.position;
         
         _imgMinMap.rectTransform.anchoredPosition = new Vector2((_mapHelp.transform.localPosition.x*-_mapsize),(_mapHelp.transform.localPosition.z*-_mapsize));
         // 角色绕 Y 轴的朝向转成 UI 绕 Z 轴旋转，并补偿底图的 90 度方向差。
         _imgArrow.transform.localEulerAngles = new Vector3(0,0,360-roleTransform.localEulerAngles.y+90);
      }
   }

   /// <summary>窗口销毁时释放场景订阅；仅隐藏时保留订阅，以便下一场景重新启用。</summary>
   private void OnDestroy()
   {
      if (_lineSelector != null) _lineSelector.onValueChanged.RemoveListener(SelectLine);
      SceneManager.sceneLoaded -= OnSceneLoaded;
   }
   #endregion

   #region 交互响应
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
         Color32 statusColor = new Color32(137, 166, 204, 255);
         if (line.LineId == result.CurrentLine)
         {
            currentIndex = index;
            statusColor = new Color32(38, 180, 116, 255);
         }
         // 满员线路使用 RO 线路列表的红字；真实可切状态仍由服务器判定。
         string lineLabel = line.LineId + " 线";
         if (line.Capacity > 0 && line.Population >= line.Capacity)
            lineLabel = "<color=#FF4F4F>" + lineLabel + "</color>";
         labels.Add(new TMP_Dropdown.OptionData(lineLabel, _lineSelector.itemImage.sprite, statusColor));
      }
      _lineSelector.AddOptions(labels);
      _currentLineIndex = currentIndex;
      _lineSelector.SetValueWithoutNotify(currentIndex);
      _lineSelector.RefreshShownValue();
      _lineSelector.gameObject.SetActive(true);
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
      _imgMinMap.rectTransform.sizeDelta = new Vector2(_mapsize,_mapsize);
   }

   /// <summary>将地图边长缩小为当前的 1/1.1，并限制在 512 到 2048。</summary>
   public void OnDecBtnClicked()
   {
      Debug.Log("OnDecBtnClicked");
      _mapsize = Mathf.Clamp(_mapsize / 1.1f,512,2048);
      _imgMinMap.rectTransform.sizeDelta = new Vector2(_mapsize,_mapsize);
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
         _imgMinMap.sprite = _mapHelp.MapSprite;
         if (_imgMinMap.sprite == null) _imgMinMap.sprite = _defaultMapSprite;
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
