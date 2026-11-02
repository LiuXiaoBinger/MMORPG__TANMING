using System;

/// <summary>
/// 吟唱状态组件，对应参考工程 rogamelibs/entity/action/singing_component.h/.cpp。
/// 只承担"读条计时 + 到期派发攻击动作事件"两件事，权威校验与结算仍由技能组件负责。
/// 按本项目边界裁剪：无目标/Buff/仇恨/TV 公告/ATTR_SPECIAL_* 属性。
/// </summary>
public class SingingComponent : ActionStateComponent<ROSingingArgs>
{

    #region 字段与属性
    /// <summary>本次吟唱要释放的技能主表 ID。</summary>
    protected int skill_id_;

    /// <summary>本次吟唱对应的技能等级效果 ID。</summary>
    protected int skill_effect_id_;

    /// <summary>已累计的吟唱毫秒数，由 OnUpdate 逐帧累加。</summary>
    protected int duration_;

    /// <summary>本次真正需要的吟唱毫秒数；与技能动作时长 CastMs 无关。</summary>
    protected int interval_;

    /// <summary>本次吟唱是否真的读过条；零时长技能直接结束，标记为 false。</summary>
    protected bool start_singing_;

    /// <summary>客户端本次施法标识，贯穿开始与结束通知。</summary>
    protected ulong skill_uuid_;

    /// <summary>本次吟唱解析出的技能等级核心，结束后置空。</summary>
    protected ROSkillCore core_;

    /// <summary>吟唱状态由 kActionSinging 事件驱动，对应 RO 把该事件绑定到 SingingComponent。</summary>
    protected override ROEventDefine? ActionEventType
    {
        get { return ROEventDefine.kActionSinging; }
    }
    #endregion

    #region 构造与初始化
    /// <summary>创建吟唱状态组件。</summary>
    /// <param name="owner">状态机所属的在线角色。</param>
    public SingingComponent(OnlineRole owner)
        : base(owner, StateDefine.kStateSinging)
    {
    }

    /// <summary>
    /// 进入吟唱：解析技能核心并通过 GetSingingTime 取得真正的读条时长，
    /// 对应 singing_component.cpp 的 OnStart（:422-617）。
    /// 零时长技能不读条，直接 Finish，与 RO 的 else 分支一致。
    /// </summary>
    /// <param name="args">触发进入的吟唱事件。</param>
    protected override void OnStart(ROSingingArgs args)
    {
        duration_ = 0;
        interval_ = 0;
        start_singing_ = false;
        core_ = null;
        if (args == null)
        {
            // 没有事件就没有可吟唱的技能，立即结束，让状态机回默认态。
            Finish();
            return;
        }
        skill_id_ = args.skill_id_;
        skill_uuid_ = args.skill_uuid_;

        // skill_component 是角色身上的技能组件（本工程为 RoleSkillComponent 实现），用于解析技能主表与效果行。
        ROSkillComponent skill_component = GetSkillComponent();
        if (skill_component == null)
        {
            Finish();
            return;
        }
        core_ = skill_component.ResolveSkillCore(skill_id_);
        if (core_ == null)
        {
            // 技能未学习或配置缺失时不读条，直接结束，由技能组件按既有拒绝码回包。
            Finish();
            return;
        }
        skill_effect_id_ = core_.SkillEffectID();
        interval_ = GetSingingTime(core_);
        if (interval_ <= 0)
        {
            // 零吟唱技能没有读条阶段，直接进入攻击动作。
            Finish();
            return;
        }
        start_singing_ = true;
    }
    #endregion

