using System.Diagnostics;
using System.Globalization;
using Google.Protobuf;

/// <summary>按 RO MoveComponent.OnMoveStart 消费在线角色移动上报，使用现有角色位置和附近同步。</summary>
public sealed class ROMoveComponent : RoleComponentBase
{
    /// <summary>触发位置纠正所需的 1.2 秒静默时长，单位为 Stopwatch 刻度。</summary>
    private static readonly long PositionReportTimeoutTicks = Stopwatch.Frequency * 1200 / 1000;

    /// <summary>按单调时钟计算的上报截止刻度，0 表示尚未计时，long.MaxValue 表示本次静默已纠正。</summary>
    private long _positionReportDeadlineTimestamp;

    /// <summary>最近一次上报的主动移动状态，生命周期与当前在线角色相同。</summary>
    private bool is_moving_;

    /// <summary>最近同步的跳跃段数，0 表示落地；只在新起跳或追加跳跃时取消技能。</summary>
    private int jump_count_;

    /// <summary>最近一次通过服务器移动处理的角色朝向，单位度；初始未上报时沿现有角色正前方0度。</summary>
    public float Facing { get; private set; }

    /// <summary>创建在线角色的移动组件，不创建额外场景宿主。</summary>
    /// <param name="owner">现有在线角色，负责位置数据与会话生命周期。</param>
    public ROMoveComponent(OnlineRole owner) : base(owner) { }

    /// <summary>查询最近上报的主动移动状态。</summary>
    public bool IsMoving()
    {
        return is_moving_;
    }

    /// <summary>推进移动控制状态，并在位置上报持续缺失时向本人发出一次位置纠正。</summary>
    /// <param name="deltaMilliseconds">固定逻辑帧的推进毫秒数，位置上报超时使用独立的单调时钟。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (is_moving_ && !Owner.GetComponent<RoleEquipComponent>().Runtime.CanMove) is_moving_ = false;

        if (Owner.State != OnlineRoleState.Online)
        {
            // 离线清除截止刻度，重新在线后从首次更新的时刻开始计时。
            _positionReportDeadlineTimestamp = 0;
            return;
        }

        // 当前单调时钟刻度，固定逻辑帧补跑时不会随逻辑推进量跳跃。
        long nowTimestamp = Stopwatch.GetTimestamp();
        if (_positionReportDeadlineTimestamp == 0)
        {
            _positionReportDeadlineTimestamp = nowTimestamp + PositionReportTimeoutTicks;
            return;
        }
        if (nowTimestamp < _positionReportDeadlineTimestamp)
        {
            return;
        }
        // 同一次静默只纠正一次，接到下一包后才重新设定截止刻度。
        _positionReportDeadlineTimestamp = long.MaxValue;

        // 基础快照保存的是服务器接受的位置，坐标分量单位为米。
        System.Numerics.Vector3 position = Owner.mainRoleInfo.BaseInfo.Pos.ToVector3();
        Owner.SendToClient(NetDefine.CMD_MovePositionCorrectionCode, new SyncMoveNtf
        {
            RoleId = Owner.mainRoleInfo.BaseInfo.RoleId,
            Dx = position.X,
            Dy = position.Y,
            Dz = position.Z
        }.ToByteString());
    }

    /// <summary>记录上报时间，并按现有移动流程处理发生变化的位置和姿态。</summary>
    /// <param name="action">本段终点（米）、朝向（度）、耗时（秒）、移动标志及当前跳跃段（0为落地，1到3为跳跃）。</param>
    public void OnMoveStart(MoveOperationReq action)
    {
        // 按当前收包时刻重设 1.2 秒后的截止刻度，站立心跳和未通过位置限制的上报也会续期。
        _positionReportDeadlineTimestamp = Stopwatch.GetTimestamp() + PositionReportTimeoutTicks;
        // 眩晕和定身阻止继续提交位移，同时结束角色现有的移动标记。
        if (!Owner.GetComponent<RoleEquipComponent>().Runtime.CanMove)
        {
            is_moving_ = false;
            return;
        }
        // 请求的世界位置沿用基础快照的下划线坐标格式，坐标单位为米。
        string reportedPosition = string.Format(CultureInfo.InvariantCulture,
            "{0}_{1}_{2}", action.Dx, action.Dy, action.Dz);
        // 已处于静止且没有任何位移的上报可以仅用于续期。
        bool isIdleReport = !action.IsMoving && !action.IsMoved && !is_moving_;
        if (isIdleReport)
        {
            // 同坐标、同朝向、同跳跃状态的站立心跳不触发区域事件或附近广播。
            if (reportedPosition == Owner.mainRoleInfo.BaseInfo.Pos && action.Face == Facing && action.JumpCount == jump_count_)
            {
                return;
            }
        }
        // 验证权威起点到上报终点的整段，拒绝时不修改角色位置或动作。
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
        // 获准移动前的世界坐标（米），用于判断玩家是否刚跨过视野边界。
        System.Numerics.Vector3 previousPosition = Owner.mainRoleInfo.BaseInfo.Pos.ToVector3();
        // 进入世界和技能附近广播都读取这份已通过限制的基础位置。
        Owner.mainRoleInfo.BaseInfo.Pos = reportedPosition;
        // 与即将同步给其他角色的Face保存同一值，供原RO背对伤害判定读取。
        Facing = action.Face;
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
        }, previousPosition);
    }
}
