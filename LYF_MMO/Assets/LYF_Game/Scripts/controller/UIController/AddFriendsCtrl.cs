using System;
using System.Collections.Generic;

/// <summary>对应 RO AddFriendsCtrl。推荐和搜索读取本区当前可见角色，实际添加沿用 FriendMgr。</summary>
public sealed class AddFriendsCtrl
{
    /// <summary>持有既有输入框、搜索键及状态栏的好友主窗。</summary>
    private readonly FriendsPanel parent;
    /// <summary>负责展示推荐的子面板。</summary>
    private readonly AddFriendsPanel view;
    /// <summary>原有好友查询及添加接口。</summary>
    private readonly FriendMgr mgr;
    /// <summary>本次读取、筛选并排序后的可见玩家。</summary>
    private readonly List<RoleBaseInfo> candidates = new List<RoleBaseInfo>();
    /// <summary>用来读取当前场景真实角色的实体列表。</summary>
    private readonly List<MEntity> entities = new List<MEntity>();
    /// <summary>推荐原因及排序参考的本角色资料。</summary>
    private RoleBaseInfo self;
    /// <summary>零到三依次表示全部、同门、等级、修为。</summary>
    private int filter;
    /// <summary>当前页首项在已筛选列表中的位置。</summary>
    private int start;
    /// <summary>仅在玩家点击搜索或提交输入时更新。</summary>
    private string query = "";

    /// <summary>将已有添加入口及弹层控件接入本区好友流程。</summary>
    /// <param name="parent">主城好友窗口。</param><param name="mgr">本角色好友管理器。</param>
    internal AddFriendsCtrl(FriendsPanel parent, FriendMgr mgr)
    {
        this.parent = parent;
        this.mgr = mgr;
        view = parent.discovery;
        view.Opened += Open;
        view.FilterChanged += SelectFilter;
        view.next.onClick.AddListener(Next);
        view.exactAdd.onClick.AddListener(AddExact);
        parent.add.onClick.AddListener(SearchFriend);
        parent.nickname.onSubmit.AddListener(Submit);
        mgr.Changed += Refresh;
    }
    /// <summary>随好友窗口移除输入、关系和按钮的回调。</summary>
    internal void Dispose()
    {
        view.Opened -= Open;
        view.FilterChanged -= SelectFilter;
        view.next.onClick.RemoveListener(Next);
        view.exactAdd.onClick.RemoveListener(AddExact);
        parent.add.onClick.RemoveListener(SearchFriend);
        parent.nickname.onSubmit.RemoveListener(Submit);
        mgr.Changed -= Refresh;
    }
    /// <summary>打开列表时读取实际区名并恢复全部推荐。</summary>
    private void Open()
    {
        query = "";
        parent.nickname.SetTextWithoutNotify("");
        view.SetServer(Global.Instance?.LoginInfo?.GameServer?.ServerName ?? "当前区服");
        view.SelectFilter(0);
    }
    /// <summary>选择分类时清空搜索并从第一页开始。</summary><param name="value">菜单下标。</param>
    private void SelectFilter(int value)
    { filter = value; query = ""; start = 0; parent.nickname.SetTextWithoutNotify(""); parent.addStatus.text = ""; Refresh(); }
    /// <summary>点击换组时重读当前可见角色并循环翻页。</summary>
    private void Next() { start += view.PageSize; parent.addStatus.text = ""; Refresh(); }
    /// <summary>移动键盘完成键和搜索按钮使用相同的查询。</summary><param name="value">输入框刚提交的文字。</param>
    private void Submit(string value) { SearchFriend(); }
    /// <summary>角色号精确匹配，昵称支持子串，空查询返回推荐。</summary>
    private void SearchFriend() { query = parent.nickname.text.Trim(); start = 0; parent.addStatus.text = ""; Refresh(); }
    /// <summary>没有可见结果时，只有玩家再次明确选择添加才发出请求。</summary>
    private void AddExact()
    {
        if (query.Length == 0) return;
        // 完整角色号沿用 UID 通道；其他文字走原有的完整昵称通道。
        int id;
        if (int.TryParse(query, out id) && id > 0) RequestAddFriend(id);
        else { parent.addStatus.text = ""; mgr.RequestAddFriend(query, 0); }
    }
    /// <summary>通过旧接口向刚选中的单个角色提交添加。</summary><param name="id">正数角色号。</param>
    private void RequestAddFriend(int id) { parent.addStatus.text = ""; mgr.RequestAddFriend("", (ulong)id); }
    /// <summary>同步可见玩家和本人已有关系，然后更新排序及当前页。</summary>
    private void Refresh()
    {
        if (!view.isActiveAndEnabled) return;
        candidates.Clear();
        // 当前主城实体与地图切换一起更新；尚未载入场景时显示空态。
        SceneWorldSystem world = Global.Instance?.SceneWorld;
        self = world?.LocalRole?.BaseInfo;
        if (self != null)
        {
            entities.Clear(); world.CopyEntities(entities);
            for (int index = 0; index < entities.Count; index++)
            {
                // 玩家实体的服务端资料，NPC、投射物等没有候选身份。
                RoleBaseInfo info = (entities[index] as RoleEntity)?.BaseInfo;
                if (info == null || info.RoleId <= 0 || info.RoleId == self.RoleId) continue;
                if (info.MapId != self.MapId || info.InstanceId != self.InstanceId) continue;
                if (mgr.Friends.Exists(friend => friend.Added && friend.RoleId == info.RoleId)) continue;
                if (query.Length > 0)
                {
                    if (info.RoleId.ToString() != query && info.Nickname.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                else if (filter == 1 && info.JobId != self.JobId) continue;
                candidates.Add(info);
            }
            candidates.Sort(Compare);
        }
        if (start >= candidates.Count) start = 0;
        view.Show(candidates, start, Reason, RequestAddFriend, query.Length > 0);
        if (query.Length > 0 && candidates.Count == 0)
        {
            // 服务端已经确认的好友按完整昵称或角色号展示关系，不再发出重复的添加请求。
            bool added = mgr.Friends.Exists(friend => friend.Added && (friend.Name == query || friend.RoleId.ToString() == query));
            if (added) { view.empty.text = "对方已在你的好友列表中"; view.exactAdd.gameObject.SetActive(false); }
        }
    }
    /// <summary>修为或等级分类按差值从小到大排序，相同时用稳定角色号排定分页。</summary>
    /// <param name="left">前一个候选。</param><param name="right">后一个候选。</param>
    private int Compare(RoleBaseInfo left, RoleBaseInfo right)
    {
        // 使用长整型差值，保留修为字段的整数精度。
        int comparison;
        if (filter == 3) comparison = Math.Abs((long)left.XiuWei - self.XiuWei).CompareTo(Math.Abs((long)right.XiuWei - self.XiuWei));
        else comparison = Math.Abs((long)left.Level - self.Level).CompareTo(Math.Abs((long)right.Level - self.Level));
        if (comparison != 0) return comparison;
        return left.RoleId.CompareTo(right.RoleId);
    }
    /// <summary>根据本人与目标的真实职业或数值生成行内推荐说明。</summary><param name="role">当前展示的候选角色。</param>
    private string Reason(RoleBaseInfo role)
    {
        if (filter == 3 && query.Length == 0) return "修为差 " + Math.Abs((long)role.XiuWei - self.XiuWei);
        if (filter != 2 && role.JobId > 0 && role.JobId == self.JobId) return "同门玩家";
        if (role.Level == self.Level) return "等级相同";
        return "相差 " + Math.Abs((long)role.Level - self.Level) + " 级";
    }
}
