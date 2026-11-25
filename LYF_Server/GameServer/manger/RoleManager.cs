using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf;

/// <summary>
/// 在线角色模块。
/// 参考 roserver：RoleManager 负责角色状态、附近角色同步和角色固定帧逻辑。
/// </summary>
public class RoleManager
{
    /// <summary>角色管理器单例实例。</summary>
    private static readonly RoleManager _instance = new RoleManager();

    /// <summary>角色模块单例，所有角色状态只由逻辑线程维护。</summary>
    public static RoleManager Instance
    {
        get { return _instance; }
    }

    #region 字段与常量
    /// <summary>主线程维护的角色 ID 到在线角色索引；网络线程只能通过 GameCommand 投递请求。</summary>
    private readonly Dictionary<int, OnlineRole> m_onlineRoleMap = new Dictionary<int, OnlineRole>();

    /// <summary>复合会话键到角色 ID 的索引，用于校验客户端会话归属。</summary>
    private readonly Dictionary<long, int> m_roleIdBySessionMap = new Dictionary<long, int>();

    /// <summary>重连时暂存的新角色，待旧实例完成保存后再激活。</summary>
    private readonly Dictionary<int, OnlineRole> m_pendingRoleMap = new Dictionary<int, OnlineRole>();

    /// <summary>待激活重连角色的原 EnterWorld 请求，激活后只重放一次。</summary>
    private readonly Dictionary<int, PendingEnterWorldRequest> _pendingEnterWorldRequests =
        new Dictionary<int, PendingEnterWorldRequest>();
    #endregion

    private RoleManager()
    {

    #region 初始化与更新
    }

    /// <summary>初始化角色管理器。</summary>
    public void OnInit()
    {
    }

    /// <summary>固定逻辑帧阶段：角色 Buff、冷却、定时器等逻辑统一从这里推进。</summary>
    public void Update(int deltaMilliseconds)
    {
        // 使用角色引用快照，允许更新过程中安全移除已释放角色。
        List<OnlineRole> onlineRoles = new List<OnlineRole>(m_onlineRoleMap.Values);
        for (int i = 0; i < onlineRoles.Count; i++)
        {
            OnlineRole role = onlineRoles[i];
            role.Update(deltaMilliseconds);
            // 角色已离线且无在途保存、组件无脏数据，满足可释放条件。
            bool offlineAndDisposable = role.State == OnlineRoleState.Offline && role.CanDispose();
            // 主角色信息及其基础信息完整，可据此定位并注销角色。
            bool hasDisposableMainRole = role.mainRoleInfo != null && role.mainRoleInfo.BaseInfo != null;
            if (offlineAndDisposable && hasDisposableMainRole)
            {
                int roleId = role.mainRoleInfo.BaseInfo.RoleId;
                role.Dispose();
                OnlineRole registeredRole;
                if (m_onlineRoleMap.TryGetValue(roleId, out registeredRole) &&
                    object.ReferenceEquals(registeredRole, role))
                {
                    m_onlineRoleMap.Remove(roleId);
                    long sessionKey = CreateSessionKey(role.GateSessionId, role.UnitySessionId);
                    int mappedRoleId;
                    if (m_roleIdBySessionMap.TryGetValue(sessionKey, out mappedRoleId) &&
                        mappedRoleId == roleId)
                    {
                        m_roleIdBySessionMap.Remove(sessionKey);
                    }
                }
            }
        }

    #region 角色进入与移除
    #endregion
        ProcessPendingRoles();
    }

