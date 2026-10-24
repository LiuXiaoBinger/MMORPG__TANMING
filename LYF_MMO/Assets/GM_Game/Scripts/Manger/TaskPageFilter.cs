/// <summary>
/// 任务主窗口与快捷面板共用的页签筛选类型，对应 QuickTaskPanel 预制体中的 Type_1/2/3 三个页签。
/// 首期只有主线有正式数据来源，其余页签在数据为空时展示空态。
/// </summary>
public enum TaskPageFilter
{
    /// <summary>主线任务，首期默认追踪的页签。</summary>
    MainLine = 1,
    /// <summary>日常任务，首期无服务端数据时展示空态。</summary>
    Daily = 2,
    /// <summary>活动任务，首期无服务端数据时展示空态。</summary>
    Activity = 3
}
