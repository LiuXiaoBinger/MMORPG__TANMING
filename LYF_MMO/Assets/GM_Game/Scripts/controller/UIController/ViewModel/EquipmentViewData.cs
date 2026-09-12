/// <summary>
/// 装备槽展示数据，装备配置解析在角色信息控制器中完成。
/// </summary>
public sealed class EquipmentViewData
{
    /// <summary>装备所在部位。</summary>
    public EquipType EquipType { get; }
    /// <summary>装备物品配置编号。</summary>
    public int ItemTypeId { get; }
    /// <summary>装备显示名称。</summary>
    public string Name { get; }
    /// <summary>装备图标路径。</summary>
    public string IconPath { get; }
    /// <summary>装备当前强化等级。</summary>
    public int StrengthenLevel { get; }

    /// <summary>创建装备槽展示数据。</summary>
    public EquipmentViewData(EquipType equipType, int itemTypeId, string name,
        string iconPath, int strengthenLevel)
    {
        EquipType = equipType;
        ItemTypeId = itemTypeId;
        Name = name;
        if (Name == null)
        {
            Name = string.Empty;
        }
        IconPath = iconPath;
        if (IconPath == null)
        {
            IconPath = string.Empty;
        }
        StrengthenLevel = strengthenLevel;
    }
}
