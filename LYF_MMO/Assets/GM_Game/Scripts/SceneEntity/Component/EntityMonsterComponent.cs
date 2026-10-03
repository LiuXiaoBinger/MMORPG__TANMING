using MMoRpgCommon.SceneEntity;

/// <summary>
/// 怪物实体业务组件，对应参考项目 ROMonsterComponent。
/// 保存怪物配置 ID 与品质等级，供 AI、表现和掉落等系统查询。
/// </summary>
public sealed class EntityMonsterComponent : EntityComponentBase
{
    /// <summary>构造怪物组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityMonsterComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Monster"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Monster; }
    }

    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }

    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }

    /// <summary>怪物配置表 ID，决定怪物模型、数值和技能等静态数据。</summary>
    public int MonsterId { get; set; }

    /// <summary>怪物品质等级（普通/精英/Boss 等），影响血条、名字颜色和 AI 表现。</summary>
    public MonsterRank Rank { get; set; }

    /// <summary>对象池回收时重置怪物 ID 和品质为默认值。</summary>
    public override void Shutdown()
    {
        MonsterId = 0;
        Rank = MonsterRank.Normal;
    }
}
