/// <summary>
/// 装备物品数据，在基础物品上增加强化和词条属性。
/// </summary>
public class EquipBase : ItemBase
{
    /// <summary>创建新的装备实例，由物品基类生成新的物品唯一编号。</summary>
    public EquipBase()
    {
    }

    /// <summary>按新建或恢复场景决定是否分配物品唯一编号。</summary>
    /// <param name="generateUid">新装备为 true；从存档恢复并沿用原编号时为 false。</param>
    private EquipBase(bool generateUid)
        : base(generateUid)
    {
    }

    /// <summary>创建不生成新 UID 的装备对象，仅用于恢复权威持久化数据。</summary>
    public static EquipBase CreateEquipForRestore()
    {
        return new EquipBase(false);
    }

    /// <summary>
    /// 装备类别。
    /// </summary>
    public int EquipType { get; set; }

    /// <summary>
    /// 装备强化等级。
    /// </summary>
    public int StrengthenLevel { get; set; }

    /// <summary>第一个装备特技的配置族编号，0 表示该槽位为空。</summary>
    public int SpecialSkillId0 { get; set; }
    /// <summary>第二个装备特技的配置族编号，0 表示该槽位为空。</summary>
    public int SpecialSkillId1 { get; set; }
    /// <summary>第三个装备特技的配置族编号，0 表示该槽位为空。</summary>
    public int SpecialSkillId2 { get; set; }
    /// <summary>当前启用的特技槽位，下标范围为 0 到 2，默认使用第一个槽位。</summary>
    public int ActiveSpecialSkillIndex { get; set; }

    /// <summary>
    /// 已开启的特技孔数量，取值 0 到 3；新装备为 0，只能按 0/1/2 顺序逐个开启。
    /// 该计数与六个宝石孔完全独立，只决定哪几个特技槽可以被安装和启用。
    /// </summary>
    public int OpenedSpecialSkillCount { get; set; }

    /// <summary>是否还能再开一个特技孔；三个孔全开时返回 false。</summary>
    public bool CanOpenSpecialSkillSlot()
    {
        return OpenedSpecialSkillCount < MaxSpecialSkillSlotCount;
    }

    /// <summary>指定槽位是否已经开孔；未开孔的槽不能安装或启用特技。</summary>
    /// <param name="index">特技槽位下标，范围为 0 到 2。</param>
    /// <returns>槽位在已开孔范围内返回 true。</returns>
    public bool IsSpecialSkillSlotOpened(int index)
    {
        if (index < 0)
        {
            return false;
        }
        return index < OpenedSpecialSkillCount;
    }

    /// <summary>特技孔数量上限，与客户端的三个特技卡片一一对应。</summary>
    public const int MaxSpecialSkillSlotCount = 3;

    /// <summary>
    /// 开启一个特技孔需要的铜币数量，项目暂定值。服务端扣费与客户端展示共用这一处常量，
    /// 避免两边价格不一致；单位是铜币（ItemEnum.ZENY）。
    /// </summary>
    public const int SpecialSkillSlotOpenCost = 1000;

    /// <summary>
    /// 判断装备部位是否落在特技族允许的部位掩码内。掩码按 1&lt;&lt;EquipType 累加，
    /// 服务端装入与启用、客户端展示都使用同一判定，避免两边标准不一致。
    /// </summary>
    /// <param name="allowedMask">配置里的允许部位掩码，0 表示不允许任何部位。</param>
    /// <param name="equipType">装备实例的部位枚举值。</param>
    /// <returns>部位被允许时返回 true。</returns>
    public static bool IsSpecialSkillPartAllowed(int allowedMask, int equipType)
    {
        // 部位值超出枚举范围时按不允许处理，避免移位溢出误判。
        if (equipType < 0)
        {
            return false;
        }
        if (equipType > 31)
        {
            return false;
        }
        // 掩码为 0 表示配置未开放任何部位，不能默认放行。
        if (allowedMask == 0)
        {
            return false;
        }
        return (allowedMask & (1 << equipType)) != 0;
    }

    /// <summary>按槽位读取装备实例保存的特技编号。</summary>
    /// <param name="index">特技槽位下标，范围为 0 到 2；其他值返回 0。</param>
    public int GetSpecialSkillId(int index)
    {
        switch (index)
        {
            case 0: return SpecialSkillId0;
            case 1: return SpecialSkillId1;
            case 2: return SpecialSkillId2;
            default: return 0;
        }
    }

    /// <summary>读取唯一启用的特技编号；启用槽为空时返回 0。</summary>
    public int GetActiveSpecialSkillId()
    {
        return GetSpecialSkillId(ActiveSpecialSkillIndex);
    }

    /// <summary>
    /// 第一个词条使用的配置编号，实际数值由 GeneValue0 保存。
    /// </summary>
    public int GeneID0 { get; set; }
    /// <summary>第一个词条的实际数值。</summary>
    public int GeneValue0 { get; set; }

    /// <summary>
    /// 第二个词条使用的配置编号，实际数值由 GeneValue1 保存。
    /// </summary>
    public int GeneID1 { get; set; }
    /// <summary>第二个词条的实际数值。</summary>
    public int GeneValue1 { get; set; }

    /// <summary>
    /// 第三个词条使用的配置编号，实际数值由 GeneValue2 保存。
    /// </summary>
    public int GeneID2 { get; set; }
    /// <summary>第三个词条的实际数值。</summary>
    public int GeneValue2 { get; set; }
}