    /// <summary>添加在线角色并建立角色 ID 与会话 ID 的双向索引。</summary>
    /// <param name="roleID">角色 ID。</param>
    /// <param name="onlineRole">待加入的在线角色实例。</param>
    public void AddOnlineRole(int roleID, OnlineRole onlineRole)
    {
        // 登录成功或重连时替换旧角色，并同步清理两个索引中的旧映射。
        if (roleID <= 0 || onlineRole == null)
        {
            return;
        }

        OnlineRole oldRole;
        if (m_onlineRoleMap.TryGetValue(roleID, out oldRole))
        {
            // 旧实例有待保存数据时先进入离线保存，不能被重连对象直接覆盖。
            oldRole.BeginOffline();
            m_roleIdBySessionMap.Remove(CreateSessionKey(oldRole.GateSessionId, oldRole.UnitySessionId));
            if (!oldRole.CanDispose())
            {
                OnlineRole replacedPendingRole;
                if (m_pendingRoleMap.TryGetValue(roleID, out replacedPendingRole))
                {
                    replacedPendingRole.Dispose();
                    _pendingEnterWorldRequests.Remove(roleID);
                }
                m_pendingRoleMap[roleID] = onlineRole;
                LogMsg.Info("角色重连等待旧实例保存完成，已暂存新会话，角色=" + roleID, LogMsgType.Warn);
                return;
            }
            oldRole.Dispose();
            m_onlineRoleMap.Remove(roleID);
        }

        long sessionKey = CreateSessionKey(onlineRole.GateSessionId, onlineRole.UnitySessionId);
        int mappedRoleID;
        if (m_roleIdBySessionMap.TryGetValue(sessionKey, out mappedRoleID) && mappedRoleID != roleID)
        {
            // 同一复合会话只能绑定一个角色；替换前完整释放旧角色并清理两个索引。
            OnlineRole mappedRole;
            if (m_onlineRoleMap.TryGetValue(mappedRoleID, out mappedRole))
            {
                mappedRole.BeginOffline();
                m_roleIdBySessionMap.Remove(CreateSessionKey(mappedRole.GateSessionId, mappedRole.UnitySessionId));
                if (!mappedRole.CanDispose())
                {
                    OnlineRole replacedPendingRole;
                    if (m_pendingRoleMap.TryGetValue(roleID, out replacedPendingRole))
                    {
                        replacedPendingRole.Dispose();
                        _pendingEnterWorldRequests.Remove(roleID);
                    }
                    m_pendingRoleMap[roleID] = onlineRole;
                    LogMsg.Info("会话冲突角色等待保存完成，已暂存新会话，角色=" + roleID, LogMsgType.Warn);
                    return;
                }
                mappedRole.Dispose();
            }

            // 即使主索引已缺失，也要删除冲突会话键，避免残留映射指向不存在角色。
            m_roleIdBySessionMap.Remove(sessionKey);
            m_onlineRoleMap.Remove(mappedRoleID);
        }

        ActivateOnlineRole(roleID, onlineRole);
    }

    /// <summary>移除在线角色；有未完成持久化时保留实例等待 ACK。</summary>
    /// <param name="roleID">待移除的角色 ID。</param>
    /// <returns>角色已找到并进入移除流程时返回 true。</returns>
    public bool RemoveOnlineRole(int roleID)
    {
        // 下线必须同时删除角色索引和会话索引，防止残留会话继续操作角色。
        OnlineRole pendingRole;
        if (m_pendingRoleMap.TryGetValue(roleID, out pendingRole))
        {
            pendingRole.Dispose();
            m_pendingRoleMap.Remove(roleID);
            _pendingEnterWorldRequests.Remove(roleID);
        }

        OnlineRole onlineRole;
        if (!m_onlineRoleMap.TryGetValue(roleID, out onlineRole))
        {
            return pendingRole != null;
        }

        // 下线后保留角色对象直到数据库 ACK，避免发送后立即释放造成脏数据永久丢失。
        onlineRole.BeginOffline();
        m_roleIdBySessionMap.Remove(CreateSessionKey(onlineRole.GateSessionId, onlineRole.UnitySessionId));
        if (onlineRole.CanDispose())
        {
            onlineRole.Dispose();
            return m_onlineRoleMap.Remove(roleID);
        }
        return true;
    }

    /// <summary>网关连接断开时移除该连接下的全部角色，并先执行强制保存。</summary>
    public void HandleGateSessionDisconnected(int gateSessionId)
    {
        if (gateSessionId <= 0)
        {
            return;
        }

        List<int> roleIds = new List<int>();
        List<KeyValuePair<int, OnlineRole>> onlineRoleEntries =
            new List<KeyValuePair<int, OnlineRole>>(m_onlineRoleMap);
        for (int i = 0; i < onlineRoleEntries.Count; i++)
        {
            KeyValuePair<int, OnlineRole> entry = onlineRoleEntries[i];
            if (entry.Value != null && entry.Value.GateSessionId == gateSessionId)
            {
                roleIds.Add(entry.Key);
            }
        }

        for (int i = 0; i < roleIds.Count; i++)
        {
            RemoveOnlineRole(roleIds[i]);
        }

        // 待激活重连角色尚未进入在线索引，也必须按断开的 Gate 会话清理。
        List<KeyValuePair<int, OnlineRole>> pendingEntries =
            new List<KeyValuePair<int, OnlineRole>>(m_pendingRoleMap);
        for (int index = 0; index < pendingEntries.Count; index++)
        {
            // 当前项是等待旧角色落库后激活的新会话角色。
            KeyValuePair<int, OnlineRole> pending = pendingEntries[index];
            if (pending.Value != null && pending.Value.GateSessionId == gateSessionId)
            {
                pending.Value.Dispose();
                m_pendingRoleMap.Remove(pending.Key);
                _pendingEnterWorldRequests.Remove(pending.Key);
            }
        }
    }

