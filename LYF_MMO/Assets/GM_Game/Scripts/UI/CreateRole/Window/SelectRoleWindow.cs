using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:选择角色window
* Descrpiton:
*/

public class SelectRoleWindow : WindowBase
{ 
    [SerializeField,Header("头像图片")] private Image _imagHead; 
    
    [SerializeField,Header("昵称")] private TMP_Text _texNickName;
    [SerializeField,Header("等级和职业")] private TMP_Text _texJobLevel;
    public  Action<int> StartGameBtnClicked;
    private int _roleID;
    public void OnStartGameBtnClicked()
    {
        
        //进入主城，开始真正的游戏
        if (_roleID>=0)
        {
            StartGameBtnClicked?.Invoke(_roleID);
        }
        
    }

    public override void ReFreshUI(object obj)
    {
        CreateRoleRet ret = obj as CreateRoleRet;
        if (ret != null)
        {
            _roleID = ret.RoleId;
            _texNickName.SetText(ret.Nickname);
            string jobstr = "";
            if (ret.JobId == 1)
            {
                jobstr = "剑修";
            }
            _texJobLevel.SetText($"职业:{jobstr}   Lv.{ret.Level}");
        }
    }

    
}
