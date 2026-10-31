using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 主题副本队伍控制器。队伍状态全部来自服务器通知，本控制器只做协议转发与界面刷新，
/// 并在收到入场通知后加载魔神塔场景，不在本地伪造队伍或场景切换。
/// </summary>
public sealed class ThemeDungeonTeamCtrl : CtrlBase
{
    /// <summary>
    /// 魔神塔在主题副本配置里的编号。RO 侧链路为 ThemeDungeonTable 第 7 条（达纳托斯之战）
    /// 指向 DungeonsTable 的 4060，再由 SceneTable 指向 sc_dungeon_moshenta_001；
    /// 本轮只把这一条绑定到魔神塔场景，其余八本不误进魔神塔。
    /// </summary>
    private const int MoshentaThemeDungeonId = 7;

    /// <summary>魔神塔在服务器 DungeonsTable 中的副本配置ID，theme=7 映射到 4060。</summary>
    private const int ServerDungeonId4060 = 4060;

    /// <summary>魔神塔场景在服务器 SceneTable 中的场景ID，与副本配置一致为 4060。</summary>
    private const int ServerSceneId4060 = 4060;

    /// <summary>
    /// 服务器下发回城通知时使用的场景ID，取值对应客户端 SceneType.Scene_MainCity 的枚举序号 2，
    /// 与服务端 TeamManager.MainCitySceneId 保持同一约定。
    /// </summary>
    private const int MainCitySceneId = 2;

    /// <summary>组队目标ID缺省值，对应 RO TeamData.autoPairTarget = 1000。</summary>
    private const int DefaultTeamTargetId = 1000;

    /// <summary>
    /// 邀请页"附近的人"可见距离上限（米）。
    /// 与服务端 RoleManager.IsNearbyRole 的 100 米口径保持一致，避免界面列出服务器认为不可见的角色。
    /// </summary>
    private const float NearbyCandidateRangeMeters = 100f;

    /// <summary>职业头像所在图集的 Resources 路径，用于按职业取已有精灵。</summary>
    private const string ProfessionAtlasResourcePath = "UI/Atlas/Common";

    /// <summary>职业头像精灵名前缀，拼上职业编号即完整精灵名。</summary>
    private const string ProfessionSpritePrefix = "UI_Common_Profession_Icon_0";

    /// <summary>主界面视图，用于取正式队伍窗、副本选择窗、详情窗与快捷队伍栏。</summary>
    private readonly MainView _mainView;

    /// <summary>正式队伍窗口实例，来自主界面序列化的正式引用。</summary>
    private readonly ThemeDungeonTeamCreateWindow _teamWindow;

    /// <summary>主界面左侧常驻快捷队伍栏，来自主界面序列化的正式引用。</summary>
    private readonly QuickTeamWidget _quickTeamWidget;

    /// <summary>队伍协议控制器，收发服务器队伍与入场协议，是本控制器的权威来源。</summary>
    private readonly TeamCtrl _teamCtrl;

    /// <summary>按职业编号缓存的头像精灵，避免每次刷新都重新查图集。</summary>
    private readonly Dictionary<int, Sprite> _jobSprites = new Dictionary<int, Sprite>();

    /// <summary>当前生命比例，供队伍窗与快捷栏显示血条。</summary>
    private float _hpRatio;

    /// <summary>当前法力比例，供快捷栏显示蓝条。</summary>
    private float _mpRatio;

    /// <summary>本次挑战选中的副本配置，由副本选择窗的挑战事件传入。</summary>
    private ThemeDungeonEntry _selectedEntry;

    /// <summary>是否已经读取过职业头像图集，避免重复加载。</summary>
    private bool _jobAtlasLoaded;

    /// <summary>已加载的职业头像精灵，按精灵名索引。</summary>
    private Sprite[] _jobAtlasSprites = new Sprite[0];

