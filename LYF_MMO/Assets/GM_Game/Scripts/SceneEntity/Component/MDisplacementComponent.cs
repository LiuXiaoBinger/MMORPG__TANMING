using UnityEngine;
using MMoRpgCommon.SceneEntity;

/// <summary>实体普通程序位移组件；容器在移动提交前计算水平差量，由 MMoveComponent 合并重力和碰撞。</summary>
public sealed class MDisplacementComponent : EntityComponentBase
{
    /// <summary>当前段及尚未提交差量所属的原技能对象。</summary>
    private MSkill _owner;

    /// <summary>当前持有的原施法 UUID，停止请求必须匹配。</summary>
    private ulong _skillUuid;

    /// <summary>开始时冻结的施法代次，不能用 Reset 后的新代次替代。</summary>
    private ulong _fireGeneration;

    /// <summary>正在推进的原位移数据，段完成后置空但保留待提交差量。</summary>
    private MSkillChargeData _data;

    /// <summary>本段标称开始毫秒，所有进度只来自 MTimerMgr。</summary>
    private long _startTimeMilliseconds;

    /// <summary>最近推进时观察到的全局毫秒数，管理器重置后立即丢弃旧运动。</summary>
    private long _lastClockMilliseconds;

    /// <summary>本段锁定的水平单位方向，后续输入和动画朝向变化不改它。</summary>
    private Vector3 _direction;

    /// <summary>本段已计算的前向累计距离，单位米，Continue 时允许为负。</summary>
    private float _appliedDistance;

    /// <summary>等待本帧移动合并的一次水平世界差量，单位米，多个同 tick 段可累计。</summary>
    private Vector3 _translation;

    /// <summary>是否已经取得可用模型并设置运动基线；冷模型首次到达不追补历史位移。</summary>
    private bool _started;

    /// <summary>实际产生本段基线的原生模型对象，同模型同步换 GO 时也必须丢弃旧差量。</summary>
    private GameObject _loadedGameObject;

    /// <summary>普通容器中的独立位移身份，不能与输入移动组件共用类型键。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Displacement; } }

    /// <summary>由组件管理器创建实体位移组件，宿主绑定与复用由基类统一管理。</summary>
    /// <param name="owner">创建时的宿主实体；池复用时会重新绑定新实体。</param>
    public MDisplacementComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>普通容器的毫秒更新入口，在输入移动提交前统一调度一次。</summary>
    /// <param name="deltaMilliseconds">本帧间隔，单位毫秒；曲线实际以全局计时器的绝对时钟求值。</param>
    public override void Update(int deltaMilliseconds)
    {
        Update(deltaMilliseconds * 0.001f);
    }

