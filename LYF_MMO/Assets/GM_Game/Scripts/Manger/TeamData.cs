using System;
using System.Collections.Generic;

/// <summary>
/// 客户端队伍数据缓存，对应 RO TeamData.lua 的成员表与队长信息。
/// 只保存服务器下发过的真实状态，不做本地权威判定，也不直接发送协议。
/// </summary>
public sealed class TeamData
{
    /// <summary>单例，全局只保留一份队伍状态。</summary>
    private static readonly TeamData _instance = new TeamData();

    /// <summary>当前队伍成员列表，顺序与服务器下发一致，第一项通常是队长。</summary>
    private readonly List<TeamMemberInfo> _members = new List<TeamMemberInfo>();

    /// <summary>本地主角角色ID，由协议控制器在每次下行时写入，用于区分邀请与申请。</summary>
    private ulong _localRoleId;

    /// <summary>最近一次收到邀请的队伍ID，0 表示没有待处理邀请。</summary>
    private ulong _inviteTeamId;

    /// <summary>最近一次邀请的邀请人角色ID。</summary>
    private ulong _inviteInviterId;

    /// <summary>最近一次邀请的邀请人昵称，用于邀请提示显示。</summary>
    private string _inviteInviterName = string.Empty;

    /// <summary>最近一次收到申请的队伍ID，0 表示没有待审批申请。</summary>
    private ulong _applyTeamId;

    /// <summary>最近一次申请人的角色ID。</summary>
    private ulong _applyApplicantId;

    /// <summary>最近一次申请人的昵称，用于申请列表显示。</summary>
    private string _applyApplicantName = string.Empty;

    /// <summary>队伍状态变化事件，参数为是否仍在队伍中。</summary>
    public event Action<bool> TeamInfoChanged;

    /// <summary>收到邀请或申请事件，无参数，界面据此刷新待处理入口。</summary>
    public event Action InvitationChanged;

    /// <summary>获取队伍数据单例。</summary>
    public static TeamData Instance
    {
        get { return _instance; }
    }

    /// <summary>当前队伍ID，0 表示不在队伍中。</summary>
    public ulong TeamId { get; private set; }

    /// <summary>当前队长角色ID，0 表示无队伍。</summary>
    public ulong CaptainId { get; private set; }

    /// <summary>服务器下发的队伍人数上限，未收到时按 5 处理。</summary>
    public int MaxMember { get; private set; }

    /// <summary>本地主角角色ID，0 表示尚未取得。</summary>
    public ulong LocalRoleId
    {
        get { return _localRoleId; }
    }

    /// <summary>是否已在队伍中，直接由服务器 TeamId 判定。</summary>
    public bool IsInTeam
    {
        get { return TeamId != 0UL; }
    }

    /// <summary>当前成员数量。</summary>
    public int MemberCount
    {
        get { return _members.Count; }
    }

    /// <summary>只读成员列表，供界面按服务器顺序渲染。</summary>
    public IList<TeamMemberInfo> Members
    {
        get { return _members; }
    }

    /// <summary>待处理邀请的队伍ID，0 表示没有邀请。</summary>
    public ulong InviteTeamId
    {
        get { return _inviteTeamId; }
    }

    /// <summary>待处理邀请的邀请人角色ID。</summary>
    public ulong InviteInviterId
    {
        get { return _inviteInviterId; }
    }

    /// <summary>待处理邀请的邀请人昵称。</summary>
    public string InviteInviterName
    {
        get { return _inviteInviterName; }
    }

    /// <summary>是否收到过待处理邀请。</summary>
    public bool HasInvitation
    {
        get { return _inviteTeamId != 0UL; }
    }

    /// <summary>待审批申请的队伍ID，0 表示没有申请。</summary>
    public ulong ApplyTeamId
    {
        get { return _applyTeamId; }
    }

    /// <summary>待审批申请人的角色ID。</summary>
    public ulong ApplyApplicantId
    {
        get { return _applyApplicantId; }
    }

    /// <summary>待审批申请人的昵称。</summary>
    public string ApplyApplicantName
    {
        get { return _applyApplicantName; }
    }

    /// <summary>是否存在待审批申请，只有队长会收到申请。</summary>
    public bool HasApplication
    {
        get { return _applyTeamId != 0UL; }
    }

    /// <summary>写入本地主角角色ID，供邀请与申请的来源判定使用。</summary>
    /// <param name="roleId">本地主角角色ID，0 表示未知。</param>
    public void SetLocalRoleId(ulong roleId)
    {
        _localRoleId = roleId;
    }

