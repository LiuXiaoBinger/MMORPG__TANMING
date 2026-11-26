using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>好友窗口的左侧目录和新建分组弹层。目录行、头像和选择格均来自预制体。</summary>
public sealed class FriendsDirectory : MonoBehaviour
{
    /// <summary>联系人页独有的搜索栏。</summary>
    [SerializeField] private GameObject searchBar;
    /// <summary>最近页将列表顶边升至页签下，联系人页为搜索框留出高度。</summary>
    [SerializeField] private RectTransform viewport;
    /// <summary>按照角色号或昵称筛选联系人。</summary>
    [SerializeField] internal ChatInputField search;
    /// <summary>分组折叠行模板。</summary>
    [SerializeField] private FriendsItemTemplate heading;
    /// <summary>可选择的联系人行模板。</summary>
    [SerializeField] private FriendsItemTemplate contact;
    /// <summary>联系人目录末尾的新建分组按钮。</summary>
    [SerializeField] private Button newGroup;
    /// <summary>底部快捷入口，直接打开本人的联系人分组编辑器。</summary>
    [SerializeField] private Button shortcut;
    /// <summary>从最近页打开好友分组时先展示完整联系人来源。</summary>
    internal event Action ContactsRequested;
    /// <summary>列表或搜索结果为空时的提示。</summary>
    [SerializeField] private Text empty;
    /// <summary>覆盖窗口的分组选择弹层。</summary>
    [SerializeField] private GameObject editor;
    /// <summary>取消本次分组编辑。</summary>
    [SerializeField] private Button cancel;
    /// <summary>从未分组好友里按编号或昵称筛选。</summary>
    [SerializeField] internal ChatInputField pickerSearch;
    /// <summary>待创建的分组名称。</summary>
    [SerializeField] internal ChatInputField groupName;
    /// <summary>弹层内每个可勾选好友的预制格。</summary>
    [SerializeField] private FriendsItemTemplate choice;
    /// <summary>弹层右上角显示实际勾选数量。</summary>
    [SerializeField] private Text count;
    /// <summary>弹层内的空列表或输入错误提示。</summary>
    [SerializeField] private Text pickerHint;
    /// <summary>提交当前勾选与组名。</summary>
    [SerializeField] private Button create;
    /// <summary>本次展示的分组标题复用池。</summary>
    private readonly List<FriendsItemTemplate> headers = new List<FriendsItemTemplate>();
    /// <summary>当前目录中的联系人复用池。</summary>
    private readonly List<FriendsItemTemplate> contacts = new List<FriendsItemTemplate>();
    /// <summary>双列选择器的格子复用池。</summary>
    private readonly List<FriendsItemTemplate> choices = new List<FriendsItemTemplate>();
    /// <summary>本次编辑已勾选的真实角色编号。</summary>
    private readonly List<int> picked = new List<int>();
    /// <summary>控制器传入的有序真实联系人，供搜索和选人复用。</summary>
    private List<GiftFriendInfo> source = new List<GiftFriendInfo>();
    /// <summary>当前角色的本地分组及特别关注记录。</summary>
    private FriendContactGroups groups = new FriendContactGroups();
    /// <summary>小于零的编号用于内置类别，非负编号对应自定义分组下标。</summary>
    private int expanded = -1;
    /// <summary>当前正在排版的标题行数。</summary>
    private int headerCount;
    /// <summary>当前正在排版的联系人行数。</summary>
    private int contactCount;
    /// <summary>按角色号查询一行的未读条数。</summary>
    private Func<int, int> unread;
    /// <summary>选中角色后由控制器进入会话。</summary>
    private Action<ulong> select;
    /// <summary>正在阅读的会话对端，用于标记选中行。</summary>
    private ulong selected;
    /// <summary>选单或搜索内容改变时重新应用控制器当前页状态。</summary>
    internal event Action Changed;
    /// <summary>将新分组名称和勾选角色交给控制器保存。</summary>
    internal event Action<string, List<int>> GroupCreated;

