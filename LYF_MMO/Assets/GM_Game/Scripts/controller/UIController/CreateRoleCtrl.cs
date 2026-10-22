using Google.Protobuf;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 管理创建角色、选择角色和开始游戏协议，并在本地角色真正进入 SceneWorld 后切换主城场景。
/// 控制器销毁时会解除网络、视图和 Global 角色完成事件，避免旧控制器收到延迟回调。
/// </summary>
public class CreateRoleCtrl : CtrlBase
{
    /// <summary>创建角色界面实例，生命周期与当前 UIRoot 控制器一致。</summary>
    private CreateRoleView _createRoleView;

    /// <summary>当前订阅本地角色完成事件的 Global 实例，用于精确解除旧实例回调。</summary>
    private Global _subscribedGlobal;

    /// <summary>控制器是否已释放；释放后网络回包和延迟角色完成事件不再驱动界面。</summary>
    private bool _isDisposed;

    /// <summary>是否正在切换主城场景，防止重复成功回包并发触发多次场景加载。</summary>
    private bool _isSceneTransitionPending;

    /// <summary>最近一次完成主城切换的登录账号 ID，用于识别已加载角色的重复回包。</summary>
    private int _lastCompletedAccountId;

    /// <summary>最近一次完成主城切换的角色 ID，用于识别同账号同角色的重复回包。</summary>
    private int _lastCompletedRoleId;

    /// <summary>初始化创建角色控制器并注册网络、视图和本地角色完成事件。</summary>
    /// <param name="view">由 UIRoot 持有的创建角色界面。</param>
    public CreateRoleCtrl(UIBase view) : base(view)
    {
        _createRoleView = view as CreateRoleView;
        if (_createRoleView != null)
        {
            _createRoleView.InitView();
        }
        RegisCommand();
        EnsureGlobalSubscription();
    }

