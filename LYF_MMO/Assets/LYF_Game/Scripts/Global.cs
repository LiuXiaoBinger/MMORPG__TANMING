using System;
using UnityEngine;
using YooAsset;

/// <summary>
/// 客户端启动入口：初始化网络、配置表和技能规则，等动画索引加载成功后创建场景实体世界。
/// 该组件从 Unity 启动持续存活到应用退出，并统一释放其持有的运行时资源。
/// </summary>
public class Global : MonoBehaviour {

    #region 字段与常量
    /// <summary>动画索引加载失败后的重试间隔，单位为秒，使用不受时间缩放影响的时钟。</summary>
    private const float AnimationPreloadRetryIntervalSeconds = 3f;

    /// <summary>全局 Unity 生命周期入口。</summary>
    public static Global Instance;

    /// <summary>YooAsset 默认资源包。</summary>
    private ResourcePackage _package;

    /// <summary>获取 YooAsset 默认资源包。</summary>
    public ResourcePackage YooPackage { get => _package; }

    /// <summary>当前账号登录信息；更换账号或退出登录时会清理上一账号的待加载角色和场景角色。</summary>
    [HideInInspector]
    public LoginRet LoginInfo
    {
        get { return _loginInfo; }
        set
        {
            if (HasLoginAccountChanged(_loginInfo, value))
            {
                ClearLocalRoleSession();
            }
            _loginInfo = value;
        }
    }

    /// <summary>当前账号登录信息的实际存储字段，生命周期持续到账号切换或 Global 销毁。</summary>
    private LoginRet _loginInfo;

    /// <summary>客户端唯一角色运行时容器。</summary>
    public SceneWorldSystem SceneWorld { get; private set; }

    /// <summary>动画索引是否已经成功加载；为 false 时不会创建场景实体世界。</summary>
    public bool IsAnimationReady
    {
        get { return MAnimationMgr.singleton.IsPreloaded; }
    }

    /// <summary>当前是否已有动画索引异步加载请求，防止失败重试期间重复登记回调。</summary>
    private bool _isAnimationPreloadRequested;

    /// <summary>下一次允许重试动画索引加载的实时秒数。</summary>
    private float _nextAnimationPreloadRetryTime;

    /// <summary>Global 是否正在退出或销毁；进入清理后不再发起异步重试。</summary>
    private bool _isShuttingDown;

    /// <summary>动画索引尚未就绪时暂存的最新角色数据；成功创建角色后清空。</summary>
    private MainRoleInfo _pendingMainRoleInfo;

    /// <summary>场景角色创建失败后下一次允许重试的实时秒数，避免每帧重复创建。</summary>
    private float _nextPendingRoleLoadRetryTime;

    /// <summary>当前待加载或已加载本地角色所属的网络连接代次；零表示离线测试角色或没有网络角色会话。</summary>
    private long _localRoleConnectionGeneration;

    /// <summary>本地角色真正进入 SceneWorld 后触发，事件载荷为本次成功创建的强类型玩家实体。</summary>
    public event Action<PlayerEntity> LocalRoleLoadCompleted;

    /// <summary>当前主角表现控制器，与纯数据角色模型保持分离。</summary>
    [HideInInspector] public RoleCtrlBase roleCtrlBase;
    #endregion

    #region 生命周期与退出清理
    /// <summary>初始化全局客户端模块。</summary>
    private void Awake() {
        Instance = this;

        // 所有技能共用一个计时器；每次施法只登记回调，由 Global 每帧推进一次时间。
        MTimerMgr.singleton.Init();

        DontDestroyOnLoad(this);
        _package = YooAssets.GetPackage("DefaultPackage");
        // 资源包安装后一次补启动此前持有的表现资产，冷技能不再在 Update 中反复查询。
        SkillRuntimePresentationCache.NotifyResourcePackageReady();
        MNetClient.Instance.Init();
        PlayStateMgr.Instance.init();
        LubanMgr.Instance.Init();
        // 先读取本次运行使用的施法规则，再允许创建角色；缺失规则时按禁止施法处理。
        SkillCastingRulesCatalog.Initialize();
        // 先读完角色状态之间的允许关系，角色创建后才能据此判断是否能切换动作或施法。
        StateExclusionManager.singleton.Initialize();
        BeginAnimationPreload();
    }

