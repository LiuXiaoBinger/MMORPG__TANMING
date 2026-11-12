using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>一个游戏分区的组队权威，管理成员、待处理邀请、申请和进本资格。</summary>
internal sealed class TeamManager
{
    /// <summary>现有客户端的队伍上限，包括队长。</summary>
    private const int MaxMember = 5;
    /// <summary>RO GlobalTableFight.TeamEnterDungeonConfirmTime 的默认确认时限，单位秒。</summary>
    private const int TeamEnterDungeonConfirmTime = 10;
    /// <summary>本分区的服务连接，所有玩家通知和场景指令由它投递给 GS。</summary>
    private readonly BaseLink _game;
    /// <summary>按队伍号保存存活队伍，最后一名成员离开时删除。</summary>
    private readonly Dictionary<ulong, Team> _teams = new Dictionary<ulong, Team>();
    /// <summary>按角色号查其当前队伍，角色最多在一支队伍中。</summary>
    private readonly Dictionary<ulong, Team> _roleTeams = new Dictionary<ulong, Team>();
    /// <summary>下一个队伍编号，0 留给无队伍。</summary>
    private ulong _nextTeamId = 1;
    /// <summary>当前游戏分区的下一个副本实例编号，0 表示未进本。</summary>
    private ulong _nextInstanceId = 1;

    /// <summary>队伍的成员和入队关系，不持有场景、角色实体或战斗对象。</summary>
    private sealed class Team
    {
        /// <summary>本分区内的队伍编号。</summary>
        public ulong Id;
        /// <summary>有权审批申请和发起进本的队长角色号。</summary>
        public ulong Captain;
        /// <summary>已授权进入的实例号；队伍在本内时不接受新成员。</summary>
        public ulong InstanceId;
        /// <summary>当前实例实际选择的副本配置号，改选其他副本时创建新的实例身份。</summary>
        public int DungeonId;
        /// <summary>当前实例的难度1至4；不同难度不能复用同一实例。</summary>
        public int DungeonsDegree;
        /// <summary>按入队顺序保存成员，原队长离开后首位继任。</summary>
        public readonly List<ulong> Members = new List<ulong>();
        /// <summary>被邀请人角色号到实际邀请人角色号的对应关系。</summary>
        public readonly Dictionary<ulong, ulong> Invitations = new Dictionary<ulong, ulong>();
        /// <summary>等待本队队长审批的申请人角色号。</summary>
        public readonly HashSet<ulong> Applications = new HashSet<ulong>();
        /// <summary>当前等待全员确认的副本，0 表示没有正在确认的入场。</summary>
        public int PendingDungeonId;
        /// <summary>等待全队确认的难度，与本轮副本目标一起保存和清除。</summary>
        public int PendingDungeonsDegree;
        /// <summary>当前确认的 UTC 截止时间，只在 PendingDungeonId 非零时使用。</summary>
        public DateTime ConfirmDeadline;
        /// <summary>本次已同意的角色，队长发起时即同意；完成或取消后清空。</summary>
        public readonly HashSet<ulong> PreparedMembers = new HashSet<ulong>();
    }

    /// <summary>创建当前游戏分区的组队管理。</summary>
    /// <param name="game">承接玩家通知及副本执行的游戏服连接。</param>
    public TeamManager(BaseLink game)
    {
        _game = game;
    }

