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
}
