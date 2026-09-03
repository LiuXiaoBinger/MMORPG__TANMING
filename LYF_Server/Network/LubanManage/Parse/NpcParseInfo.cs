using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using cfg;

/// <summary>
/// 解析后的 NPC 配置数据。
/// </summary>
public class NpcParseInfo
{
    /// <summary>
    /// NPC 配置 ID。
    /// </summary>
    public int ID { get; private set; }

    /// <summary>
    /// NPC 名称。
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// NPC 预制体路径。
    /// </summary>
    public string PrefabPath { get; private set; }

    /// <summary>
    /// NPC 世界坐标。
    /// </summary>
    public Vector3 Position { get; private set; }

    /// <summary>
    /// NPC 所在地图 ID。
    /// </summary>
    public int MapID { get; private set; }

    /// <summary>
    /// NPC 类型。
    /// </summary>
    public int Type { get; private set; }

    /// <summary>
    /// NPC 出售的商品及限购配置。
    /// </summary>
    public List<NpcShopItem> ShopItemList { get; private set; }

    /// <summary>
    /// NPC 自言自语内容。
    /// </summary>
    public string Think { get; private set; }

    /// <summary>
    /// NPC 交谈内容。
    /// </summary>
    public string Talk { get; private set; }

    /// <summary>
    /// 将 Luban NPC 配置转换为运行时数据。
    /// </summary>
    public static NpcParseInfo Create(NpcInfo npcInfo)
    {
        if (npcInfo == null)
        {
            return null;
        }

        return new NpcParseInfo
        {
            ID = npcInfo.Id,
            Name = npcInfo.Name,
            PrefabPath = npcInfo.PrefabPath,
            Position = ParsePosition(npcInfo.Pos),
            MapID = npcInfo.Mapid,
            Type = npcInfo.Tyep,
            ShopItemList = ParseShopItemList(npcInfo.ItemList),
            Think = npcInfo.Think,
            Talk = npcInfo.Talk,
        };
    }

    private static Vector3 ParsePosition(string positionText)
    {
        if (string.IsNullOrWhiteSpace(positionText))
        {
            return Vector3.Zero;
        }

        string[] values = positionText.Split('_');
        if (values.Length != 3)
        {
            return Vector3.Zero;
        }

        float x;
        float y;
        float z;
        if (!float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
            || !float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
            || !float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
        {
            return Vector3.Zero;
        }

        return new Vector3(x, y, z);
    }

    private static List<NpcShopItem> ParseShopItemList(string itemListText)
    {
        List<NpcShopItem> shopItemList = new List<NpcShopItem>();
        if (string.IsNullOrWhiteSpace(itemListText))
        {
            return shopItemList;
        }

        string[] itemTexts = itemListText.Split('_');
        foreach (string itemText in itemTexts)
        {
            string[] values = itemText.Split(':');
            if (values.Length != 3)
            {
                continue;
            }

            int shopID;
            int limitType;
            int limitCount;
            if (!int.TryParse(values[0], out shopID)
                || !int.TryParse(values[1], out limitType)
                || !int.TryParse(values[2], out limitCount))
            {
                continue;
            }

            shopItemList.Add(new NpcShopItem
            {
                ShopID = shopID,
                LimitType = (ShopLimitType)limitType,
                LimitCount = limitCount,
            });
        }

        return shopItemList;
    }
}

/// <summary>
/// NPC 商店商品限购类型。
/// </summary>
public enum ShopLimitType
{
    /// <summary>
    /// 每日限购。
    /// </summary>
    Daily = 1,

    /// <summary>
    /// 永久限购。
    /// </summary>
    Permanent = 2,

    /// <summary>
    /// 不限购。
    /// </summary>
    Unlimited = 3,

    /// <summary>
    /// 每周限购。
    /// </summary>
    Weekly = 4,

    /// <summary>
    /// 每月限购。
    /// </summary>
    Monthly = 5,

    /// <summary>
    /// 每年限购。
    /// </summary>
    Yearly = 6,
}

/// <summary>
/// NPC 商店商品及限购配置。
/// </summary>
public class NpcShopItem
{
    /// <summary>
    /// 商店商品配置 ID，对应 ShopInfo.Id。
    /// </summary>
    public int ShopID { get; set; }

    /// <summary>
    /// 限购类型。
    /// </summary>
    public ShopLimitType LimitType { get; set; }

    /// <summary>
    /// 限购数量；不限购时为 0。
    /// </summary>
    public int LimitCount { get; set; }
}
