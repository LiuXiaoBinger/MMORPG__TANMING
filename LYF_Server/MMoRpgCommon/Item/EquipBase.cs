/// <summary>
/// 装备物品数据，在基础物品上增加强化和词条属性。
/// </summary>
public class EquipBase : ItemBase
{
    public EquipBase()
    {
    }

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
    /// 第一个词条配置 ID 和数值。
    /// </summary>
    public int GeneID0 { get; set; }
    public int GeneValue0 { get; set; }

    /// <summary>
    /// 第二个词条配置 ID 和数值。
    /// </summary>
    public int GeneID1 { get; set; }
    public int GeneValue1 { get; set; }

    /// <summary>
    /// 第三个词条配置 ID 和数值。
    /// </summary>
    public int GeneID2 { get; set; }
    public int GeneValue2 { get; set; }
}
