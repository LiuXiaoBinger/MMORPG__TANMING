using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

/**
* Title:控制器base类
* Descrpiton:
*/

public class CtrlBase : IDisposable
{
    protected UIBase _view;
    public CtrlBase(UIBase view)
    {
        _view =  view;
    }

    public virtual void ShowView(bool isShow = true)
    {
        _view.Show(isShow);
    }
    /// <summary>
    /// 显示Window
    /// </summary>
    /// <param name="windowType"></param>
    public virtual void ShowWindow(WindowType windowType,object obj=null)
    {
        if (!_view.gameObject.activeSelf) { _view.Show(); }
        _view.ShowWindow(windowType,obj);
    }
    /// <summary>
    /// 显示Window
    /// </summary>
    /// <param name="windowType"></param>
    public virtual void ShowMainWindow(WindowType windowType,object obj=null)
    {
        if (!_view.gameObject.activeSelf) { _view.Show(); }
        _view.ShowMainWindow(windowType,obj);
    }
    public virtual void Dispose()
    {
        // 子控制器在这里注销网络、输入和视图事件。
    }
    public virtual void RefreshWindow(WindowType windowType, Object obj=null)
    {
        _view.RefreshWindow(windowType,obj);
    }
}
