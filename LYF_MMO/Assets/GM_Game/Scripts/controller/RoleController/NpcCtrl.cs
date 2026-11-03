using System.Collections;
using System.Collections.Generic;
using MMoRpgCommon;
using UnityEngine;
/**
* NPC 模型侧的对话入口。
*/

/// <summary>
/// NPC 表现控制器：保存对应的 NpcEntity，负责走近检测并打开 NPC 对话窗口。
/// </summary>
public class NpcCtrl : RoleCtrlBase
{
    /// <summary>允许打开对话的三维距离上限，单位为世界单位；点击 NPC 导航也使用此距离。</summary>
    public const float TalkDistance = 10f;
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
    
    /// <summary>主角位于对话距离内时，让 NPC 面向主角并打开对话窗口；过远只显示提示。</summary>
    /// <param name="mainRole">本地主角的现有表现控制器，用于读取实际碰撞后位置。</param>
    public void OpenTalk(RoleCtrlBase mainRole)
    {
        if (mainRole == null)
        {
            TipsMgr.Instance.ShowSystemTips("主角尚未创建完成");
            return;
        }

        if (Vector3.Distance(mainRole.transform.position, transform.position) > TalkDistance)
        {
            TipsMgr.Instance.ShowSystemTips("距离过远，请靠近");
            return;
        }
        transform.LookAtTarget(mainRole.transform);
       
        UIRoot.Instance.MainCtrl.ShowMainWindow(WindowType.TalkWindow, _npcEntity);
    }
}
