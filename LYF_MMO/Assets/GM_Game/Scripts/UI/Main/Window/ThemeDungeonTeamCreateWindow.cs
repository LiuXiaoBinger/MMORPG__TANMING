using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 展示 RO 队伍主窗及关联界面；只处理展示与控件事件转发，队伍业务交给控制器。
/// 成员卡沿用预制体原有的 RO 成员卡实例，按服务器下发的成员数量复制，不新建控件节点。
/// </summary>
public sealed class ThemeDungeonTeamCreateWindow : WindowBase
{
    /// <summary>未建队时显示的创建按钮。</summary>
    [SerializeField] private GameObject _notInTeamRoot;
    /// <summary>成员卡容器，成员卡与邀请空位都是它的子节点。</summary>
    [SerializeField] private GameObject _memberRoot;
    /// <summary>成员卡模板，同时作为第一张成员卡使用。</summary>
    [SerializeField] private GameObject _memberSlotTemplate;
    /// <summary>成员卡末尾的邀请空位，未满员且本人是队长时显示。</summary>
    [SerializeField] private GameObject _inviteSlot;
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
    /// <summary>创建队伍的按钮。</summary>
    [SerializeField] private Button _createButton;
    /// <summary>正式队伍主窗关闭按钮。</summary>
    [SerializeField] private Button _closeButton;
    /// <summary>原版离队按钮，未建队时不可操作。</summary>
    [SerializeField] private Button _leaveButton;
    /// <summary>按 TeamPage 编号保存的界面引用，全部来自预制体。</summary>
    [SerializeField] private GameObject[] _pages;
    /// <summary>主窗外框，独立子窗口显示时收起。</summary>
    [SerializeField] private GameObject _mainContent;

    /// <summary>邀请玩家页的目标输入框，输入被邀请角色ID。</summary>
    [SerializeField] private InputField _inviteInput;
    /// <summary>邀请玩家页的确认按钮，提交邀请请求。</summary>
    [SerializeField] private Button _inviteConfirmButton;
    /// <summary>邀请玩家页的候选人行，显示当前可邀请对象。</summary>
    [SerializeField] private GameObject _inviteRowRoot;
    /// <summary>候选人行上的昵称文本。</summary>
    [SerializeField] private Text _inviteRowName;
    /// <summary>候选人行上的邀请按钮。</summary>
    [SerializeField] private Button _inviteRowButton;

    /// <summary>寻找队伍页的队伍ID输入框，输入要申请加入的队伍ID。</summary>
    [SerializeField] private InputField _searchTeamIdInput;

    /// <summary>寻找队伍页的申请行，显示当前待申请队伍。</summary>
    [SerializeField] private GameObject _searchRowRoot;
    /// <summary>寻找队伍页的申请按钮。</summary>
    [SerializeField] private Button _searchApplyButton;

    /// <summary>申请列表页的申请行，显示当前申请人。</summary>
    [SerializeField] private GameObject _applyRowRoot;
    /// <summary>申请行上的申请人昵称。</summary>
    [SerializeField] private Text _applyName;
    /// <summary>申请行上的同意入队按钮。</summary>
    [SerializeField] private Button _applyAgreeButton;
    /// <summary>申请行上的忽略按钮，用于拒绝申请。</summary>
    [SerializeField] private Button _applyRejectButton;

    /// <summary>队伍邀请页，服务器把邀请下发给本人时显示。</summary>
    [SerializeField] private GameObject _offerRoot;
    /// <summary>邀请页上的邀请人昵称。</summary>
    [SerializeField] private Text _offerInviterName;
    /// <summary>邀请页上的说明文本。</summary>
    [SerializeField] private Text _offerInfo;
    /// <summary>邀请页上的队伍人数文本。</summary>
    [SerializeField] private Text _offerMemberCount;
    /// <summary>邀请页上的目标文本。</summary>
    [SerializeField] private Text _offerTarget;
    /// <summary>邀请页上的加入按钮，等价同意邀请。</summary>
    [SerializeField] private Button _offerAgreeButton;
    /// <summary>邀请页上的关闭按钮，等价拒绝邀请。</summary>
    [SerializeField] private Button _offerRejectButton;