    /// <summary>旧角色保存完成后激活暂存的新会话，统一走正常登录初始化流程。</summary>
    private void ProcessPendingRoles()
    {
        // 待激活角色可能在处理过程中修改字典，因此先复制键值对快照。
        List<KeyValuePair<int, OnlineRole>> pendingRoles =
            new List<KeyValuePair<int, OnlineRole>>(m_pendingRoleMap);
        for (int i = 0; i < pendingRoles.Count; i++)
        {
            KeyValuePair<int, OnlineRole> pending = pendingRoles[i];
            OnlineRole oldRole;
            if (m_onlineRoleMap.TryGetValue(pending.Key, out oldRole))
            {
                if (!oldRole.CanDispose())
                {
                    continue;
                }

                oldRole.Dispose();
                m_onlineRoleMap.Remove(pending.Key);
            }

            m_pendingRoleMap.Remove(pending.Key);
            ActivateOnlineRole(pending.Key, pending.Value);

            // 旧实例保存完成后，使用已激活新会话重放唯一一次进入世界请求。
            PendingEnterWorldRequest pendingEnterWorld;
            if (_pendingEnterWorldRequests.TryGetValue(pending.Key, out pendingEnterWorld))
            {
                _pendingEnterWorldRequests.Remove(pending.Key);
                if (pendingEnterWorld.Package != null &&
                    GetOnlineRoleBySession(pendingEnterWorld.Package.GateSessionId,
                        pendingEnterWorld.Package.UnitySessionId) == pending.Value)
                {
                    HandleClientEnterWorld(pendingEnterWorld.Transport, pendingEnterWorld.Package);
                }
            }
        }
    }

    /// <summary>完成角色组件初始化和角色/会话双索引注册。</summary>
    private void ActivateOnlineRole(int roleID, OnlineRole onlineRole)
    {
        onlineRole.InitSerializes();
        onlineRole.AfterLoad();
        onlineRole.State = OnlineRoleState.Online;

    #region 查询
    #endregion
        m_onlineRoleMap[roleID] = onlineRole;
        m_roleIdBySessionMap[CreateSessionKey(onlineRole.GateSessionId, onlineRole.UnitySessionId)] = roleID;
        onlineRole.AfterAddedToRole();
    }

    /// <summary>按角色 ID 获取在线角色实例。</summary>
    /// <param name="roleID">角色 ID。</param>
    /// <returns>在线角色实例；角色不存在时返回 null。</returns>
    public OnlineRole GetOnlineRole(int roleID)
    {
        // 角色 ID 索引只在逻辑线程访问，找不到角色时返回 null。
        OnlineRole onlineRole;
        if (m_onlineRoleMap.TryGetValue(roleID, out onlineRole))
        {
            return onlineRole;
        }

        return null;
    }

    /// <summary>按网关会话和 Unity 会话获取在线角色实例。</summary>
    /// <param name="gateSessionID">网关连接会话 ID。</param>
    /// <param name="unitySessionID">客户端 Unity 会话 ID。</param>
    /// <returns>绑定到指定会话的在线角色；未找到时返回 null。</returns>
    public OnlineRole GetOnlineRoleBySession(int gateSessionID, int unitySessionID)
    {
        // 先查会话索引再查角色索引，避免遍历所有在线角色。
        int roleID;
        if (m_roleIdBySessionMap.TryGetValue(CreateSessionKey(gateSessionID, unitySessionID), out roleID))
        {
            return GetOnlineRole(roleID);
        }

        return null;
    }

    /// <summary>获取在线角色索引快照，快照中的角色对象仍为可修改的原引用。</summary>

    #region 客户端消息处理
    #endregion
    /// <returns>角色 ID 到在线角色实例的字典副本。</returns>
    public Dictionary<int, OnlineRole> GetOnlineRoleMap()
    {
        // 返回快照而非内部字典，广播遍历期间不会破坏索引结构。
        return new Dictionary<int, OnlineRole>(m_onlineRoleMap);
    }

