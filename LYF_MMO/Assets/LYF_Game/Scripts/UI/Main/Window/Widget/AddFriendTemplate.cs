using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO AddFriendTemplate，只展示一个由本区场景同步的真实玩家。</summary>
public sealed class AddFriendTemplate : MonoBehaviour
{
    /// <summary>角色职业所用的 RO 形象插画。</summary>
    [SerializeField] private Image portrait;
    /// <summary>头像旁标示真实职业的 RO 小图标。</summary>
    [SerializeField] private Image profession;
    /// <summary>头像下方的角色等级。</summary>
    [SerializeField] private Text level;
    /// <summary>行内玩家的真实昵称。</summary>
    [SerializeField] private Text nickname;
    /// <summary>本次推荐这名玩家的原因。</summary>
    [SerializeField] private Text reason;
    /// <summary>向选中的玩家发送添加请求。</summary>
    [SerializeField] private Button add;
    /// <summary>换组后更新为当前角色编号。</summary>
    private int roleId;
    /// <summary>将本行选中的编号交给好友控制器。</summary>
    private Action<int> selected;

    /// <summary>每行只注册一次加号按钮。</summary>
    private void Awake() { add.onClick.AddListener(Choose); }
    /// <summary>点击时读取当前绑定的玩家，避免换组后沿用旧目标。</summary>
    private void Choose() { selected?.Invoke(roleId); }

    /// <summary>将可见玩家资料和本次筛选原因绑定到行。</summary>
    /// <param name="info">服务器同步的角色资料。</param><param name="icon">根据职业选出的 RO 形象。</param><param name="job">该职业的标志，未知职业时为空。</param>
    /// <param name="why">推荐原因。</param><param name="onAdd">通过真实编号添加的回调。</param>
    internal void SetData(RoleBaseInfo info, Sprite icon, Sprite job, string why, Action<int> onAdd)
    {
        roleId = info.RoleId;
        portrait.sprite = icon;
        profession.sprite = job;
        profession.gameObject.SetActive(job != null);
        level.text = info.Level.ToString();
        nickname.text = info.Nickname;
        reason.text = why;
        selected = onAdd;
    }
}
