using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:创建角色和选择角色
* Descrpiton:
*/

/// <summary>持有创建角色和选择角色两个窗口，把窗口事件交给创建角色控制器处理。</summary>
public class CreateRoleView : UIBase
{
    /// <summary>输入昵称、切换职业并提交创建请求的窗口。</summary>
    [SerializeField,Header("创建角色window")]private CreateRoleWindow _createRoleWindow; 
    /// <summary>展示已有角色并提交开始游戏操作的窗口。</summary>
    [SerializeField,Header("选择角色Window")]private SelectRoleWindow _selectRoleWindow;  
    /// <summary>把两个窗口登记到 View 的 WindowType 字典，供控制器切换和刷新。</summary>
    public override void InitView()
    {
        base.InitView();
        windowDic.Add(WindowType.CreateRoleWindow, _createRoleWindow);
        windowDic.Add(WindowType.SelectRoleWindow, _selectRoleWindow);
    }

    /// <summary>设置创建按钮回调 func，参数依次为昵称和从 1 开始的职业编号。</summary>
    public void RegisCreateRoleBtnClick(Action<string,int> func)
    {
        _createRoleWindow.CreateRoleBtnClickAction = func;
    }
    /// <summary>设置开始游戏回调 func，参数为当前选中的角色 ID。</summary>
    public void RegisStartGameBtnClick(Action<int> func)
    {
        _selectRoleWindow.StartGameBtnClicked = func;
    }

    /// <summary>清理创建角色控制器注册的视图事件。</summary>
    public void ClearActions()
    {
        _createRoleWindow.CreateRoleBtnClickAction = null;
        _selectRoleWindow.StartGameBtnClicked = null;
    }
}
