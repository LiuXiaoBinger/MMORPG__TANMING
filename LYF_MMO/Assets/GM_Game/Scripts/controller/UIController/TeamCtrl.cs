using System;
using Google.Protobuf;

/// <summary>
/// 收发组队协议：收到队伍与邀请通知后更新 TeamData，再通知界面；用户操作则组装请求交给网络层。
/// </summary>
public sealed class TeamCtrl : CtrlBase
{

    #region 字段、事件与属性
    /// <summary>队伍状态变化回调，参数为是否仍在队伍中。</summary>
    public event Action<bool> TeamInfoReceived;

    /// <summary>收到邀请或申请回调，无参数，界面据此显示同意与拒绝入口。</summary>
    public event Action InvitationReceived;

    /// <summary>服务器确认入场回调，携带实例、场景与出生点。</summary>
    public event Action<TeamEnterDungeonNtf> EnterDungeonReceived;

    /// <summary>是否已经注册协议，避免重复注册。</summary>
    private bool _registered;

    /// <summary>当前实际持有三条队伍下行协议的控制器实例，用于接管前先注销旧实例。</summary>
    private static TeamCtrl _activeInstance;
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>构造队伍协议控制器并立即注册下行协议。</summary>
    /// <param name="view">所属界面视图，仅用于满足 CtrlBase 分层。</param>
    public TeamCtrl(UIBase view) : base(view)
    {
        RegisterHandlers();
    }

