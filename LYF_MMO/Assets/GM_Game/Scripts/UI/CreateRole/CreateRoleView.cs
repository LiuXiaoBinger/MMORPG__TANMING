using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:创建角色和选择角色
* Descrpiton:
*/

public class CreateRoleView : UIBase
{
    [SerializeField,Header("创建角色window")]private CreateRoleWindow _createRoleWindow; 
    [SerializeField,Header("选择角色Window")]private SelectRoleWindow _selectRoleWindow;  
    public override void InitView()
    {
        base.InitView();
        windowDic.Add(WindowType.CreateRoleWindow, _createRoleWindow);
        windowDic.Add(WindowType.SelectRoleWindow, _selectRoleWindow);
    }

    public void RegisCreateRoleBtnClick(Action<string,int> func)
    {
        _createRoleWindow.CreateRoleBtnClickAction = func;
    }
    public void RegisStartGameBtnClick(Action<int> func)
    {
        _selectRoleWindow.StartGameBtnClicked = func;
    }
}
