using System;
using System.Collections.Generic;
using cfg;
using Google.Protobuf;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>技能界面控制器，负责快捷输入与服务器MP/CD结果展示，施法发送交给RO模块链。</summary>
public sealed class SkillCtrl : CtrlBase
{
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

    /// <summary>待应用技能回包所属的连接代次，零表示没有待应用回包。</summary>
    private long _pendingRoleSkillGeneration;

    /// <summary>快捷键事件是否已绑定，避免 UI 反复初始化时重复施法。</summary>
    private bool _inputRegistered;

    /// <summary>创建控制器并登记 11080 与三个施法协议的唯一处理器。</summary>
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
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            Global.Instance.SceneWorld.LocalRoleChanged += OnLocalRoleChanged;
        }
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.AimButtonClicked += OnAimButton;
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

    /// <summary>装载中心服已学等级，建立长期核心并更新技能列表与快捷栏。</summary>
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

    /// <summary>将协议字节解析为成功且带有效角色 ID 的已学技能快照。</summary>
    /// <param name="data">11080 对应的只读 protobuf 消息体。</param>
    /// <param name="result">解析成功时的强类型角色技能快照，失败时为空。</param>
    /// <returns>消息格式、结果码和角色 ID 均有效时返回 true。</returns>
    private static bool TryParseRoleSkillInfo(ByteString data,
        out RoleSkillInfoRet result)
    {
        result = null;
        if (data == null || data.IsEmpty)
        {
            return false;
        }
        try
        {
            result = RoleSkillInfoRet.Parser.ParseFrom(data);
        }
        catch (InvalidProtocolBufferException exception)
        {
            // 协议边界的解码异常，不能让坏包中断技能窗口刷新链。
            Debug.LogWarning("技能列表回包解析失败：" + exception.Message);
            return false;
        }
        if (result == null || result.CmdCode != CmdCode.Succeed ||
            result.RoleId <= 0)
        {
            return false;
        }
        return true;
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
            _skillViews.Add(new SkillViewData(skillConfig.Id, skillConfig.Name,
                skillConfig.Desc, skillConfig.Icon, roleSkill.Level,
                skillConfig.CDTime, bindKey, 0f));
            if (roleSkill.Level > 0 && !string.IsNullOrEmpty(roleSkill.BindKey))
            {
                // 快捷键统一使用大写，兼容服务器快照中的小写配置。
                _skillsByKey[bindKey] = skillConfig;
            }
        }
        ApplySkillCores(localRole);
        RefreshSkillWindows();
    }

    /// <summary>主角晚于技能列表创建时补装已学效果，不复制旧控制器冷却字典。</summary>
    /// <param name="localRole">刚加入场景世界的本地角色，离场时为空。</param>
    private void OnLocalRoleChanged(PlayerEntity localRole)
    {
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
        // 新角色只消费同一连接代次且 RoleId 相符的待应用列表。
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

    /// <summary>快捷键入口：选中已学技能，交给RO经典释放模块并刷新当前CD视图。</summary>
    /// <param name="key">输入系统上报的固定技能快捷键。</param>
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

    /// <summary>沿 RO 同名入口优先沿用合格敌人，否则自动找敌；本项目无目标攻击只辅助选择与朝向。</summary>
    /// <param name="skillCore">本次已学技能，只有主表 Type 一或二的攻击参与自动选敌。</param>
    /// <param name="target">玩家当前选中实体，可为空或友方。</param>
    /// <returns>最近合格敌人；无敌人或非攻击技能返回空，保留原无目标施法。</returns>
    public MEntity GetTargetFilterSkill(MSkillCore skillCore, MEntity target)
    {
        // 沿现有主表技能类别，滑步和轻功不触发选敌，也不改变其朝向。
        SkillInfo skillInfo = LubanMgr.Instance.GetSkillInfoById(skillCore.SkillId);
        if (skillInfo.Type != 1 && skillInfo.Type != 2)
        {
            return null;
        }
        if (MSkillTargetMgr.singleton.CanAttack(target))
        {
            return target;
        }
        return MSkillTargetMgr.singleton.FindEnemyBySkillBtn(true, null);
    }

    /// <summary>正式瞄准按钮和 Tab 统一交给 RO 同名切换方法。</summary>
    private void OnAimButton()
    {
        MSkillTargetMgr.singleton.FindEnemyByAimBtn();
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
        if (result == null || result.SkillUuid == 0)
        {
            return;
        }
        // 当前本地角色；断线或切图后不可把旧回包写入新角色。
        PlayerEntity localRole = GetLocalRole();
        if (localRole != null && localRole.Skill != null &&
            localRole.GetID() == _skillViewRoleId)
        {
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

    /// <summary>用技能核心的实时 CD 生成视图快照并刷新两个正式窗口。</summary>
    private void RefreshSkillWindows()
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
            // 有效效果配置才使用新表中的权威 CD 时长。
            MSkillCore core = null;
            if (localRole != null && localRole.Skill != null)
            {
                core = localRole.Skill.GetSkillCore(previous.SkillId);
            }
            float duration = previous.Cooldown;
            float remaining = 0f;
            if (core != null)
            {
                duration = core.CooldownSeconds;
                remaining = localRole.Skill.GetRemainingCooldown(
                    previous.SkillId, Time.realtimeSinceStartup);
            }
            views.Add(new SkillViewData(previous.SkillId, previous.Name,
                previous.Description, previous.IconPath, previous.Level, duration,
                previous.BindKey, remaining));
        }
        if (_skillWindow != null)
        {
            _skillWindow.ReFreshUI(views);
        }
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.ReFreshUI(views);
        }
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

    /// <summary>注销协议与输入，避免旧主界面处理新连接消息。</summary>
    public override void Dispose()
    {
        MSkillTargetMgr.singleton.TargetChanged -= OnTargetChanged;
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.AimButtonClicked -= OnAimButton;
        }
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_RoleSkillInfoCode,
            OnRoleSkillInfo);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SkillCastCode,
            OnSkillCastResult);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SkillCastStartCode,
            OnSkillCastStart);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SkillCastEndCode,
            OnSkillCastEnd);
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
}
