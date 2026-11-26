using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO FriendsItemTemplate，显示一个联系人的名字、关系、红点和选中态。</summary>
public sealed class FriendsItemTemplate : MonoBehaviour
{
    /// <summary>选中会话的整行按钮。</summary>
    [SerializeField] private Button button;
    /// <summary>联系人当前角色昵称。</summary>
    [SerializeField] private Text nickname;
    /// <summary>主动添加、对方添加或双方好友的关系说明。</summary>
    [SerializeField] private Text relation;
    /// <summary>当前会话的未读徽标。</summary>
    [SerializeField] private GameObject badge;
    /// <summary>徽标内的实际未读数。</summary>
    [SerializeField] private Text unread;
    /// <summary>当前选中的 RO 行底纹。</summary>
    [SerializeField] private GameObject selected;
    /// <summary>该复用行绑定的角色编号。</summary>
    private ulong uid;
    /// <summary>将点击的实际角色号交给好友控制器。</summary>
    private Action<ulong> choose;
    /// <summary>每个行实例只绑定一次原生按钮。</summary>
    private void Awake() { button.onClick.AddListener(OnClick); }
    /// <summary>打开当前绑定的联系人的对话。</summary>
    private void OnClick() { choose?.Invoke(uid); }
    /// <summary>刷新列表的一行。</summary><param name="info">双向关系和角色名。</param><param name="count">该会话未读数量。</param><param name="active">是否为当前对话。</param><param name="onClick">选中联系人的控制器回调。</param>
    internal void Bind(GiftFriendInfo info, int count, bool active, Action<ulong> onClick)
    {
        // 本人的两个关系方向决定列表副标题。
        string detail;
        if (info.Added && info.AddedMe) detail = "互为好友";
        else if (info.Added) detail = "已添加"; else detail = "对方添加了你";
        BindRoom((ulong)info.RoleId, info.Name, detail, count, active, onClick);
    }
    /// <summary>群目录及联系人的共用行，展示名称、摘要与红点。</summary><param name="id">该行代表的角色或群编号。</param><param name="name">显示名称。</param><param name="detail">关系或群成员摘要。</param><param name="count">未读条数。</param><param name="active">是否当前选中。</param><param name="onClick">选择该行的动作。</param>
    internal void BindRoom(ulong id, string name, string detail, int count, bool active, Action<ulong> onClick)
    {
        uid = id; choose = onClick; nickname.text = name; relation.text = detail;
        selected.SetActive(active); badge.SetActive(count > 0); unread.text = count.ToString();
    }
}
