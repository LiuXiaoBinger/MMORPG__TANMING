using System;
using Google.Protobuf;

/// <summary>
/// 队伍协议控制器，对应 RO TeamMgr.lua 的上下行分支。
/// 负责收发队伍协议：下行写入 TeamData 并向上层抛事件，上行统一从本类发出，数据类不发包。
/// </summary>
public sealed class TeamCtrl : CtrlBase
{
    /// <summary>队伍状态变化回调，参数为是否仍在队伍中。</summary>
    public event Action<bool> TeamInfoReceived;

    /// <summary>收到邀请或申请回调，无参数，界面据此显示同意与拒绝入口。</summary>
    public event Action InvitationReceived;

    /// <summary>服务器确认入场回调，携带实例、场景与出生点。</summary>
    public event Action<TeamEnterDungeonNtf> EnterDungeonReceived;

    /// <summary>是否已经注册协议，避免重复注册。</summary>
    private bool _registered;

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
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_TeamInfoCode, OnTeamInfo);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_TeamInvatationCode, OnInvatation);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_TeamEnterDungeonCode, OnEnterDungeon);
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
        _registered = false;
    }

    /// <summary>
    /// 请求创建队伍，对应 RO TeamMgr.lua:43 RequestCreateTeam(targetId)。
    /// </summary>
    /// <param name="targetId">组队目标ID，RO 缺省 1000。</param>
    /// <returns>请求已提交返回 true；未连接服务器返回 false。</returns>
    public bool RequestCreateTeam(int targetId)
    {
        // 创建队伍请求体：只填组队目标ID，序列化后随 CMD_CreateTeamCode 上行，服务端据此决定队伍类型。
        CreateTeamArg arg = new CreateTeamArg();
        arg.TargetId = targetId;
        return Send(NetDefine.CMD_CreateTeamCode, arg.ToByteString());
    }

    /// <summary>
    /// 邀请他人入队，对应 RO TeamMgr.lua:81 InviteJoinTeam(UserID, UserLevel)。
    /// 等级仅用于界面校验提示，服务端以会话身份为准。
    /// </summary>
    /// <param name="userId">被邀请角色ID。</param>
    /// <param name="userLevel">被邀请角色等级，未知名传 0。</param>
    /// <returns>请求已提交返回 true。</returns>
    public bool InviteJoinTeam(ulong userId, int userLevel)
    {
        if (userId == 0UL)
        {
            return false;
        }
        // 邀请入队请求体：只携带被邀请角色ID；userLevel 仅用于界面提示，等级与身份以服务端会话为准，不随包上传。
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
        // 表示章节与难度由副本配置表决定，客户端不上报，防止改包指定难度。
        CaptainRequestEnterFBArg arg = new CaptainRequestEnterFBArg();
        arg.DungeonId = dungeonId;
        arg.ChapterId = 0;
        arg.DungeonsDegree = 0;
        return Send(NetDefine.CMD_CaptainRequestEnterFBCode, arg.ToByteString());
    }

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

    /// <summary>处理服务器确认的副本入场通知，客户端据此才加载场景。</summary>
    /// <param name="data">TeamEnterDungeonNtf 二进制消息体。</param>
    private void OnEnterDungeon(ByteString data)
    {
        // 入场确认通知：携带场景ID、实例号与出生点，客户端收到后才允许加载副本场景。
        TeamEnterDungeonNtf notice = TeamEnterDungeonNtf.Parser.ParseFrom(data);
        if (EnterDungeonReceived != null)
        {
            EnterDungeonReceived(notice);
        }
    }

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

    /// <summary>统一发送入口，未连接时返回 false 且不抛异常。</summary>
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

    /// <summary>释放时注销协议并清空事件订阅。</summary>
    public override void Dispose()
    {
        UnregisterHandlers();
        TeamInfoReceived = null;
        InvitationReceived = null;
        EnterDungeonReceived = null;
    }
}
