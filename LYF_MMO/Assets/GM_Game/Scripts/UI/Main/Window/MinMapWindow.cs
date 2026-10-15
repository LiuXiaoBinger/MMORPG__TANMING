using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class MinMapWindow : WindowBase
{
   [SerializeField,Header("小地图背景图片")]private Image _imgMinMap;

   [SerializeField,Header("小地图箭头图片")]private Image _imgArrow;

   /// <summary>当前本地玩家实体；模型异步加载完成后从实体中读取真实 Transform。</summary>
   private PlayerEntity _mainRole;


   private MinMapHelp _mapHelp;

   private float _mapsize;

   private void Start()
   {
      _mapsize=_imgMinMap.sprite.rect.width;
      Debug.Log("_mapsize:"+_mapsize);
      _mapHelp = MinMapHelp.instance;

   }

   private void Update()
   {
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
