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
