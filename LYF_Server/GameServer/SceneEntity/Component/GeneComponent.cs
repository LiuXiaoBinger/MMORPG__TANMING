using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 服务端统一基因组件：角色与场景怪物共用同一实现，宿主通过现有 IGeneOwner 暴露真实属性组件与自身运行容器。
/// 怪物由实体容器按 Gene 类别装配；角色由 RoleAttributeComponent 的属性槽位依次驱动生命周期。
/// 组件本身不持有宿主专有接口，怪物专有数据仍留在各自属性组件。
/// </summary>
public class GeneComponent : IEntityComponent
{
    /// <summary>真实宿主，角色为 OnlineRole，怪物为场景实体；节点通过它访问属性组件与自身容器。</summary>
    private readonly IGeneOwner owner;

    /// <summary>角色侧属性通知入口；怪物为 null，因此怪物不做角色网络同步。</summary>
    private readonly RoleAttributeComponent roleAttributes;

    /// <summary>当前宿主独立持有的具体节点容器。</summary>
    public GeneRuntime Runtime { get; private set; }

    /// <summary>本次通知前发生变化的原根引用；只有角色侧分配，怪物侧保持空引用。</summary>
    private readonly List<GeneBase> changedGenes;

    /// <summary>登录装备恢复后的第一次属性通知必须包含完整列表，空列表也能清空客户端旧状态。</summary>
    private bool initialSync = true;

    /// <summary>组件在宿主容器中的类别键，角色与怪物同为 Gene。</summary>
    public EntityComponentType ComponentType
    {
        get { return EntityComponentType.Gene; }
    }

    /// <summary>创建基因组件；角色侧额外传入属性组件以获得变化通知入口，场景怪物留空。</summary>
    /// <param name="owner">持有本组件的真实宿主，角色为 OnlineRole，怪物为场景实体。</param>
    /// <param name="roleAttributes">角色侧属性通知入口；怪物不传，因此怪物不做角色网络同步。</param>
    public GeneComponent(IGeneOwner owner, RoleAttributeComponent roleAttributes = null)
    {
        this.owner = owner;
        this.roleAttributes = roleAttributes;
        if (roleAttributes != null)
        {
            // 只有角色需要合并下发变化身份，怪物不分配该列表。
            this.changedGenes = new List<GeneBase>();
        }
    }

    /// <summary>创建真实节点容器；角色额外订阅根变化以便合并进属性通知。</summary>
    public void Initialize()
    {
        Runtime = new GeneRuntime(owner, LubanMgr.Instance.GetGeneFactory());
        if (roleAttributes != null)
        {
            Runtime.Changed += OnGeneChanged;
        }
    }

    /// <summary>收集新增、刷新、叠层、次数和删除的根，发送时读取该身份的最终状态。</summary>
    /// <param name="gene">发生实际变化的原根。</param>
    /// <param name="removed">是否已移除；发送前通过原身份查询最终存在状态。</param>
    private void OnGeneChanged(GeneBase gene, bool removed)
    {
        if (!changedGenes.Contains(gene)) changedGenes.Add(gene);
    }

    /// <summary>推进节点寿命；角色只在资源或节点变化时发送属性面板，怪物不做同步。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧毫秒数。</param>
    public void Update(int deltaMilliseconds)
    {
        Runtime.Update(deltaMilliseconds);
        if (roleAttributes == null)
        {
            return;
        }
        if (roleAttributes.ResourcesChanged || changedGenes.Count > 0)
        {
            roleAttributes.NotifyAttributeUpdate();
        }
    }

    /// <summary>执行真实事件；角色的结果经属性通知合并发送，怪物仅结算效果。</summary>
    /// <param name="args">真实战斗事件与对方。</param>
    /// <returns>实际激活的子节点数量。</returns>
    public int Trigger(GeneTriggerArgs args)
    {
        // 节点 OnCall 已完成本次业务结算，下面只发布结果。
        int activated = Runtime.Trigger(args);
        if (roleAttributes == null)
        {
            return activated;
        }
        if (activated > 0 || changedGenes.Count > 0)
        {
            roleAttributes.NotifyAttributeUpdate();
        }
        return activated;
    }

    /// <summary>将本次完整或变化列表写入既有11162回包；只有角色侧调用，普通时间推进不会产生变化项。</summary>
    /// <param name="notification">即将下发的服务器最终属性通知。</param>
    public void AppendSynchronization(RoleAttributeUpdateNtf notification)
    {
        notification.GenesFull = initialSync;
        notification.GeneScenario = Runtime.Scenario;
        if (initialSync)
        {
            // 首次装备恢复后的真实根集合，包括没有任何基因的空列表。
            for (int index = 0; index < Runtime.ActiveGenes.Count; index++)
            {
                notification.Genes.Add(CreateData(Runtime.ActiveGenes[index]));
            }
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
                    if (sent.GeneId == changed.GeneID && sent.SourceId == changed.SourceId
                        && sent.SourceSlot == changed.SourceSlot)
                    {
                        alreadyAdded = true;
                    }
                }
                if (alreadyAdded)
                {
                    continue;
                }
                if (current != null)
                {
                    notification.Genes.Add(CreateData(current));
                }
                else
                {
                    notification.Genes.Add(new RoleGeneData
                    {
                        GeneId = changed.GeneID,
                        SourceId = changed.SourceId,
                        SourceSlot = changed.SourceSlot,
                        Removed = true
                    });
                }
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
        RoleGeneData data = new RoleGeneData
        {
            GeneId = gene.GeneID,
            SourceId = gene.SourceId,
            SourceSlot = gene.SourceSlot,
            FirerId = gene.FirerId,
            StackCount = gene.StackCount,
            RemainingMs = gene.GetDisplayRemainingMilliseconds(),
            Name = gene.DisplayName,
            EffectType = (int)gene.EffectType,
            Scenario = gene.Scenario
        };
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
        for (int index = 0; index < gene.Children.Count; index++)
        {
            data.Children.Add(CreateData(gene.Children[index]));
        }
        return data;
    }

    /// <summary>宿主离场时角色先解除订阅并清空待发送身份，所有实例最后都撤销节点效果。</summary>
    public void Shutdown()
    {
        if (roleAttributes != null)
        {
            Runtime.Changed -= OnGeneChanged;
            changedGenes.Clear();
        }
        Runtime.Clear();
    }
}
