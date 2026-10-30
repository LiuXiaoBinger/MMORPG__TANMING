using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面左侧常驻快捷队伍栏控件，绑定从只读 RO 工程 QuickTeamPanel.prefab 原样移植的预制体。
/// 只负责把本地单人队伍的真实角色数据填进 RO 的成员行，并把「组队」「创建队伍」点击交给控制器；
/// 寻找、邀请与召集只打开对应界面壳，具体联网业务尚未接入。
/// </summary>
public sealed class QuickTeamWidget : MonoBehaviour
{
    /// <summary>未建队分支根节点，对应 RO 的 NotInTeamPanel，内含「创建队伍」「寻找队伍」两行。</summary>
    [SerializeField, Header("未建队分支")] private GameObject _notInTeamRoot;

    /// <summary>已建队分支根节点，对应 RO 的 InTeamPanel，内含成员行与邀请空位行。</summary>
    [SerializeField, Header("已建队分支")] private GameObject _inTeamRoot;

    /// <summary>成员名字文本，取当前真实角色昵称。</summary>
    [SerializeField, Header("成员名")] private TMP_Text _memberName;

    /// <summary>成员等级文本，取当前真实角色等级。</summary>
    [SerializeField, Header("成员等级")] private TMP_Text _memberLevel;

    /// <summary>队长标记，本地单人队伍中本人即队长。</summary>
    [SerializeField, Header("队长标记")] private GameObject _captainMark;

    /// <summary>职业头像，按当前角色职业切换已有图集精灵。</summary>
    [SerializeField, Header("职业头像")] private Image _professionImage;

    /// <summary>生命条，按当前角色血量比例设置滑条值。</summary>
    [SerializeField, Header("生命条")] private Slider _hpSlider;

    /// <summary>法力条，按当前角色法力比例设置滑条值。</summary>
    [SerializeField, Header("法力条")] private Slider _mpSlider;

    /// <summary>右侧「组队」按钮，点击打开 RO 正式队伍窗口。</summary>
    [SerializeField, Header("组队按钮")] private Button _groupButton;

    /// <summary>右侧召集按钮，只打开原版招募界面壳，不发送联网招募。</summary>
    [SerializeField, Header("召集按钮")] private Button _summonButton;

    /// <summary>右侧援团按钮属于佣兵系统，保留原位置，本轮不执行该系统业务。</summary>
    [SerializeField, Header("援团按钮")] private Button _assistButton;

    /// <summary>未建队分支里的「创建队伍」行，沿 RO 入口直接请求创建本地单人队伍。</summary>
    [SerializeField, Header("创建队伍行")] private Button _createButton;

    /// <summary>未建队分支里的寻找队伍行，打开原版寻找界面壳。</summary>
    [SerializeField, Header("寻找队伍行")] private Button _searchButton;

    /// <summary>原版邀请空位按钮，打开邀请界面壳。</summary>
    [SerializeField] private Button _inviteButton;
    /// <summary>原版本人条目按钮，打开队员详情壳。</summary>
    [SerializeField] private Button _memberButton;
    /// <summary>本人条目右侧箭头，打开成员快捷菜单壳。</summary>
    [SerializeField] private Button _memberMenuButton;

    /// <summary>点击「组队」时触发，由队伍控制器打开正式队伍窗。</summary>
    public System.Action OpenTeamRequested;

    /// <summary>点击快捷创建入口时触发，由控制器把本人加入本地单人队伍。</summary>
    public System.Action CreateTeamRequested;

    /// <summary>请求打开已有的 RO 队伍关联界面，不执行联网操作。</summary>
    public System.Action<TeamPage> OpenPageRequested;

    /// <summary>是否已完成控件绑定，避免重复初始化叠加监听。</summary>
    private bool _inited;

    /// <summary>绑定按钮监听并把快捷栏复位为未建队状态。</summary>
    public void InitWidget()
    {
        if (_inited)
        {
            RemoveListeners();
        }
        if (_groupButton != null)
        {
            _groupButton.onClick.AddListener(OnOpenTeamClicked);
        }
        if (_createButton != null)
        {
            _createButton.onClick.AddListener(OnCreateTeamClicked);
        }
        _searchButton.onClick.AddListener(OnSearchClicked);
        _summonButton.onClick.AddListener(OnSummonClicked);
        _inviteButton.onClick.AddListener(OnInviteClicked);
        _memberButton.onClick.AddListener(OnMemberClicked);
        _memberMenuButton.onClick.AddListener(OnMemberMenuClicked);
        // 援团属于佣兵系统，本轮只迁移队伍分支，保持原位且不执行该系统业务。
        SetInteractable(_summonButton, true);
        SetInteractable(_assistButton, false);
        SetInteractable(_searchButton, true);
        SetInteractable(_inviteButton, true);
        SetTeamState(false, string.Empty, 0, 0f, 0f);
        _inited = true;
    }

