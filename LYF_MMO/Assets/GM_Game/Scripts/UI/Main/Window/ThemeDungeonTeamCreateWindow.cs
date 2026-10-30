using UnityEngine;
using UnityEngine.UI;

/// <summary>展示 RO 队伍主窗及关联界面；只处理展示和切换，建队业务交给控制器。</summary>
public sealed class ThemeDungeonTeamCreateWindow : WindowBase
{
    /// <summary>未建队时显示的创建按钮。</summary>
    [SerializeField] private GameObject _notInTeamRoot;
    /// <summary>正式成员卡，建队后显示当前玩家。</summary>
    [SerializeField] private GameObject _memberRoot;
    /// <summary>成员卡上的真实昵称。</summary>
    [SerializeField] private Text _memberName;
    /// <summary>成员卡上的真实等级。</summary>
    [SerializeField] private Text _memberLevel;
    /// <summary>成员卡上的队长标记。</summary>
    [SerializeField] private GameObject _captainMark;
    /// <summary>成员卡原有的职业图标。</summary>
    [SerializeField] private Image _professionImage;
    /// <summary>随当前玩家昵称更新的队伍名称。</summary>
    [SerializeField] private Text _teamName;
    /// <summary>从副本详情选择的队伍目标。</summary>
    [SerializeField] private Text _targetName;
    /// <summary>原版未建队空态，属于主窗内容。</summary>
    [SerializeField] private GameObject _emptyState;
    /// <summary>创建本地单人队伍的按钮。</summary>
    [SerializeField] private Button _createButton;
    /// <summary>正式队伍主窗关闭按钮。</summary>
    [SerializeField] private Button _closeButton;
    /// <summary>原版离队按钮，未建队时不可操作。</summary>
    [SerializeField] private Button _leaveButton;
    /// <summary>按 TeamPage 编号保存的界面引用，全部来自预制体。</summary>
    [SerializeField] private GameObject[] _pages;
    /// <summary>主窗外框，独立子窗口显示时收起。</summary>
    [SerializeField] private GameObject _mainContent;
    /// <summary>创建队伍请求，由控制器处理。</summary>
    public System.Action CreateRequested;
    /// <summary>关闭整个队伍窗口的请求。</summary>
    public System.Action CloseRequested;
    /// <summary>离开本地单人队伍，由控制器清除队伍状态。</summary>
    public System.Action LeaveRequested;
    /// <summary>当前显示的界面。</summary>
    private TeamPage _currentPage;
    /// <summary>进入子窗口前的界面，仅用于关闭后返回。</summary>
    private TeamPage _returnPage;
    /// <summary>本地队伍状态，仅控制成员卡与空态展示。</summary>
    private bool _teamCreated;

    /// <summary>绑定普通按钮事件，初始化为未建队的主界面。</summary>
    public override void InitWindow()
    {
        base.InitWindow();
        _createButton.onClick.RemoveListener(OnCreateClicked);
        _createButton.onClick.AddListener(OnCreateClicked);
        _closeButton.onClick.RemoveListener(OnCloseClicked);
        _closeButton.onClick.AddListener(OnCloseClicked);
        SetTeamCreated(false, string.Empty, 0);
        OpenPage(TeamPage.Members);
    }

    /// <summary>刷新正式成员卡，不使用源预制体中的示例角色数据。</summary>
    /// <param name="created">是否已创建本地单人队伍。</param>
    /// <param name="roleName">当前玩家真实昵称，未建队时为空。</param>
    /// <param name="roleLevel">当前玩家真实等级，未建队时为零。</param>
    public void SetTeamCreated(bool created, string roleName, int roleLevel)
    {
        _teamCreated = created;
        _notInTeamRoot.SetActive(!created);
        _memberRoot.SetActive(created);
        _emptyState.SetActive(!created && _currentPage == TeamPage.Members);
        _memberName.text = roleName;
        _memberLevel.text = "Lv." + roleLevel;
        _captainMark.SetActive(created);
        _leaveButton.interactable = created;
        if (created)
        {
            _teamName.text = roleName + "的队伍";
        }
        else
        {
            _teamName.text = "我的队伍";
        }
    }

    /// <summary>更新从副本详情带入的目标，未选择时显示选择提示。</summary>
    /// <param name="targetName">当前副本名称，空字符串表示未选择。</param>
    public void SetTargetName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName))
        {
            _targetName.text = "目标：请选择副本";
            return;
        }
        _targetName.text = "目标：" + targetName;
    }

    /// <summary>把当前职业头像填入原有图标，不新增成员卡控件。</summary>
    /// <param name="jobSprite">现有职业图集中的精灵。</param>
    public void SetProfessionSprite(Sprite jobSprite)
    {
        if (jobSprite != null)
        {
            _professionImage.sprite = jobSprite;
        }
    }

    /// <summary>Unity 持久按钮事件的整数参数边界，转换为具名界面编号。</summary>
    /// <param name="pageIndex">预制体保存的 TeamPage 编号。</param>
    public void ShowPage(int pageIndex)
    {
        OpenPage((TeamPage)pageIndex);
    }

    /// <summary>显示指定界面；独立窗口保持原始布局，不拉伸到成员内容框。</summary>
    /// <param name="page">本次打开的队伍界面。</param>
    public void OpenPage(TeamPage page)
    {
        _returnPage = _currentPage;
        _currentPage = page;
        // 成员和申请列表共用原队伍主窗外框，其余界面使用自身原外框。
        bool showMainFrame = page == TeamPage.Members || page == TeamPage.Applications;
        _mainContent.SetActive(showMainFrame);
        // 数组与具名界面编号一致，只切换显示，不创建控件。
        for (int pageIndex = 0; pageIndex < _pages.Length; pageIndex++)
        {
            if (_pages[pageIndex] != null)
            {
                _pages[pageIndex].SetActive(pageIndex == (int)page);
            }
        }
        _emptyState.SetActive(!_teamCreated && page == TeamPage.Members);
    }

    /// <summary>关闭当前子窗口，返回打开它的界面；下一次关闭返回主窗。</summary>
    public void ClosePage()
    {
        // 保存返回目标，OpenPage 会更新当前层来源。
        TeamPage target = _returnPage;
        OpenPage(target);
        _returnPage = TeamPage.Members;
    }

    /// <summary>原有返回按钮的兼容入口，回到队伍主界面。</summary>
    public void HidePages()
    {
        OpenPage(TeamPage.Members);
        _returnPage = TeamPage.Members;
    }

    /// <summary>把创建请求交给控制器。</summary>
    private void OnCreateClicked()
    {
        CreateRequested?.Invoke();
    }

    /// <summary>寻找界面的创建入口，提交请求后回到正式队伍主窗。</summary>
    public void RequestCreateTeam()
    {
        CreateRequested?.Invoke();
        HidePages();
    }

    /// <summary>原版离队按钮只请求控制器退出本地队伍。</summary>
    public void RequestLeaveTeam()
    {
        LeaveRequested?.Invoke();
    }

    /// <summary>把关闭请求交给控制器。</summary>
    private void OnCloseClicked()
    {
        CloseRequested?.Invoke();
    }

    /// <summary>销毁时解除本窗口添加的普通按钮监听。</summary>
    private void OnDestroy()
    {
        if (_createButton != null)
        {
            _createButton.onClick.RemoveListener(OnCreateClicked);
        }
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