    /// <summary>推进动画资源缓存、唯一 RO 时间轴时钟和客户端角色模型组件。</summary>
    private void Update()
    {
        MAnimationMgr.singleton.UpdateCache();
        UpdateAnimationPreload();
        UpdatePendingLocalRoleLoad();
        if (SceneWorld == null)
        {
            return;
        }
        // 当前 Unity 帧间隔换算为实体系统使用的毫秒整数。
        int deltaMilliseconds = Mathf.RoundToInt(Time.deltaTime * 1000f);
        // 与技能累计时钟使用同一整数毫秒来源，角色数量与当前/旧技能数量均不加速时间轴。
        MTimerMgr.singleton.Update(deltaMilliseconds * 0.001f);
        SceneWorld.Update(deltaMilliseconds);
    }

    /// <summary>在 Unity 帧末唯一驱动实体状态机和模型动画刷新。</summary>
    private void LateUpdate()
    {
        if (SceneWorld == null)
        {
            return;
        }
        // 当前 Unity 帧间隔换算为实体帧末更新使用的毫秒整数。
        int deltaMilliseconds = Mathf.RoundToInt(Time.deltaTime * 1000f);
        SceneWorld.LateUpdate(deltaMilliseconds);
    }

    /// <summary>Global 被销毁时兜底释放场景实体和全部动画 YooAsset 句柄。</summary>
    private void OnDestroy()
    {
        if (!ReferenceEquals(Instance, this))
        {
            return;
        }

        ReleaseRuntimeResources();
        Instance = null;
    }

    /// <summary>退出客户端时断开连接并清理角色运行时数据。</summary>
    private void OnApplicationQuit()
    {
        MNetClient.Instance.Disconnect();
        ReleaseRuntimeResources();
    }

