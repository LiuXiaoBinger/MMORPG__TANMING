using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class NpcCtrl : RoleCtrlBase
{
    
    public void OpenTalk( RoleCtrlBase mainRole )
    {
        if (Vector3.Distance(mainRole.transform.position, transform.position) > 10)
        {
            TipsMgr.Instance.ShowSystemTips("距离过远，请靠近");
            return;
        }
        transform.LookAtTarget(mainRole.transform);
        
        UIRoot.Instance.MainCtrl.ShowMainWindow(WindowType.TalkWindow);
    }
}
