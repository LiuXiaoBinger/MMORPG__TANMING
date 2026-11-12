using System;
using System.Collections.Generic;

/// <summary>真实角色持有的节点集合；只管理施加、撤销、计时和事件，具体效果由节点 OnCall 执行。</summary>
public class GeneRuntime
{
    /// <summary>按配置编号创建具体节点的工厂；直接添加节点时不需要它。</summary>
    private readonly GeneFactory factory;
    /// <summary>当前角色直接持有的根节点，普通组合子项保存在各自父节点中。</summary>
    protected readonly List<GeneBase> genes = new List<GeneBase>();
    /// <summary>当前角色事件概率使用的独立随机源。</summary>
    private readonly Random random;
    /// <summary>直接接受节点行为的真实角色或怪物，不是结算包装。</summary>
    public IGeneOwner Owner { get; private set; }
    /// <summary>当前角色已经经过的逻辑毫秒，用于事件冷却。</summary>
    internal long ElapsedMilliseconds { get; private set; }
    /// <summary>当前集合的只读视图，元素就是实际 GeneBase 派生对象。</summary>
    public IReadOnlyList<GeneBase> ActiveGenes { get; private set; }
    /// <summary>根节点添加、刷新、层数/次数变化或移除时通知宿主；普通时间推进不通知。</summary>
    public event Action<GeneBase, bool> Changed;
    /// <summary>宿主环境：0 公共、1 副本、2 PvP。</summary>
    public int Scenario { get; private set; }

    /// <summary>绑定真实角色；以后每次 OnCall 都传入同一角色。</summary>
    /// <param name="owner">拥有属性组件的实际宿主。</param>
    /// <param name="geneFactory">按配置编号添加时使用的工厂。</param>
    /// <param name="scenario">宿主当前环境。</param>
    /// <param name="triggerRandom">事件随机源，测试可传入确定序列。</param>
    public GeneRuntime(IGeneOwner owner, GeneFactory geneFactory = null, int scenario = 0, Random triggerRandom = null)
    {
        Owner = owner;
        factory = geneFactory;
        Scenario = scenario;
        ActiveGenes = genes.AsReadOnly();
        random = triggerRandom;
        if (random == null) random = new Random();
    }

    /// <summary>编号入口只负责创建节点，再按节点自身的行为施加。</summary>
    /// <param name="geneId">配置中的根编号。</param>
    /// <param name="sourceId">施加者或装备 UID。</param>
    /// <param name="value">可选的根数值覆盖。</param>
    /// <param name="sourceSlot">同来源的独立词条槽。</param>
    /// <param name="firerId">实际施加角色编号。</param>
    /// <returns>本次挂载或刷新的原节点，场景不适用返回 null。</returns>
    public virtual GeneBase AddGene(int geneId, ulong sourceId = 0, float? value = null, int sourceSlot = 0, ulong firerId = 0)
    {
        // 相同来源与槽位刷新原节点，保留周期和冷却进度。
        GeneBase existing = FindGene(geneId, sourceId, sourceSlot);
        if (existing != null) return RefreshGene(existing, value);
        return AddGene(factory.CreateGene(geneId, value), sourceId, sourceSlot, firerId);
    }

    /// <summary>直接保存并执行传入节点，不创建额外实例包装。</summary>
    /// <param name="gene">本次独立创建的根节点。</param>
    /// <param name="sourceId">来源实体或装备 UID。</param>
    /// <param name="sourceSlot">同来源的独立效果槽。</param>
    /// <param name="firerId">实际施加角色编号。</param>
    /// <returns>本次挂载或刷新的节点，场景不适用返回 null。</returns>
    public virtual GeneBase AddGene(GeneBase gene, ulong sourceId = 0, int sourceSlot = 0, ulong firerId = 0)
    {
        // 当前角色中的同来源原对象仍是唯一生效节点。
        GeneBase existing = FindGene(gene.GeneID, sourceId, sourceSlot);
        if (existing != null) return RefreshGene(existing, gene.Value);
        if (!gene.AppliesToScenario(Scenario)) return null;
        gene.Initialize(sourceId, sourceSlot, firerId);
        // 纯即时组合执行后即可释放，存在持续子项时才保存根节点。
        bool persistent = false;
        for (int index = 0; index < gene.ActiveNodes.Count; index++)
            if (!gene.ActiveNodes[index].IsInstant) persistent = true;
        if (persistent) genes.Add(gene);
        CallAttachedNodes(gene, true);
        if (genes.Contains(gene)) Changed?.Invoke(gene, false);
        return gene;
    }