    /// <summary>创建队伍请求，由控制器处理。</summary>
    public System.Action CreateRequested;
    /// <summary>关闭整个队伍窗口的请求。</summary>
    public System.Action CloseRequested;
    /// <summary>离开队伍，由控制器清除服务器队伍关系。</summary>
    public System.Action LeaveRequested;
    /// <summary>邀请指定角色入队，参数为被邀请角色ID。</summary>
    public System.Action<ulong> InviteRoleRequested;
    /// <summary>申请加入指定队伍，参数为目标队伍ID。</summary>
    public System.Action<ulong> ApplyTeamRequested;
    /// <summary>同意收到的入队邀请。</summary>
    public System.Action InviteAgreed;
    /// <summary>拒绝收到的入队邀请。</summary>
    public System.Action InviteRejected;
    /// <summary>同意收到的入队申请。</summary>
    public System.Action ApplicationAgreed;
    /// <summary>拒绝收到的入队申请。</summary>
    public System.Action ApplicationRejected;
    /// <summary>打开或切换界面后请求控制器刷新数据，用于重建附近邀请候选行。</summary>
    public System.Action PageOpened;

    /// <summary>成员卡上的昵称路径，相对成员卡根节点。</summary>
    private const string SlotNamePath = "BG2/GameObject/PlayerName";
    /// <summary>成员卡上的等级路径，相对成员卡根节点。</summary>
    private const string SlotLevelPath = "BG2/Lv";
    /// <summary>成员卡上的队长标记路径，相对成员卡根节点。</summary>
    private const string SlotCaptainPath = "BG2/Flag/LeadFlag";
    /// <summary>成员卡上的职业图标路径，相对成员卡根节点。</summary>
    private const string SlotJobPath = "BG2/GameObject/Img_Job";

    /// <summary>候选人行上的昵称路径，相对候选人行根节点，与 RO TeamInviteCtrl 的 NameInfo/Name 一致。</summary>
    private const string InviteRowNamePath = "NameInfo/Name";
    /// <summary>候选人行上的等级路径，相对候选人行根节点，与 RO TeamInviteCtrl 的 Lv 一致。</summary>
    private const string InviteRowLevelPath = "Lv";
    /// <summary>候选人行上的邀请按钮路径，相对候选人行根节点，与 RO TeamInviteCtrl 的 BtnSmall_B 一致。</summary>
    private const string InviteRowButtonPath = "BtnSmall_B";

    /// <summary>运行时复制出来的成员卡，下标与服务器成员顺序一致。</summary>
    private readonly List<GameObject> _memberSlots = new List<GameObject>();

    /// <summary>邀请页候选人行，第一项是预制体原行模板，其余为按需复制的行。</summary>
    private readonly List<GameObject> _inviteRows = new List<GameObject>();

    /// <summary>每个候选人行当前绑定的真实角色ID，下标与 _inviteRows 一一对应。</summary>
    private readonly List<ulong> _inviteRowRoleIds = new List<ulong>();

    /// <summary>当前显示的界面。</summary>
    private TeamPage _currentPage;
    /// <summary>进入子窗口前的界面，仅用于关闭后返回。</summary>
    private TeamPage _returnPage;
    /// <summary>本地是否已在队伍中，仅控制成员区与空态展示。</summary>
    private bool _inTeam;

    /// <summary>当前是否存在真实待处理邀请，邀请确认页只在这个值为真时允许显示。</summary>
    private bool _hasPendingInvitation;

