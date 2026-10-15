using UnityEngine;

/// <summary>
/// 将 RO 逻辑动作名映射到现有角色/怪物 Controller 的 Movement、Action 参数。
/// 现有控制器没有 Idle、Move 等状态节点时，使用该兼容协议驱动原状态机。
/// </summary>
public static class MAnimatorStateCompatibility
{
    /// <summary>旧角色 Controller 中表示待机或移动速度档位的浮点参数名称。</summary>
    public const string MovementParameter = "Movement";

    /// <summary>旧角色 Controller 中表示一次性动作编号的整数参数名称。</summary>
    public const string ActionParameter = "Action";

    /// <summary>判断 Animator 是否同时声明兼容协议要求的 Movement 浮点参数和 Action 整数参数。</summary>
    /// <param name="animator">待检查的 Unity Animator 组件；允许为空，为空时返回 false。</param>
    /// <returns>两个参数均存在且类型匹配时返回 true，否则返回 false。</returns>
    public static bool Supports(Animator animator)
    {
        if (animator == null || animator.parameters == null)
        {
            return false;
        }

        // 记录遍历期间是否找到类型正确的 Movement 浮点参数。
        bool movement = false;

        // 记录遍历期间是否找到类型正确的 Action 整数参数。
        bool action = false;

        // 使用当前 Animator 参数数组快照，保证本次索引遍历的数据源一致。
        AnimatorControllerParameter[] parameters = animator.parameters;

        // 当前待检查参数在参数数组中的零基索引。
        for (int index = 0; index < parameters.Length; index++)
        {
            // 当前索引对应的 Animator 参数定义，用于核对名称和参数类型。
            AnimatorControllerParameter parameter = parameters[index];
            if (parameter.name == MovementParameter &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                movement = true;
            }
            if (parameter.name == ActionParameter &&
                parameter.type == AnimatorControllerParameterType.Int)
            {
                action = true;
            }
        }
        return movement && action;
    }

    /// <summary>返回逻辑动作状态在旧 Controller 中对应的 Action 整数值。</summary>
    /// <param name="stateName">RO 动画状态名称，取值来自 MAnimationStateNames 或 MAnimator 移动状态常量。</param>
    /// <returns>旧 Controller 的 Action 参数编号；未单独映射的状态使用兼容默认值 1。</returns>
    public static int GetActionValue(string stateName)
    {
        if (stateName == MAnimationStateNames.Skill)
        {
            return 34;
        }
        if (stateName == MAnimationStateNames.Special)
        {
            return 31;
        }
        if (stateName == MAnimationStateNames.BeHit)
        {
            return 41;
        }
        if (stateName == MAnimationStateNames.Born)
        {
            return 1;
        }
        if (stateName == MAnimationStateNames.Stun)
        {
            return 1;
        }
        if (stateName == MAnimationStateNames.Singing)
        {
            return 1;
        }
        if (stateName == MAnimationStateNames.Dead)
        {
            return 1;
        }
        return 1;
    }

    /// <summary>把 RO 动画状态转换为旧 Controller 的 Movement、Action 参数并执行写入。</summary>
    /// <param name="animator">接收兼容参数的 Unity Animator 组件；为空或协议不匹配时不写入。</param>
    /// <param name="stateName">准备驱动的 RO 动画状态名称。</param>
    /// <returns>Animator 支持兼容协议且参数已完成写入时返回 true，否则返回 false。</returns>
    public static bool Apply(Animator animator, string stateName)
    {
        if (!Supports(animator))
        {
            return false;
        }
        // 兼容 Controller 的 Movement 参数约定：2 表示行走，3 表示跑步。
        bool isRunState = stateName == MAnimator.STATE_ZD_MOVE_RUN ||
            stateName == MAnimator.STATE_NONZD_MOVE_RUN;
        if (isRunState)
        {
            animator.SetFloat(MovementParameter, 3f);
            animator.SetInteger(ActionParameter, 1);
            return true;
        }
        // 旧协议 Move/FMove 或自动导航行走状态。
        bool isOldOrZdWalkState = stateName == MAnimationStateNames.Move ||
            stateName == MAnimationStateNames.FMove ||
            stateName == MAnimator.STATE_ZD_MOVE_WALK;
        // 非自动导航行走状态。
        bool isNonZdWalkState = stateName == MAnimator.STATE_NONZD_MOVE_WALK;
        if (isOldOrZdWalkState || isNonZdWalkState)
        {
            animator.SetFloat(MovementParameter, 2f);
            animator.SetInteger(ActionParameter, 1);
            return true;
        }
        if (stateName == MAnimationStateNames.Idle || stateName == MAnimationStateNames.FIdle)
        {
            animator.SetFloat(MovementParameter, 0f);
            animator.SetInteger(ActionParameter, 1);
            return true;
        }
        animator.SetInteger(ActionParameter, GetActionValue(stateName));
        return true;
    }
}