    /// <summary>处理客户端进入世界请求，并同步附近在线角色。</summary>
    /// <param name="serverBase">接收请求的游戏服网络连接。</param>
    /// <param name="package">客户端请求数据包。</param>
    public void HandleClientEnterWorld(BaseLink serverBase, BasePackage package)
    {
        // 解析前先校验传输对象，避免无效数据触发协议解析异常。
        if (serverBase == null || package == null || package.Data == null)
        {
            return;
        }

        // 当前会话提交的进入世界参数，用于校验角色并同步附近玩家。
        EnterWroldReq request = EnterWroldReq.Parser.ParseFrom(package.Data);

        if (request == null)
        {
            return;
        }

        OnlineRole currentRole = GetOnlineRoleBySession(package.GateSessionId, package.UnitySessionId);
        if (currentRole == null)
        {
            // 旧在线实例待保存时，新会话的首个 EnterWorld 不能丢弃，否则 11080 永远不会装载。
            OnlineRole pendingRole;
            // 该角色 ID 是否命中待处理表。
            bool foundPending = m_pendingRoleMap.TryGetValue(request.RoleId, out pendingRole);
            // 待处理角色非空且网关、客户端会话标识完全匹配（pendingRole 判空前置防 NRE）。
            bool pendingSessionMatch = pendingRole != null &&
                pendingRole.GateSessionId == package.GateSessionId &&
                pendingRole.UnitySessionId == package.UnitySessionId;
            if (foundPending && pendingSessionMatch)
            {
                _pendingEnterWorldRequests[request.RoleId] =
                    new PendingEnterWorldRequest(serverBase, package);
            }
            return;
        }
        if (currentRole.mainRoleInfo == null ||
            currentRole.mainRoleInfo.BaseInfo == null ||
            currentRole.mainRoleInfo.BaseInfo.RoleId != request.RoleId)
        {
            return;
        }

        // 会话与角色校验通过后先落出生坐标：实时移动已改走 KCP，入场前的移动上报不再有先后保证，
        // 必须在下面的附近广播与附近下发之前写入，否则会按原点筛玩家。
        // 旧客户端不携带该字段时保持中心服已有位置，不做覆盖。
        // 已在副本实例内的角色以服务器分配的出生点为准，客户端上报位置不得覆写。
        if (!string.IsNullOrEmpty(request.Pos) && currentRole.mainRoleInfo.BaseInfo.InstanceId == 0)
        {
            currentRole.mainRoleInfo.BaseInfo.Pos = request.Pos;
        }

        // 再次进入世界或切图前结束上一场景中的活动施法，CD 仍保持在线状态。
        RoleSkillComponent skillComponent;
        if (currentRole.TryGetComponent(out skillComponent))
        {
            skillComponent.InterruptActiveCast(true);
        }

        // 会话校验通过后再转发中心服，避免伪造角色请求进入后续流程。
        if (serverBase._client != null)
        {
            serverBase._client.Send(package);
        }

        // 进入世界后，先通知附近玩家，再将附近角色发送给当前玩家。
        BroadcastRoleEnterWorld(currentRole.mainRoleInfo);
        SendNearbyRoles(currentRole);
    }

    /// <summary>处理客户端背包扩容请求，并将成功结果转发中心服持久化。</summary>
    /// <param name="serverBase">接收请求的游戏服网络连接。</param>
    /// <param name="package">客户端请求数据包。</param>
    public void HandleClientOpenKnapsackGrid(BaseLink serverBase, BasePackage package)
    {
        // 背包扩容在主线程完成校验和状态修改，成功后分别回客户端并通知中心服持久化。
        // 本次扩容参数；协议包或载荷缺失时保持为空，沿用下方业务处理。
        OpenKnapsackGridReq request = null;
        if (package != null && package.Data != null)
        {
            request = OpenKnapsackGridReq.Parser.ParseFrom(package.Data);
        }

        if (serverBase == null || package == null)
        {
            return;
        }

        OpenKnapsackGridRet result = new OpenKnapsackGridRet();
        result.RoleId = 0;
        result.KnapsackType = (int)KnapsackType.RolePackPlain;
        if (request != null)
        {
            result.RoleId = request.RoleId;
            result.KnapsackType = request.KnapsackType;
        }
        if (request == null || request.RoleId <= 0 || request.OpenGridCount <= 0)
        {
            result.CmdCode = CmdCode.ReqParamError;
            serverBase.Send(package, NetDefine.CMD_OpenKnapsackGridCode, result.ToByteString());
            return;
        }

        OnlineRole role = GetOnlineRoleBySession(package.GateSessionId, package.UnitySessionId);
        // 在线角色、主角色信息或基础信息缺失时按参数错误返回。
        if (role == null || role.mainRoleInfo == null)
        {
            result.CmdCode = CmdCode.ReqParamError;
            serverBase.Send(package, NetDefine.CMD_OpenKnapsackGridCode, result.ToByteString());
            return;
        }
        // 基础信息缺失或角色 ID 与请求不符时按参数错误返回。
        if (role.mainRoleInfo.BaseInfo == null || role.mainRoleInfo.BaseInfo.RoleId != request.RoleId)
        {
            result.CmdCode = CmdCode.ReqParamError;
            serverBase.Send(package, NetDefine.CMD_OpenKnapsackGridCode, result.ToByteString());
            return;
        }

        BagExpansionTransition transition = new BagExpansionTransition(role);
        BagTransitionResult transitionResult = transition.UnlockGrid(
            (KnapsackType)request.KnapsackType, request.OpenGridCount);
        result.CmdCode = MapBagTransitionResult(transitionResult);
        RoItemComponent component = role.GetComponent<RoItemComponent>();
        if (component != null)
        {
            result.CurrentOpenedGridCount = component.GetOpenedGridCount((KnapsackType)request.KnapsackType);
        }

        serverBase.Send(package, NetDefine.CMD_OpenKnapsackGridCode, result.ToByteString());
        if (result.CmdCode != CmdCode.Succeed || serverBase._client == null)
        {
            return;
        }

        BasePackage persistencePackage = new BasePackage
        {
            UnitySessionId = package.UnitySessionId,
            GateSessionId = package.GateSessionId
        };
        SyncKnapsackGridCountReq persistenceRequest = new SyncKnapsackGridCountReq
        {
            RoleId = result.RoleId,
            KnapsackType = request.KnapsackType,
            OpenedGridCount = result.CurrentOpenedGridCount
        };
        serverBase._client.Send(
            persistencePackage,
            NetDefine.CMD_SyncKnapsackGridCountCode,
            persistenceRequest.ToByteString());
    }