    /// <summary>注册创建角色和开始游戏协议，以及两个界面按钮事件。</summary>
    private void RegisCommand()
    {
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_CreateRoleCode,CreateRoleHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_StartGameCode,StartGame);
        if (_createRoleView != null)
        {
            _createRoleView.RegisCreateRoleBtnClick(OnCreateRoleBtnClicked);
            _createRoleView.RegisStartGameBtnClick(OnStartGameBtnClicked);
        }
    }

    /// <summary>处理开始游戏回包，把成功角色数据提交给 Global 的动画启动门控。</summary>
    /// <param name="data">服务器返回的 StartGameRet Protobuf 字节数据。</param>
    private void StartGame(ByteString data)
    {
        if (_isDisposed || data == null)
        {
            return;
        }

        // 解析后的开始游戏结果，包含命令状态和完整本地角色信息。
        StartGameRet startGameResult = StartGameRet.Parser.ParseFrom(data.ToByteArray());
        if (startGameResult == null || startGameResult.CmdCode != CmdCode.Succeed)
        {
            TipsMgr.Instance.ShowSystemTips("开始游戏失败");
            return;
        }
        if (startGameResult.MainRoleInfo == null ||
            startGameResult.MainRoleInfo.BaseInfo == null)
        {
            TipsMgr.Instance.ShowSystemTips("开始游戏失败");
            return;
        }

        if (!EnsureGlobalSubscription())
        {
            Debug.LogError("全局客户端生命周期尚未初始化，无法接收开始游戏角色数据。");
            TipsMgr.Instance.ShowSystemTips("客户端尚未初始化，请稍后重试");
            return;
        }

        // 当前 Global 返回的提交状态，用于明确区分立即加载、排队、重复回包和拒绝。
        Global.LocalRoleLoadSubmitResult submitResult =
            _subscribedGlobal.SubmitLocalRoleLoad(startGameResult.MainRoleInfo);
        if (submitResult == Global.LocalRoleLoadSubmitResult.Rejected)
        {
            TipsMgr.Instance.ShowSystemTips("角色数据无效，无法进入游戏");
            return;
        }
        if (submitResult == Global.LocalRoleLoadSubmitResult.Queued)
        {
            Debug.Log("开始游戏成功，角色数据正在等待动画资源就绪。");
            return;
        }
        if (submitResult == Global.LocalRoleLoadSubmitResult.Loaded)
        {
            Debug.Log("开始游戏成功，本地角色已进入场景实体世界。");
            return;
        }

        Debug.Log("开始游戏成功，相同本地角色已存在，未重复创建实体。");
    }

    /// <summary>确保只订阅当前 Global 实例；实例切换时先解除旧事件，隔离旧生命周期回调。</summary>
    /// <returns>存在可用 Global 且完成事件订阅时返回 true。</returns>
    private bool EnsureGlobalSubscription()
    {
        if (_isDisposed)
        {
            return false;
        }

        // 当前 Unity 生命周期中的 Global 实例，可能与控制器此前订阅的旧实例不同。
        Global currentGlobal = Global.Instance;
        if (currentGlobal == null)
        {
            return false;
        }
        if (ReferenceEquals(_subscribedGlobal, currentGlobal))
        {
            return true;
        }
        if (_subscribedGlobal != null)
        {
            _subscribedGlobal.LocalRoleLoadCompleted -= OnLocalRoleLoadCompleted;
        }
        _subscribedGlobal = currentGlobal;
        _subscribedGlobal.LocalRoleLoadCompleted += OnLocalRoleLoadCompleted;
        return true;
    }

    /// <summary>接收 Global 在动画门控后真正创建成功的本地玩家，并继续开始游戏流程。</summary>
    /// <param name="localRole">已经完成 SceneWorld 创建和入世界生命周期的本地玩家实体。</param>
    private void OnLocalRoleLoadCompleted(PlayerEntity localRole)
    {
        if (_isDisposed || localRole == null)
        {
            return;
        }
        if (localRole.RoleInfo == null || localRole.BaseInfo == null)
        {
            return;
        }
        if (_lastCompletedAccountId == localRole.RoleInfo.AccountId &&
            _lastCompletedRoleId == localRole.BaseInfo.RoleId)
        {
            Debug.Log("忽略同一账号角色的重复本地角色完成通知。");
            return;
        }
        CompleteStartGame(localRole);
    }

    /// <summary>在本地角色实际可用后仅执行一次主城场景切换和主界面初始化。</summary>
    /// <param name="localRole">已经可供主城场景读取的本地玩家实体。</param>
    private void CompleteStartGame(PlayerEntity localRole)
    {
        if (_isDisposed || _isSceneTransitionPending || localRole == null)
        {
            return;
        }
        if (localRole.RoleInfo == null || localRole.BaseInfo == null)
        {
            return;
        }

        _isSceneTransitionPending = true;
        _lastCompletedAccountId = localRole.RoleInfo.AccountId;
        _lastCompletedRoleId = localRole.BaseInfo.RoleId;
        // 发起切场景时持有角色会话的 Global 实例，回调时必须仍为同一生命周期。
        Global requestedGlobal = _subscribedGlobal;
        // 发起切场景时持有角色的实体世界，防止 Global 重建后旧回调进入新世界。
        SceneWorldSystem requestedWorld = requestedGlobal.SceneWorld;
        // 发起切场景时的玩家实例，引用身份用于隔离对象池复用后的同 UID 对象。
        PlayerEntity requestedLocalRole = localRole;
        // 发起切场景时玩家实体的运行时 UID，用于识别角色替换和对象池复用。
        ulong requestedRoleUid = localRole.UID;
        // 发起切场景时的账号 ID，用于阻断换号后的旧场景回调。
        int requestedAccountId = localRole.RoleInfo.AccountId;
        // 发起切场景时的角色 ID，用于阻断同账号换角色后的旧场景回调。
        int requestedRoleId = localRole.BaseInfo.RoleId;
        // 发起切场景时的网络连接代次，用于阻断断线或自动重连前的旧场景回调。
        long requestedConnectionGeneration =
            MNetClient.Instance.CurrentConnectionGeneration;
        SceneMgr.Instance.LoadSceneMode(SceneType.Scene_MainCity, () =>
        {
            // 无论回调是否仍有效，都先释放本控制器的场景切换占用状态。
            _isSceneTransitionPending = false;
            if (!IsCurrentStartGameTransition(requestedGlobal, requestedWorld,
                requestedLocalRole, requestedRoleUid, requestedAccountId,
                requestedRoleId, requestedConnectionGeneration))
            {
                return;
            }
            if (UIRoot.Instance == null ||
                !ReferenceEquals(UIRoot.Instance.CreateRoleCtrl, this))
            {
                return;
            }
            UIRoot.Instance.CreateRoleCtrl.ShowView(false);
            UIRoot.Instance.InitMainCtrl();
        });
    }

    /// <summary>校验异步场景回调仍属于当前 Global、连接代次和同一个本地玩家实例。</summary>
    /// <param name="requestedGlobal">发起切场景时订阅的 Global 生命周期实例。</param>
    /// <param name="requestedWorld">发起切场景时持有该玩家的 SceneWorld 实例。</param>
    /// <param name="requestedLocalRole">发起切场景时实际入世界的玩家实例。</param>
    /// <param name="requestedRoleUid">发起切场景时玩家实体的运行时唯一 UID。</param>
    /// <param name="requestedAccountId">发起切场景时玩家所属的正数账号 ID。</param>
    /// <param name="requestedRoleId">发起切场景时玩家所属的正数角色 ID。</param>
    /// <param name="requestedConnectionGeneration">发起切场景时的正数网络连接代次。</param>
    /// <returns>全部生命周期身份仍一致且角色仍为当前本地玩家时返回 true。</returns>
    private bool IsCurrentStartGameTransition(Global requestedGlobal,
        SceneWorldSystem requestedWorld, PlayerEntity requestedLocalRole,
        ulong requestedRoleUid, int requestedAccountId, int requestedRoleId,
        long requestedConnectionGeneration)
    {
        if (_isDisposed || requestedGlobal == null || requestedWorld == null)
        {
            return false;
        }
        if (requestedLocalRole == null || requestedConnectionGeneration <= 0)
        {
            return false;
        }
        if (!ReferenceEquals(Global.Instance, requestedGlobal) ||
            !ReferenceEquals(_subscribedGlobal, requestedGlobal))
        {
            return false;
        }
        if (!ReferenceEquals(requestedGlobal.SceneWorld, requestedWorld) ||
            MNetClient.Instance.CurrentConnectionGeneration !=
            requestedConnectionGeneration)
        {
            return false;
        }
        if (!ReferenceEquals(requestedWorld.LocalRole, requestedLocalRole) ||
            requestedLocalRole.UID != requestedRoleUid ||
            requestedLocalRole.LifecycleState != EntityLifecycleState.Active)
        {
            return false;
        }
        if (requestedLocalRole.RoleInfo == null ||
            requestedLocalRole.BaseInfo == null)
        {
            return false;
        }
        if (requestedLocalRole.RoleInfo.AccountId != requestedAccountId ||
            requestedLocalRole.BaseInfo.RoleId != requestedRoleId)
        {
            return false;
        }
        if (requestedGlobal.LoginInfo == null ||
            requestedGlobal.LoginInfo.AccountId != requestedAccountId)
        {
            return false;
        }
        return true;
    }

    /// <summary>发送开始游戏请求；场景切换进行中时忽略重复点击。</summary>
    /// <param name="roleId">玩家在选角界面选择的正数角色 ID。</param>
    private void OnStartGameBtnClicked(int roleId)
    {
        if (_isDisposed || _isSceneTransitionPending || roleId <= 0)
        {
            return;
        }
        if (MNetClient.Client == null)
        {
            return;
        }
        // 显式的新选角请求允许同账号同角色在断线重登后重新进入主城。
        _lastCompletedAccountId = 0;
        _lastCompletedRoleId = 0;
        // 发往服务器的开始游戏请求，只包含当前选中的角色 ID。
        StartGameReq request = new StartGameReq(){RoleId = roleId};
        MNetClient.Instance.Send(NetDefine.CMD_StartGameCode,request.ToByteString());
    }

    /// <summary>发送创建角色请求。</summary>
    /// <param name="name">玩家输入的新角色名称。</param>
    /// <param name="jobId">玩家选择的职业配置 ID。</param>
    private void OnCreateRoleBtnClicked(string name,int jobId)
    {
        if (_isDisposed || Global.Instance == null)
        {
            return;
        }
        if (Global.Instance.LoginInfo == null || MNetClient.Client == null)
        {
            return;
        }
        // 发往服务器的创建角色请求，携带当前登录账号、区服和职业选择。
        CreateRoleReq request = new CreateRoleReq()
        {
            Nickname = name,
            AccountId = Global.Instance.LoginInfo.AccountId,
            GameServerId = Global.Instance.LoginInfo.GameServer.ServerId,
            JobId = jobId
        };
        MNetClient.Instance.Send(NetDefine.CMD_CreateRoleCode,request.ToByteString());
    }

    /// <summary>处理创建角色回包并刷新选角界面。</summary>
    /// <param name="data">服务器返回的 CreateRoleRet Protobuf 字节数据。</param>
    private void CreateRoleHandle(ByteString data)
    {
        if (_isDisposed || data == null)
        {
            return;
        }
        // 解析后的创建角色结果，用于判断是否刷新角色选择窗口。
        CreateRoleRet createRoleResult = CreateRoleRet.Parser.ParseFrom(data.ToByteArray());
        if (createRoleResult != null&&createRoleResult.CmdCode==CmdCode.Succeed)
        {
            Debug.Log("角色创建成功");
            ShowWindow(WindowType.SelectRoleWindow,createRoleResult);
        }
        else
        {
            Debug.Log("CreateRole Error角色创建失败");
            TipsMgr.Instance.ShowSystemTips("角色创建失败");
        }
    }

    /// <summary>注销创建角色协议、视图事件和 Global 延迟角色完成事件。</summary>
    public override void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_CreateRoleCode, CreateRoleHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_StartGameCode, StartGame);
        if (_subscribedGlobal != null)
        {
            _subscribedGlobal.LocalRoleLoadCompleted -= OnLocalRoleLoadCompleted;
            _subscribedGlobal = null;
        }
        if (_createRoleView != null)
        {
            _createRoleView.ClearActions();
        }
    }
}
