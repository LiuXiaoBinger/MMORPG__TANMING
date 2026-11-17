using System;
using System.Collections.Generic;

/// <summary>联机客户端的具体节点容器，只维护服务器展示状态，不调用属性、资源或事件结算。</summary>
public sealed class ClientGeneRuntime : GeneRuntime
{
    /// <summary>只有尚未到零的有限寿命根需要逐帧显示计时，永久节点不加入。</summary>
    private readonly List<GeneBase> timedGenes = new List<GeneBase>();
    /// <summary>上次权威节点变化后汇总的状态，普通帧不重新扫描。</summary>
    private GeneState displayedStates;

    /// <summary>沿用真实实体和公共工厂，不创建代理角色。</summary>
    /// <param name="owner">本客户端实际实体。</param>
    /// <param name="factory">已有配置工厂，直接节点同步仍由公共具体类型工厂创建。</param>
    public ClientGeneRuntime(IGeneOwner owner, GeneFactory factory) : base(owner, factory) { }

    /// <summary>联机客户端挂载只保存节点，不能对服务器最终属性再次执行加成。</summary>
    /// <param name="gene">本次挂载或刷新的具体根节点。</param>
    /// <param name="includeInstant">沿用基类签名；客户端所有资源效果均由服务器通知。</param>
    protected override void CallAttachedNodes(GeneBase gene, bool includeInstant) { }

    /// <summary>联机客户端移除只改变展示，不能从服务器最终属性中再次扣除加成。</summary>
    /// <param name="gene">服务器已确认移除的具体根。</param>
    protected override void RemoveNodeEffects(GeneBase gene) { }

    /// <summary>客户端没有权威事件结算，实际事件结果由服务器更新节点。</summary>
    /// <param name="args">客户端持有的事件参数，不在这里产生业务效果。</param>
    /// <returns>客户端不激活服务器节点，恒为零。</returns>
    public override int Trigger(GeneTriggerArgs args) { return 0; }