    /// <summary>绑定按钮事件，初始化为未建队的主界面。</summary>
    public override void InitWindow()
    {
        base.InitWindow();
        _createButton.onClick.RemoveListener(OnCreateClicked);
        _createButton.onClick.AddListener(OnCreateClicked);
        _closeButton.onClick.RemoveListener(OnCloseClicked);
        _closeButton.onClick.AddListener(OnCloseClicked);
        BindInteractableButtons();
        SetTeamState(false, string.Empty);
        SetMembers(null, 0UL, false);
        SetPendingInvitation(false, string.Empty, 0, 0, string.Empty);
        SetPendingApplication(false, string.Empty);
        SetInviteCandidates(null);
        OpenPage(TeamPage.Members);
    }

    /// <summary>绑定邀请、申请与邀请确认页上的正式按钮，事件都转成控制器可用的语义请求。</summary>
    private void BindInteractableButtons()
    {
        if (_inviteConfirmButton != null)
        {
            _inviteConfirmButton.onClick.RemoveListener(OnInviteConfirmClicked);
            _inviteConfirmButton.onClick.AddListener(OnInviteConfirmClicked);
        }
        if (_searchApplyButton != null)
        {
            _searchApplyButton.onClick.RemoveListener(OnSearchApplyClicked);
            _searchApplyButton.onClick.AddListener(OnSearchApplyClicked);
        }
        if (_applyAgreeButton != null)
        {
            _applyAgreeButton.onClick.RemoveListener(OnApplyAgreeClicked);
            _applyAgreeButton.onClick.AddListener(OnApplyAgreeClicked);
        }
        if (_applyRejectButton != null)
        {
            _applyRejectButton.onClick.RemoveListener(OnApplyRejectClicked);
            _applyRejectButton.onClick.AddListener(OnApplyRejectClicked);
        }
        if (_offerAgreeButton != null)
        {
            _offerAgreeButton.onClick.RemoveListener(OnOfferAgreeClicked);
            _offerAgreeButton.onClick.AddListener(OnOfferAgreeClicked);
        }
        if (_offerRejectButton != null)
        {
            _offerRejectButton.onClick.RemoveListener(OnOfferRejectClicked);
            _offerRejectButton.onClick.AddListener(OnOfferRejectClicked);
        }
    }

    /// <summary>刷新队伍成立状态、队伍名与空态，成员内容由 SetMembers 单独刷新。</summary>
    /// <param name="inTeam">服务器是否确认已在队伍中。</param>
    /// <param name="teamName">队伍显示名，未建队时传空字符串。</param>
    public void SetTeamState(bool inTeam, string teamName)
    {
        _inTeam = inTeam;
        _notInTeamRoot.SetActive(!inTeam);
        _memberRoot.SetActive(inTeam);
        _emptyState.SetActive(!inTeam && _currentPage == TeamPage.Members);
        _leaveButton.interactable = inTeam;
        if (inTeam)
        {
            _teamName.text = teamName;
        }
        else
        {
            _teamName.text = "我的队伍";
        }
    }

    /// <summary>
    /// 按服务器成员表重建成员卡。第一张成员卡使用预制体原实例，其余按需复制同一张卡，
    /// 始终排在邀请空位之前，多余成员卡隐藏，不新建控件节点。
    /// </summary>
    /// <param name="members">服务器下发的成员列表，为空表示不在队伍中。</param>
    /// <param name="localRoleId">本地主角角色ID，用于标记本人与队长。</param>
    /// <param name="canInvite">是否允许显示邀请空位，只有队长且未满员时为真。</param>
    public void SetMembers(IList<TeamMemberInfo> members, ulong localRoleId, bool canInvite)
    {
        if (_memberSlotTemplate == null)
        {
            return;
        }
        // 本次需要显示的成员数量，成员表为空时为零。
        int count = 0;
        if (members != null)
        {
            count = members.Count;
        }
        EnsureMemberSlots(count);
        // index 为成员卡列表零基下标，既定位控件也作为成员表下标使用。
        for (int index = 0; index < _memberSlots.Count; index++)
        {
            // 当前成员卡，超出成员数量时隐藏。
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
            ApplyMemberToSlot(slot, members[index], localRoleId, index);
        }
        if (_inviteSlot != null)
        {
            _inviteSlot.SetActive(canInvite);
            // 邀请空位始终排在所有成员卡之后，保持原版从左到右的排列顺序。
            _inviteSlot.transform.SetAsLastSibling();
        }
    }

