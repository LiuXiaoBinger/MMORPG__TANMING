/// <summary>原AISendSceneEvent延时毫秒与强类型事件载荷。</summary>
internal sealed class MoshentaScheduledEvent
{
    /// <summary>所属实例逻辑时钟的派发时刻。</summary>
    internal long At;
    /// <summary>原事件参数，实例内派发后释放。</summary>
    internal MoshentaAiEvent Event;
}
