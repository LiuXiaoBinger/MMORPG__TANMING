/// <summary>
/// 钓鱼动作状态组件。
/// 实体进行钓鱼休闲玩法时的状态。
/// 监听钓鱼开始事件切入本状态。
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
    
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
