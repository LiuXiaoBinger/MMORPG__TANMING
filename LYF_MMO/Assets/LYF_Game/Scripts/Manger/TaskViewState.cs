/// <summary>
/// 任务在客户端界面上的展示状态，只用于界面呈现，不代表服务端权威状态。
/// 服务端权威的完成状态由协议下发，客户端不据此发奖。
/// </summary>
public enum TaskViewState
{
    /// <summary>已接取且目标未完成，显示进行中样式。</summary>
    Accepted = 0,
    /// <summary>目标已达成、等待交付，界面只提示可交付，不自动完成。</summary>
    Finishable = 1,
    /// <summary>服务端已确认完成，显示完成样式。</summary>
    Finished = 2
}
