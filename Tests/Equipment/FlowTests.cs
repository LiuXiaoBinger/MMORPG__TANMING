extern alias Server;
using System;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf;
using BagTransitionResult = Server::BagTransitionResult;
using BagWearItemTransition = Server::BagWearItemTransition;
using OnlineRole = Server::OnlineRole;
using RoItemComponent = Server::RoItemComponent;
using RoItemContainer = Server::RoItemContainer;
using UpdateType = Server::UpdateType;

/// <summary>使用真实服务端组件与真实配置验证装备事务和存档协议，不连接运行中的游戏服或数据库。</summary>
public static class FlowTests
{
    /// <summary>已经通过的业务断言数，只用于测试结果汇总。</summary>
    private static int passed;

    /// <summary>加载正式配置，依次执行实例穿脱、满背包换装、特技切换和序列化回读。</summary>
    public static int Main()
    {
        // 测试进程使用独立逻辑服编号，和正式启动一样先配置 UID 生成器。
        UIDHelper.ConfigureServerInstanceId(254);
        LubanMgr.Instance.Init();
        // 当前正式装备表中的配置原对象，测试只读取配置，不修改表或生成数据。
        List<cfg.EquipInfo> configurations = new List<cfg.EquipInfo>(LubanMgr.Instance.GetEquipInfos().Values);
        Check(configurations.Count > 0, "正式装备配置存在");
        // 选择第一条真实装备配置，测试角色等级和职业与其匹配。
        cfg.EquipInfo config = configurations[0];
        // 当前测试角色不加入在线角色索引，不会访问玩家存档。
        OnlineRole role = CreateRole(config, 91001);
        // 真实物品组件处理发放、移动、序列化和重新装载。
        RoItemComponent items = role.GetComponent<RoItemComponent>();
        // 测试将设备背包缩至两格，快速覆盖容量边界，不修改正式配置。
        RoItemContainer bag = items.GetContainer(KnapsackType.RolePackEquip);
        bag.SetContainerSize(2);
        // 同配置的两件装备必须有不同 UID，确保按实例而不是配置 ID 操作。
        EquipBase first = Give(items, config.ItemTypeId);
        // 第二件同配置装备作为换装目标。
        EquipBase second = Give(items, config.ItemTypeId);
        Check(first.GetItemUID() != second.GetItemUID(), "同配置装备实例 UID 不同");
        Check(first.SpecialSkillId0 == config.SpecialSkillId0 && first.SpecialSkillId1 == config.SpecialSkillId1 &&
            first.SpecialSkillId2 == config.SpecialSkillId2, "发放复制三个默认特技族");
        Check(first.ActiveSpecialSkillIndex == config.ActiveSpecialSkillIndex, "发放复制默认启用槽");
        // 事务复用正常角色装载状态、等级职业校验与装备重算回调。
        BagWearItemTransition transition = new BagWearItemTransition(role);
        Check(transition.WearItem(config.ItemTypeId) == BagTransitionResult.InvalidArgument,
            "配置 ID 不能冒充装备实例 UID");
        Check(transition.WearItem(first.GetItemUID()) == BagTransitionResult.Success, "指定实例首次穿戴成功");
        Check(ReferenceEquals(items.GetContainer(KnapsackType.RoleCurrtEquipPack).GetItemByPos(config.EquipType), first),
            "穿戴部位保存原实例引用");
        // 补满新装备腾出的格子，使普通背包重新达到容量上限。
        EquipBase third = Give(items, config.ItemTypeId);
        Check(bag.IsFull(), "测试前装备背包确实已满");
        Check(transition.TakeOffItem(first.GetItemUID()) == BagTransitionResult.BagFull, "满背包卸装明确失败");
        Check(first.GetBagType() == (int)KnapsackType.RoleCurrtEquipPack && bag.GetItemMap().Count == 2,
            "卸装失败保持原位置且不丢物");
        // 记录新装备原格位；换下装备必须使用这个已腾出的格子。
        int secondPosition = second.GetBagIndex();
        Check(transition.WearItem(second.GetItemUID()) == BagTransitionResult.Success, "满背包仍可同槽换装");
        Check(ReferenceEquals(bag.GetItemByPos(secondPosition), first), "旧装备回到新装备原格位");
        Check(ReferenceEquals(items.GetContainer(KnapsackType.RoleCurrtEquipPack).GetItemByPos(config.EquipType), second),
            "同槽替换后只保留指定新实例");
        Check(bag.GetItemMap().Count == 2 && bag.GetItemByUid(third.GetItemUID()) == third, "换装保持两包全部实例");

        // 独立测试数据覆盖每个可保存字段；这些值只修改测试角色的内存装备。
        second.SpecialSkillId0 = 1;
        second.SpecialSkillId1 = 2;
        second.SpecialSkillId2 = 0;
        second.GeneID0 = 3;
        second.GeneID1 = 4;
        second.GeneID2 = 5;
        second.GeneValue0 = 17;
        second.GeneValue1 = 23;
        second.GeneValue2 = 31;
        second.StrengthenLevel = 2;
        Check(transition.SwitchSpecialSkill(second.GetItemUID(), 2) == BagTransitionResult.InvalidArgument,
            "不能启用空特技槽");
        Check(transition.SwitchSpecialSkill(second.GetItemUID(), 3) == BagTransitionResult.InvalidArgument,
            "不能启用超范围槽位");
        second.SpecialSkillId2 = 3;
        Check(transition.SwitchSpecialSkill(second.GetItemUID(), 2) == BagTransitionResult.Success,
            "三个特技中可切换唯一启用槽");
        Check(second.GetActiveSpecialSkillId() == 3 && second.SpecialSkillId0 == 1 && second.SpecialSkillId1 == 2,
            "切换不覆盖未启用槽位");
        VerifyPersistenceRoundTrip(role, second, config);
        VerifyIncrementalRoundTrip(second);
        VerifyLevelAndJob(configurations);
        Console.WriteLine("PASS Equipment Flow：" + passed + " 项真实组件/协议断言；未连接数据库或玩家会话。");
        return 0;
    }