    /// <summary>把单个成员信息写入指定成员卡。</summary>
    /// <param name="slot">目标成员卡实例。</param>
    /// <param name="member">服务器下发的成员信息。</param>
    /// <param name="localRoleId">本地主角角色ID。</param>
    /// <param name="index">成员在列表中的下标，0 表示第一张卡。</param>
    private void ApplyMemberToSlot(GameObject slot, TeamMemberInfo member, ulong localRoleId,
        int index)
    {
        if (member == null)
        {
            return;
        }
        // 第一张卡沿用预制体已序列化的引用，其余卡按固定路径取同结构控件。
        Text nameText = _memberName;
        Text levelText = _memberLevel;
        GameObject captainMark = _captainMark;
        Image jobImage = _professionImage;
        if (index > 0)
        {
            nameText = FindChildComponent<Text>(slot.transform, SlotNamePath);
            levelText = FindChildComponent<Text>(slot.transform, SlotLevelPath);
            captainMark = FindChildObject(slot.transform, SlotCaptainPath);
            jobImage = FindChildComponent<Image>(slot.transform, SlotJobPath);
        }
        if (nameText != null)
        {
            nameText.text = member.Nickname;
        }
        if (levelText != null)
        {
            levelText.text = "Lv." + member.Level;
        }
        if (captainMark != null)
        {
            captainMark.SetActive(member.IsCaptain);
        }
        // 本人所在的成员卡统一使用本人职业头像，其余成员保留预制体默认图。
        if (jobImage != null && localRoleId != 0UL && member.RoleId == localRoleId)
        {
            jobImage.enabled = true;
        }
    }

    /// <summary>按需补齐成员卡数量，复制模板并插到邀请空位之前。</summary>
    /// <param name="count">本次需要显示的成员数量。</param>
    private void EnsureMemberSlots(int count)
    {
        // 第一张成员卡就是预制体里的原实例，先登记再按需复制。
        if (_memberSlots.Count == 0)
        {
            _memberSlots.Add(_memberSlotTemplate);
        }
        while (_memberSlots.Count < count)
        {
            // 复制出来的成员卡实例，命名带当前数量后缀便于层级里区分，随后登记进列表复用。
            GameObject copy = Instantiate(_memberSlotTemplate, _memberRoot.transform);
            copy.name = _memberSlotTemplate.name + "_" + _memberSlots.Count;
            _memberSlots.Add(copy);
        }
    }

    /// <summary>
    /// 按当前场景真实附近的玩家重建邀请页候选行，对应 RO TeamInviteCtrl:SetPanelByInfo。
    /// 第一张行沿用预制体已序列化的行模板，其余按需复制同一行，行容器即原 ScrollRect 的 Content。
    /// 没有候选时隐藏全部候选行，恢复该页原有的空态表现，不新建空态节点。
    /// </summary>
    /// <param name="candidates">控制器过滤后的真实附近玩家，为空或 null 表示没有候选人。</param>
    public void SetInviteCandidates(IList<TeamMemberInfo> candidates)
    {
        if (_inviteRowRoot == null)
        {
            return;
        }
        // 本次需要显示的候选行数量，候选表为空时为零。
        int count = 0;
        if (candidates != null)
        {
            count = candidates.Count;
        }
        EnsureInviteRows(count);
        // index 为候选行列表零基下标，既定位控件也作为候选表下标使用。
        for (int index = 0; index < _inviteRows.Count; index++)
        {
            // 当前候选行，超出候选数量时隐藏。
            GameObject row = _inviteRows[index];
            if (row == null)
            {
                continue;
            }
            if (index >= count)
            {
                row.SetActive(false);
                _inviteRowRoleIds[index] = 0UL;
                continue;
            }
            row.SetActive(true);
            ApplyCandidateToRow(row, candidates[index], index);
        }
    }

