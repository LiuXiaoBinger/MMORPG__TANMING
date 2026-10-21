using UnityEngine;

/// <summary>沿RO名称描述点地或目标导航，技能到达动作使用强类型载荷。</summary>
public sealed class MNavigationEventArgs : IEventArg
{
    /// <summary>世界空间目的点，单位米；NeedTarget为true时改用目标当前位置。</summary>
    public Vector3 Pos;
    /// <summary>是否跟随Target的当前位置重新计算路线。</summary>
    public bool NeedTarget;
    /// <summary>本次要追近的原实体引用；普通点地与现有无目标技能为空。</summary>
    public MEntity Target;
    /// <summary>可停止的水平距离上限，单位米，零表示走到目的点。</summary>
    public float UpperRange;
    /// <summary>可停止的水平距离下限，单位米，零表示无最小距离。</summary>
    public float LowerRange;
    /// <summary>非零时替代上下限的水平到达距离，单位米。</summary>
    public float CustomRange;
    /// <summary>到达后是否执行NavAction；普通点地为false。</summary>
    public bool UseAfterAction;
    /// <summary>由本事件拥有的到达技能指令；跨帧导航另行Clone后持有。</summary>
    public MSkillEventArgs NavAction { get; private set; }
    /// <summary>事件池回收标记，true表示本次导航指令已失效。</summary>
    public bool Deprecated { get; set; }

    /// <summary>事件池创建空导航指令，具体数据由Create设置。</summary>
    public MNavigationEventArgs() { }

    /// <summary>沿原技能入口借出指令，现有无目标技能在自己的释放点立即到达。</summary>
    /// <param name="navAction">本次技能输入；事件接管其回收责任。</param>
    /// <returns>同步派发结束后由实体回收的导航事件。</returns>
    public static MNavigationEventArgs Create(MSkillEventArgs navAction)
    {
        // 原无目标技能以角色当前位置为释放点，不把辅助选择目标变成追近目标。
        MNavigationEventArgs args = Create(navAction.CastPoint);
        args.UseAfterAction = true;
        args.NavAction = navAction;
        return args;
    }

    /// <summary>借出普通点地导航指令，距离范围和目标可按RO字段显式填写。</summary>
    /// <param name="pos">世界空间点击或导航目的点，单位米。</param>
    /// <returns>由实体派发器在同步处理结束后回收的导航事件。</returns>
    public static MNavigationEventArgs Create(Vector3 pos)
    {
        // 当前请求独占的池化事件，所有可选字段已在上次Recycle中清空。
        MNavigationEventArgs args = MEventPool<MNavigationEventArgs>.GetEvent();
        args.Pos = pos;
        return args;
    }

    /// <summary>复制导航字段及可空技能载荷，使延后到达不会引用已回收事件。</summary>
    /// <returns>由持有者独立回收的完整指令。</returns>
    public IEventArg Clone()
    {
        // 本次复制件保持原实体引用，技能参数另租独立事件。
        MNavigationEventArgs clone = Create(Pos);
        clone.NeedTarget = NeedTarget;
        clone.Target = Target;
        clone.UpperRange = UpperRange;
        clone.LowerRange = LowerRange;
        clone.CustomRange = CustomRange;
        clone.UseAfterAction = UseAfterAction;
        if (NavAction != null)
        {
            clone.NavAction = (MSkillEventArgs)NavAction.Clone();
        }
        return clone;
    }

    /// <summary>回收可空技能动作并清空导航字段，供下次点地或技能复用。</summary>
    public void Recycle()
    {
        if (NavAction != null)
        {
            NavAction.Recycle();
            NavAction = null;
        }
        Pos = Vector3.zero;
        NeedTarget = false;
        Target = null;
        UpperRange = 0f;
        LowerRange = 0f;
        CustomRange = 0f;
        UseAfterAction = false;
        MEventPool<MNavigationEventArgs>.Recycle(this);
    }
}
