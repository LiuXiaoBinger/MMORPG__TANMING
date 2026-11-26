using System;
using System.Collections.Generic;

/// <summary>对应 RO GiftCtrl。处理背包赠礼入口、好友选择和礼物数量，协议与物品查询交给 GiftMgr。</summary>
public sealed class GiftCtrl
{
    /// <summary>与当前窗口共用生命周期的 RO 赠礼数据和协议入口。</summary>
    private readonly GiftMgr mgr = new GiftMgr();
    /// <summary>窗口所属的正式主城视图。</summary>
    private readonly MainView view;
    /// <summary>当前实例的赠礼窗口。</summary>
    private readonly GiftPanel panel;
    /// <summary>已选实际物品 UID 及本次要寄出的件数。</summary>
    private readonly Dictionary<long, int> quantities = new Dictionary<long, int>();
    /// <summary>最近一次好友查询结果。</summary>
    private GetGiftFriendsRes friends = new GetGiftFriendsRes();
    /// <summary>当前收件好友的角色编号，零代表尚未选择。</summary>
    private int receiver;
    /// <summary>已发出、等待服务端扣包投递回执的当前操作。</summary>
    private bool sending;

    /// <summary>连接背包赠送按钮、正式窗口和两个 RO 业务回包。</summary><param name="main">持有背包与赠礼窗口的主城视图。</param>
    internal GiftCtrl(MainView main)
    {
        view = main;
        panel = main.giftPanel;
        view.KnapsackWindow.giftButton.onClick.AddListener(Open);
        panel.addFriend.onClick.AddListener(AddFriend);
        panel.send.onClick.AddListener(GiveGifts);
        mgr.FriendsReceived += OnFriends;
        mgr.GiftReceived += OnSent;
    }

    /// <summary>主城释放时移除赠礼窗口和网络回调。</summary>
    internal void Dispose()
    {
        view.KnapsackWindow.giftButton.onClick.RemoveListener(Open);
        panel.addFriend.onClick.RemoveListener(AddFriend);
        panel.send.onClick.RemoveListener(GiveGifts);
        mgr.FriendsReceived -= OnFriends;
        mgr.GiftReceived -= OnSent;
        mgr.Dispose();
    }

    /// <summary>从背包进入赠礼，重新读取当前角色的可赠实例与好友列表。</summary>
    private void Open()
    {
        view.ShowMainWindow(WindowType.GiftPanel);
        receiver = 0;
        quantities.Clear();
        friends = new GetGiftFriendsRes();
        panel.status.text = "";
        panel.nickname.text = "";
        RenderFriends();
        RenderBag();
        RequestFriends("");
    }

    /// <summary>用玩家输入的角色名保存为好友。</summary>
    private void AddFriend()
    {
        // 保留名字中间字符，清掉两端输入空格。
        string name = panel.nickname.text.Trim();
        if (name.Length == 0) { panel.status.text = "请输入好友的完整角色名"; return; }
        RequestFriends(name);
    }

    /// <summary>输入昵称时添加；空串时只刷新。</summary><param name="name">指定同区角色昵称。</param>
    private void RequestFriends(string name)
    {
        if (!mgr.GetGiftLimitInfo(name))
            panel.status.text = "暂时无法连接赠礼服务";
    }

    /// <summary>好友列表只使用服务端返回的真实角色编号。</summary><param name="result">查询或添加好友的回包。</param>
    private void OnFriends(GetGiftFriendsRes result)
    {
        // 失败时允许更正昵称并保留当前好友选择。
        if (result.Error.Length > 0) { panel.status.text = result.Error; return; }
        friends = result;
        panel.nickname.text = "";
        panel.status.text = "";
        RenderFriends();
        RenderBag();
    }

    /// <summary>已添加的好友可选为收礼人，添加过自己的角色可点击回加。</summary>
    private void RenderFriends()
    {
        // 行引用与服务器的排序一一对应。
        List<GiftRow> rows = panel.FriendRows(friends.Friends.Count);
        // 当前行与本次查询返回的联系人同序。
        for (int index = 0; index < friends.Friends.Count; index++)
        {
            // 每行捕获自己的联系人，避免按钮引用后续行。
            GiftFriendInfo contact = friends.Friends[index];
            if (contact.Added)
            {
                if (receiver == 0) receiver = contact.RoleId;
                // 关系标签区分单向添加与已回加。
                string detail = "好友";
                if (contact.AddedMe) detail = "互为好友";
                rows[index].Bind(contact.Name, detail, receiver == contact.RoleId,
                    () => { receiver = contact.RoleId; RenderFriends(); RenderBag(); }, null);
            }
            else rows[index].Bind(contact.Name, "点击加回", false, () => RequestFriends(contact.Name), null);
        }
    }

    /// <summary>当前赠礼配置中的实体道具，均位于普通背包；按背包格顺序显示实例。</summary>
    private void RenderBag()
    {
        // 由礼物管理器按真实背包格序取得符合配置的实例。
        List<ItemBase> available = mgr.GetGiftItemList();
        // 清除已从包中移走的实例，按现有件数收缩旧选择。
        var previous = new Dictionary<long, int>(quantities);
        quantities.Clear();
        // 本次显示各堆的可复用行。
        List<GiftRow> rows = panel.ItemRows(available.Count);
        // 所有可见堆叠的已选件数之和。
        long total = 0;
        // 按背包格序将每个实际实例绑定到对应行。
        for (int index = 0; index < available.Count; index++)
        {
            // UID 在物品移动格子时仍表示同一堆。
            ItemBase item = available[index];
            // 当前物品在客户端与游戏服共用的实例标识。
            long uid = item.GetItemUID();
            // 该实例现存件数，作为输入和加号的上限。
            int held = item.GetItemCount();
            // 取回此实例在上次绘制时选中的件数。
            previous.TryGetValue(uid, out int count);
            count = Math.Min(count, held);
            if (count > 0) quantities[uid] = count;
            total += count;
            // 已到上限或下限时只禁用对应按钮。
            Action plus = null;
            // 减一回调仅在已有寄出件数时有效。
            Action minus = null;
            if (count < held) plus = () => { quantities[uid] = count + 1; RenderBag(); };
            if (count > 0) minus = () => { quantities[uid] = count - 1; RenderBag(); };
            // 名称和图标与背包物品格读取同一配置。
            ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(item.GetItemID());
            rows[index].Bind(config.ItemName, "持有 " + held, count > 0, plus, minus);
            rows[index].SetItem(config.Icon, count, value => { quantities[uid] = Math.Max(0, Math.Min(held, value)); RenderBag(); });
        }
        panel.total.text = "已选 " + total + " 件";
        panel.send.interactable = total > 0 && receiver > 0 && !sending;
    }

    /// <summary>发送已选真实物品 UID 与件数，由服务端回执确认实际入库。</summary>
    private void GiveGifts()
    {
        sending = mgr.GiveGifts(receiver, quantities);
        if (!sending) panel.status.text = "暂时无法连接赠礼服务";
        RenderBag();
    }

    /// <summary>只有扣包与投递已共同入库才展示寄出成功。</summary><param name="result">实际赠礼的服务端回执。</param>
    private void OnSent(GiveGiftsRes result)
    {
        // 服务端错误沿用原选项，玩家可调整后重新寄出。
        sending = false;
        panel.status.text = result.Error;
        if (result.Error.Length == 0) { quantities.Clear(); panel.status.text = "礼物已寄到好友邮箱"; }
        RenderBag();
    }
}