    /// <summary>绑定队伍窗、快捷队伍栏与副本选择窗事件，事件都只在本控制器内处理。</summary>
    /// <param name="view">主界面视图，必须是 MainView 才能取到正式控件引用。</param>
    public ThemeDungeonTeamCtrl(UIBase view) : base(view)
    {
        _mainView = view as MainView;
        if (_mainView == null)
        {
            return;
        }
        _teamWindow = _mainView.ThemeDungeonTeamCreateWindow;
        _quickTeamWidget = _mainView.QuickTeamWidget;
        if (_mainView.ThemeDungeonArroundWindow != null)
        {
            _mainView.ThemeDungeonArroundWindow.OnChallengeRequested += OnChallengeRequested;
        }
        if (_teamWindow != null)
        {
            _teamWindow.CreateRequested += OnCreateRequested;
            _teamWindow.CloseRequested += OnCloseRequested;
            _teamWindow.LeaveRequested += OnLeaveRequested;
            _teamWindow.InviteRoleRequested += OnInviteRoleRequested;
            _teamWindow.ApplyTeamRequested += OnApplyTeamRequested;
            _teamWindow.InviteAgreed += OnInviteAgreed;
            _teamWindow.InviteRejected += OnInviteRejected;
            _teamWindow.ApplicationAgreed += OnApplicationAgreed;
            _teamWindow.ApplicationRejected += OnApplicationRejected;
            _teamWindow.PageOpened += OnPageOpened;
        }
        if (_quickTeamWidget != null)
        {
            _quickTeamWidget.OpenTeamRequested += OnOpenTeamRequested;
            _quickTeamWidget.CreateTeamRequested += OnCreateRequested;
            _quickTeamWidget.OpenPageRequested += OnOpenPageRequested;
        }
        // 队伍状态全部来自服务器通知，控制器只做界面转发。
        _teamCtrl = new TeamCtrl(view);
        _teamCtrl.TeamInfoReceived += OnTeamInfoReceived;
        _teamCtrl.InvitationReceived += OnInvitationReceived;
        _teamCtrl.EnterDungeonReceived += OnEnterDungeonReceived;
        RefreshViews();
    }

    /// <summary>
    /// 详情页「开始挑战」：已有队伍直接向服务器申请进入魔神塔，没有队伍先打开正式队伍窗建队。
    /// 建队不会自动进图，必须再点一次开始挑战。
    /// </summary>
    /// <param name="entry">详情页当前确认挑战的副本配置。</param>
    private void OnChallengeRequested(ThemeDungeonEntry entry)
    {
        if (entry == null)
        {
            return;
        }
        _selectedEntry = entry;
        // 只开放已授权的魔神塔，其余主题本不走服务器入场。
        if (entry.id != MoshentaThemeDungeonId)
        {
            return;
        }
        if (TeamData.Instance.IsInTeam)
        {
            // 已有队伍时由队长向服务器申请入场，收到入场通知后才加载场景。
            _teamCtrl.CaptainRequestEnterFBPtc(ServerDungeonId4060);
            return;
        }
        OpenTeamWindow();
    }

    /// <summary>快捷栏「组队」：打开正式队伍窗，不改变当前选中副本。</summary>
    private void OnOpenTeamRequested()
    {
        OpenTeamWindow();
    }

    /// <summary>展开正式队伍窗，窗口实例由主界面持有，重复打开不会被收起。</summary>
    private void OpenTeamWindow()
    {
        if (_teamWindow == null)
        {
            return;
        }
        if (!_mainView.gameObject.activeSelf)
        {
            _mainView.Show();
        }
        RefreshViews();
        _teamWindow.transform.SetAsLastSibling();
        _teamWindow.Show(true);
        _teamWindow.OpenPage(TeamPage.Members);
    }

    /// <summary>从快捷栏打开已迁移的 RO 界面壳，只处理界面导航。</summary>
    /// <param name="page">快捷栏按钮对应的队伍界面。</param>
    private void OnOpenPageRequested(TeamPage page)
    {
        OpenTeamWindow();
        _teamWindow.OpenPage(page);
    }

