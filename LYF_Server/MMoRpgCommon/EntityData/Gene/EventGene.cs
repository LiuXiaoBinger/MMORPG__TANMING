/// <summary>事件运行节点，保存自己的冷却；匹配真实宿主事件后施加关联基因。</summary>
public class EventGene : GeneBase
{
    /// <summary>需要匹配的宿主事件；次数子类允许 None 匹配任意非空事件。</summary>
    public GeneEventType TriggerEvent { get; set; }
    /// <summary>触发概率，单位万分比；0 不触发，10000 必定触发。</summary>
    public int TriggerChance { get; set; }
    /// <summary>成功触发后的冷却间隔，单位毫秒。</summary>
    public int CooldownMilliseconds { get; set; }
    /// <summary>原宿主逻辑时间到达此毫秒值后才可再次触发。</summary>
    public long CooldownUntilMilliseconds { get; private set; }
    /// <summary>事件节点的效果分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.Event; } }
    /// <summary>事件节点等待实际事件，不在初次挂载时触发。</summary>
    public override bool CallsOnAttach { get { return false; } }
    /// <summary>关联子项须等事件成功后才激活。</summary>
    public override bool WaitsForEvent { get { return true; } }

    /// <summary>独立施加的事件副本从未冷却状态开始；原事件节点的冷却保持不变。</summary>
    /// <returns>拥有独立子树与冷却的同类型节点。</returns>
    public override GeneBase Clone()
    {
        // 基类复制实际运行类型，次数子类仍保留自己的类型与配置总次数。
        EventGene copy = (EventGene)base.Clone();
        copy.CooldownUntilMilliseconds = 0;
        return copy;
    }

    /// <summary>由本事件节点独立判断事件、概率和冷却，并执行关联节点。</summary>
    /// <param name="role">收到战斗事件的真实角色。</param>
    /// <param name="args">当前事件及实际对方；为空时没有事件可触发。</param>
    /// <returns>实际激活的关联节点数量。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        if (args.Event == GeneEventType.None) return 0;
        // 冷却与概率属于原角色的基因模块，效果目标可以是事件对方。
        GeneRuntime host = role.GeneRuntime;
        if (!AppliesToScenario(host.Scenario)) return 0;
        if (TriggerEvent != GeneEventType.None && TriggerEvent != args.Event) return 0;
        // 嵌套事件沿祖先继承目标选择，普通子项随后直接施加到选中的容器。
        GeneRuntime target = host;
        GeneBase ancestor = this;
        while (ancestor != null)
        {
            if (ancestor.TargetOpponent) target = args.TargetRuntime;
            ancestor = ancestor.Parent;
        }
        if (target == null) return 0;
        if (host.ElapsedMilliseconds < CooldownUntilMilliseconds) return 0;
        if (IsExhausted) return 0;
        if (TriggerChance <= 0) return 0;
        if (TriggerChance < 10000 && host.NextTriggerRoll() >= TriggerChance) return 0;
        // 只有确实激活子节点，才开启冷却或由次数子类扣除次数。
        int activated = 0;
        for (int index = 0; index < Children.Count; index++)
            activated += Children[index].ActivateFromEvent(role, args, target);
        if (activated > 0) CooldownUntilMilliseconds = host.ElapsedMilliseconds + CooldownMilliseconds;
        return activated;
    }

    /// <summary>相邻事件继续调用原节点的 OnCall，保留原冷却和次数。</summary>
    /// <param name="role">收到事件的原角色。</param>
    /// <param name="args">沿关联链传递的同一个事件。</param>
    /// <param name="target">上层选中的目标；OnCall 通过父链继承该选择。</param>
    /// <returns>当前事件子树激活的节点数量。</returns>
    internal override int ActivateFromEvent(IGeneOwner role, GeneTriggerArgs args, GeneRuntime target)
    {
        return OnCall(role, args);
    }
}
