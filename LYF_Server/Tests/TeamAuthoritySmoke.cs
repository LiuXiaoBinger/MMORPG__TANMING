using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

/// <summary>
/// 服务端队伍与副本实例权威的独立进程冒烟测试。
/// 直接调用 TeamManager 与 RoleManager 的正式方法，不使用数据库、账号或镜像算法。
/// </summary>
public static class TeamAuthoritySmoke
{
    /// <summary>断言失败计数，为 0 时整体通过。</summary>
    private static int _failures;

    /// <summary>已执行的断言条数，用于日志核对。</summary>
    private static int _checks;

    /// <summary>本测试注册进 RoleManager 在线索引的角色，结束时统一移除。</summary>
    private static readonly List<OnlineRole> _registeredRoles = new List<OnlineRole>();

    /// <summary>测试入口，返回 0 表示全部断言通过。</summary>
    /// <param name="args">命令行参数，本测试不接受参数。</param>
    /// <returns>全部通过返回 0，否则返回失败条数。</returns>
    public static int Main(string[] args)
    {
        // 本测试不依赖 Luban 配置表与数据库，直接使用强类型协议对象。
        try
        {
            CheckCreateAndInviteFlow();
            CheckCapacityAndSixthRejected();
            CheckApplicationNeedsCaptainApproval();
            CheckCaptainOnlyEnter();
            CheckSingleMemberEnterAndInstance();
            CheckTwoTeamIsolation();
            CheckBroadcastRecipientFilter();
            CheckLeaveAndCaptainTransfer();
            CheckLastMemberDissolve();
            CheckRepeatEnterKeepsInstance();
            CheckSpawnCoordinates();
            CheckWireDeliveryByLoopbackSocket();
            CheckMainCityLeaveKeepsVisibility();
            CheckNonCaptainMainCityLeaveKeepsTeam();
            CheckSoloMainCityLeaveDissolvesWithoutTeleport();
            CheckDungeonLeaveBroadcastsLeaveAndReturns();
            CheckOfflineInMainCityStillRemovesRole();
        }
        catch (Exception ex)
        {
            // ex 为用例执行中抛出的异常，只打印消息并计一次失败，不中断后续用例。
            Console.WriteLine("[EXCEPTION] " + ex.Message);
            _failures = _failures + 1;
        }
        finally
        {
            // 无论用例是否异常都移除测试注册的角色，避免污染同进程在线索引。
            UnregisterAllRoles();
        }

        Console.WriteLine("checks=" + _checks.ToString() + " failures=" + _failures.ToString());
        if (_failures == 0)
        {
            Console.WriteLine("TEAM_AUTHORITY_SMOKE: PASS");
            return 0;
        }
        Console.WriteLine("TEAM_AUTHORITY_SMOKE: FAIL");
        return _failures;
    }

    /// <summary>验证建队后队长为本人、成员数为 1，且待邀请未同意不能入队。</summary>
    private static void CheckCreateAndInviteFlow()
    {
        // 队长角色：角色ID 1001，用于发起建队与邀请，整个用例中身份不变。
        OnlineRole captain = NewRole(1001, "队长甲");
        // 被邀请角色：角色ID 1002，用于验证"已邀请但未同意不得入队"。
        OnlineRole invited = NewRole(1002, "队员乙");

        // 建队返回的队伍ID，0 表示建队失败，后续断言都以它作为队伍身份。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);
        Expect(teamId != 0UL, "建队返回非0队伍ID");
        Expect(TeamManager.Instance.GetCaptainId(teamId) == 1001UL, "建队者成为队长");
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 1, "建队后成员数为1");
        Expect(TeamManager.Instance.GetTeamId(1002UL) == 0UL, "被邀请人尚未入队");

        // 仅邀请未同意时，被邀请人不得进入队伍。
        // 邀请登记结果：true 表示待确认邀请已写入，此时被邀请人仍未入队。
        bool invitedOk = TeamManager.Instance.InviteJoinTeam(captain, 1002UL);
        Expect(invitedOk, "邀请登记成功");
        Expect(TeamManager.Instance.GetTeamId(1002UL) == 0UL, "邀请未同意不能入队");

