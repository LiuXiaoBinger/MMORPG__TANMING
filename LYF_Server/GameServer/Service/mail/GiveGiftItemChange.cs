using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>对应 RO 游戏服 GiveGiftItemChange，按物品实例扣包并随该版背包投递礼物邮件。</summary>
public sealed class GiveGiftItemChange : RoleComponentBase
{
    /// <summary>已扣除、等待与下一版角色背包共同入库的礼物。</summary>
    private readonly List<GiftMailPost> pending = new List<GiftMailPost>();
    /// <summary>已交给在途存档的收件人，提交成功后再发新信通知。</summary>
    private readonly List<int> saving = new List<int>();

    /// <summary>在登录阶段绑定发件角色。</summary><param name="owner">持有物品的在线角色。</param>
    public GiveGiftItemChange(OnlineRole owner) : base(owner) { }

    /// <summary>在同一角色线程先核对全部实例，再扣除所选件数并安排投递。</summary>
    /// <param name="call">MS 已确认的收件好友、署名模板与实际选择。</param>
    internal void Give(MailCall call)
    {
        // 权威背包与 RO 一样按 UID 区分同种物品的不同堆。
        RoItemComponent bag = Owner.GetComponent<RoItemComponent>();
        // 按请求顺序保留通过检查的原始堆叠引用。
        var selected = new List<ItemBase>();
        // 本封礼物已检查的 UID，用于拒绝相同堆叠重复计数。
        var uids = new HashSet<long>();
        // 核对整封清单之后才开始扣除第一堆。
        for (int index = 0; index < call.Gift.UidItems.Count; index++)
        {
            // 客户端只指定扣哪一堆和多少件。
            GiftUidItem pick = call.Gift.UidItems[index];
            // 按发件人背包的实际实例取得配置、数量和状态。
            ItemBase item = bag.GetItemByUID(pick.ItemUid);
            if (item == null || !uids.Add(pick.ItemUid) || !GiftTable.Contains(item.GetItemID()))
            { Reply("所选物品不存在或不可赠送"); return; }
            if (item.GetItemSign() != 0 || item.GetExpireTime() != DateTime.MinValue)
            { Reply("绑定或限时物品不能赠送"); return; }
            if (pick.ItemCount < 1 || pick.ItemCount > item.GetItemCount())
            { Reply("所选物品数量不足"); return; }
            selected.Add(item);
        }
        if (selected.Count == 0) { Reply("请选择可赠物品"); return; }
        // 扣除与前一轮验证顺序一致的数量。
        for (int index = 0; index < selected.Count; index++)
        {
            // 保留未寄出部分的原摊位成本；收件时以绑定附件重新入包。
            ItemBase item = selected[index];
            // 当前通过验证的寄出件数。
            int count = call.Gift.UidItems[index].ItemCount;
            // 依寄出比例从原堆成本中扣除的金额。
            long removedPrice = (long)((decimal)item.GetTotalPrice() * count / item.GetItemCount());
            item.SetTotalPrice(item.GetTotalPrice() - removedPrice);
            bag.DeleteItem(item.GetItemUID(), count, new ChangeReason(), UpdateType.eUT_Normal);
            call.SendMail.Content.MailItems.Add(new MailItem { ItemId = item.GetItemID(), Count = count, IsBind = true });
        }
        pending.Add(new GiftMailPost { ReceiverId = call.Gift.RoleUid, Mail = call.SendMail });
        Owner.SaveMailAssets();
    }

    /// <summary>有已扣除的礼物时随角色存档提交。</summary>
    public override bool NeedSave() { return pending.Count > 0; }

    /// <summary>把礼物附入本版背包快照，沿角色发送器已有的重发和确认流程处理。</summary>
    /// <param name="fieldManager">各资产组件共用的存档。</param><param name="saveVersion">该次存档版本。</param>
    public override bool Save(RoleDataFieldManager fieldManager, long saveVersion)
    {
        fieldManager.GetRequest().GiftPosts.Add(pending);
        // 为每次待确认寄送保留其收件角色号。
        for (int index = 0; index < pending.Count; index++) saving.Add(pending[index].ReceiverId);
        pending.Clear();
        return true;
    }

    /// <summary>收件记录与扣包共同提交后提示双方；离线收件人登录时从同一邮箱读取。</summary>
    /// <param name="saveVersion">角色存档发送器已确认的版本。</param>
    public override void OnSaveAcknowledged(long saveVersion)
    {
        // 按本次已入库的赠礼逐封回执并通知收件人。
        for (int index = 0; index < saving.Count; index++)
        {
            if (Owner.State == OnlineRoleState.Online) Reply("");
            MasterTeamLink.Instance.SendToMaster(new BasePackage { ProtoCode = MailCode.GiftArrived,
                Data = new MailCall { RoleId = saving[index] }.ToByteString() });
        }
        saving.Clear();
    }

    /// <summary>将寄出结果返回发件角色。</summary><param name="error">空串表示已实际寄出。</param>
    private void Reply(string error) { Owner.SendToClient(MailCode.GiveGifts, new GiveGiftsRes { Error = error }.ToByteString()); }
}
