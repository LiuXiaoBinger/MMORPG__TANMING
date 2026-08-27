
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
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleSkillInfoCode,RoleSkillInfoHandle);
    }

    private void RoleSkillInfoHandle(ByteString data)
    {
        RoleSkillInfoRet ret =RoleSkillInfoRet.Parser.ParseFrom(data);
        if (ret != null && ret.CmdCode == CmdCode.Succeed)
        {
            Debug.Log("RoleSkillInfoHandle"+ret.ToString());
            //更新技能相关列表
            UIRoot.Instance.MainCtrl.RefreshWindow(WindowType.SkillInfoWindow,ret.RoleSkillInfoList);
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
