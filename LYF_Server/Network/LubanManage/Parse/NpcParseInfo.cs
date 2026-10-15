using System;
using System.Globalization;
using System.Numerics;
using cfg;

/// <summary>
/// 解析后的 NPC 配置数据。
/// </summary>
public class NpcParseInfo
{
    /// <summary>NPC 配置 ID。</summary>
    public int ID { get; private set; }
    /// <summary>NPC 显示名称。</summary>
    public string Name { get; private set; }
    /// <summary>NPC 预制体资源路径。</summary>
    public string PrefabPath { get; private set; }
    /// <summary>NPC 在场景中的出生坐标。</summary>
    public Vector3 Position { get; private set; }

    /// <summary>
    /// NPC 在场景中的 Y 轴朝向（度）。Pos 的第 4 段为空或无效时使用 0。
    /// </summary>
    public float RotationY { get; private set; }
    /// <summary>NPC 所在地图 ID。</summary>
    public int MapID { get; private set; }
    /// <summary>NPC 类型（配置 Tyep 字段）。</summary>
    public int Type { get; private set; }

    /// <summary>
    /// NPC 关联的商店主表 ID，对应 ShopTable.Id；商品明细通过 ShopItemInfo.ShopId 查询。
    /// </summary>
    public int ShopId { get; private set; }

    /// <summary>NPC 思考/行为描述文本。</summary>
    public string Think { get; private set; }
    /// <summary>NPC 对话文本。</summary>
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
            Position = ParsePosition(npcInfo.Pos, out float rotationY),
            RotationY = rotationY,
            MapID = npcInfo.Mapid,
            Type = npcInfo.Tyep,
            ShopId = npcInfo.ShopId,
            Think = npcInfo.Think,
            Talk = npcInfo.Talk,
        };
    }

    private static Vector3 ParsePosition(string positionText, out float rotationY)
    {
        rotationY = 0f;
        if (string.IsNullOrWhiteSpace(positionText))
        {
            return Vector3.Zero;
        }

        string[] values = positionText.Split('_');
        // 配置允许追加朝向字段，坐标只取前三段，避免因第四段存在而丢失位置。
        if (values.Length < 3)
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

        if (values.Length > 3)
        {
            float.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out rotationY);
        }

        return new Vector3(x, y, z);
    }
}
