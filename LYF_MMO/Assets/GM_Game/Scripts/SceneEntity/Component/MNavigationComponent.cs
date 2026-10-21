using System;
using System.Collections.Generic;
using Google.Protobuf;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>沿RO导航流程维护目的地、完整路线、逐帧路点与到达后的强类型技能动作。</summary>
public class MNavigationComponent : EntityComponentBase
{
    /// <summary>沿RO common_define.h的SELECT_TARGET_DISTANCE_HEIGHT，目标高度差超过6米不能按水平距离到达。</summary>
    private const float SelectTargetDistanceHeight = 6f;
    /// <summary>使用项目已有导航组件编号20，由本地主角普通组件容器挂载和回收。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Navigation; } }
    /// <summary>导航事件处理器，随实体组件挂载订阅、卸载时由基类解除。</summary>
    private readonly MEventHandler _onGetEvent;
    /// <summary>本次查询返回的世界路点，生命周期随组件；中断时清空。</summary>
    private readonly List<Vector3> _pathPoints = new List<Vector3>();
    /// <summary>下一未消费路点的零基索引。</summary>
    private int _currentPathIdx;
    /// <summary>当前是否有有效的路线需要移动组件消费。</summary>
    private bool _isNavigation;
    /// <summary>查询完成后的可达目的地，单位米。</summary>
    private Vector3 _destination;
    /// <summary>当前位置或目标实体提供的导航目标，单位米。</summary>
    private Vector3 _tarPosition;
    /// <summary>是否按目标实体位置变化更新当前路线。</summary>
    private bool _naviWithTarget;
    /// <summary>当前导航追近的原实体引用；中断、到达及卸载时清空。</summary>
    private MEntity _target;
    /// <summary>允许到达的水平距离上限，单位米。</summary>
    private float _upperRange;
    /// <summary>允许到达的水平距离下限，单位米。</summary>
    private float _lowerRange;
    /// <summary>非零时替代上下限的水平距离，单位米。</summary>
    private float _customRange;
    /// <summary>到达后是否释放当前持有的技能动作。</summary>
    private bool _useAfterAction;
    /// <summary>跨帧独立持有的技能指令复制件，中断或到达后回收。</summary>
    private MSkillEventArgs _afterAction;

    /// <summary>移动组件据此选择RO导航路点分支，不占用手动方向输入标记。</summary>
    public bool IsNavigation { get { return _isNavigation; } }
    /// <summary>当前完整可达路线的末点，世界单位为米。</summary>
    public Vector3 Destination { get { return _destination; } }

    /// <summary>创建实体导航模块并绑定当前生命周期内的事件入口。</summary>
    /// <param name="owner">拥有导航和已学技能的本地主角实体。</param>
    public MNavigationComponent(ROObject owner) : base(owner)
    {
        _onGetEvent = new MEventHandler(OnGetEvent);
    }

    /// <summary>沿RO导航事件入口接收NPC、显式坐标与技能导航指令。</summary>
    protected override void EventSubscribe()
    {
        RegisterEvent(MEventType.MEvent_Navigation, _onGetEvent);
    }

    /// <summary>中断当前路线并释放未执行动作；方向接管时由后续输入设置移动态。</summary>
    /// <param name="toIdle">true立即停止水平移动；false交给同帧方向或技能流程接管。</param>
    public void Interrupt(bool toIdle = true)
    {
        // 无导航时不干预已有手动方向移动或技能动作。
        bool wasNavigation = _isNavigation;
        close();
        clearAfterAction();
        if (wasNavigation && toIdle)
        {
            Entity.MoveComp.StopHorizontalMovement();
        }
    }

    /// <summary>对象池回收时释放跨帧技能复制件与路线，不触发业务到达。</summary>
    public override void Shutdown()
    {
        close();
        clearAfterAction();
    }

    /// <summary>保存RO目的点、范围和目标；原有无目标技能仍立即走原施法入口。</summary>
    /// <param name="e">同步派发结束即回收的具名导航指令。</param>
    protected void OnGetEvent(IEventArg e)
    {
        // 当前事件只在同步入口读取，延后执行的技能参数使用独立复制件。
        MNavigationEventArgs args = (MNavigationEventArgs)e;
        Interrupt();
        if (args.UseAfterAction && args.NavAction != null && !args.NeedTarget)
        {
            closeTargetAfter(args.NavAction);
            return;
        }
        _tarPosition = args.Pos;
        _target = args.Target;
        _naviWithTarget = args.NeedTarget;
        _upperRange = args.UpperRange;
        _lowerRange = args.LowerRange;
        _customRange = args.CustomRange;
        _useAfterAction = args.UseAfterAction;
        if (args.NavAction != null)
        {
            _afterAction = (MSkillEventArgs)args.NavAction.Clone();
        }
        // 新导航接管原方向；导航不伪装成持续按键，输入取消不会每帧误停路线。
        Entity.MoveComp.StopHorizontalMovement();
        navigate();
    }