    /// <summary>先撤销旧持续修正，再修改层数与数值，最后执行新修正。</summary>
    /// <param name="gene">集合中的原根节点。</param>
    /// <param name="value">本次指定的新根数值；为空时保留原值。</param>
    /// <returns>刷新的原节点，当前场景不适用返回 null。</returns>
    private GeneBase RefreshGene(GeneBase gene, float? value)
    {
        if (!gene.AppliesToScenario(Scenario)) return null;
        RemoveNodeEffects(gene);
        if (gene.AllowsStacking)
            if (gene.MaxStack == 0 || gene.StackCount < gene.MaxStack) gene.StackCount++;
        if (value.HasValue && gene.HasOwnEffect) gene.Value = value.Value;
        gene.ResetTriggers();
        gene.RefreshDuration();
        CallAttachedNodes(gene, true);
        if (genes.Contains(gene)) Changed?.Invoke(gene, false);
        return gene;
    }

    /// <summary>逐项调用节点自身的施加行为，事件与周期等待自己的调用时机。</summary>
    /// <param name="gene">当前根节点。</param>
    /// <param name="includeInstant">首次施加和刷新执行即时资源，切换场景不再次兑现。</param>
    protected virtual void CallAttachedNodes(GeneBase gene, bool includeInstant)
    {
        // 列表保留原节点引用，子类可以一次修改多个属性。
        for (int index = 0; index < gene.ActiveNodes.Count; index++)
        {
            // 当前实际执行行为的子节点。
            GeneBase node = gene.ActiveNodes[index];
            if (!node.CallsOnAttach) continue;
            if (!includeInstant && node.IsInstant) continue;
            if (node.AppliesToScenario(Scenario)) node.OnCall(Owner);
        }
    }

    /// <summary>让各子节点撤销自己在当前场景下施加的持续修正。</summary>
    /// <param name="gene">即将移除、刷新或切换场景的根节点。</param>
    protected virtual void RemoveNodeEffects(GeneBase gene)
    {
        // 普通资源节点的 OnRemove 为空，因此已造成的伤害和恢复不回滚。
        for (int index = 0; index < gene.ActiveNodes.Count; index++)
        {
            // 仅撤销此前符合当前场景的已激活效果。
            GeneBase node = gene.ActiveNodes[index];
            if (node.AppliesToScenario(Scenario)) node.OnRemove(Owner);
        }
    }

    /// <summary>按编号、来源和槽位定位实际节点。</summary>
    /// <param name="geneId">根编号。</param>
    /// <param name="sourceId">来源实体或装备 UID。</param>
    /// <param name="sourceSlot">同来源的效果槽。</param>
    /// <returns>实际生效节点；不存在时返回 null。</returns>
    public GeneBase FindGene(int geneId, ulong sourceId = 0, int sourceSlot = 0)
    {
        for (int index = 0; index < genes.Count; index++)
            if (genes[index].GeneID == geneId && genes[index].SourceId == sourceId && genes[index].SourceSlot == sourceSlot) return genes[index];
        return null;
    }

    /// <summary>按编号与来源撤销普通槽的节点。</summary>
    /// <param name="geneId">根编号。</param>
    /// <param name="sourceId">来源编号。</param>
    /// <returns>实际找到并撤销时返回 true。</returns>
    public virtual bool RemoveGene(int geneId, ulong sourceId = 0) { return RemoveGene(FindGene(geneId, sourceId)); }

    /// <summary>按原对象撤销效果并移除节点。</summary>
    /// <param name="gene">此前 AddGene 返回的原节点。</param>
    /// <returns>当前集合确实持有它时返回 true。</returns>
    public virtual bool RemoveGene(GeneBase gene)
    {
        if (!genes.Remove(gene)) return false;
        RemoveNodeEffects(gene);
        Changed?.Invoke(gene, true);
        return true;
    }

    /// <summary>卸装时撤销此装备来源的全部槽位，保留其他来源。</summary>
    /// <param name="sourceId">被卸下装备的 UID。</param>
    public virtual void RemoveGenesBySource(ulong sourceId)
    {
        for (int index = genes.Count - 1; index >= 0; index--)
            if (genes[index].SourceId == sourceId) RemoveGene(genes[index]);
    }

