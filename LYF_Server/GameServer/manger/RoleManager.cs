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

    private RoleManager()
    {
    }
    /// <summary>主线程维护的角色 ID 到在线角色索引；网络线程只能通过 GameCommand 投递请求。</summary>
    private readonly Dictionary<int, OnlineRole> m_onlineRoleMap = new Dictionary<int, OnlineRole>();
    /// <summary>复合会话键到角色 ID 的索引，用于校验客户端会话归属。</summary>
    private readonly Dictionary<long, int> m_roleIdBySessionMap = new Dictionary<long, int>();
    /// <summary>重连时暂存的新角色，待旧实例完成保存后再激活。</summary>
    private readonly Dictionary<int, OnlineRole> m_pendingRoleMap = new Dictionary<int, OnlineRole>();

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
            if (role.State == OnlineRoleState.Offline && role.CanDispose() &&
                role.mainRoleInfo != null && role.mainRoleInfo.BaseInfo != null)
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
        }
    }

    /// <summary>完成角色组件初始化和角色/会话双索引注册。</summary>
    private void ActivateOnlineRole(int roleID, OnlineRole onlineRole)
    {
        onlineRole.InitSerializes();
        onlineRole.AfterLoad();
        onlineRole.State = OnlineRoleState.Online;
        m_onlineRoleMap[roleID] = onlineRole;
        m_roleIdBySessionMap[CreateSessionKey(onlineRole.GateSessionId, onlineRole.UnitySessionId)] = roleID;
        onlineRole.AfterAddedToRole();
    }

    /// <summary>获取在线角色索引快照，快照中的角色对象仍为可修改的原引用。</summary>
    /// <returns>角色 ID 到在线角色实例的字典副本。</returns>
    public Dictionary<int, OnlineRole> GetOnlineRoleMap()
    {
        // 返回快照而非内部字典，广播遍历期间不会破坏索引结构。
        return new Dictionary<int, OnlineRole>(m_onlineRoleMap);
    }

    /// <summary>处理客户端进入世界请求，并同步附近在线角色。</summary>
    /// <param name="serverBase">接收请求的游戏服网络连接。</param>
    /// <param name="package">客户端请求数据包。</param>
    public void HandleClientEnterWorld(ServerBase serverBase, BasePackage package)
    {
        // 解析前先校验传输对象，避免无效数据触发协议解析异常。
        if (serverBase == null || package == null || package.Data == null)
        {
            return;
        }

        EnterWroldReq request;
        try
        {
            request = EnterWroldReq.Parser.ParseFrom(package.Data);
        }
        catch (InvalidProtocolBufferException)
        {
            return;
        }

        if (request == null)
        {
            return;
        }

        OnlineRole currentRole = GetOnlineRoleBySession(package.GateSessionId, package.UnitySessionId);
        if (currentRole == null || currentRole.mainRoleInfo == null ||
            currentRole.mainRoleInfo.BaseInfo == null ||
            currentRole.mainRoleInfo.BaseInfo.RoleId != request.RoleId)
        {
            return;
        }

        // 会话校验通过后再转发中心服，避免伪造角色请求进入后续流程。
        if (serverBase._client != null)
        {
            serverBase._client.SendData(package);
        }

        // 进入世界后，先通知附近玩家，再将附近角色发送给当前玩家。
        BroadcastRoleEnterWorld(currentRole.mainRoleInfo);
        SendNearbyRoles(currentRole);
    }

    /// <summary>处理客户端背包扩容请求，并将成功结果转发中心服持久化。</summary>
    /// <param name="serverBase">接收请求的游戏服网络连接。</param>
    /// <param name="package">客户端请求数据包。</param>
    public void HandleClientOpenKnapsackGrid(ServerBase serverBase, BasePackage package)
    {
        // 背包扩容在主线程完成校验和状态修改，成功后分别回客户端并通知中心服持久化。
        OpenKnapsackGridReq request = null;
        if (package != null && package.Data != null)
        {
            try
            {
                request = OpenKnapsackGridReq.Parser.ParseFrom(package.Data);
            }
            catch (InvalidProtocolBufferException)
            {
                request = null;
            }
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
            serverBase.SendData(package, NetDefine.CMD_OpenKnapsackGridCode, result.ToByteString());
            return;
        }

        OnlineRole role = GetOnlineRoleBySession(package.GateSessionId, package.UnitySessionId);
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null ||
            role.mainRoleInfo.BaseInfo.RoleId != request.RoleId)
        {
            result.CmdCode = CmdCode.ReqParamError;
            serverBase.SendData(package, NetDefine.CMD_OpenKnapsackGridCode, result.ToByteString());
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

        serverBase.SendData(package, NetDefine.CMD_OpenKnapsackGridCode, result.ToByteString());
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
        serverBase._client.SendData(
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
        }

        OpenKnapsackGridRet result = OpenKnapsackGridRet.Parser.ParseFrom(package.Data);
        if (result.CmdCode != CmdCode.Succeed)
        {
            LogMsg.Info("背包开格子数量持久化失败: " + result, LogMsgType.Error);
        }
    }

    private void BroadcastRoleEnterWorld(MainRoleInfo currentRole)
    {
        // 广播范围限定为同地图且距离不超过 100 的在线角色。
        List<OnlineRole> onlineRoles = new List<OnlineRole>(m_onlineRoleMap.Values);
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
                session.SendData(role.UnitySessionId, NetDefine.CMD_SyncotherOnlineCode,
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
        if (sessionRole == null || sessionRole.mainRoleInfo == null ||
            sessionRole.mainRoleInfo.BaseInfo == null ||
            sessionRole.mainRoleInfo.BaseInfo.RoleId != currentRole.BaseInfo.RoleId)
        {
            return;
        }

        Session targetSession = SessionMgr.Instance.GetSession(sessionRole.GateSessionId);
        if (targetSession == null)
        {
            return;
        }

        List<OnlineRole> onlineRoles = new List<OnlineRole>(m_onlineRoleMap.Values);
        for (int i = 0; i < onlineRoles.Count; i++)
        {
            OnlineRole role = onlineRoles[i];
            if (!IsNearbyRole(role, currentRole) || role.mainRoleInfo.BaseInfo.RoleId == currentRole.BaseInfo.RoleId)
            {
                continue;
            }

            // Session.SendData(int, ...) 每次创建独立 BasePackage，避免修改进入世界请求包。
            targetSession.SendData(sessionRole.UnitySessionId, NetDefine.CMD_SyncotherOnlineCode,
                role.mainRoleInfo.BaseInfo.ToByteString());
        }
    }

    private static bool IsNearbyRole(OnlineRole role, MainRoleInfo currentRole)
    {
        // 先过滤空对象和地图，再计算三维距离，避免无效坐标触发异常。
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null ||
            currentRole == null || currentRole.BaseInfo == null)
        {
            return false;
        }

        if (role.mainRoleInfo.BaseInfo.MapId != currentRole.BaseInfo.MapId)
        {
            return false;
        }

        return Vector3.Distance(
            role.mainRoleInfo.BaseInfo.Pos.ToVector3(),
            currentRole.BaseInfo.Pos.ToVector3()) <= 100f;
    }

    private static long CreateSessionKey(int gateSessionID, int unitySessionID)
    {
        // 将两个 32 位会话拼成一个 64 位键，避免不同网关的会话号冲突。
        return ((long)(uint)gateSessionID << 32) | (uint)unitySessionID;
    }
}
