using System;
using System.Collections.Generic;
using cfg;
using Google.Protobuf;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>把技能快捷键交给施法模块，接收服务器技能列表与施法通知，并刷新技能栏、法力值和冷却显示。</summary>
public sealed class SkillCtrl : CtrlBase
{

    #region 字段
    /// <summary>技能列表窗口，仅接收控制器生成的视图数据。</summary>
    private readonly SkillInfoWindow _skillWindow;

    /// <summary>角色快捷技能栏和 MP 展示窗口。</summary>
    private readonly RoleCurrInfoWindow _roleCurrentWindow;

    /// <summary>按已绑定快捷键索引旧技能主表配置。</summary>
    private readonly Dictionary<string, SkillInfo> _skillsByKey =
        new Dictionary<string, SkillInfo>();

    /// <summary>当前本地角色的技能展示快照，角色离场时清空。</summary>
    private readonly List<SkillViewData> _skillViews = new List<SkillViewData>();

    /// <summary>技能快照所属的持久化角色 ID，零表示尚未接收当前角色的列表。</summary>
    private int _skillViewRoleId;

    /// <summary>本地角色尚未创建时按持久化角色 ID 暂存的 11080 回包。</summary>
    private readonly Dictionary<int, RoleSkillInfoRet> _pendingRoleSkillInfos =
        new Dictionary<int, RoleSkillInfoRet>();

    /// <summary>待应用技能回包所属的连接编号，零表示没有待应用回包。</summary>
    private long _pendingRoleSkillGeneration;

    /// <summary>快捷键事件是否已绑定，避免 UI 反复初始化时重复施法。</summary>
    private bool _inputRegistered;

    /// <summary>苍蓝屏障技能 ID，护罩跟随自身，选中远处敌人时也必须原地释放。</summary>
    internal const int BlueBarrierSkillId = 10120;

