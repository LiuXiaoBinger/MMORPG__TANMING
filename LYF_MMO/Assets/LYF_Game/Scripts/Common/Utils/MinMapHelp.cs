using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// 小地图辅助组件，挂载在小地图节点上，向外部提供静态单例入口。
/// </summary>
public class MinMapHelp : MonoBehaviour
{
   /// <summary>当前场景的小地图辅助点，由 Awake 登记、OnDestroy 清理，供常驻窗口重新绑定。</summary>
   public static MinMapHelp instance;

   /// <summary>场景自己的俯视底图；未指定时沿用主城窗口的默认底图。</summary>
   [SerializeField] private Sprite _mapSprite;
   /// <summary>供常驻小地图窗口在场景加载时切换底图。</summary>
   public Sprite MapSprite => _mapSprite;

   /// <summary>当前区域在地图标题和坐标栏中的名称。</summary>
   public string AreaName
   {
      get
      {
         if (gameObject.scene.name == "Scene_MainCity") return "主城";
         return gameObject.scene.name;
      }
   }

   /// <summary>将世界米制坐标投影到以底图中心为零的水平面。</summary>
   /// <param name="point">场景对象或导航目标的世界位置。</param>
   public Vector2 WorldToMap(Vector3 point)
   {
      // 父节点保存制图时的原点、旋转和世界覆盖尺寸。
      Vector3 local = transform.parent.InverseTransformPoint(point);
      return new Vector2(local.x, local.z);
   }

   /// <summary>把底图相对坐标还原为世界 XZ，Y 由调用方的导航表面决定。</summary>
   /// <param name="point">相对底图中心的横纵偏移，一个单位等于底图高度。</param>
   public Vector3 MapToWorld(Vector2 point)
   {
      return transform.parent.TransformPoint(new Vector3(point.x, 0f, point.y));
   }

   /// <summary>将图标锚在底图上，缩放和拖动都沿用同一个地理位置。</summary>
   /// <param name="mark">轴点居中的图标矩形。</param>
   /// <param name="point">以方形地理范围高度为单位的坐标。</param>
   /// <param name="aspect">当前底图显示宽高比。</param>
   public static void Place(RectTransform mark, Vector2 point, float aspect)
   {
      // 宽幅区域图的左右留出额外地形，纵向比例与正方形 HUD 底图一致。
      Vector2 anchor = new Vector2(0.5f + point.x / aspect, 0.5f + point.y);
      mark.anchorMin = anchor;
      mark.anchorMax = anchor;
      mark.anchoredPosition = Vector2.zero;
   }

   /// <summary>组件加载时把自身写入静态单例，便于全局取用小地图节点。</summary>
   private void Awake()
   {
      instance = this;
   }

   /// <summary>场景卸载时清理自身发布的引用，不清除其他场景已经登记的新辅助点。</summary>
   private void OnDestroy()
   {
      if (instance == this)
      {
         instance = null;
      }
   }
}
