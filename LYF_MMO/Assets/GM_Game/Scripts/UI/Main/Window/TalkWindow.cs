using System.Collections;
using System.Collections.Generic;
using MMoRpgCommon;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class TalkWindow : WindowBase
{
    private NpcEntity _currentNpc;

    public override void ReFreshUI(object obj)
    {
        _currentNpc = obj as NpcEntity;
    }

    public void OnShopBtnClick()
    {
        if (_currentNpc == null)
        {
            TipsMgr.Instance.ShowSystemTips("当前没有可交互的 NPC");
            return;
        }

        UIRoot.Instance.MainCtrl.ShowMainWindow(WindowType.ShopWindow, _currentNpc);
    }
}
