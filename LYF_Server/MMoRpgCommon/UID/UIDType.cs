/// <summary>
/// UID 的业务类型，占用 UID 最高有效位中的 3 个比特。
/// </summary>
public enum UIDType
{
    UID_Item = 0,
    UID_Shop = 1,
    UID_Reward = 2,
    UID_Pet = 3,
    UID_Mail = 4,
    // 当业务类型不足时统一使用此类型，不额外占用类型位。
    UID_Common = 5,
    UID_Max = ((1 << 3) - 1),
}
