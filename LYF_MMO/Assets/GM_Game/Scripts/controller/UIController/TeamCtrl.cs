using System;
using System.Collections;
using UnityEngine;
using Google.Protobuf;
using MMoRpgCommon.SceneEntity;

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
    /// <summary>MS 发起全队进本确认，通知副本控制器打开 RO 确认窗。</summary>
    public event Action<AskMemberEnterFBData> AskMemberEnterFBReceived;
    /// <summary>MS 同步某位队员的真实答复。</summary>
    public event Action<SyncMemberReplyEnterFBData> MemberReplyEnterFBReceived;
    /// <summary>MS 取消本次进本确认，原因包含超时或名单变化。</summary>
    public event Action<FailEnterFBData> FailEnterFBReceived;

    /// <summary>是否已经注册协议，避免重复注册。</summary>
    private bool _registered;
    /// <summary>原20002黑幕播放协程，进出副本或控制器释放时停止。</summary>
    private Coroutine blackCurtain;

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
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_DungeonInteractResultCode, OnDungeonInteractResult);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.PtcM2C_AskMemberEnterFBNtf, OnAskMemberEnterFB);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.PtcM2C_SyncMemberReplyEnterFBNtf, OnSyncMemberReplyEnterFB);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.PtcM2C_FailEnterFBNtf, OnFailEnterFB);
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
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_DungeonInteractResultCode, OnDungeonInteractResult);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.PtcM2C_AskMemberEnterFBNtf, OnAskMemberEnterFB);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.PtcM2C_SyncMemberReplyEnterFBNtf, OnSyncMemberReplyEnterFB);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.PtcM2C_FailEnterFBNtf, OnFailEnterFB);
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
        StopBlackCurtain();
        UnregisterHandlers();
        TeamInfoReceived = null;
        InvitationReceived = null;
        EnterDungeonReceived = null;
        AskMemberEnterFBReceived = null;
        MemberReplyEnterFBReceived = null;
        FailEnterFBReceived = null;
    }
    #endregion

    /// <summary>转发 RO 进本询问，保留服务器给出的副本与时限。</summary>
    /// <param name="data">AskMemberEnterFBData 的消息体。</param>
    private void OnAskMemberEnterFB(ByteString data)
    {
        AskMemberEnterFBReceived?.Invoke(AskMemberEnterFBData.Parser.ParseFrom(data));
    }

    /// <summary>转发队员答复，客户端只显示服务器已广播的选择。</summary>
    /// <param name="data">SyncMemberReplyEnterFBData 的消息体。</param>
    private void OnSyncMemberReplyEnterFB(ByteString data)
    {
        MemberReplyEnterFBReceived?.Invoke(SyncMemberReplyEnterFBData.Parser.ParseFrom(data));
    }

    /// <summary>转发服务器取消确认通知。</summary>
    /// <param name="data">FailEnterFBData 的消息体。</param>
    private void OnFailEnterFB(ByteString data)
    {
        FailEnterFBReceived?.Invoke(FailEnterFBData.Parser.ParseFrom(data));
    }

    /// <summary>沿 RO MemberReplyEnterFBPtc 提交选择，实际角色身份由 GS 会话取得。</summary>
    /// <param name="reply">玩家明确点击的同意或拒绝。</param><returns>网络层是否接收本次请求。</returns>
    public bool SendMemberReplyEnterFBPtc(ReplyType reply)
    {
        return Send(NetDefine.PtcC2M_MemberReplyEnterFBPtc, new MemberReplyEnterFBData { Type = reply }.ToByteString());
    }

    /// <summary>处理服务器确认的副本入场通知，客户端据此才加载场景。</summary>
    /// <param name="data">TeamEnterDungeonNtf 二进制消息体。</param>
    private void OnEnterDungeon(ByteString data)
    {
        // 入场确认通知：携带场景ID、实例号与出生点，客户端收到后才允许加载副本场景。
        TeamEnterDungeonNtf notice = TeamEnterDungeonNtf.Parser.ParseFrom(data);
        StopBlackCurtain();
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
        // 同一通知承载怪物动作、怪物失血或某玩家被打的权威结果。
        MonsterAttrUpdateNtf notice = MonsterAttrUpdateNtf.Parser.ParseFrom(data);
        if (!TeamData.Instance.OnMonsterAttrUpdate(notice)) return;
        for (int index = 0; index < notice.AiNotices.Count; index++) ShowAiNotice(notice.AiNotices[index]);
        if (notice.PlayerHits.Count == 0) return;
        // 当前显示该实例的角色索引，本人和远端分别存放。
        SceneWorldSystem world = Global.Instance.SceneWorld;
        for (int index = 0; index < notice.PlayerHits.Count; index++)
        {
            // 角色 ID 是持久身份，不可用于查找本地 SceneWorld 的实体 UID。
            MonsterPlayerHitData hit = notice.PlayerHits[index];
            // 本地角色同时需要刷新当前生命条。
            PlayerEntity self = world.LocalRole;
            if (self != null && self.GetID() == hit.TargetRoleId)
            {
                self.ApplyMonsterHit(hit);
                // 主界面重建后由新的队伍控制器接管此窗口。
                MainView main = _view as MainView;
                if (main != null && main.RoleCurrentWindow != null)
                    main.RoleCurrentWindow.UpdateHp(hit.Hp, hit.MaxHp);
            }
            else
            {
                // 只对当前可见的其他玩家派发受击，队伍成员血量已由数据缓存更新。
                RoleEntity other;
                if (world.TryGetRole(hit.TargetRoleId, out other)) other.ApplyMonsterHit(hit);
            }
        }
        TeamInfoReceived?.Invoke(TeamData.Instance.IsInTeam);
    }

    /// <summary>沿现有提示、真实模型和主界面窗口接收原AI显示节点。</summary>
    /// <param name="notice">当前副本实例已接受的原AI通知。</param>
    private void ShowAiNotice(MonsterAiNoticeData notice)
    {
        // 原who_can_see只给指定角色时，不能在同实例其它客户端显示。
        PlayerEntity self = Global.Instance.SceneWorld.LocalRole;
        if (notice.TargetRoleId != 0)
            if (self == null || self.GetID() != notice.TargetRoleId) return;
        // 当前通知已通过实例身份校验，按该实例场景登记读取原文与时间配置。
        DungeonSceneResource resource = DungeonSceneResource.Load(TeamData.Instance.DungeonSceneId);
        // 当前根目录的只读文本与时长，未导入时不显示其他副本的通知。
        MSceneMonsterNoticeCatalog data = MSceneMonsterNoticeCatalog.Load(resource.battleResourceRoot);
        if (data == null) return;
        if (notice.Kind == 1)
        {
            for (int index = 0; index < data.Notices.Length; index++)
                if (data.Notices[index].Id == notice.NoticeId) TipsMgr.Instance.ShowSystemTips(data.Notices[index].Text);
        }
        else if (notice.Kind == 2 && notice.NoticeId == 5) TeamData.Instance.OnMonsterHeadExpression(notice);
        else if (notice.Kind == 3 && notice.NoticeId == 20002)
        {
            if (blackCurtain != null) return;
            blackCurtain = _view.StartCoroutine(PlayBlackCurtain(data));
        }
    }

    /// <summary>以原BlackCurtainCtrl顺序调度20002五行文字，窗口只展示本帧数据。</summary>
    /// <param name="data">原表五行文字与原Lua默认时长。</param>
    private IEnumerator PlayBlackCurtain(MSceneMonsterNoticeCatalog data)
    {
        // 黑幕由现有MainView持有，沿WindowType管理显隐。
        MainView main = (MainView)_view;
        // 正式预制体上的强类型子窗口。
        MoshentaBlackCurtainWindow window = main.MoshentaBlackCurtainWindow;
        // 每行实际透明度，生命周期只覆盖本次黑幕。
        float[] alphas = new float[data.CurtainLines.Length];
        window.Display(0f, data.CurtainLines, alphas);
        if (!window.gameObject.activeSelf) main.ShowMainWindow(WindowType.MoshentaBlackCurtainWindow);
        // 原整段末尾等待时刻：背景渐入后逐行渐入并保留0.5秒间隔。
        float textEnd = data.FadeIn;
        for (int index = 0; index < data.CurtainLineTimes.Length; index++) textEnd += data.CurtainLineTimes[index] + data.LinePause;
        // 原内容区先退场，随后黑色背景淡出。
        float contentEnd = textEnd + data.ParagraphPause + data.ContentFadeOut;
        // 当前播放的本地起点，使用未缩放时间保持UI时钟。
        float started = Time.unscaledTime;
        while (Time.unscaledTime - started < contentEnd + data.FadeOut)
        {
            // 本次黑幕的已过秒数。
            float elapsed = Time.unscaledTime - started;
            // 原BG的线性透明度。
            float background = Mathf.Clamp01(elapsed / data.FadeIn);
            if (elapsed > contentEnd) background = 1f - Mathf.Clamp01((elapsed - contentEnd) / data.FadeOut);
            // 原下一行开始渐入的时刻。
            float lineAt = data.FadeIn;
            for (int index = 0; index < alphas.Length; index++)
            {
                alphas[index] = Mathf.Clamp01((elapsed - lineAt) / data.CurtainLineTimes[index]);
                // 原FadeOut先ClearCache移除行文字，再淡出内容背景。
                if (elapsed >= textEnd + data.ParagraphPause) alphas[index] = 0f;
                lineAt += data.CurtainLineTimes[index] + data.LinePause;
            }
            window.Display(background, data.CurtainLines, alphas);
            yield return null;
        }
        main.ShowMainWindow(WindowType.MoshentaBlackCurtainWindow);
        blackCurtain = null;
    }

    /// <summary>离开实例或释放控制器时关闭该次原黑幕，不影响其他主界面窗口。</summary>
    private void StopBlackCurtain()
    {
        if (blackCurtain != null) _view.StopCoroutine(blackCurtain);
        blackCurtain = null;
        // 控制器在MainView实际存在时才持有此正式子窗口。
        MainView main = _view as MainView;
        if (main == null || main.MoshentaBlackCurtainWindow == null) return;
        if (main.MoshentaBlackCurtainWindow.gameObject.activeSelf) main.ShowMainWindow(WindowType.MoshentaBlackCurtainWindow);
    }

    /// <summary>沿原NetAIEventCommand发送玩家明确选中的NPC剧情事件。</summary>
    /// <param name="name">原命令事件名；角色身份和所属动态场景由服务端当前会话取得。</param>
    public static void RequestDungeonCommandEvent(string name)
    {
        // 原命令只携带name，不能由客户端写Boss状态或任务完成值。
        DungeonCommandEventReq request = new DungeonCommandEventReq { Name = name };
        Send(NetDefine.CMD_DungeonCommandEventCode, request.ToByteString());
    }

    /// <summary>发送正式按钮所选的门户或采集波次，不提交目标坐标。</summary>
    /// <param name="interaction">当前附近的配置物件身份。</param>
    public static void RequestDungeonInteraction(MSceneInteraction interaction)
    {
        // 物件零号有效，采集物使用独立波次字段。
        DungeonInteractReq request = new DungeonInteractReq();
        request.ObjectId = interaction.ObjectId;
        request.WaveId = interaction.WaveId;
        Send(NetDefine.CMD_DungeonInteractCode, request.ToByteString());
    }

    /// <summary>应用服务器传送或空气墙纠正，其他队员同样按角色身份更新。</summary>
    /// <param name="data">DungeonInteractRet 的二进制消息体。</param>
    private void OnDungeonInteractResult(ByteString data)
    {
        // 协议解析直接交给 Protobuf，不吞掉数据异常。
        DungeonInteractRet notice = DungeonInteractRet.Parser.ParseFrom(data);
        if (notice.InstanceId != TeamData.Instance.DungeonInstanceId || notice.Exited) return;
        // 当前实体世界及服务器指定的移动角色。
        SceneWorldSystem world = Global.Instance.SceneWorld;
        RoleEntity player = world.LocalRole;
        if (player == null) return;
        if (player.GetID() != notice.RoleId)
        {
            if (!notice.Success) return;
            RoleEntity other;
            if (!world.TryGetRole(notice.RoleId, out other)) return;
            player = other;
        }
        if (!string.IsNullOrEmpty(notice.Pos))
        {
            // 沿项目已有位置文本格式恢复服务器坐标，同时更新基础信息。
            RoleBaseInfo info = player.BaseInfo.Clone();
            info.Pos = notice.Pos;
            player.UpdateBaseInfo(info);
            // 项目共用坐标扩展以服务器三分量文本解析。
            System.Numerics.Vector3 position = notice.Pos.ToVector3();
            player.NavComp.Interrupt();
            player.MoveComp.StopHorizontalMovement();
            player.Position = new UnityEngine.Vector3(position.X, position.Y, position.Z);
        }
        if (!notice.Success && !string.IsNullOrEmpty(notice.Reason))
            TipsMgr.Instance.ShowSystemTips(notice.Reason);
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
    /// <param name="difficultyId">难度表编号一至六，由详情页选择，服务端决定实际实例配置。</param>
    /// <param name="layer">独立于章节的挑战层数，非分层难度为零。</param>
    /// <returns>请求已提交返回 true。</returns>
    public bool CaptainRequestEnterFBPtc(int dungeonId, int difficultyId, int layer)
    {
        // 基础副本编号保留现有地图；难度单独发送，不用缺失资源的其他地图编号代替难度。
        CaptainRequestEnterFBArg arg = new CaptainRequestEnterFBArg();
        arg.DungeonId = dungeonId;
        arg.ChapterId = 0;
        arg.DungeonsDegree = difficultyId;
        arg.DungeonLayer = layer;
        return Send(NetDefine.CMD_CaptainRequestEnterFBCode, arg.ToByteString());
    }
    #endregion
}
