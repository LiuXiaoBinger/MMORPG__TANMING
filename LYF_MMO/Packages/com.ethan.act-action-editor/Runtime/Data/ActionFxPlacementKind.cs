/// <summary>粒子预览的位置语义；正式游戏由宿主自己的粒子处理器执行相同规则。</summary>
public enum ActionFxPlacementKind
{
    /// <summary>角色根节点局部坐标，旧资产的默认模式。</summary>
    ActorRoot = 0,
    /// <summary>相对角色模型中完整路径或唯一短名称挂点。</summary>
    NamedSocket = 1,
    /// <summary>目标渲染边界头顶，无目标时显示施法者前方十米的明确兜底。</summary>
    TargetTop = 2,
    /// <summary>角色前方世界落点，X 角使用事件值，Y/Z 角采用角色当前朝向。</summary>
    CasterForwardWorld = 3,
    /// <summary>偏移直接作为世界坐标。</summary>
    WorldAbsolute = 4
}