    /// <summary>按实体后资源的顺序清理运行时状态；该方法可被退出和销毁流程重复安全调用。</summary>
    private void ReleaseRuntimeResources()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        ClearLocalRoleSession();
        SceneWorld = null;
        LocalRoleLoadCompleted = null;
        // 实体已逐个取消其技能票据，退出再解除任何剩余全局回调引用。
        MTimerMgr.singleton.Uninit();
        MAnimationMgr.singleton.Uninit();
    }
    #endregion

    #region 动画资源预加载
    /// <summary>
    /// 开始加载动画索引；只有成功后才建立场景实体世界，避免角色找不到动作配置。
    /// </summary>
    private void BeginAnimationPreload()
    {
        if (_isShuttingDown || _isAnimationPreloadRequested ||
            MAnimationMgr.singleton.IsPreloaded)
        {
            return;
        }

        if (_package == null)
        {
            Debug.LogError("默认 YooAsset 资源包不存在，动画索引将在稍后重试。");
            _nextAnimationPreloadRetryTime = Time.unscaledTime +
                AnimationPreloadRetryIntervalSeconds;
            return;
        }

        _isAnimationPreloadRequested = true;
        MAnimationMgr.singleton.Preload(OnAnimationPreloadCompleted);
    }

    /// <summary>
    /// 动画索引成功时创建 SceneWorld 并尝试装载暂存角色；失败时记录下次重试时间。
    /// </summary>
    /// <param name="isSucceed">二维动画索引是否已完整解析并写入 MAnimationMgr。</param>
    private void OnAnimationPreloadCompleted(bool isSucceed)
    {
        _isAnimationPreloadRequested = false;
        if (_isShuttingDown)
        {
            return;
        }

        if (isSucceed)
        {
            if (SceneWorld == null)
            {
                SceneWorld = new SceneWorldSystem();
            }
            TryConsumePendingLocalRole();
            return;
        }

        Debug.LogError("动画索引加载失败，场景实体系统保持阻断并将在稍后重试。");
        _nextAnimationPreloadRetryTime = Time.unscaledTime +
            AnimationPreloadRetryIntervalSeconds;
    }

    /// <summary>在动画索引失败或资源包暂不可用时，按固定间隔重新发起加载。</summary>
    private void UpdateAnimationPreload()
    {
        if (_isShuttingDown || SceneWorld != null ||
            _isAnimationPreloadRequested)
        {
            return;
        }

        if (Time.unscaledTime < _nextAnimationPreloadRetryTime)
        {
            return;
        }

        BeginAnimationPreload();
    }
    #endregion

    #region 本地角色装载
    /// <summary>
    /// 收到开始游戏成功回包后提交角色数据；场景世界没准备好就暂存，准备好后立即尝试创建角色。
    /// 相同角色的重复回包不会重新创建本地玩家实体。
    /// </summary>
    /// <param name="roleInfo">服务器返回的完整本地角色信息，方法内部会克隆以隔离协议对象后续修改。</param>
    /// <returns>本次提交被拒绝、进入排队、立即加载或命中已加载角色的明确状态。</returns>
    public LocalRoleLoadSubmitResult SubmitLocalRoleLoad(MainRoleInfo roleInfo)
    {
        if (_isShuttingDown || !IsValidMainRoleInfo(roleInfo))
        {
            Debug.LogError("开始游戏角色数据无效或客户端正在退出，拒绝加载本地角色。");
            return LocalRoleLoadSubmitResult.Rejected;
        }

        if (_loginInfo != null && _loginInfo.AccountId > 0 &&
            roleInfo.AccountId != _loginInfo.AccountId)
        {
            Debug.LogError("开始游戏角色账号与当前登录账号不一致，拒绝加载旧账号角色。");
            return LocalRoleLoadSubmitResult.Rejected;
        }

        // 只有底层连接成功的真实回包才绑定连接代次；连接尝试或离线测试固定使用零代次。
        long connectionGeneration = 0;
        if (MNetClient.Instance.IsConnected)
        {
            connectionGeneration = MNetClient.Instance.CurrentConnectionGeneration;
        }

        if (SceneWorld != null && IsSameLoadedRole(roleInfo))
        {
            _pendingMainRoleInfo = null;
            _localRoleConnectionGeneration = connectionGeneration;
            NotifyLocalRoleLoadCompleted(SceneWorld.LocalRole);
            return LocalRoleLoadSubmitResult.AlreadyLoaded;
        }

        // 克隆协议对象，确保异步门控期间暂存数据不受网络回包对象生命周期影响。
        MainRoleInfo pendingRoleInfo = roleInfo.Clone();
        _pendingMainRoleInfo = pendingRoleInfo;
        _localRoleConnectionGeneration = connectionGeneration;
        _nextPendingRoleLoadRetryTime = 0f;
        if (SceneWorld == null)
        {
            Debug.Log("动画索引尚未就绪，开始游戏角色数据已排队等待场景实体世界。");
            return LocalRoleLoadSubmitResult.Queued;
        }

        if (TryConsumePendingLocalRole())
        {
            return LocalRoleLoadSubmitResult.Loaded;
        }

        return LocalRoleLoadSubmitResult.Queued;
    }

    /// <summary>用最新暂存数据创建本地角色；创建失败时保留数据，等到重试时间再尝试。</summary>
    /// <returns>本次是否成功创建本地玩家并发出完成事件。</returns>
    private bool TryConsumePendingLocalRole()
    {
        if (_isShuttingDown || SceneWorld == null || _pendingMainRoleInfo == null)
        {
            return false;
        }

        // 只有绑定了真实网络代次的角色才校验连接一致性；零代次测试角色不依赖网络连接。
        long currentConnectionGeneration =
            MNetClient.Instance.CurrentConnectionGeneration;
        if (_localRoleConnectionGeneration > 0 &&
            currentConnectionGeneration != _localRoleConnectionGeneration)
        {
            return false;
        }

        // 本次准备消费的强类型角色副本；成功前仍由待处理字段持有，避免创建失败丢失回包。
        MainRoleInfo pendingRoleInfo = _pendingMainRoleInfo;
        // 场景实体世界返回的唯一新本地玩家，用于确认加载成功并通知 UI 流程。
        PlayerEntity loadedLocalRole = SceneWorld.LoadLocalRole(pendingRoleInfo);
        if (loadedLocalRole == null)
        {
            _nextPendingRoleLoadRetryTime = Time.unscaledTime +
                AnimationPreloadRetryIntervalSeconds;
            Debug.LogError("本地角色创建失败，已保留开始游戏数据并将在稍后重试。");
            return false;
        }

        _pendingMainRoleInfo = null;
        _nextPendingRoleLoadRetryTime = 0f;
        NotifyLocalRoleLoadCompleted(loadedLocalRole);
        return true;
    }

    /// <summary>通知订阅者本地角色已经创建完成，主城管理器可继续加载模型。</summary>
    /// <param name="localRole">已完成入世界生命周期的玩家实体；为空时不触发事件。</param>
    private void NotifyLocalRoleLoadCompleted(PlayerEntity localRole)
    {
        if (localRole == null)
        {
            return;
        }
        // 当前完成事件快照，避免订阅方在回调内解绑时影响本轮委托调用。
        Action<PlayerEntity> completedHandler = LocalRoleLoadCompleted;
        if (completedHandler != null)
        {
            completedHandler.Invoke(localRole);
        }
    }

    /// <summary>场景角色创建曾失败时按固定间隔重试，保证已接受的成功回包不会被吞掉。</summary>
    private void UpdatePendingLocalRoleLoad()
    {
        if (_pendingMainRoleInfo == null || SceneWorld == null ||
            Time.unscaledTime < _nextPendingRoleLoadRetryTime)
        {
            return;
        }
        TryConsumePendingLocalRole();
    }

    /// <summary>判断协议角色数据是否具备创建本地玩家所需的账号和正数角色 ID。</summary>
    /// <param name="roleInfo">待验证的开始游戏角色数据。</param>
    /// <returns>基础信息存在且角色 ID、账号 ID 均为正数时返回 true。</returns>
    private static bool IsValidMainRoleInfo(MainRoleInfo roleInfo)
    {
        if (roleInfo == null)
        {
            return false;
        }
        if (roleInfo.BaseInfo == null)
        {
            return false;
        }
        // 角色 ID 与账号 ID 均为正数，才具备创建本地玩家所需的有效账号角色。
        return roleInfo.BaseInfo.RoleId > 0 && roleInfo.AccountId > 0;
    }

    /// <summary>判断 SceneWorld 当前本地玩家是否就是本次回包指定的同一账号角色。</summary>
    /// <param name="roleInfo">用于比对账号 ID 和角色 ID 的服务器角色数据。</param>
    /// <returns>当前已加载角色与回包账号、角色 ID 均相同时返回 true。</returns>
    private bool IsSameLoadedRole(MainRoleInfo roleInfo)
    {
        if (SceneWorld == null)
        {
            return false;
        }
        if (SceneWorld.LocalRole == null)
        {
            return false;
        }
        // 当前本地玩家的角色数据与基础信息均已就位，才继续比对账号与角色 ID。
        if (SceneWorld.LocalRole.RoleInfo == null ||
            SceneWorld.LocalRole.BaseInfo == null)
        {
            return false;
        }
        return SceneWorld.LocalRole.RoleInfo.AccountId == roleInfo.AccountId &&
            SceneWorld.LocalRole.BaseInfo.RoleId == roleInfo.BaseInfo.RoleId;
    }

    /// <summary>判断登录信息赋值是否代表账号切换或退出，用于隔离旧账号待处理角色。</summary>
    /// <param name="previousLogin">赋值前的登录结果；首次登录时允许为空。</param>
    /// <param name="nextLogin">即将写入的登录结果；退出登录时允许为空。</param>
    /// <returns>已有账号被清除或账号 ID 发生变化时返回 true。</returns>
    private static bool HasLoginAccountChanged(LoginRet previousLogin,
        LoginRet nextLogin)
    {
        if (previousLogin == null)
        {
            return false;
        }
        if (nextLogin == null)
        {
            return true;
        }
        return previousLogin.AccountId != nextLogin.AccountId;
    }
    #endregion

    #region 本地角色会话清理
    /// <summary>无条件清除当前账号的待加载角色和已创建场景角色，供换号、主动断线和退出调用。</summary>
    public void ClearLocalRoleSession()
    {
        ClearLocalRoleSessionCore();
    }

    /// <summary>仅清理由指定失败连接代次拥有的角色会话，避免旧失败回调删除自动重连后的新角色。</summary>
    /// <param name="failedConnectionGeneration">已经失效的正数网络连接代次。</param>
    /// <returns>当前角色会话属于该失败代次并已完成清理时返回 true。</returns>
    public bool ClearLocalRoleSession(long failedConnectionGeneration)
    {
        if (failedConnectionGeneration <= 0 ||
            _localRoleConnectionGeneration != failedConnectionGeneration)
        {
            return false;
        }
        ClearLocalRoleSessionCore();
        return true;
    }

    /// <summary>按表现引用、待处理数据、场景实体的顺序清空本地角色会话。</summary>
    private void ClearLocalRoleSessionCore()
    {
        // 先清空表现控制器，随后 LocalRoleChanged(null) 的消费者只能观察到空跟踪目标。
        roleCtrlBase = null;
        _pendingMainRoleInfo = null;
        _nextPendingRoleLoadRetryTime = 0f;
        _localRoleConnectionGeneration = 0;
        if (SceneWorld != null)
        {
            SceneWorld.Clear();
        }
    }
    #endregion

    /// <summary>开始游戏角色数据提交后的处理状态，用于区分拒绝、排队、立即加载和重复回包。</summary>
    public enum LocalRoleLoadSubmitResult
    {
        /// <summary>角色数据无效、账号不匹配或全局生命周期已结束，提交未被接受。</summary>
        Rejected = 0,

        /// <summary>角色数据已暂存，等待动画索引或场景实体世界就绪后继续加载。</summary>
        Queued = 1,

        /// <summary>角色数据已经在本次提交中加载为本地玩家实体。</summary>
        Loaded = 2,

        /// <summary>相同角色已经存在于场景实体世界，本次重复回包未重新创建实体。</summary>
        AlreadyLoaded = 3
    }

}
