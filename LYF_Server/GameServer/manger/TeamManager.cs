using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 服务端队伍与副本实例管理器，对应 RO TeamMgr.lua 与 DungeonMgr.lua 的服务端职责。
/// 同时维护队伍成员、队长、副本实例分配，是多人组队副本的唯一权威来源。
/// </summary>
public sealed class TeamManager
{
    /// <summary>单例实例，全局只保留一份队伍状态。</summary>
    private static readonly TeamManager _instance = new TeamManager();

    /// <summary>队伍人数上限，对应 RO TeamData.lua:63 的 maxTeamNumber = 5。</summary>
    public const int MaxMember = 5;

    /// <summary>魔神塔副本配置ID，对应 theme=7 映射的 DungeonsTable 4060。</summary>
    public const int MoshentaDungeonId = 4060;

    /// <summary>魔神塔场景ID，服务端下发入场通知时使用。</summary>
    public const int MoshentaSceneId = 4060;

    /// <summary>魔神塔地图ID，写入成员 BaseInfo.MapId，供附近过滤使用。</summary>
    public const int MoshentaMapId = 4060;

    /// <summary>
    /// 魔神塔出生坐标，沿用 RoleBaseInfo.pos 的 X_Y_Z 下划线格式。
    /// 取值来源：已验场景 Assets/GM_Game/Scenes/RODungeon_Moshenta/sc_dungeon_moshenta_001.unity
    /// 中 RoleSpawnPoint 的世界坐标（其父节点位于原点），与客户端 MainCityMgr._localRoleSpawnPoint 一致，
    /// 不能再用 0_0_0，否则服务端通知位置与客户端实际出生点不同，进入后互见与移动同步会错位。
    /// </summary>
    public const string MoshentaSpawnPos = "132.97_58.89_60.95";

    /// <summary>主城地图ID，与进入副本前的公共场景一致，0 表示公共主城。</summary>
    public const int MainCityMapId = 0;

    /// <summary>主城场景通知ID，对应客户端 SceneType.Scene_MainCity 的枚举序号 2。</summary>
    public const int MainCitySceneId = 2;

    /// <summary>
    /// 主城出生坐标，沿用 RoleBaseInfo.pos 的 X_Y_Z 下划线格式。
    /// 取值来源：客户端 MainCityMgr.ResolveMainRoleSpawnPosition 未配置出生点时的原有硬编码坐标，
    /// 主城离队回城必须回到该点，不能用 0_0_0 顶替。
    /// </summary>
    public const string MainCitySpawnPos = "62.1493416_19.4139996_80.6689758";

    /// <summary>队伍表，键为队伍ID，值为队伍状态。</summary>
    private readonly Dictionary<ulong, Team> _teams = new Dictionary<ulong, Team>();

    /// <summary>角色到队伍的索引，键为角色ID，值为队伍ID，用于快速反查。</summary>
    private readonly Dictionary<ulong, ulong> _roleTeam = new Dictionary<ulong, ulong>();

    /// <summary>待处理邀请表，键为"队伍ID_被邀请角色ID"，值为邀请发起人角色ID。</summary>
    private readonly Dictionary<string, ulong> _invitations = new Dictionary<string, ulong>();

    /// <summary>待处理入队申请表，键为"队伍ID_申请角色ID"，值为申请人角色ID，供队长同意或拒绝。</summary>
    private readonly Dictionary<string, ulong> _applications = new Dictionary<string, ulong>();

    /// <summary>队伍ID自增序列，从 1 开始，0 保留表示无队伍。</summary>
    private ulong _nextTeamId = 1UL;

    /// <summary>副本实例ID自增序列，从 1 开始，0 保留表示公共场景。</summary>
    private ulong _nextInstanceId = 1UL;

    /// <summary>队伍状态，保存成员、队长与当前副本实例。</summary>
    private sealed class Team
    {
        /// <summary>队伍唯一ID。</summary>
        public ulong TeamId;

        /// <summary>队长角色ID，仅队长可发起副本挑战。</summary>
        public ulong CaptainId;

        /// <summary>成员角色ID列表，顺序即入队顺序，第一个为创建者。</summary>
        public List<ulong> Members = new List<ulong>();