    /// <summary>绑定联系人筛选和分组编辑的本地交互。</summary>
    private void Awake()
    {
        search.onValueChanged.AddListener(value => Changed?.Invoke());
        pickerSearch.onValueChanged.AddListener(value => DrawChoices());
        newGroup.onClick.AddListener(OpenEditor);
        shortcut.onClick.AddListener(() => { ContactsRequested?.Invoke(); OpenEditor(); });
        cancel.onClick.AddListener(() => editor.SetActive(false));
        create.onClick.AddListener(CreateGroup);
    }

    /// <summary>切换主角或离开窗口时关闭编辑层。</summary>
    private void OnDisable() { editor.SetActive(false); }

    /// <summary>清除上一个角色在目录中输入的查询或选中分类。</summary>
    internal void ResetRole()
    {
        search.SetTextWithoutNotify("");
        expanded = -1;
        editor.SetActive(false);
    }

    /// <summary>按当前页签与分组顺序绑定左栏。</summary>
    /// <param name="list">已按最近时间排序的好友。</param><param name="data">本角色的目录分组。</param>
    /// <param name="recent">是否只显示最近联系人。</param><param name="peer">当前会话角色号。</param>
    /// <param name="getUnread">读取单个联系人的未读数。</param><param name="onSelect">选择联系人后进入私聊。</param>
    internal void Show(List<GiftFriendInfo> list, FriendContactGroups data, bool recent, ulong peer, Func<int, int> getUnread, Action<ulong> onSelect)
    {
        source = list; groups = data; unread = getUnread; select = onSelect; selected = peer;
        headerCount = 0; contactCount = 0;
        searchBar.SetActive(!recent);
        // 目录底边对齐页脚，两个页签只调整顶边。
        float height = 319f;
        if (recent) height = 369f;
        viewport.sizeDelta = new Vector2(viewport.sizeDelta.x, height);
        viewport.anchoredPosition = new Vector2(viewport.anchoredPosition.x, -197f + height * .5f);
        newGroup.gameObject.SetActive(!recent);
        if (recent) AddContacts(list);
        else if (search.text.Trim().Length > 0)
        {
            AddContacts(list.FindAll(friend => Matches(friend, search.text.Trim())));
        }
        else
        {
            for (int index = 0; index < data.groups.Count; index++)
            {
                // 当前分组按其保存的角色号与最新好友列表取交集。
                FriendContactGroups.Group group = data.groups[index];
                AddGroup(index, group.name, list.FindAll(friend => group.members.Contains(friend.RoleId)));
            }
            AddGroup(-1, "全部好友", list.FindAll(friend => friend.Added));
            AddGroup(-2, "特别关注", list.FindAll(friend => data.stars.Contains(friend.RoleId)));
            AddGroup(-3, "关注我的人", list.FindAll(friend => friend.AddedMe));
            newGroup.transform.SetAsLastSibling();
        }
        HideTail(headers, headerCount);
        HideTail(contacts, contactCount);
        empty.gameObject.SetActive(headerCount == 0 && contactCount == 0);
        if (recent) empty.text = "暂无最近联系人"; else empty.text = "没有找到联系人";
        if (editor.activeSelf) DrawChoices();
    }

    /// <summary>从已存在的行池取得所需的一行并排到当前顺序末尾。</summary>
    /// <param name="rows">对应行的复用池。</param><param name="prototype">保存布局的原始模板。</param><param name="index">本轮显示的行下标。</param>
    private static FriendsItemTemplate Row(List<FriendsItemTemplate> rows, FriendsItemTemplate prototype, int index)
    {
        if (index == rows.Count) rows.Add(Instantiate(prototype, prototype.transform.parent));
        // 此实例对应本次排序后的真实行。
        FriendsItemTemplate row = rows[index];
        row.gameObject.SetActive(true); row.transform.SetAsLastSibling(); return row;
    }