    /// <summary>按原客户端协议处理已由游戏服会话识别的角色请求。</summary>
    /// <param name="request">包含真实发起人、昵称及原始组队协议。</param>
    public void Handle(MasterTeamRolePacket request)
    {
        // 业务载荷按内部封包保留的客户端协议直接解析。
        ByteString data = request.Packet.Data;
        switch (request.Packet.ProtoCode)
        {
            case NetDefine.CMD_CreateTeamCode:
                CreateTeamArg.Parser.ParseFrom(data);
                Create(request.RoleId);
                break;
            case NetDefine.CMD_InviteJoinTeamCode:
                Invite(request, InviteJoinTeamArg.Parser.ParseFrom(data).UserId);
                break;
            case NetDefine.CMD_AcceptTeamInvatationCode:
                Answer(request.RoleId, AcceptTeamInvatationArg.Parser.ParseFrom(data));
                break;
            case NetDefine.CMD_BegInTeamByTeamIdCode:
                Apply(request, BegInTeamByTeamIdArg.Parser.ParseFrom(data).TeamId);
                break;
            case NetDefine.CMD_LeaveTeamCode:
                // 允许旧客户端留空，显式传入的离队角色必须是会话本人。
                ulong leaving = LeaveTeamArg.Parser.ParseFrom(data).RoleId;
                if (leaving == 0 || leaving == request.RoleId)
                {
                    Leave(request.RoleId, true);
                }
                break;
            case NetDefine.PtcC2M_MemberReplyEnterFBPtc:
                MemberReplyEnterFB(request.RoleId, MemberReplyEnterFBData.Parser.ParseFrom(data).Type);
                break;
            case NetDefine.CMD_CaptainRequestEnterFBCode:
                // 沿用队长请求中的难度字段，场景与难度作为同一确认目标传递。
                CaptainRequestEnterFBArg entry = CaptainRequestEnterFBArg.Parser.ParseFrom(data);
                Enter(request.RoleId, entry.DungeonId, entry.DungeonsDegree);
                break;
        }
    }

    /// <summary>建立一人队伍，由创建者担任队长。</summary>
    /// <param name="roleId">创建者角色号。</param>
    private void Create(ulong roleId)
    {
        if (_roleTeams.ContainsKey(roleId)) return;
        // 队伍号随本分区连接的生命周期递增。
        Team team = new Team { Id = _nextTeamId++, Captain = roleId };
        _teams.Add(team.Id, team);
        Join(team, roleId);
    }

    /// <summary>为邀请人所在队记录邀请，并通知目标玩家。</summary>
    /// <param name="actor">真实邀请人及其昵称。</param>
    /// <param name="target">被邀请的角色号。</param>
    private void Invite(MasterTeamRolePacket actor, ulong target)
    {
        // 任一已有队员都可发起邀请，仍需被邀者同意。
        Team team;
        if (!_roleTeams.TryGetValue(actor.RoleId, out team))
        {
            Error(actor.RoleId, CmdCode.ReqParamError);
            return;
        }
        if (team.InstanceId != 0 || team.Members.Count >= MaxMember || _roleTeams.ContainsKey(target))
        {
            Error(actor.RoleId, CmdCode.ServerError);
            return;
        }
        team.Invitations[target] = actor.RoleId;
        NotifyInvite(team, actor, target);
    }

    /// <summary>记录主动入队申请并发给当前队长。</summary>
    /// <param name="actor">申请人身份与昵称。</param>
    /// <param name="teamId">要加入的队伍号。</param>
    private void Apply(MasterTeamRolePacket actor, ulong teamId)
    {
        // 保留客户端对无效申请的既有静默处理。
        Team team;
        if (!_teams.TryGetValue(teamId, out team) || _roleTeams.ContainsKey(actor.RoleId)) return;
        if (team.InstanceId != 0 || team.Members.Count >= MaxMember) return;
        team.Applications.Add(actor.RoleId);
        NotifyInvite(team, actor, team.Captain);
    }

    /// <summary>把加入邀请或申请按原客户端约定投递给目标玩家。</summary>
    /// <param name="team">该邀请或申请所指的队伍。</param>
    /// <param name="actor">发起邀请或申请的玩家。</param>
    /// <param name="target">接收弹窗的被邀请人或队长。</param>
    private void NotifyInvite(Team team, MasterTeamRolePacket actor, ulong target)
    {
        // 客户端通过队长号区分申请和普通邀请弹窗。
        TeamInvatationNtf notice = new TeamInvatationNtf
        {
            TeamId = team.Id, CaptainId = team.Captain, InviterId = actor.RoleId, InviterName = actor.Nickname
        };
        Deliver(target, NetDefine.CMD_TeamInvatationCode, notice.ToByteString());
    }

