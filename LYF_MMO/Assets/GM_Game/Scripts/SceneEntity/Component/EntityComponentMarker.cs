using MMoRpgCommon.SceneEntity;

/// <summary>
/// 用于暂未接入具体表现逻辑的实体组件占位，实现统一生命周期。
/// 该组件不持有业务状态，也不进入对象池复用，回收时直接丢弃。
/// </summary>
public sealed class EntityComponentMarker : EntityComponentBase
{
    /// <summary>占位组件对外声明的类型标识。</summary>
    private readonly EntityComponentType _componentType;

    /// <summary>构造占位组件。</summary>
    /// <param name="owner">宿主实体。</param>
    /// <param name="componentType">对外声明的组件类型。</param>
    public EntityComponentMarker(ROObject owner, EntityComponentType componentType)
        : base(owner)
    {
        _componentType = componentType;
    }

    /// <summary>组件类型标识，由构造时传入。</summary>
    public override EntityComponentType ComponentType
    {
        get { return _componentType; }
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
}
