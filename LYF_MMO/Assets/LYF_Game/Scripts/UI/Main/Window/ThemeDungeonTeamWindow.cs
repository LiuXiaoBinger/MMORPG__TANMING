using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RO ThemeDungeonTeam 入场确认窗，仅展示成员答复和倒计时，协议交给队伍控制器。</summary>
public sealed class ThemeDungeonTeamWindow : WindowBase
{
    /// <summary>本次挑战的副本名称。</summary>
    [SerializeField] private TMP_Text _dungeonName;
    /// <summary>确认剩余秒数，不用于决定是否入场。</summary>
    [SerializeField] private TMP_Text _countdown;
    /// <summary>原版确认进度条，数值范围 0 到 1。</summary>
    [SerializeField] private Slider _timerSlider;
    /// <summary>RO 成员确认卡模板，复制后的卡也保留在此父节点下。</summary>
    [SerializeField] private Transform _memberTemplate;
    /// <summary>向控制器提交同意选择的按钮。</summary>
    [SerializeField] private Button _agreeButton;
    /// <summary>向控制器提交拒绝选择的按钮。</summary>
    [SerializeField] private Button _refuseButton;
    /// <summary>成员卡池，关闭窗口不销毁卡片。</summary>
    private readonly List<Transform> _rows = new List<Transform>();
    /// <summary>当前各成员卡绑定的真实角色号，仅用于定位展示行。</summary>
    private readonly List<ulong> _roleIds = new List<ulong>();
    /// <summary>当前本人的角色号，用于停用已答复按钮。</summary>
    private ulong _localRoleId;
    /// <summary>本次显示倒计时的结束时刻，使用 Unity 不缩放时间。</summary>
    private float _deadline;
    /// <summary>服务端给出的本次确认时限，单位秒。</summary>
    private float _duration;
    /// <summary>用户点击同意或拒绝后交给控制器的强类型事件。</summary>
    public event Action<ReplyType> ReplyRequested;

    /// <summary>绑定原版按钮；正式窗口在 MainView 初始化时调用。</summary>
    public override void InitWindow()
    {
        _agreeButton.onClick.AddListener(OnAgreeClicked);
        _refuseButton.onClick.AddListener(OnRefuseClicked);
        _rows.Add(_memberTemplate);
        CloseWindow();
    }

    /// <summary>显示全队成员，队长发起即视为同意，其余成员等待真实答复。</summary>
    /// <param name="members">最新服务器队伍名单。</param><param name="localRoleId">本机角色号。</param>
    /// <param name="seconds">服务端本次确认的持续秒数。</param>
    /// <param name="dungeonName">服务端询问中副本编号对应的正式名称。</param>
    /// <param name="difficultyName">服务端询问中难度编号对应的正式名称。</param>
    public void SetMembers(IList<TeamMemberInfo> members, ulong localRoleId, uint seconds,
        string dungeonName, string difficultyName)
    {
        _localRoleId = localRoleId;
        _duration = seconds;
        _deadline = Time.unscaledTime + seconds;
        _dungeonName.text = "即将进入" + dungeonName + " · " + difficultyName;
        _roleIds.Clear();
        _agreeButton.interactable = true;
        _refuseButton.interactable = true;
        while (_rows.Count < members.Count)
            _rows.Add(Instantiate(_memberTemplate, _memberTemplate.parent));
        // 每张卡使用真实昵称，状态标签沿用 RO 等待、同意、拒绝三个节点。
        for (int index = 0; index < _rows.Count; index++)
        {
            // 当前展示卡，队伍缩小时隐藏多余实例。
            Transform row = _rows[index];
            row.gameObject.SetActive(index < members.Count);
            if (index >= members.Count) continue;
            // 名单顺序与快捷栏一致，不通过附近实体推测队友。
            TeamMemberInfo member = members[index];
            _roleIds.Add(member.RoleId);
            row.Find("NameLab").GetComponent<TMP_Text>().text = member.Nickname;
            row.Find("StateLab1").gameObject.SetActive(member.IsCaptain);
            row.Find("StateLab2").gameObject.SetActive(!member.IsCaptain);
            row.Find("StateLab3").gameObject.SetActive(false);
            row.Find("LeadFlag").gameObject.SetActive(member.IsCaptain);
            if (member.RoleId == localRoleId && member.IsCaptain) DisableReply();
        }
        Update();
    }

    /// <summary>根据服务器同步的答复更新对应成员卡。</summary>
    /// <param name="roleId">实际答复人。</param><param name="reply">服务器已接纳的答复。</param>
    public void SetReply(ulong roleId, ReplyType reply)
    {
        // 卡片按角色号匹配，避免把队长或本人硬编码为第一行。
        int index = _roleIds.IndexOf(roleId);
        if (index < 0) return;
        _rows[index].Find("StateLab1").gameObject.SetActive(reply == ReplyType.Agree);
        _rows[index].Find("StateLab2").gameObject.SetActive(false);
        _rows[index].Find("StateLab3").gameObject.SetActive(reply == ReplyType.Refuse);
        if (roleId == _localRoleId) DisableReply();
    }

    /// <summary>使用队伍控制器已有的职业图标资源更新确认卡，避免显示原版模板占位职业。</summary>
    /// <param name="index">当前成员卡下标。</param><param name="sprite">该成员职业对应的现有图标。</param>
    public void SetMemberJobSprite(int index, Sprite sprite)
    {
        if (sprite != null) _rows[index].Find("Image").GetComponent<Image>().sprite = sprite;
    }

    /// <summary>请求成功提交后停用本人的两个选择按钮，等待服务器结果。</summary>
    public void DisableReply()
    {
        _agreeButton.interactable = false;
        _refuseButton.interactable = false;
    }

    /// <summary>只更新显示，不在客户端超时后代替玩家同意。</summary>
    private void Update()
    {
        // 当前显示的剩余秒数，下限为零，最终关闭由 MS 通知决定。
        float remaining = Mathf.Max(0f, _deadline - Time.unscaledTime);
        _countdown.text = "等待队员确认（" + Mathf.CeilToInt(remaining) + "秒）";
        if (_duration > 0f) _timerSlider.value = remaining / _duration;
    }

    /// <summary>转发同意选择，窗口自身不切换场景。</summary>
    private void OnAgreeClicked() { ReplyRequested?.Invoke(ReplyType.Agree); }
    /// <summary>转发拒绝选择，由服务器通知全队取消入场。</summary>
    private void OnRefuseClicked() { ReplyRequested?.Invoke(ReplyType.Refuse); }
}
