/// <summary>
/// UID 的业务类型，占用 UID 最高有效位中的 3 个比特。
/// </summary>
public enum UIDType
{
    /// <summary>物品实例的唯一编号类别。</summary>
    UID_Item = 0,
    /// <summary>商店相关对象的唯一编号类别。</summary>
    UID_Shop = 1,
    /// <summary>奖励记录的唯一编号类别。</summary>
    UID_Reward = 2,
    /// <summary>宠物相关对象的唯一编号类别。</summary>
    UID_Pet = 3,
    /// <summary>邮件记录的唯一编号类别。</summary>
    UID_Mail = 4,
    // 当业务类型不足时统一使用此类型，不额外占用类型位。
    UID_Common = 5,
    /// <summary>三位类别字段能表示的上界，不可作为业务类别生成编号。</summary>
    UID_Max = ((1 << 3) - 1),
}
