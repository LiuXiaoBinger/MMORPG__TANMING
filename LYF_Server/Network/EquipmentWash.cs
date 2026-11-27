using System;
using Google.Protobuf;

/// <summary>装备洗练的萃石档位、属性区间、评分与五槽快照，工坊和游戏服共用。</summary>
public static class EquipmentWash
{
    /// <summary>物品表内连续三档萃石的起始编号。</summary>
    public const int FirstStone = 8151;
    /// <summary>四档萃玉的首个物品编号，相邻档位依次可锁一至四条。</summary>
    public const int FirstJade = 8161;
    /// <summary>所有装备最多保存五条生效词条。</summary>
    public const int Slots = 5;
    /// <summary>一次洗练消耗同档萃石的数量。</summary>
    public const int Count = 3;

    /// <summary>录屏中的三档铜币价格，非法编号返回零。</summary>
    /// <param name="stone">物品表中的萃石编号。</param>
    public static int Cost(int stone)
    {
        switch (stone)
        {
            case FirstStone: return 5000;
            case FirstStone + 1: return 10000;
            case FirstStone + 2: return 20000;
            default: return 0;
        }
    }

    /// <summary>取得合法档位可洗出的最大词条数。</summary>
    /// <param name="stone">初、中、高级萃石的物品编号。</param>
    public static int Limit(int stone) { return stone - FirstStone + 3; }

    /// <summary>读取合法萃玉的锁定上限；不使用萃玉时为零。</summary>
    /// <param name="jade">背包内真实的萃玉物品编号。</param>
    public static int LockLimit(int jade)
    {
        if (jade < FirstJade || jade > FirstJade + 3) return 0;
        return jade - FirstJade + 1;
    }

    /// <summary>从生效槽位验证锁位、计数和本档萃石容量。</summary>
    /// <param name="active">装备自己的生效词条。</param>
    /// <param name="stone">本次消耗的萃石编号。</param>
    /// <param name="jade">锁定时所选萃玉。</param>
    /// <param name="mask">低五位依次对应当前方案中的五条属性。</param>
    public static bool CanLock(RoleEquipGeneInfo active, int stone, int jade, int mask)
    {
        if (Cost(stone) == 0 || mask < 0 || mask >= 1 << Slots) return false;
        if (jade != 0 && LockLimit(jade) == 0) return false;
        int count = 0; // 不可锁定空槽或超出萃玉容量。
        for (int slot = 0; slot < Slots; slot++)
        {
            if ((mask & (1 << slot)) == 0) continue;
            if (Id(active, slot) == 0) return false;
            count++;
        }
        return count <= LockLimit(jade) && count <= Limit(stone);
    }

    /// <summary>产生一组不重复的词条，条数和数值在各自闭区间内均匀抽取。</summary>
    /// <param name="stone">已经核实的三档萃石之一。</param>
    /// <param name="random">游戏服的随机数源。</param>
    /// <param name="active">本装备已生效的词条，锁定从这里取值。</param>
    /// <param name="jade">本次锁词条采用的萃玉编号。</param>
    /// <param name="mask">已经过服务端核验的锁位。</param>
    public static RoleEquipGeneInfo Roll(int stone, Random random, RoleEquipGeneInfo active, int jade, int mask)
    {
        // 独立候选，只有确认后才写回装备的生效槽。
        RoleEquipGeneInfo lines = new RoleEquipGeneInfo();
        // 普通点数、四种基础点数和五系攻穿抗均来自正式基因表。
        var pool = new System.Collections.Generic.List<int> { 3, 4, 5, 6, 7, 9, 11, 15, 16, 19, 23, 932027, 932028, 932029 };
        for (int gene = 930101; gene <= 930115; gene++) pool.Add(gene);
        // 最少一条，同一装备可以洗出低于该档上限的候选。
        int locked = 0; // 按原词条顺序复制类型与点数，再补抽剩余位置。
        for (int slot = 0; slot < Slots; slot++)
        {
            if ((mask & (1 << slot)) == 0) continue;
            int id = Id(active, slot); // 锁定的类型不会再次出现在本次可抽池。
            Set(lines, locked++, id, Value(active, slot));
            pool.Remove(id);
        }
        lines.WashJade = jade;
        lines.WashLocks = mask;
        int count = random.Next(Math.Max(1, locked), Limit(stone) + 1);
        for (int slot = locked; slot < count; slot++)
        {
            // 抽中的类型移出本次池子，不影响下一次洗练。
            int index = random.Next(pool.Count);
            int id = pool[index];
            pool.RemoveAt(index);
            Set(lines, slot, id, random.Next(1, Maximum(id) + 1));
        }
        return lines;
    }

    /// <summary>各类词条的单条满值，也是对比进度条的满刻度。</summary>
    /// <param name="id">表内基因编号。</param>
    public static int Maximum(int id)
    {
        if (id >= 930106 && id <= 930110) return 5;
        if (id >= 930111 && id <= 930115) return 20;
        return 40;
    }

