using System.Collections;
using System.Collections.Generic;
using MMoRpgCommon;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// NPC 表现控制器：持有 NpcEntity 领域数据，负责走近检测并打开 NPC 对话窗口。
/// </summary>
public class NpcCtrl : RoleCtrlBase
{
    /// <summary>当前 NPC 对应的领域实体数据。</summary>
    private NpcEntity _npcEntity;

    /// <summary>只读暴露 NPC 领域数据给对话窗口使用。</summary>
    public NpcEntity NpcData => _npcEntity;

    /// <summary>注入 NPC 领域实体。</summary>
    /// <param name="npcEntity">NPC 实体数据。</param>
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
