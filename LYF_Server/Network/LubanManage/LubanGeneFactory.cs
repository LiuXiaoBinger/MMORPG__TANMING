using System;
using System.Collections.Generic;

/// <summary>Luban 配置到运行节点的唯一转换工厂；持有原索引以读取后续动态注册的定义。</summary>
public class LubanGeneFactory : GeneFactory
{
    /// <summary>配置装载层的原始索引，仅在创建边界查询，不复制或修改配置行。</summary>
    private readonly Dictionary<int, cfg.Gene> definitions;

    /// <summary>连接已装载的配置索引，新增内部配置可在下次创建时直接读取。</summary>
    /// <param name="geneDefinitions">Luban 管理器或测试配置装载层提供的原索引。</param>
    public LubanGeneFactory(Dictionary<int, cfg.Gene> geneDefinitions) { definitions = geneDefinitions; }

    /// <summary>创建完整独立节点树；同编号多次创建不会共享数值、周期、冷却或次数。</summary>
    /// <param name="geneId">需要创建的根配置编号。</param>
    /// <param name="value">仅覆盖根节点每层数值，关联子节点保持各自表值。</param>
    /// <returns>配置转换后的具体 GeneBase 运行节点。</returns>
    public override GeneBase CreateGene(int geneId, float? value = null)
    {
        // 路径集合只检测当前递归链，允许不同分支合法引用同一编号。
        GeneBase root = CreateNode(geneId, new HashSet<int>());
        if (value.HasValue) root.Value = value.Value;
        root.ResetTriggers();
        return root;
    }

    /// <summary>转换当前配置行并按五个槽位顺序创建关联子树。</summary>
    /// <param name="geneId">当前配置编号。</param>
    /// <param name="path">当前递归路径中的编号，不共享到其他创建请求。</param>
    /// <returns>不持有配置引用的完整运行节点。</returns>
    private GeneBase CreateNode(int geneId, HashSet<int> path)
    {
        if (!path.Add(geneId)) throw new InvalidOperationException("基因组合存在循环引用：" + geneId);
        // cfg.Gene 仅存在于工厂转换边界，后续运行只读取复制后的强类型节点。
        cfg.Gene definition = definitions[geneId];
        // EffectType 和 AttributeId 只负责一次类型选择，不参与运行器行为分派。
        GeneBase node = CreateType(definition);
        node.GeneID = definition.Id;
        node.DisplayName = definition.EffectDesc;
        node.Value = definition.Value;
        node.ValueType = definition.ValueType;
        node.DurationSeconds = definition.GeneTime;
        node.Scenario = definition.Scenario;
        node.AllowsStacking = definition.IsStack == 0;
        node.MaxStack = definition.MaxStack;
        node.TargetOpponent = definition.Target == 1;
        // 全部具体类型都能组合，槽位为零表示未配置关联项。
        int[] attachedIds = { definition.AttachGeneId0, definition.AttachGeneId1, definition.AttachGeneId2,
            definition.AttachGeneId3, definition.AttachGeneId4 };
        // 子树同样由本次调用独立创建，原配置行与其他实例不保存运行状态。
        for (int index = 0; index < attachedIds.Length; index++)
        {
            if (attachedIds[index] != 0) node.AddChild(CreateNode(attachedIds[index], path));
        }
        path.Remove(geneId);
        return node;
    }

    /// <summary>按效果类别选择具体节点，并只复制该类型实际使用的配置参数。</summary>
    /// <param name="definition">当前待转换的配置行，转换后不保留引用。</param>
    /// <returns>尚未连接子树的具体运行节点。</returns>
    private static GeneBase CreateType(cfg.Gene definition)
    {
        // 配置装载和客户端同步使用相同的具体类型映射。
        GeneBase node = GeneFactory.CreateNode((GeneEffectType)definition.EffectType, (GeneAttributeType)definition.AttributeId);
        // 状态位属于该具体节点的参数，运行过程中不再读取配置行。
        StateGene state = node as StateGene;
        if (state != null) state.States = (GeneState)definition.State;
        // 周期配置在边界明确拒绝零间隔，避免运行时无法推进。
        TimeBuffGeneBase periodic = node as TimeBuffGeneBase;
        if (periodic != null)
        {
            if (definition.Interval <= 0) throw new InvalidOperationException("周期基因的 Interval 必须大于零：" + definition.Id);
            periodic.IntervalMilliseconds = definition.Interval;
        }
        // 条件增伤节点读取自己的阈值百分点；其他类型忽略该列。
        ConditionDamageGeneBase conditional = node as ConditionDamageGeneBase;
        if (conditional != null) conditional.Condition = definition.Condition;
        // 条件减伤节点同样按条件列区分来源类别。
        ConditionReduceGeneBase reduce = node as ConditionReduceGeneBase;
        if (reduce != null) reduce.Condition = definition.Condition;
        // 普通事件必须指定事件；次数节点允许接收任意非空事件。
        EventGene triggered = node as EventGene;
        if (triggered != null)
        {
            if (definition.EffectType == (int)GeneEffectType.Event && definition.TriggerEvent == 0)
                throw new InvalidOperationException("事件基因必须指定 TriggerEvent：" + definition.Id);
            triggered.TriggerEvent = (GeneEventType)definition.TriggerEvent;
            triggered.TriggerChance = definition.TriggerChance;
            triggered.CooldownMilliseconds = definition.Cooldown;
        }
        // 次数节点的配置总量与其当前剩余次数分开保存。
        CountGene counted = node as CountGene;
        if (counted != null)
        {
            if (definition.TriggerCount <= 0) throw new InvalidOperationException("次数基因的 TriggerCount 必须大于零：" + definition.Id);
            counted.TriggerLimit = definition.TriggerCount;
        }
        return node;
    }

}