        /// <summary>该队伍当前所在副本实例ID，0 表示不在副本内。</summary>
        public ulong InstanceId;
    }

    /// <summary>获取队伍管理器单例。</summary>
    public static TeamManager Instance
    {
        get { return _instance; }
    }

    private TeamManager()
    {
    }

    /// <summary>
    /// 创建队伍，对应 RO TeamMgr.lua:43 RequestCreateTeam。
    /// 创建者自动成为队长，队伍人数为 1。
    /// </summary>
    /// <param name="role">发起创建的在世角色，身份来自会话校验结果。</param>
    /// <param name="targetId">组队目标ID，RO 缺省 1000，仅记录不参与匹配。</param>
    /// <returns>创建成功返回队伍ID，角色已在队伍中则返回 0。</returns>
    public ulong RequestCreateTeam(OnlineRole role, int targetId)
    {
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return 0UL;
        }
        // 角色ID即服务端权威身份，客户端无法替换。
        ulong roleId = (ulong)role.mainRoleInfo.BaseInfo.RoleId;
        // 已在队伍中的角色不允许重复建队。
        if (_roleTeam.ContainsKey(roleId))
        {
            return 0UL;
        }

        // 新建队伍实体：先占自增ID，再填队长与初始成员，最后同时写入队伍表与角色反查索引。
        Team team = new Team();
        team.TeamId = _nextTeamId;
        _nextTeamId = _nextTeamId + 1UL;
        team.CaptainId = roleId;
        team.InstanceId = 0UL;
        team.Members.Add(roleId);

