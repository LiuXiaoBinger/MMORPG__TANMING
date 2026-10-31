using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面左侧常驻快捷队伍栏控件，绑定从只读 RO 工程 QuickTeamPanel.prefab 原样移植的预制体。
/// 按服务器下发的成员表逐行渲染 RO 原成员行，并把「组队」「创建队伍」「寻找队伍」等点击交给控制器。
/// </summary>
public sealed class QuickTeamWidget : MonoBehaviour
{
    /// <summary>未建队分支根节点，对应 RO 的 NotInTeamPanel，内含「创建队伍」「寻找队伍」两行。</summary>
    [SerializeField, Header("未建队分支")] private GameObject _notInTeamRoot;

    /// <summary>已建队分支根节点，对应 RO 的 InTeamPanel，内含成员行与邀请空位行。</summary>
    [SerializeField, Header("已建队分支")] private GameObject _inTeamRoot;

    /// <summary>成员行容器，纵向排布成员行，邀请空位行排在最后。</summary>
    [SerializeField, Header("成员行容器")] private Transform _memberListRoot;

    /// <summary>成员行模板，同时作为第一行成员使用。</summary>
    [SerializeField, Header("成员行模板")] private GameObject _memberSlotTemplate;

    /// <summary>成员行末尾的邀请空位行，未满员且本人是队长时显示。</summary>
    [SerializeField, Header("邀请空位行")] private GameObject _addSlot;

    /// <summary>成员名字文本，取当前真实角色昵称。</summary>
    [SerializeField, Header("成员名")] private TMP_Text _memberName;

    /// <summary>成员等级文本，取当前真实角色等级。</summary>
    [SerializeField, Header("成员等级")] private TMP_Text _memberLevel;

    /// <summary>队长标记，只有队长所在行显示。</summary>
    [SerializeField, Header("队长标记")] private GameObject _captainMark;

    /// <summary>职业头像，按成员职业切换已有图集精灵。</summary>
    [SerializeField, Header("职业头像")] private Image _professionImage;

    /// <summary>生命条，按成员血量比例设置滑条值。</summary>
    [SerializeField, Header("生命条")] private Slider _hpSlider;

    /// <summary>法力条，按成员法力比例设置滑条值。</summary>
    [SerializeField, Header("法力条")] private Slider _mpSlider;

    /// <summary>右侧「组队」按钮，点击打开 RO 正式队伍窗口。</summary>
    [SerializeField, Header("组队按钮")] private Button _groupButton;

    /// <summary>右侧召集按钮，只打开原版招募界面壳，不发送联网招募。</summary>
    [SerializeField, Header("召集按钮")] private Button _summonButton;

    /// <summary>右侧援团按钮属于佣兵系统，保留原位置，本轮不执行该系统业务。</summary>
    [SerializeField, Header("援团按钮")] private Button _assistButton;

    /// <summary>未建队分支里的「创建队伍」行，沿 RO 入口直接请求创建队伍。</summary>
    [SerializeField, Header("创建队伍行")] private Button _createButton;

    /// <summary>未建队分支里的寻找队伍行，打开原版寻找界面壳。</summary>
    [SerializeField, Header("寻找队伍行")] private Button _searchButton;

    /// <summary>原版邀请空位按钮，打开邀请界面壳。</summary>
    [SerializeField] private Button _inviteButton;
    /// <summary>原版本人条目按钮，打开队员详情壳。</summary>
    [SerializeField] private Button _memberButton;
    /// <summary>本人条目右侧箭头，打开成员快捷菜单壳。</summary>
    [SerializeField] private Button _memberMenuButton;

    /// <summary>成员行上的名字路径，相对成员行根节点。</summary>
    private const string SlotNamePath = "Name";
    /// <summary>成员行上的等级路径，相对成员行根节点。</summary>
    private const string SlotLevelPath = "Level";
    /// <summary>成员行上的职业头像路径，相对成员行根节点。</summary>
    private const string SlotJobPath = "ProfessionImage";
    /// <summary>成员行上的生命条路径，相对成员行根节点。</summary>
    private const string SlotHpPath = "SliderHP";
    /// <summary>成员行上的法力条路径，相对成员行根节点。</summary>
    private const string SlotMpPath = "SliderMP";
    /// <summary>成员行上的队长标记路径，相对成员行根节点。</summary>
    private const string SlotCaptainPath = "ImageCaptain";

