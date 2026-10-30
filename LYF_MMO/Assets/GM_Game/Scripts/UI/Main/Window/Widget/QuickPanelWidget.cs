using UnityEngine;
using UnityEngine.UI;

/// <summary>沿用 RO QuickPanel 页签，让任务与队伍快捷内容共用左侧同一区域。</summary>
public sealed class QuickPanelWidget : MonoBehaviour
{
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

    /// <summary>同步内容显隐及页签图形，始终只展开一套快捷内容。</summary>
    /// <param name="showTeam">true 选择队伍，false 选择任务。</param>
    private void ShowPanel(bool showTeam)
    {
        _taskContent.SetActive(!showTeam);
        _teamContent.SetActive(showTeam);
        _taskOn.SetActive(!showTeam);
        _taskOff.SetActive(showTeam);
        _teamOn.SetActive(showTeam);
        _teamOff.SetActive(!showTeam);
    }

    /// <summary>销毁主界面时释放本控件添加的按钮监听。</summary>
    private void OnDestroy()
    {
        _taskButton.onClick.RemoveListener(ShowTaskPanel);
        _teamButton.onClick.RemoveListener(ShowTeamPanel);
    }
}
