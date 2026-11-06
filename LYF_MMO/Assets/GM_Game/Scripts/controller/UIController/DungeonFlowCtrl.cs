using System.Text;

/// <summary>将权威副本目标和本地附近物件接入主视图，点击只发送服务器请求。</summary>
public class DungeonFlowCtrl : CtrlBase
{
    /// <summary>由 MainView 持有的正式副本窗口。</summary>
    private readonly DungeonFlowWindow window;

    /// <summary>订阅快照与附近交互变化。</summary>
    /// <param name="view">已完成窗口注册的主视图。</param>
    public DungeonFlowCtrl(MainView view) : base(view)
    {
        window = view.DungeonFlowWindow;
        window.InteractionRequested += OnInteraction;
        TeamData.Instance.DungeonFlowUpdated += Refresh;
        TeamData.Instance.NearbyInteractionUpdated += RefreshInteraction;
        Refresh();
    }

    /// <summary>完整主界面销毁时解除所有数据和按钮订阅。</summary>
    public override void Dispose()
    {
        window.InteractionRequested -= OnInteraction;
        TeamData.Instance.DungeonFlowUpdated -= Refresh;
        TeamData.Instance.NearbyInteractionUpdated -= RefreshInteraction;
    }

    /// <summary>仅显示服务器给出的目标与完成量，不计算本地进度。</summary>
    private void Refresh()
    {
        // 本次最新权威阶段；退出时空值收起窗口。
        DungeonFlowState flow = TeamData.Instance.DungeonFlow;
        if (flow == null)
        {
            window.CloseWindow();
            return;
        }
        // 各目标保持服务器顺序，便于沿副本路线阅读。
        StringBuilder text = new StringBuilder("魔神塔\n");
        for (int index = 0; index < flow.Objectives.Count; index++)
        {
            // 原目标描述与实际计数，不补出尚未激活的目标。
            DungeonObjectiveData objective = flow.Objectives[index];
            if (objective.Completed) text.Append("✓ ");
            text.Append(objective.Description).Append("  ").Append(objective.Current).Append("/").Append(objective.Total).Append('\n');
        }
        if (flow.Finished) text.Append("挑战完成，请前往出口传送");
        window.SetObjectives(text.ToString());
        if (!window.gameObject.activeSelf) ShowMainWindow(WindowType.DungeonFlowWindow);
        RefreshInteraction();
    }

    /// <summary>候选物件变化后切换按钮标题和显隐。</summary>
    private void RefreshInteraction()
    {
        // 场景只报告当前附近候选，不提前改变服务器物件状态。
        MSceneInteraction interaction = TeamData.Instance.NearbyInteraction;
        if (interaction == null) window.SetInteraction(null);
        else window.SetInteraction(interaction.Title);
    }

    /// <summary>点击时读取最新候选，发送其配置身份。</summary>
    private void OnInteraction()
    {
        // 离开区域后候选被清空，不发送上一次按钮的数据。
        MSceneInteraction interaction = TeamData.Instance.NearbyInteraction;
        if (interaction != null) TeamCtrl.RequestDungeonInteraction(interaction);
    }
}
