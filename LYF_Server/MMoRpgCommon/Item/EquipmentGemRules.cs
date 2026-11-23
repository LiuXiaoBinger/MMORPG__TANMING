/// <summary>装备镶嵌共用的宝石品种、等级及部位规则，服务器校验与客户端选材使用相同的物品编号。</summary>
public static class EquipmentGemRules
{
    /// <summary>固定孔位从左到右编号 0 至 5；未开孔的装备仍按自身装备表限制。</summary>
    public const int SocketCount = 6;
    /// <summary>每种宝石有十个独立的物品编号，库存和等级随编号区分。</summary>
    public const int GradeCount = 10;
    /// <summary>五种基础石、三种首饰玉和五色水晶、玛瑙、琉璃的品种总数。</summary>
    public const int FamilyCount = 23;
    /// <summary>月光石一级为 8201；每个品种相隔 100，最后两位为等级。</summary>
    private const int BaseId = 8200;

    /// <summary>取得指定品种一级宝石的物品编号。</summary>
    /// <param name="family">按物品表排列的品种，月光石为 0。</param>
    public static int FirstItemId(int family) { return BaseId + family * 100 + 1; }

    /// <summary>从真实物品编号取出品种与等级，排除保留编号和普通材料。</summary>
    /// <param name="itemId">背包物品配置编号。</param>
    /// <param name="family">合法时返回 0 至 22 的品种。</param>
    /// <param name="grade">合法时返回 1 至 10 的等级。</param>
    public static bool TryRead(int itemId, out int family, out int grade)
    {
        family = -1;
        grade = 0;
        if (itemId <= BaseId || itemId > FirstItemId(FamilyCount - 1) + GradeCount - 1) return false;
        family = (itemId - BaseId) / 100;
        grade = (itemId - BaseId) % 100;
        return grade >= 1 && grade <= GradeCount;
    }

    /// <summary>按装备实际部位筛选宝石；水晶为攻击、玛瑙为抗性、琉璃为穿透。</summary>
    /// <param name="part">装备配置中的部位，主副武器使用同一组。</param>
    /// <param name="family">0 至 4 为体劲气御敏，5 至 7 为紫牙玉、梅花玉和血玉。</param>
    public static bool Allows(EquipType part, int family)
    {
        // 三种五色宝石按水、毒、火、风、元依次排列。
        bool crystal = family >= 8 && family < 13;
        bool agate = family >= 13 && family < 18;
        bool glaze = family >= 18 && family < FamilyCount;
        // 武器按内外功选择劲或气，护腕可选体、御。
        bool weaponStone = family == 1 || family == 2;
        bool bracerStone = family == 0 || family == 3;
        switch (part)
        {
            case EquipType.MainWeapon:
            case EquipType.OffhandWeapon: return weaponStone || crystal;
            case EquipType.Head:
            case EquipType.Chest: return family == 0 || agate;
            case EquipType.Pants:
            case EquipType.Shoes: return family == 3 || agate;
            case EquipType.WaistWaist: return bracerStone || weaponStone || crystal;
            case EquipType.Bracer: return bracerStone || family == 4 || crystal;
            case EquipType.Necklace: return family == 6 || family == 7 || glaze;
            case EquipType.Ring: return family == 5 || family == 7 || glaze;
            case EquipType.Bracelet:
            case EquipType.JadePendant: return family == 5 || family == 6 || glaze;
            default: return false;
        }
    }

    /// <summary>常规合成最高七级；八至十级另需特殊材料。</summary>
    public const int ComposeCap = 7;
    /// <summary>商会只提供一、二、三级材料。</summary>
    public const int MarketGrades = 3;
    /// <summary>每个角色对每种、每级商会宝石的独立配额。</summary>
    public const int MarketLimit = 900;

    /// <summary>按实际部位和从左起的孔号选择基础、元素两类孔；已有镶嵌仍按存档展示。</summary>
    /// <param name="part">装备配置中的部位。</param>
    /// <param name="slot">左起零基孔位，一、二、五孔为基础属性孔。</param>
    /// <param name="family">物品编号解析得到的宝石品种。</param>
    public static bool AllowsInSocket(EquipType part, int slot, int family)
    {
        if (slot < 0 || slot >= SocketCount || !Allows(part, family)) return false;
        // 前八种石、玉提升基础属性，其余是元素攻击、抗性及穿透。
        bool basic = slot == 0 || slot == 1 || slot == 4;
        return basic == (family < 8);
    }

    /// <summary>同品种的两颗低一级宝石折一颗高一级宝石，以一级为一份。</summary>
    /// <param name="grade">一至七级的有效合成材料或目标等级。</param>
    public static int Units(int grade) { return 1 << (grade - 1); }

    /// <summary>商会一、二、三级宝石分别定价六万、十二万和二十四万铜币。</summary>
    /// <param name="grade">一至三级的商会商品等级。</param>
    public static int MarketPrice(int grade) { return 60000 * Units(grade); }
}