        // 同意后才入队。
        // 同意结果：true 表示被邀请人已真正加入该队伍。
        bool acceptOk = TeamManager.Instance.AcceptTeamInvatation(invited, teamId, 1001UL, true);
        Expect(acceptOk, "同意邀请返回成功");
        Expect(TeamManager.Instance.GetTeamId(1002UL) == teamId, "同意后进入该队伍");
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 2, "同意后成员数为2");

        // 拒绝邀请：另一名角色收到邀请后拒绝，不得入队。
        // 拒绝邀请的角色：角色ID 1003，用于验证拒绝后既不占队伍名额也不留待确认记录。
        OnlineRole rejected = NewRole(1003, "队员丙");
        TeamManager.Instance.InviteJoinTeam(captain, 1003UL);
        // 拒绝结果：false 表示确实未入队，是拒绝分支的预期返回值。
        bool rejectOk = TeamManager.Instance.AcceptTeamInvatation(rejected, teamId, 1001UL, false);
        Expect(!rejectOk, "拒绝邀请返回失败");
        Expect(TeamManager.Instance.GetTeamId(1003UL) == 0UL, "拒绝后未入队");

        // 清理：队长离队后队伍解散，避免影响后续用例。
        TeamManager.Instance.LeaveTeam(1002UL);
        TeamManager.Instance.LeaveTeam(1001UL);
        Expect(TeamManager.Instance.GetTeamId(1001UL) == 0UL, "清理后队长已不在队伍");
    }

    /// <summary>验证队伍容量为 5，第 6 人无法入队。</summary>
    private static void CheckCapacityAndSixthRejected()
    {
        // 容量用例的队长：角色ID 1101，占用第一个名额。
        OnlineRole captain = NewRole(1101, "容量队长");
        // 容量用例的队伍ID，整段用它校验成员数增减。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);
        Expect(TeamManager.MaxMember == 5, "队伍上限为5");

        // 依次让 4 名角色通过邀请加入，凑满 5 人。
        // index 为本次补员轮次，范围 [0, 4)，同时决定被邀请角色ID 与昵称后缀。
        for (int index = 0; index < 4; index++)
        {
            // 本轮要加入的成员角色ID，从 1102 递增，避免与队长 1101 冲突。
            int roleId = 1102 + index;
            // 本轮成员角色实例，用于走"邀请+同意"完整入队路径。
            OnlineRole member = NewRole(roleId, "容量成员" + index.ToString());
            TeamManager.Instance.InviteJoinTeam(captain, (ulong)roleId);
            TeamManager.Instance.AcceptTeamInvatation(member, teamId, 1101UL, true);
        }
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 5, "满员为5人");

        // 第 6 人邀请必须被拒绝。
        // 第六名角色：角色ID 1106，用于验证满员后无法再占名额。
        OnlineRole sixth = NewRole(1106, "第六人");
        // 第六人邀请结果：false 表示满员保护生效。
        bool sixthInvited = TeamManager.Instance.InviteJoinTeam(captain, 1106UL);
        Expect(!sixthInvited, "满员后邀请被拒绝");
        Expect(TeamManager.Instance.GetTeamId(1106UL) == 0UL, "第六人未入队");

        // 清理。
        // index 为清理轮次，范围 [0, 5)，按角色ID 1101 起逐个离队直到队伍解散。
        for (int index = 0; index < 5; index++)
        {
            TeamManager.Instance.LeaveTeam((ulong)(1101 + index));
        }
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 0, "清理后队伍已解散");
    }

    /// <summary>验证按队伍ID申请后不会直接入队，必须由队长同意。</summary>
    private static void CheckApplicationNeedsCaptainApproval()
    {
        // 审批用例的队长：角色ID 1201，是唯一有权同意申请的人。
        OnlineRole captain = NewRole(1201, "审批队长");
        // 申请人：角色ID 1202，走 BegInTeamByTeamId 申请入口。
        OnlineRole applicant = NewRole(1202, "申请人");
        // 与队伍无关的第三方：角色ID 1203，用于验证非队长无权审批。
        OnlineRole outsider = NewRole(1203, "无关成员");
        // 审批用例的队伍ID，三人共用同一支队伍做权限对比。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);

        // 申请登记结果：true 表示申请已写入，但此时申请人仍未入队。
        bool applied = TeamManager.Instance.BegInTeamByTeamId(applicant, teamId);
        Expect(applied, "申请登记成功");
        Expect(TeamManager.Instance.GetTeamId(1202UL) == 0UL, "申请后未直接入队");

        // 非队长无法审批，申请保持待处理。
        // 非队长审批结果：false 表示权限校验拦住了越权审批。
        bool outsiderApprove = TeamManager.Instance.AcceptTeamInvatation(outsider, teamId, 1202UL, true);
        Expect(!outsiderApprove, "非队长审批被拒绝");
        Expect(TeamManager.Instance.GetTeamId(1202UL) == 0UL, "非队长审批后仍未入队");

        // 队长同意后入队。
        // 队长审批结果：true 表示申请被批准、申请人正式入队。
        bool captainApprove = TeamManager.Instance.AcceptTeamInvatation(captain, teamId, 1202UL, true);
        Expect(captainApprove, "队长同意申请成功");
        Expect(TeamManager.Instance.GetTeamId(1202UL) == teamId, "队长同意后入队");
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 2, "审批后成员数为2");

        TeamManager.Instance.LeaveTeam(1202UL);
        TeamManager.Instance.LeaveTeam(1201UL);
    }

    /// <summary>验证只有队长可以发起副本挑战，非队长被拒绝。</summary>
    private static void CheckCaptainOnlyEnter()
    {
        // 挑战用例的队长：角色ID 1301，是唯一有权发起副本挑战的人。
        OnlineRole captain = NewRole(1301, "挑战队长");
        // 普通队员：角色ID 1302，用于验证非队长挑战会被拒绝。
        OnlineRole member = NewRole(1302, "普通队员");
        // 挑战用例的队伍ID，先建队再拉入一名队员。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);
        TeamManager.Instance.InviteJoinTeam(captain, 1302UL);
        TeamManager.Instance.AcceptTeamInvatation(member, teamId, 1301UL, true);

        // 队员挑战结果：false 表示队长身份校验生效。
        bool memberEnter = TeamManager.Instance.CaptainRequestEnterFBPtc(member, TeamManager.MoshentaDungeonId);
        Expect(!memberEnter, "非队长挑战被拒绝");
        Expect(TeamManager.Instance.GetInstanceId(1301UL) == 0UL, "被拒绝后未分配实例");

        // 未开放副本同样被拒绝。
        // 非法副本ID挑战结果：false 表示只放行已授权的魔神塔 4060。
        bool wrongDungeon = TeamManager.Instance.CaptainRequestEnterFBPtc(captain, 9999);
        Expect(!wrongDungeon, "未开放副本被拒绝");

        TeamManager.Instance.LeaveTeam(1302UL);
        TeamManager.Instance.LeaveTeam(1301UL);
    }

    /// <summary>验证单人成队后可以入场，且实例ID非0。</summary>
    private static void CheckSingleMemberEnterAndInstance()
    {
        // 单人用例的角色：角色ID 1401，建队后队伍只有他一人。
        OnlineRole solo = NewRole(1401, "单人角色");
        // 单人队伍的队伍ID，用于校验成员数与实例归属。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(solo, 1000);
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 1, "单人队伍成员数为1");

        // 单人入场结果：true 表示单人成队同样可以开副本实例。
        bool entered = TeamManager.Instance.CaptainRequestEnterFBPtc(solo, TeamManager.MoshentaDungeonId);
        Expect(entered, "单人成队可入场");
        // 入场后分配给本人的实例ID，用于校验非 0 与离队后归零。
        ulong instanceId = TeamManager.Instance.GetInstanceId(1401UL);
        Expect(instanceId != 0UL, "单人入场实例ID非0");

        TeamManager.Instance.LeaveTeam(1401UL);
        Expect(TeamManager.Instance.GetInstanceId(1401UL) == 0UL, "离队后实例归零");
    }

    /// <summary>验证同队成员进入同一实例，不同队伍实例互相隔离。</summary>
    private static void CheckTwoTeamIsolation()
    {
        // 甲队队长：角色ID 1501，负责建队、拉人与发起挑战。
        OnlineRole captainA = NewRole(1501, "甲队队长");
        // 甲队队员：角色ID 1502，用于验证同队两人进入同一实例。
        OnlineRole memberA = NewRole(1502, "甲队队员");
        // 乙队队长：角色ID 1601，用于验证不同队伍实例互相隔离。
        OnlineRole captainB = NewRole(1601, "乙队队长");

        // 甲队队伍ID，两人共用。
        ulong teamA = TeamManager.Instance.RequestCreateTeam(captainA, 1000);
        TeamManager.Instance.InviteJoinTeam(captainA, 1502UL);
        TeamManager.Instance.AcceptTeamInvatation(memberA, teamA, 1501UL, true);
        TeamManager.Instance.CaptainRequestEnterFBPtc(captainA, TeamManager.MoshentaDungeonId);

        // 乙队队伍ID，独立建队并独立入场，用于和甲队比较实例号。
        ulong teamB = TeamManager.Instance.RequestCreateTeam(captainB, 1000);
        TeamManager.Instance.CaptainRequestEnterFBPtc(captainB, TeamManager.MoshentaDungeonId);

        // 甲队队长实际所在实例号。
        ulong instanceA1 = TeamManager.Instance.GetInstanceId(1501UL);
        // 甲队队员实际所在实例号，应与队长一致。
        ulong instanceA2 = TeamManager.Instance.GetInstanceId(1502UL);
        // 乙队队长实际所在实例号，应与甲队不同。
        ulong instanceB = TeamManager.Instance.GetInstanceId(1601UL);
        Expect(instanceA1 != 0UL && instanceA2 != 0UL, "甲队两人均有实例");
        Expect(instanceA1 == instanceA2, "同队成员进入同一实例");
        Expect(instanceB != 0UL, "乙队有实例");
        Expect(instanceA1 != instanceB, "两队实例互相隔离");

        TeamManager.Instance.LeaveTeam(1502UL);
        TeamManager.Instance.LeaveTeam(1501UL);
        TeamManager.Instance.LeaveTeam(1601UL);
    }

    /// <summary>验证四个广播与补发共用的收件人谓词按同地图同实例过滤。</summary>
    private static void CheckBroadcastRecipientFilter()
    {
        // 观察者角色：角色ID 1701，其地图/实例/坐标在用例中被反复改写以覆盖四种过滤情形。
        OnlineRole observer = NewRole(1701, "观察者");
        // 事件来源角色：角色ID 1702，固定在魔神塔实例 7 的原点，作为过滤基准。
        OnlineRole source = NewRole(1702, "事件来源");

        // 同地图同实例且相邻：应收件。
        source.mainRoleInfo.BaseInfo.MapId = TeamManager.MoshentaMapId;
        source.mainRoleInfo.BaseInfo.InstanceId = 7;
        source.mainRoleInfo.BaseInfo.Pos = "0_0_0";
        observer.mainRoleInfo.BaseInfo.MapId = TeamManager.MoshentaMapId;
        observer.mainRoleInfo.BaseInfo.InstanceId = 7;
        observer.mainRoleInfo.BaseInfo.Pos = "1_0_1";
        Expect(RoleManager.IsNearbyRole(observer, source.mainRoleInfo), "同实例相邻角色应收件");

        // 同地图不同实例：不应收件。
        observer.mainRoleInfo.BaseInfo.InstanceId = 8;
        Expect(!RoleManager.IsNearbyRole(observer, source.mainRoleInfo), "不同实例不收件");

        // 同实例超出100米：不应收件。
        observer.mainRoleInfo.BaseInfo.InstanceId = 7;
        observer.mainRoleInfo.BaseInfo.Pos = "500_0_0";
        Expect(!RoleManager.IsNearbyRole(observer, source.mainRoleInfo), "超出100米不收件");

        // 不同地图：不应收件。
        observer.mainRoleInfo.BaseInfo.Pos = "1_0_1";
        observer.mainRoleInfo.BaseInfo.MapId = 0;
        Expect(!RoleManager.IsNearbyRole(observer, source.mainRoleInfo), "不同地图不收件");
    }

    /// <summary>验证离队清理队伍关系，队长离队后队长转移给剩余首人。</summary>
    private static void CheckLeaveAndCaptainTransfer()
    {
        // 队长：角色ID 1801，本用例中主动离队以触发队长转移。
        OnlineRole captain = NewRole(1801, "转交队长");
        // 首名队员：角色ID 1802，队长离队后应接管队长。
        OnlineRole first = NewRole(1802, "首名队员");
        // 次名队员：角色ID 1803，用于验证掉线清理走同一路径。
        OnlineRole second = NewRole(1803, "次名队员");
        // 转交用例的队伍ID，三人共用，便于比较成员数变化。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);
        TeamManager.Instance.InviteJoinTeam(captain, 1802UL);
        TeamManager.Instance.AcceptTeamInvatation(first, teamId, 1801UL, true);
        TeamManager.Instance.InviteJoinTeam(captain, 1803UL);
        TeamManager.Instance.AcceptTeamInvatation(second, teamId, 1801UL, true);

        // 队长离队结果：true 表示确实移除了队伍关系，随后队长应转给剩余首人。
        bool left = TeamManager.Instance.LeaveTeam(1801UL);
        Expect(left, "队长离队成功");
        Expect(TeamManager.Instance.GetTeamId(1801UL) == 0UL, "离队者不再有队伍");
        Expect(TeamManager.Instance.GetCaptainId(teamId) == 1802UL, "队长转给剩余首人");
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 2, "剩余成员数为2");

        // 掉线清理走同一路径且不向失效会话发包。
        TeamManager.Instance.OnRoleOffline(1803UL);
        Expect(TeamManager.Instance.GetTeamId(1803UL) == 0UL, "掉线角色被清理出队伍");
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 1, "掉线后剩余1人");

        TeamManager.Instance.LeaveTeam(1802UL);
    }

    /// <summary>验证最后一名成员离队后队伍解散、实例回收。</summary>
    private static void CheckLastMemberDissolve()
    {
        // 解散用例的角色：角色ID 1901，作为队伍唯一成员，离队即触发解散。
        OnlineRole solo = NewRole(1901, "末人角色");
        // 解散用例的队伍ID，用于校验解散后成员数与队长都归零。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(solo, 1000);
        TeamManager.Instance.CaptainRequestEnterFBPtc(solo, TeamManager.MoshentaDungeonId);
        Expect(TeamManager.Instance.GetInstanceId(1901UL) != 0UL, "入场后有实例");

        TeamManager.Instance.LeaveTeam(1901UL);
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 0, "末人离队后队伍解散");
        Expect(TeamManager.Instance.GetCaptainId(teamId) == 0UL, "解散后无队长");
        Expect(TeamManager.Instance.GetInstanceId(1901UL) == 0UL, "解散后实例归零");
    }

    /// <summary>验证重复挑战不重新分配实例，进入世界不清除已有实例。</summary>
    private static void CheckRepeatEnterKeepsInstance()
    {
        // 重复进入用例的角色：角色ID 2001，单人成队后连续挑战两次。
        OnlineRole solo = NewRole(2001, "重复进入");
        TeamManager.Instance.RequestCreateTeam(solo, 1000);
        TeamManager.Instance.CaptainRequestEnterFBPtc(solo, TeamManager.MoshentaDungeonId);
        // 首次挑战分配的实例ID，作为"重复挑战必须复用"的比对基准。
        ulong first = TeamManager.Instance.GetInstanceId(2001UL);
        Expect(first != 0UL, "首次入场有实例");

        // 重复挑战复用同一实例，且服务器权威字段必须真的落到 BaseInfo 上。
        TeamManager.Instance.CaptainRequestEnterFBPtc(solo, TeamManager.MoshentaDungeonId);
        Expect(TeamManager.Instance.GetInstanceId(2001UL) == first, "重复挑战复用同一实例");
        Expect(solo.mainRoleInfo.BaseInfo.InstanceId != 0, "入场后 BaseInfo.InstanceId 非0");
        Expect(solo.mainRoleInfo.BaseInfo.InstanceId == (int)first, "BaseInfo.InstanceId 与队伍实例一致");
        Expect(solo.mainRoleInfo.BaseInfo.MapId == TeamManager.MoshentaMapId, "入场后 BaseInfo.MapId 为魔神塔");
        Expect(solo.mainRoleInfo.BaseInfo.Pos == TeamManager.MoshentaSpawnPos, "入场后 BaseInfo.Pos 为场景真实出生点");

        TeamManager.Instance.LeaveTeam(2001UL);
        Expect(solo.mainRoleInfo.BaseInfo.InstanceId == 0, "离队后 BaseInfo.InstanceId 归零");
        Expect(solo.mainRoleInfo.BaseInfo.MapId == TeamManager.MainCityMapId, "离队后 BaseInfo.MapId 回到主城");
        Expect(solo.mainRoleInfo.BaseInfo.Pos == TeamManager.MainCitySpawnPos, "离队后 BaseInfo.Pos 为主城真实出生点");
    }

    /// <summary>验证出生点常量与场景真实坐标一致，且同队成员进入后位置完全一致。</summary>
    private static void CheckSpawnCoordinates()
    {
        // 魔神塔 RoleSpawnPoint 世界坐标来自 sc_dungeon_moshenta_001.unity。
        Expect(TeamManager.MoshentaSpawnPos == "132.97_58.89_60.95", "魔神塔出生点与场景 RoleSpawnPoint 一致");
        // 主城出生坐标来自 MainCityMgr.ResolveMainRoleSpawnPosition 的原有硬编码值。
        Expect(TeamManager.MainCitySpawnPos == "62.1493416_19.4139996_80.6689758", "主城出生点与 MainCityMgr 默认坐标一致");

        // 坐标用例的队长：角色ID 2101，负责建队并挑战入场。
        OnlineRole captain = NewRole(2101, "坐标队长");
        // 坐标用例的队员：角色ID 2102，用于和队长比较入场后的位置与实例。
        OnlineRole member = NewRole(2102, "坐标队员");
        // 坐标用例的队伍ID，两人共用同一实例。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);
        TeamManager.Instance.InviteJoinTeam(captain, 2102UL);
        TeamManager.Instance.AcceptTeamInvatation(member, teamId, 2101UL, true);
        TeamManager.Instance.CaptainRequestEnterFBPtc(captain, TeamManager.MoshentaDungeonId);

        Expect(captain.mainRoleInfo.BaseInfo.Pos == member.mainRoleInfo.BaseInfo.Pos, "同队成员入场位置一致");
        Expect(captain.mainRoleInfo.BaseInfo.InstanceId == member.mainRoleInfo.BaseInfo.InstanceId,
            "同队成员实例一致");
        TeamManager.Instance.LeaveTeam(2102UL);
        TeamManager.Instance.LeaveTeam(2101UL);
    }

    /// <summary>
    /// 验证主城普通场景主动离队只清队伍关系：不广播离开、不改写位置，离队后双方仍互为可见。
    /// 这是"主城退队后队友再也看不到我"的定向回归，断言同时覆盖帧与服务器权威字段。
    /// </summary>
    private static void CheckMainCityLeaveKeepsVisibility()
    {
        // 离队者甲：角色ID 3001，队长，在主城原地离队，是本用例的观察对象。
        OnlineRole leaver = NewRole(3001, "主城离队甲");
        // 旁观者乙：角色ID 3002，与甲同处主城且相邻，用于验证不会收到甲的离开通知。
        OnlineRole observer = NewRole(3002, "主城旁观乙");
        // 回环链路：甲占 Unity 会话 1，乙占 Unity 会话 2，服务端写出的真实帧都在这条连接上。
        LoopbackLink link = new LoopbackLink(true);
        leaver.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        leaver.mainRoleInfo.BaseInfo.InstanceId = 0;
        leaver.mainRoleInfo.BaseInfo.Pos = "10_0_10";
        observer.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        observer.mainRoleInfo.BaseInfo.InstanceId = 0;
        observer.mainRoleInfo.BaseInfo.Pos = "12_0_10";
        leaver.GateSessionId = link.SessionId;
        leaver.UnitySessionId = 1;
        observer.GateSessionId = link.SessionId;
        observer.UnitySessionId = 2;

        // 先建队并让乙入队，确保离队分支确实有队伍关系可清。
        // 甲建队得到的队伍ID，后续用它查队长与成员数，验证主城离队只清关系、不改写位置。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(leaver, 1000);
        TeamManager.Instance.InviteJoinTeam(leaver, 3002UL);
        TeamManager.Instance.AcceptTeamInvatation(observer, teamId, 3001UL, true);
        // 离队前的位置快照，用于比对离队后坐标是否被改写。
        string posBefore = leaver.mainRoleInfo.BaseInfo.Pos;
        link.ClearFrames();

        // 甲在主城主动离队：只应清队伍关系，不应广播离开，也不应被传送回出生点。
        TeamManager.Instance.LeaveTeam(3001UL);
        // 离队后窗口内收到的全部真实帧，用于负向断言"乙没有收到离开通知"。
        List<DecodedFrame> frames = link.WaitWindow(800);
        // 先确认帧链路真的有下行，再断言没有离开通知，避免"收不到任何包"被误判成通过。
        Expect(HasFrame(frames, NetDefine.CMD_TeamInfoCode, 2), "主城离队后乙仍收到队伍状态刷新");
        Expect(!HasFrame(frames, NetDefine.CMD_RoleLeaveCode, 2), "主城离队未向同场景观察者广播 RoleLeave");
        Expect(TeamManager.Instance.GetTeamId(3001UL) == 0UL, "主城离队后甲不在队伍");
        Expect(leaver.mainRoleInfo.BaseInfo.MapId == TeamManager.MainCityMapId, "主城离队后地图未被改写");
        Expect(leaver.mainRoleInfo.BaseInfo.InstanceId == 0, "主城离队后实例未被改写");
        Expect(leaver.mainRoleInfo.BaseInfo.Pos == posBefore, "主城离队后坐标未被传送回出生点");
        Expect(RoleManager.IsNearbyRole(observer, leaver.mainRoleInfo), "主城离队后乙仍与甲互为可见");

        // 离队后甲继续出现与移动，乙仍应收件，证明可见链路没有被离队切断。
        link.ClearFrames();
        RoleManager.Instance.BroadcastRoleEnterWorldByRole(leaver);
        // 甲重新出现后乙收到的出现通知帧，用于证明离队没有切断乙的可见链路。
        List<DecodedFrame> appearFrames = link.WaitFrames(NetDefine.CMD_SyncotherOnlineCode, 2, 1200);
        Expect(HasFrame(appearFrames, NetDefine.CMD_SyncotherOnlineCode, 2), "主城离队后乙仍能收到甲的出现通知");
        link.ClearFrames();
        RoleManager.Instance.SyncMove(leaver, new SyncMoveNtf());
        // 甲移动后乙收到的移动同步帧，用于证明离队同样没有切断乙的移动收件链路。
        List<DecodedFrame> moveFrames = link.WaitFrames(NetDefine.CMD_SyncMoveCode, 2, 1200);
        Expect(HasFrame(moveFrames, NetDefine.CMD_SyncMoveCode, 2), "主城离队后乙仍能收到甲的移动同步");

        // 队长离队后队长转给剩余首人，随后乙离队解散队伍。
        Expect(TeamManager.Instance.GetCaptainId(teamId) == 3002UL, "主城队长离队后队长转给剩余成员");
        TeamManager.Instance.LeaveTeam(3002UL);
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 0, "主城末人离队后队伍解散");
        link.Close();
    }

    /// <summary>验证非队长在主城离队同样不广播离开、不动位置，且队伍仍由队长保留。</summary>
    private static void CheckNonCaptainMainCityLeaveKeepsTeam()
    {
        // 队长：角色ID 3301，离队分支中不参与，用于验证队伍在他名下继续存在。
        OnlineRole captain = NewRole(3301, "留任队长");
        // 非队长成员：角色ID 3302，本用例中主动离队。
        OnlineRole member = NewRole(3302, "离队队员");
        // 回环链路：队长占 Unity 会话 1，离队队员占会话 2，服务端写出的真实帧都在这条连接上。
        LoopbackLink link = new LoopbackLink(true);
        captain.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        captain.mainRoleInfo.BaseInfo.InstanceId = 0;
        captain.mainRoleInfo.BaseInfo.Pos = "30_0_30";
        member.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        member.mainRoleInfo.BaseInfo.InstanceId = 0;
        member.mainRoleInfo.BaseInfo.Pos = "32_0_30";
        captain.GateSessionId = link.SessionId;
        captain.UnitySessionId = 1;
        member.GateSessionId = link.SessionId;
        member.UnitySessionId = 2;

        // 队长建队得到的队伍ID，用于断言非队长离队后队伍仍在队长名下保留。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);
        TeamManager.Instance.InviteJoinTeam(captain, 3302UL);
        TeamManager.Instance.AcceptTeamInvatation(member, teamId, 3301UL, true);
        // 离队前的坐标快照，用于比对非队长离队是否被传送。
        string posBefore = member.mainRoleInfo.BaseInfo.Pos;
        link.ClearFrames();

        TeamManager.Instance.LeaveTeam(3302UL);
        // 非队长离队后窗口内的全部真实帧，用于负向断言"队长没有收到离开通知"。
        List<DecodedFrame> frames = link.WaitWindow(800);
        // 先确认帧链路真的有下行，再断言没有离开通知，避免"收不到任何包"被误判成通过。
        Expect(HasFrame(frames, NetDefine.CMD_TeamInfoCode, 1), "非队长主城离队后队长仍收到队伍状态刷新");
        Expect(!HasFrame(frames, NetDefine.CMD_RoleLeaveCode, 1), "非队长主城离队未向队长广播 RoleLeave");
        Expect(member.mainRoleInfo.BaseInfo.Pos == posBefore, "非队长主城离队后坐标未被改写");
        Expect(TeamManager.Instance.GetTeamId(3302UL) == 0UL, "非队长离队后不再有队伍");
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 1, "非队长离队后队伍仍保留队长");
        Expect(TeamManager.Instance.GetCaptainId(teamId) == 3301UL, "非队长离队不改变队长");

        TeamManager.Instance.LeaveTeam(3301UL);
        link.Close();
    }

    /// <summary>验证主城单人成队后离队即解散，同样不广播离开、不传送回出生点。</summary>
    private static void CheckSoloMainCityLeaveDissolvesWithoutTeleport()
    {
        // 单人角色：角色ID 3401，主城原地建队后离队，队伍应直接解散。
        OnlineRole solo = NewRole(3401, "主城单人");
        // 回环链路：单人角色占 Unity 会话 1，服务端写出的真实帧都在这条连接上。
        LoopbackLink link = new LoopbackLink(true);
        solo.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        solo.mainRoleInfo.BaseInfo.InstanceId = 0;
        solo.mainRoleInfo.BaseInfo.Pos = "40_0_40";
        solo.GateSessionId = link.SessionId;
        solo.UnitySessionId = 1;
        // 单人建队得到的队伍ID，用于断言末人离队后队伍成员数归零即解散。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(solo, 1000);
        // 离队前的坐标快照，用于比对单人离队是否被传送。
        string posBefore = solo.mainRoleInfo.BaseInfo.Pos;
        link.ClearFrames();

        TeamManager.Instance.LeaveTeam(3401UL);
        // 单人离队后窗口内的全部真实帧，用于负向断言"本人也没有收到离开通知"。
        List<DecodedFrame> frames = link.WaitWindow(800);
        // 先确认帧链路真的有下行，再断言没有离开通知，避免"收不到任何包"被误判成通过。
        Expect(HasFrame(frames, NetDefine.CMD_TeamInfoCode, 1), "主城单人离队后本人仍收到空队伍状态");
        Expect(!HasFrame(frames, NetDefine.CMD_RoleLeaveCode, 1), "主城单人离队未向本人广播 RoleLeave");
        Expect(TeamManager.Instance.GetMemberCount(teamId) == 0, "主城单人离队后队伍解散");
        Expect(solo.mainRoleInfo.BaseInfo.Pos == posBefore, "主城单人离队后坐标未被传送");
        Expect(solo.mainRoleInfo.BaseInfo.MapId == TeamManager.MainCityMapId, "主城单人离队后仍在主城");
        link.Close();
    }

    /// <summary>
    /// 验证副本内离队仍按旧实例广播离开、服务器赋主城出生点并通知回城，
    /// 且回城后主城观察者能收到该角色的出现，不能因为修主城退队 bug 把副本分支一起关掉。
    /// </summary>
    private static void CheckDungeonLeaveBroadcastsLeaveAndReturns()
    {
        // 副本队长：角色ID 3101，留在副本实例内，是"旧实例观察者"。
        OnlineRole captain = NewRole(3101, "副本队长");
        // 副本队员：角色ID 3102，本用例中在副本内离队。
        OnlineRole member = NewRole(3102, "副本离队队员");
        // 主城观察者：角色ID 3103，守在主城出生点，用于验证回城后能收到出现通知。
        OnlineRole cityObserver = NewRole(3103, "主城出生点观察者");
        // 回环链路：队长、队员、主城观察者分别占 Unity 会话 1、2、3，三者的真实帧共用这条连接。
        LoopbackLink link = new LoopbackLink(true);
        captain.GateSessionId = link.SessionId;
        captain.UnitySessionId = 1;
        member.GateSessionId = link.SessionId;
        member.UnitySessionId = 2;
        cityObserver.GateSessionId = link.SessionId;
        cityObserver.UnitySessionId = 3;
        cityObserver.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        cityObserver.mainRoleInfo.BaseInfo.InstanceId = 0;
        cityObserver.mainRoleInfo.BaseInfo.Pos = TeamManager.MainCitySpawnPos;

        // 副本队长建队得到的队伍ID，用于组队入场并在断言里查成员与队长状态。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(captain, 1000);
        TeamManager.Instance.InviteJoinTeam(captain, 3102UL);
        TeamManager.Instance.AcceptTeamInvatation(member, teamId, 3101UL, true);
        TeamManager.Instance.CaptainRequestEnterFBPtc(captain, TeamManager.MoshentaDungeonId);
        Expect(member.mainRoleInfo.BaseInfo.InstanceId != 0, "入场后队员实例非0");
        link.ClearFrames();

        // 队员在副本内离队：旧实例观察者必须收到离开，本人必须回城，主城观察者必须收到出现。
        TeamManager.Instance.LeaveTeam(3102UL);
        // 离队是一次同步调用，离开、回城与出现通知会在同一批写出；
        // 先等离开帧，再补收一个窗口并把补收帧并入同一份列表，避免先出现的帧被前一次等待消费掉。
        List<DecodedFrame> frames = link.WaitFrames(NetDefine.CMD_RoleLeaveCode, 1, 1500);
        // 等待离开帧期间可能已经写出的回城与出现通知，补收后并入 frames，防止断言漏帧。
        List<DecodedFrame> extraFrames = link.WaitWindow(400);
        // index 为补收帧列表的零基下标，范围 [0, extraFrames.Count)，只用于合并两批帧。
        for (int index = 0; index < extraFrames.Count; index++)
        {
            frames.Add(extraFrames[index]);
        }
        Expect(HasFrame(frames, NetDefine.CMD_RoleLeaveCode, 1), "副本内离队向同实例观察者广播 RoleLeave");
        Expect(HasFrame(frames, NetDefine.CMD_TeamEnterDungeonCode, 2), "副本内离队向本人下发回城通知");
        Expect(HasFrame(frames, NetDefine.CMD_SyncotherOnlineCode, 3), "副本内离队后主城观察者收到出现通知");
        Expect(member.mainRoleInfo.BaseInfo.InstanceId == 0, "副本内离队后实例归零");
        Expect(member.mainRoleInfo.BaseInfo.MapId == TeamManager.MainCityMapId, "副本内离队后回到主城地图");
        Expect(member.mainRoleInfo.BaseInfo.Pos == TeamManager.MainCitySpawnPos, "副本内离队后赋主城出生点");

        TeamManager.Instance.LeaveTeam(3101UL);
        link.Close();
    }

    /// <summary>
    /// 验证主城掉线清理仍会向观察者广播离开并复位位置，
    /// 不能因为修主城主动退队 bug 把掉线的幽灵角色清理一起关掉。
    /// </summary>
    private static void CheckOfflineInMainCityStillRemovesRole()
    {
        // 掉线角色：角色ID 3201，主城内掉线，观察者应删掉他的远端模型。
        OnlineRole offlineRole = NewRole(3201, "主城掉线甲");
        // 旁观者：角色ID 3202，与掉线者同处主城且相邻。
        OnlineRole observer = NewRole(3202, "主城旁观乙");
        // 回环链路：掉线者占 Unity 会话 1，旁观者占会话 2，掉线清理写出的真实帧都在这条连接上。
        LoopbackLink link = new LoopbackLink(true);
        offlineRole.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        offlineRole.mainRoleInfo.BaseInfo.InstanceId = 0;
        offlineRole.mainRoleInfo.BaseInfo.Pos = "20_0_20";
        observer.mainRoleInfo.BaseInfo.MapId = TeamManager.MainCityMapId;
        observer.mainRoleInfo.BaseInfo.InstanceId = 0;
        observer.mainRoleInfo.BaseInfo.Pos = "22_0_20";
        offlineRole.GateSessionId = link.SessionId;
        offlineRole.UnitySessionId = 1;
        observer.GateSessionId = link.SessionId;
        observer.UnitySessionId = 2;

        // 掉线者建队得到的队伍ID，用于断言掉线清理会把他从队伍索引里移除。
        ulong teamId = TeamManager.Instance.RequestCreateTeam(offlineRole, 1000);
        TeamManager.Instance.InviteJoinTeam(offlineRole, 3202UL);
        TeamManager.Instance.AcceptTeamInvatation(observer, teamId, 3201UL, true);
        link.ClearFrames();

        TeamManager.Instance.OnRoleOffline(3201UL);
        // 掉线清理后等到的离开通知帧，用于正向断言"掉线仍会让观察者删掉远端角色"。
        List<DecodedFrame> frames = link.WaitFrames(NetDefine.CMD_RoleLeaveCode, 2, 1500);
        Expect(HasFrame(frames, NetDefine.CMD_RoleLeaveCode, 2), "主城掉线仍向观察者广播 RoleLeave");
        Expect(TeamManager.Instance.GetTeamId(3201UL) == 0UL, "主城掉线后被清理出队伍");

        TeamManager.Instance.LeaveTeam(3202UL);
        link.Close();
    }

    /// <summary>判断已解码帧集合中是否存在指定协议号且发往指定 Unity 会话的帧。</summary>
    /// <param name="frames">已解码帧列表，可能为空。</param>
    /// <param name="protoCode">期望协议号，对应 NetDefine 的 CMD_* 常量。</param>
    /// <param name="unitySessionId">期望目标 Unity 会话号。</param>
    /// <returns>存在该帧返回 true。</returns>
    private static bool HasFrame(List<DecodedFrame> frames, int protoCode, int unitySessionId)
    {
        // index 为帧列表零基下标，范围 [0, frames.Count)，只服务于本次线性查找。
        for (int index = 0; index < frames.Count; index++)
        {
            // 当前帧，可能为空；协议号与目标会话同时匹配才算命中。
            DecodedFrame frame = frames[index];
            if (frame != null && frame.ProtoCode == protoCode && frame.UnitySessionId == unitySessionId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>构造一名仅含基础信息的在线角色，并注册进 RoleManager 现有在线索引。</summary>
    /// <param name="roleId">角色ID，作为服务端权威身份。</param>
    /// <param name="nickname">角色昵称，用于队伍通知载荷。</param>
    /// <returns>可直接传给 TeamManager 的在线角色实例。</returns>
    private static OnlineRole NewRole(int roleId, string nickname)
    {
        // 待构造的测试角色实例，基础信息在下面逐项填充，最后注册进在线索引。
        OnlineRole role = new OnlineRole();
        role.mainRoleInfo = new MainRoleInfo();
        role.mainRoleInfo.BaseInfo = new RoleBaseInfo();
        role.mainRoleInfo.BaseInfo.RoleId = roleId;
        role.mainRoleInfo.BaseInfo.Nickname = nickname;
        role.mainRoleInfo.BaseInfo.Level = 20;
        role.mainRoleInfo.BaseInfo.JobId = 1;
        role.mainRoleInfo.BaseInfo.CurrHp = 100;
        role.mainRoleInfo.BaseInfo.MaxHp = 100;
        role.mainRoleInfo.BaseInfo.Pos = "0_0_0";
        RegisterOnlineRole(role);
        return role;
    }

    /// <summary>
    /// 把测试角色注册进 RoleManager 的在线角色索引。
    /// 仅测试使用反射：m_onlineRoleMap 是 RoleManager 私有字段，测试不为此在生产代码新增访问入口，
    /// 也不访问数据库、不建立会话，只写入内存索引并把状态置为 Online，
    /// 这样 TeamManager 写 BaseInfo 时不会被"角色不在线"分支跳过。
    /// </summary>
    /// <param name="role">需要注册的测试角色。</param>
    private static void RegisterOnlineRole(OnlineRole role)
    {
        // 反射取得的私有字段描述符，只用于读写在线角色索引，不新增生产访问入口。
        FieldInfo field = typeof(RoleManager).GetField("m_onlineRoleMap",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException("RoleManager.m_onlineRoleMap 字段未找到");
        }
        // 以非泛型字典接口访问在线索引，避免测试依赖私有字段的泛型实参类型。
        IDictionary map = field.GetValue(RoleManager.Instance) as IDictionary;
        if (map == null)
        {
            throw new InvalidOperationException("RoleManager.m_onlineRoleMap 不是字典");
        }
        role.State = OnlineRoleState.Online;
        map[role.mainRoleInfo.BaseInfo.RoleId] = role;
        _registeredRoles.Add(role);
    }

    /// <summary>测试结束时把注册过的角色从在线索引移除，避免影响同进程其他用例。</summary>
    private static void UnregisterAllRoles()
    {
        // 反射取得的私有字段描述符，仅用于清空测试写入的在线索引。
        FieldInfo field = typeof(RoleManager).GetField("m_onlineRoleMap",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            return;
        }
        // 以非泛型字典接口访问在线索引，避免测试依赖私有字段的泛型实参类型。
        IDictionary map = field.GetValue(RoleManager.Instance) as IDictionary;
        if (map == null)
        {
            return;
        }
        // index 为本测试已注册角色列表的零基下标，逐个从在线索引里移除。
        for (int index = 0; index < _registeredRoles.Count; index++)
        {
            // 当前待移除的测试角色，为空或基础信息缺失时跳过。
            OnlineRole role = _registeredRoles[index];
            if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
            {
                continue;
            }
            map.Remove(role.mainRoleInfo.BaseInfo.RoleId);
        }
        _registeredRoles.Clear();
    }

    /// <summary>
    /// 包级验证：用回环 TCP 会话接收服务端真实写出的字节帧。
    /// 测试侧自行建立回环 Socket 并按现有 Session 构造签名注册，生产 Send 未增加任何 Mock 钩子。
    /// 只断言实际收到的字节数，同实例观察者应收到帧、异实例观察者不应收到任何字节。
    /// </summary>
    private static void CheckWireDeliveryByLoopbackSocket()
    {
        // 本机回环监听器，端口由系统分配，用完即停，不占用正式服端口。
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        // 测试侧回环客户端，用它的接收流统计服务端真实写出的字节。
        TcpClient client = new TcpClient();
        client.Connect((IPEndPoint)listener.LocalEndpoint);
        // 监听器接受的套接字，交给 Session 作为接收端，构成一条完整的回环 TCP 连接。
        Socket accepted = listener.AcceptSocket();
        // 与正式服同一条 Session 构造路径，SessionMgr 自动分配会话 ID。
        Session session = new Session(new Dictionary<int, IContainer>(), null);
        session.ReceiveData(accepted);

        // 包级来源角色：角色ID 2901，固定在魔神塔实例 7，触发离开广播。
        OnlineRole source = NewRole(2901, "包级来源");
        // 同实例观察者：角色ID 2902，绑定本回环会话的 Unity 会话 1，应收到真实帧。
        OnlineRole sameInstance = NewRole(2902, "同实例观察者");
        // 异实例观察者：角色ID 2903，绑定本回环会话的 Unity 会话 2，不应收到任何字节。
        OnlineRole otherInstance = NewRole(2903, "异实例观察者");
        source.mainRoleInfo.BaseInfo.MapId = TeamManager.MoshentaMapId;
        source.mainRoleInfo.BaseInfo.InstanceId = 7;
        source.mainRoleInfo.BaseInfo.Pos = "0_0_0";
        sameInstance.mainRoleInfo.BaseInfo.MapId = TeamManager.MoshentaMapId;
        sameInstance.mainRoleInfo.BaseInfo.InstanceId = 7;
        sameInstance.mainRoleInfo.BaseInfo.Pos = "1_0_1";
        otherInstance.mainRoleInfo.BaseInfo.MapId = TeamManager.MoshentaMapId;
        otherInstance.mainRoleInfo.BaseInfo.InstanceId = 8;
        otherInstance.mainRoleInfo.BaseInfo.Pos = "1_0_1";
        RegisterOnlineRole(source);
        RegisterOnlineRole(sameInstance);
        RegisterOnlineRole(otherInstance);
        sameInstance.GateSessionId = session.SessionID;
        sameInstance.UnitySessionId = 1;
        otherInstance.GateSessionId = session.SessionID;
        otherInstance.UnitySessionId = 2;

        // 场景一：同实例观察者应收件。
        RoleManager.Instance.BroadcastRoleLeave(source);
        // 回环连接上实际收到的字节数，大于 0 说明帧确实写到了 TCP。
        int receivedBytes = DrainSocketBytes(client, 500);
        Expect(receivedBytes > 0, "同实例观察者在回环 TCP 上收到 RoleLeave 真实帧，字节数=" +
            receivedBytes.ToString());

        // 场景二：两个观察者都改为异实例后不应再收到任何字节。
        sameInstance.mainRoleInfo.BaseInfo.InstanceId = 9;
        RoleManager.Instance.BroadcastRoleLeave(source);
        // 观察窗口内收到的字节数，应为 0，证明异实例被过滤掉。
        int blockedBytes = DrainSocketBytes(client, 500);
        Expect(blockedBytes == 0, "异实例观察者未收到任何字节，字节数=" +
            blockedBytes.ToString());

        SessionMgr.Instance.RemoveSession(session.SessionID);
        client.Close();
        listener.Stop();
    }

    /// <summary>读取回环连接上已经到达的字节总数，读超时即视为本轮无更多数据。</summary>
    /// <param name="client">测试侧回环客户端。</param>
    /// <param name="timeoutMs">单次读取超时毫秒数。</param>
    /// <returns>累计收到的字节数。</returns>
    private static int DrainSocketBytes(TcpClient client, int timeoutMs)
    {
        // 回环连接的接收流，读超时由入参控制，超时即视为本轮无更多数据。
        NetworkStream stream = client.GetStream();
        stream.ReadTimeout = timeoutMs;
        // 单次读取缓冲区，大小取 4096 足以容纳本测试的若干小帧。
        byte[] buffer = new byte[4096];
        // 本轮累计读到的字节总数，循环结束后返回。
        int total = 0;
        while (true)
        {
            try
            {
                // 本次读取返回的字节数，小于等于 0 表示对端已关闭或没有数据。
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }
                total = total + read;
            }
            catch (System.IO.IOException)
            {
                break;
            }
        }
        return total;
    }

    /// <summary>记录一条断言结果。</summary>
    /// <param name="condition">断言条件，为 true 表示通过。</param>
    /// <param name="message">断言说明，失败时输出。</param>
    private static void Expect(bool condition, string message)
    {
        _checks = _checks + 1;
        if (condition)
        {
            Console.WriteLine("[PASS] " + message);
            return;
        }
        _failures = _failures + 1;
        Console.WriteLine("[FAIL] " + message);
    }
}
