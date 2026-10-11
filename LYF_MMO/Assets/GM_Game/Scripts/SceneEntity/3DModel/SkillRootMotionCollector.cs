using UnityEngine;

/// <summary>截取真实Animator水平根位移，供实体移动组件与重力合并后提交碰撞控制器。</summary>
[DisallowMultipleComponent]
public sealed class SkillRootMotionCollector : MonoBehaviour
{
    /// <summary>产生根位移的实际原生动画器。</summary>
    private Animator _source;
    /// <summary>动画器所属模型，用于防止模型池复用时串用旧施法。</summary>
    private MModel _model;
    /// <summary>当前占用动作槽的普通技能，旧脱手实例不参与根移动。</summary>
    private MArtSkill _skill;
    /// <summary>等待移动组件消费的水平世界位移，单位米。</summary>
    private Vector3 _translation;
    /// <summary>绑定前的根运动开关，在模型解绑时恢复。</summary>
    private bool _savedApplyRootMotion;
    /// <summary>当前是否已绑定真实动画器。</summary>
    private bool _initialized;

    /// <summary>绑定真实动画器并接管根位移；未声明技能窗口的动作保持原地。</summary>
    /// <param name="model">唯一管理该动画器的模型。</param>
    /// <param name="source">模型中实际播放动画的原生组件。</param>
    public void Init(MModel model, Animator source)
    {
        Unbind();
        _model = model;
        _source = source;
        if (_source != null)
        {
            _savedApplyRootMotion = _source.applyRootMotion;
            _source.applyRootMotion = true;
            _initialized = true;
        }
    }

    /// <summary>切换当前动作所有者时丢弃旧段位移，重复绑定同一实例不清掉本帧数据。</summary>
    /// <param name="skill">当前技能动作实例，可以为空表示退出技能。</param>
    public void Bind(MArtSkill skill)
    {
        if (!ReferenceEquals(_skill, skill))
        {
            _translation = Vector3.zero;
            _skill = skill;
        }
    }

    /// <summary>技能离开动作槽时解除其自身绑定，不能清理后续技能的位移。</summary>
    /// <param name="skill">请求解除的原动作实例。</param>
    public void Release(MArtSkill skill)
    {
        if (ReferenceEquals(_skill, skill))
        {
            Bind(null);
        }
    }

    /// <summary>消费一次已采集位移；施法失效时直接清零，禁止回池旧实例继续推动。</summary>
    /// <returns>当前动作尚有效时的水平世界位移，单位米。</returns>
    public Vector3 ConsumeDelta()
    {
        // 待提交碰撞的本帧累计水平位移，消费后不重复应用。
        Vector3 delta = _translation;
        _translation = Vector3.zero;
        if (_skill == null || !_skill.OwnsRootMotion(_model))
        {
            return Vector3.zero;
        }
        return delta;
    }

    /// <summary>每次原生动画评估只采集声明窗口内的水平delta，忽略Y和根旋转。</summary>
    private void OnAnimatorMove()
    {
        if (!_initialized || _source == null || _skill == null || !_skill.OwnsRootMotion(_model))
        {
            _translation = Vector3.zero;
            return;
        }
        // 当前Skill动画状态的真实时钟；过渡进入Skill时读取目标状态。
        AnimatorStateInfo state = _source.GetCurrentAnimatorStateInfo(0);
        if (_source.IsInTransition(0))
        {
            // 过渡目标的信息；离开Skill时不采集旧段的混合残余。
            AnimatorStateInfo nextState = _source.GetNextAnimatorStateInfo(0);
            if (!nextState.IsName(MAnimator.STATE_SKILL))
            {
                _translation = Vector3.zero;
                return;
            }
            state = nextState;
        }
        if (!state.IsName(MAnimator.STATE_SKILL))
        {
            _translation = Vector3.zero;
            return;
        }
        // 已就绪的真实Skill剪辑；元数据和RootT不能代替Humanoid评估出的世界delta。
        MAnimator.MAnimInfo animation = _model.Ator.GetAnimInfo(MAnimator.STATE_SKILL);
        if (animation == null || animation.Clip == null)
        {
            return;
        }
        // 原动画秒数用于匹配编辑器半开根运动窗口。
        float seconds = state.normalizedTime * animation.Clip.length;
        // 当前窗口的非负水平位移倍率；窗口外返回零。
        float scale = _skill.RootMotionScale(seconds);
        // 当前原生世界位移，Y由重力与跳跃组件独占，不能带回姿态下沉。
        Vector3 delta = _source.deltaPosition;
        delta.y = 0f;
        _translation += delta * scale;
    }

    /// <summary>模型解绑时恢复原根运动开关并解除所有池实例引用。</summary>
    public void Unbind()
    {
        if (_initialized && _source != null)
        {
            _source.applyRootMotion = _savedApplyRootMotion;
        }
        _initialized = false;
        _translation = Vector3.zero;
        _skill = null;
        _model = null;
        _source = null;
    }

    /// <summary>动画器临时失活时丢弃未消费delta；保持绑定以便正常重新激活。</summary>
    private void OnDisable()
    {
        _translation = Vector3.zero;
    }

    /// <summary>模型节点销毁后确保原生动画器开关及池引用均释放。</summary>
    private void OnDestroy()
    {
        Unbind();
    }
}
