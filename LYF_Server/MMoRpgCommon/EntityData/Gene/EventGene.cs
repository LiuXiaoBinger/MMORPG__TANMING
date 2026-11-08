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
    /// <summary>关联子项须等事件成功后才激活。</summary>
    public override bool WaitsForEvent { get { return true; } }

    /// <summary>匹配场景、事件、目标、冷却与概率后，沿原子树兑现关联项。</summary>
    /// <param name="args">本次真实事件及事件对方。</param>
    /// <param name="instance">提供来源与施加角色的根实例。</param>
    /// <param name="host">保存原事件时间与随机源的宿主。</param>
    /// <param name="target">上层选择的效果承接容器。</param>
    /// <returns>实际成功施加或刷新的关联项数量。</returns>
    internal override int Trigger(GeneTriggerArgs args, GeneInstance instance, GeneRuntime host, GeneRuntime target)
    {
        if (!AppliesToScenario(host.Scenario)) return 0;
        if (TriggerEvent != GeneEventType.None && TriggerEvent != args.Event) return 0;
        if (TargetOpponent) target = args.TargetRuntime;
        if (target == null) return 0;
        if (host.ElapsedMilliseconds < CooldownUntilMilliseconds) return 0;
        if (IsExhausted) return 0;
        if (TriggerChance <= 0) return 0;
        if (TriggerChance < 10000 && host.NextTriggerRoll() >= TriggerChance) return 0;
        // 只有真实产生效果才消耗次数或开始冷却。
        int activated = 0;
        // 相邻事件/次数继续使用原子树；普通节点从本次触发开始独立计时。
        for (int index = 0; index < Children.Count; index++)
        {
            activated += Children[index].ActivateFromEvent(args, instance, host, target);
        }
        if (activated > 0)
        {
            OnTriggered();
            CooldownUntilMilliseconds = host.ElapsedMilliseconds + CooldownMilliseconds;
        }
        return activated;
    }

    /// <summary>相邻事件子项继续使用当前节点的冷却和次数，不创建新事件树。</summary>
    /// <param name="args">沿关联链传递的同一个实际事件。</param>
    /// <param name="instance">保留来源和施加角色的根实例。</param>
    /// <param name="host">负责此事件树计时的原宿主。</param>
    /// <param name="target">继承自上层的承接容器。</param>
    /// <returns>当前事件子树实际成功施加数量。</returns>
    internal override int ActivateFromEvent(GeneTriggerArgs args, GeneInstance instance, GeneRuntime host, GeneRuntime target)
    {
        return Trigger(args, instance, host, target);
    }

    /// <summary>次数节点在成功产生效果后覆盖此入口扣次；普通事件不限次数。</summary>
    protected virtual void OnTriggered() { }
}