    /// <summary>按生效或候选的实际点数换算独立洗练评分。</summary>
    /// <param name="lines">参与本次比较的一组属性。</param>
    public static int Score(RoleEquipGeneInfo lines)
    {
        int score = 0; // 各条属性加权点数之和。
        for (int slot = 0; slot < Slots; slot++)
        {
            int id = Id(lines, slot); // 当前词条的权重由基因类型决定。
            int weight = 30; // 基础点数每点三十分。
            if (id >= 930101 && id <= 930105) weight = 48;
            else if (id >= 930106 && id <= 930110) weight = 100;
            else if (id >= 930111 && id <= 930115) weight = 20;
            if (id != 0) score += Value(lines, slot) * weight;
        }
        return score;
    }

    /// <summary>读取服务端为该装备保留的上一组候选。</summary>
    /// <param name="encoded">数据库和物品快照共用的候选字节串。</param>
    public static RoleEquipGeneInfo Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded)) return new RoleEquipGeneInfo();
        return RoleEquipGeneInfo.Parser.ParseFrom(Convert.FromBase64String(encoded));
    }

    /// <summary>只存五槽候选；物品归属和洗练版本由外层装备实例持有。</summary>
    /// <param name="lines">服务器刚生成的候选。</param>
    public static string Encode(RoleEquipGeneInfo lines) { return Convert.ToBase64String(lines.ToByteArray()); }

    /// <summary>从当前装备取得可以展示或随操作回包发送的状态。</summary>
    /// <param name="equip">背包或身上的实际装备。</param>
    public static RoleEquipGeneInfo Snapshot(EquipBase equip)
    {
        return new RoleEquipGeneInfo
        {
            ItemUid = equip.GetItemUID(), RoleId = equip.GetRoleID(),
            WashRevision = equip.WashRevision, WashStone = equip.WashStone, WashCandidate = equip.WashCandidate ?? string.Empty,
            GeneId0 = equip.GeneID0, GeneValue0 = equip.GeneValue0,
            GeneId1 = equip.GeneID1, GeneValue1 = equip.GeneValue1,
            GeneId2 = equip.GeneID2, GeneValue2 = equip.GeneValue2,
            GeneId3 = equip.GeneID3, GeneValue3 = equip.GeneValue3,
            GeneId4 = equip.GeneID4, GeneValue4 = equip.GeneValue4
        };
    }

    /// <summary>覆盖五个生效槽，候选较短时同步清空旧的尾部。</summary>
    /// <param name="equip">确认替换的原装备实例。</param>
    /// <param name="lines">服务端从该实例自己保存的候选解出的五槽。</param>
    public static void Replace(EquipBase equip, RoleEquipGeneInfo lines)
    {
        equip.GeneID0 = lines.GeneId0;
        equip.GeneValue0 = lines.GeneValue0;
        equip.GeneID1 = lines.GeneId1;
        equip.GeneValue1 = lines.GeneValue1;
        equip.GeneID2 = lines.GeneId2;
        equip.GeneValue2 = lines.GeneValue2;
        equip.GeneID3 = lines.GeneId3;
        equip.GeneValue3 = lines.GeneValue3;
        equip.GeneID4 = lines.GeneId4;
        equip.GeneValue4 = lines.GeneValue4;
    }

    /// <summary>按显示顺序读取单槽的基因编号。</summary>
    /// <param name="lines">任一组生效或候选词条。</param>
    /// <param name="slot">零起的词条序号。</param>
    public static int Id(RoleEquipGeneInfo lines, int slot)
    {
        switch (slot)
        {
            case 0: return lines.GeneId0;
            case 1: return lines.GeneId1;
            case 2: return lines.GeneId2;
            case 3: return lines.GeneId3;
            case 4: return lines.GeneId4;
            default: throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }

    /// <summary>按显示顺序读取单槽的整数点数。</summary>
    /// <param name="lines">任一组生效或候选词条。</param>
    /// <param name="slot">零起的词条序号。</param>
    public static int Value(RoleEquipGeneInfo lines, int slot)
    {
        switch (slot)
        {
            case 0: return lines.GeneValue0;
            case 1: return lines.GeneValue1;
            case 2: return lines.GeneValue2;
            case 3: return lines.GeneValue3;
            case 4: return lines.GeneValue4;
            default: throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }

    /// <summary>按抽取顺序写入候选的对应槽。</summary>
    /// <param name="lines">尚在构造的候选。</param>
    /// <param name="slot">零至四的目标下标。</param>
    /// <param name="id">属性基因编号。</param>
    /// <param name="value">本次抽取的点数。</param>
    private static void Set(RoleEquipGeneInfo lines, int slot, int id, int value)
    {
        switch (slot)
        {
            case 0: lines.GeneId0 = id; lines.GeneValue0 = value; break;
            case 1: lines.GeneId1 = id; lines.GeneValue1 = value; break;
            case 2: lines.GeneId2 = id; lines.GeneValue2 = value; break;
            case 3: lines.GeneId3 = id; lines.GeneValue3 = value; break;
            case 4: lines.GeneId4 = id; lines.GeneValue4 = value; break;
        }
    }
}
