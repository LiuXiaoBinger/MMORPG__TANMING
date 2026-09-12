namespace MMoRpgCommon
{

/// <summary>
/// NPC 实体。
/// </summary>
public class NpcEntity : global::EntityBase
{
    public int NpcID { get; set; }
    public int NpcType { get; set; }
    public int DialogueID { get; set; }
    public string PrefabPath { get; set; } = string.Empty;
    public string Think { get; set; } = string.Empty;
    public string Talk { get; set; } = string.Empty;
    /// <summary>
    /// NPC 关联的商店主表 ID；商品明细由 ShopItemInfo.ShopId 关联。
    /// </summary>
    public int ShopId { get; set; }
}
}
