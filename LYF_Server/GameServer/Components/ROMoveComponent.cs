using System.Globalization;

/// <summary>按 RO MoveComponent.OnMoveStart 消费在线角色移动上报，使用现有角色位置和附近同步。</summary>
public sealed class ROMoveComponent : RoleComponentBase
{
    /// <summary>最近一次上报的主动移动状态，生命周期与当前在线角色相同。</summary>
    private bool is_moving_;

    /// <summary>最近同步的跳跃段数，0 表示落地；只在新起跳或追加跳跃时取消技能。</summary>
    private int jump_count_;

    /// <summary>创建在线角色的移动组件，不创建额外场景宿主。</summary>
    /// <param name="owner">现有在线角色，负责位置数据与会话生命周期。</param>
    public ROMoveComponent(OnlineRole owner) : base(owner) { }

    /// <summary>查询最近上报的主动移动状态。</summary>
    public bool IsMoving()
    {
        return is_moving_;
    }

    /// <summary>按 RO 顺序更新上报位置和移动状态，再同步给现有附近角色。</summary>
    /// <param name="action">本段终点（米）、朝向（度）、耗时（秒）、移动标志及当前跳跃段（0为落地，1到3为跳跃）。</param>
    public void OnMoveStart(MoveOperationReq action)
    {
        // 先验证权威起点到上报终点的整段，拒绝时不修改角色位置或动作。
        if (!TeamManager.Instance.AllowDungeonMove(Owner, new System.Numerics.Vector3(action.Dx, action.Dy, action.Dz))) return;
        // 新跳跃沿用现有移动协议；同一空中姿势的后续同步不重复触发技能取消。
        if (action.JumpCount > jump_count_ && action.JumpCount <= 3)
        {
            // 只切出技能动作，不把死亡、受控或其他动作强制变为默认态。
            StateMachineComponent jump_state_machine = Owner.GetComponent<StateMachineComponent>();
            if (jump_state_machine.GetCurrentState() == StateDefine.kStateSkill)
            {
                jump_state_machine.ForceToDefaultState(false);
            }
        }
        jump_count_ = action.JumpCount;
        is_moving_ = action.IsMoving;
        // 主动移动必须先打断不可带动作移动的状态（吟唱），对应 RO ActionStateComponent::Move。
        if (is_moving_)
        {
            // state_machine 是本角色的状态机组件；取不到说明角色尚未装配状态机，直接跳过打断。
            StateMachineComponent state_machine;
            if (Owner.TryGetComponent(out state_machine))
            {
                state_machine.OnMove();
            }
        }
        // 复用正式基础快照的下划线坐标格式，使进入世界和技能附近广播读取同一位置。
        Owner.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture,
            "{0}_{1}_{2}", action.Dx, action.Dy, action.Dz);
        TeamManager.Instance.OnDungeonMove(Owner);
        // 本项目没有 RO 场景高度/导航结算，直接沿用当前客户端碰撞完成后的终点。
        RoleManager.Instance.SyncMove(Owner, new SyncMoveNtf
        {
            RoleId = Owner.mainRoleInfo.BaseInfo.RoleId,
            Dx = action.Dx,
            Dy = action.Dy,
            Dz = action.Dz,
            Face = action.Face,
            Time = action.Time,
            IsMoving = is_moving_,
            IsMoved = action.IsMoved,
            // 只转发本段跳跃姿势，不由坐标高度猜测动画，也不另做服务器跳跃物理。
            JumpCount = action.JumpCount
        });
    }
}
