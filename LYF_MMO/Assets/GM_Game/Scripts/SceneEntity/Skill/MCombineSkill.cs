using System.Collections.Generic;

/// <summary>沿用 RO 的组合主技能，按 CombineSkillList 顺序执行子技能并保留上一段表现尾部。</summary>
public sealed class MCombineSkill : MSkill
{
    /// <summary>仍有动作或表现尾部的子技能实例，均使用主技能 UUID。</summary>
    private readonly List<MSkill> _skills = new List<MSkill>();
    /// <summary>下一项子技能在配置列表中的零起始下标。</summary>
    private int _combine_num;
    /// <summary>当前占用动画的子技能，上一段完成后由下一段替换。</summary>
    private MSkill _current;
    /// <summary>RO 组合技能运行类型。</summary>
    public override int SkillType { get { return SkillLogicType.SkillTypeCombined; } }
    /// <summary>全部子技能及其声音、时间轴事件都已结束时完成表现。</summary>
    public override bool IsPresentationFinished { get { return _skills.Count == 0; } }

    /// <summary>主技能不生成自己的事件，直接开始第一段。</summary>
    protected override void Start()
    {
        _combine_num = 0;
        // 主技能生命期采用主表合计动作时长，子技能各自保留完整表现尾部。
        _durationSeconds = 0f;
        _skillStateTimeSeconds = 0f;
        SkillStateEnd();
    }

    /// <summary>推进当前段及历史表现，动作结束立即衔接下一段。</summary>
    /// <param name="deltaSeconds">当前帧经过的秒数。</param>
    public override bool Update(float deltaSeconds)
    {
        // 子技能 Cease 会从原列表移除自己，因此移除分支不递增下标。
        for (int index = 0; index < _skills.Count;)
        {
            // 当前仍持有声音、事件或动作的子技能原引用。
            MSkill skill = _skills[index];
            if (!skill.Update(deltaSeconds)) skill.Cease(true);
            else index++;
        }
        if (!_animationReleased)
        {
            if (_current == null || _current.IsEnd()) SkillStateEnd();
        }
        return base.Update(deltaSeconds);
    }

    /// <summary>上一段交出动画后按 RO 列表启动下一段，不重复扣蓝、检查冷却或发请求。</summary>
    public void SkillStateEnd()
    {
        if (_current != null) _current.ReleaseAnimation();
        _current = null;
        // 主配置以稳定技能 ID 引用原技能，同一 ID 可连续出现多次。
        List<int> skillIds = SkillCastingRulesCatalog.Get(SkillId).CombineSkillList;
        if (_combine_num >= skillIds.Count) return;
        // 子技能按主技能等级装配，远端角色不依赖本地已学字典。
        MSkillCore core = MSkillFactory.CreateCore(skillIds[_combine_num++], _core.Level, _core.ProfessionId);
        if (core == null) return;
        core.SelectPresentation(0, UnityEngine.Time.realtimeSinceStartup);
        _durationSeconds += core.AnimationDurationSeconds;
        _skillStateTimeSeconds = _durationSeconds;
        // 每段都是独立普通技能，保留原动画、特效和程序位移处理器。
        MSkill skill = MSkillFactory.CreateSkill(core);
        if (skill.Fire(core, _firer, SkillUuid, TargetUid, CastPoint, IsPredicted, this))
        {
            _skills.Add(skill);
            _current = skill;
            skill.TriggerAnim();
        }
        else MSkillFactory.ReturnSkill(skill);
        core.Release();
    }

    /// <summary>子技能回收时解除原列表引用，不通知角色主动作结束。</summary>
    /// <param name="skill">正在结束的子技能原对象。</param>
    public void OnSkillEnd(MSkill skill)
    {
        _skills.Remove(skill);
        if (ReferenceEquals(_current, skill)) _current = null;
    }

    /// <summary>状态机统一动画入口转发给当前子技能。</summary>
    public override void TriggerAnim()
    {
        if (_current != null) _current.TriggerAnim();
    }

    /// <summary>主技能收到权威确认时同步确认已开始的子表现，后续段沿用主技能状态。</summary>
    /// <param name="skillUuid">服务器回传的同一次组合施法 UUID。</param>
    public override bool Confirm(ulong skillUuid)
    {
        if (!base.Confirm(skillUuid)) return false;
        // 当前存活子表现的下标，确认不会重新开始任何动画或事件。
        for (int index = 0; index < _skills.Count; index++) _skills[index].Confirm(skillUuid);
        return true;
    }

    /// <summary>离开主动作时停止继续衔接，保留已经开始的声音和事件尾部。</summary>
    public override void ReleaseAnimation()
    {
        _animationReleased = true;
        if (_current != null) _current.ReleaseAnimation();
    }

    /// <summary>组合列表全部执行且最后一段已交出动作时结束。</summary>
    public override bool IsEnd()
    {
        if (_combine_num < SkillCastingRulesCatalog.Get(SkillId).CombineSkillList.Count) return false;
        return _current == null || _current.IsEnd();
    }

    /// <summary>取得当前保留的子实例，负一表示组合主技能。</summary>
    /// <param name="skillNum">存活子实例下标，负一为主技能。</param>
    public override MSkill GetSkill(int skillNum)
    {
        if (skillNum == -1) return this;
        if (skillNum < 0 || skillNum >= _skills.Count) return null;
        return _skills[skillNum];
    }

    /// <summary>拒绝、打断或回收时终止所有子技能及待触发事件。</summary>
    protected override void OnReset()
    {
        while (_skills.Count > 0) _skills[0].Cease(true);
        _current = null;
        _combine_num = 0;
    }

    /// <summary>归还组合技能自己的对象池。</summary>
    public override void Recycle() { MCommonObjectPool<MCombineSkill>.Release(this); }
}
