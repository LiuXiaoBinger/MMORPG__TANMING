using UnityEngine;

/// <summary>RO 普通技能表现载体，负责 Skill04 事件特效与槽位释放。</summary>
public class MArtSkill : MSkill
{
    /// <summary>无限剑决在旧技能主表中的 ID，仅该技能读取既有 Skill04 特效事件。</summary>
    private const int SkySwordSkillId = 10070;

    /// <summary>本次 Skill04 时间轴独立持有的剪辑租约，退出动作槽后仍等待该路径加载完成。</summary>
    private MAnimationMgr.ClipInfo _eventClipInfo;

    /// <summary>已读取事件的真实剪辑；剪辑尚未就绪时为空。</summary>
    private AnimationClip _eventClip;

    /// <summary>当前剪辑中的事件快照，按时间顺序仅消费一次。</summary>
    private AnimationEvent[] _skillEvents;

    /// <summary>下一条尚未处理的剪辑事件下标，从零开始。</summary>
    private int _nextEventIndex;

    /// <summary>普通技能的 RO 运行类型，零表示 Art。</summary>
    public override int SkillType { get { return SkillLogicType.SkillTypeArt; } }

    /// <summary>按 RO 普通技能启动钩子登记表现时间轴，独立持有 Skill04 的事件剪辑。</summary>
    protected override void Start()
    {
        base.Start();
        if (IsActive && SkillId == SkySwordSkillId && _eventClipInfo == null)
        {
            // 时间轴不能依赖会被下一次施法复用的动画槽；同一路径仍共用资源加载。
            _eventClipInfo = MAnimationMgr.singleton.GetClipInfo(_animationLocation);
            if (_eventClipInfo != null)
            {
                _eventClipInfo.RetainClip(null);
            }
        }
    }

    /// <summary>推进普通技能时钟，并按无限剑决原剪辑的两条特效事件触发粒子。</summary>
    /// <param name="deltaSeconds">本帧经过的非负秒数。</param>
    public override void Update(float deltaSeconds)
    {
        base.Update(deltaSeconds);
        if (!IsActive || SkillId != SkySwordSkillId || _eventClipInfo == null)
        {
            return;
        }
        // 剪辑异步加载期间不保存空事件，资源就绪后在后续帧补读时间轴。
        AnimationClip clip = _eventClipInfo.Clip;
        if (clip == null)
        {
            return;
        }
        if (_eventClip != clip)
        {
            _eventClip = clip;
            _skillEvents = clip.events;
            _nextEventIndex = 0;
        }
        while (_skillEvents != null && _nextEventIndex < _skillEvents.Length)
        {
            // 当前剪辑事件按原动画秒数排序，超过本次时钟的事件留待下一帧。
            AnimationEvent skillEvent = _skillEvents[_nextEventIndex];
            if (skillEvent.time > ElapsedSeconds)
            {
                return;
            }
            if (!TryPlaySkill04Effect(skillEvent))
            {
                // 剪辑依赖仍未就绪时停在该事件，防止跳过后永远不播放。
                return;
            }
            _nextEventIndex++;
        }
    }