    /// <summary>撤销旧场景修正，再为保留的组合执行新场景下的持续效果。</summary>
    /// <param name="scenario">新的公共、副本或 PvP 场景类别。</param>
    public virtual void SetScenario(int scenario)
    {
        // 场景变化可能只改变保留组合的有效子树，也需要通知客户端更新状态。
        bool scenarioChanged = Scenario != scenario;
        for (int index = 0; index < genes.Count; index++) RemoveNodeEffects(genes[index]);
        Scenario = scenario;
        for (int index = genes.Count - 1; index >= 0; index--)
        {
            if (!genes[index].AppliesToScenario(scenario))
            {
                // 撤销已在切场景前完成，此处只通知真实根移除。
                GeneBase removed = genes[index];
                genes.RemoveAt(index);
                Changed?.Invoke(removed, true);
            }
            else
            {
                CallAttachedNodes(genes[index], false);
                if (scenarioChanged) Changed?.Invoke(genes[index], false);
            }
        }
    }

    /// <summary>现有战斗调用读取角色属性组件已经生效的修正，不扫描基因数值。</summary>
    /// <param name="attribute">要读取的属性。</param>
    /// <param name="baseValue">不含基因的基础值。</param>
    /// <returns>属性组件计算的最终值。</returns>
    public virtual double CalculateAttribute(GeneAttributeType attribute, double baseValue) { return Owner.AttrComp.CalculateAttribute(attribute, baseValue); }
    /// <summary>召唤继承时只读排除指定来源槽的已生效属性贡献，不卸载节点或改变寿命。</summary>
    /// <param name="attribute">需要计算的现有属性类型。</param>
    /// <param name="baseValue">不含当前宿主Gene的基础值。</param>
    /// <param name="sourceId">需要排除的施加来源。</param>
    /// <param name="sourceSlot">该来源的精确槽；副本难度使用负槽，原Buff使用非负槽。</param>
    public double CalculateAttributeExcludingSource(GeneAttributeType attribute, double baseValue, ulong sourceId, int sourceSlot)
    {
        // 先读取真实属性组件，再仅扣除该来源槽当前仍生效的线性修正。
        double result = CalculateAttribute(attribute, baseValue);
        for (int index = 0; index < genes.Count; index++)
        {
            // 根节点存在才代表未到期，事件待触发子树不会出现在ActiveNodes中。
            GeneBase root = genes[index];
            if (root.SourceId != sourceId || root.SourceSlot != sourceSlot) continue;
            for (int nodeIndex = 0; nodeIndex < root.ActiveNodes.Count; nodeIndex++)
            {
                // 属性节点按原OnCall的Value、层数、ValueType贡献，不重新执行效果。
                AttributeGeneBase node = root.ActiveNodes[nodeIndex] as AttributeGeneBase;
                if (node == null || node.Attribute != attribute) continue;
                if (!node.CallsOnAttach || !node.AppliesToScenario(Scenario)) continue;
                // 百分比以调用者原基础值计算，固定值只扣实际点数。
                double contribution = node.Value * node.StackCount;
                if (node.ValueType == 1) contribution *= baseValue / 100;
                result -= contribution;
            }
        }
        return result;
    }
    /// <summary>读取属性组件按来源维护的有效状态。</summary>
    /// <returns>当前生效的状态位。</returns>
    public virtual GeneState GetStates() { return Owner.AttrComp.States; }
    /// <summary>眩晕、定身或击倒禁止移动。</summary>
    public bool CanMove { get { return (GetStates() & (GeneState.Stun | GeneState.Root | GeneState.Knockdown)) == 0; } }
    /// <summary>眩晕、沉默或击倒禁止施法。</summary>
    public bool CanCast { get { return (GetStates() & (GeneState.Stun | GeneState.Silence | GeneState.Knockdown)) == 0; } }

    /// <summary>推进周期和寿命，周期到点直接调用节点 OnCall，不返回中转结果列表。</summary>
    /// <param name="deltaMilliseconds">本帧经过的非负毫秒数。</param>
    public virtual void Update(int deltaMilliseconds)
    {
        if (deltaMilliseconds <= 0) return;
        ElapsedMilliseconds += deltaMilliseconds;
        // 直接执行资源可能触发共享生命死亡清理，使用原对象快照避免删除影响本帧索引。
        List<GeneBase> current = new List<GeneBase>(genes);
        for (int index = current.Count - 1; index >= 0; index--)
        {
            // 已被前一个节点的死亡清理移除的效果不再执行。
            GeneBase gene = current[index];
            if (!genes.Contains(gene)) continue;
            // 周期只推进到根节点到期时刻，不能结算寿命以外的时间。
            long activeMilliseconds = deltaMilliseconds;
            if (gene.RemainingMilliseconds >= 0) activeMilliseconds = Math.Min(activeMilliseconds, gene.RemainingMilliseconds);
            for (int nodeIndex = 0; nodeIndex < gene.ActiveNodes.Count; nodeIndex++)
            {
                // 当前子节点自己决定如何处理周期，普通属性无需逐帧重新取值。
                GeneBase node = gene.ActiveNodes[nodeIndex];
                if (node.AppliesToScenario(Scenario)) node.AdvanceTime(activeMilliseconds, Owner);
            }
            if (gene.RemainingMilliseconds >= 0)
            {
                gene.RemainingMilliseconds -= activeMilliseconds;
                if (gene.RemainingMilliseconds == 0) RemoveGene(gene);
            }
        }
    }

