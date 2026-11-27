using System;

/// <summary>装备六槽的基础值、强化、进阶与珍品判定，服务端和详情面板使用同一口径。</summary>
public static class EquipmentGrowth
{
    /// <summary>装备模板的基础基因槽数。</summary>
    public const int Slots = 6;
    /// <summary>可强化装备的等级上限。</summary>
    public const int MaxStrength = 50;
    /// <summary>初始为一阶，最高可升至十阶。</summary>
    public const int MaxTier = 10;
    /// <summary>每次重铸整件装备所需铜币。</summary>
    public const int ReforgeCost = 500;
    /// <summary>游戏服为新实例与重铸共用的随机源。</summary>
    private static readonly Random Randomizer = new Random();

    /// <summary>依次读取模板约定的六个基础值。</summary>
    /// <param name="config">当前装备物品配置。</param>
    public static int[] Templates(cfg.EquipInfo config)
    {
        return new[] { config.GeneValue0, config.GeneValue1, config.GeneValue2,
            config.GeneValue3, config.GeneValue4, config.GeneValue5 };
    }

    /// <summary>读取六个模板槽的基因编号，零为空槽。</summary>
    /// <param name="config">当前装备物品配置。</param>
    public static int[] Genes(cfg.EquipInfo config)
    {
        return new[] { config.GeneId0, config.GeneId1, config.GeneId2,
            config.GeneId3, config.GeneId4, config.GeneId5 };
    }

    /// <summary>模板值的八成向上取整，为随机区间下限。</summary>
    /// <param name="template">装备模板给定的非负整数。</param>
    public static int Minimum(int template) { return (int)((template * 8L + 9) / 10); }

    /// <summary>模板值的一点二倍四舍五入，为可达到的满值。</summary>
    /// <param name="template">装备模板给定的非负整数。</param>
    public static int Maximum(int template) { return (int)((template * 12L + 5) / 10); }

    /// <summary>新装备或重铸时生成六个基础数值，空槽写零。</summary>
    /// <param name="equipment">玩家持有的独立装备实例。</param>
    /// <param name="config">决定各槽类型与区间的装备配置。</param>
    public static void Roll(EquipBase equipment, cfg.EquipInfo config)
    {
        // 基因编号为零的槽不参与随机与评级。
        int[] genes = Genes(config);
        // 每个槽按本部位的模板数值独立摇取。
        int[] values = Templates(config);
        equipment.BaseRolls.Clear();
        lock (Randomizer)
        {
            for (int slot = 0; slot < Slots; slot++)
            {
                // 随机上界为排他边界，加一使满值也可以摇出。
                int value = 0;
                if (genes[slot] != 0) value = Randomizer.Next(Minimum(values[slot]), Maximum(values[slot]) + 1);
                equipment.BaseRolls.Add(value);
            }
        }
        // 内外功上下限各自的模板区间可能重叠，颠倒时交换两个实际抽出的值。
        for (int low = 0; low < Slots; low++)
        {
            if (genes[low] != 3 && genes[low] != 5) continue;
            for (int high = 0; high < Slots; high++)
            {
                if (genes[high] != genes[low] + 1 || equipment.BaseRolls[low] <= equipment.BaseRolls[high]) continue;
                // 两个模板本来有序，交换后的数值仍在各自的合法区间。
                int larger = equipment.BaseRolls[low];
                equipment.BaseRolls[low] = equipment.BaseRolls[high];
                equipment.BaseRolls[high] = larger;
            }
        }
    }

    /// <summary>读取实例值；历史固定属性装备延续原模板。</summary>
    /// <param name="equipment">待结算的装备实例。</param>
    /// <param name="template">该槽缺少实例数据时的原值。</param>
    /// <param name="slot">从零开始的基础槽。</param>
    public static int Base(EquipBase equipment, int template, int slot)
    {
        if (slot < equipment.BaseRolls.Count) return equipment.BaseRolls[slot];
        return template;
    }

    /// <summary>每强化一级增加原始基础值的 1.5%，单项四舍五入。</summary>
    /// <param name="value">该槽重铸后的基础值。</param>
    /// <param name="level">已强化的级数。</param>
    public static int Strength(int value, int level)
    {
        return (int)((value * 15L * Math.Min(MaxStrength, Math.Max(0, level)) + 500) / 1000);
    }

    /// <summary>每超过一阶增加原始基础值的 7.5%，单项四舍五入。</summary>
    /// <param name="value">该槽重铸后的基础值。</param>
    /// <param name="steps">一阶为零，十阶为九。</param>
    public static int Tier(int value, int steps)
    {
        return (int)((value * 75L * Math.Min(MaxTier - 1, Math.Max(0, steps)) + 500) / 1000);
    }

    /// <summary>返回实际挂载到装备基础基因的最终整数。</summary>
    /// <param name="equipment">提供养成进度和随机值的实例。</param>
    /// <param name="template">旧装备本槽的基准数。</param>
    /// <param name="slot">从零开始的属性槽。</param>
    public static int Total(EquipBase equipment, int template, int slot)
    {
        // 两项养成按同一基础分别计算，重铸不会积累历史加成。
        int value = Base(equipment, template, slot);
        return value + Strength(value, equipment.StrengthenLevel) + Tier(value, equipment.GoldTierSteps);
    }

    /// <summary>全部有效基础属性达到各自满值的九成时显示极。</summary>
    /// <param name="equipment">待评级的实例。</param>
    /// <param name="config">提供有效属性与满值的配置。</param>
    public static bool IsExtreme(EquipBase equipment, cfg.EquipInfo config)
    {
        // 只有实际提供正值的基因槽计入珍品评价。
        int[] genes = Genes(config);
        // 评级仅比较随机基础值，养成贡献不能提高评级。
        int[] values = Templates(config);
        // 空配置不授予极标记。
        bool found = false;
        for (int slot = 0; slot < Slots; slot++)
        {
            if (genes[slot] == 0 || values[slot] <= 0) continue;
            found = true;
            if (Base(equipment, values[slot], slot) * 10L < Maximum(values[slot]) * 9L) return false;
        }
        return found;
    }

    /// <summary>强化按目标等级每级一百铜币，普通进阶按目标阶每阶一千铜币。</summary>
    /// <param name="operation">强化、普通进阶或重铸。</param>
    /// <param name="target">服务器核对的目标等级或阶数。</param>
    public static int Cost(EquipOperation operation, int target)
    {
        if (operation == EquipOperation.Strengthen) return 100 * target;
        if (operation == EquipOperation.Advance) return 1000 * target;
        return ReforgeCost;
    }
}
