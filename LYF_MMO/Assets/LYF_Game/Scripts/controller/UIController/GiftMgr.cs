using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>对应 RO GiftMgr。读取可赠背包实例，收发 GetGiftLimitInfo 和 GiveGifts 协议。</summary>
public sealed class GiftMgr
{
    /// <summary>好友或添加好友的服务端结果到达。</summary>
    internal event Action<GetGiftFriendsRes> FriendsReceived;
    /// <summary>当前玩家寄出的礼物已结算或被拒绝。</summary>
    internal event Action<GiveGiftsRes> GiftReceived;

    /// <summary>与主城赠礼控制器同生命周期注册两项业务回包。</summary>
    internal GiftMgr()
    {
        SocketDispatcher.Instance.AddEventHandler(MailCode.GetGiftLimitInfo, OnFriends);
        SocketDispatcher.Instance.AddEventHandler(MailCode.GiveGifts, OnGift);
    }

    /// <summary>离开主城时移除网络订阅。</summary>
    internal void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(MailCode.GetGiftLimitInfo, OnFriends);
        SocketDispatcher.Instance.RemoveEventHandler(MailCode.GiveGifts, OnGift);
    }

    /// <summary>按背包格顺序列出实际可赠的未绑定、非限时物品堆。</summary>
    internal List<ItemBase> GetGiftItemList()
    {
        // 当前本地玩家持有的真实背包，进入世界后才有已加载的实例。
        var player = Global.Instance?.SceneWorld?.LocalRole;
        // 返回可寄出的实际物品引用列表，未加载背包时为空。
        var available = new List<ItemBase>();
        if (player != null && player.TryGetComponent(out RoItemComponent bag))
        {
            // 修习丹在普通包，秘籍碎片在秘籍包；仍按赠礼表筛选可寄物品。
            available.AddRange(bag.GetContainer(KnapsackType.RolePackPlain).GetItemMap().Values);
            available.AddRange(bag.GetContainer(KnapsackType.RolePackManual).GetItemMap().Values);
        }
        available.RemoveAll(item => !GiftTable.Contains(item.GetItemID()) || item.GetItemSign() != 0 || item.GetExpireTime() != DateTime.MinValue);
        available.Sort((left, right) => left.GetBagIndex().CompareTo(right.GetBagIndex()));
        return available;
    }

    /// <summary>向中心角色库查询赠礼好友，附带昵称时先添加联系人。</summary><param name="name">空串为仅刷新，同区完整昵称为添加。</param>
    internal bool GetGiftLimitInfo(string name)
    {
        return MNetClient.Instance.Send(MailCode.GetGiftLimitInfo, new GiftFriendArg { Name = name }.ToByteString());
    }

    /// <summary>按 RO 的 GiveGiftsArg 只上传收件人、物品实例及件数。</summary>
    /// <param name="receiver">已添加好友的角色编号。</param><param name="quantities">待赠的真实 UID 与数量。</param>
    internal bool GiveGifts(int receiver, Dictionary<long, int> quantities)
    {
        // 一次赠送对应一封邮件，每个选择的堆叠只出现一次。
        var request = new GiveGiftsArg { RoleUid = receiver };
        // 用实例编号回查每堆的已选件数。
        var uids = new List<long>(quantities.Keys);
        // 每条协议项按同一实例取出其选择数量。
        for (int index = 0; index < uids.Count; index++)
            request.UidItems.Add(new GiftUidItem { ItemUid = uids[index], ItemCount = quantities[uids[index]] });
        return MNetClient.Instance.Send(MailCode.GiveGifts, request.ToByteString());
    }

    /// <summary>解析并交给窗口显示联系人或错误。</summary><param name="data">好友业务回包。</param>
    private void OnFriends(ByteString data) { FriendsReceived?.Invoke(GetGiftFriendsRes.Parser.ParseFrom(data)); }

    /// <summary>解析服务器完成扣包投递后的回执。</summary><param name="data">赠礼业务回包。</param>
    private void OnGift(ByteString data) { GiftReceived?.Invoke(GiveGiftsRes.Parser.ParseFrom(data)); }
}
