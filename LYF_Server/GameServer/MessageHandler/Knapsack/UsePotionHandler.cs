using Google.Protobuf;

/// <summary>实现游戏服消息分派器要求的处理接口，在角色逻辑线程结算一瓶恢复药。</summary>
public sealed class UsePotionHandler : IGameMessageHandler
{
    /// <summary>接收经网关转发的用药请求。</summary>
    /// <param name="source">消息的实际来源。</param><param name="commandCode">网关保留的业务协议号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == RecoveryPotion.UseCommand;
    }

    /// <summary>以当前会话确定角色，按服务器库存、等级和剩余生命或法力结算。</summary>
    /// <param name="context">持有原请求和回包链路的主线程上下文。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 请求仅标识玩家点选的堆叠，恢复量不由客户端指定。
        UsePotionRequest request = UsePotionRequest.Parser.ParseFrom(context.Package.Data);
        // 物品与资源都属于当前网关会话所绑定的角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(context.Package.GateSessionId, context.Package.UnitySessionId);
        // 失败原因按原会话返回到背包和 HUD 共用的提示入口。
        UsePotionReply reply = new UsePotionReply { Message = Use(role, request.ItemUid) };
        context.Transport.Send(context.Package, RecoveryPotion.UseCommand, reply.ToByteString());
    }

    /// <summary>成功返回空消息。用尽堆叠发送移除通知，部分扣除发送新数量。</summary>
    /// <param name="role">服务器当前会话的在线角色。</param><param name="uid">实际要扣减的一叠药的 UID。</param>
    internal static string Use(OnlineRole role, long uid)
    {
        if (role == null || !role.IsRoleDataReady()) return "角色数据尚未就绪";
        // 从当前角色的背包查找实例，其他角色或者已经用尽的堆叠均不可用。
        RoItemComponent bag = role.GetComponent<RoItemComponent>();
        // 这一叠的所有权、数量与扣减均由当前角色的背包维护。
        ItemBase stack = bag.GetItemByUID(uid);
        if (stack == null || stack.GetItemCount() <= 0) return "药品已用完";
        // 以物品编号查单瓶恢复量和使用等级。
        cfg.ItemInfo item = LubanMgr.Instance.GetItemInfoById(stack.GetItemID());
        // mana 表示消耗后增加法力，amount 是表里的单瓶点数。
        if (!RecoveryPotion.TryRead(item, out bool mana, out int amount)) return "该物品无法作为恢复药使用";
        // 战斗时使用装备加成后的上限，当前值读自权威角色资源。
        RoleAttributeComponent attributes = role.GetComponent<RoleAttributeComponent>();
        // 已保存的当前生命、法力和角色等级。
        RoleBaseInfo info = role.mainRoleInfo.BaseInfo;
        if (info.CurrHp <= 0) return "倒地时无法使用药品";
        if (mana && info.CurrMp >= attributes.MaxMp) return "法力已满，无法使用";
        if (!mana && info.CurrHp >= attributes.MaxHp) return "血量已满，无法使用";
        if (info.Level < item.NeedLevel) return "使用此药品需要" + item.NeedLevel + "级";
        // 耗尽走带删除回包的整叠入口，余量走同一 UID 的数量更新。
        ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_USE);
        // 只有权威背包实际扣除成功才恢复资源。
        bool consumed;
        if (stack.GetItemCount() == 1) consumed = bag.DeleteItem(uid, reason);
        else consumed = bag.DeleteItem(uid, 1, reason, UpdateType.eUT_Normal);
        if (!consumed) return "药品已用完";
        if (mana) attributes.ChangeMp(amount);
        else attributes.ChangeHp(amount, role.GetID());
        bag.NotifyItemChanges();
        attributes.NotifyAttributeUpdate();
        return string.Empty;
    }
}