    /// <summary>
    /// 刷新快捷栏展示：有队伍时显示 RO 成员行与邀请空位，未建队时显示 RO 的创建/寻找队伍分支。
    /// </summary>
    /// <param name="hasTeam">当前是否已经建立本地单人队伍。</param>
    /// <param name="roleName">当前真实角色昵称，未建队时传空。</param>
    /// <param name="roleLevel">当前真实角色等级，未建队时传 0。</param>
    /// <param name="hpRatio">生命比例，0 到 1。</param>
    /// <param name="mpRatio">法力比例，0 到 1。</param>
    public void SetTeamState(bool hasTeam, string roleName, int roleLevel, float hpRatio,
        float mpRatio)
    {
        if (_inTeamRoot != null)
        {
            _inTeamRoot.SetActive(hasTeam);
        }
        if (_notInTeamRoot != null)
        {
            _notInTeamRoot.SetActive(!hasTeam);
        }
        if (_memberName != null)
        {
            _memberName.text = roleName;
        }
        if (_memberLevel != null)
        {
            _memberLevel.text = roleLevel.ToString();
        }
        if (_captainMark != null)
        {
            _captainMark.SetActive(hasTeam);
        }
        SetSliderValue(_hpSlider, hpRatio);
        SetSliderValue(_mpSlider, mpRatio);
    }

    /// <summary>按当前角色职业切换头像，找不到对应精灵时保留预制体默认图。</summary>
    /// <param name="jobSprite">职业头像精灵，为空时不做修改。</param>
    public void SetProfessionSprite(Sprite jobSprite)
    {
        if (_professionImage == null || jobSprite == null)
        {
            return;
        }
        _professionImage.sprite = jobSprite;
    }

    /// <summary>设置按钮是否可点击，节点为空时直接跳过。</summary>
    /// <param name="button">目标按钮。</param>
    /// <param name="interactable">true 可点，false 置灰不可点。</param>
    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    /// <summary>按比例设置滑条值，比例会被夹到 0 到 1，滑条为空时跳过。</summary>
    /// <param name="slider">目标滑条。</param>
    /// <param name="ratio">目标比例，0 到 1。</param>
    private static void SetSliderValue(Slider slider, float ratio)
    {
        if (slider == null)
        {
            return;
        }
        // 滑条使用零到一的无量纲比例。
        float clamped = ratio;
        if (clamped < 0f)
        {
            clamped = 0f;
        }
        if (clamped > 1f)
        {
            clamped = 1f;
        }
        slider.value = clamped;
    }

    /// <summary>组队按钮请求打开正式队伍窗口。</summary>
    private void OnOpenTeamClicked()
    {
        if (OpenTeamRequested != null)
        {
            OpenTeamRequested();
        }
    }

    /// <summary>把快捷创建请求交给控制器，控件不保存业务状态。</summary>
    private void OnCreateTeamClicked()
    {
        CreateTeamRequested?.Invoke();
    }

    /// <summary>原预制体嵌套按钮的整数事件边界，只转发具名界面请求。</summary>
    /// <param name="pageIndex">预制体保存的 TeamPage 编号。</param>
    public void ShowPage(int pageIndex)
    {
        OpenPageRequested?.Invoke((TeamPage)pageIndex);
    }

    /// <summary>打开寻找队伍界面壳。</summary>
    private void OnSearchClicked() { OpenPageRequested?.Invoke(TeamPage.Search); }
    /// <summary>打开邀请玩家界面壳。</summary>
    private void OnInviteClicked() { OpenPageRequested?.Invoke(TeamPage.Invite); }
    /// <summary>打开招募界面壳，保留原召集按钮。</summary>
    private void OnSummonClicked() { OpenPageRequested?.Invoke(TeamPage.Recruitment); }
    /// <summary>打开本人详情界面壳。</summary>
    private void OnMemberClicked() { OpenPageRequested?.Invoke(TeamPage.MemberDetails); }
    /// <summary>打开原成员快捷菜单。</summary>
    private void OnMemberMenuClicked() { OpenPageRequested?.Invoke(TeamPage.MemberActions); }

    /// <summary>移除已绑定的按钮监听，供重复初始化与销毁复用。</summary>
    private void RemoveListeners()
    {
        if (_groupButton != null)
        {
            _groupButton.onClick.RemoveListener(OnOpenTeamClicked);
        }
        if (_createButton != null)
        {
            _createButton.onClick.RemoveListener(OnCreateTeamClicked);
        }
        if (_searchButton != null) _searchButton.onClick.RemoveListener(OnSearchClicked);
        if (_summonButton != null) _summonButton.onClick.RemoveListener(OnSummonClicked);
        if (_inviteButton != null) _inviteButton.onClick.RemoveListener(OnInviteClicked);
        if (_memberButton != null) _memberButton.onClick.RemoveListener(OnMemberClicked);
        if (_memberMenuButton != null) _memberMenuButton.onClick.RemoveListener(OnMemberMenuClicked);
    }

    /// <summary>销毁时注销监听，避免控件对象残留回调。</summary>
    private void OnDestroy()
    {
        RemoveListeners();
    }
}