    /// <summary>将背包事务结果转换为现有协议错误码。</summary>
    private static CmdCode MapBagTransitionResult(BagTransitionResult result)
    {
        switch (result)
        {
            case BagTransitionResult.Success:
                return CmdCode.Succeed;
            case BagTransitionResult.RoleNotFound:
                return CmdCode.RoleNotExist;
            case BagTransitionResult.KnapsackUnavailable:
                return CmdCode.ServerError;
            case BagTransitionResult.InvalidArgument:
            default:
                return CmdCode.ReqParamError;
        }
    }

    /// <summary>处理中心服返回的背包容量持久化结果。</summary>
    /// <param name="package">中心服返回数据包。</param>
    public static void HandleCenterSyncKnapsackGridCount(BasePackage package)
    {
        // 持久化结果仅用于记录失败；在线容量已由开格子事务更新。
        if (package == null || package.Data == null)
        {
            return;

    #region 广播与同步
    #endregion
        }

        OpenKnapsackGridRet result = OpenKnapsackGridRet.Parser.ParseFrom(package.Data);
        if (result.CmdCode != CmdCode.Succeed)
        {
            LogMsg.Info("背包开格子数量持久化失败: " + result, LogMsgType.Error);
        }
    }

    /// <summary>向施法者和同地图 100 米内的现有在线角色发送开始表现。</summary>
    /// <param name="caster">已通过会话校验的在线施法者。</param>
    /// <param name="notice">仅代表施法已接受、不代表命中或伤害的通知。</param>
    public void BroadcastSkillStart(OnlineRole caster, SkillCastStartNtf notice)
    {
        if (caster == null || notice == null)
        {
            return;
        }
        BroadcastSkillNotice(caster, NetDefine.CMD_SkillCastStartCode, notice.ToByteString());
    }

    /// <summary>向施法者和同地图附近角色发送自然结束或中断表现。</summary>
    /// <param name="caster">需要结束当前施法的在线角色。</param>
    /// <param name="notice">包含服务器施法 ID 和中断标记的结束通知。</param>
    public void BroadcastSkillEnd(OnlineRole caster, SkillCastEndNtf notice)
    {
        if (caster == null || notice == null)
        {
            return;
        }
        BroadcastSkillNotice(caster, NetDefine.CMD_SkillCastEndCode, notice.ToByteString());
    }

    /// <summary>通过现有附近角色快照转发技能表现，不把此范围当作权威命中范围。</summary>
    /// <param name="caster">通知的施法者，本人始终先收到一次通知。</param>
    /// <param name="protocolCode">11171 开始或 11173 结束协议号。</param>
    /// <param name="payload">protobuf 序列化边界上的强类型通知载荷。</param>
    private void BroadcastSkillNotice(OnlineRole caster, ushort protocolCode, ByteString payload)
    {
        caster.SendToClient(protocolCode, payload);
        if (caster.mainRoleInfo == null || caster.mainRoleInfo.BaseInfo == null)
        {
            return;
        }

        // 引用快照允许下线流程同时维护角色索引，仍修改原在线角色对象。
        List<OnlineRole> onlineRoles = SceneObservers(caster);
        for (int index = 0; index < onlineRoles.Count; index++)
        {
            // 只向现有同地图 100 米可见范围的其他在线角色转发表现事件。
            OnlineRole observer = onlineRoles[index];
            // 空对象、自身或非在线角色均不转发。
            if (observer == null || observer == caster || observer.State != OnlineRoleState.Online)
            {
                continue;
            }
            // 距离不在同地图 100 米范围内的角色不转发。
            if (!IsNearbyRole(observer, caster.mainRoleInfo))
            {
                continue;
            }
            observer.SendToClient(protocolCode, payload);
        }
    }

