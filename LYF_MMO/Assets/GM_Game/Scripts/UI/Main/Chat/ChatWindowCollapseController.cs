using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 切换完整聊天窗口与快捷聊天入口，并在折叠时统计可见频道的新消息。
/// 控件由 Inspector 提供；主动展开会清空这里保存的全部频道未读数。
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatWindowCollapseController : MonoBehaviour
{
    /// <summary>提供消息接收事件与频道可见状态的聊天窗口。</summary>
    [Header("References")]
    [SerializeField] private ChatWindows chatWindow;
    /// <summary>完整聊天面板，activeSelf 为 true 时不累计折叠入口未读数。</summary>
    [SerializeField] private GameObject expandedPanel;
    /// <summary>折叠时显示的快捷聊天入口，与完整面板互斥显示。</summary>
    [SerializeField] private GameObject quickChatPanel;
    /// <summary>展开聊天并清空未读计数的按钮。</summary>
    [SerializeField] private Button quickChatButton;
    /// <summary>收起完整面板、恢复快捷入口的按钮。</summary>
    [SerializeField] private Button closeButton;
    /// <summary>显示可见频道未读总数的角标文字，总数为 0 时隐藏。</summary>
    [SerializeField] private TMP_Text unreadCountText;

    /// <summary>首次 Start 后是否使用折叠状态。</summary>
    [Header("Initial State")]
    [SerializeField] private bool startCollapsed = true;

    /// <summary>按 ChatChannel 枚举下标保存折叠期间接收的消息数，与 ChatWindows 内部滚动未读数分开管理。</summary>
    private readonly int[] unreadCounts = new int[Enum.GetValues(typeof(ChatChannel)).Length];

    /// <summary>注册展开和关闭按钮，再检查 Inspector 引用是否齐全。</summary>
    private void Awake()
    {
        if (quickChatButton != null)
        {
            quickChatButton.onClick.AddListener(ShowExpandedChat);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ShowQuickChat);
        }

        ValidateReferences();
    }

    /// <summary>启用时监听收到消息与频道显隐变化。</summary>
    private void OnEnable()
    {
        if (chatWindow == null)
        {
            return;
        }

        chatWindow.MessageReceived += OnMessageReceived;
        chatWindow.ChannelVisibilityChanged += OnChannelVisibilityChanged;
    }

    /// <summary>按 startCollapsed 应用首次显隐，不主动清空已经收到的未读数。</summary>
    private void Start()
    {
        SetExpanded(!startCollapsed, false);
    }

    /// <summary>停用时解除聊天事件订阅，保留未读计数。</summary>
    private void OnDisable()
    {
        if (chatWindow != null)
        {
            chatWindow.MessageReceived -= OnMessageReceived;
            chatWindow.ChannelVisibilityChanged -= OnChannelVisibilityChanged;
        }
    }

    /// <summary>销毁时移除本组件的展开和关闭按钮监听。</summary>
    private void OnDestroy()
    {
        if (quickChatButton != null)
        {
            quickChatButton.onClick.RemoveListener(ShowExpandedChat);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ShowQuickChat);
        }
    }

    /// <summary>展开完整聊天面板，并清空所有频道的折叠未读数。</summary>
    public void ShowExpandedChat()
    {
        SetExpanded(true, true);
    }

    /// <summary>收起完整面板并显示快捷入口，保留原未读数。</summary>
    public void ShowQuickChat()
    {
        SetExpanded(false, false);
    }

    /// <summary>同步两个面板的显隐，再按需要清空计数并更新角标。</summary>
    /// <param name="expanded">true 显示完整面板，false 显示快捷入口。</param>
    /// <param name="clearUnread">是否清空所有频道的折叠未读数。</param>
    private void SetExpanded(bool expanded, bool clearUnread)
    {
        if (expandedPanel != null)
        {
            expandedPanel.SetActive(expanded);
        }

        if (quickChatPanel != null)
        {
            quickChatPanel.SetActive(!expanded);
        }

        if (clearUnread)
        {
            Array.Clear(unreadCounts, 0, unreadCounts.Length);
        }

        RefreshUnreadBadge();
    }

    /// <summary>完整面板关闭且消息频道标签可见时，为对应频道增加一条未读。</summary>
    /// <param name="message">ChatWindows 接收并发出通知的消息，允许为空。</param>
    private void OnMessageReceived(ChatMessage message)
    {
        if (message == null || expandedPanel == null || expandedPanel.activeSelf || chatWindow == null)
        {
            return;
        }

        if (!chatWindow.IsChannelTabVisible(message.Channel))
        {
            return;
        }

        // 消息频道直接作为计数数组下标，越界频道不累计。
        int index = (int)message.Channel;
        if (index >= 0 && index < unreadCounts.Length)
        {
            unreadCounts[index]++;
            RefreshUnreadBadge();
        }
    }

    /// <summary>频道被隐藏时清空其折叠未读数，之后重新计算总角标。</summary>
    /// <param name="channel">显隐发生变化的频道。</param>
    /// <param name="isVisible">该频道标签当前是否显示。</param>
    private void OnChannelVisibilityChanged(ChatChannel channel, bool isVisible)
    {
        if (!isVisible)
        {
            // 需要清零的频道在未读数组中的位置。
            int index = (int)channel;
            if (index >= 0 && index < unreadCounts.Length)
            {
                unreadCounts[index] = 0;
            }
        }

        RefreshUnreadBadge();
    }

    /// <summary>合计仍可见频道的未读数；无聊天引用时合计全部频道，零总数时隐藏文字。</summary>
    private void RefreshUnreadBadge()
    {
        if (unreadCountText == null)
        {
            return;
        }

        // 本轮汇总结果，用于决定角标显隐和文本。
        int total = 0;
        for (int index = 0; index < unreadCounts.Length; index++)
        {
            // 当前计数槽对应的频道，只有其标签可见才计入总数。
            ChatChannel channel = (ChatChannel)index;
            if (chatWindow == null || chatWindow.IsChannelTabVisible(channel))
            {
                total += unreadCounts[index];
            }
        }

        unreadCountText.gameObject.SetActive(total > 0);
        if (total > 0)
        {
            unreadCountText.text = total.ToString();
        }
    }

    /// <summary>只报告缺失引用，不自动创建控件或修改引用。</summary>
    private void ValidateReferences()
    {
        if (chatWindow == null || expandedPanel == null || quickChatPanel == null ||
            quickChatButton == null || closeButton == null || unreadCountText == null)
        {
            Debug.LogWarning("ChatWindowCollapseController requires all ChatWindow UI references in the Inspector.", this);
        }
    }
}
