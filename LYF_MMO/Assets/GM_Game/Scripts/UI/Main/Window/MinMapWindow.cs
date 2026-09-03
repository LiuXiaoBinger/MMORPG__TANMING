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
   //暂时控制器赋值
   [SerializeField,Header("主角对象")]private RoleCtrlBase _mainRoleCtrl;

   private MinMapHelp _mapHelp;
   private float _mapsize;
   private void Start()
   {
      _mapsize=_imgMinMap.sprite.rect.width;
      Debug.Log("_mapsize:"+_mapsize);
      _mapHelp = MinMapHelp.instance;

      if (_mainRoleCtrl == null)
      {
         _mainRoleCtrl = Global.Instance.roleCtrlBase;

      }
   }

   private void Update()
   {
      if (_mainRoleCtrl != null)
      {
         //实施更新小地图位置 ，根据角色信息的位置来更新
         _mapHelp.transform.position = _mainRoleCtrl.transform.position;
         
         _imgMinMap.rectTransform.anchoredPosition = new Vector2((_mapHelp.transform.localPosition.x*-_mapsize),(_mapHelp.transform.localPosition.z*-_mapsize));
         //箭头
         _imgArrow.transform.localEulerAngles = new Vector3(0,0,360-_mainRoleCtrl.transform.localEulerAngles.y+90);
      }
      else
      {
         _mainRoleCtrl = Global.Instance.roleCtrlBase;
      }
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