    /// <summary>向服务器请求建队，成员列表以服务器回包为准。</summary>
    private void OnCreateRequested()
    {
        if (TeamData.Instance.IsInTeam)
        {
            return;
        }
        if (!_teamCtrl.RequestCreateTeam(DefaultTeamTargetId))
        {
            return;
        }
        if (_mainView.QuickPanel != null)
        {
            _mainView.QuickPanel.ShowTeamPanel();
        }
    }

    /// <summary>关闭队伍窗，队伍状态保留在服务器与缓存里，再次挑战仍是已建队。</summary>
    private void OnCloseRequested()
    {
        if (_teamWindow != null)
        {
            _teamWindow.CloseWindow();
        }
    }

    /// <summary>向服务器请求离队，界面在收到空 TeamInfoNtf 后复位。</summary>
    private void OnLeaveRequested()
    {
        _teamCtrl.LeaveTeam();
    }

    /// <summary>邀请页提交的角色ID转成邀请协议。</summary>
    /// <param name="roleId">被邀请角色ID。</param>
    private void OnInviteRoleRequested(ulong roleId)
    {
        _teamCtrl.InviteJoinTeam(roleId, 0);
    }

    /// <summary>寻找队伍页提交的队伍ID转成申请协议。</summary>
    /// <param name="teamId">目标队伍ID。</param>
    private void OnApplyTeamRequested(ulong teamId)
    {
        _teamCtrl.BegInTeamByTeamId(teamId);
    }

    /// <summary>同意收到的入队邀请。</summary>
    private void OnInviteAgreed()
    {
        _teamCtrl.AcceptTeamInvatation(TeamData.Instance.InviteTeamId,
            TeamData.Instance.InviteInviterId, true);
    }

    /// <summary>拒绝收到的入队邀请。</summary>
    private void OnInviteRejected()
    {
        _teamCtrl.AcceptTeamInvatation(TeamData.Instance.InviteTeamId,
            TeamData.Instance.InviteInviterId, false);
    }

    /// <summary>同意收到的入队申请。</summary>
    private void OnApplicationAgreed()
    {
        _teamCtrl.AcceptTeamInvatation(TeamData.Instance.ApplyTeamId,
            TeamData.Instance.ApplyApplicantId, true);
    }

    /// <summary>拒绝收到的入队申请。</summary>
    private void OnApplicationRejected()
    {
        _teamCtrl.AcceptTeamInvatation(TeamData.Instance.ApplyTeamId,
            TeamData.Instance.ApplyApplicantId, false);
    }

    /// <summary>打开或切换队伍界面后刷新邀请页附近候选，不做每帧轮询。</summary>
    private void OnPageOpened()
    {
        RefreshInviteCandidates();
    }

    /// <summary>把当前场景真实附近的玩家交给队伍窗渲染成邀请候选行。</summary>
    private void RefreshInviteCandidates()
    {
        if (_teamWindow == null)
        {
            return;
        }
        _teamWindow.SetInviteCandidates(CollectNearbyCandidates());
    }