    /// <summary>解析无限剑决原 PlayEffect 事件并实例化对应的已有粒子预制体。</summary>
    /// <param name="skillEvent">动画剪辑携带的配置引用、配置序号及阶段标记。</param>
    /// <returns>事件已处理或不属于目标两段时返回 true；依赖未就绪时返回 false。</returns>
    private bool TryPlaySkill04Effect(AnimationEvent skillEvent)
    {
        if (skillEvent.functionName != "PlayEffect")
        {
            return true;
        }
        // 只消费 Skill04 已配置的蓄力与落剑两条事件，不解释其他动画事件。
        bool isCharge = skillEvent.intParameter == 0 &&
            string.IsNullOrEmpty(skillEvent.stringParameter);
        bool isDrop = skillEvent.intParameter == 1 &&
            skillEvent.stringParameter == "skill04_2";
        if (!isCharge && !isDrop)
        {
            return true;
        }
        // 动画事件对 ScriptableObject 的直接引用由剪辑资源依赖提供。
        RoleAtkConfig config = skillEvent.objectReferenceParameter as RoleAtkConfig;
        if (config == null || config.atkConfigEntities == null ||
            skillEvent.intParameter >= config.atkConfigEntities.Count)
        {
            return false;
        }
        // 只读取共享配置，绝不回写位置或朝向。
        atkConfigEntity attackConfig =
            config.atkConfigEntities[skillEvent.intParameter];
        if (attackConfig == null || attackConfig._effectInfo == null ||
            attackConfig._effectInfo._fx == null || _firer == null ||
            _firer.Model == null || _firer.Model.GameObject.Transform == null)
        {
            return false;
        }
        // 本次特效根节点；蓄力特效仍按原配置挂在角色的 EffectPos。
        Transform roleTransform = _firer.Model.GameObject.Transform;
        Transform effectParent = null;
        if (isCharge &&
            !string.IsNullOrEmpty(attackConfig._effectInfo._parentName))
        {
            effectParent = roleTransform.Find(attackConfig._effectInfo._parentName);
            if (effectParent == null)
            {
                return false;
            }
        }
        // 本次粒子独立于技能实例，创建时安排按自身播放寿命销毁。
        ParticleSystem effect = Object.Instantiate(attackConfig._effectInfo._fx);
        if (effect == null)
        {
            return false;
        }
        if (isCharge)
        {
            effect.transform.SetParent(effectParent, false);
            effect.transform.localPosition = attackConfig._effectInfo._position;
            effect.transform.localEulerAngles = attackConfig._effectInfo._eulerAngles;
        }
        else
        {
            // 原技能缺目标时落在施法者前方十单位；有目标时落到目标模型头顶。
            Vector3 dropPosition = roleTransform.position + roleTransform.forward * 10f;
            if (TargetUid != 0 && Global.Instance != null &&
                Global.Instance.SceneWorld != null)
            {
                // 场景 UID 命中的实体必须仍为有效角色表现实体。
                ROObject targetObject;
                if (Global.Instance.SceneWorld.TryGetEntity(TargetUid,
                    out targetObject))
                {
                    // 模型高度使用世界单位，目标中心仍以实体当前位置为准。
                    MEntity target = targetObject as MEntity;
                    if (target != null && target.Model != null)
                    {
                        dropPosition = target.Position + Vector3.up * target.Height;
                    }
                }
            }
            effect.transform.position = dropPosition + attackConfig._effectInfo._position;
            effect.transform.eulerAngles = attackConfig._effectInfo._eulerAngles;
        }
        effect.transform.localScale = Vector3.one;
        effect.Play(true);
        // 粒子播放可能晚于服务器施法结束；交给 Unity 延迟销毁而非随池实例提前截断。
        Object.Destroy(effect.gameObject, GetEffectLifetimeSeconds(effect));
        return true;
    }

    /// <summary>统计根粒子及子粒子的启动延迟、持续和尾粒寿命，供 Unity 安排销毁。</summary>
    /// <param name="effect">本次实例化的粒子根节点。</param>
    /// <returns>自创建起至少保留的秒数，覆盖所有已配置粒子的播放尾段。</returns>
    private static float GetEffectLifetimeSeconds(ParticleSystem effect)
    {
        // 子粒子同属当前特效实例，取最长一条配置时间线。
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        float lifetimeSeconds = 0f;
        for (int index = 0; index < particles.Length; index++)
        {
            // 粒子主模块的时间量均以秒表示；曲线取最大常量作为保守上界。
            ParticleSystem.MainModule main = particles[index].main;
            float particleLifetime = main.startDelay.constantMax + main.duration +
                main.startLifetime.constantMax;
            if (particleLifetime > lifetimeSeconds)
            {
                lifetimeSeconds = particleLifetime;
            }
        }
        return Mathf.Max(lifetimeSeconds, effect.main.duration);
    }

    /// <summary>解除本次施法的剪辑事件引用，粒子已由 Unity 按配置寿命安排销毁。</summary>
    protected override void OnReset()
    {
        if (_eventClipInfo != null)
        {
            // 未注册异步回调；解除引用后旧资源即使晚到也不会触发已回收施法的事件。
            _eventClipInfo.ReleaseClip(null);
            _eventClipInfo = null;
        }
        _eventClip = null;
        _skillEvents = null;
        _nextEventIndex = 0;
    }

    /// <summary>退出动作槽时归还当前动画租约，保留施法 UUID 等待结束通知。</summary>
    public override void ReleaseAnimation()
    {
        _animationReleased = true;
        if (_boundAnimator != null)
        {
            _boundAnimator.ReleaseSkillAnimAfterExit(_animationLocation);
            _boundAnimator = null;
        }
    }

    /// <summary>普通技能没有组合子节点，按 RO 的普通技能行为返回自身。</summary>
    /// <param name="skillNum">调用方请求的组合内子技能序号，普通技能不使用。</param>
    /// <returns>当前普通技能实例。</returns>
    public override MSkill GetSkill(int skillNum)
    {
        return this;
    }

    /// <summary>清理结束后将普通技能实例归还其具体类型对象池。</summary>
    public override void Recycle()
    {
        MCommonObjectPool<MArtSkill>.Release(this);
    }
}
