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
   /// <summary>当前场景中的小地图实例引用，由 Awake 赋值，供其它模块直接访问。</summary>
   public static MinMapHelp instance;

   /// <summary>组件加载时把自身写入静态单例，便于全局取用小地图节点。</summary>
   private void Awake()
   {
      instance = this;
   }
}
