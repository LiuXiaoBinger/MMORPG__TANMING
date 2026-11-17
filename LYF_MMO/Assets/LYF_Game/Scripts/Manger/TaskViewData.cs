/// <summary>
/// 单个任务的界面展示数据，由服务端任务快照转换而来，只供任务窗口与快捷面板读取。
/// 界面不得自行修改其中的进度、数量或奖励字段。
/// </summary>
public class TaskViewData
{
    /// <summary>普通任务为 QuestInfo 编号；副本展示条目为负的服务器目标编号。</summary>
    public int TaskId;
    /// <summary>是否为副本阶段投影；只展示权威目标，不提供普通任务接取、放弃或交付。</summary>
    public bool IsDungeonObjective;
    /// <summary>任务标题文本，对应标题节点的 TitleText。</summary>
    public string Title;
    /// <summary>任务类型名称文本，无配置时为空字符串。</summary>
    public string TypeName;
    /// <summary>任务或目标描述文本，无描述时为空字符串。</summary>
    public string Description;
    /// <summary>任务目标行的显示文字，可包含当前数量和目标数量。</summary>
    public string TargetText;
    /// <summary>任务展示状态，决定标题与进度样式。</summary>
    public TaskViewState State;
    /// <summary>所属页签，决定在哪个页签下展示。</summary>
    public TaskPageFilter Page;
    /// <summary>是否存在时限信息，为 false 时隐藏截止时间行。</summary>
    public bool HasDeadline;
    /// <summary>截止时间显示文本，仅在 HasDeadline 为 true 时有意义。</summary>
    public string DeadlineText;
    /// <summary>是否存在补充限制说明，为 false 时隐藏限制行。</summary>
    public bool HasLimit;
    /// <summary>补充限制说明文本，仅在 HasLimit 为 true 时有意义。</summary>
    public string LimitText;
    /// <summary>是否存在奖励展示内容，为 false 时隐藏奖励区域。</summary>
    public bool HasReward;
    /// <summary>是否允许放弃，决定放弃按钮是否显示。</summary>
    public bool CanGiveUp;
    /// <summary>保留的任务追踪标志；当前 TaskMgr 的快捷列表不按此字段过滤。</summary>
    public bool NeedTrack;
}
