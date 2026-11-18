using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>组织聊天表情分类和内置表情翻页，复用当前发言目标。</summary>
public sealed class YimengChatEmojiPicker : MonoBehaviour
{
    /// <summary>表情、历史和其他内容分类，第一项为可发的系统表情。</summary>
    [SerializeField] private YimengChatPages _categories;
    /// <summary>系统表情网格的各页，顺序与页码一致。</summary>
    [SerializeField] private GameObject[] _gridPages;
    /// <summary>全部可发送表情格，也是完整标记和图像的唯一绑定来源。</summary>
    [SerializeField] private YimengChatEmojiTile[] _tiles;
    /// <summary>返回前一页表情的按钮，第一页时不可点击。</summary>
    [SerializeField] private Button _previous;
    /// <summary>前往后一页表情的按钮，最后一页时不可点击。</summary>
    [SerializeField] private Button _next;
    /// <summary>以从 1 开始的人类页码展示当前页和总页数。</summary>
    [SerializeField] private TMP_Text _pageCaption;
    /// <summary>当前网格页在预制体数组内的索引，从 0 开始。</summary>
    private int _page;

    /// <summary>由聊天窗口初始化一次发送和翻页动作。</summary>
    /// <param name="send">把选中格子的整条标记交给聊天发送入口。</param>
    internal void Init(Action<string> send)
    {
        _previous.onClick.AddListener(() => ShowGridPage(_page - 1));
        _next.onClick.AddListener(() => ShowGridPage(_page + 1));
        // 所有网格项按各自绑定的标记发言，翻页只控制显隐。
        for (int i = 0; i < _tiles.Length; i++)
        {
            // 单个按钮在切页之后继续引用同一条配置。
            YimengChatEmojiTile tile = _tiles[i];
            tile.Button.onClick.AddListener(() => send(tile.Marker));
        }
    }

    /// <summary>每次点开笑脸时显示第一类和系统表情第一页。</summary>
    internal void Open()
    {
        _categories.SelectPage(0);
        ShowGridPage(0);
        gameObject.SetActive(true);
    }

    /// <summary>按发言频道的能力刷新表情按钮，分类和翻页仍可使用。</summary>
    /// <param name="canPost">当前项目正式接通发言的频道为 true。</param>
    internal void SetCanPost(bool canPost)
    {
        // 各页表情使用同一发言目标，离屏格子也同步可点击状态。
        for (int i = 0; i < _tiles.Length; i++)
            _tiles[i].Button.interactable = canPost;
    }

    /// <summary>按原文匹配内置表情格，部分匹配及普通正文返回空。</summary>
    /// <param name="text">服务器广播或历史中保存的整条文字正文。</param>
    internal YimengChatEmojiTile Find(string text)
    {
        // 文本没有完整表情标记时按普通消息呈现。
        for (int i = 0; i < _tiles.Length; i++)
        {
            if (string.Equals(_tiles[i].Marker, text, StringComparison.Ordinal))
                return _tiles[i];
        }
        return null;
    }

    /// <summary>同步系统表情显示页、按钮可用态和页码。</summary>
    /// <param name="page">预制体内的目标网格索引，从 0 开始。</param>
    private void ShowGridPage(int page)
    {
        _page = page;
        // 同时只展示一页网格，隐藏页也不会接收点选。
        for (int i = 0; i < _gridPages.Length; i++)
            _gridPages[i].SetActive(i == page);
        _previous.interactable = page > 0;
        _next.interactable = page < _gridPages.Length - 1;
        _pageCaption.text = (page + 1) + " / " + _gridPages.Length;
    }
}