    #region 事件回调
    /// <summary>
    /// 状态机切换前的校验，对应 singing_component.cpp 的 OnGetEvent（:273-420）。
    /// 本项目只校验事件载荷与角色可用性，目标/Buff/仇恨校验未移植。
    /// </summary>
    /// <param name="args">触发切换的吟唱事件。</param>
    /// <param name="last_state">切换前状态机所处的状态。</param>
    protected override bool OnGetEvent(ROSingingArgs args, StateDefine last_state)
    {
        if (args == null || args.skill_id_ <= 0 || args.skill_uuid_ == 0UL)
        {
            return false;
        }
        if (Owner == null || Owner.State != OnlineRoleState.Online || !Owner.IsRoleDataReady())
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 逐帧累加真实吟唱时长，达到 interval_ 后自然结束，
    /// 对应 singing_component.cpp 的 OnUpdate（:619-661）。
    /// </summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    protected override void OnUpdate(int deltaMilliseconds)
    {
        duration_ += deltaMilliseconds;
        if (duration_ < interval_)
        {
            return;
        }
        Finish();
    }

    /// <summary>
    /// 被打断时清空吟唱上下文，对应 singing_component.cpp 的 OnStop（:663-680）。
    /// 这里只做 RO 原语义的状态清理；"被打断的请求必须回明确终态"由
    /// RoleSingingComponent 覆写本方法实现，避免把项目协议塞进 RO 纯逻辑里。
    /// </summary>
    /// <param name="next">即将进入的状态。</param>
    /// <param name="args">触发停止的事件。</param>
    protected override void OnStop(StateDefine next, ROEvent args)
    {
        start_singing_ = false;
        core_ = null;
    }

    /// <summary>
    /// 吟唱自然结束：派发攻击动作事件，对应 singing_component.cpp 的 OnFinished（:682-688）。
    /// </summary>
    protected override void OnFinished()
    {
        FireSkill();
        core_ = null;
        start_singing_ = false;
    }
    #endregion

    #region 查询与判定
    /// <summary>
    /// 返回真正的吟唱毫秒数，对应 RO 的 GetSingingTime。
    /// RO 从 FixSingingTime 与 FloatSingingTime 计算读条；本项目当前没有这两项配置，
    /// 已接入的技能均为非吟唱技能，因此返回零。CastMs 是动作时长，不能用来代替吟唱。
    /// </summary>
    /// <param name="skillcore">已解析的技能核心；当前配置没有独立吟唱时长，所有核心均按零处理。</param>
    /// <returns>当前技能的吟唱毫秒数，现阶段固定为零。</returns>
    protected int GetSingingTime(ROSkillCore skillcore)
    {
        return 0;
    }

    /// <summary>本次吟唱是否真的读过条。</summary>
    public bool IsStartSinging()
    {
        return start_singing_;
    }

    /// <summary>
    /// 吟唱能否被打断进入目标状态，对应 singing_component.cpp 的 CanActiveInterrupt。
    /// 移动、死亡等目标一律放行；但新的吟唱请求不允许打断当前吟唱
    /// （RO 互斥表 State_AJ_StateSinging × State_AJ_StateSinging 为 -1 未配置，
    /// 由 RoleSingingComponent 的请求队列延后到本次吟唱结束后再执行）。
    /// </summary>
    /// <param name="state">即将进入的目标状态。</param>
    public override bool CanActiveInterrupt(StateDefine state)
    {
        return state != StateDefine.kStateSinging;
    }

    /// <summary>
    /// 吟唱期间不允许带动作移动，必须先打断吟唱，对应 singing_component.cpp 的 CanActiveMove。
    /// </summary>
    public override bool CanActiveMove()
    {
        return false;
    }

    /// <summary>取本角色身上的技能组件；未注册时为空。</summary>
    /// <returns>角色技能组件，未注册返回空。</returns>
    protected ROSkillComponent GetSkillComponent()
    {
        if (Owner == null)
        {
            return null;
        }
        // skill_component 接收角色身上的技能组件实现，用来把基础类型转成本工程的具体实现。
        RoleSkillComponent skill_component;
        if (Owner.TryGetComponent(out skill_component))
        {
            return skill_component;
        }
        return null;
    }
    #endregion

    #region 核心逻辑
    /// <summary>
    /// 每帧前置检查：角色离线、数据未就绪或血量归零时立即打断吟唱，
    /// 对应 RO 在 OnUpdate 开头做的合法性校验。
    /// </summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    protected override void PreUpdate(int deltaMilliseconds)
    {
        if (Owner == null || Owner.mainRoleInfo == null || Owner.mainRoleInfo.BaseInfo == null)
        {
            Stop(StateDefine.kStateDefault, null);
            return;
        }
        if (Owner.State != OnlineRoleState.Online || Owner.mainRoleInfo.BaseInfo.CurrHp <= 0)
        {
            Stop(StateDefine.kStateDefault, null);
        }
    }

    /// <summary>
    /// 派发攻击动作事件，对应 singing_component.cpp 的 FireSkill（:690-704）。
    /// 事件从对象池取用，派发结束后立即释放调用方引用。
    /// </summary>
    protected virtual void FireSkill()
    {
        if (Owner == null)
        {
            return;
        }
        // args 是从对象池取用的攻击动作事件，派发后在 finally 里归还，避免每次施法新建对象。
        ROAttackActionArgs args = ROEventPool<ROAttackActionArgs>.Instance.Get();
        try
        {
            args.skill_id_ = skill_id_;
            args.skill_uuid_ = skill_uuid_;
            // 本项目协议不携带目标与施法点，保持 RO 字段的零值默认。
            args.target_ = 0UL;
            args.cast_point_ = new ROVector3(0f, 0f, 0f);
            Owner.GetEventDispatcher().DispatcherEvent(args);
        }
        finally
        {
            args.Release();
        }
    }
    #endregion
}
