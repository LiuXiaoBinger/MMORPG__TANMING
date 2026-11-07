using System;

/// <summary>使用真实客户端物品源码和独立宿主验证完整装备装载、同包换装和特技字段增量。</summary>
public static class ClientDeltaTests
{
    /// <summary>已通过的断言数。</summary>
    private static int _checks;

    /// <summary>执行隔离客户端模型回归，不连接服务器或挂载正式场景。</summary>
    public static int Main()
    {
        Console.WriteLine("开始客户端物品源码隔离回归");
        try
        {
            return Run();
        }
        catch (Exception failure)
        {
            // 测试执行边界只输出失败信息，不包含或吞掉任何协议解析调用。
            Console.WriteLine(failure.GetType().FullName);
            Console.WriteLine(failure.Message);
            // 依赖初始化失败时保留内部异常类型和消息，避免运行时ToString二次失败掩盖根因。
            Exception cause = failure.InnerException;
            while (cause != null)
            {
                Console.WriteLine(cause.GetType().FullName + ": " + cause.Message);
                cause = cause.InnerException;
            }
            return 1;
        }
    }

    /// <summary>依次应用完整快照、两种顺序的换装增量和特技切换增量。</summary>
    private static int Run()
    {
        LubanMgr.Instance.Init();
        // 测试宿主只提供角色协议和脏标记，真实物品源码不依赖Unity场景相机。
        PlayerEntity role = new PlayerEntity(new MainRoleInfo { BaseInfo = new RoleBaseInfo { RoleId = 99001, Nickname = "客户端隔离验证" } });
        // 当前真实装备表中的主武器配置，测试不修改任何策划数据。
        cfg.EquipInfo config = LubanMgr.Instance.GetEquipInfoById(21001);
        // 使用正式客户端物品组件和容器类型。
        RoItemComponent items = new RoItemComponent(role);
        items.Initialize();
        // 登录快照：一件已穿戴、一件同配置在背包。
        RoleKanpsackInfo snapshot = new RoleKanpsackInfo();
        snapshot.RoleCurrtEquipPack.Add(Item(900001, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 0));
        snapshot.RolePackEquip.Add(Item(900002, config, KnapsackType.RolePackEquip, 0, 1));
        items.LoadKnapsack(new RoleKanpsackInfoRet { CmdCode = CmdCode.Succeed, RoleKanpsackInfo = snapshot });
        CheckEquip(items, 900001, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 0);
        CheckEquip(items, 900002, config, KnapsackType.RolePackEquip, 0, 1);
        // 同包先列待穿戴物品，目标格仍被另一件占用，覆盖历史逐条应用会丢失的路径。
        UpdateItem swap = new UpdateItem();
        swap.ItemList.Add(Item(900002, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 1));
        swap.ItemList.Add(Item(900001, config, KnapsackType.RolePackEquip, 0, 0));
        items.ApplyServerItemDelta(swap);
        CheckEquip(items, 900002, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 1);
        CheckEquip(items, 900001, config, KnapsackType.RolePackEquip, 0, 0);
        Check(items.GetContainer(KnapsackType.RoleCurrtEquipPack).GetItemMap().Count == 1, "换装后穿戴栏没有丢失或重复");
        Check(items.GetContainer(KnapsackType.RolePackEquip).GetItemMap().Count == 1, "换装后背包没有丢失或重复");
        // 一条特技切换增量仍必须重建为完整 EquipBase，不能降格成 ItemBase。
        UpdateItem switchSkill = new UpdateItem();
        switchSkill.ItemList.Add(Item(900002, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 0));
        items.ApplyServerItemDelta(switchSkill);
        CheckEquip(items, 900002, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 0);
        // 换装反向顺序也需成立，验证结果不依赖列表排序。
        swap.ItemList.Clear();
        swap.ItemList.Add(Item(900001, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 0));
        swap.ItemList.Add(Item(900002, config, KnapsackType.RolePackEquip, 0, 0));
        items.ApplyServerItemDelta(swap);
        CheckEquip(items, 900001, config, KnapsackType.RoleCurrtEquipPack, config.EquipType, 0);
        CheckEquip(items, 900002, config, KnapsackType.RolePackEquip, 0, 0);
        Console.WriteLine("PASS " + _checks + " 客户端装备增量断言");
        return 0;
    }

    /// <summary>构造测试专用的完整服务器装备数据，不接触正式存档。</summary>
    /// <param name="uid">测试物品唯一编号。</param>
    /// <param name="config">真实装备静态配置。</param>
    /// <param name="bag">服务器确认的目标背包。</param>
    /// <param name="index">目标背包格位。</param>
    /// <param name="active">本次启用的特技保存槽。</param>
    private static RoleItemInfo Item(long uid, cfg.EquipInfo config, KnapsackType bag, int index, int active)
    {
        return new RoleItemInfo
        {
            ItemUid = uid, ItemTypeId = config.ItemTypeId, Count = 1, RoleId = 99001,
            BagType = (int)bag, BagIndex = index,
            EquipInfo = new RoleEquipItemInfo
            {
                EquipType = config.EquipType, StrengthenLevel = 9,
                SpecialSkillId0 = 2, SpecialSkillId1 = 4, SpecialSkillId2 = 0,
                ActiveSpecialSkillIndex = active
            },
            EquipGeneInfo = new RoleEquipGeneInfo
            {
                GeneId0 = 1, GeneValue0 = 155, GeneId1 = 2, GeneValue1 = 25,
                GeneId2 = 20, GeneValue2 = 3
            }
        };
    }

    /// <summary>检查完整装备类型、实例附加数据和服务器位置。</summary>
    /// <param name="items">真实客户端物品组件。</param>
    /// <param name="uid">被检查的物品 UID。</param>
    /// <param name="config">该装备的静态配置。</param>
    /// <param name="bag">预期背包分类。</param>
    /// <param name="index">预期格位。</param>
    /// <param name="active">预期启用特技保存槽。</param>
    private static void CheckEquip(RoItemComponent items, long uid, cfg.EquipInfo config, KnapsackType bag, int index, int active)
    {
        // 按 UID 查询必须返回装备派生类型及其完整附加字段。
        EquipBase equip = items.GetItemByUID(uid) as EquipBase;
        Check(equip != null, "物品仍为EquipBase " + uid);
        Check(equip.GetBagType() == (int)bag && equip.GetBagIndex() == index, "服务器格位 " + uid);
        Check(equip.GetItemID() == config.ItemTypeId && equip.StrengthenLevel == 9, "配置与强化 " + uid);
        Check(equip.GeneID0 == 1 && equip.GeneValue0 == 155, "实例词条0 " + uid);
        Check(equip.GeneID1 == 2 && equip.GeneValue1 == 25, "实例词条1 " + uid);
        Check(equip.GeneID2 == 20 && equip.GeneValue2 == 3, "实例词条2 " + uid);
        Check(equip.SpecialSkillId0 == 2 && equip.SpecialSkillId1 == 4 && equip.SpecialSkillId2 == 0, "特技保存槽 " + uid);
        Check(equip.ActiveSpecialSkillIndex == active, "启用特技槽 " + uid);
    }

    /// <summary>记录断言，失败立即保留可定位的说明。</summary>
    /// <param name="condition">本项实际检查条件。</param>
    /// <param name="message">失败时可识别的装备行为说明。</param>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _checks++;
    }
}
