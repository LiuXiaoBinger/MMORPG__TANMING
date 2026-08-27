      using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

/**
* Title:UI基类
* Descrpiton:适用于所有UI的基类
*/

public class UIBase : MonoBehaviour
{ 
    protected Dictionary<WindowType, WindowBase> windowDic;

    public virtual void InitView()
    {
        windowDic = new Dictionary<WindowType, WindowBase>();
    }

   public virtual void Show(bool isShow = true)
   {
       gameObject.SetActive(isShow);
   }
   
   /// <summary>
   /// 根据WindowType返回window
   /// </summary>
   /// <param name="windowType"></param>
   /// <returns></returns>
   public WindowBase GetWindow(WindowType windowType)
   { 
       return windowDic[windowType];
   }
   /// <summary>
   /// 根据WindowType显示window
   /// </summary>
   /// <param name="windowType"></param>
   public void ShowWindow(WindowType windowType,object obj = null)
   {
       if(windowDic==null||windowDic.Count<=0) return; 
       //1.隐藏当前window
       foreach (var item in windowDic)
       {
           item.Value.Show(windowType==item.Key,obj);
       }
       
   }
   
   /// <summary>
   /// 根据WindowType显示window
   /// </summary>
   /// <param name="windowType"></param>
   public void ShowMainWindow(WindowType windowType,object obj = null)
   {
       if(windowDic==null||windowDic.Count<=0) return; 
       //1.隐藏当前window
       if (windowDic.ContainsKey(windowType))
       {
           if (windowDic[windowType].gameObject.activeSelf)
           {
               windowDic[windowType].Show(false);
           }
           else
           {
               windowDic[windowType].Show(true);
           }
       }
       
   }

   public virtual void RefreshWindow(WindowType windowType, object obj = null)
   {
       windowDic[windowType].ReFreshUI(obj);
   }
}
