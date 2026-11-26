using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>好友、群聊和飞鹰共用的页头，显示四种实际货币余额并转发右侧导航点击。</summary>
public sealed class SocialPanelHeader : MonoBehaviour
{
    /// <summary>按铜币、银两、银票、元宝顺序排列的余额。</summary>
    [SerializeField] private Text[] balances;
    /// <summary>右栏好友入口。</summary>
    [SerializeField] private Button friends;
    /// <summary>右栏飞鹰入口。</summary>
    [SerializeField] private Button mail;
    /// <summary>好友或长期群的未读提示。</summary>
    [SerializeField] private GameObject friendHint;
    /// <summary>飞鹰的未读来信提示。</summary>
    [SerializeField] private GameObject mailHint;
    /// <summary>更新两个栏目从游戏菜单取得的真实红点。</summary><param name="friend">是否有私聊、群未读或邀请。</param><param name="unreadMail">是否有未读传书。</param>
    internal void SetHints(bool friend, bool unreadMail) { friendHint.SetActive(friend); mailHint.SetActive(unreadMail); }
    /// <summary>当前页显示时由主城提供四种货币的现值。</summary>
    internal event Action<SocialPanelHeader> Opened;
    /// <summary>通过主城已有的 WindowType 切换到所选栏目。</summary>
    internal event Action<WindowType> PageRequested;

    /// <summary>把已序列化的两个侧栏入口接入主城导航。</summary>
    private void Awake()
    {
        friends.onClick.AddListener(() => PageRequested?.Invoke(WindowType.FriendsPanel));
        mail.onClick.AddListener(() => PageRequested?.Invoke(WindowType.EmailPanel));
    }

    /// <summary>每次显示时重新读取该角色余额。</summary>
    private void OnEnable() { Opened?.Invoke(this); }

    /// <summary>以游戏当前数量填充页头，保留完整整数。</summary>
    /// <param name="copper">铜币余额。</param><param name="silver">银两余额。</param>
    /// <param name="ticket">银票余额。</param><param name="yuanbao">元宝余额。</param>
    internal void SetBalances(long copper, long silver, long ticket, long yuanbao)
    {
        balances[0].text = copper.ToString();
        balances[1].text = silver.ToString();
        balances[2].text = ticket.ToString();
        balances[3].text = yuanbao.ToString();
    }
}