    /// <summary>从实际 SaveAllMoudle 生成完整背包快照，序列化后作为登录背包回包装载到新角色。</summary>
    /// <param name="role">已完成特技切换并持有脏背包的原测试角色。</param>
    /// <param name="expected">需要验证完整字段的当前穿戴实例。</param>
    /// <param name="config">创建回读角色所用的同一装备配置。</param>
    private static void VerifyPersistenceRoundTrip(OnlineRole role, EquipBase expected, cfg.EquipInfo config)
    {
        // 真实保存入口返回两类发生过变化的背包完整列表。
        SaveRoleDataReq saved = SaveRoleDataReq.Parser.ParseFrom(role.SaveAllMoudle().ToByteArray());
        Check(saved.ItemDataLists.Count == 2, "穿脱标记源和目标两个背包存档");
        // 仅模拟中心服的登录封装，物品字段直接取真实保存协议并保留原值。
        RoleKanpsackInfoRet login = new RoleKanpsackInfoRet
        {
            CmdCode = CmdCode.Succeed,
            RoleKanpsackInfo = new RoleKanpsackInfo { SaveVersion = saved.Version }
        };
        // packIndex 是本次保存的背包列表索引，实际测试只包含装备背包与穿戴栏。
        for (int packIndex = 0; packIndex < saved.ItemDataLists.Count; packIndex++)
        {
            // 当前背包完整快照，含可为空的物品列表。
            ItemDataList pack = saved.ItemDataLists[packIndex];
            if (pack.BagType == (int)KnapsackType.RolePackEquip)
            {
                login.RoleKanpsackInfo.RolePackEquip.Add(pack.Items);
            }
            else if (pack.BagType == (int)KnapsackType.RoleCurrtEquipPack)
            {
                login.RoleKanpsackInfo.RoleCurrtEquipPack.Add(pack.Items);
            }
        }
        // 新角色仅用于验证实际 LoadKnapsack 的登录恢复行为。
        OnlineRole restoredRole = CreateRole(config, 91002);
        // 实际组件读回经过 protobuf 字节往返后的完整背包。
        RoItemComponent restoredItems = restoredRole.GetComponent<RoItemComponent>();
        restoredItems.LoadKnapsack(RoleKanpsackInfoRet.Parser.ParseFrom(login.ToByteArray()));
        // 恢复结果必须仍是装备实例，而不是丢掉装备扩展的 ItemBase。
        EquipBase restored = restoredItems.GetContainer(KnapsackType.RoleCurrtEquipPack)
            .GetItemByUid(expected.GetItemUID()) as EquipBase;
        CheckEquipFields(expected, restored, "完整存档回读");
        Check(restoredItems.GetContainer(KnapsackType.RolePackEquip).GetItemMap().Count == 2,
            "完整回读保留同配置背包内另两件装备");
        Check(new BagWearItemTransition(restoredRole).TakeOffItem(expected.GetItemUID()) == BagTransitionResult.Success,
            "回读后的原穿戴实例可正常卸装");
    }