    /// <summary>根据已有邀请或申请决定是否把玩家加入权威名单。</summary>
    /// <param name="roleId">提交答复的真实角色。</param>
    /// <param name="answer">客户端回传的队伍、发起人及同意状态。</param>
    private void Answer(ulong roleId, AcceptTeamInvatationArg answer)
    {
        // 正在答复的队伍。
        Team team;
        if (!_teams.TryGetValue(answer.TeamId, out team))
        {
            if (answer.Agree) Error(roleId, CmdCode.ReqParamError);
            return;
        }
        // 普通邀请由本人入队，队长审批申请时由申请人入队。
        ulong joining = roleId;
        if (team.Applications.Contains(answer.InviterId))
        {
            if (team.Captain != roleId)
            {
                Error(roleId, CmdCode.ReqParamError);
                return;
            }
            joining = answer.InviterId;
            team.Applications.Remove(joining);
            if (!answer.Agree) return;
        }
        else
        {
            if (_roleTeams.ContainsKey(roleId))
            {
                Error(roleId, CmdCode.ServerError);
                return;
            }
            if (!answer.Agree)
            {
                team.Invitations.Remove(roleId);
                team.Applications.Remove(roleId);
                return;
            }
            // 用服务器保存的邀请人检查同意包，防止无邀请入队。
            ulong inviter;
            if (!team.Invitations.TryGetValue(roleId, out inviter) || inviter != answer.InviterId)
            {
                Error(roleId, CmdCode.ReqParamError);
                return;
            }
        }
        if (team.InstanceId != 0 || team.Members.Count >= MaxMember || _roleTeams.ContainsKey(joining))
        {
            Error(roleId, CmdCode.ServerError);
            return;
        }
        team.Invitations.Remove(joining);
        Join(team, joining);
    }

    /// <summary>更新名单及反向索引，并通知全部队员。</summary>
    /// <param name="team">接纳新成员的队伍。</param>
    /// <param name="roleId">已通过检查的成员角色号。</param>
    private void Join(Team team, ulong roleId)
    {
        CancelConfirmation(team, roleId, FailEnterFBReason.FailEnterFbSomeoneLeaveWaitingScene);
        team.Members.Add(roleId);
        _roleTeams.Add(roleId, team);
        NotifyRoster(team);
    }

