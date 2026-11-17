using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:选择角色window
* Descrpiton:
*/

/// <summary>展示服务器返回的角色摘要，并把选中角色 ID 交给开始游戏回调。</summary>
public class SelectRoleWindow : WindowBase
{ 
    /// <summary>预留头像图片引用，本类当前刷新流程尚未设置它。</summary>
    [SerializeField,Header("头像图片")] private Image _imagHead; 
    
    /// <summary>当前角色的昵称文本。</summary>
    [SerializeField,Header("昵称")] private TMP_Text _texNickName;
    /// <summary>合并显示职业名称和等级的文本。</summary>
    [SerializeField,Header("等级和职业")] private TMP_Text _texJobLevel;
    /// <summary>开始游戏通知，参数为缓存的角色 ID。</summary>
    public  Action<int> StartGameBtnClicked;
    /// <summary>最近一次角色刷新得到的 ID，点击开始游戏时提交。</summary>
    private int _roleID;
    /// <summary>缓存角色 ID 非负时通知控制器进入游戏，本窗口不直接加载主城。</summary>
    public void OnStartGameBtnClicked()
    {
        
        // 把角色选择交给外部控制器继续处理。
        if (_roleID>=0)
        {
            StartGameBtnClicked?.Invoke(_roleID);
        }
        
    }

    /// <summary>从 obj 中的 CreateRoleRet 更新 ID、昵称、职业和等级；其他类型的数据不处理。</summary>
    public override void ReFreshUI(object obj)
    {
        // 控制器提供的角色信息回包。
        CreateRoleRet ret = obj as CreateRoleRet;
        if (ret != null)
        {
            _roleID = ret.RoleId;
            _texNickName.SetText(ret.Nickname);
            // 当前仅给职业编号 1 配置了显示名称，其他编号保留空名称。
            string jobstr = "";
            if (ret.JobId == 1)
            {
                jobstr = "剑修";
            }
            _texJobLevel.SetText($"职业:{jobstr}   Lv.{ret.Level}");
        }
    }

    
}