    /// <summary>调用真实增量序列化和恢复函数，验证新增装备不会降级为普通物品。</summary>
    /// <param name="expected">已配置三条实例词条与三个特技的测试装备。</param>
    private static void VerifyIncrementalRoundTrip(EquipBase expected)
    {
        // 反射只用于测试私有序列化边界，业务代码没有测试入口或反射载荷。
        MethodInfo serializeMethod = typeof(RoItemComponent).GetMethod("ToRoleItemInfo", BindingFlags.Static | BindingFlags.NonPublic);
        // 将真实私有方法绑定为强类型委托，避免复制它的序列化实现。
        Func<ItemBase, RoleItemInfo> serialize = (Func<ItemBase, RoleItemInfo>)Delegate.CreateDelegate(
            typeof(Func<ItemBase, RoleItemInfo>), serializeMethod);
        // 同样调用真实恢复入口，保留 UID 与所有装备扩展。
        MethodInfo restoreMethod = typeof(RoItemComponent).GetMethod("CreateLoadedItem", BindingFlags.Static | BindingFlags.NonPublic);
        // 强类型测试委托只在当前测试进程中存在。
        Func<RoleItemInfo, ItemBase> restore = (Func<RoleItemInfo, ItemBase>)Delegate.CreateDelegate(
            typeof(Func<RoleItemInfo, ItemBase>), restoreMethod);
        // 完整装备增量经过真实 protobuf 字节编码再解码。
        RoleItemInfo wire = RoleItemInfo.Parser.ParseFrom(serialize(expected).ToByteArray());
        Check(wire.EquipInfo != null && wire.EquipGeneInfo != null, "物品增量包含装备和三条实例词条");
        CheckEquipFields(expected, restore(wire) as EquipBase, "增量协议回读");
    }

    /// <summary>在测试进程注入有明确等级职业限制的配置副本，验证拒绝时物品留在原背包。</summary>
    /// <param name="configurations">正式装备表中的只读配置列表。</param>
    private static void VerifyLevelAndJob(List<cfg.EquipInfo> configurations)
    {
        // 正式演示装备可能均无职业限制；反射仅用于复制测试配置，不修改正式表文件。
        cfg.EquipInfo config = (cfg.EquipInfo)typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(configurations[0], null);
        typeof(cfg.EquipInfo).GetField("NeedLevel").SetValue(config, 50);
        typeof(cfg.EquipInfo).GetField("Job").SetValue(config, 2);
        // 读取真实运行时索引，临时替换一条配置，最后恢复原对象。
        Dictionary<int, cfg.EquipInfo> registry = LubanMgr.Instance.GetEquipInfos();
        // 保存原配置原对象，finally 只恢复测试进程内的替换。
        cfg.EquipInfo original = registry[config.ItemTypeId];
        registry[config.ItemTypeId] = config;
        try
        {
            // 独立角色用于修改等级和职业，不影响前面的存档场景。
            OnlineRole role = CreateRole(config, 91003);
            // 此角色的真实装备发放结果。
            EquipBase equip = Give(role.GetComponent<RoItemComponent>(), config.ItemTypeId);
            // 复用正常穿戴事务进行两次实际拒绝验证。
            BagWearItemTransition transition = new BagWearItemTransition(role);
            role.mainRoleInfo.BaseInfo.Level = config.NeedLevel - 1;
            Check(transition.WearItem(equip.GetItemUID()) == BagTransitionResult.InvalidArgument, "等级不足不能穿戴");
            role.mainRoleInfo.BaseInfo.Level = config.NeedLevel;
            role.mainRoleInfo.BaseInfo.JobId = config.Job + 100;
            Check(transition.WearItem(equip.GetItemUID()) == BagTransitionResult.InvalidArgument, "职业不符不能穿戴");
            Check(equip.GetBagType() == (int)KnapsackType.RolePackEquip, "条件拒绝不移动装备");
        }
        finally
        {
            registry[config.ItemTypeId] = original;
        }
    }