    /// <summary>龙卷斩的稳定主表编号，施法沿角色当前水平朝向放出特效。</summary>
    internal const int TornadoSlashSkillId = 10130;
    /// <summary>逐月的主表编号，突进始终沿施法者当前面向。</summary>
    internal const int MoonChaseSkillId = 10140;
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>创建控制器并登记技能协议、目标切换和快捷栏点击。</summary>
    /// <param name="mainView">持有技能列表与快捷栏窗口的主界面。</param>
    public SkillCtrl(MainView mainView) : base(mainView)
    {
        _skillWindow = mainView.SkillWindow;
        _roleCurrentWindow = mainView.RoleCurrentWindow;
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleSkillInfoCode,
            OnRoleSkillInfo);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_SkillCastCode,
            OnSkillCastResult);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_SkillCastStartCode,
            OnSkillCastStart);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_SkillCastEndCode,
            OnSkillCastEnd);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_MartialManual, OnManualResult);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_SkillUpgradeCode,
            OnSkillUpgradeResult);
        if (_skillWindow != null)
        {
            _skillWindow.UpgradeRequested += RequestSkillUpgrade;
            _skillWindow.ManualRequested += RequestManual;
            _skillWindow.GrowthContextRequested += RefreshVisibleSkills;
        }
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            Global.Instance.SceneWorld.LocalRoleChanged += OnLocalRoleChanged;
        }
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.AimButtonClicked += OnAimButton;
            _roleCurrentWindow.SkillCastRequested += OnSkillKey;
        }
        MSkillTargetMgr.singleton.TargetChanged += OnTargetChanged;
        RegisterInput();
    }

    /// <summary>幂等登记技能快捷键，兼容角色输入组件晚于主界面初始化。</summary>
    public void RegisterInput()
    {
        if (_inputRegistered || PlayerInputCtr.Instance == null)
        {
            return;
        }
        PlayerInputCtr.Instance.SkillKeyEvent += OnSkillKey;
        _inputRegistered = true;
    }

    /// <summary>注销协议与输入，避免旧主界面处理新连接消息。</summary>
    public override void Dispose()
    {
        MSkillTargetMgr.singleton.TargetChanged -= OnTargetChanged;
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.AimButtonClicked -= OnAimButton;
            _roleCurrentWindow.SkillCastRequested -= OnSkillKey;
        }
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_RoleSkillInfoCode,
            OnRoleSkillInfo);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SkillCastCode,
            OnSkillCastResult);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SkillCastStartCode,
            OnSkillCastStart);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SkillCastEndCode,
            OnSkillCastEnd);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_MartialManual, OnManualResult);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SkillUpgradeCode,
            OnSkillUpgradeResult);
        if (_skillWindow != null)
        {
            _skillWindow.UpgradeRequested -= RequestSkillUpgrade;
            _skillWindow.ManualRequested -= RequestManual;
            _skillWindow.GrowthContextRequested -= RefreshVisibleSkills;
        }
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            Global.Instance.SceneWorld.LocalRoleChanged -= OnLocalRoleChanged;
        }
        if (_inputRegistered && PlayerInputCtr.Instance != null)
        {
            PlayerInputCtr.Instance.SkillKeyEvent -= OnSkillKey;
            _inputRegistered = false;
        }
        _skillsByKey.Clear();
        _skillViews.Clear();
        _skillViewRoleId = 0;
        _pendingRoleSkillInfos.Clear();
        _pendingRoleSkillGeneration = 0;
    }
    #endregion

    #region 交互响应
    /// <summary>角色出现时应用同连接、同角色的暂存技能列表；角色离场时清空快捷键和显示数据。</summary>
    /// <param name="localRole">刚加入场景世界的本地角色，离场时为空。</param>
    private void OnLocalRoleChanged(PlayerEntity localRole)
    {
        if (_skillWindow != null) _skillWindow.ClearManual();
        if (localRole == null)
        {
            TouchObject.singleton.Clear();
            OnTargetChanged(null);
            _skillsByKey.Clear();
            _skillViews.Clear();
            _skillViewRoleId = 0;
            // LoadLocalRole 会先通知 null，再创建实体；同连接待应用回包在此保留。
            if (_pendingRoleSkillGeneration !=
                MNetClient.Instance.CurrentConnectionGeneration)
            {
                _pendingRoleSkillInfos.Clear();
                _pendingRoleSkillGeneration = 0;
            }
            RefreshSkillWindows();
            return;
        }
        // 新角色只消费同一连接编号且 RoleId 相符的待应用列表。
        RoleSkillInfoRet pendingResult = null;
        bool hasMatchingPendingResult = _pendingRoleSkillGeneration > 0 &&
            _pendingRoleSkillGeneration ==
            MNetClient.Instance.CurrentConnectionGeneration &&
            _pendingRoleSkillInfos.TryGetValue(localRole.GetID(),
                out pendingResult);
        if (hasMatchingPendingResult)
        {
            _pendingRoleSkillInfos.Clear();
            _pendingRoleSkillGeneration = 0;
            ApplyRoleSkillInfo(pendingResult, localRole);
            return;
        }
        _pendingRoleSkillInfos.Clear();
        _pendingRoleSkillGeneration = 0;
        if (_skillViewRoleId != localRole.GetID())
        {
            _skillsByKey.Clear();
            _skillViews.Clear();
            _skillViewRoleId = 0;
        }
        ApplySkillCores(localRole);
        RefreshSkillWindows();
    }

    /// <summary>将选中实体交给现角色窗口，只更新目标文字，不发技能或伤害请求。</summary>
    /// <param name="target">新选中的原实体，空值显示未选中。</param>
    private void OnTargetChanged(MEntity target)
    {
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.UpdateTarget(target);
        }
    }
    #endregion

    #region 事件回调
    /// <summary>接收服务器的已学技能列表；当前角色未创建就暂存，否则按角色 ID 应用等级和快捷键。</summary>
    /// <param name="data">11080 对应的只读 protobuf 消息体。</param>
    private void OnRoleSkillInfo(ByteString data)
    {
        // 角色已学技能列表的服务端快照，失败回包不替换现有模型。
        RoleSkillInfoRet result;
        if (!TryParseRoleSkillInfo(data, out result))
        {
            return;
        }
        // 11080 只接纳当前连接派发的回包，旧连接暂存数据不能跨会话装载。
        long dispatchGeneration = SocketDispatcher.Instance.CurrentDispatchGeneration;
        if (dispatchGeneration <= 0 || dispatchGeneration !=
            MNetClient.Instance.CurrentConnectionGeneration)
        {
            return;
        }
        // 场景角色可能晚于 11080 创建，先按 RoleId 保留各角色自己的快照。
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null)
        {
            if (_pendingRoleSkillGeneration != dispatchGeneration)
            {
                _pendingRoleSkillInfos.Clear();
                _pendingRoleSkillGeneration = dispatchGeneration;
            }
            _pendingRoleSkillInfos[result.RoleId] = result;
            return;
        }
        // 迟到的角色 A 回包不得替换当前角色 B 的快捷键与技能核心。
        if (localRole.GetID() <= 0 || result.RoleId != localRole.GetID())
        {
            return;
        }
        _pendingRoleSkillInfos.Clear();
        _pendingRoleSkillGeneration = 0;
        ApplyRoleSkillInfo(result, localRole);
    }

    /// <summary>键盘或快捷栏点击共用的入口，按当前绑定发起施法并刷新显示。</summary>
    /// <param name="key">键盘输入或鼠标点中的技能槽对应的固定快捷键。</param>
    private void OnSkillKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }
        // 输入系统与服务器快照的快捷键大小写不应影响技能查找。
        key = key.ToUpperInvariant();
        // 快捷键绑定的旧技能主表配置。
        SkillInfo skillInfo;
        if (!_skillsByKey.TryGetValue(key, out skillInfo) || skillInfo == null)
        {
            return;
        }
        // 本地玩家场景实体负责技能状态与预测 CD，旧 MainRoleCtrl 不参与施法。
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null || localRole.Skill == null)
        {
            return;
        }
        if (localRole.BaseInfo == null ||
            localRole.GetID() != _skillViewRoleId)
        {
            return;
        }
        // 快捷键只选择已学核心，发送职责沿RO经典释放、交接与导航到达模块执行。
        MSkillCore core = localRole.Skill.GetSkillCore(skillInfo.Id);
        if (core == null)
        {
            return;
        }
        MSkillClassicsHandler.CastSkill(core, localRole.Position, 
            GetTargetFilterSkill(core, localRole.Target));
        RefreshSkillWindows();
    }

    /// <summary>正式瞄准按钮和 Tab 统一交给 RO 同名切换方法。</summary>
    private void OnAimButton()
    {
        MSkillTargetMgr.singleton.FindEnemyByAimBtn();
    }

    /// <summary>应用 11170 权威结果并同步当前 MP、单技能/组 CD。</summary>
    /// <param name="data">SkillCastRet 的 protobuf 消息体。</param>
    private void OnSkillCastResult(ByteString data)
    {
        if (MNetClient.Instance == null ||
            SocketDispatcher.Instance.CurrentDispatchGeneration !=
            MNetClient.Instance.CurrentConnectionGeneration)
        {
            return;
        }
        // 本次服务器权威施法结果，失败同样带 MP/CD 快照。
        SkillCastRet result = SkillCastRet.Parser.ParseFrom(data);
        if (result == null || (result.SkillUuid == 0 && !result.CooldownOnly))
        {
            return;
        }
        // 当前本地角色；断线或切图后不可把旧回包写入新角色。
        PlayerEntity localRole = GetLocalRole();
        if (localRole != null && localRole.Skill != null &&
            localRole.GetID() == _skillViewRoleId)
        {
            if (result.CooldownOnly)
            {
                localRole.Skill.ApplyCooldownUpdate(result, Time.realtimeSinceStartup);
                RefreshSkillWindows();
                return;
            }
            // 身份与快照角色一致后才把权威结果写入技能核心，同步失败则不刷新界面。
            if (localRole.Skill.OnSkillCoreSync(result, Time.realtimeSinceStartup))
            {
                localRole.ApplyAuthoritativeMp(result.CurrentMp);
                if (_roleCurrentWindow != null && localRole.BaseInfo != null)
                {
                    _roleCurrentWindow.UpdateMp(localRole.BaseInfo.CurrMp,
                        localRole.BaseInfo.MaxMp);
                }
                RefreshSkillWindows();
                if (result.CmdCode != CmdCode.Succeed)
                {
                    TipsMgr.Instance.ShowSystemTips(GetCastErrorText(result.CmdCode));
                }
            }
        }
    }

    /// <summary>向服务端发送一次技能升级，等权威结果后刷新等级和钱包。</summary>
    /// <param name="skill">窗口当前显示的技能与在点击时的已学等级。</param>
    private void RequestSkillUpgrade(SkillViewData skill)
    {
        if (MNetClient.Instance == null || _skillViewRoleId <= 0) return;
        // 只传技能及当前等级，下一行、角色身份、费用由服务端读取。
        SkillUpgradeReq request = new SkillUpgradeReq { SkillId = skill.SkillId, CurrentLevel = skill.Level };
        MNetClient.Instance.Send(NetDefine.CMD_SkillUpgradeCode, request.ToByteString());
    }

    /// <summary>沿当前连接发送秘笈操作，费用与身份由游戏服决定。</summary><param name="request">本次界面意图。</param>
    private void RequestManual(ManualRequest request)
    {
        if (MNetClient.Instance != null) MNetClient.Instance.Send(NetDefine.CMD_MartialManual, request.ToByteString());
    }

    /// <summary>只把当前连接与角色的秘笈回执交给武学窗口。</summary><param name="payload">网关转交的秘笈消息。</param>
    private void OnManualResult(ByteString payload)
    {
        if (MNetClient.Instance == null || SocketDispatcher.Instance.CurrentDispatchGeneration != MNetClient.Instance.CurrentConnectionGeneration) return;
        // 校验角色归属，角色未就绪的业务提示没有目录。
        ManualReply reply = ManualReply.Parser.ParseFrom(payload);
        PlayerEntity role = GetLocalRole();
        if (reply.Catalog != null && (role == null || role.GetID() != reply.RoleId)) return;
        if (reply.Action == ManualAction.ManualCompose || reply.Action == ManualAction.ManualLearn)
        {
            // 背包内操作也可收到结果；两种操作分别报告入包与学会。
            string message = reply.Message;
            if (reply.Success && reply.Action == ManualAction.ManualCompose) message = "合成成功，整本秘笈已放入背包";
            if (reply.Success && reply.Action == ManualAction.ManualLearn) message = "领悟成功";
            TipsMgr.Instance.ShowSystemTips(message);
        }
        if (_skillWindow != null) _skillWindow.ApplyManual(reply);
    }

    /// <summary>打开武学页时读取最新的同步角色等级和虚拟背包余额。</summary>
    private void RefreshVisibleSkills()
    {
        RefreshSkillWindows();
    }

    /// <summary>只用当前连接和当前角色的服务端结果刷新学习等级与技能核心。</summary>
    /// <param name="payload">服务器确认的强类型升级结果。</param>
    private void OnSkillUpgradeResult(ByteString payload)
    {
        if (MNetClient.Instance == null || SocketDispatcher.Instance.CurrentDispatchGeneration !=
            MNetClient.Instance.CurrentConnectionGeneration) return;
        // 成功及失败都携带当前服务端学习等级和铜币余额。
        SkillUpgradeRet result = SkillUpgradeRet.Parser.ParseFrom(payload);
        // 技能等级只写进对应的本地主角，切换角色后的旧回包无效。
        PlayerEntity role = GetLocalRole();
        if (role == null || result.RoleId != _skillViewRoleId || role.GetID() != result.RoleId) return;
        for (int index = 0; index < _skillViews.Count; index++)
        {
            // 替换本次命中的一条技能，快捷键由此前权威快照继续提供。
            SkillViewData previous = _skillViews[index];
            if (previous.SkillId != result.SkillId) continue;
            if (result.Level > previous.Level)
            {
                // 主表提供技能名称和图标，最新成长行负责真正的数值。
                SkillInfo definition = LubanMgr.Instance.GetSkillInfoById(previous.SkillId);
                _skillViews[index] = CreateSkillView(definition, result.Level, previous.BindKey);
                role.Skill.AttachSkillInfo(result.SkillId, result.Level);
            }
            break;
        }
        RefreshSkillWindows(result.Copper);

        // 一次拒绝提示只说明用户可见的失败原因，不改本地模拟等级。
        string message;
        switch (result.CmdCode)
        {
            case CmdCode.Succeed: return;
            case CmdCode.SkillUpgradeMaxLevel: message = "该技能已满级"; break;
            case CmdCode.SkillUpgradeRoleLevel: message = "角色等级还未达到升级要求"; break;
            case CmdCode.NotEnoughCurrency: message = "铜币不足"; break;
            case CmdCode.SkillUpgradeLevelChanged: message = "技能等级已更新"; break;
            default: message = "当前无法升级该技能"; break;
        }
        TipsMgr.Instance.ShowSystemTips(message);
    }

    /// <summary>服务器广播开始时按持久化 RoleId 定位场景实体并去重播放。</summary>
    /// <param name="data">SkillCastStartNtf 的 protobuf 消息体。</param>
    private void OnSkillCastStart(ByteString data)
    {
        // RO 使用请求中的同一 UUID 匹配本地预表现；角色 ID 只负责找场景实体。
        SkillCastStartNtf notice = SkillCastStartNtf.Parser.ParseFrom(data);
        if (notice == null || notice.SkillUuid == 0)
        {
            return;
        }
        // 施法者场景实体，由角色持久化 ID 映射得到。
        RoleEntity caster = FindRole(notice.CasterRoleId);
        if (caster == null || caster.Skill == null || caster.BaseInfo == null)
        {
            return;
        }
        caster.Skill.OnSkillStart(notice);
    }

    /// <summary>服务器结束广播按同一 UUID 匹配当前或旧实例；自然结束保留事件尾段，打断立即注销并回池。</summary>
    /// <param name="data">SkillCastEndNtf 的 protobuf 消息体。</param>
    private void OnSkillCastEnd(ByteString data)
    {
        // 结束通知不承担命中、伤害和 MP 结算。
        SkillCastEndNtf notice = SkillCastEndNtf.Parser.ParseFrom(data);
        if (notice == null || notice.SkillUuid == 0)
        {
            return;
        }
        // 角色实体必须由持久化 RoleId 查询，不能将其当作本地 UID。
        RoleEntity caster = FindRole(notice.CasterRoleId);
        if (caster != null && caster.Skill != null)
        {
            // 协议已有打断标志；自然结束保留时间轴尾段，真实打断立即注销此 UUID 的全部未触发指令。
            caster.Skill.OnSkillEnd(notice);
        }
    }
    #endregion

    #region 查询与判定
    /// <summary>将协议字节解析为成功且带有效角色 ID 的已学技能快照。</summary>
    /// <param name="data">11080 对应的只读 protobuf 消息体。</param>
    /// <param name="result">解析后的角色技能列表；空输入时为空，结果码或角色 ID 无效时仍可能保留解析对象。</param>
    /// <returns>非空输入成功解析，且结果码成功、角色 ID 为正时返回 true；协议解析异常直接向外抛出。</returns>
    private static bool TryParseRoleSkillInfo(ByteString data,
        out RoleSkillInfoRet result)
    {
        result = null;
        if (data == null || data.IsEmpty)
        {
            return false;
        }
        result = RoleSkillInfoRet.Parser.ParseFrom(data);
        if (result == null || result.CmdCode != CmdCode.Succeed ||
            result.RoleId <= 0)
        {
            return false;
        }
        return true;
    }

    /// <summary>攻击优先保持有效当前目标，常规攻击允许自动找敌，刀刃风暴只用已选目标确定起手朝向。</summary>
    /// <param name="skillCore">本次已学技能；主表 Type 一为普攻，Type 二为其他主动攻击。</param>
    /// <param name="target">玩家当前选中实体，可为空或友方。</param>
    /// <returns>有效当前敌人或允许自动选择时的新敌人，空值表示保持角色当前朝向。</returns>
    public MEntity GetTargetFilterSkill(MSkillCore skillCore, MEntity target)
    {
        // 苍蓝屏障护持自己，施法无需传入敌人。
        if (skillCore.SkillId == BlueBarrierSkillId)
        {
            return null;
        }
        // 龙卷斩沿角色面向释放。
        if (skillCore.SkillId == TornadoSlashSkillId)
        {
            return null;
        }
        // 沿现有主表技能类别，滑步和轻功不触发选敌，也不改变其朝向。
        SkillInfo skillInfo = LubanMgr.Instance.GetSkillInfoById(skillCore.SkillId);
        if (skillInfo.Type != 1 && skillInfo.Type != 2)
        {
            return null;
        }
        // 自动取得和主动选择的敌人同样保持，身后或更近的新怪不会抢走已有目标。
        if (MSkillTargetMgr.singleton.CanAttack(target))
        {
            return target;
        }
        // 逐月优先追当前锁定，无锁定时从同一立体范围内选最近的敌人，身后及空中均可。
        if (skillCore.SkillId == MoonChaseSkillId)
        {
            return MSkillTargetDefine.FindOneTarget(true, MSkillTargetMgr.MaxChooseDistance);
        }
        // 无锁定时刀刃风暴沿角色当前方向起手，不自动抢占敌人。
        if (skillCore.SkillId == MWaitingDataMgr.BladeStormSkillId)
        {
            return null;
        }
        return MSkillTargetMgr.singleton.FindEnemyBySkillBtn(true, MSkillTargetDefine.IsInAttackFacing);
    }

    /// <summary>从统一场景世界取得当前本地主角。</summary>
    /// <returns>已进入世界的本地玩家，否则为空。</returns>
    private static PlayerEntity GetLocalRole()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return null;
        }
        return Global.Instance.SceneWorld.LocalRole;
    }

    /// <summary>按持久化角色 ID 查询本地或其他角色实体。</summary>
    /// <param name="roleId">服务器广播的施法者 RoleId。</param>
    /// <returns>当前场景中对应实体，离场或未知时为空。</returns>
    private static RoleEntity FindRole(int roleId)
    {
        if (roleId <= 0 || Global.Instance == null ||
            Global.Instance.SceneWorld == null)
        {
            return null;
        }
        // 当前本地主角，其 RoleId 来自已装载的角色基础信息。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole != null && localRole.GetID() == roleId)
        {
            return localRole;
        }
        // 其他角色按照 SceneWorld 现有 RoleId 索引读取。
        RoleEntity otherRole;
        if (Global.Instance.SceneWorld.TryGetRole(roleId, out otherRole))
        {
            return otherRole;
        }
        return null;
    }

    /// <summary>取技能节点的两字定位标签，普攻读取主表标记，其他武功按招式编号或类型展示。</summary>
    /// <param name="skillConfig">技能主表配置，提供普攻标记、技能编号与类型。</param>
    /// <returns>固定两个汉字的定位标签，例如“突进”“绝学”。</returns>
    private static string GetSkillTag(SkillInfo skillConfig)
    {
        if (skillConfig.IsBasicAttack) return "普攻";
        // 已收录的魔剑士技能使用各自的招式定位，便于玩家在环形节点上快速辨认。
        switch (skillConfig.Id)
        {
            case 10040:
                return "突进";
            case 10050:
                return "冲锋";
            case 10060:
                return "爆发";
            case 10070:
                return "绝学";
            case 10080:
                return "滑行";
            case 10090:
                return "轻功";
            case 10100:
                return "连招";
            case 10110:
                return "群攻";
            case 10120:
                return "护盾";
            case TornadoSlashSkillId:
                return "真气";
            case MoonChaseSkillId:
                return "突进";
        }
        // 未收录技能按主表类型回退，保证任何职业都不会出现空标签。
        if (skillConfig.Type == 3)
        {
            return "滑行";
        }
        if (skillConfig.Type == 4)
        {
            return "轻功";
        }
        return "攻击";
    }

    /// <summary>将服务器拒绝码转换为可见的简短技能反馈。</summary>
    /// <param name="code">服务器 SkillCastRet 的失败码。</param>
    /// <returns>技能槽上方系统提示使用的中文文本。</returns>
    private static string GetCastErrorText(CmdCode code)
    {
        if (code == CmdCode.SkillNotLearned)
        {
            return "尚未学会该技能";
        }
        if (code == CmdCode.SkillOnCooldown)
        {
            return "技能尚未冷却";
        }
        if (code == CmdCode.SkillInsufficientMp)
        {
            return "法力值不足";
        }
        if (code == CmdCode.SkillStateBlocked)
        {
            return "当前状态无法施法";
        }
        if (code == CmdCode.SkillTargetUnsupported)
        {
            return "当前技能暂不可释放";
        }
        return "技能释放失败";
    }
    #endregion

    #region 技能视图刷新与应用
    /// <summary>用技能核心的实时 CD 生成视图快照并刷新两个正式窗口。</summary>
    /// <param name="authoritativeCopper">服务器本次动作返回的铜币余额，普通刷新从本地同步钱包读取。</param>
    private void RefreshSkillWindows(long? authoritativeCopper = null)
    {
        // 当前角色持有的技能组件，在角色尚未进入世界时为空。
        PlayerEntity localRole = GetLocalRole();
        // 本次界面刷新使用的新展示快照，避免修改上次已交给 View 的列表。
        List<SkillViewData> views = new List<SkillViewData>(_skillViews.Count);
        for (int index = 0; index < _skillViews.Count; index++)
        {
            // 旧技能展示配置保留名称、描述、图标和快捷键。
            SkillViewData previous = _skillViews[index];
            if (previous == null)
            {
                continue;
            }
            // 角色已经建立技能核心时，从核心读取实际冷却时长和剩余时间。
            MSkillCore core = null;
            if (localRole != null && localRole.Skill != null)
            {
                core = localRole.Skill.GetSkillCore(previous.SkillId);
            }
            // 完整冷却时长，单位为秒；无技能核心时使用原展示配置。
            float duration = previous.Cooldown;
            // 当前剩余冷却秒数，无核心时按 0 显示。
            float remaining = 0f;
            if (core != null)
            {
                duration = core.CooldownSeconds;
                remaining = localRole.Skill.GetRemainingCooldown(
                    previous.SkillId, Time.realtimeSinceStartup);
            }
            views.Add(new SkillViewData(previous.SkillId, previous.Name,
                previous.Description, previous.IconPath, previous.Level, duration,
                previous.BindKey, remaining, previous.SkillType, previous.Tag,
                previous.MpCost, previous.Range, previous.UpgradeCost, previous.NextRoleLevel,
                previous.HasGrowthConfig, previous.TrueQiCost));
        }
        if (_skillWindow != null)
        {
            if (localRole != null && localRole.BaseInfo != null)
            {
                // 有升级权威结果时立即显示成交余额，否则复用本地虚拟背包的同步余额。
                long copper = authoritativeCopper.GetValueOrDefault();
                // 角色创建期间背包组件尚未挂载时暂显示零。
                RoItemComponent items;
                if (!authoritativeCopper.HasValue && localRole.TryGetRoleComponent(out items))
                    copper = items.GetVirtualItemCount((int)ItemEnum.ZENY);
                _skillWindow.SetGrowthContext(localRole.BaseInfo.Level, copper);
            }
            _skillWindow.ReFreshUI(views);
        }
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.ReFreshUI(views);
        }
    }
    /// <summary>解析 11080 技能回包，并按当前本地角色身份应用技能数据。</summary>
    /// <param name="data">与网络回调相同的 11080 protobuf 消息体。</param>
    /// <returns>回包有效且角色 ID 与当前本地主角一致时返回 true。</returns>
    public bool ApplyRoleSkillInfoPayload(ByteString data)
    {
        // 离线注入与网络回调共用协议解析，角色尚未创建时由调用方稍后重试。
        RoleSkillInfoRet result;
        if (!TryParseRoleSkillInfo(data, out result))
        {
            return false;
        }
        return ApplyLearnedSkillSnapshot(result);
    }

    /// <summary>将同角色的 11080 快照转为快捷键、展示数据及长期技能核心。</summary>
    /// <param name="result">RoleId 已与当前角色核对的服务端技能列表。</param>
    /// <param name="localRole">当前已创建且与回包 RoleId 相同的本地角色。</param>
    private void ApplyRoleSkillInfo(RoleSkillInfoRet result, PlayerEntity localRole)
    {
        _skillsByKey.Clear();
        _skillViews.Clear();
        _skillViewRoleId = result.RoleId;
        for (int index = 0; index < result.RoleSkillInfoList.Count; index++)
        {
            // 单个已学技能的服务器等级与快捷键绑定。
            RoleSkillInfo roleSkill = result.RoleSkillInfoList[index];
            // 原技能表仍提供显示名称、图标和历史描述。
            SkillInfo skillConfig = LubanMgr.Instance.GetSkillInfoById(
                roleSkill.SkillId);
            if (skillConfig == null)
            {
                continue;
            }
            // 视图绑定与输入索引保持相同大小写，保证固定快捷槽能命中。
            string bindKey = roleSkill.BindKey;
            if (!string.IsNullOrEmpty(bindKey))
            {
                bindKey = bindKey.ToUpperInvariant();
            }
            _skillViews.Add(CreateSkillView(skillConfig, roleSkill.Level, bindKey));
            if (roleSkill.Level > 0 && !string.IsNullOrEmpty(roleSkill.BindKey))
            {
                // 快捷键统一使用大写，兼容服务器快照中的小写配置。
                _skillsByKey[bindKey] = skillConfig;
            }
        }
        ApplySkillCores(localRole);
        RefreshSkillWindows();
    }

    /// <summary>构建技能面板快照，已接入成长的技能从服务器同版表取本级与下一级。</summary>
    /// <param name="definition">配置提供的技能主身份。</param>
    /// <param name="level">服务器确认的当前学习等级。</param>
    /// <param name="bindKey">服务器保存的快捷键。</param>
    /// <returns>当前详情和预览都已解析的技能展示对象。</returns>
    private static SkillViewData CreateSkillView(SkillInfo definition, int level, string bindKey)
    {
        // 尚未学习时可浏览该技能的一级效果，实际显示与施法仍记录零级。
        SkillEffectInfo current = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(definition.Id, Math.Max(1, level));
        if (current != null && current.RequireRoleLevel > 0)
        {
            // 只有已学技能才会有提升一级的目标行，满级查询返回空。
            SkillEffectInfo next = null;
            if (level > 0) next = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(definition.Id, level + 1);
            // 门槛与价格直接取下一等级，避免界面依据当前级别自己推算。
            int requiredLevel = 0;
            // 缺少下一行时由界面隐藏升级入口。
            int copper = 0;
            if (next != null)
            {
                requiredLevel = next.RequireRoleLevel;
                copper = next.UpgradeCopper;
            }
            // 两门特殊 Buff 技能保留完整词条，其他正式武功展示各自能生效的成长数值。
            bool hasSpecialTraits = definition.Id == BlueBarrierSkillId || definition.Id == TornadoSlashSkillId;
            // 已习得的技能才有下一级或满级提示，未解锁时只浏览其实际一级说明。
            string description;
            if (hasSpecialTraits) description = MjsSkillDescription.Build(current, next, level > 0);
            else description = MjsSkillDescription.BuildWeapon(definition, current, next, level > 0);
            return new SkillViewData(definition.Id, definition.Name, description,
                definition.Icon, level, current.CooldownMs / 1000f, bindKey, 0f, definition.Type,
                GetSkillTag(definition), current.MpCost, definition.AtkDistance,
                copper, requiredLevel, true, current.ChiCost);
        }
        return new SkillViewData(definition.Id, definition.Name, definition.Desc, definition.Icon,
            level, definition.CDTime, bindKey, 0f, definition.Type, GetSkillTag(definition),
            definition.SpendMP, definition.AtkDistance);
    }

    /// <summary>用已学等级和职业配置建立本地角色技能核心。</summary>
    /// <param name="localRole">当前场景世界中的本地主角。</param>
    private void ApplySkillCores(PlayerEntity localRole)
    {
        if (localRole == null || localRole.Skill == null)
        {
            return;
        }
        if (localRole.BaseInfo == null ||
            localRole.GetID() != _skillViewRoleId)
        {
            return;
        }
        // 已保存同角色的服务器列表，装配职责按RO归技能组件，控制器只转换已有视图快照。
        List<RoleSkillInfo> skills = new List<RoleSkillInfo>(_skillViews.Count);
        for (int index = 0; index < _skillViews.Count; index++)
        {
            // 已学技能展示快照；零级技能只展示，不允许释放。
            SkillViewData view = _skillViews[index];
            if (view == null || view.Level <= 0)
            {
                continue;
            }
            skills.Add(new RoleSkillInfo { SkillId = view.SkillId, Level = view.Level });
        }
        localRole.Skill.OnAttachSkill(skills);
    }

    /// <summary>应用当前角色已学习技能快照，供网络回包和测试包共用装载链。</summary>
    /// <param name="result">包含角色 ID、技能等级和快捷键绑定的强类型快照。</param>
    /// <returns>快照角色与当前本地主角一致并完成装载时返回 true。</returns>
    private bool ApplyLearnedSkillSnapshot(RoleSkillInfoRet result)
    {
        if (result == null || result.CmdCode != CmdCode.Succeed ||
            result.RoleId <= 0)
        {
            return false;
        }
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null || localRole.GetID() != result.RoleId)
        {
            return false;
        }
        ApplyRoleSkillInfo(result, localRole);
        return true;
    }
    #endregion
}
