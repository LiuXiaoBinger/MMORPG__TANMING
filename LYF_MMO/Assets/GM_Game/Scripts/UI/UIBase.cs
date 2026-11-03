      using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

/**
* Title:UI基类
* Descrpiton:适用于所有UI的基类
*/

/// <summary>
/// 一个业务 View 的公共基类，按 WindowType 保存它拥有的子窗口。
/// ShowWindow 负责互斥显示，ShowMainWindow 只开关指定窗口；业务数据与协议由外部控制器提供。
/// </summary>
public class UIBase : MonoBehaviour
{ 
    /// <summary>本视图管理的全部子窗口，按键为窗口枚举。</summary>
    protected Dictionary<WindowType, WindowBase> windowDic;

    /// <summary>创建窗口登记表；子 View 在调用基类后加入自己的预制体窗口引用。</summary>
    public virtual void InitView()
    {
        windowDic = new Dictionary<WindowType, WindowBase>();
    }

   /// <summary>按 isShow 显隐整个 View，默认显示，不改变各子窗口自身的激活状态。</summary>
   public virtual void Show(bool isShow = true)
   {
       gameObject.SetActive(isShow);
   }
   
   /// <summary>
   /// 取得已经登记的窗口；直接访问字典，未登记的类型会抛出键不存在异常。
   /// </summary>
   /// <param name="windowType">本 View 已登记的窗口类型。</param>
   /// <returns>预制体上的窗口组件引用。</returns>
   public WindowBase GetWindow(WindowType windowType)
   { 
       return windowDic[windowType];
   }
   /// <summary>
   /// 只显示指定子窗口，隐藏同一 View 登记的其余窗口。
   /// 非空数据会交给所有窗口的 Show，因此隐藏窗口也可能刷新。
   /// </summary>
   /// <param name="windowType">需要保留显示的窗口类型。</param>
   /// <param name="obj">传给各窗口的刷新数据，空值表示不通过 Show 刷新。</param>
   public void ShowWindow(WindowType windowType,object obj = null)
   {
       if(windowDic==null||windowDic.Count<=0) return; 
       // 对全部已登记窗口设置显隐；WindowBase.Show 会另行处理非空刷新数据。
       foreach (var item in windowDic)
       {
           item.Value.Show(windowType==item.Key,obj);
       }
       
   }
   
   /// <summary>
   /// 开关指定窗口，不改变其他窗口；打开前先刷新一次，关闭时不刷新。
   /// </summary>
   /// <param name="windowType">要切换显隐的窗口类型。</param>
   /// <param name="obj">仅打开时传给目标窗口的数据，可为空。</param>
   public void ShowMainWindow(WindowType windowType,object obj = null)
   {
       if(windowDic==null||windowDic.Count<=0) return; 
       // 找到指定窗口后，显示中的窗口关闭，隐藏中的窗口刷新后打开。
       if (windowDic.ContainsKey(windowType))
       {
           if (windowDic[windowType].gameObject.activeSelf)
           {
               windowDic[windowType].Show(false);
           }
           else
           {
               windowDic[windowType].ReFreshUI(obj);
               windowDic[windowType].Show(true);
           }
       }
       
   }

   /// <summary>把 obj 交给 windowType 对应窗口刷新，不改变显隐；窗口必须已经登记。</summary>
   public virtual void RefreshWindow(WindowType windowType, object obj = null)
   {
       windowDic[windowType].ReFreshUI(obj);
   }
}
