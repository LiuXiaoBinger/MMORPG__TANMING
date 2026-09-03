
using Google.Protobuf;
using UnityEngine;

/**
* Title:
* Descrpiton:
*/

public class MainCtrl : CtrlBase
{
    private MainView _mainView;
    public MainCtrl(UIBase view) : base(view)
    {
        _mainView = view as MainView;
        if (_mainView != null) _mainView.InitView();
        RegisCommand();
    }

    private void RegisCommand()
    {
        // 装备数据和背包数据由同一个服务器快照返回，因此共用背包协议码。
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleSkillInfoCode,RoleSkillInfoHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleKnapsackInfoCode,RoleKnapsackInfoHandle);
    }
    /// <summary>
    /// 角色端返回角色背包数据
    /// </summary>
    /// <param name="data"></param>
    private void RoleKnapsackInfoHandle(ByteString data)
    {
        RoleKanpsackInfoRet ret =RoleKanpsackInfoRet.Parser.ParseFrom(data);
        if (ret != null && ret.CmdCode == CmdCode.Succeed)
        {
            Debug.Log("RoleKnapsackInfoHandle:: "+ret.ToString());
            // 背包窗口显示所有物品，角色属性窗口只读取 RoleCurrtEquipPack。
            UIRoot.Instance.MainCtrl.RefreshWindow(WindowType.KnapsackWindow,ret);
            UIRoot.Instance.MainCtrl.RefreshWindow(WindowType.RoleAttriibuteWindow, ret);
        }
    }
    /// <summary>
    /// 服务端返回技能相关处理
    /// </summary>
    /// <param name="data"></param>
    private void RoleSkillInfoHandle(ByteString data)
    {
        RoleSkillInfoRet ret =RoleSkillInfoRet.Parser.ParseFrom(data);
        if (ret != null && ret.CmdCode == CmdCode.Succeed)
        {
            Debug.Log("RoleSkillInfoHandle"+ret.ToString());
            //更新技能相关列表
            UIRoot.Instance.MainCtrl.RefreshWindow(WindowType.SkillInfoWindow,ret.RoleSkillInfoList);
            
            //更新技能相关列表
            UIRoot.Instance.MainCtrl.RefreshWindow(WindowType.RoleCurrtInfoWindow,ret.RoleSkillInfoList);
        }
    }

  

    public void MainUIKeyHandler(string key)
    {
        switch (key)
        {
            case "L":
                ShowMainWindow(WindowType.SkillInfoWindow);
                break;
            case "B":
                ShowMainWindow(WindowType.KnapsackWindow);
                CameraMgr.Instance.KnapsackWindowAngle(_mainView.GetWindow(WindowType.KnapsackWindow));
                break;
            case "I":
                ShowMainWindow(WindowType.RoleAttriibuteWindow);
                CameraMgr.Instance.RoleAttrWindowAngle(_mainView.GetWindow(WindowType.RoleAttriibuteWindow));
                break;
        }
    }
}