    /// <summary>把单个真实候选玩家写入指定候选行，并记住该行的真实角色ID。</summary>
    /// <param name="row">目标候选行。</param>
    /// <param name="candidate">来自场景实体的真实候选玩家信息。</param>
    /// <param name="index">候选行下标，0 表示预制体原行模板。</param>
    private void ApplyCandidateToRow(GameObject row, TeamMemberInfo candidate, int index)
    {
        if (candidate == null)
        {
            return;
        }
        // 昵称文本：第一张行沿用预制体已序列化的引用，其余行按固定路径取同结构控件。
        Text nameText = _inviteRowName;
        if (index > 0)
        {
            nameText = FindChildComponent<Text>(row.transform, InviteRowNamePath);
        }
        // 等级文本：行模板上没有序列化引用，所有行统一按固定路径取控件。
        Text levelText = FindChildComponent<Text>(row.transform, InviteRowLevelPath);
        if (nameText != null)
        {
            nameText.text = candidate.Nickname;
        }
        if (levelText != null)
        {
            levelText.text = "Lv." + candidate.Level;
        }
        // 本行绑定的真实角色ID，点击邀请按钮时按行下标取出，不再依赖输入框。
        _inviteRowRoleIds[index] = candidate.RoleId;
    }

    /// <summary>按需补齐候选行数量，复制行模板并挂到原行模板所在的 ScrollRect 内容节点下。</summary>
    /// <param name="count">本次需要显示的候选行数量。</param>
    private void EnsureInviteRows(int count)
    {
        // 第一张候选行就是预制体里的原行模板，先登记并绑定它的邀请按钮。
        if (_inviteRows.Count == 0)
        {
            _inviteRows.Add(_inviteRowRoot);
            _inviteRowRoleIds.Add(0UL);
            BindInviteRowButton(_inviteRowButton, 0);
        }
        while (_inviteRows.Count < count)
        {
            // slot 为待创建候选行的 0 基行下标：0 号是预制体里的原行模板，这里直接取已有行数量作为新行下标。
            int slot = _inviteRows.Count;
            // 复制出来的候选行：挂到原行模板的父节点（ScrollRect 的 Content）下，命名带行下标后缀便于层级里区分。
            GameObject copy = Instantiate(_inviteRowRoot, _inviteRowRoot.transform.parent);
            copy.name = _inviteRowRoot.name + "_" + slot.ToString();
            _inviteRows.Add(copy);
            _inviteRowRoleIds.Add(0UL);
            // 复制行的邀请按钮按固定路径取，绑定后回调携带自己的行下标。
            Button copyButton = FindChildComponent<Button>(copy.transform, InviteRowButtonPath);
            BindInviteRowButton(copyButton, slot);
        }
    }

    /// <summary>给指定候选行的邀请按钮绑定按行下标回调，保证每行点击发出自己的真实角色ID。</summary>
    /// <param name="button">候选行上的邀请按钮。</param>
    /// <param name="slot">该行在候选行列表中的下标。</param>
    private void BindInviteRowButton(Button button, int slot)
    {
        if (button == null)
        {
            return;
        }
        // 行下标的本地副本，供闭包安全捕获，避免所有行共用同一个循环变量。
        int rowSlot = slot;
        button.onClick.AddListener(delegate { OnInviteRowSlotClicked(rowSlot); });
    }