    /// <summary>在同图同实例角色跨过 100 米边界时双向通知进出视野，并向范围内的观察者同步移动。</summary>
    /// <param name="pRole">本次移动的原在线角色，其基础位置已由移动组件更新。</param>
    /// <param name="notice">客户端本段移动数据，角色 ID 由服务器填充。</param>
    /// <param name="previousPosition">本次获准移动前的世界坐标（米）；仅补发当前位置时省略。</param>
    public void SyncMove(OnlineRole pRole, SyncMoveNtf notice, Vector3? previousPosition = null)
    {
        // 移动者的当前权威资料，刚进入视野的观察者用它创建角色。
        RoleBaseInfo movingInfo = pRole.mainRoleInfo.BaseInfo;
        // 本次上报已接受的世界坐标，单位米。
        Vector3 endPosition = movingInfo.Pos.ToVector3();
        // 补发位置时起点与终点相同，不产生进出通知。
        Vector3 startPosition = previousPosition.GetValueOrDefault(endPosition);
        // 序列化一次后复用不可变字节；本人继续本地移动，不等待移动回包。
        ByteString payload = notice.ToByteString();
        // 使用现有索引的引用快照，保持角色对象和既有附近判断一致。
        List<OnlineRole> onlineRoles = SceneObservers(pRole);
        // 快照下标只在本次广播内有效。
        for (int index = 0; index < onlineRoles.Count; index++)
        {
            // 目标仍是正式索引中的原角色对象，用于读取当前会话与地图。
            OnlineRole observer = onlineRoles[index];
            if (observer == pRole || observer.State != OnlineRoleState.Online ||
                !IsInSameRoleScene(observer, pRole.mainRoleInfo))
            {
                continue;
            }
            // 同场景观察者的权威资料，供移动者创建或移除对方。
            RoleBaseInfo observerInfo = observer.mainRoleInfo.BaseInfo;
            // 用同一个观察者位置比较移动前后，双方同时走动时按游戏服处理顺序更新。
            Vector3 observerPosition = observerInfo.Pos.ToVector3();
            // 正好 100 米仍可见，连续留在同一侧不重复发送出现或离开。
            bool wasNearby = IsWithinSight(observerPosition, startPosition);
            if (IsWithinSight(observerPosition, endPosition))
            {
                if (!wasNearby)
                {
                    observer.SendToClient(NetDefine.CMD_SyncotherOnlineCode, movingInfo.ToByteString());
                    pRole.SendToClient(NetDefine.CMD_SyncotherOnlineCode, observerInfo.ToByteString());
                }
                observer.SendToClient(NetDefine.CMD_SyncMoveCode, payload);
            }
            else if (wasNearby)
            {
                observer.SendToClient(NetDefine.CMD_RoleLeaveCode, BuildRoleLeavePayload(movingInfo));
                pRole.SendToClient(NetDefine.CMD_RoleLeaveCode, BuildRoleLeavePayload(observerInfo));
            }
        }
    }

    /// <summary>
    /// 向角色离开前的真实观察者广播离开通知。
    /// 必须在改写该角色的 MapId/InstanceId/Pos 之前调用，否则附近过滤会用新位置算范围。
    /// 收件人来自现有在线角色索引，按同地图、同实例、100 米过滤，不限于同一队伍。
    /// </summary>
    /// <param name="leavingRole">即将离开当前场景位置的在线角色。</param>
    public void BroadcastRoleLeave(OnlineRole leavingRole)
    {
        if (leavingRole == null || leavingRole.mainRoleInfo == null ||
            leavingRole.mainRoleInfo.BaseInfo == null)
        {
            return;
        }
        // 离开者的基础信息，此时 MapId/InstanceId 仍是离开前的旧值，是本次过滤与通知内容的依据。
        RoleBaseInfo leavingInfo = leavingRole.mainRoleInfo.BaseInfo;
        // 已序列化的通知字节，整轮广播复用同一份，避免每个观察者重复序列化。
        ByteString payload = BuildRoleLeavePayload(leavingInfo);

        // 使用现有索引的引用快照，与移动、技能广播保持同一份收件人来源。
        // onlineRoles 是遍历用的副本，避免广播过程中索引被并发修改。
        List<OnlineRole> onlineRoles = SceneObservers(leavingRole);
        // index 为快照列表的零基下标，范围 [0, onlineRoles.Count)，仅用于本次广播。
        for (int index = 0; index < onlineRoles.Count; index++)
        {
            // 当前候选收件人；跳过自己、空项与已离线的角色，再按附近规则过滤。
            OnlineRole observer = onlineRoles[index];
            if (observer == null || observer == leavingRole ||
                observer.State != OnlineRoleState.Online)
            {
                continue;
            }
            if (!IsNearbyRole(observer, leavingRole.mainRoleInfo))
            {
                continue;
            }
            observer.SendToClient(NetDefine.CMD_RoleLeaveCode, payload);
        }
    }

