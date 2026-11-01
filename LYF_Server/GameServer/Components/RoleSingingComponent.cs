/// <summary>
/// 施法请求组件，对应参考工程 rogamelibs/entity/action/ExtendAction/role_singing_component.cpp。
/// 只承担一件事：把客户端施法事件转换成吟唱事件，真正的释放校验与结算由技能组件负责。
/// </summary>
public sealed class RoleSingingComponent : RoleComponentBase, IROEventListener
{
    /// <summary>创建施法请求组件并绑定在线角色。</summary>
    /// <param name="owner">持有角色快照、技能组件与事件派发器的在线角色。</param>
    public RoleSingingComponent(OnlineRole owner)
        : base(owner)
    {
    }

    /// <summary>组件初始化时向角色派发器登记客户端施法事件监听。</summary>
    public override void Initialize()
    {
        base.Initialize();
        Owner.GetEventDispatcher().AddEventListener(
            ROEventDefine.kActionClienFireSkill, this, OnClientFireSkillRequest);
    }

    /// <summary>组件释放时解除监听，避免角色销毁后仍被回调。</summary>
    public override void Dispose()
    {
        if (Owner != null)
        {
            Owner.GetEventDispatcher().RemoveEventListener(
                ROEventDefine.kActionClienFireSkill, this);
        }
        base.Dispose();
    }

    /// <summary>
    /// 处理客户端施法事件：把请求字段原样转成吟唱事件并派发。
    /// 本方法只保留 RO 的分层命名（请求事件 → 吟唱事件），不搬运 RO 在同一函数里做的
    /// 技能核解析（GetReplaceSkillCoreOnClientRequest）、学习/UUID 硬拦截与 CheckDoSkill，
    /// 这些校验由现有技能组件在吟唱事件中复用 TryCast 完成，不另建第二套判定。
    /// </summary>
    /// <param name="args">派发器传入的客户端施法事件。</param>
    private void OnClientFireSkillRequest(ROEvent args)
    {
        // 事件类型由监听表保证，这里只做载荷类型确认。
        ROClientFireSkillArgs action = args as ROClientFireSkillArgs;
        if (action == null)
        {
            return;
        }

        // 吟唱事件逐字段承接请求，保持 RO 的事件分层与字段命名。
        ROSingingArgs singing_args = new ROSingingArgs();
        singing_args.skill_id_ = action.skill_id_;
        singing_args.target_ = action.target_;
        singing_args.cast_point_ = action.cast_point_;
        singing_args.skill_uuid_ = action.skill_uuid_;

        Owner.GetEventDispatcher().DispatcherEvent(singing_args);
    }
}
