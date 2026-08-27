using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class TalkWindow : WindowBase
{

    public void OnShopBtnClick()
    {
        UIRoot.Instance.MainCtrl.ShowMainWindow(WindowType.ShopWindow);
        CloseWindow();
    }
}
