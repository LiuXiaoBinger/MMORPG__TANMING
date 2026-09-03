using System.Collections.Generic;

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
    public List<NpcShopData> ShopItemList { get; set; } = new List<NpcShopData>();
}

/// <summary>
/// NPC 商城条目。使用基础数据类型，避免公共实体依赖客户端或配置程序集。
/// </summary>
public class NpcShopData
{
    public int ShopID { get; set; }
    public int LimitType { get; set; }
    public int LimitCount { get; set; }
}
}