    /// <summary>注册三条队伍下行协议到现有分发器。</summary>
    private void RegisterHandlers()
    {
        if (_registered)
        {
            return;
        }
        if (SocketDispatcher.Instance == null)
        {
            return;
        }
        // 分发器的 AddEventHandler 先注册者优先，已存在则静默忽略。
        // 主界面重建会新建本控制器，旧实例若不注销，队伍通知会一直投给已销毁的旧实例，
        // 表现为双方队伍界面不刷新、收到入场通知也不切场景。这里在注册前接管所有权。
        if (_activeInstance != null && _activeInstance != this)
        {
            _activeInstance.UnregisterHandlers();
        }
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_TeamInfoCode, OnTeamInfo);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_TeamInvatationCode, OnInvatation);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_TeamEnterDungeonCode, OnEnterDungeon);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_MonsterAttrUpdateCode, OnMonsterAttrUpdate);
        _activeInstance = this;
        _registered = true;
    }

    /// <summary>注销三条队伍下行协议。</summary>
    private void UnregisterHandlers()
    {
        if (!_registered)
        {
            return;
        }
        if (SocketDispatcher.Instance == null)
        {
            return;
        }
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_TeamInfoCode, OnTeamInfo);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_TeamInvatationCode, OnInvatation);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_TeamEnterDungeonCode, OnEnterDungeon);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_MonsterAttrUpdateCode, OnMonsterAttrUpdate);
        if (_activeInstance == this)
        {
            // 只有仍由本实例持有时才清空静态标记，避免误清后来接管的实例。
            _activeInstance = null;
        }
        _registered = false;
    }

    /// <summary>释放时注销协议并清空事件订阅。</summary>
    public override void Dispose()
    {
        UnregisterHandlers();
        TeamInfoReceived = null;
        InvitationReceived = null;
        EnterDungeonReceived = null;
    }
    #endregion

    /// <summary>处理服务器确认的副本入场通知，客户端据此才加载场景。</summary>
    /// <param name="data">TeamEnterDungeonNtf 二进制消息体。</param>
    private void OnEnterDungeon(ByteString data)
    {
        // 入场确认通知：携带场景ID、实例号与出生点，客户端收到后才允许加载副本场景。
        TeamEnterDungeonNtf notice = TeamEnterDungeonNtf.Parser.ParseFrom(data);
        TeamData.Instance.OnEnterScene(notice);
        if (EnterDungeonReceived != null)
        {
            EnterDungeonReceived(notice);
        }
    }

    /// <summary>将可靠下行的怪物血量交给当前副本状态。</summary>
    /// <param name="data">GameServer 发送的 MonsterAttrUpdateNtf 原始字节。</param>
    private void OnMonsterAttrUpdate(ByteString data)
    {
        TeamData.Instance.OnMonsterAttrUpdate(MonsterAttrUpdateNtf.Parser.ParseFrom(data));
    }

    #region 事件回调
    /// <summary>处理服务器下发的整包队伍状态。</summary>
    /// <param name="data">TeamInfoNtf 二进制消息体。</param>
    private void OnTeamInfo(ByteString data)
    {
        // 整包队伍快照：队伍ID、队长、成员列表与实例号都在这里，是客户端队伍状态的唯一权威来源。
        TeamInfoNtf notice = TeamInfoNtf.Parser.ParseFrom(data);
        TeamData.Instance.SetLocalRoleId(ResolveLocalRoleId());
        TeamData.Instance.ApplyTeamInfo(notice);
        if (TeamInfoReceived != null)
        {
            TeamInfoReceived(TeamData.Instance.IsInTeam);
        }
    }

    /// <summary>处理服务器下发的邀请或申请通知。</summary>
    /// <param name="data">TeamInvatationNtf 二进制消息体。</param>
    private void OnInvatation(ByteString data)
    {
        // 邀请或申请通知：写入 TeamData 的待处理槽位后，由界面决定展示同意/拒绝入口。
        TeamInvatationNtf notice = TeamInvatationNtf.Parser.ParseFrom(data);
        TeamData.Instance.ApplyNotice(notice, ResolveLocalRoleId());
        if (InvitationReceived != null)
        {
            InvitationReceived();
        }
    }
    #endregion

    #region 网络请求与消息
    /// <summary>
    /// 请求创建队伍，对应 RO TeamMgr.lua:43 RequestCreateTeam(targetId)。
    /// </summary>
    /// <param name="targetId">队伍要参与的玩法目标编号，调用方当前使用 1000。</param>
    /// <returns>请求已提交返回 true；未连接服务器返回 false。</returns>
    public bool RequestCreateTeam(int targetId)
    {
        // 创建队伍请求体：只填组队目标ID，序列化后随 CMD_CreateTeamCode 上行，服务端据此决定队伍类型。
        CreateTeamArg arg = new CreateTeamArg();
        arg.TargetId = targetId;
        return Send(NetDefine.CMD_CreateTeamCode, arg.ToByteString());
    }

    /// <summary>统一交给网络层发送；本方法返回值表示网络层是否接收请求，不代表服务器业务已成功。</summary>
    /// <param name="protoCode">协议号。</param>
    /// <param name="data">已序列化的协议体。</param>
    /// <returns>成功提交到网络层返回 true。</returns>
    private static bool Send(int protoCode, ByteString data)
    {
        if (MNetClient.Instance == null)
        {
            return false;
        }
        return MNetClient.Instance.Send(protoCode, data);
    }
    #endregion

    /// <summary>取当前场景世界中的本地主角角色ID，未进入世界时返回 0。</summary>
    /// <returns>本地主角角色ID；场景世界或本地主角未就绪时返回 0。</returns>
    private static ulong ResolveLocalRoleId()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return 0UL;
        }
        // 当前场景世界中的本地主角实体，为空说明客户端尚未进入世界，此时无法确定队伍身份。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole == null)
        {
            return 0UL;
        }
        // 主角实体ID，非正数代表实体尚未分配ID，与"未就绪"同样返回 0。
        int roleId = localRole.GetID();
        if (roleId <= 0)
        {
            return 0UL;
        }
        return (ulong)roleId;
    }

    #region 核心逻辑
    /// <summary>
    /// 邀请他人入队，对应 RO TeamMgr.lua:81 InviteJoinTeam(UserID, UserLevel)。
    /// 当前方法不读取等级参数；请求只携带被邀请角色的 ID。
    /// </summary>
    /// <param name="userId">被邀请角色ID。</param>
    /// <param name="userLevel">保留的角色等级参数，当前未使用，可传 0。</param>
    /// <returns>请求已提交返回 true。</returns>
    public bool InviteJoinTeam(ulong userId, int userLevel)
    {
        if (userId == 0UL)
        {
            return false;
        }
        // 邀请请求只携带被邀请角色 ID；userLevel 不参与本方法判断，也不随包发送。
        InviteJoinTeamArg arg = new InviteJoinTeamArg();
        arg.UserId = userId;
        return Send(NetDefine.CMD_InviteJoinTeamCode, arg.ToByteString());
    }

    /// <summary>
    /// 同意或拒绝入队邀请，对应 RO TeamMgr.lua:165 AcceptTeamInvatation()。
    /// 队长审批他人申请也复用本入口，通过 teamId 与 inviterId 区分来源。
    /// </summary>
    /// <param name="teamId">队伍ID。</param>
    /// <param name="inviterId">邀请人角色ID；审批申请时传申请人角色ID。</param>
    /// <param name="agree">true 同意，false 拒绝。</param>
    /// <returns>请求已提交返回 true。</returns>
    public bool AcceptTeamInvatation(ulong teamId, ulong inviterId, bool agree)
    {
        if (teamId == 0UL)
        {
            return false;
        }
        // 同意/拒绝请求体：teamId 定位队伍，inviterId 区分"邀请来源"与"申请来源"，agree 是本次最终选择。
        AcceptTeamInvatationArg arg = new AcceptTeamInvatationArg();
        arg.TeamId = teamId;
        arg.InviterId = inviterId;
        arg.Agree = agree;
        // 本地先清待处理通知，服务端回包后以 TeamInfoNtf 为准。
        if (agree)
        {
            TeamData.Instance.ClearInvitation();
        }
        else
        {
            TeamData.Instance.ClearInvitation();
            TeamData.Instance.ClearApplication();
        }
        return Send(NetDefine.CMD_AcceptTeamInvatationCode, arg.ToByteString());
    }

    /// <summary>
    /// 离队，对应 RO TeamMgr.lua:315 LeaveTeam()。
    /// </summary>
    /// <returns>请求已提交返回 true。</returns>
    public bool LeaveTeam()
    {
        // 离队请求体：RoleId 固定填 0，表示由服务端按当前会话身份解析离队者，避免客户端伪造他人离队。
        LeaveTeamArg arg = new LeaveTeamArg();
        arg.RoleId = 0UL;
        return Send(NetDefine.CMD_LeaveTeamCode, arg.ToByteString());
    }

    /// <summary>
    /// 按队伍ID申请加入，对应 RO TeamMgr.lua:413 BegInTeamByTeamId(cTeamId)。
    /// 申请后需队长同意才真正入队。
    /// </summary>
    /// <param name="teamId">目标队伍ID。</param>
    /// <returns>请求已提交返回 true。</returns>
    public bool BegInTeamByTeamId(ulong teamId)
    {
        if (teamId == 0UL)
        {
            return false;
        }
        // 按队伍ID申请入队的请求体：只携带目标队伍ID，申请人身份由服务端会话解析。
        BegInTeamByTeamIdArg arg = new BegInTeamByTeamIdArg();
        arg.TeamId = teamId;
        return Send(NetDefine.CMD_BegInTeamByTeamIdCode, arg.ToByteString());
    }

    /// <summary>
    /// 队长请求进入副本，对应 RO DungeonMgr.lua CaptainRequestEnterFBPtc。
    /// 只有服务器回 TeamEnterDungeonNtf 后客户端才加载场景。
    /// </summary>
    /// <param name="dungeonId">副本配置ID，魔神塔为 4060。</param>
    /// <returns>请求已提交返回 true。</returns>
    public bool CaptainRequestEnterFBPtc(int dungeonId)
    {
        // 队长进副本请求体：DungeonId 为副本配置ID（魔神塔 4060）；ChapterId 与 DungeonsDegree 固定 0，
        // 当前客户端没有选择章节和难度，两个字段均发送 0。
        CaptainRequestEnterFBArg arg = new CaptainRequestEnterFBArg();
        arg.DungeonId = dungeonId;
        arg.ChapterId = 0;
        arg.DungeonsDegree = 0;
        return Send(NetDefine.CMD_CaptainRequestEnterFBCode, arg.ToByteString());
    }
    #endregion
}