        _teams.Add(team.TeamId, team);
        _roleTeam.Add(roleId, team.TeamId);
        // 建队后立刻整包下发，客户端以服务器状态为准刷新窗口。
        NotifyTeamInfo(team);
        return team.TeamId;
    }

    /// <summary>
    /// 邀请他人入队，对应 RO TeamMgr.lua:81 InviteJoinTeam(UserID, UserLevel)。
    /// 只登记待确认邀请，被邀请人同意后才真正入队。
    /// </summary>
    /// <param name="inviterRole">发起邀请的在世角色，必须是某队成员。</param>
    /// <param name="targetRoleId">被邀请角色ID。</param>
    /// <returns>邀请登记成功返回 true；无队伍、队伍已满、目标已在队伍中或队伍已开副本实例返回 false。</returns>
    public bool InviteJoinTeam(OnlineRole inviterRole, ulong targetRoleId)
    {
        if (inviterRole == null || inviterRole.mainRoleInfo == null ||
            inviterRole.mainRoleInfo.BaseInfo == null)
        {
            return false;
        }
        // 邀请人的权威角色ID，取自会话校验过的 BaseInfo，客户端无法替换。
        ulong inviterId = (ulong)inviterRole.mainRoleInfo.BaseInfo.RoleId;
        // 邀请人所在队伍ID，由 _roleTeam 反查得到；反查失败说明邀请人当前不在任何队伍中。
        ulong teamId;
        // 邀请人必须已在队伍中。
        if (!_roleTeam.TryGetValue(inviterId, out teamId))
        {
            return false;
        }
        // 邀请人所在队伍状态，用于校验人数上限与实例占用；取不到说明队伍已解散，属异常残留。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            return false;
        }
        // 队伍已满时不再登记邀请。
        if (team.Members.Count >= MaxMember)
        {
            return false;
        }
        // 队伍已分配副本实例时拒绝登记邀请：与 AcceptTeamInvatation、BegInTeamByTeamId
        // 对已开实例队伍的拒绝保持一致，避免出现"可邀请但同意必失败"的两入口不一致。
        if (team.InstanceId != 0UL)
        {
            return false;
        }
        // 目标已在队伍中时无需邀请。
        if (_roleTeam.ContainsKey(targetRoleId))
        {
            return false;
        }

        // 邀请键由队伍ID与被邀请角色ID拼接，保证同队同人只有一条待确认邀请。
        string key = teamId.ToString() + "_" + targetRoleId.ToString();
        _invitations[key] = inviterId;

        // 被邀请人在线时立即下发邀请通知，离线则仅保留待确认记录。
        // targetRole 为被邀请人的在线角色对象，为空表示对方不在线，本次不发包。
        OnlineRole targetRole = RoleManager.Instance.GetOnlineRole((int)targetRoleId);
        if (targetRole != null)
        {
            // 下发给被邀请人的邀请通知：队伍ID、邀请人、邀请人昵称与队长ID，供界面判断展示同意/拒绝。
            TeamInvatationNtf notice = new TeamInvatationNtf();
            notice.TeamId = teamId;
            notice.InviterId = inviterId;
            notice.InviterName = inviterRole.mainRoleInfo.BaseInfo.Nickname;
            notice.CaptainId = team.CaptainId;
            targetRole.SendToClient(NetDefine.CMD_TeamInvatationCode, notice.ToByteString());
        }
        return true;
    }

    /// <summary>
    /// 同意入队邀请，对应 RO TeamMgr.lua:165 AcceptTeamInvatation()。
    /// 校验队伍ID与邀请人一致后把角色加入队伍。
    /// </summary>
    /// <param name="role">同意邀请的在世角色。</param>
    /// <param name="teamId">邀请中的队伍ID。</param>
    /// <param name="inviterId">邀请人角色ID，用于确认邀请未被替换。</param>
    /// <returns>入队成功返回 true；邀请不存在、队伍已满或角色已在队伍中返回 false。</returns>
    public bool AcceptTeamInvatation(OnlineRole role, ulong teamId, ulong inviterId, bool agree)
    {
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return false;
        }
        // 请求者的权威角色ID，用于判断其是否就是该队队长（只有队长可以审批他人申请）。
        ulong roleId = (ulong)role.mainRoleInfo.BaseInfo.RoleId;
        // 队长处理他人申请：同意则入队，拒绝只清除申请记录。
        // applicationKey 为"队伍ID_申请人角色ID"，与 BegInTeamByTeamId 写入申请时的键格式必须完全一致。
        string applicationKey = teamId.ToString() + "_" + inviterId.ToString();
        // 命中申请时回填的申请人角色ID，即本次要被批准入队的人；未命中说明走的是邀请分支。
        ulong applicantId;
        if (_applications.TryGetValue(applicationKey, out applicantId))
        {
            // 申请所属队伍状态，用于校验审批人是否为队长、队伍是否已满或已开副本实例。
            Team applicantTeam;
            if (!_teams.TryGetValue(teamId, out applicantTeam))
            {
                _applications.Remove(applicationKey);
                return false;
            }
            // 只有该队队长可以审批申请，其他成员的身份不具备审批权。
            if (applicantTeam.CaptainId != roleId)
            {
                return false;
            }
            _applications.Remove(applicationKey);
            if (!agree)
            {
                return false;
            }
            if (applicantTeam.Members.Count >= MaxMember)
            {
                return false;
            }
            if (_roleTeam.ContainsKey(applicantId))
            {
                return false;
            }
            // 已开出副本实例的队伍不再接受申请入队，避免新成员只有队伍实例而没有一致的 BaseInfo 场景状态。
            if (applicantTeam.InstanceId != 0UL)
            {
                return false;
            }
            applicantTeam.Members.Add(applicantId);
            _roleTeam.Add(applicantId, teamId);
            NotifyTeamInfo(applicantTeam);
            return true;
        }

        // 已在队伍中的角色不能再接受邀请。
        if (_roleTeam.ContainsKey(roleId))
        {
            return false;
        }
        // 邀请分支的待确认键："队伍ID_本人角色ID"，与 InviteJoinTeam 登记邀请时写入的键格式一致。
        string key = teamId.ToString() + "_" + roleId.ToString();
        // 拒绝邀请时同时清除待确认邀请与该角色的申请记录，避免客户端清缓存后服务端仍留残留。
        if (!agree)
        {
            _invitations.Remove(key);
            _applications.Remove(key);
            return false;
        }
        // 服务端登记的邀请人角色ID，用于和请求携带的 inviterId 比对，防止顶替他人邀请。
        ulong recordedInviter;
        // 没有待确认邀请时拒绝，避免绕过邀请直接入队。
        if (!_invitations.TryGetValue(key, out recordedInviter))
        {
            return false;
        }
        // 邀请人必须与请求携带的一致。
        if (recordedInviter != inviterId)
        {
            return false;
        }
        // 邀请所属队伍状态，用于校验人数上限与实例占用。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            return false;
        }
        // 队伍已满时拒绝入队。
        if (team.Members.Count >= MaxMember)
        {
            return false;
        }
        // 已开出副本实例的队伍不再接受邀请入队，与申请入口保持一致，避免两条入队路径行为不同。
        if (team.InstanceId != 0UL)
        {
            return false;
        }

        _invitations.Remove(key);
        team.Members.Add(roleId);
        _roleTeam.Add(roleId, teamId);
        NotifyTeamInfo(team);
        return true;
    }

    /// <summary>
    /// 申请加入指定队伍，对应 RO TeamMgr.lua:413 BegInTeamByTeamId(cTeamId)。
    /// 本阶段按"申请即入队"的最短流程处理，不引入额外审批队列。
    /// </summary>
    /// <param name="role">发起申请的在世角色。</param>
    /// <param name="teamId">目标队伍ID。</param>
    /// <returns>入队成功返回 true；队伍不存在、已满或角色已在队伍中返回 false。</returns>
    public bool BegInTeamByTeamId(OnlineRole role, ulong teamId)
    {
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return false;
        }
        // 申请人的权威角色ID，取自会话校验过的 BaseInfo，作为登记申请与后续入队的唯一依据。
        ulong roleId = (ulong)role.mainRoleInfo.BaseInfo.RoleId;
        if (_roleTeam.ContainsKey(roleId))
        {
            return false;
        }
        // 目标队伍状态，用于校验人数上限与实例占用；取不到说明队伍ID无效或已解散。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            return false;
        }
        if (team.Members.Count >= MaxMember)
        {
            return false;
        }
        // 已开出副本实例的队伍不再接受直接申请，避免新成员无声加入与实例不一致。
        if (team.InstanceId != 0UL)
        {
            return false;
        }

        // 登记申请并由队长同意后才真正入队，不直接把陌生人加入队伍。
        // key 为"队伍ID_申请人角色ID"，与 AcceptTeamInvatation 查询申请时的键格式必须完全一致。
        string key = teamId.ToString() + "_" + roleId.ToString();
        _applications[key] = roleId;

        // 通知队长有新申请，复用现有邀请型强类型通知，来源字段携带申请人角色ID。
        // captainRole 为队长的在线角色对象，为空表示队长不在线，申请仅留在表里等其上线后处理。
        OnlineRole captainRole = RoleManager.Instance.GetOnlineRole((int)team.CaptainId);
        if (captainRole != null)
        {
            // 发给队长的申请通知：InviterId 承载申请人角色ID，CaptainId 等于队长本人，客户端据此判定是"申请"而非"被邀请"。
            TeamInvatationNtf notice = new TeamInvatationNtf();
            notice.TeamId = teamId;
            notice.InviterId = roleId;
            notice.InviterName = role.mainRoleInfo.BaseInfo.Nickname;
            notice.CaptainId = team.CaptainId;
            captainRole.SendToClient(NetDefine.CMD_TeamInvatationCode, notice.ToByteString());
        }
        return true;
    }

    /// <summary>
    /// 离队，对应 RO TeamMgr.lua:315 LeaveTeam()。
    /// 队长离队时把队长转移给剩余第一个成员；最后一人离队则解散队伍并回收实例。
    /// </summary>
    /// <param name="roleId">离队角色ID。</param>
    /// <returns>确实移除了队伍关系返回 true。</returns>
    public bool LeaveTeam(ulong roleId)
    {
        return LeaveTeamInternal(roleId, true);
    }

    /// <summary>
    /// 离队的内部实现，按是否通知本人区分主动离队与掉线清理。
    /// </summary>
    /// <param name="roleId">离队角色ID。</param>
    /// <param name="notifySelf">是否向本人下发空队伍状态；掉线时为 false，避免向失效会话发包。</param>
    /// <returns>确实移除了队伍关系返回 true。</returns>
    private bool LeaveTeamInternal(ulong roleId, bool notifySelf)
    {
        // 离队角色当前所在队伍ID，由 _roleTeam 反查；反查失败说明本来就不在任何队伍，直接返回。
        ulong teamId;
        if (!_roleTeam.TryGetValue(roleId, out teamId))
        {
            return false;
        }
        // 队伍状态，用于移除成员与判断是否解散；取不到说明队伍已消失，只需清掉角色侧残留索引。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            _roleTeam.Remove(roleId);
            return false;
        }

        team.Members.Remove(roleId);
        _roleTeam.Remove(roleId);
        // 离队角色回到公共场景，避免继续占用副本实例。
        ResetRoleInstance(roleId, notifySelf);

        if (team.Members.Count == 0)
        {
            // 最后一人离队：解散队伍并回收实例。
            _teams.Remove(teamId);
            RemoveTeamInvitations(teamId);
            if (team.InstanceId != 0UL)
            {
                ReleaseInstance(team.InstanceId);
            }
            return true;
        }

        // 队长离队时转移给剩余第一个成员，保持队伍可用。
        if (team.CaptainId == roleId)
        {
            team.CaptainId = team.Members[0];
        }
        NotifyTeamInfo(team);
        return true;
    }

    /// <summary>
    /// 角色掉线时的队伍清理入口，由 RoleManager 下线流程调用。
    /// 行为与主动离队一致，保证掉线不会留下僵尸成员。
    /// </summary>
    /// <param name="roleId">掉线角色ID。</param>
    public void OnRoleOffline(ulong roleId)
    {
        LeaveTeamInternal(roleId, false);
        // 掉线角色未确认的邀请一并清除，避免邀请残留。
        RemoveInvitationsOfRole(roleId);
    }

    /// <summary>
    /// 队长请求进入魔神塔，对应 RO DungeonMgr.lua:1274 CaptainRequestEnterFBPtc。
    /// 服务端先分配实例并写入每个成员的 MapId/InstanceId/出生点，再通知全队加载场景。
    /// </summary>
    /// <param name="role">发起挑战的在世角色，必须是队长。</param>
    /// <param name="dungeonId">副本配置ID，当前只开放魔神塔 4060。</param>
    /// <returns>入场通知已下发返回 true；非队长、无队伍或副本未开放返回 false。</returns>
    public bool CaptainRequestEnterFBPtc(OnlineRole role, int dungeonId)
    {
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return false;
        }
        // 只开放已授权的魔神塔，其他副本不因本任务被打开。
        if (dungeonId != MoshentaDungeonId)
        {
            return false;
        }
        // 挑战发起者的权威角色ID，取自会话校验过的 BaseInfo，用于确认其是否为队长。
        ulong roleId = (ulong)role.mainRoleInfo.BaseInfo.RoleId;
        // 发起者所在队伍ID，由 _roleTeam 反查；为空说明单人状态，不能开副本实例。
        ulong teamId;
        if (!_roleTeam.TryGetValue(roleId, out teamId))
        {
            return false;
        }
        // 待开实例的队伍状态：整段入场流程都读写它的 Members 与 InstanceId，是本次入场的作用对象。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            return false;
        }
        // 只有队长可以发起副本挑战。
        if (team.CaptainId != roleId)
        {
            return false;
        }
        // 重复请求复用已有实例，避免同一队伍开出两个副本。
        if (team.InstanceId == 0UL)
        {
            team.InstanceId = _nextInstanceId;
            _nextInstanceId = _nextInstanceId + 1UL;
        }

        // 进入新实例前，先按原主城/原实例的真实观察者广播离开通知，不能只通知同队。
        // 广播必须发生在改写 MapId/InstanceId 之前，否则附近过滤读到的已经是新位置。
        // index 是队伍成员表的零基下标，范围 [0, team.Members.Count)，本循环只用于按序广播离开。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员在"改写位置之前"的在线角色对象，为空说明该成员已掉线，跳过。
            OnlineRole leavingRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (leavingRole == null)
            {
                continue;
            }
            RoleManager.Instance.BroadcastRoleLeave(leavingRole);
        }

        // 先写服务器权威位置，再通知客户端加载，保证进入后过滤条件已生效。
        // index 是队伍成员表的零基下标，范围 [0, team.Members.Count)，本循环负责写权威位置。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的权威角色ID，先取出再查在线对象，避免下标表达式重复求值。
            ulong memberId = team.Members[index];
            // 当前成员的在线角色对象，用于写入 MapId/InstanceId/出生点；为空说明已掉线，跳过。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)memberId);
            if (memberRole == null || memberRole.mainRoleInfo == null ||
                memberRole.mainRoleInfo.BaseInfo == null)
            {
                continue;
            }
            memberRole.mainRoleInfo.BaseInfo.MapId = MoshentaMapId;
            memberRole.mainRoleInfo.BaseInfo.InstanceId = (int)team.InstanceId;
            memberRole.mainRoleInfo.BaseInfo.Pos = MoshentaSpawnPos;
        }

        // 新实例内互相可见：先把本人补发给同实例其他成员，再把同实例其他成员补发给本人。
        // 第一轮：index 为成员表零基下标，逐个把"本人"广播给同实例其他成员。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的在线角色对象；为空说明已掉线，无需广播。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (memberRole == null)
            {
                continue;
            }
            RoleManager.Instance.BroadcastRoleEnterWorldByRole(memberRole);
        }
        // 第二轮：index 同上，逐个把"同实例其他成员"补发给本人，使双向可见。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的在线角色对象，作为接收方；为空说明已掉线，跳过补发。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (memberRole == null)
            {
                continue;
            }
            RoleManager.Instance.ResendNearbyRoles(memberRole);
        }

        // 入场确认通知：全队共用同一份内容，先构造一次再序列化，避免每个成员重复构造。
        TeamEnterDungeonNtf notice = new TeamEnterDungeonNtf();
        notice.TeamId = teamId;
        notice.DungeonId = dungeonId;
        notice.SceneId = MoshentaSceneId;
        notice.InstanceId = team.InstanceId;
        notice.MapId = MoshentaMapId;
        notice.Pos = MoshentaSpawnPos;
        // 已序列化的入场通知字节，全队复用同一份，保证各成员收到的场景与实例完全一致。
        ByteString payload = notice.ToByteString();
        // index 为成员表零基下标，逐个下发同一份 payload。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的在线角色对象，作为接收方；为空说明已掉线，跳过下发。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (memberRole == null)
            {
                continue;
            }
            memberRole.SendToClient(NetDefine.CMD_TeamEnterDungeonCode, payload);
        }
        // 队伍状态变化后同步成员实例信息。
        NotifyTeamInfo(team);
        return true;
    }

    /// <summary>
    /// 返回角色所在副本实例ID，供 RoleManager 的附近过滤使用。
    /// </summary>
    /// <param name="roleId">待查询的角色ID。</param>
    /// <returns>所在队伍实例ID；不在队伍或不在副本时返回 0。</returns>
    public ulong GetInstanceId(ulong roleId)
    {
        // 角色所在队伍ID，由 _roleTeam 反查；反查失败说明不在队伍中，实例按 0（公共场景）处理。
        ulong teamId;
        if (!_roleTeam.TryGetValue(roleId, out teamId))
        {
            return 0UL;
        }
        // 队伍状态，实例号只记录在队伍上；取不到说明队伍已解散，同样按公共场景处理。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            return 0UL;
        }
        return team.InstanceId;
    }

    /// <summary>返回队伍当前成员数量，用于容量校验与状态查询。</summary>
    /// <param name="teamId">待查询的队伍ID。</param>
    /// <returns>成员数量；队伍不存在时返回 0。</returns>
    public int GetMemberCount(ulong teamId)
    {
        // 待查询队伍的状态；取不到说明队伍ID无效或已解散，成员数按 0 返回。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            return 0;
        }
        return team.Members.Count;
    }

    /// <summary>返回队伍队长角色ID，用于身份校验与断言。</summary>
    /// <param name="teamId">待查询的队伍ID。</param>
    /// <returns>队长角色ID；队伍不存在时返回 0。</returns>
    public ulong GetCaptainId(ulong teamId)
    {
        // 待查询队伍的状态；取不到说明队伍ID无效或已解散，队长ID按 0 返回。
        Team team;
        if (!_teams.TryGetValue(teamId, out team))
        {
            return 0UL;
        }
        return team.CaptainId;
    }

    /// <summary>
    /// 返回角色所在队伍ID。
    /// </summary>
    /// <param name="roleId">待查询的角色ID。</param>
    /// <returns>队伍ID；不在队伍时返回 0。</returns>
    public ulong GetTeamId(ulong roleId)
    {
        // 角色所在队伍ID，由 _roleTeam 反查；反查失败说明不在队伍中，按 0 返回。
        ulong teamId;
        if (!_roleTeam.TryGetValue(roleId, out teamId))
        {
            return 0UL;
        }
        return teamId;
    }

    /// <summary>向队伍全部在线成员整包下发队伍状态。</summary>
    /// <param name="team">发生变化的队伍。</param>
    private void NotifyTeamInfo(Team team)
    {
        if (team == null)
        {
            return;
        }
        // 本次要整包下发的队伍状态快照，成员信息按 team.Members 顺序逐项填充。
        TeamInfoNtf notice = new TeamInfoNtf();
        notice.TeamId = team.TeamId;
        notice.CaptainId = team.CaptainId;
        notice.MaxMember = MaxMember;
        // index 为成员表零基下标，范围 [0, team.Members.Count)，按入队顺序生成成员项。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的权威角色ID，用于填成员项与查询在线对象。
            ulong memberId = team.Members[index];
            // 单个成员的下行信息项，昵称/等级/血量等取自在线对象，不在线时只保留ID与实例。
            TeamMemberInfo memberInfo = new TeamMemberInfo();
            memberInfo.RoleId = memberId;
            memberInfo.IsCaptain = memberId == team.CaptainId;
            memberInfo.InstanceId = (int)team.InstanceId;
            // 当前成员的在线角色对象，用来补昵称/等级/血量等展示字段；为空说明已掉线，只发基础项。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)memberId);
            if (memberRole != null && memberRole.mainRoleInfo != null &&
                memberRole.mainRoleInfo.BaseInfo != null)
            {
                // 该成员的角色基础信息，是昵称、等级、职业、场景与血量的唯一来源。
                RoleBaseInfo baseInfo = memberRole.mainRoleInfo.BaseInfo;
                memberInfo.Nickname = baseInfo.Nickname;
                memberInfo.Level = baseInfo.Level;
                memberInfo.JobId = baseInfo.JobId;
                // 本项目以 MapId 作为场景标识，对应 RO memberList.sceneId。
                memberInfo.SceneId = baseInfo.MapId;
                memberInfo.CurrHp = baseInfo.CurrHp;
                memberInfo.MaxHp = baseInfo.MaxHp;
            }
            notice.Members.Add(memberInfo);
        }

        // 已序列化的队伍状态字节，全队复用同一份，保证各成员看到同一版本快照。
        ByteString payload = notice.ToByteString();
        // index 为成员表零基下标，逐个下发同一份 payload。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的在线角色对象，作为接收方；为空说明已掉线，跳过下发。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (memberRole == null)
            {
                continue;
            }
            memberRole.SendToClient(NetDefine.CMD_TeamInfoCode, payload);
        }
    }

    /// <summary>
    /// 把角色场景与实例恢复为主城公共状态，用于离队与掉线清理。
    /// 顺序固定：先按旧位置广播离开，再改写 MapId/InstanceId/Pos，最后下发回城通知。
    /// </summary>
    /// <param name="roleId">需要复位的角色ID。</param>
    /// <param name="notifySelf">是否向本人下发回城与空队伍状态；掉线时为 false，避免向失效会话发包。</param>
    private void ResetRoleInstance(ulong roleId, bool notifySelf)
    {
        // 待复位的在线角色对象，为空说明角色已下线或数据不完整，直接放弃复位。
        OnlineRole role = RoleManager.Instance.GetOnlineRole((int)roleId);
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return;
        }
        // 改写字段前广播，此时附近过滤读到的仍是角色离开前的旧地图与旧实例。
        // 掉线也必须执行，否则旧场景其他观察者会留下幽灵角色。
        RoleManager.Instance.BroadcastRoleLeave(role);

        // 离队后回到公共主城：实例归零、地图回到主城、出生点回到主城真实坐标，避免残留在副本视野。
        role.mainRoleInfo.BaseInfo.InstanceId = 0;
        role.mainRoleInfo.BaseInfo.MapId = MainCityMapId;
        role.mainRoleInfo.BaseInfo.Pos = MainCitySpawnPos;

        // 主动离队且会话仍在线时，下发回城通知让客户端切回主城；掉线不发。
        if (!notifySelf)
        {
            return;
        }
        // 回城通知：复用入场通知结构，SceneId 填主城、InstanceId 归零，客户端据此切回主城场景。
        TeamEnterDungeonNtf backNotice = new TeamEnterDungeonNtf();
        backNotice.TeamId = 0UL;
        backNotice.DungeonId = 0;
        backNotice.SceneId = MainCitySceneId;
        backNotice.InstanceId = 0UL;
        backNotice.MapId = MainCityMapId;
        backNotice.Pos = MainCitySpawnPos;
        role.SendToClient(NetDefine.CMD_TeamEnterDungeonCode, backNotice.ToByteString());
        // 回到主城后重新补发主城附近角色，客户端据此重建远端列表并保留本人。
        RoleManager.Instance.ResendNearbyRoles(role);

        // 空队伍状态：TeamId 与 CaptainId 都为 0，客户端据此清空队伍界面并判定已离队。
        TeamInfoNtf emptyNotice = new TeamInfoNtf();
        emptyNotice.TeamId = 0UL;
        emptyNotice.CaptainId = 0UL;
        emptyNotice.MaxMember = MaxMember;
        role.SendToClient(NetDefine.CMD_TeamInfoCode, emptyNotice.ToByteString());
    }

    /// <summary>回收副本实例，当前实例只记录在队伍上，清空即回收。</summary>
    /// <param name="instanceId">待回收的实例ID。</param>
    private void ReleaseInstance(ulong instanceId)
    {
        // 实例状态保存在 Team.InstanceId 上，队伍解散时随队伍一并释放。
        LogMsg.Info("副本实例回收: " + instanceId.ToString());
    }

    /// <summary>清除某队伍登记的全部待确认邀请。</summary>
    /// <param name="teamId">需要清理邀请的队伍ID。</param>
    private void RemoveTeamInvitations(ulong teamId)
    {
        // 目标队伍的键前缀"队伍ID_"，用来匹配所有以该队伍ID开头的邀请键。
        string prefix = teamId.ToString() + "_";
        // 邀请键快照：遍历期间会删除元素，先复制一份键列表避免边遍历边改集合。
        List<string> keys = new List<string>(_invitations.Keys);
        // index 为键快照的零基下标，范围 [0, keys.Count)，仅用于本次清理。
        for (int index = 0; index < keys.Count; index++)
        {
            if (keys[index].StartsWith(prefix, StringComparison.Ordinal))
            {
                _invitations.Remove(keys[index]);
            }
        }
    }

    /// <summary>清除某角色作为被邀请人的全部待确认邀请。</summary>
    /// <param name="roleId">掉线或被清理的角色ID。</param>
    private void RemoveInvitationsOfRole(ulong roleId)
    {
        // 被邀请角色ID的键后缀"_角色ID"，用来匹配所有以该角色ID结尾的邀请键。
        string suffix = "_" + roleId.ToString();
        // 邀请键快照：遍历期间会删除元素，先复制一份键列表避免边遍历边改集合。
        List<string> keys = new List<string>(_invitations.Keys);
        // index 为键快照的零基下标，范围 [0, keys.Count)，仅用于本次清理。
        for (int index = 0; index < keys.Count; index++)
        {
            if (keys[index].EndsWith(suffix, StringComparison.Ordinal))
            {
                _invitations.Remove(keys[index]);
            }
        }
    }
}
