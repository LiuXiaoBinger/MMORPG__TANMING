/// <summary>
/// 把 FishingStarted 事件接入钓鱼动作状态，并指定 Fishing 动画名。
/// 本类不处理钓鱼流程或产出。
/// </summary>
public sealed class MFishingComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化钓鱼状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MFishingComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateFishing; }

    /// <summary>钓鱼状态对应的动画机状态名。</summary>
    public override string StateName { get { return "Fishing"; } }

    /// <summary>注册钓鱼开始事件：收到钓鱼触发消息时切入钓鱼状态。</summary>
    protected override void RegisterActionEvents() { RegisterActionEvent(MEventType.FishingStarted); }
    
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