    /// <summary>本项目普通组件Update位于MMove之后，借此接入RO帧后导航入口。</summary>
    /// <param name="deltaMilliseconds">当前逻辑帧间隔，单位毫秒。</param>
    public override void Update(int deltaMilliseconds)
    {
        // MEntity.LateUpdate当前不派发普通组件；仅本组件适配现有顺序，不改实体生命周期。
        LateUpdate(deltaMilliseconds * 0.001f);
    }

    /// <summary>沿RO在移动后更新追近目标及到达，实际碰撞位置决定能否触发技能。</summary>
    /// <param name="deltaTime">本次帧后间隔，单位秒；移动预算由MMoveComponent计算。</param>
    public override void LateUpdate(float deltaTime)
    {
        if (!_isNavigation || !Entity.IsMovable)
        {
            return;
        }
        if (_naviWithTarget)
        {
            if (_target == null || _target.LifecycleState != EntityLifecycleState.Active)
            {
                Interrupt();
                return;
            }
            // 目标本帧的实际世界位置，变化后沿RO重新查询，不启动等待队列。
            Vector3 targetPoint = _target.Position;
            if ((targetPoint - _tarPosition).sqrMagnitude > Vector3.kEpsilon)
            {
                _tarPosition = targetPoint;
                navigate();
            }
        }
        if (_isNavigation && isTargetInRange())
        {
            onCloseTarget();
        }
    }

    /// <summary>按实际位置消费路点，同帧可跨共线门户，但单次碰撞位移止于真实转角。</summary>
    /// <param name="orgPos">本帧开始时的角色实际世界位置，单位米。</param>
    /// <param name="dis">本帧允许的非负水平移动路程，单位米。</param>
    /// <param name="nextPos">路线上的下一位置；失败时保留起点。</param>
    /// <returns>当前路线有可移动的直线段或已走到路线末点时为true。</returns>
    public bool TryGetNextStep(Vector3 orgPos, float dis, out Vector3 nextPos)
    {
        nextPos = orgPos;
        if (!_isNavigation || _pathPoints.Count == 0)
        {
            return false;
        }
        // 只以碰撞提交后的实际位置推进真实索引，预测会抵达的转角不能提前消费。
        while (_currentPathIdx < _pathPoints.Count)
        {
            // 实际位置到当前路点的水平差量，单位米。
            Vector3 tmp = _pathPoints[_currentPathIdx] - orgPos;
            tmp.y = 0f;
            if (tmp.sqrMagnitude <= 0.0001f)
            {
                _currentPathIdx++;
                continue;
            }
            if (_currentPathIdx > 0)
            {
                // 前一段的水平单位方向，用于识别实际已跨过的共线短门户。
                Vector3 pathDir = _pathPoints[_currentPathIdx] - _pathPoints[_currentPathIdx - 1];
                pathDir.y = 0f;
                pathDir = pathDir.normalized;
                // 当前位置沿原线段剩余的有向米数，负值表示已实际越过此路点。
                float remainingDistance = Vector3.Dot(tmp, pathDir);
                // 偏离线段的水平距离平方；只忽略一厘米内的浮点与贴边误差。
                float sideDistanceSquared = (tmp - pathDir * remainingDistance).sqrMagnitude;
                if (remainingDistance <= 0f && sideDistanceSquared <= 0.0001f)
                {
                    _currentPathIdx++;
                    continue;
                }
            }
            break;
        }
        if (_currentPathIdx >= _pathPoints.Count)
        {
            nextPos = _pathPoints[_pathPoints.Count - 1];
            return true;
        }
        // 本帧第一段方向，遇到非共线转角后不再拼成斜切障碍的弦。
        Vector3 direction = _pathPoints[_currentPathIdx] - orgPos;
        direction.y = 0f;
        direction = direction.normalized;
        // 仅扫描同向共线门户，不把预测末点写入真实消费索引。
        int nextPathIdx = _currentPathIdx;
        while (nextPathIdx + 1 < _pathPoints.Count)
        {
            // 下一门户相对当前门户的水平向量，用于检测转角与折返。
            Vector3 segment = _pathPoints[nextPathIdx + 1] - _pathPoints[nextPathIdx];
            segment.y = 0f;
            // 单位方向与该段的水平叉积，绝对值是该段偏离直线的米数。
            float sideDistance = direction.x * segment.z - direction.z * segment.x;
            if (Mathf.Abs(sideDistance) > 0.001f || Vector3.Dot(segment, direction) < 0f)
            {
                break;
            }
            nextPathIdx++;
        }
        nextPos = _pathPoints[nextPathIdx];
        // 选中直线段剩余的水平向量，碰撞、重力与跳跃仍由原移动组件合并。
        Vector3 movement = nextPos - orgPos;
        movement.y = 0f;
        if (movement.magnitude > dis)
        {
            nextPos = orgPos + direction * dis;
        }
        return true;
    }