    /// <summary>只推进有限寿命的显示时间；到零仍保留节点，等待服务器移除。</summary>
    /// <param name="deltaMilliseconds">本帧毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (deltaMilliseconds <= 0) return;
        // 永久节点和已显示为零的节点不进入此循环。
        for (int index = timedGenes.Count - 1; index >= 0; index--)
        {
            // 原具体根节点保存当前显示倒计时，不另建节点镜像。
            GeneBase gene = timedGenes[index];
            gene.AdvanceDisplayTime(deltaMilliseconds);
            if (gene.RemainingMilliseconds == 0) timedGenes.RemoveAt(index);
        }
    }

    /// <summary>返回上次同步变化后计算的状态，客户端不执行状态节点 OnCall。</summary>
    /// <returns>服务器当前生效节点提供的状态位。</returns>
    public override GeneState GetStates() { return displayedStates; }

    /// <summary>完整列表按身份对齐，增量仅更新指定根；空增量保持，空完整清空。</summary>
    /// <param name="notification">本角色已经校验身份的11162通知，仅在转换边界读取。</param>
    /// <returns>列表、名称、层数、次数或状态有真实变化时返回 true；只有时间校正返回 false。</returns>
    public bool ApplySynchronization(RoleAttributeUpdateNtf notification)
    {
        // 变化只决定界面列表刷新，不影响服务器属性值。
        bool changed = Scenario != notification.GeneScenario;
        if (Scenario != notification.GeneScenario) SetScenario(notification.GeneScenario);
        if (notification.GenesFull)
        {
            // 先移除完整列表中已经不存在的根，同数量替换也能正确处理。
            for (int index = genes.Count - 1; index >= 0; index--)
            {
                // 待核对的实际根引用。
                GeneBase gene = genes[index];
                // 完整通知中是否仍有同一编号、来源和槽。
                bool present = false;
                for (int dataIndex = 0; dataIndex < notification.Genes.Count; dataIndex++)
                {
                    // 只比较稳定身份，不比较本地已推进的时间。
                    RoleGeneData data = notification.Genes[dataIndex];
                    if (Matches(gene, data) && !data.Removed) present = true;
                }
                if (!present) { RemoveGene(gene); changed = true; }
            }
        }
        // 协议对象只在本次接收中使用，最终保留的始终是具体 GeneBase。
        for (int index = 0; index < notification.Genes.Count; index++)
        {
            // 当前完整或增量项，不放入运行容器。
            RoleGeneData data = notification.Genes[index];
            // 现有根保持引用，只有结构确实改变才替换。
            GeneBase gene = FindGene(data.GeneId, data.SourceId, data.SourceSlot);
            if (data.Removed)
            {
                if (gene != null) { RemoveGene(gene); changed = true; }
                continue;
            }
            if (gene != null && !MatchesTree(gene, data)) { RemoveGene(gene); gene = null; changed = true; }
            if (gene == null)
            {
                gene = CreateNode(data);
                // 客户端基类挂载入口已覆盖为无结算，仍使用实际根集合与来源身份。
                AddGene(gene, data.SourceId, data.SourceSlot, data.FirerId);
                changed = true;
            }
            if (CopyDisplay(gene, data)) changed = true;
            if (gene.RemainingMilliseconds > 0)
            {
                if (!timedGenes.Contains(gene)) timedGenes.Add(gene);
            }
            else timedGenes.Remove(gene);
        }
        if (changed)
        {
            displayedStates = GeneState.None;
            // 只在节点变化时汇总状态，事件待触发子树不提供状态。
            for (int index = 0; index < genes.Count; index++) displayedStates |= ReadStates(genes[index]);
            if ((displayedStates & GeneState.SuperArmor) != 0) displayedStates &= ~(GeneState.Stun | GeneState.Root);
        }
        return changed;
    }

    /// <summary>同步移除后也停止该原节点的显示计时。</summary>
    /// <param name="gene">当前真实根对象。</param>
    /// <returns>集合确实移除了该节点时返回 true。</returns>
    public override bool RemoveGene(GeneBase gene) { timedGenes.Remove(gene); return base.RemoveGene(gene); }

    /// <summary>角色离场只清理展示，不触碰已同步的最终属性。</summary>
    public override void Clear() { base.Clear(); timedGenes.Clear(); displayedStates = GeneState.None; }

    /// <summary>核对一个根的稳定身份。</summary>
    /// <param name="gene">当前实际节点。</param>
    /// <param name="data">网络边界中的根记录。</param>
    /// <returns>编号、来源和槽位都相同时返回 true。</returns>
    private static bool Matches(GeneBase gene, RoleGeneData data) { return gene.GeneID == data.GeneId && gene.SourceId == data.SourceId && gene.SourceSlot == data.SourceSlot; }

    /// <summary>实际类型和关联树相同才能复用节点引用。</summary>
    /// <param name="gene">现有具体节点。</param>
    /// <param name="data">服务器实际节点结构。</param>
    /// <returns>类型与子树结构一致时返回 true。</returns>
    private static bool MatchesTree(GeneBase gene, RoleGeneData data)
    {
        if (gene.GeneID != data.GeneId || (int)gene.EffectType != data.EffectType || gene.Children.Count != data.Children.Count) return false;
        // 属性节点还必须拥有相同的具体属性类型。
        AttributeGeneBase attribute = gene as AttributeGeneBase;
        if (attribute != null && (int)attribute.Attribute != data.AttributeId) return false;
        // 保留子节点对应的原顺序与独立次数。
        for (int index = 0; index < gene.Children.Count; index++) if (!MatchesTree(gene.Children[index], data.Children[index])) return false;
        return true;
    }

    /// <summary>从服务器实际类型创建具体节点，支持客户端没有配置行的魔神塔动态基因。</summary>
    /// <param name="data">当前网络节点，仅用于类型和树转换。</param>
    /// <returns>没有执行任何业务的独立具体节点。</returns>
    private static GeneBase CreateNode(RoleGeneData data)
    {
        // 同一工厂映射同时服务 Luban 配置装载与网络展示。
        GeneBase gene = GeneFactory.CreateNode((GeneEffectType)data.EffectType, (GeneAttributeType)data.AttributeId);
        gene.GeneID = data.GeneId;
        gene.Scenario = data.Scenario;
        // 组合与事件子树同样保留实际具体类型。
        for (int index = 0; index < data.Children.Count; index++) gene.AddChild(CreateNode(data.Children[index]));
        return gene;
    }

    /// <summary>更新实际节点上的展示值；倒计时校正单独赋值，不触发列表重建。</summary>
    /// <param name="gene">需要保留的实际节点引用。</param>
    /// <param name="data">服务器确认的展示字段。</param>
    /// <returns>除时间之外的可见数据是否变化。</returns>
    private static bool CopyDisplay(GeneBase gene, RoleGeneData data)
    {
        // 根层数或名称变化需要更新对应界面行。
        bool changed = gene.StackCount != data.StackCount || gene.DisplayName != data.Name || gene.Scenario != data.Scenario;
        gene.DisplayName = data.Name;
        gene.Scenario = data.Scenario;
        gene.ApplyDisplayState(data.SourceId, data.SourceSlot, data.FirerId, data.StackCount, data.RemainingMs);
        // 仅复制状态位，不调用状态节点的业务方法。
        StateGene state = gene as StateGene;
        if (state != null) { if ((int)state.States != data.State) changed = true; state.States = (GeneState)data.State; }
        // 事件类型用于展示触发时机。
        EventGene triggered = gene as EventGene;
        if (triggered != null) { if ((int)triggered.TriggerEvent != data.TriggerEvent) changed = true; triggered.TriggerEvent = (GeneEventType)data.TriggerEvent; }
        // 次数节点采用服务器已经消耗后的值。
        CountGene counted = gene as CountGene;
        if (counted != null) { if (counted.RemainingTriggers != data.RemainingTriggers) changed = true; counted.ApplyDisplayTriggers(data.RemainingTriggers); }
        // 嵌套次数与状态都属于真实子节点，不建立第二份运行数据。
        for (int index = 0; index < gene.Children.Count; index++) if (CopyDisplay(gene.Children[index], data.Children[index])) changed = true;
        return changed;
    }

    /// <summary>仅合并当前场景已经激活的状态节点，跳过事件待触发子树。</summary>
    /// <param name="gene">当前实际节点。</param>
    /// <returns>该子树已经生效的状态。</returns>
    private GeneState ReadStates(GeneBase gene)
    {
        if (gene.Scenario != 0 && gene.Scenario != Scenario) return GeneState.None;
        // 本节点若是状态类型则贡献其状态位。
        GeneState states = GeneState.None;
        // 使用具体状态类型读取展示状态，不运行 OnCall。
        StateGene state = gene as StateGene;
        if (state != null) states |= state.States;
        if (gene.WaitsForEvent) return states;
        // 普通关联项已随根生效。
        for (int index = 0; index < gene.Children.Count; index++) states |= ReadStates(gene.Children[index]);
        return states;
    }
}