    /// <summary>创建只存在于测试进程的在线角色并完成既有背包装载状态。</summary>
    /// <param name="config">用于选择匹配职业及足够等级的装备配置。</param>
    /// <param name="roleId">测试进程内区分角色的编号，不写数据库。</param>
    private static OnlineRole CreateRole(cfg.EquipInfo config, int roleId)
    {
        // 裸属性只包含测试角色自己的基础值，不预先写装备加成。
        OnlineRole role = new OnlineRole
        {
            mainRoleInfo = new MainRoleInfo { BaseInfo = new RoleBaseInfo
            {
                RoleId = roleId, Level = Math.Max(config.NeedLevel, 1), JobId = config.Job,
                MaxHp = 1000, CurrHp = 1000, MaxMp = 200, CurrMp = 200
            } }
        };
        role.InitSerializes();
        Check(role.InitializeSaveVersion(0), "测试角色保存版本初始化");
        Check(role.CompleteRoleDataLoad(), "测试角色完成既有装载状态");
        return role;
    }

    /// <summary>通过真实 AddItem 创建一件装备，验证默认特技的发放路径。</summary>
    /// <param name="items">测试角色的真实物品组件。</param>
    /// <param name="itemId">正式装备配置编号。</param>
    private static EquipBase Give(RoItemComponent items, int itemId)
    {
        // 本次创建的装备使用正常 UID 生成和背包入格流程。
        EquipBase equip = items.AddItem(itemId, 1, new ChangeReason(ItemChangeReason.ITEM_REASON_NONE),
            ItemSign.IsNoSign, 0, 0, UpdateType.eUT_Normal, new IdipParam()) as EquipBase;
        Check(equip != null, "真实 AddItem 创建装备实例");
        return equip;
    }

    /// <summary>比较实例身份、位置及强化、词条和特技，避免只验证协议对象非空。</summary>
    /// <param name="expected">序列化之前的原装备实例。</param>
    /// <param name="actual">通过真实恢复入口得到的装备。</param>
    /// <param name="stage">断言失败时提示的验证阶段。</param>
    private static void CheckEquipFields(EquipBase expected, EquipBase actual, string stage)
    {
        Check(actual != null, stage + "仍为 EquipBase");
        Check(actual.GetItemUID() == expected.GetItemUID() && actual.GetBagType() == expected.GetBagType() &&
            actual.GetBagIndex() == expected.GetBagIndex(), stage + "保留 UID 和位置");
        Check(actual.EquipType == expected.EquipType && actual.StrengthenLevel == expected.StrengthenLevel,
            stage + "保留部位和强化");
        Check(actual.GeneID0 == expected.GeneID0 && actual.GeneID1 == expected.GeneID1 && actual.GeneID2 == expected.GeneID2,
            stage + "保留三个实例词条编号");
        Check(actual.GeneValue0 == expected.GeneValue0 && actual.GeneValue1 == expected.GeneValue1 && actual.GeneValue2 == expected.GeneValue2,
            stage + "保留三个词条实际值");
        Check(actual.SpecialSkillId0 == expected.SpecialSkillId0 && actual.SpecialSkillId1 == expected.SpecialSkillId1 &&
            actual.SpecialSkillId2 == expected.SpecialSkillId2, stage + "保留三个特技族");
        Check(actual.ActiveSpecialSkillIndex == expected.ActiveSpecialSkillIndex, stage + "保留唯一启用槽");
    }

    /// <summary>累计成功断言，失败立即抛出具体场景信息。</summary>
    /// <param name="condition">当前真实业务结果是否符合预期。</param>
    /// <param name="message">失败时可定位业务行为的中文说明。</param>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed++;
    }
}
