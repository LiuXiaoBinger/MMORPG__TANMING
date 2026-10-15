using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

/**
* Title:控制器base类
* Descrpiton:
*/

/// <summary>
/// UI 控制器基类：持有对应视图并转发窗口显隐、弹窗打开与销毁逻辑，子控制器在此注册业务回包。
/// </summary>
public class CtrlBase : IDisposable
{
    /// <summary>该控制器绑定的 UI 视图实例。</summary>
    protected UIBase _view;
    /// <summary>构造时绑定视图。</summary>
    /// <param name="view">对应视图对象。</param>
    public CtrlBase(UIBase view)
    {
        _view =  view;
    }

    /// <summary>显示或隐藏当前窗口。</summary>
    /// <param name="isShow">true 显示，false 隐藏。</param>
    public virtual void ShowView(bool isShow = true)
    {
        _view.Show(isShow);
    }
    /// <summary>
    /// 弹出指定类型子窗口。
    /// </summary>
    /// <param name="windowType">目标窗口枚举。</param>
    /// <param name="obj">透传给窗口的业务数据。</param>
    public virtual void ShowWindow(WindowType windowType,object obj=null)
    {
        if (!_view.gameObject.activeSelf) { _view.Show(); }
        _view.ShowWindow(windowType,obj);
    }
    /// <summary>
    /// 在主界面中打开指定常驻窗口。
    /// </summary>
    /// <param name="windowType">目标主界面窗口枚举。</param>
    /// <param name="obj">透传给窗口的业务数据。</param>
    public virtual void ShowMainWindow(WindowType windowType,object obj=null)
    {
        if (!_view.gameObject.activeSelf) { _view.Show(); }
        _view.ShowMainWindow(windowType,obj);
    }
    /// <summary>
    /// 释放控制器资源，子类在此注销网络、输入和视图事件。
    /// </summary>
    public virtual void Dispose()
    {
        // 子控制器在这里注销网络、输入和视图事件。
    }
    /// <summary>
    /// 刷新指定窗口数据。
    /// </summary>
    /// <param name="windowType">需要刷新的窗口枚举。</param>
    /// <param name="obj">刷新所需的数据对象。</param>
    public virtual void RefreshWindow(WindowType windowType, Object obj=null)
    {
        _view.RefreshWindow(windowType,obj);
    }
}