    /// <summary>从唯一全局计时器计算累计曲线差量，暂停或同 clock 重复更新不会重复积分。</summary>
    /// <param name="fDeltaT">保留 RO Update 秒接口；进度以标称绝对时钟计算，不再叠加该帧秒数。</param>
    public override void Update(float fDeltaT)
    {
        // 暂停或宿主失效时停止本段位移；保持原 || 短路顺序逐组早退。
        if (!Enabled || Entity == null || Entity.IsFake)
        {
            StopDisplacement();
            return;
        }
        // owner 非空后再校验归属，不满足同样立即停止。
        if (_owner == null || !_owner.OwnsDisplacement(Entity, _skillUuid, _fireGeneration))
        {
            StopDisplacement();
            return;
        }
        if (MTimerMgr.singleton.ElapsedTime < _lastClockMilliseconds)
        {
            StopDisplacement();
            return;
        }
        _lastClockMilliseconds = MTimerMgr.singleton.ElapsedTime;
        if (_data == null || MTimerMgr.singleton.IsPause)
        {
            return;
        }
        // 当前段在全局时钟中的经过秒数，时钟重置后不能倒退或重新交付旧运动。
        float elapsedSeconds = (MTimerMgr.singleton.ElapsedTime - _startTimeMilliseconds) * 0.001f;
        if (elapsedSeconds < 0f)
        {
            StopDisplacement();
            return;
        }
        if (Entity.Model == null || !Entity.Model.IsLoaded)
        {
            _started = false;
            _loadedGameObject = null;
            _translation = Vector3.zero;
            return;
        }
        if (!_started || !ReferenceEquals(_loadedGameObject, Entity.Model.GameObject.NativeObject))
        {
            _started = true;
            _loadedGameObject = Entity.Model.GameObject.NativeObject;
            _translation = Vector3.zero;
            SetLateBaseline(elapsedSeconds);
            return;
        }
        // 曲线使用段内归一化时间；零时长直接评价终值，掉帧跨末端也只交付一次。
        float durationSeconds = _data.End - _data.At;
        // 本次曲线评价位置，范围零至一，不额外给 Program 结束帧加一帧。
        float progress = 1f;
        if (durationSeconds > 0f)
        {
            progress = Mathf.Clamp01(elapsedSeconds / durationSeconds);
        }
        // Y maxDistance 硬上限始终生效，Continue 可回拉但不能继续向前超限。
        float targetDistance = _data.CurveZ.Evaluate(progress) * _data.DistanceZ;
        if (float.IsNaN(targetDistance) || float.IsInfinity(targetDistance))
        {
            StopDisplacement();
            Debug.LogError("技能程序位移曲线产生非有限距离，停止本段。");
            return;
        }
        targetDistance = Mathf.Min(targetDistance, _data.DistanceZ);
        // 当前累计比例要求的米数增量，Clamp 与 Stop 不允许回拉。
        float deltaDistance = targetDistance - _appliedDistance;
        if (deltaDistance < 0f && !_data.AllowReverse)
        {
            if (_data.StopAtDistanceLimit)
            {
                _data = null;
            }
            deltaDistance = 0f;
        }
        _appliedDistance += deltaDistance;
        _translation += _direction * deltaDistance;
        // 本段是否已结束：进度到终值，或开启距离上限时已达到上限距离。
        bool segmentFinished = progress >= 1f ||
            (_data.StopAtDistanceLimit && _appliedDistance >= _data.DistanceZ);
        if (_data != null && segmentFinished)
        {
            _data = null;
        }
    }

    /// <summary>每次实体装配时清空旧运动，避免组件池复用残留前一个宿主的指令。</summary>
    public override void Initialize()
    {
        StopDisplacement();
    }

    /// <summary>实体销毁或组件归池时取消运动；宿主解绑由基类池流程执行。</summary>
    public override void Shutdown()
    {
        StopDisplacement();
    }

    /// <summary>直接卸载或对象池解绑时立即清运动和模型引用，不等待下一帧消费。</summary>
    public override void OnDetachFromHost()
    {
        StopDisplacement();
        base.OnDetachFromHost();
    }

    /// <summary>沿 RO 开始入口接管一条已经到期的指令，同 owner 多段先完成前段差量再切换。</summary>
    /// <param name="args">计时回调冻结的技能身份、原点、方向及冷登记基线。</param>
    /// <returns>身份仍占用当前动作槽且数据有效时返回 true。</returns>
    public bool onStart(MDisplacementEventArgs args)
    {
        // 任一前置校验不通过即拒绝接管；按原 || 短路顺序逐组早退，后项不再求值。
        if (!Enabled || Entity == null || args == null)
        {
            return false;
        }
        if (args.Source == null || args.Data == null)
        {
            return false;
        }
        if (float.IsNaN(args.angle) || float.IsInfinity(args.angle))
        {
            return false;
        }
        if (float.IsNaN(args.initialElapsedSeconds) || float.IsInfinity(args.initialElapsedSeconds))
        {
            return false;
        }
        if (!args.Source.OwnsDisplacement(Entity, args.skillUuid, args.fireGeneration))
        {
            return false;
        }
        if (!args.Data.IsValid(args.Source.DisplacementDurationSeconds))
        {
            return false;
        }
        if (!ReferenceEquals(_owner, args.Source) || _skillUuid != args.skillUuid ||
            _fireGeneration != args.fireGeneration)
        {
            StopDisplacement();
        }
        else
        {
            if (Entity.Model == null || !Entity.Model.IsLoaded ||
                !ReferenceEquals(_loadedGameObject, Entity.Model.GameObject.NativeObject))
            {
                // 上一段已经完成时也可能仍持有未提交差量，模型替换后不能带给新段或新 GO。
                _translation = Vector3.zero;
            }
            // 同一大 tick 到期多段，先累计旧段到当前绝对时刻，不能覆盖末段差量。
            Update(0f);
        }
        _owner = args.Source;
        _skillUuid = args.skillUuid;
        _fireGeneration = args.fireGeneration;
        _data = args.Data;
        _startTimeMilliseconds = args.startTimeMilliseconds;
        _lastClockMilliseconds = MTimerMgr.singleton.ElapsedTime;
        _direction = Quaternion.Euler(0f, args.angle, 0f) * Vector3.forward;
        _appliedDistance = 0f;
        _started = false;
        _loadedGameObject = null;
        if (Entity.Model != null && Entity.Model.IsLoaded)
        {
            _started = true;
            _loadedGameObject = Entity.Model.GameObject.NativeObject;
            if (args.initialElapsedSeconds >= 0f)
            {
                SetLateBaseline(args.initialElapsedSeconds);
            }
        }
        return true;
    }