    /// <summary>按RO顺序判定范围、选择目标或位置查询，再启动既有移动动画。</summary>
    private void navigate()
    {
        _isNavigation = true;
        if (_naviWithTarget)
        {
            if (_target == null || _target.LifecycleState != EntityLifecycleState.Active)
            {
                Interrupt();
                return;
            }
            _tarPosition = _target.Position;
        }
        if (isTargetInRange())
        {
            onCloseTarget();
            return;
        }
        if (Entity.IsMovable)
        {
            Entity.Machine.CurrentState.Move();
        }
        if (_naviWithTarget)
        {
            navToTarget();
        }
        else
        {
            navToPos();
        }
        if (_isNavigation)
        {
            Entity.MoveComp.SetMoving(true);
        }
    }

    /// <summary>查询当前目的点的完整可达路线，失败时中断且不执行到达动作。</summary>
    private void navToPos()
    {
        _currentPathIdx = 0;
        // 目的点按当前距离范围计算，单位米。
        Vector3 pos = calcTargetPos();
        if (!MNavigationMgr.singleton.TryGetPath(Entity.Position, pos, _pathPoints))
        {
            Interrupt();
            return;
        }
        _destination = _pathPoints[_pathPoints.Count - 1];
        if (!_naviWithTarget && _upperRange == 0f && _customRange == 0f)
        {
            // 显式坐标导航以吸附成功的可达末点停止，不追加不可走的原始坐标。
            _tarPosition = _destination;
        }
    }

    /// <summary>沿RO目标导航入口复用当前位置与范围查询，目标位置由navigate和LateUpdate更新。</summary>
    private void navToTarget()
    {
        navToPos();
    }

    /// <summary>按RO上下限中值选追近点，目标自身或显式坐标范围为零时返回原目标。</summary>
    /// <returns>本次查询使用的世界目的点，单位米。</returns>
    private Vector3 calcTargetPos()
    {
        // 自己指向目标的水平向量，保留目标原高度供最近导航面查询。
        Vector3 dir = _tarPosition - Entity.Position;
        dir.y = 0f;
        if (dir == Vector3.zero)
        {
            return _tarPosition;
        }
        // 目标范围中值，显式坐标导航为零；自定义到达范围使用同样的范围中心。
        float range = (_upperRange + _lowerRange) * 0.5f;
        if (_customRange > 0f)
        {
            range = _customRange * 0.5f;
        }
        return _tarPosition - dir.normalized * range;
    }

    /// <summary>按RO水平距离和0.01米边界余量判断是否到达本次指定范围。</summary>
    /// <returns>当前位置进入上下限或非零自定义范围时为true。</returns>
    private bool isTargetInRange()
    {
        // 世界坐标差，NPC普通交互使用原对话三维距离，技能仍使用RO水平与高度规则。
        Vector3 dir = _tarPosition - Entity.Position;
        if (_target is NpcEntity && !_useAfterAction)
        {
            return dir.magnitude <= NpcCtrl.TalkDistance;
        }
        // 沿RO先排除超过6米的不同高度，再判水平距离；保留原参考范围语义。
        if (Mathf.Abs(dir.y) > SelectTargetDistanceHeight)
        {
            return false;
        }
        dir.y = 0f;
        // 当前水平目标距离，单位米。
        float dis = dir.magnitude;
        if (_customRange > 0f)
        {
            return dis < _customRange + 0.01f;
        }
        return dis > _lowerRange - 0.01f && dis < _upperRange + 0.01f;
    }

