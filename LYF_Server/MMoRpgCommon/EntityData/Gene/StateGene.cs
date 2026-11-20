/// <summary>在根寿命内持续提供动作限制状态的运行节点。</summary>
public class StateGene : GeneBase
{
    /// <summary>此节点提供的眩晕、定身、沉默或霸体位。</summary>
    public GeneState States { get; set; }
    /// <summary>状态查询由此具体节点实现。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.State; } }

    /// <summary>本节点执行时在角色属性组件登记状态来源。</summary>
    /// <param name="role">当前承受状态的角色。</param>
    /// <param name="args">状态施加不使用额外事件参数。</param>
    /// <returns>登记状态后返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        role.AttrComp.ChangeState(States, 1);
        return 1;
    }

    /// <summary>只撤销当前节点贡献的一份状态来源。</summary>
    /// <param name="role">此前承受状态的角色。</param>
    public override void OnRemove(IGeneOwner role) { role.AttrComp.ChangeState(States, -1); }

    /// <summary>解除当前节点提供的指定控制位，保留同节点的其余状态。</summary>
    /// <param name="role">已经登记此状态的宿主。</param>
    /// <param name="states">允许解除的控制状态集合。</param>
    /// <returns>实际撤销了至少一个状态时返回 true。</returns>
    internal bool Dispel(IGeneOwner role, GeneState states)
    {
        // 仅撤销受到此节点计数影响的状态来源。
        GeneState removed = States & states;
        if (removed == GeneState.None) return false;
        role.AttrComp.ChangeState(removed, -1);
        States &= ~removed;
        return true;
    }
}
