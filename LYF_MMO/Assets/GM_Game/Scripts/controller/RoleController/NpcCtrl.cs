using System.Collections;
using System.Collections.Generic;
using MMoRpgCommon;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class NpcCtrl : RoleCtrlBase
{
    private NpcEntity _npcEntity;

    public NpcEntity NpcData => _npcEntity;

    public void Initialize(NpcEntity npcEntity)
    {
        _npcEntity = npcEntity;
    }

    /*private void OnMouseDown()
    {
        RoleCtrlBase mainRole = Global.Instance == null ? null : Global.Instance.roleCtrlBase;
        OpenTalk(mainRole);
    }*/
    
    public void OpenTalk(RoleCtrlBase mainRole)
    {
        if (mainRole == null)
        {
            TipsMgr.Instance.ShowSystemTips("主角尚未创建完成");
            return;
        }

        if (Vector3.Distance(mainRole.transform.position, transform.position) > 10)
        {
            TipsMgr.Instance.ShowSystemTips("距离过远，请靠近");
            return;
        }
        transform.LookAtTarget(mainRole.transform);
       
        UIRoot.Instance.MainCtrl.ShowMainWindow(WindowType.TalkWindow, _npcEntity);
    }
}
