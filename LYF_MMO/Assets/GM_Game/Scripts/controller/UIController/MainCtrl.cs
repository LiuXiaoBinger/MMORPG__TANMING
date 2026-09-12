using Google.Protobuf;
using MMoRpgCommon;
using UnityEngine;

/// <summary>
/// 主界面控制器，组合商城、技能、角色信息和聊天流程。
/// </summary>
public class MainCtrl : CtrlBase
{
    /// <summary>主界面视图集合。</summary>
    private readonly MainView _mainView;
    /// <summary>商城子控制器。</summary>
    private readonly ShopCtrl _shopCtrl;
    /// <summary>技能子控制器。</summary>
    private readonly SkillCtrl _skillCtrl;
    /// <summary>角色信息子控制器。</summary>
    private readonly RoleInfoCtrl _roleInfoCtrl;
    /// <summary>背包角色组件与背包窗口之间的控制器。</summary>
    private readonly KnapsackCtrl _knapsackCtrl;
    /// <summary>聊天窗口视图。</summary>
    private readonly ChatWindow _chatWindow;
    /// <summary>主界面快捷键是否已注册。</summary>
    private bool _mainInputRegistered;
    /// <summary>当前对话关联的 NPC，仅由控制器保存领域对象。</summary>
    private NpcEntity _currentTalkNpc;

    /// <summary>创建主界面控制器并组合各业务子控制器。</summary>
    public MainCtrl(UIBase view) : base(view)
    {
        _mainView = view as MainView;
        if (_mainView == null)
        {
            return;
        }

        _mainView.InitView();
        _shopCtrl = new ShopCtrl(_mainView);
        _skillCtrl = new SkillCtrl(_mainView);
        _roleInfoCtrl = new RoleInfoCtrl(_mainView);
        _knapsackCtrl = new KnapsackCtrl(_mainView);
        if (Global.Instance != null && Global.Instance.RoleWorld != null)
        {
            Global.Instance.RoleWorld.LocalRoleChanged += OnLocalRoleChanged;
        }
        BindTrackedRole();
        _chatWindow = _mainView.ChatWindow;
        RegisterCommands();
        RegisterViewActions();
    }

