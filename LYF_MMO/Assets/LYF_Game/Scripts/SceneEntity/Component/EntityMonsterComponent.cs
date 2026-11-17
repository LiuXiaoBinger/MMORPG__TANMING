using MMoRpgCommon.SceneEntity;

/// <summary>
/// 保存怪物编号及普通、精英、Boss 等档位，随组件回收清空。
/// 这里只存数据，不执行 AI、掉落或名字颜色变化。
/// </summary>
public sealed class EntityMonsterComponent : EntityComponentBase
{

    #region 构造、初始化与事件注册
    /// <summary>构造怪物组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityMonsterComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>数据由公开方法直接写入，此处不订阅事件。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>本组件没有自行订阅事件，无需额外解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }

    /// <summary>对象池回收时重置怪物 ID 和品质为默认值。</summary>
    public override void Shutdown()
    {
        MonsterId = 0;
        Rank = MonsterRank.Normal;
    }
    #endregion

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Monster"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Monster; }
    }

    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }

    /// <summary>怪物配置表 ID，决定怪物模型、数值和技能等静态数据。</summary>
    public int MonsterId { get; set; }

    /// <summary>怪物档位（普通/精英/Boss 等）；此组件只保存该值，不应用显示或 AI 效果。</summary>
    public MonsterRank Rank { get; set; }
}
