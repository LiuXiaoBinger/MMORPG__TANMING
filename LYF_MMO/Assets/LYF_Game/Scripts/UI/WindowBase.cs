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
   /// <summary>所属的窗口层，由主城视图初始化时按预制体父节点绑定。</summary>
   private RectTransform _panelParent;

   /// <summary>绑定窗口显示时使用的父容器。</summary>
   /// <param name="panelParent">该窗口在主城预制体中所属的功能层或提示层。</param>
   internal void SetPanelParentData(RectTransform panelParent)
   {
      _panelParent = panelParent;
   }

   /// <summary>应用已绑定的父容器，并将窗口放到该层最后绘制。</summary>
   private void SetPanelParent()
   {
      if (_panelParent == null)
      {
         return;
      }

      // 窗口根节点保留本地布局，并通过同级顺序绘制在所属层的其他窗口上方。
      Transform panel = transform;
      panel.SetParent(_panelParent, false);
      panel.SetAsLastSibling();
   }

   /// <summary>窗口初始化扩展点，基类不做操作；具体窗口按需绑定控件。</summary>
   public virtual void InitWindow()
   {

   }
   /// <summary>显示时先应用所属层的顺序，再设置窗口显隐。obj 非空时在显隐后刷新。</summary>
   /// <param name="isShow">true 显示并调整已绑定窗口的层内顺序，false 隐藏。</param>
   /// <param name="obj">沿用调用端传入的刷新数据，为空则跳过刷新。</param>
   public virtual void Show(bool  isShow = true,object obj = null)
   {
      if (isShow)
      {
         SetPanelParent();
      }
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