    /// <summary>注册主界面统一接收的协议回包。</summary>
    private void RegisterCommands()
    {
        // 背包快照只由主控制器注册一次，再分发到各业务控制器和模型。
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleKnapsackInfoCode,
            RoleKnapsackInfoHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_OpenKnapsackGridCode,
            OpenKnapsackGridHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_BuyShopItemCode,
            BuyShopItemHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_SC_UpdateItemInfoCode,
            UpdateItemInfoHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_CountUpdateSyncInfoCode,
            CountUpdateSyncInfoHandle);
    }

    /// <summary>注册 NPC 对话和聊天视图事件。</summary>
    private void RegisterViewActions()
    {
        if (_mainView.TalkWindow != null)
        {
            _mainView.TalkWindow.ShopRequested += OnShopRequested;
        }
        if (_chatWindow != null)
        {
            _chatWindow.InputFocusChanged += OnChatInputFocusChanged;
            _chatWindow.SendRequested += OnChatSendRequested;
        }
    }

    /// <summary>幂等注册主界面快捷键。</summary>
    public void RegisterMainInput()
    {
        // 主角可能晚于主界面控制器创建，因此每次主 UI 注册时重新绑定小地图。
        BindTrackedRole();
        _skillCtrl.RegisterInput();
        if (_mainInputRegistered || PlayerInputCtr.Instance == null)
        {
            return;
        }
        PlayerInputCtr.Instance.MainUIKeyHandler += MainUIKeyHandler;
        _mainInputRegistered = true;
    }

    /// <summary>商城窗口先由商城控制器准备数据，其余窗口沿用基础流程。</summary>
    public override void ShowMainWindow(WindowType windowType, object obj = null)
    {
        if (windowType == WindowType.TalkWindow)
        {
            _currentTalkNpc = obj as NpcEntity;
            NpcTalkViewData viewData = CreateNpcTalkViewData(_currentTalkNpc);
            base.ShowMainWindow(windowType, viewData);
            return;
        }
        if (windowType == WindowType.ShopWindow)
        {
            _shopCtrl.OpenNpcShop(obj as NpcEntity);
            return;
        }
        base.ShowMainWindow(windowType, obj);
    }

    /// <summary>处理 NPC 对话窗口发出的商城请求。</summary>
    private void OnShopRequested()
    {
        if (_currentTalkNpc == null)
        {
            TipsMgr.Instance.ShowSystemTips("当前没有可交互的 NPC");
            return;
        }
        _shopCtrl.OpenNpcShop(_currentTalkNpc);
    }

    /// <summary>在主角创建完成后把跟踪对象交给小地图 View。</summary>
    private void BindTrackedRole()
    {
        if (_mainView == null || _mainView.MinMapWindow == null || Global.Instance == null)
        {
            return;
        }
        _mainView.MinMapWindow.SetTrackedRole(Global.Instance.roleCtrlBase);
    }

    /// <summary>角色切换或断线清理时重新绑定背包控制器。</summary>
    private void OnLocalRoleChanged(ClientRole role)
    {
        if (_knapsackCtrl != null)
        {
            _knapsackCtrl.BindRole(role);
        }
        BindTrackedRole();
    }

    /// <summary>把 NPC 领域对象转换为对话展示数据。</summary>
    private NpcTalkViewData CreateNpcTalkViewData(NpcEntity npcEntity)
    {
        if (npcEntity == null)
        {
            return null;
        }
        return new NpcTalkViewData(npcEntity.NpcID, npcEntity.Name, npcEntity.Talk);
    }

    /// <summary>把商城购买回包交给商城模型解析。</summary>
    private void BuyShopItemHandle(ByteString data)
    {
        ShopMgr.Instance.HandleResponse(data);
    }

    /// <summary>接收服务器物品增量并交给角色背包组件更新客户端模型。</summary>
    private void UpdateItemInfoHandle(ByteString data)
    {
        UpdateItemRet result;
        try
        {
            result = UpdateItemRet.Parser.ParseFrom(data);
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError("解析物品增量回包失败：" + exception.Message);
            return;
        }

        if (result == null || result.CmdCode != CmdCode.Succeed || result.ItemDate == null)
        {
            return;
        }

        ClientRole localRole = null;
        if (Global.Instance != null && Global.Instance.RoleWorld != null)
        {
            localRole = Global.Instance.RoleWorld.LocalRole;
        }
        if (localRole == null)
        {
            return;
        }

        RoItemComponent itemComponent;
        if (localRole.TryGetComponent(out itemComponent))
        {
            itemComponent.ApplyServerItemDelta(result.ItemDate);
        }
    }

    /// <summary>接收服务器次数增量并写入当前角色次数组件。</summary>
    private void CountUpdateSyncInfoHandle(ByteString data)
    {
        CountUpdateSyncInfo result;
        try
        {
            result = CountUpdateSyncInfo.Parser.ParseFrom(data);
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError("解析次数同步回包失败：" + exception.Message);
            return;
        }
        if (result == null)
        {
            return;
        }
        ClientRole localRole = null;
        if (Global.Instance != null && Global.Instance.RoleWorld != null)
        {
            localRole = Global.Instance.RoleWorld.LocalRole;
        }
        if (localRole == null)
        {
            return;
        }
        RoleCountComponent countComponent;
        if (localRole.TryGetComponent(out countComponent))
        {
            countComponent.ApplyServerSync(result);
        }
    }

    /// <summary>分发角色背包快照到背包、商城和角色信息模块。</summary>
    private void RoleKnapsackInfoHandle(ByteString data)
    {
        RoleKanpsackInfoRet result = RoleKanpsackInfoRet.Parser.ParseFrom(data);
        if (result == null || result.CmdCode != CmdCode.Succeed ||
            result.RoleKanpsackInfo == null)
        {
            return;
        }

        RoleKanpsackInfo info = result.RoleKanpsackInfo;
        ClientRole localRole = Global.Instance.RoleWorld.LocalRole;
        if (localRole == null || !localRole.LoadRoleData(result))
        {
            return;
        }
        _knapsackCtrl.BindRole(localRole);
        _roleInfoCtrl.RefreshEquipment(info);
        _roleInfoCtrl.RefreshCurrentRole();
    }

    /// <summary>使用服务端返回的容量刷新对应背包分类。</summary>
    private void OpenKnapsackGridHandle(ByteString data)
    {
        OpenKnapsackGridRet result = OpenKnapsackGridRet.Parser.ParseFrom(data);
        if (result == null || result.CmdCode != CmdCode.Succeed)
        {
            return;
        }

        RoItemComponent itemComponent;
        ClientRole localRole = Global.Instance.RoleWorld.LocalRole;
        if (localRole != null && localRole.TryGetComponent(out itemComponent))
        {
            itemComponent.SetOpenedGridCount((KnapsackType)result.KnapsackType,
                result.CurrentOpenedGridCount);
        }
    }

    /// <summary>聊天输入期间暂停角色输入。</summary>
    private void OnChatInputFocusChanged(bool focused)
    {
        if (PlayerInputCtr.Instance == null)
        {
            return;
        }
        if (focused)
        {
            PlayerInputCtr.Instance.OnDisable();
        }
        else
        {
            PlayerInputCtr.Instance.OnEnable();
        }
    }

    /// <summary>校验聊天文本并协调本地回显。</summary>
    private void OnChatSendRequested(string channel, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TipsMgr.Instance.ShowSystemTips("请输入聊天内容");
            return;
        }
        if (_chatWindow != null)
        {
            // 当前聊天协议尚未接入，暂由控制器提供本地昵称和回显。
            _chatWindow.AppendMessage(channel, "昵称:小米", message);
        }
    }

    /// <summary>处理主界面快捷键。</summary>
    public void MainUIKeyHandler(string key)
    {
        switch (key)
        {
            case "L":
                ShowMainWindow(WindowType.SkillInfoWindow);
                break;
            case "B":
                ShowMainWindow(WindowType.KnapsackWindow);
                CameraMgr.Instance.KnapsackWindowAngle(
                    _mainView.GetWindow(WindowType.KnapsackWindow));
                break;
            case "I":
                ShowMainWindow(WindowType.RoleAttriibuteWindow);
                CameraMgr.Instance.RoleAttrWindowAngle(
                    _mainView.GetWindow(WindowType.RoleAttriibuteWindow));
                break;
        }
    }

    /// <summary>注销主控制器持有的全部协议、输入和视图事件。</summary>
    public override void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_RoleKnapsackInfoCode);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_OpenKnapsackGridCode);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_BuyShopItemCode);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SC_UpdateItemInfoCode);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_CountUpdateSyncInfoCode);
        if (_mainInputRegistered && PlayerInputCtr.Instance != null)
        {
            PlayerInputCtr.Instance.MainUIKeyHandler -= MainUIKeyHandler;
            _mainInputRegistered = false;
        }
        if (_mainView != null && _mainView.TalkWindow != null)
        {
            _mainView.TalkWindow.ShopRequested -= OnShopRequested;
        }
        if (_chatWindow != null)
        {
            _chatWindow.InputFocusChanged -= OnChatInputFocusChanged;
            _chatWindow.SendRequested -= OnChatSendRequested;
        }
        if (_shopCtrl != null)
        {
            _shopCtrl.Dispose();
        }
        if (_skillCtrl != null)
        {
            _skillCtrl.Dispose();
        }
        if (_roleInfoCtrl != null)
        {
            _roleInfoCtrl.Dispose();
        }
        if (_knapsackCtrl != null)
        {
            _knapsackCtrl.Dispose();
        }
        if (Global.Instance != null && Global.Instance.RoleWorld != null)
        {
            Global.Instance.RoleWorld.LocalRoleChanged -= OnLocalRoleChanged;
        }
        _currentTalkNpc = null;
    }
}
