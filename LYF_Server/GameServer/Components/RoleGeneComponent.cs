using System.Collections.Generic;

/// <summary>在线角色的权威基因组件，推进真实效果并收集根节点变化供现有属性通知同步。</summary>
public class RoleGeneComponent : RoleComponentBase
{
    /// <summary>当前角色直接持有的实际 GeneBase 节点。</summary>
    public GeneRuntime Runtime { get; private set; }
    /// <summary>本次通知前发生变化的原根引用，仅在实际变化时加入，不按时间生成全表。</summary>
    private readonly List<GeneBase> changedGenes = new List<GeneBase>();
    /// <summary>登录装备恢复后的第一次属性通知必须包含完整列表，空列表也能清空客户端旧状态。</summary>
    private bool initialSync = true;

    /// <summary>绑定当前实际在线角色。</summary>
    /// <param name="owner">持有本组件的角色。</param>
    public RoleGeneComponent(OnlineRole owner) : base(owner) { }

    /// <summary>连接真实角色、具体节点工厂及变化通知。</summary>
    public override void Initialize()
    {
        Runtime = new GeneRuntime(Owner, LubanMgr.Instance.GetGeneFactory());
        Runtime.Changed += OnGeneChanged;
    }

    /// <summary>收集新增、刷新、叠层、次数和删除的根，发送时读取该身份的最终状态。</summary>
    /// <param name="gene">发生实际变化的原根。</param>
    /// <param name="removed">是否已移除；发送前通过原身份查询最终存在状态。</param>
    private void OnGeneChanged(GeneBase gene, bool removed)
    {
        if (!changedGenes.Contains(gene)) changedGenes.Add(gene);
    }

    /// <summary>服务器推进实际资源与寿命；只有资源/节点变化才发送。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        Runtime.Update(deltaMilliseconds);
        if (Owner.AttrComp.ResourcesChanged || changedGenes.Count > 0) Owner.AttrComp.NotifyAttributeUpdate();
    }

    /// <summary>真实事件执行完后通过原属性通知合并发送节点和最终属性。</summary>
    /// <param name="args">真实战斗事件与对方。</param>
    /// <returns>实际激活的子节点数量。</returns>
    public int Trigger(GeneTriggerArgs args)
    {
        // 节点 OnCall 已完成本次业务结算，下面只发布结果。
        int activated = Runtime.Trigger(args);
        if (activated > 0 || changedGenes.Count > 0) Owner.AttrComp.NotifyAttributeUpdate();
        return activated;
    }

    /// <summary>将本次完整或变化列表写入既有11162回包；普通时间推进不会产生变化项。</summary>
    /// <param name="notification">即将下发的服务器最终属性通知。</param>
    public void AppendSynchronization(RoleAttributeUpdateNtf notification)
    {
        notification.GenesFull = initialSync;
        notification.GeneScenario = Runtime.Scenario;
        if (initialSync)
        {
            // 首次装备恢复后的真实根集合，包括没有任何基因的空列表。
            for (int index = 0; index < Runtime.ActiveGenes.Count; index++) notification.Genes.Add(CreateData(Runtime.ActiveGenes[index]));
            initialSync = false;
        }
        else
        {
            // 每个变化身份只写一次最终值，覆盖同一批穿脱导致的同数量替换。
            for (int index = 0; index < changedGenes.Count; index++)
            {
                // 原引用用于查找稳定的编号/来源/槽位身份。
                GeneBase changed = changedGenes[index];
                // 当前仍在集合中则发送更新，否则发送该精确身份的删除项。
                GeneBase current = Runtime.FindGene(changed.GeneID, changed.SourceId, changed.SourceSlot);
                // 同批先删后加可能有两个原引用，按根身份避免重复写出。
                bool alreadyAdded = false;
                for (int sentIndex = 0; sentIndex < notification.Genes.Count; sentIndex++)
                {
                    // 当前已加入的协议项仅在序列化边界存活。
                    RoleGeneData sent = notification.Genes[sentIndex];
                    if (sent.GeneId == changed.GeneID && sent.SourceId == changed.SourceId && sent.SourceSlot == changed.SourceSlot) alreadyAdded = true;
                }
                if (alreadyAdded) continue;
                if (current != null) notification.Genes.Add(CreateData(current));
                else notification.Genes.Add(new RoleGeneData { GeneId = changed.GeneID, SourceId = changed.SourceId, SourceSlot = changed.SourceSlot, Removed = true });
            }
        }
        changedGenes.Clear();
    }

    /// <summary>只复制显示和身份字段，实际效果参数和权威结算仍由服务端节点负责。</summary>
    /// <param name="gene">当前实际运行节点，可能来自服务端动态配置。</param>
    /// <returns>一次发送使用的强类型协议对象。</returns>
    private static RoleGeneData CreateData(GeneBase gene)
    {
        // 运行模型与协议对象分离，协议对象不会保存在运行器中。
        RoleGeneData data = new RoleGeneData { GeneId = gene.GeneID, SourceId = gene.SourceId, SourceSlot = gene.SourceSlot,
            FirerId = gene.FirerId, StackCount = gene.StackCount, RemainingMs = gene.GetDisplayRemainingMilliseconds(),
            Name = gene.DisplayName, EffectType = (int)gene.EffectType, Scenario = gene.Scenario };
        if (string.IsNullOrEmpty(data.Name)) data.Name = "基因 " + gene.GeneID;
        // 属性类型本身决定属性语义，不依赖客户端也拥有动态配置行。
        AttributeGeneBase attribute = gene as AttributeGeneBase;
        if (attribute != null) data.AttributeId = (int)attribute.Attribute;
        // 状态位只用于客户端状态展示，不在客户端执行属性增减。
        StateGene state = gene as StateGene;
        if (state != null) data.State = (int)state.States;
        // 事件类型用于界面说明触发时机。
        EventGene triggered = gene as EventGene;
        if (triggered != null) data.TriggerEvent = (int)triggered.TriggerEvent;
        // 次数保留各子树原节点的独立剩余值。
        CountGene counted = gene as CountGene;
        if (counted != null) data.RemainingTriggers = counted.RemainingTriggers;
        // 全部关联类型均保留，保证事件内部的次数同样可显示。
        for (int index = 0; index < gene.Children.Count; index++) data.Children.Add(CreateData(gene.Children[index]));
        return data;
    }

    /// <summary>先解除同步订阅，再撤销实际服务器效果并释放角色引用。</summary>
    public override void Dispose()
    {
        Runtime.Changed -= OnGeneChanged;
        Runtime.Clear();
        changedGenes.Clear();
        base.Dispose();
    }
}