    /// <summary>
    /// 从当前场景世界收集可邀请的真实附近玩家，对应 RO TeamInviteCtrl:SetPanelByInfo 的候选来源，
    /// 但本项目没有 RO 的 GetInvitationList 推荐协议，因此复用服务器已同步的真实远端角色实体，
    /// 不引入 Mock、临时假角色或手输ID替代。
    /// 过滤口径：排除本人、已离开或资料缺失的实体、同队成员、不同地图或不同实例、超出 100 米的角色。
    /// </summary>
    /// <returns>可邀请候选人列表，顺序与场景实体快照一致；无候选时返回空列表。</returns>
    private static List<TeamMemberInfo> CollectNearbyCandidates()
    {
        // 本次要返回的候选列表，逐项填充后交给队伍窗渲染。
        List<TeamMemberInfo> candidates = new List<TeamMemberInfo>();
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return candidates;
        }
        // 当前场景世界，其远端角色由服务器 CMD_SyncotherOnlineCode 同步下来，是唯一的真实候选来源。
        SceneWorldSystem world = Global.Instance.SceneWorld;
        // 本地主角实体，用它作为地图、实例与距离的比较基准。
        PlayerEntity localRole = world.LocalRole;
        if (localRole == null || localRole.BaseInfo == null)
        {
            return candidates;
        }
        // 本地主角的基础信息，代表本人所在的地图与实例。
        RoleBaseInfo localInfo = localRole.BaseInfo;
        // 场景实体快照，覆盖当前已进入世界的全部实体，已离开的角色不在其中。
        List<MEntity> entities = new List<MEntity>();
        world.CopyEntities(entities);
        // index 为实体快照的零基下标，范围 [0, entities.Count)，逐个判断是否为可邀请候选。
        for (int index = 0; index < entities.Count; index++)
        {
            // 当前快照实体；只有其他玩家角色才可能是候选，怪物与投射物直接排除。
            RoleEntity candidate = entities[index] as RoleEntity;
            if (candidate == null)
            {
                continue;
            }
            // 候选角色由服务器同步下来的基础资料，缺失或非法ID说明尚未就绪。
            RoleBaseInfo info = candidate.BaseInfo;
            if (info == null || info.RoleId <= 0 || info.RoleId == localInfo.RoleId)
            {
                continue;
            }
            // 不同地图或不同副本实例的角色互相不可见，不能作为候选。
            if (info.MapId != localInfo.MapId || info.InstanceId != localInfo.InstanceId)
            {
                continue;
            }
            // 超出服务器可见距离的角色不算"附近的人"。
            if ((candidate.Position - localRole.Position).sqrMagnitude >
                NearbyCandidateRangeMeters * NearbyCandidateRangeMeters)
            {
                continue;
            }
            // 已在当前队伍中的成员不需要再邀请。
            if (TeamData.Instance.FindMember((ulong)info.RoleId) != null)
            {
                continue;
            }
            // 单个候选项，只携带界面需要的真实角色ID、昵称、等级与职业。
            TeamMemberInfo item = new TeamMemberInfo();
            item.RoleId = (ulong)info.RoleId;
            item.Nickname = info.Nickname;
            item.Level = info.Level;
            item.JobId = info.JobId;
            candidates.Add(item);
        }
        return candidates;
    }

    /// <summary>
    /// 收起副本相关界面并加载魔神塔场景，主角由新场景的主城管理器重建。
    /// 本方法只由服务器 4060 入场通知调用：服务器已确认整队入场，
    /// 被邀请的队员未打开过副本详情页，因此不能用界面选中项作为入场前提。
    /// </summary>
    private void EnterMoshenta()
    {
        if (_mainView.ThemeStoryWindow != null)
        {
            _mainView.ThemeStoryWindow.CloseWindow();
        }
        if (_mainView.ThemeDungeonArroundWindow != null)
        {
            _mainView.ThemeDungeonArroundWindow.CloseWindow();
        }
        if (_teamWindow != null)
        {
            _teamWindow.CloseWindow();
        }
        if (SceneMgr.Instance == null)
        {
            return;
        }
        SceneMgr.Instance.LoadSceneMode(SceneType.Scene_DungeonMoshenta);
    }

    /// <summary>收到服务器队伍状态后刷新界面，并按本人真实角色ID定位自己的成员数据。</summary>
    /// <param name="inTeam">服务器确认是否仍在队伍中。</param>
    private void OnTeamInfoReceived(bool inTeam)
    {
        // 本地主角角色ID，队伍成员里只有这一项代表本人。
        ulong localRoleId = ResolveLocalRoleId();
        TeamData.Instance.SetLocalRoleId(localRoleId);
        // 本人成员信息，服务器成员表里按角色ID查找，不依赖下发顺序。
        TeamMemberInfo self = TeamData.Instance.FindMember(localRoleId);
        if (self != null)
        {
            _hpRatio = CalculateRatio(self.CurrHp, self.MaxHp);
            _mpRatio = _hpRatio;
        }
        RefreshViews();
    }

    /// <summary>收到邀请或申请通知后刷新界面，并把对应界面直接展示给玩家。</summary>
    private void OnInvitationReceived()
    {
        RefreshViews();
        if (_teamWindow == null)
        {
            return;
        }
        // 队长收到申请时打开申请列表，被邀请人收到邀请时打开邀请确认页。
        if (TeamData.Instance.HasApplication)
        {
            _teamWindow.OpenPage(TeamPage.Applications);
            return;
        }
        if (TeamData.Instance.HasInvitation)
        {
            _teamWindow.OpenPage(TeamPage.Recruitment);
        }
    }

    /// <summary>
    /// 收到服务器场景通知后才切换场景：魔神塔加载副本场景，主城则回城主城场景。
    /// 先把服务器权威的地图、实例与出生点写回本地角色，保证进入后过滤与移动同步一致。
    /// </summary>
    /// <param name="notice">服务器下发的入场或回城通知。</param>
    private void OnEnterDungeonReceived(TeamEnterDungeonNtf notice)
    {
        if (notice == null)
        {
            return;
        }
        // 先同步服务器权威位置，客户端不自行推测地图与实例。
        ApplyAuthoritativePosition(notice);
        // 魔神塔：关闭副本界面并加载魔神塔场景。
        if (notice.SceneId == ServerSceneId4060)
        {
            EnterMoshenta();
            return;
        }
        // 主城：离队回城，只有确实不在主城时才重新加载，避免无故重载。
        if (notice.SceneId == MainCitySceneId)
        {
            ReturnToMainCity();
        }
    }

    /// <summary>把服务器通知的地图、实例与出生点写回本地主角，保持本地权威一致。</summary>
    /// <param name="notice">携带目标地图、实例与出生点的场景通知。</param>
    private static void ApplyAuthoritativePosition(TeamEnterDungeonNtf notice)
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return;
        }
        // 当前场景世界的本地主角实体，是权威位置的回写对象；为空说明主角尚未就绪。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole == null || localRole.BaseInfo == null)
        {
            return;
        }
        // 克隆后回写，保持 PlayerEntity 的 BaseInfo 深拷贝约定。
        RoleBaseInfo updated = localRole.BaseInfo.Clone();
        updated.MapId = notice.MapId;
        // 协议里的实例ID是无符号 64 位，角色基础信息用 32 位整数保存，取低位即可满足本项目实例量级。
        updated.InstanceId = (int)notice.InstanceId;
        updated.Pos = notice.Pos;
        localRole.UpdateBaseInfo(updated);
    }

    /// <summary>离队回城：收起副本与队伍界面并切回主城，已在本场景时不重复加载。</summary>
    private void ReturnToMainCity()
    {
        if (_mainView.ThemeDungeonArroundWindow != null)
        {
            _mainView.ThemeDungeonArroundWindow.CloseWindow();
        }
        if (_teamWindow != null)
        {
            _teamWindow.CloseWindow();
        }
        if (SceneMgr.Instance == null)
        {
            return;
        }
        // 已经在主城时只清队伍界面，不做多余的场景重载。
        if (SceneMgr.Instance._currentSceneType == SceneType.Scene_MainCity)
        {
            return;
        }
        SceneMgr.Instance.LoadSceneMode(SceneType.Scene_MainCity);
    }

    /// <summary>把服务器队伍状态同步到正式队伍窗与主界面快捷队伍栏。</summary>
    private void RefreshViews()
    {
        // 本地主角角色ID，用于在成员表里定位本人。
        ulong localRoleId = ResolveLocalRoleId();
        // 队伍是否成立完全由服务器状态决定。
        bool inTeam = TeamData.Instance.IsInTeam;
        // 当前服务器下发的成员表，界面按它的顺序逐行渲染。
        IList<TeamMemberInfo> members = TeamData.Instance.Members;
        // 只有队长可以继续邀请，未满员时显示邀请空位。
        bool canInvite = TeamData.Instance.IsCaptain(localRoleId) &&
            members.Count < TeamData.Instance.MaxMember;
        // 队伍显示名沿用队长昵称，未取得时退回固定文案。
        string teamName = ResolveTeamName(members);
        if (_teamWindow != null)
        {
            _teamWindow.SetTeamState(inTeam, teamName);
            _teamWindow.SetMembers(members, localRoleId, inTeam && canInvite);
            if (_selectedEntry != null)
            {
                _teamWindow.SetTargetName(_selectedEntry.themeName);
            }
            else
            {
                _teamWindow.SetTargetName(string.Empty);
            }
            _teamWindow.SetPendingInvitation(TeamData.Instance.HasInvitation,
                TeamData.Instance.InviteInviterName, members.Count, TeamData.Instance.MaxMember,
                string.Empty);
            _teamWindow.SetPendingApplication(TeamData.Instance.HasApplication,
                TeamData.Instance.ApplyApplicantName);
            ApplyMemberSprites(members, localRoleId);
            // 队伍状态变化后一并刷新附近候选，入队或离队会改变同队过滤结果。
            RefreshInviteCandidates();
        }
        if (_quickTeamWidget != null)
        {
            _quickTeamWidget.SetTeamState(inTeam, members, localRoleId, _hpRatio, _mpRatio);
            ApplyQuickMemberSprites(members, localRoleId);
        }
    }

    /// <summary>按队长昵称生成队伍显示名，取不到时返回固定文案。</summary>
    /// <param name="members">服务器下发的成员表。</param>
    /// <returns>队伍显示名。</returns>
    private static string ResolveTeamName(IList<TeamMemberInfo> members)
    {
        // index 为成员表零基下标，范围 [0, members.Count)，逐个找带队长标记的成员。
        for (int index = 0; index < members.Count; index++)
        {
            // 当前成员项，可能为空；只有 IsCaptain 为真的那一项提供队名。
            TeamMemberInfo member = members[index];
            if (member != null && member.IsCaptain)
            {
                return member.Nickname + "的队伍";
            }
        }
        return "我的队伍";
    }

    /// <summary>把每个成员行的职业头像按职业编号填进正式队伍窗。</summary>
    /// <param name="members">服务器下发的成员表。</param>
    /// <param name="localRoleId">本地主角角色ID。</param>
    private void ApplyMemberSprites(IList<TeamMemberInfo> members, ulong localRoleId)
    {
        if (_teamWindow == null)
        {
            return;
        }
        // index 为成员表零基下标，同时作为队伍窗成员行的行号使用。
        for (int index = 0; index < members.Count; index++)
        {
            // 当前成员项，可能为空；为空的槽位保留预制体默认图，不设头像。
            TeamMemberInfo member = members[index];
            if (member == null)
            {
                continue;
            }
            // 只有本人所在的成员卡换成真实职业头像，其余成员保留预制体默认图。
            if (localRoleId == 0UL || member.RoleId != localRoleId)
            {
                continue;
            }
            _teamWindow.SetMemberJobSprite(index, ResolveJobSprite(member.JobId));
        }
    }

    /// <summary>把本人职业头像填进快捷队伍栏的本人行。</summary>
    /// <param name="members">服务器下发的成员表。</param>
    /// <param name="localRoleId">本地主角角色ID。</param>
    private void ApplyQuickMemberSprites(IList<TeamMemberInfo> members, ulong localRoleId)
    {
        if (_quickTeamWidget == null)
        {
            return;
        }
        // index 为成员表零基下标，同时作为快捷队伍栏成员行的行号使用。
        for (int index = 0; index < members.Count; index++)
        {
            // 当前成员项，可能为空；为空的槽位保留快捷栏预制体默认图。
            TeamMemberInfo member = members[index];
            if (member == null)
            {
                continue;
            }
            if (localRoleId == 0UL || member.RoleId != localRoleId)
            {
                continue;
            }
            _quickTeamWidget.SetMemberJobSprite(index, ResolveJobSprite(member.JobId));
        }
    }

    /// <summary>取当前场景世界中的本地主角角色ID，未进入世界时返回 0。</summary>
    /// <returns>本地主角角色ID；全局或场景世界未就绪时返回 0。</returns>
    private static ulong ResolveLocalRoleId()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return 0UL;
        }
        // 当前场景世界的本地主角实体，用于取角色ID；为空说明尚未进入世界。
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

    /// <summary>按职业编号从已有图集取职业头像精灵，取不到时返回空并由控件保留默认图。</summary>
    /// <param name="jobId">服务器返回的职业配置编号。</param>
    /// <returns>职业头像精灵；图集内不存在该职业图时返回 null。</returns>
    private Sprite ResolveJobSprite(int jobId)
    {
        if (jobId <= 0)
        {
            return null;
        }
        // 已按职业编号查找过的头像结果。
        Sprite cached = null;
        if (_jobSprites.TryGetValue(jobId, out cached))
        {
            return cached;
        }
        if (!_jobAtlasLoaded)
        {
            _jobAtlasSprites = Resources.LoadAll<Sprite>(ProfessionAtlasResourcePath);
            _jobAtlasLoaded = true;
        }
        // RO 职业头像在 Common 图集中的精灵名。
        string targetName = ProfessionSpritePrefix + jobId;
        // 本次查找结果，不存在时保留预制体默认头像。
        Sprite result = null;
        // 当前图集精灵的零基索引。
        for (int index = 0; index < _jobAtlasSprites.Length; index++)
        {
            // 当前比较的现有图集精灵。
            Sprite current = _jobAtlasSprites[index];
            if (current != null && current.name == targetName)
            {
                result = current;
                break;
            }
        }
        _jobSprites[jobId] = result;
        return result;
    }

    /// <summary>计算比例，分母为零时返回零，结果夹到 0 到 1。</summary>
    /// <param name="current">当前值。</param>
    /// <param name="max">最大值。</param>
    /// <returns>0 到 1 之间的比例。</returns>
    private static float CalculateRatio(int current, int max)
    {
        if (max <= 0)
        {
            return 0f;
        }
        // 当前资源占最大资源的比例，无量纲。
        float ratio = (float)current / max;
        if (ratio < 0f)
        {
            return 0f;
        }
        if (ratio > 1f)
        {
            return 1f;
        }
        return ratio;
    }

    /// <summary>注销窗口、快捷栏与副本选择窗事件，避免控制器释放后仍被回调。</summary>
    public override void Dispose()
    {
        if (_mainView != null && _mainView.ThemeDungeonArroundWindow != null)
        {
            _mainView.ThemeDungeonArroundWindow.OnChallengeRequested -= OnChallengeRequested;
        }
        if (_teamWindow != null)
        {
            _teamWindow.CreateRequested -= OnCreateRequested;
            _teamWindow.CloseRequested -= OnCloseRequested;
            _teamWindow.LeaveRequested -= OnLeaveRequested;
            _teamWindow.InviteRoleRequested -= OnInviteRoleRequested;
            _teamWindow.ApplyTeamRequested -= OnApplyTeamRequested;
            _teamWindow.InviteAgreed -= OnInviteAgreed;
            _teamWindow.InviteRejected -= OnInviteRejected;
            _teamWindow.ApplicationAgreed -= OnApplicationAgreed;
            _teamWindow.ApplicationRejected -= OnApplicationRejected;
            _teamWindow.PageOpened -= OnPageOpened;
        }
        if (_quickTeamWidget != null)
        {
            _quickTeamWidget.OpenTeamRequested -= OnOpenTeamRequested;
            _quickTeamWidget.CreateTeamRequested -= OnCreateRequested;
            _quickTeamWidget.OpenPageRequested -= OnOpenPageRequested;
        }
        if (_teamCtrl != null)
        {
            _teamCtrl.TeamInfoReceived -= OnTeamInfoReceived;
            _teamCtrl.InvitationReceived -= OnInvitationReceived;
            _teamCtrl.EnterDungeonReceived -= OnEnterDungeonReceived;
            _teamCtrl.Dispose();
        }
        _selectedEntry = null;
    }
}
