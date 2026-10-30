using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

/// <summary>跟随当前场景小地图辅助点展示角色位置；未配置辅助点的场景隐藏小地图。</summary>
public class MinMapWindow : WindowBase
{
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

   /// <summary>初始化图片边长并订阅场景切换，保持常驻窗口与场景辅助点的生命周期一致。</summary>
   private void Start()
   {
      _mapsize=_imgMinMap.sprite.rect.width;
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
         //实施更新小地图位置 ，根据角色信息的位置来更新
         _mapHelp.transform.position = roleTransform.position;
         
         _imgMinMap.rectTransform.anchoredPosition = new Vector2((_mapHelp.transform.localPosition.x*-_mapsize),(_mapHelp.transform.localPosition.z*-_mapsize));
         //箭头
         _imgArrow.transform.localEulerAngles = new Vector3(0,0,360-roleTransform.localEulerAngles.y+90);
      }
   }

   /// <summary>场景对象完成 Awake 后取新辅助点，不沿用主城已销毁的缓存。</summary>
   /// <param name="scene">本次完成加载的场景；辅助点由该场景自身发布。</param>
   /// <param name="mode">Unity 本次场景加载模式，不改变既有加载流程。</param>
   private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
   {
      BindSceneMap();
   }

   /// <summary>绑定当前场景辅助点；没有小地图配置时隐藏整块 UI，回到有配置场景时恢复。</summary>
   private void BindSceneMap()
   {
      _mapHelp = MinMapHelp.instance;
      gameObject.SetActive(_mapHelp != null);
   }

   /// <summary>窗口销毁时释放场景订阅；仅隐藏时保留订阅，以便下一场景重新启用。</summary>
   private void OnDestroy()
   {
      SceneManager.sceneLoaded -= OnSceneLoaded;
   }



   /// <summary>由主界面控制器设置需要跟踪的本地玩家实体。</summary>
   /// <param name="role">场景世界持有的本地玩家；断线清理时可为空。</param>
   public void SetTrackedRole(PlayerEntity role)
   {
      _mainRole = role;
   }


   public void OnPluseBtnClicked()
   {
      Debug.Log("OnPluseBtnClicked");
      _mapsize = Mathf.Clamp(_mapsize * 1.1f,512,2048);
      _imgMinMap.rectTransform.sizeDelta = new Vector2(_mapsize,_mapsize);
   }

   public void OnDecBtnClicked()
   {
      Debug.Log("OnDecBtnClicked");
      _mapsize = Mathf.Clamp(_mapsize / 1.1f,512,2048);
      _imgMinMap.rectTransform.sizeDelta = new Vector2(_mapsize,_mapsize);
   }
}