    /// <summary>沿 RO 停止入口取消原施法，动作自然离槽与打断都丢弃未提交差量，保留碰撞安全。</summary>
    /// <param name="args">开始运动时保存的身份；旧 UUID 或旧代次停止请求无副作用。</param>
    public void onStop(MDisplacementStopEventArgs args)
    {
        // 停止身份是否与本组件登记的施法 owner、UUID、代次完全一致。
        bool stopIdentityMatch = ReferenceEquals(args.Source, _owner) && args.skillUuid == _skillUuid &&
            args.fireGeneration == _fireGeneration;
        if (args != null && stopIdentityMatch)
        {
            StopDisplacement();
        }
    }

    /// <summary>冷配置或模型迟到时仅建立当前累计基线，已经结束的窗口直接跳过。</summary>
    /// <param name="elapsedSeconds">段内已经经过的非负秒数。</param>
    private void SetLateBaseline(float elapsedSeconds)
    {
        // 原段持续秒数，零时长的冷历史事件也不能迟到瞬移。
        float durationSeconds = _data.End - _data.At;
        if (elapsedSeconds >= durationSeconds)
        {
            _data = null;
            return;
        }
        // 原曲线乘距离先验证有限，再做硬上限，不能用上限掩盖正无穷或非数采样。
        float baselineDistance = _data.CurveZ.Evaluate(elapsedSeconds / durationSeconds) * _data.DistanceZ;
        if (float.IsNaN(baselineDistance) || float.IsInfinity(baselineDistance))
        {
            StopDisplacement();
            Debug.LogError("技能程序位移冷加载基线产生非有限距离，停止本段。");
            return;
        }
        _appliedDistance = Mathf.Min(baselineDistance, _data.DistanceZ);
        if (!_data.AllowReverse && _appliedDistance < 0f)
        {
            // Clamp 和 Stop 从不实际回拉，冷基线也不能留下负距离债务导致后续超过前向上限。
            _appliedDistance = 0f;
        }
    }

    /// <summary>提交前再次校验当前动作归属并消费差量，重复调用返回零。</summary>
    /// <returns>尚有效施法的一次水平世界位移，单位米。</returns>
    public Vector3 ConsumeDelta()
    {
        // 组件停用、宿主缺失或原施法 owner 缺失时直接清零提交量。
        if (!Enabled || Entity == null || _owner == null)
        {
            StopDisplacement();
            return Vector3.zero;
        }
        // owner 非空后再校验当前施法归属是否仍有效。
        if (!_owner.OwnsDisplacement(Entity, _skillUuid, _fireGeneration))
        {
            StopDisplacement();
            return Vector3.zero;
        }
        if (Entity.Model == null || !Entity.Model.IsLoaded ||
            !ReferenceEquals(_loadedGameObject, Entity.Model.GameObject.NativeObject))
        {
            _translation = Vector3.zero;
            return Vector3.zero;
        }
        // 本次唯一提交量，先清缓存防止下次重复消费。
        Vector3 delta = _translation;
        _translation = Vector3.zero;
        return delta;
    }

    /// <summary>撞墙、动作离槽、失效或实体组件归还时清除全部运动引用，不硬拉理论终点。</summary>
    public void StopDisplacement()
    {
        _data = null;
        _owner = null;
        _skillUuid = 0;
        _fireGeneration = 0;
        _translation = Vector3.zero;
        _appliedDistance = 0f;
        _started = false;
        _loadedGameObject = null;
        _startTimeMilliseconds = 0;
        _lastClockMilliseconds = 0;
        _direction = Vector3.zero;
    }
}