    /// <summary>运行时复制出来的成员行，下标与服务器成员顺序一致。</summary>
    private readonly List<GameObject> _memberSlots = new List<GameObject>();

    /// <summary>点击「组队」时触发，由队伍控制器打开正式队伍窗。</summary>
    public System.Action OpenTeamRequested;

    /// <summary>点击快捷创建入口时触发，由控制器向服务器请求建队。</summary>
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
        SetTeamState(false, null, 0UL, 0f, 0f);
        _inited = true;
    }

    /// <summary>
    /// 刷新快捷栏展示：有队伍时按服务器成员表逐行渲染 RO 成员行，未建队时显示 RO 的创建/寻找队伍分支。
    /// </summary>
    /// <param name="hasTeam">当前是否已经在队伍中。</param>
    /// <param name="members">服务器下发的成员列表，未建队时传空。</param>
    /// <param name="localRoleId">本地主角角色ID，用于定位本人行。</param>
    /// <param name="hpRatio">本人生命比例，0 到 1。</param>
    /// <param name="mpRatio">本人法力比例，0 到 1。</param>
    public void SetTeamState(bool hasTeam, IList<TeamMemberInfo> members, ulong localRoleId,
        float hpRatio, float mpRatio)
    {
        if (_inTeamRoot != null)
        {
            _inTeamRoot.SetActive(hasTeam);
        }
        if (_notInTeamRoot != null)
        {
            _notInTeamRoot.SetActive(!hasTeam);
        }
        // 本次需要显示的成员数量，成员表为空时为零。
        int count = 0;
        if (members != null)
        {
            count = members.Count;
        }
        if (!hasTeam)
        {
            count = 0;
        }
        EnsureMemberSlots(count);
        // index 为成员行列表零基下标，既定位行控件也作为成员表下标使用。
        for (int index = 0; index < _memberSlots.Count; index++)
        {
            // 当前成员行，超出成员数量时隐藏。
            GameObject slot = _memberSlots[index];
            if (slot == null)
            {
                continue;
            }
            if (index >= count)
            {
                slot.SetActive(false);
                continue;
            }
            slot.SetActive(true);
            ApplyMemberToSlot(slot, members[index], localRoleId, index, hpRatio, mpRatio);
        }
        if (_addSlot != null)
        {
            _addSlot.SetActive(hasTeam && count < ResolveMaxMember());
            // 邀请空位始终排在所有成员行之后，保持原版从上到下的排列顺序。
            _addSlot.transform.SetAsLastSibling();
        }
    }

    /// <summary>取服务器下发的队伍人数上限，未收到时按 RO 缺省的 5 人处理。</summary>
    /// <returns>队伍人数上限。</returns>
    private static int ResolveMaxMember()
    {
        // 服务器未下发上限时按 5 人处理，与服务端 TeamManager.MaxMember 保持一致。
        int maxMember = TeamData.Instance.MaxMember;
        if (maxMember <= 0)
        {
            return 5;
        }
        return maxMember;
    }

    /// <summary>按需补齐成员行数量，复制模板并插到邀请空位之前。</summary>
    /// <param name="count">本次需要显示的成员数量。</param>
    private void EnsureMemberSlots(int count)
    {
        if (_memberSlotTemplate == null)
        {
            return;
        }
        // 第一行成员就是预制体里的原实例，先登记再按需复制。
        if (_memberSlots.Count == 0)
        {
            _memberSlots.Add(_memberSlotTemplate);
        }
        while (_memberSlots.Count < count)
        {
            // 复制出来的成员行实例，命名带当前数量后缀便于层级里区分，随后登记进列表复用。
            GameObject copy = Instantiate(_memberSlotTemplate, _memberListRoot);
            copy.name = _memberSlotTemplate.name + "_" + _memberSlots.Count;
            _memberSlots.Add(copy);
        }
    }

    /// <summary>把单个成员信息写入指定成员行。</summary>
    /// <param name="slot">目标成员行实例。</param>
    /// <param name="member">服务器下发的成员信息。</param>
    /// <param name="localRoleId">本地主角角色ID。</param>
    /// <param name="index">成员在列表中的下标，0 表示第一行。</param>
    /// <param name="hpRatio">本人生命比例，非本人行按满值显示。</param>
    /// <param name="mpRatio">本人法力比例，非本人行按满值显示。</param>
    private void ApplyMemberToSlot(GameObject slot, TeamMemberInfo member, ulong localRoleId,
        int index, float hpRatio, float mpRatio)
    {
        if (member == null)
        {
            return;
        }
        // 第一行沿用预制体已序列化的引用，其余行按固定路径取同结构控件。
        TMP_Text nameText = _memberName;
        TMP_Text levelText = _memberLevel;
        Image jobImage = _professionImage;
        Slider hpSlider = _hpSlider;
        Slider mpSlider = _mpSlider;
        GameObject captainMark = _captainMark;
        if (index > 0)
        {
            nameText = FindChildComponent<TMP_Text>(slot.transform, SlotNamePath);
            levelText = FindChildComponent<TMP_Text>(slot.transform, SlotLevelPath);
            jobImage = FindChildComponent<Image>(slot.transform, SlotJobPath);
            hpSlider = FindChildComponent<Slider>(slot.transform, SlotHpPath);
            mpSlider = FindChildComponent<Slider>(slot.transform, SlotMpPath);
            captainMark = FindChildObject(slot.transform, SlotCaptainPath);
        }
        if (nameText != null)
        {
            nameText.text = member.Nickname;
        }
        if (levelText != null)
        {
            levelText.text = member.Level.ToString();
        }
        if (captainMark != null)
        {
            captainMark.SetActive(member.IsCaptain);
        }
        // 本人行显示真实血蓝比例，其他成员行暂按满值显示。
        if (localRoleId != 0UL && member.RoleId == localRoleId)
        {
            SetSliderValue(hpSlider, hpRatio);
            SetSliderValue(mpSlider, mpRatio);
        }
        else
        {
            SetSliderValue(hpSlider, 1f);
            SetSliderValue(mpSlider, 1f);
        }
        if (jobImage != null && localRoleId != 0UL && member.RoleId == localRoleId)
        {
            jobImage.enabled = true;
        }
    }

    /// <summary>给指定成员行设置职业头像，索引无效时跳过。</summary>
    /// <param name="index">成员行下标。</param>
    /// <param name="jobSprite">职业头像精灵，为空时不做修改。</param>
    public void SetMemberJobSprite(int index, Sprite jobSprite)
    {
        if (jobSprite == null || index < 0 || index >= _memberSlots.Count)
        {
            return;
        }
        // 第一行成员直接用预制体序列化的头像引用。
        Image target = _professionImage;
        if (index > 0)
        {
            target = FindChildComponent<Image>(_memberSlots[index].transform, SlotJobPath);
        }
        if (target != null)
        {
            target.sprite = jobSprite;
        }
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

    /// <summary>按固定相对路径查找子节点上的指定组件，找不到时返回空。</summary>
    /// <typeparam name="T">期望的组件类型。</typeparam>
    /// <param name="root">查找起点。</param>
    /// <param name="path">相对路径。</param>
    /// <returns>找到的组件；路径不存在时返回 null。</returns>
    private static T FindChildComponent<T>(Transform root, string path) where T : Component
    {
        // 按相对路径找到的子节点，找不到时由调用方按空值处理。
        Transform child = root.Find(path);
        if (child == null)
        {
            return null;
        }
        return child.GetComponent<T>();
    }

    /// <summary>按固定相对路径查找子节点，找不到时返回空。</summary>
    /// <param name="root">查找起点。</param>
    /// <param name="path">相对路径。</param>
    /// <returns>找到的节点；路径不存在时返回 null。</returns>
    private static GameObject FindChildObject(Transform root, string path)
    {
        // 按相对路径找到的子节点，找不到时由调用方按空值处理。
        Transform child = root.Find(path);
        if (child == null)
        {
            return null;
        }
        return child.gameObject;
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
    /// <summary>召集按钮进入已有的邀请玩家页，不再复用收到邀请的确认页，避免无邀请时弹出空邀请框。</summary>
    private void OnSummonClicked() { OpenPageRequested?.Invoke(TeamPage.Invite); }
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