    /// <summary>到达后先结束路线和水平移动，再交给原NPC对话控制器或原技能流程。</summary>
    private void onCloseTarget()
    {
        // close会清理目标；只在普通NPC导航到达时保存现有表现控制器供本次同步调用。
        NpcCtrl npcCtrl = null;
        if (_target is NpcEntity && !_useAfterAction)
        {
            npcCtrl = _target.GetEntityCtrl() as NpcCtrl;
        }
        close();
        Entity.MoveComp.StopHorizontalMovement();
        if (npcCtrl != null)
        {
            npcCtrl.OpenTalk(Entity.GetEntityCtrl());
        }
        if (_useAfterAction && _afterAction != null)
        {
            closeTargetAfter(_afterAction);
        }
        clearAfterAction();
    }

    /// <summary>只清除本次路线与目标，不把中断当作到达。</summary>
    private void close()
    {
        _isNavigation = false;
        _pathPoints.Clear();
        _currentPathIdx = 0;
        _target = null;
        _naviWithTarget = false;
        _destination = Vector3.zero;
    }

    /// <summary>释放当前导航独占的技能复制件，防止池化指令跨导航复用。</summary>
    private void clearAfterAction()
    {
        if (_afterAction != null)
        {
            _afterAction.Recycle();
            _afterAction = null;
        }
        _useAfterAction = false;
    }

    /// <summary>RO同名技能到达入口，先上报已有位置同步，再进入原技能发送流程。</summary>
    /// <param name="skillArgs">本次到达后执行的强类型技能指令。</param>
    private void closeTargetAfter(MSkillEventArgs skillArgs)
    {
        // 当前位置上报继续沿既有协议和移动状态，不新建导航消息。
        PlayerEntity player = (PlayerEntity)Entity;
        player.UpdateMove();
        OnAfterAction(skillArgs);
    }

    /// <summary>沿RO到达回调发送技能请求，成功发送后立即开始原本地预表现，不增加确认等待。</summary>
    /// <param name="skillArgs">本次输入的技能主表ID、释放点和可空目标。</param>
    protected void OnAfterAction(MSkillEventArgs skillArgs)
    {
        // 导航模块所属本地主角；当前无目标协议不上传尚未接入的目标或施法点。
        PlayerEntity localRole = Entity as PlayerEntity;
        // 原施法输入时钟，单位为不缩放秒；保留统一跳跃、特殊滑步、MP与CD许可。
        float realtimeSeconds = Time.realtimeSinceStartup;
        if (!localRole.Skill.
                CanStart(skillArgs.Identify, realtimeSeconds,
                    localRole.BaseInfo.CurrMp))
        {
            return;
        }
        // RO实体施法UUID在1至9999循环，请求与本地预表现共用这一笔身份。
        ulong skillUuid = localRole.NextSkillUUID;
        if (NetSocketMgr.Client != null)
        {
            try
            {
                // 沿原无目标协议，只发送UUID与主表ID，不扩导航或目标字段。
                SkillCastReq request = new SkillCastReq { SkillUuid = skillUuid, SkillId = skillArgs.Identify };
                NetSocketMgr.Client.SendData(NetDefine.CMD_SkillCastCode, request.ToByteString());
            }
            catch (Exception exception)
            {
                // 原发送失败语义保持：请求未提交时不启动本次预表现。
                Debug.LogError("技能施法请求发送失败：" + exception);
                return;
            }
        }
        // 只有已通过原技能准入的攻击辅助转向；目标只是选择辅助，不改变无目标技能的施法点。
        if (skillArgs.Target != null)
        {
            // 当前主表类别，Type 三的滑步不自动转向目标。
            cfg.SkillInfo skillInfo = LubanMgr.Instance.GetSkillInfoById(skillArgs.Identify);
            if (skillInfo.Type == 1 || skillInfo.Type == 2)
            {
                // 只使用水平朝向，单位米；目标与自己重合时保持原旋转。
                Vector3 direction = skillArgs.Target.Position - localRole.Position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0f)
                {
                    localRole.Model.Rotation = Quaternion.LookRotation(direction);
                }
            }
        }
        localRole.Skill.BeginPrediction(skillArgs.Identify, skillUuid,
            Time.realtimeSinceStartup, localRole.BaseInfo.CurrMp);
    }
}