    /// <summary>收起本次列表长度以外的已创建行。</summary><param name="rows">可复用的行。</param><param name="visible">当前可见行数。</param>
    private static void HideTail(List<FriendsItemTemplate> rows, int visible)
    {
        for (int index = visible; index < rows.Count; index++) rows[index].gameObject.SetActive(false);
    }

    /// <summary>显示一个可展开的分类及其中的好友。</summary>
    /// <param name="key">组在本机目录中的下标或内置分类编号。</param><param name="name">分类名称。</param><param name="items">当下匹配分类的好友。</param>
    private void AddGroup(int key, string name, List<GiftFriendInfo> items)
    {
        // 当前只展开一个分类，便于在窄列表内查看成员。
        bool open = expanded == key;
        // 方向提示跟随当前分类的展开状态。
        string arrow = "▼";
        if (open) arrow = "▲";
        Row(headers, heading, headerCount++).BindRoom(0, name + "  " + items.Count, arrow, 0, open, uid =>
        {
            if (expanded == key) expanded = int.MinValue; else expanded = key;
            Changed?.Invoke();
        });
        if (open) AddContacts(items);
    }

    /// <summary>按控制器给定的顺序显示联系人。</summary><param name="items">当前页或分类实际匹配的人。</param>
    private void AddContacts(List<GiftFriendInfo> items)
    {
        for (int index = 0; index < items.Count; index++)
        {
            // 复用行只显示当前联系人本人的关系与未读。
            GiftFriendInfo item = items[index];
            Row(contacts, contact, contactCount++).Bind(item, unread(item.RoleId), selected == (ulong)item.RoleId, select);
        }
    }

    /// <summary>匹配昵称子串或完整角色号。</summary><param name="friend">候选联系人。</param><param name="query">玩家输入的检索文字。</param>
    private static bool Matches(GiftFriendInfo friend, string query)
    {
        return friend.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || friend.RoleId.ToString() == query;
    }

    /// <summary>清空上一次临时选择，打开双列分组编辑弹层。</summary>
    private void OpenEditor()
    {
        picked.Clear(); groupName.SetTextWithoutNotify(""); pickerSearch.SetTextWithoutNotify("");
        editor.SetActive(true); DrawChoices();
    }

    /// <summary>列出尚未归入自定义分组且匹配搜索的真实好友。</summary>
    private void DrawChoices()
    {
        // 已经绘制的勾选格数量。
        int visible = 0;
        for (int index = 0; index < source.Count; index++)
        {
            // 从当前联系人页来源中挑选可归类好友。
            GiftFriendInfo friend = source[index];
            if (!friend.Added || !Matches(friend, pickerSearch.text.Trim())) continue;
            if (groups.groups.Exists(group => group.members.Contains(friend.RoleId))) continue;
            Row(choices, choice, visible++).Bind(friend, 0, picked.Contains(friend.RoleId), ToggleChoice);
        }
        HideTail(choices, visible);
        count.text = "已选择好友：" + picked.Count;
        pickerHint.text = "";
        if (visible == 0) pickerHint.text = "没有可选择的未分组好友";
    }

    /// <summary>点击好友卡片切换其勾选状态。</summary><param name="uid">当前点击的角色编号。</param>
    private void ToggleChoice(ulong uid)
    {
        if (!picked.Remove((int)uid)) picked.Add((int)uid);
        DrawChoices();
    }

    /// <summary>验证组名并将选择提交给角色的目录存储。</summary>
    private void CreateGroup()
    {
        // 去除输入法提交时的首尾空格。
        string name = groupName.text.Trim();
        if (name.Length == 0) { pickerHint.text = "请输入分组名称"; return; }
        if (groups.groups.Exists(group => group.name == name)) { pickerHint.text = "已有同名分组"; return; }
        expanded = groups.groups.Count;
        editor.SetActive(false); GroupCreated?.Invoke(name, new List<int>(picked));
    }
}