    /// <summary>给指定成员卡设置职业头像，索引无效时跳过。</summary>
    /// <param name="index">成员卡下标。</param>
    /// <param name="jobSprite">职业头像精灵，为空时不做修改。</param>
    public void SetMemberJobSprite(int index, Sprite jobSprite)
    {
        if (jobSprite == null || index < 0 || index >= _memberSlots.Count)
        {
            return;
        }
        // 第一张成员卡直接用预制体序列化的图标引用。
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

    /// <summary>刷新收到邀请时的确认页内容，没有邀请时整页隐藏。</summary>
    /// <param name="hasInvitation">是否存在待处理邀请。</param>
    /// <param name="inviterName">邀请人昵称。</param>
    /// <param name="memberCount">邀请方当前人数。</param>
    /// <param name="maxMember">邀请方人数上限。</param>
    /// <param name="targetName">邀请方行动目标。</param>
    public void SetPendingInvitation(bool hasInvitation, string inviterName, int memberCount,
        int maxMember, string targetName)
    {
        if (_offerRoot == null)
        {
            return;
        }
        if (_offerInviterName != null)
        {
            _offerInviterName.text = inviterName;
        }
        if (_offerInfo != null)
        {
            _offerInfo.text = "邀请你加入队伍";
        }
        if (_offerMemberCount != null)
        {
            _offerMemberCount.text = memberCount + "/" + maxMember;
        }
        if (_offerTarget != null)
        {
            if (string.IsNullOrEmpty(targetName))
            {
                _offerTarget.text = "目标：待队长选择";
            }
            else
            {
                _offerTarget.text = targetName;
            }
        }
        if (_offerAgreeButton != null)
        {
            _offerAgreeButton.interactable = hasInvitation;
        }
        if (_offerRejectButton != null)
        {
            _offerRejectButton.interactable = hasInvitation;
        }
        // 本页面的显隐与真实待处理邀请保持一致：没有邀请时整页隐藏，邀请被同意或拒绝后随状态一起收起。
        _hasPendingInvitation = hasInvitation;
        _offerRoot.SetActive(hasInvitation && _currentPage == TeamPage.Recruitment);
    }

    /// <summary>刷新申请列表页内容，没有申请时显示原版空态。</summary>
    /// <param name="hasApplication">是否存在待审批申请。</param>
    /// <param name="applicantName">申请人昵称。</param>
    public void SetPendingApplication(bool hasApplication, string applicantName)
    {
        if (_applyRowRoot != null)
        {
            _applyRowRoot.SetActive(hasApplication);
        }
        if (_applyName != null)
        {
            _applyName.text = applicantName;
        }
        if (_applyAgreeButton != null)
        {
            _applyAgreeButton.interactable = hasApplication;
        }
        if (_applyRejectButton != null)
        {
            _applyRejectButton.interactable = hasApplication;
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
        // 邀请确认页就是待处理邀请的展示载体，没有真实邀请时不允许打开，
        // 否则会露出只有占位昵称与占位等级的假邀请框，且没有数据来源能让它消失。
        if (page == TeamPage.Recruitment && !_hasPendingInvitation)
        {
            page = TeamPage.Members;
        }
        _returnPage = _currentPage;
        _currentPage = page;
        // 成员和申请列表共用原队伍主窗外框，其余界面使用自身原外框。
        bool showMainFrame = page == TeamPage.Members || page == TeamPage.Applications;
        _mainContent.SetActive(showMainFrame);
        // 数组与具名界面编号一致，只切换显示，不创建控件。
        // pageIndex 为界面数组零基下标，与 TeamPage 枚举值一一对应，用于判断当前应显示哪一个。
        for (int pageIndex = 0; pageIndex < _pages.Length; pageIndex++)
        {
            if (_pages[pageIndex] != null)
            {
                _pages[pageIndex].SetActive(pageIndex == (int)page);
            }
        }
        _emptyState.SetActive(!_inTeam && page == TeamPage.Members);
        // 切页后交给控制器刷新数据，候选行只在打开或切页时重建，不做每帧轮询。
        if (PageOpened != null)
        {
            PageOpened();
        }
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

    /// <summary>把输入框里的数字解析为角色或队伍ID，非法输入返回 0。</summary>
    /// <param name="input">输入框引用，可为空。</param>
    /// <returns>解析出的ID；输入为空或非数字时返回 0。</returns>
    private static ulong ParseId(InputField input)
    {
        if (input == null)
        {
            return 0UL;
        }
        // 输入框原始文本，可能为空或含前后空白，解析前先判空再 Trim。
        string raw = input.text;
        if (string.IsNullOrEmpty(raw))
        {
            return 0UL;
        }
        // 解析结果，失败时保持零值表示非法输入。
        ulong parsed;
        if (!ulong.TryParse(raw.Trim(), out parsed))
        {
            return 0UL;
        }
        return parsed;
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

    /// <summary>原版离队按钮只请求控制器退出队伍。</summary>
    public void RequestLeaveTeam()
    {
        LeaveRequested?.Invoke();
    }

    /// <summary>把关闭请求交给控制器。</summary>
    private void OnCloseClicked()
    {
        CloseRequested?.Invoke();
    }

    /// <summary>邀请页确认按钮：按输入框里的角色ID发出邀请。</summary>
    private void OnInviteConfirmClicked()
    {
        // 邀请页输入框解析出的角色ID，为 0 表示输入为空或非法，直接忽略本次点击。
        ulong roleId = ParseId(_inviteInput);
        if (roleId == 0UL)
        {
            return;
        }
        InviteRoleRequested?.Invoke(roleId);
    }

    /// <summary>候选人行上的邀请按钮：对该行已绑定的真实角色发邀请。</summary>
    /// <param name="slot">被点击的候选行下标。</param>
    private void OnInviteRowSlotClicked(int slot)
    {
        // 行下标越界或该行当前未绑定角色时不发请求，避免向 0 号角色发邀请。
        if (slot < 0 || slot >= _inviteRowRoleIds.Count)
        {
            return;
        }
        // 该行绑定的真实角色ID，来自场景实体同步下来的远端角色资料。
        ulong roleId = _inviteRowRoleIds[slot];
        if (roleId == 0UL)
        {
            return;
        }
        InviteRoleRequested?.Invoke(roleId);
    }

    /// <summary>寻找队伍页的申请按钮：按输入框里的队伍ID申请加入。</summary>
    private void OnSearchApplyClicked()
    {
        // 寻找队伍页输入框解析出的队伍ID，为 0 表示输入为空或非法，直接忽略本次点击。
        ulong teamId = ParseId(_searchTeamIdInput);
        if (teamId == 0UL)
        {
            return;
        }
        ApplyTeamRequested?.Invoke(teamId);
    }

    /// <summary>申请行同意按钮。</summary>
    private void OnApplyAgreeClicked()
    {
        ApplicationAgreed?.Invoke();
    }

    /// <summary>申请行忽略按钮，等价拒绝申请。</summary>
    private void OnApplyRejectClicked()
    {
        ApplicationRejected?.Invoke();
    }

    /// <summary>邀请确认页加入按钮，等价同意邀请。</summary>
    private void OnOfferAgreeClicked()
    {
        InviteAgreed?.Invoke();
    }

    /// <summary>邀请确认页关闭按钮，等价拒绝邀请。</summary>
    private void OnOfferRejectClicked()
    {
        InviteRejected?.Invoke();
    }

    /// <summary>销毁时解除本窗口添加的按钮监听。</summary>
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
        if (_inviteConfirmButton != null)
        {
            _inviteConfirmButton.onClick.RemoveListener(OnInviteConfirmClicked);
        }
        if (_searchApplyButton != null)
        {
            _searchApplyButton.onClick.RemoveListener(OnSearchApplyClicked);
        }
        if (_applyAgreeButton != null)
        {
            _applyAgreeButton.onClick.RemoveListener(OnApplyAgreeClicked);
        }
        if (_applyRejectButton != null)
        {
            _applyRejectButton.onClick.RemoveListener(OnApplyRejectClicked);
        }
        if (_offerAgreeButton != null)
        {
            _offerAgreeButton.onClick.RemoveListener(OnOfferAgreeClicked);
        }
        if (_offerRejectButton != null)
        {
            _offerRejectButton.onClick.RemoveListener(OnOfferRejectClicked);
        }
    }
}