    /// <summary>通知 GS 本次名单及队长，由持有角色实体的 GS 补齐界面展示字段。</summary>
    /// <param name="team">发生变更的队伍。</param>
    private void NotifyRoster(Team team)
    {
        // 使用现有队伍通知结构，不改变客户端协议。
        TeamInfoNtf notice = new TeamInfoNtf { TeamId = team.Id, CaptainId = team.Captain, MaxMember = MaxMember };
        // 玩家在客户端的排列顺序与权威名单一致。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的角色号。
            ulong id = team.Members[index];
            notice.Members.Add(new TeamMemberInfo { RoleId = id, IsCaptain = id == team.Captain, InstanceId = (int)team.InstanceId });
        }
        _game.Send(NetDefine.CMD_MasterTeamRoster, notice.ToByteString());
    }

    /// <summary>移除成员，转让队长或在最后一人离开时解散队伍。</summary>
    /// <param name="roleId">离开队伍的角色号。</param>
    /// <param name="notifySelf">主动退队时要求 GS 通知本人并处理回城；下线时 GS 已清理角色。</param>
    private void Leave(ulong roleId, bool notifySelf)
    {
        // 查找本人当前所在的权威队伍。
        Team team;
        if (!_roleTeams.TryGetValue(roleId, out team)) return;
        CancelConfirmation(team, roleId, FailEnterFBReason.FailEnterFbSomeoneLeaveWaitingScene);
        team.Members.Remove(roleId);
        _roleTeams.Remove(roleId);
        if (notifySelf)
        {
            _game.Send(NetDefine.CMD_MasterTeamExit, new LeaveTeamArg { RoleId = roleId }.ToByteString());
        }
        if (team.Members.Count == 0)
        {
            _teams.Remove(team.Id);
            return;
        }
        if (team.Captain == roleId) team.Captain = team.Members[0];
        NotifyRoster(team);
    }

    /// <summary>清理下线角色的队伍与待答复关系。</summary>
    /// <param name="roleId">已经离开游戏服的角色号。</param>
    public void OnRoleOffline(ulong roleId)
    {
        Leave(roleId, false);
        // 快照只复制队伍引用，删除的是原队伍中的待处理关系。
        List<Team> teams = new List<Team>(_teams.Values);
        // 逐队清理本人的未处理邀请和申请。
        for (int index = 0; index < teams.Count; index++)
        {
            teams[index].Invitations.Remove(roleId);
            teams[index].Applications.Remove(roleId);
        }
    }

    /// <summary>确认队长身份和目标后授权游戏服创建或重入实际副本。</summary>
    /// <param name="roleId">请求进入副本的角色号。</param>
    /// <param name="dungeonId">现有客户端选择的副本配置。</param>
    /// <param name="degree">队长选择的难度1至4；旧客户端0代表普通。</param>
    private void Enter(ulong roleId, int dungeonId, int degree)
    {
        if (degree == 0) degree = 1;
        if (LubanMgr.Instance.GetDungeonDifficultyById(degree) == null)
        {
            Error(roleId, CmdCode.ReqParamError);
            return;
        }
        // 找出该角色的队伍并核对其发起权限。
        Team team;
        // 所选原副本行决定场景资源登记；不存在的配置不能分配实例。
        DungeonsTable dungeon = LubanMgr.Instance.GetDungeonsTableById(dungeonId);
        if (dungeon == null) { Error(roleId, CmdCode.ReqParamError); return; }
        // 与客户端共用正式Scenes登记，未迁入的副本在修改MS状态前失败。
        if (typeof(TeamManager).Assembly.GetManifestResourceInfo("Dungeons." + dungeon.SceneID + ".json") == null)
        {
            LogMsg.Info("副本场景尚未登记，DungeonId=" + dungeonId);
            Error(roleId, CmdCode.ServerError);
            return;
        }
        if (!_roleTeams.TryGetValue(roleId, out team)) return;
        if (team.Captain != roleId) return;
        // RO 单人队伍直接进入；多人队伍先广播确认，队长视为已同意。
        if (team.Members.Count == 1)
        {
            EnterConfirmedDungeon(team, dungeonId, degree);
            return;
        }
        team.PendingDungeonId = dungeonId;
        team.PendingDungeonsDegree = degree;
        team.ConfirmDeadline = DateTime.UtcNow.AddSeconds(TeamEnterDungeonConfirmTime);
        team.PreparedMembers.Clear();
        team.PreparedMembers.Add(roleId);
        Broadcast(team, NetDefine.PtcM2C_AskMemberEnterFBNtf,
            new AskMemberEnterFBData { DungeonId = (uint)dungeonId, DungeonsDegree = degree, ConfirmSeconds = TeamEnterDungeonConfirmTime }.ToByteString());
    }

    /// <summary>处理实际队员的进本答复；拒绝立即取消，全部同意才发送 GS 入场指令。</summary>
    /// <param name="roleId">GS 会话认证后的答复人。</param><param name="reply">该成员的同意或拒绝。</param>
    private void MemberReplyEnterFB(ulong roleId, ReplyType reply)
    {
        // 只处理当前队伍正在等待的确认，已取消或结束的答复没有入场效果。
        Team team;
        if (!_roleTeams.TryGetValue(roleId, out team) || team.PendingDungeonId == 0) return;
        if (reply != ReplyType.Agree && reply != ReplyType.Refuse) return;
        Broadcast(team, NetDefine.PtcM2C_SyncMemberReplyEnterFBNtf,
            new SyncMemberReplyEnterFBData { Uid = roleId, Type = reply }.ToByteString());
        if (reply == ReplyType.Refuse)
        {
            ClearConfirmation(team);
            return;
        }
        team.PreparedMembers.Add(roleId);
        if (team.PreparedMembers.Count != team.Members.Count) return;
        // 保存确认目标后清理本轮数据，GS 仍是场景与战斗的执行者。
        int dungeonId = team.PendingDungeonId;
        // 队员同意的是发起时的难度，不能在清理确认状态后重新取默认值。
        int degree = team.PendingDungeonsDegree;
        ClearConfirmation(team);
        EnterConfirmedDungeon(team, dungeonId, degree);
    }

    /// <summary>按 MS 的时钟结束未全员同意的确认，不因客户端关闭窗口而自动入场。</summary>
    /// <param name="now">当前 UTC 时间。</param>
    public void Update(DateTime now)
    {
        // 仅复制队伍引用，超时直接修改原队伍的确认状态。
        List<Team> teams = new List<Team>(_teams.Values);
        for (int index = 0; index < teams.Count; index++)
        {
            // 当前待检查的队伍，未发起确认的队伍没有计时行为。
            Team team = teams[index];
            if (team.PendingDungeonId != 0 && now >= team.ConfirmDeadline)
                CancelConfirmation(team, 0, FailEnterFBReason.FailEnterFnTimeout);
        }
    }

    /// <summary>取消尚未结束的确认，并让全队关闭确认窗。</summary>
    /// <param name="team">当前队伍。</param><param name="roleId">变更成员，超时为 0。</param><param name="reason">RO 失败原因。</param>
    private void CancelConfirmation(Team team, ulong roleId, FailEnterFBReason reason)
    {
        if (team.PendingDungeonId == 0) return;
        ClearConfirmation(team);
        Broadcast(team, NetDefine.PtcM2C_FailEnterFBNtf,
            new FailEnterFBData { Uid = roleId, Reason = reason }.ToByteString());
    }

    /// <summary>释放单次入场确认的数据，队伍关系及已有副本保持不变。</summary>
    /// <param name="team">完成或取消确认的队伍。</param>
    private static void ClearConfirmation(Team team)
    {
        team.PendingDungeonId = 0;
        team.PendingDungeonsDegree = 0;
        team.PreparedMembers.Clear();
    }

    /// <summary>沿现有定向投递通道将相同确认通知发送给每个真实成员。</summary>
    /// <param name="team">收件队伍。</param><param name="code">客户端通知协议号。</param><param name="payload">序列化后的通知。</param>
    private void Broadcast(Team team, ushort code, ByteString payload)
    {
        for (int index = 0; index < team.Members.Count; index++)
            Deliver(team.Members[index], code, payload);
    }

    /// <summary>已确认的名单交给 GS 创建或重入同一副本，不在 MS 执行场景与战斗。</summary>
    /// <param name="team">完成确认的队伍。</param><param name="dungeonId">已同意进入的副本。</param>
    /// <param name="degree">已经全队确认的规范难度1至4。</param>
    private void EnterConfirmedDungeon(Team team, int dungeonId, int degree)
    {
        if (team.InstanceId == 0 || team.DungeonId != dungeonId || team.DungeonsDegree != degree)
        {
            team.InstanceId = _nextInstanceId++;
            team.DungeonId = dungeonId;
            team.DungeonsDegree = degree;
        }
        // 创建怪物和切换角色场景属于 GS，MS 只传入队伍、实例编号和名单。
        MasterTeamDungeonOrder order = new MasterTeamDungeonOrder { TeamId = team.Id, InstanceId = team.InstanceId, DungeonId = dungeonId, DungeonsDegree = degree };
        order.MemberIds.Add(team.Members);
        _game.Send(NetDefine.CMD_MasterTeamEnter, order.ToByteString());
        NotifyRoster(team);
    }

    /// <summary>把真实队员写入聊天收件名单，调用期间分区串行处理。</summary>
    /// <param name="sender">已由 GS 认证的发言角色。</param><param name="notice">待发送的队伍通知。</param>
    /// <returns>发言人是否已加入队伍。</returns>
    internal bool GetChatMembers(ulong sender, ChatMsgWithLists notice)
    {
        // 名单由 MS 直接提供，客户端不能指定其他玩家收件。
        Team team;
        if (!_roleTeams.TryGetValue(sender, out team)) return false;
        notice.ReceiverList.Add(team.Members);
        return true;
    }

    /// <summary>沿现有错误协议通知发起玩家。</summary>
    /// <param name="roleId">要收到错误提示的玩家。</param>
    /// <param name="code">原组队协议定义的业务错误。</param>
    private void Error(ulong roleId, CmdCode code)
    {
        Deliver(roleId, NetDefine.CMD_ErrCode, new ErrMsg { CmdCode = code }.ToByteString());
    }

    /// <summary>把定向的邀请或错误交由本分区 GS 找到在线角色。</summary>
    /// <param name="roleId">最终接收者的角色号。</param>
    /// <param name="code">既有客户端协议号。</param>
    /// <param name="payload">与协议号对应的序列化消息。</param>
    private void Deliver(ulong roleId, ushort code, ByteString payload)
    {
        // 目标角色号仅用于内网寻址，玩家收到的仍是原始客户端消息。
        MasterTeamRolePacket delivery = new MasterTeamRolePacket { RoleId = roleId, Packet = new BasePackage { ProtoCode = code, Data = payload } };
        _game.Send(NetDefine.CMD_MasterTeamDelivery, delivery.ToByteString());
    }
}