    /// <summary>把无目标事件交给各事件节点的独立 OnCall。</summary>
    /// <param name="geneEvent">实际发生的角色事件。</param>
    /// <returns>实际激活的子节点数量。</returns>
    public virtual int Trigger(GeneEventType geneEvent) { return Trigger(new GeneTriggerArgs(geneEvent)); }

    /// <summary>把真实事件分派给事件与次数节点，由它们自己判断并执行效果。</summary>
    /// <param name="args">事件类型、实际对方与战斗数据。</param>
    /// <returns>实际施加或刷新的子节点数量。</returns>
    public virtual int Trigger(GeneTriggerArgs args)
    {
        if (args.Event == GeneEventType.None) return 0;
        // 回调可能增删节点，同一事件只通知开始时已经存在的原对象。
        List<GeneBase> current = new List<GeneBase>(genes);
        // 用于向原调用方返回实际发生的效果数量。
        int activated = 0;
        for (int index = current.Count - 1; index >= 0; index--)
        {
            // 已被资源结算清理的节点不重复响应。
            GeneBase gene = current[index];
            if (!genes.Contains(gene)) continue;
            // 成功事件会改变次数或冷却，通知当前根；失败事件不发增量。
            int previousActivated = activated;
            for (int nodeIndex = 0; nodeIndex < gene.ActiveNodes.Count; nodeIndex++)
            {
                // 属性和周期节点有各自的时机，不在战斗事件中重复施加。
                GeneBase node = gene.ActiveNodes[nodeIndex];
                if (node.WaitsForEvent) activated += node.OnCall(Owner, args);
            }
            if (gene.IsExhausted) RemoveGene(gene);
            else if (activated > previousActivated && genes.Contains(gene)) Changed?.Invoke(gene, false);
        }
        return activated;
    }

    /// <summary>汇总当前角色按真实结算前数据产生的伤害加成百分点；由权威结算方在扣血前调用。</summary>
    /// <param name="context">本次结算前的真实双方资源与目标类别。</param>
    /// <returns>各条件节点返回的百分点之和，没有条件节点时为 0。</returns>
    public virtual double ResolveDamageBonus(GeneDamageContext context)
    {
        // 同时遍历根节点和已激活子项，组合与事件下的条件节点都能被结算方读到。
        double bonus = 0;
        for (int index = 0; index < genes.Count; index++)
        {
            IReadOnlyList<GeneBase> nodes = genes[index].ActiveNodes;
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                ConditionDamageGeneBase node = nodes[nodeIndex] as ConditionDamageGeneBase;
                if (node == null) continue;
                // 当前环境不适用的节点不参与本次结算，与施加时的场景判断保持一致。
                if (!node.AppliesToScenario(Scenario)) continue;
                bonus += node.ResolveBonus(context);
            }
        }
        return bonus;
    }

    /// <summary>汇总当前角色按伤害来源类别产生的减伤百分点；由受击结算方在扣血前调用。</summary>
    /// <param name="attackerIsBoss">本次伤害来源是否为真实首领实体。</param>
    /// <returns>各条件减伤节点返回的百分点之和，没有此类节点时为 0。</returns>
    public virtual double ResolveDamageReduction(bool attackerIsBoss)
    {
        // 减伤节点只在受击侧持有，根节点与关联子项都要读到。
        double reduction = 0;
        for (int index = 0; index < genes.Count; index++)
        {
            IReadOnlyList<GeneBase> nodes = genes[index].ActiveNodes;
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                ConditionReduceGeneBase node = nodes[nodeIndex] as ConditionReduceGeneBase;
                if (node == null) continue;
                // 当前环境不适用的节点不参与本次减伤。
                if (!node.AppliesToScenario(Scenario)) continue;
                reduction += node.ResolveReduction(attackerIsBoss);
            }
        }
        return reduction;
    }

    /// <summary>角色离场时先撤销各节点的持续效果，再释放集合。</summary>
    public virtual void Clear()
    {
        for (int index = genes.Count - 1; index >= 0; index--) RemoveGene(genes[index]);
    }

    /// <summary>事件节点使用原角色的随机源做万分比判定。</summary>
    /// <returns>0 至 9999 的随机值。</returns>
    internal int NextTriggerRoll() { return random.Next(10000); }
}
