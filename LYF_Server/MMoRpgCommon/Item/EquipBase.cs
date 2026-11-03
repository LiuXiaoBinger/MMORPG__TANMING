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
