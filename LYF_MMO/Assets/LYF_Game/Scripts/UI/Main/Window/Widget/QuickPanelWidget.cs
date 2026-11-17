using UnityEngine;
using UnityEngine.UI;

/// <summary>沿用 RO QuickPanel 页签，让任务与队伍快捷内容共用左侧同一区域。</summary>
public sealed class QuickPanelWidget : MonoBehaviour
{
    /// <summary>销毁主界面时释放本控件添加的按钮监听。</summary>
    private void OnDestroy()
    {
        _taskButton.onClick.RemoveListener(ShowTaskPanel);
        _teamButton.onClick.RemoveListener(ShowTeamPanel);
    }

    /// <summary>RO 原任务页签按钮。</summary>
    [SerializeField] private Button _taskButton;

    /// <summary>RO 原队伍页签按钮。</summary>
    [SerializeField] private Button _teamButton;

    /// <summary>既有任务内容，保留任务/玩法/奇遇子页签。</summary>
    [SerializeField] private GameObject _taskContent;

    /// <summary>既有队伍内容，展示创建入口或本人成员行。</summary>
    [SerializeField] private GameObject _teamContent;

    /// <summary>任务页签选中图形。</summary>
    [SerializeField] private GameObject _taskOn;

    /// <summary>任务页签未选中图形。</summary>
    [SerializeField] private GameObject _taskOff;

    /// <summary>队伍页签选中图形。</summary>
    [SerializeField] private GameObject _teamOn;

    /// <summary>队伍页签未选中图形。</summary>
    [SerializeField] private GameObject _teamOff;

    /// <summary>快捷任务面板控件，折叠期间抑制任务/队伍内容切换，左侧只保留折叠按钮。</summary>
    [SerializeField] private QuickTaskPanelWidget _taskPanelWidget;

    /// <summary>当前是否展示队伍内容，供快捷任务面板展开折叠后按原状态恢复。</summary>
    private bool _showTeam;

    /// <summary>由 MainView 绑定页签，沿 RO 默认显示任务内容。</summary>
    public void InitWidget()
    {
        _taskButton.onClick.RemoveListener(ShowTaskPanel);
        _taskButton.onClick.AddListener(ShowTaskPanel);
        _teamButton.onClick.RemoveListener(ShowTeamPanel);
        _teamButton.onClick.AddListener(ShowTeamPanel);
        ShowTaskPanel();
    }

    /// <summary>显示任务内容，不修改任务或队伍数据。</summary>
    public void ShowTaskPanel()
    {
        ShowPanel(false);
    }

    /// <summary>显示队伍内容，建队成功后也通过此入口切换。</summary>
    public void ShowTeamPanel()
    {
        ShowPanel(true);
    }

    /// <summary>同步内容显隐及页签图形，始终只展开一套快捷内容；折叠期间任务容器保留以容纳折叠按钮，队伍内容不弹出。</summary>
    /// <param name="showTeam">true 选择队伍，false 选择任务。</param>
    private void ShowPanel(bool showTeam)
    {
        _showTeam = showTeam;
        // 折叠期间左侧只保留折叠按钮：任务容器保持激活（其内部子节点已由折叠逻辑隐藏），队伍内容保持收起。
        bool folded = _taskPanelWidget != null && _taskPanelWidget.IsFolded;
        _taskContent.SetActive(folded || showTeam == false);
        _teamContent.SetActive(showTeam && folded == false);
        _taskOn.SetActive(showTeam == false);
        _taskOff.SetActive(showTeam);
        _teamOn.SetActive(showTeam);
        _teamOff.SetActive(showTeam == false);
    }

    /// <summary>按最近一次页签状态重新应用任务/队伍内容显隐，供快捷任务面板展开折叠后恢复。</summary>
    public void ApplyPanelState()
    {
        ShowPanel(_showTeam);
    }
}