    /// <summary>把指定角色的出现通知按当前权威位置补发给同场景附近角色。</summary>
    /// <param name="role">刚切换到新地图或新实例的在线角色。</param>
    public void BroadcastRoleEnterWorldByRole(OnlineRole role)
    {
        if (role == null || role.mainRoleInfo == null)
        {
            return;
        }
        BroadcastRoleEnterWorld(role.mainRoleInfo);
    }

    /// <summary>把当前场景附近的其他角色补发给指定角色，用于切场景后重建远端列表。</summary>
    /// <param name="role">需要接收附近角色列表的在线角色。</param>
    public void ResendNearbyRoles(OnlineRole role)
    {
        SendNearbyRoles(role);
    }

    private void BroadcastRoleEnterWorld(MainRoleInfo currentRole)
    {
        // 广播范围限定为同地图且距离不超过 100 的在线角色。
        List<OnlineRole> onlineRoles = SceneObservers(GetOnlineRole(currentRole.BaseInfo.RoleId));
        for (int i = 0; i < onlineRoles.Count; i++)
        {
            OnlineRole role = onlineRoles[i];
            if (!IsNearbyRole(role, currentRole) || role.mainRoleInfo.BaseInfo.RoleId == currentRole.BaseInfo.RoleId)
            {
                continue;
            }

            Session session = SessionMgr.Instance.GetSession(role.GateSessionId);
            if (session != null)
            {
                session.Send(role.UnitySessionId, NetDefine.CMD_SyncotherOnlineCode,
                    currentRole.BaseInfo.ToByteString());
            }
        }
    }

    private void SendNearbyRoles(OnlineRole currentOnlineRole)
    {
        // 当前玩家进入世界后，补发已在线且在视野范围内的其他角色。
        if (currentOnlineRole == null || currentOnlineRole.mainRoleInfo == null ||
            currentOnlineRole.mainRoleInfo.BaseInfo == null)
        {
            return;
        }

        MainRoleInfo currentRole = currentOnlineRole.mainRoleInfo;
        // 通过角色/会话双索引确认当前玩家仍绑定此会话，避免使用其他请求的传输对象串包。
        OnlineRole sessionRole = GetOnlineRoleBySession(
            currentOnlineRole.GateSessionId,
            currentOnlineRole.UnitySessionId);
        // 会话绑定的在线角色或其主角色信息缺失时放弃补发。
        if (sessionRole == null || sessionRole.mainRoleInfo == null)
        {
            return;
        }
        // 基础信息缺失或会话角色 ID 与当前角色不符时放弃补发。
        if (sessionRole.mainRoleInfo.BaseInfo == null ||
            sessionRole.mainRoleInfo.BaseInfo.RoleId != currentRole.BaseInfo.RoleId)
        {
            return;
        }

        Session targetSession = SessionMgr.Instance.GetSession(sessionRole.GateSessionId);
        if (targetSession == null)
        {
            return;
        }

        List<OnlineRole> onlineRoles = SceneObservers(currentOnlineRole);
        for (int i = 0; i < onlineRoles.Count; i++)
        {
            OnlineRole role = onlineRoles[i];
            if (!IsNearbyRole(role, currentRole) || role.mainRoleInfo.BaseInfo.RoleId == currentRole.BaseInfo.RoleId)
            {
                continue;
            }

            // Session.Send(int, ...) 每次创建独立 BasePackage，避免修改进入世界请求包。
            targetSession.Send(sessionRole.UnitySessionId, NetDefine.CMD_SyncotherOnlineCode,
                role.mainRoleInfo.BaseInfo.ToByteString());
        }
    }

    /// <summary>
    /// 判断两个在线角色是否处于同一地图、同一副本实例且相距不超过 100 米。
    /// 四个广播方法与进入世界补发都以此为准，公开为静态以便独立进程验证收件人范围。
    /// </summary>
    /// <param name="role">候选收件人角色。</param>
    /// <param name="currentRole">事件来源角色的主角色信息。</param>
    /// <returns>应收到该广播返回 true。</returns>
    public static bool IsNearbyRole(OnlineRole role, MainRoleInfo currentRole)
    {
        if (!IsInSameRoleScene(role, currentRole))
        {
            return false;
        }
        return IsWithinSight(role.mainRoleInfo.BaseInfo.Pos.ToVector3(), currentRole.BaseInfo.Pos.ToVector3());
    }

