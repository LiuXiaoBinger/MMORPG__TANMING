using System.Collections.Generic;

/// <summary>魔剑士金装的单件、单步配方，由作坊预览和角色服扣料共用。</summary>
public sealed class GoldWorkshopRecipe
{
    /// <summary>辉银主手的物品编号，也是八个装备部位的起点。</summary>
    public const int FirstItem = 21101;
    /// <summary>辉银鞋履的物品编号。</summary>
    public const int LastItem = 21108;
    /// <summary>主手、副手、头、衣、腰、裤、腕、鞋对应的部位专材。</summary>
    private static readonly int[] Parts = { 10704, 10711, 10705, 10706, 10707, 10708, 10709, 10710 };
    /// <summary>每个部位进二阶的主材基数，副手逐阶资料缺失。</summary>
    private static readonly int[] Bases = { 20, 0, 18, 18, 14, 14, 25, 30 };
    /// <summary>目标二阶至十阶的部位专材、主材倍率。</summary>
    private static readonly int[] Multipliers = { 1, 2, 3, 4, 5, 6, 8, 10, 12 };
    /// <summary>从二阶到十阶分别消耗的风灵翠羽数量，对应材料 10703。</summary>
    private static readonly int[] Glazes = { 30, 60, 100, 150, 200, 300, 500, 800, 1000 };
    /// <summary>逐阶铜钱，资料原表单位为万。</summary>
    private static readonly int[] Fees = { 75, 150, 300, 600, 1200, 2000, 3000, 5250, 8000 };
    /// <summary>四行对应主手、头衣、腰裤、腕鞋，十列对应目标70至160级。</summary>
    private static readonly int[,] LevelMaterials = {
        { 1, 2, 3, 5, 7, 10, 15, 3, 3, 3 }, { 1, 2, 3, 5, 7, 10, 15, 5, 5, 5 },
        { 1, 1, 2, 2, 3, 5, 7, 2, 2, 2 }, { 1, 3, 5, 7, 10, 15, 20, 7, 7, 7 }
    };
    /// <summary>本次需要的材料编号和件数，不含货币。</summary>
    public readonly List<ItemDesc> Materials = new List<ItemDesc>();
    /// <summary>本步实际收取的铜钱数，按枚计。</summary>
    public int Copper { get; private set; }
    /// <summary>本步完成后显示的装备等级或阶数。</summary>
    public int Target { get; private set; }
    /// <summary>无法执行时向玩家说明缺方或已满的原因。</summary>
    public string Unavailable { get; private set; } = "";

    /// <summary>检查物品是否属于作坊使用的辉银八件装备。</summary>
    /// <param name="id">装备配置编号。</param>
    public static bool IsGold(int id) { return id >= FirstItem && id <= LastItem; }

    /// <summary>从存档档数换算装备的实际等级，旧实例默认60级。</summary>
    /// <param name="steps">已完成的十级升级次数。</param>
    public static int Level(int steps) { return 60 + 10 * steps; }

    /// <summary>从当前实例进度取得下一步材料，缺失的配方不接受提交。</summary>
    /// <param name="operation">合成、进阶或升级。</param>
    /// <param name="id">金装物品配置编号。</param>
    /// <param name="levelSteps">实例已升的档数，0至10。</param>
    /// <param name="tierSteps">实例已进的阶数，0至9。</param>
    public static GoldWorkshopRecipe Get(EquipOperation operation, int id, int levelSteps, int tierSteps)
    {
        // 新清单只包含本次确认要扣的材料。
        GoldWorkshopRecipe recipe = new GoldWorkshopRecipe();
        if (!IsGold(id)) { recipe.Unavailable = "请选择魔剑士辉银金装"; return recipe; }
        // 连续编号的位置与部位专材表同序。
        int slot = id - FirstItem;
        // 主副手使用狮鹫金羽，六件防具使用月光珍珠。
        int primary = 10701;
        if (slot < 2) primary = 10702;
        if (operation == EquipOperation.ForgeGold)
        {
            recipe.Target = 60;
            if (slot == 0) { recipe.Unavailable = "主手合成配方待补"; return recipe; }
            // 副手初合数量来自录屏，防具取各部位首合表。
            int amount = Bases[slot];
            if (slot == 1) amount = 10;
            recipe.Materials.Add(new ItemDesc(primary, amount));
            recipe.Materials.Add(new ItemDesc(Parts[slot], 5));
            // 这两件的用料已有记录，首合工费尚缺，继续展示材料供预览。
            if (slot == 5 || slot == 6) recipe.Unavailable = "首次合成工费待核";
            else recipe.Copper = 100000;
        }
        else if (slot == 1 && operation == EquipOperation.UpgradeGold) recipe.Unavailable = "副手成长配方待补";
        else if (operation == EquipOperation.AdvanceGold)
        {
            if (tierSteps < 0 || tierSteps >= Multipliers.Length) { recipe.Unavailable = "已达最高十阶"; return recipe; }
            recipe.Target = tierSteps + 2;
            // 副手逐阶主材以主手的二阶基数 20 为项目定值。
            int baseCost = Bases[slot];
            if (slot == 1) baseCost = Bases[0];
            recipe.Materials.Add(new ItemDesc(primary, baseCost * Multipliers[tierSteps]));
            recipe.Materials.Add(new ItemDesc(Parts[slot], 5 * Multipliers[tierSteps]));
            recipe.Materials.Add(new ItemDesc(10703, Glazes[tierSteps]));
            recipe.Copper = Fees[tierSteps] * 10000;
        }
        else if (operation == EquipOperation.UpgradeGold)
        {
            if (levelSteps < 0 || levelSteps >= LevelMaterials.GetLength(1)) { recipe.Unavailable = "已达最高160级"; return recipe; }
            // 相同升级用量的部位使用同一行。
            int row = 0;
            if (slot >= 6) row = 3;
            else if (slot >= 4) row = 2;
            else if (slot >= 2) row = 1;
            recipe.Target = Level(levelSteps + 1);
            recipe.Materials.Add(new ItemDesc(primary, LevelMaterials[row, levelSteps]));
        }
        else recipe.Unavailable = "作坊操作无效";
        return recipe;
    }
}
