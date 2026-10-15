using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// 所有 UI 子窗口的基类：提供初始化、显隐、数据刷新与关闭的默认虚实现，子类按需重写。
/// </summary>
public class WindowBase : MonoBehaviour
{
   /// <summary>窗口初始化，子类重写以绑定控件。</summary>
   public virtual void InitWindow()
   {

   }
   /// <summary>显示窗口并在有数据时刷新。</summary>
   /// <param name="isShow">true 显示，false 隐藏。</param>
   /// <param name="obj">刷新数据，为空则不刷新。</param>
   public virtual void Show(bool  isShow = true,object obj = null)
   {
      gameObject.Show(isShow);
      if (obj != null)
      {
         ReFreshUI(obj);
      }
   }

   /// <summary>用外部数据刷新窗口内容，子类重写。</summary>
   /// <param name="obj">刷新数据。</param>
   public virtual void ReFreshUI(object obj)
   {

   }

   /// <summary>关闭（隐藏）本窗口。</summary>
   public virtual void CloseWindow()
   {
      gameObject.Show(false);
   }
}