    /// <summary>按三维世界距离判断玩家是否在 100 米视野内，包含边界。</summary>
    /// <param name="observer">观察者的世界坐标，单位米。</param>
    /// <param name="subject">被观察者的世界坐标，单位米。</param>
    /// <returns>距离不超过 100 米时为真。</returns>
    private static bool IsWithinSight(Vector3 observer, Vector3 subject)
    {
        return Vector3.DistanceSquared(observer, subject) <= 100f * 100f;
    }

    /// <summary>自然走出视野和切换场景共用的 11213 离开通知。</summary>
    /// <param name="role">接收方需要移除的角色及其离开前的地图和实例。</param>
    /// <returns>可发给观察者的离开消息字节。</returns>
    private static ByteString BuildRoleLeavePayload(RoleBaseInfo role)
    {
        return new RoleLeaveNtf
        {
            RoleId = (ulong)role.RoleId,
            SceneId = role.MapId,
            InstanceId = (ulong)role.InstanceId
        }.ToByteString();
    }

    /// <summary>检查两个角色是否都具备基础资料并位于同一地图和副本实例。</summary>
    /// <param name="role">候选观察者。</param>
    /// <param name="currentRole">被观察角色的权威资料。</param>
    /// <returns>可以在同一场景内比较视野距离时为真。</returns>
    internal static bool IsInSameRoleScene(OnlineRole role, MainRoleInfo currentRole)
    {
        // 先过滤空对象和地图，再计算三维距离，避免无效坐标触发异常。
        // 角色一侧的主角色快照或基础信息缺失时按不邻近处理。
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return false;
        }
        // 参考角色一侧缺失时按不邻近处理。
        if (currentRole == null || currentRole.BaseInfo == null)
        {
            return false;
        }

        if (role.mainRoleInfo.ServerId != currentRole.ServerId || role.mainRoleInfo.BaseInfo.MapId != currentRole.BaseInfo.MapId)
        {
            return false;
        }
    #endregion
        if (currentRole.BaseInfo.InstanceId == 0 && role.PublicScene != null)
        {
            // 施法、移动和视野补发都按玩家实际所在的公共场景实例比较。
            OnlineRole source = Instance.GetOnlineRole(currentRole.BaseInfo.RoleId);
            if (source == null || source.PublicScene == null || role.PublicScene.SceneUid != source.PublicScene.SceneUid) return false;
        }
        // 同一地图内不同队伍分属不同副本实例，实例不同即互相不可见。
        return role.mainRoleInfo.BaseInfo.InstanceId == currentRole.BaseInfo.InstanceId;
    }

    /// <summary>公共场景遍历本线成员，单节点旧档与副本继续使用已有索引。</summary>
    /// <param name="role">视野事件所在的角色实例。</param>
    private List<OnlineRole> SceneObservers(OnlineRole role)
    {
        return SceneLineHandler.Observers(role) ?? new List<OnlineRole>(m_onlineRoleMap.Values);
    }

    /// <summary>离开公共场景时双方同时清除旧视野，客户端可直接复用地图资源。</summary>
    /// <param name="role">正在切换分线的在线角色。</param>
    internal void LeavePublicScene(OnlineRole role)
    {
        BroadcastRoleLeave(role);
        // 旧场景内需要从本人视野移除的角色。
        List<OnlineRole> others = SceneObservers(role);
        for (int index = 0; index < others.Count; index++)
        {
            // 离场前的旧实例仍有效，沿用既有位置判定。
            OnlineRole other = others[index];
            if (other != role && IsNearbyRole(other, role.mainRoleInfo))
                role.SendToClient(NetDefine.CMD_RoleLeaveCode, BuildRoleLeavePayload(other.mainRoleInfo.BaseInfo));
        }
    }

    /// <summary>已确认存档的公共角色交给目标宿主；保持区服队伍关系。</summary>
    /// <param name="role">刚完成持久化且已通知离场的角色。</param>
    internal void DetachForSceneChange(OnlineRole role)
    {
        m_roleIdBySessionMap.Remove(CreateSessionKey(role.GateSessionId, role.UnitySessionId));
        m_onlineRoleMap.Remove((int)role.GetID());
        role.Dispose();
    }
    /// <summary>把网关与客户端两个编号组合成一个字典键，区分不同网关上相同的客户端编号。</summary>
    /// <param name="gateSessionID">高 32 位保存的网关连接编号。</param>
    /// <param name="unitySessionID">低 32 位保存的客户端会话编号。</param>
    private static long CreateSessionKey(int gateSessionID, int unitySessionID)
    {
        // 将两个 32 位会话拼成一个 64 位键，避免不同网关的会话号冲突。
        return ((long)(uint)gateSessionID << 32) | (uint)unitySessionID;
    }
}
