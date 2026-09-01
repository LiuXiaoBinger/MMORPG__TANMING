using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the expanded and collapsed states of the scene chat window.
/// All UI references are assigned in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatWindowCollapseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChatWindows chatWindow;
    [SerializeField] private GameObject expandedPanel;
    [SerializeField] private GameObject quickChatPanel;
    [SerializeField] private Button quickChatButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text unreadCountText;

    [Header("Initial State")]
    [SerializeField] private bool startCollapsed = true;

    private readonly int[] unreadCounts = new int[Enum.GetValues(typeof(ChatChannel)).Length];

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

    private void OnEnable()
    {
        if (chatWindow == null)
        {
            return;
        }

        chatWindow.MessageReceived += OnMessageReceived;
        chatWindow.ChannelVisibilityChanged += OnChannelVisibilityChanged;
    }

    private void Start()
    {
        SetExpanded(!startCollapsed, false);
    }

    private void OnDisable()
    {
        if (chatWindow != null)
        {
            chatWindow.MessageReceived -= OnMessageReceived;
            chatWindow.ChannelVisibilityChanged -= OnChannelVisibilityChanged;
        }
    }

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

    public void ShowExpandedChat()
    {
        SetExpanded(true, true);
    }

    public void ShowQuickChat()
    {
        SetExpanded(false, false);
    }

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

        int index = (int)message.Channel;
        if (index >= 0 && index < unreadCounts.Length)
        {
            unreadCounts[index]++;
            RefreshUnreadBadge();
        }
    }

    private void OnChannelVisibilityChanged(ChatChannel channel, bool isVisible)
    {
        if (!isVisible)
        {
            int index = (int)channel;
            if (index >= 0 && index < unreadCounts.Length)
            {
                unreadCounts[index] = 0;
            }
        }

        RefreshUnreadBadge();
    }

    private void RefreshUnreadBadge()
    {
        if (unreadCountText == null)
        {
            return;
        }

        int total = 0;
        for (int index = 0; index < unreadCounts.Length; index++)
        {
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

    private void ValidateReferences()
    {
        if (chatWindow == null || expandedPanel == null || quickChatPanel == null ||
            quickChatButton == null || closeButton == null || unreadCountText == null)
        {
            Debug.LogWarning("ChatWindowCollapseController requires all ChatWindow UI references in the Inspector.", this);
        }
    }
}
