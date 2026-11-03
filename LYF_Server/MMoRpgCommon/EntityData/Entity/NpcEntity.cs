namespace MMoRpgCommon
{

/// <summary>
/// NPC 实体。
/// </summary>
public class NpcEntity : global::EntityBase
{
    /// <summary>NPC 的业务编号，用于区分配置或生成的 NPC 数据。</summary>
    public int NpcID { get; set; }
    /// <summary>NPC 分类值，与配置中的类型字段对应。</summary>
    public int NpcType { get; set; }
    /// <summary>NPC 关联的对话配置编号。</summary>
    public int DialogueID { get; set; }
    /// <summary>客户端创建 NPC 外观时使用的预制体资源路径。</summary>
    public string PrefabPath { get; set; } = string.Empty;
    /// <summary>配置提供的思考或行为描述文本。</summary>
    public string Think { get; set; } = string.Empty;
    /// <summary>配置提供的 NPC 对话文本。</summary>
    public string Talk { get; set; } = string.Empty;
    /// <summary>
    /// NPC 关联的商店主表 ID；商品明细由 ShopItemInfo.ShopId 关联。
    /// </summary>
    public int ShopId { get; set; }
}
}