    /// <summary>按索引取成员信息，越界时返回 null。</summary>
    /// <param name="index">成员在服务器下发顺序中的零基下标。</param>
    /// <returns>成员信息；下标无效时返回 null。</returns>
    public TeamMemberInfo GetMember(int index)
    {
        if (index < 0 || index >= _members.Count)
        {
            return null;
        }
        return _members[index];
    }

    /// <summary>按角色ID查找成员，用于在成员表中定位本人而不是依赖下发顺序。</summary>
    /// <param name="roleId">待查找的角色ID。</param>
    /// <returns>找到的成员信息；不在队伍中时返回 null。</returns>
    public TeamMemberInfo FindMember(ulong roleId)
    {
        if (roleId == 0UL)
        {
            return null;
        }
        // 按服务器下发顺序逐个比较角色ID，找到即返回。
        // index 是成员表的零基下标，范围 [0, _members.Count)，只服务于本次线性查找，不对外暴露。
        for (int index = 0; index < _members.Count; index++)
        {
            // 当前下标的成员项；下行数据可能含空位，比较角色ID前必须先判空。
            TeamMemberInfo member = _members[index];
            if (member != null && member.RoleId == roleId)
            {
                return member;
            }
        }
        return null;
    }

    /// <summary>判断指定角色是否为当前队长。</summary>
    /// <param name="roleId">待判断的角色ID。</param>
    /// <returns>是队长返回 true。</returns>
    public bool IsCaptain(ulong roleId)
    {
        return CaptainId != 0UL && CaptainId == roleId;
    }

    /// <summary>用服务器下发的整包队伍状态覆盖本地缓存。</summary>
    /// <param name="notice">服务器 TeamInfoNtf，TeamId 为 0 表示已离队。</param>
    public void ApplyTeamInfo(TeamInfoNtf notice)
    {
        if (notice == null)
        {
            return;
        }
        TeamId = notice.TeamId;
        CaptainId = notice.CaptainId;
        MaxMember = notice.MaxMember;
        _members.Clear();
        // 按服务器顺序重建成员表，本地不排序也不补人。
        // index 是下行成员列表的零基下标，范围 [0, notice.Members.Count)，仅用于本次重建。
        for (int index = 0; index < notice.Members.Count; index++)
        {
            _members.Add(notice.Members[index]);
        }
        // 已入队则邀请与申请都不再需要保留。
        if (TeamId != 0UL)
        {
            ClearInvitation();
            ClearApplication();
        }
        if (TeamInfoChanged != null)
        {
            TeamInfoChanged(TeamId != 0UL);
        }
    }

    /// <summary>
    /// 记录一条待处理通知。服务器复用 TeamInvatationNtf 下发邀请与申请，
    /// 以 CaptainId 是否等于本地角色ID区分：等于本人表示本人是队长、收到的是申请。
    /// </summary>
    /// <param name="notice">服务器 TeamInvatationNtf。</param>
    /// <param name="localRoleId">本地主角角色ID。</param>
    public void ApplyNotice(TeamInvatationNtf notice, ulong localRoleId)
    {
        if (notice == null)
        {
            return;
        }
        SetLocalRoleId(localRoleId);
        // 队长本人收到的通知代表有人申请入队，其余情况代表本人被邀请。
        if (localRoleId != 0UL && notice.CaptainId == localRoleId)
        {
            _applyTeamId = notice.TeamId;
            _applyApplicantId = notice.InviterId;
            _applyApplicantName = notice.InviterName;
            RaiseInvitationChanged();
            return;
        }
        _inviteTeamId = notice.TeamId;
        _inviteInviterId = notice.InviterId;
        _inviteInviterName = notice.InviterName;
        RaiseInvitationChanged();
    }

    /// <summary>清除待处理邀请，用于同意、拒绝或已入队后复位界面。</summary>
    public void ClearInvitation()
    {
        _inviteTeamId = 0UL;
        _inviteInviterId = 0UL;
        _inviteInviterName = string.Empty;
        RaiseInvitationChanged();
    }

    /// <summary>清除待审批申请，用于同意、拒绝或已入队后复位界面。</summary>
    public void ClearApplication()
    {
        _applyTeamId = 0UL;
        _applyApplicantId = 0UL;
        _applyApplicantName = string.Empty;
        RaiseInvitationChanged();
    }

    /// <summary>清空全部队伍状态，用于收到空 TeamInfo 时复位界面。</summary>
    public void Clear()
    {
        ApplyTeamInfo(new TeamInfoNtf());
    }

    /// <summary>统一抛出待处理通知变化事件，避免各分支重复判空。</summary>
    private void RaiseInvitationChanged()
    {
        if (InvitationChanged != null)
        {
            InvitationChanged();
        }
    }
}
