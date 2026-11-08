extern alias Server;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using MMoRpgCommon.SceneEntity;
using OnlineRole = Server::OnlineRole;
using RoleEquipComponent = Server::RoleEquipComponent;
using RoItemComponent = Server::RoItemComponent;
using BagWearItemTransition = Server::BagWearItemTransition;
using GeneComponent = Server::GeneComponent;
using MonsterDefenseAttributes = Server::MonsterDefenseAttributes;
using SceneWaveSpawner = Server::SceneWaveSpawner;
using SceneMonster = Server::GameServer.SceneEntity.MonsterEntity;

/// <summary>真实穿戴组件、正式特技配置与真实副本扣血入口的独立验收，不连接运行中的服务器。</summary>
public static class GeneEquipmentTests
{
    /// <summary>已经通过的实际行为断言数。</summary>
    private static int passed;

    /// <summary>装载正式表并验证穿戴属性、特技等级、资源及场景伤害。</summary>
    /// <returns>全部断言通过时为 0，失败直接保留异常信息。</returns>
    public static int Main()
    {
        try
        {
            UIDHelper.ConfigureServerInstanceId(253);
            LubanMgr.Instance.Init();
            VerifyEquipmentAndResources();
            VerifyMonsterCombat();
            VerifyControlAndCritical();
            Console.WriteLine("PASS Equipment Gene：" + passed + " 项断言。");
            return 0;
        }
        // 测试进程只报告失败，不吞掉失败状态；简短消息避免运行时展开异常栈再次失败。
        catch (Exception failure)
        {
            Console.WriteLine("FAIL Equipment Gene after " + passed + ": " + failure.GetType().Name + " " + failure.Message);
            return 1;
        }
    }

    /// <summary>用真实事务穿四个不同部位，验证同名等级封顶和实例词条独立累计。</summary>
    private static void VerifyEquipmentAndResources()
    {
        // 正式配置列表，用第一件的职业选择兼容的四个部位。
        List<cfg.EquipInfo> rows = new List<cfg.EquipInfo>(LubanMgr.Instance.GetEquipInfos().Values);
        // 测试角色有明确裸属性，任何穿脱都不能写回这些基础数值。
        OnlineRole role = CreateRole(rows[0].Job, 93001);
        // 真实在线装备模块，包含实际生命周期注册的 GeneRuntime。
        RoleEquipComponent equipment = role.GetComponent<RoleEquipComponent>();
        // 真实物品模块负责新建 UID 与背包事务。
        RoItemComponent items = role.GetComponent<RoItemComponent>();
        // 已穿部位去重，防止测试用同槽替换误算同名件数。
        HashSet<int> slots = new HashSet<int>();
        // 保留原装备实例供切换、卸装与来源断言使用。
        List<EquipBase> worn = new List<EquipBase>();
        for (int i = 0; i < rows.Count && worn.Count < 4; i++)
        {
            // 当前真实装备候选，职业不兼容时不参与测试。
            cfg.EquipInfo row = rows[i];
            if (row.Job != 0 && row.Job != rows[0].Job) continue;
            if (!slots.Add(row.EquipType)) continue;
            // 由正式发放入口生成装备，只在测试实例上设置特技与重复词条。
            EquipBase item = (EquipBase)items.AddItem(row.ItemTypeId, 1, new ChangeReason(ItemChangeReason.ITEM_REASON_NONE),
                ItemSign.IsNoSign, 0, 0, Server::UpdateType.eUT_Normal, new IdipParam());
            item.SpecialSkillId0 = 1;
            item.SpecialSkillId1 = 4;
            item.SpecialSkillId2 = 5;
            item.ActiveSpecialSkillIndex = 0;
            item.GeneID0 = 3;
            item.GeneValue0 = 20;
            item.GeneID1 = 3;
            item.GeneValue1 = 30;
            item.GeneID2 = 0;
            Check(new BagWearItemTransition(role).WearItem(item.GetItemUID()) == Server::BagTransitionResult.Success, "正式穿戴事务成功");
            worn.Add(item);
            // 新装备贡献两个相同编号的不同槽，不能把后一项覆盖前一项。
            Check(equipment.Runtime.FindGene(3, (ulong)item.GetItemUID(), 7) != null, "第一实例攻击词条已挂载");
            Check(equipment.Runtime.FindGene(3, (ulong)item.GetItemUID(), 8) != null, "第二重复攻击词条独立挂载");
        }
        Check(worn.Count == 4, "正式配置包含四个可穿戴部位");
        // 四件同族也只使用最高三级治疗增益，背包未启用槽不会同时增加资源特技。
        Equal(16, equipment.CalculateAttribute().HealBonus - role.mainRoleInfo.BaseInfo.HealBonus, "同名四件封顶三级");
        Equal(100, role.mainRoleInfo.BaseInfo.AtkExternalMin, "穿戴未污染裸攻击");
        Equal(1000, role.mainRoleInfo.BaseInfo.MaxHp, "穿戴未污染裸生命上限");
        Check(CountRoots(equipment.Runtime, 84001) == 0 && CountRoots(equipment.Runtime, 85001) == 0, "另两个保存槽没有启用");
        // 同一套穿戴重复调用不能把属性叠算或刷新事件实例。
        RoleBaseInfo before = equipment.CalculateAttribute();
        equipment.OnEquipChange();
        equipment.OnEquipChange();
        Equal(before.AtkExternalMin, equipment.CalculateAttribute().AtkExternalMin, "重复挂载不双算攻击");
        Equal(before.MaxHp, equipment.CalculateAttribute().MaxHp, "重复挂载不双算生命");
        // 其他来源临时攻击保持到自己的到期时间，不随一件装备卸载。
        equipment.Runtime.AddGene(3, 999999, 77);
        Check(new BagWearItemTransition(role).TakeOffItem(worn[0].GetItemUID()) == Server::BagTransitionResult.Success, "正式卸装事务成功");
        Check(equipment.Runtime.FindGene(3, 999999) != null, "卸装保留其他来源基因");
        Check(equipment.Runtime.FindGene(3, (ulong)worn[0].GetItemUID(), 7) == null, "卸装移除该装备所有实例槽");
        // 直接施加真实即时治疗配置，检验治疗增益按当前最终面板结算。
        role.mainRoleInfo.BaseInfo.CurrHp = 10;
        before = equipment.CalculateAttribute();
        equipment.Runtime.AddGene(84101, 999998, null, 0, role.GetID());
        Equal(10 + (int)((int)(before.MaxHp * 0.1) * 1.16), role.mainRoleInfo.BaseInfo.CurrHp, "疗伤圣手增强真实即时恢复");
        // 切换一件到血殇后疗伤族降为两件；新的根实例在重复变更通知后仍应是原对象。
        Check(new BagWearItemTransition(role).SwitchSpecialSkill(worn[1].GetItemUID(), 1) == Server::BagTransitionResult.Success, "真实特技槽切换成功");
        Equal(12, equipment.CalculateAttribute().HealBonus, "同名启用件数减少后降级");
        // 确认同一特技根对象未被重复 OnEquipChange 替换。
        GeneBase root = equipment.Runtime.FindGene(84001, (ulong)worn[1].GetItemUID(), 104);
        equipment.OnEquipChange();
        Check(ReferenceEquals(root, equipment.Runtime.FindGene(84001, (ulong)worn[1].GetItemUID(), 104)), "重复变更保留特技运行实例");
        role.mainRoleInfo.BaseInfo.CurrHp = 10;
        before = equipment.CalculateAttribute();
        equipment.Trigger(new GeneTriggerArgs(GeneEventType.Kill));
        Equal(10 + (int)((int)(before.MaxHp * 0.1) * 1.12), role.mainRoleInfo.BaseInfo.CurrHp, "击败即时恢复且叠加疗伤增益");
        // 装备上限下降时只裁切当前资源，不覆盖裸上限。
        role.mainRoleInfo.BaseInfo.CurrHp = int.MaxValue;
        role.mainRoleInfo.BaseInfo.CurrMp = int.MaxValue;
        equipment.OnEquipChange();
        before = equipment.CalculateAttribute();
        Equal(before.MaxHp, role.mainRoleInfo.BaseInfo.CurrHp, "当前生命按最终上限裁切");
        Equal(before.MaxMp, role.mainRoleInfo.BaseInfo.CurrMp, "当前法力按最终上限裁切");
        Equal(200, role.mainRoleInfo.BaseInfo.MaxMp, "法力上限仍保存裸值");
    }

    /// <summary>用真实怪物防御、场景生命周期及正式 ChangeHP 验证破甲和击败事件。</summary>
    private static void VerifyMonsterCombat()
    {
        // 独立正式副本实例，初始化路径会创建同一批权威怪物组件。
        SceneWaveSpawner scene = new SceneWaveSpawner(4060);
        // 反射仅用于测试访问私有实例集合，不向正式业务暴露测试入口。
        FieldInfo monstersField = typeof(SceneWaveSpawner).GetField("monsters_", BindingFlags.Instance | BindingFlags.NonPublic);
        // 集合中的对象就是本次真实世界更新的原怪物引用。
        List<SceneMonster> monsters = (List<SceneMonster>)monstersField.GetValue(scene);
        // 当前入口已包含剧情和零生命机制实体，记录本次战斗波次追加的起点。
        int waveStart = monsters.Count;
        // 测试通过正式创建入口激活22波；object[] 仅用于私有方法的反射调用边界。
        MethodInfo activateWave = typeof(SceneWaveSpawner).GetMethod("ActivateWave", BindingFlags.Instance | BindingFlags.NonPublic);
        activateWave.Invoke(scene, new object[] { 22 });
        Check(monsters.Count > waveStart, "正式副本创建了权威怪物");
        // 目标取本次明确激活的战斗波，不把入口剧情实体假定为可战斗怪物。
        SceneMonster target = monsters[waveStart];
        // 其他实体移出测试命中盒，测试不改正式出生配置。
        for (int i = 0; i < monsters.Count; i++)
        {
            if (i != waveStart) monsters[i].Transform.X = 100000;
        }
        target.Transform.X = 0;
        target.Transform.Y = 0;
        target.Transform.Z = 1.7f;
        // 目标的真实 GeneComponent 会由 SceneWaveSpawner.Update 推进。
        GeneRuntime runtime = target.Components.Get<GeneComponent>(EntityComponentType.Gene).Runtime;
        // 当前模板从原 EntityTable 映射得到的基础防御，不能使用假设的零防。
        MonsterDefenseAttributes defense = MonsterDefenseAttributes.Get(target.ConfigId);
        Check(defense.DefExternal > 0 && defense.DefInternal > 0, "目标实际外内防非零");
        // 两种攻击都为正，便于分别检测外破甲和内破甲的真实扣血增益。
        OnlineRole role = CreateRole(1, 93002);
        RoleBaseInfo attributes = role.GetComponent<RoleEquipComponent>().CalculateAttribute();
        // 未破甲的正式外功+内功伤害结果。
        int damage = defense.CalculateDamage(attributes, runtime, 100);
        runtime.AddGene(82101, 100, null, 0, role.GetID());
        Check(defense.CalculateDamage(attributes, runtime, 100) > damage, "外破甲提高真实结算伤害");
        runtime.RemoveGene(82101, 100);
        runtime.AddGene(83101, 100, null, 0, role.GetID());
        Check(defense.CalculateDamage(attributes, runtime, 100) > damage, "内破甲提高真实结算伤害");
        // 八秒由真实场景世界推进，不是测试直接调用 Runtime.Update。
        scene.Update(8000, new MonsterAttrUpdateNtf());
        Equal(damage, defense.CalculateDamage(attributes, runtime, 100), "场景八秒推进恢复原防御");
        // 装备加成进入伤害计算；裸属性没有被写回。
        role.GetComponent<RoleEquipComponent>().Runtime.AddGene(3, 101, 100);
        attributes = role.GetComponent<RoleEquipComponent>().CalculateAttribute();
        Check(defense.CalculateDamage(attributes, runtime, 100) > damage, "最终装备攻击实际进入普攻伤害");
        // 反射只调用现有的六参数正式命中入口，不替代命中或死亡算法。
        MethodInfo changeHp = typeof(SceneWaveSpawner).GetMethod("ChangeHP", BindingFlags.Instance | BindingFlags.NonPublic);
        role.GetComponent<RoleEquipComponent>().Runtime.AddGene(84001, 102, null, 0, role.GetID());
        role.GetComponent<RoleEquipComponent>().Runtime.AddGene(85001, 103, null, 0, role.GetID());
        target.CurrentHp = 1;
        // 仅反射调用边界需要 object[]；业务事件载荷仍为 GeneTriggerArgs。
        changeHp.Invoke(scene, new object[] { new MonsterAttrUpdateNtf(), Vector3.Zero, new Vector2(0, 1), 100, new HashSet<ulong>(), role });
        Equal(0, target.CurrentHp, "正式命中流程扣至零");
        Equal(600, role.mainRoleInfo.BaseInfo.CurrHp, "真实新死亡触发一次10%回血");
        Equal(70, role.mainRoleInfo.BaseInfo.CurrMp, "真实新死亡触发一次10%回蓝");
        // 即使用新的命中集合，已经零血的怪物也不能再次奖励。
        changeHp.Invoke(scene, new object[] { new MonsterAttrUpdateNtf(), Vector3.Zero, new Vector2(0, 1), 100, new HashSet<ulong>(), role });
        Equal(600, role.mainRoleInfo.BaseInfo.CurrHp, "已死亡目标不重复触发击败回血");
        Equal(70, role.mainRoleInfo.BaseInfo.CurrMp, "已死亡目标不重复触发击败回蓝");
    }

    /// <summary>验证已有移动入口的持续控制与怪物暴击标记不会在受击事件前丢失。</summary>
    private static void VerifyControlAndCritical()
    {
        // 只使用正式角色组件，不挂测试组件到场景。
        OnlineRole role = CreateRole(1, 93003);
        // 让真实移动入口建立移动标记后再施加眩晕。
        Server::ROMoveComponent movement = role.GetComponent<Server::ROMoveComponent>();
        movement.OnMoveStart(new MoveOperationReq { Dx = 1, Dy = 0, Dz = 1, IsMoving = true });
        Check(movement.IsMoving(), "真实移动入口建立移动标记");
        role.GetComponent<RoleEquipComponent>().Runtime.AddGene(1301);
        movement.Update(50);
        Check(!movement.IsMoving(), "持续眩晕结束已有移动标记");
        movement.OnMoveStart(new MoveOperationReq { Dx = 100, Dy = 0, Dz = 100, IsMoving = true });
        Check(role.mainRoleInfo.BaseInfo.Pos == "1_0_1", "受控后正式移动入口拒绝新位移");
        // 测试面板仅用于确定性覆盖100%暴击分支，不改正式角色配置或概率公式。
        RoleBaseInfo target = new RoleBaseInfo { Level = 1, DefExternal = 500, CritResistPoint = -100 };
        // 同一次随机计算向调用方返回真实暴击结果。
        bool critical;
        // 确定性随机源让兼容旧接口与新接口可直接比较。
        int damage = Server::OrcBasicAttackDamage.Calculate(target, new Random(17), out critical);
        Check(critical && damage > 0, "真实暴击结果和伤害同时返回");
        Equal(damage, Server::OrcBasicAttackDamage.Calculate(target, new Random(17)), "保留旧伤害接口的相同结果");
        target.CritResistPoint = 1000;
        Server::OrcBasicAttackDamage.Calculate(target, new Random(17), out critical);
        Check(!critical, "非暴击结果正确透传");
    }

    /// <summary>建立具有正式组件生命周期的测试角色，只存在于独立进程。</summary>
    /// <param name="job">装备穿戴判定需要的职业编号。</param>
    /// <param name="id">测试角色唯一编号。</param>
    /// <returns>已完成正式组件初始化的在线角色。</returns>
    private static OnlineRole CreateRole(int job, int id)
    {
        // 裸属性用于观察每次装备与基因增量，不写任何用户存档。
        OnlineRole role = new OnlineRole { mainRoleInfo = new MainRoleInfo { BaseInfo = new RoleBaseInfo
        {
            RoleId = id, JobId = job, Level = 1000, MaxHp = 1000, CurrHp = 500, MaxMp = 200, CurrMp = 50,
            AtkExternalMin = 100, AtkExternalMax = 100, AtkInternalMin = 100, AtkInternalMax = 100, Pos = "0_0_0"
        } } };
        role.InitSerializes();
        role.InitializeSaveVersion(0);
        role.CompleteRoleDataLoad();
        return role;
    }

    /// <summary>统计实际运行中的指定特技根，不读取配置来推测是否生效。</summary>
    /// <param name="runtime">被测角色的原运行容器。</param>
    /// <param name="id">需要查找的根配置编号。</param>
    /// <returns>实际活跃根数量。</returns>
    private static int CountRoots(GeneRuntime runtime, int id)
    {
        // 计数只遍历实际生效的根实例，不包含尚未触发的子配置。
        int count = 0;
        for (int i = 0; i < runtime.ActiveGenes.Count; i++) if (runtime.ActiveGenes[i].GeneID == id) count++;
        return count;
    }

    /// <summary>比较最终数值，容忍百分比计算的浮点尾差。</summary>
    /// <param name="expected">明确预期的结果。</param>
    /// <param name="actual">实际正式代码返回值。</param>
    /// <param name="message">失败场景说明。</param>
    private static void Equal(double expected, double actual, string message)
    {
        Check(Math.Abs(expected - actual) < 0.0001, message + "，实际=" + actual + "，期望=" + expected);
    }

    /// <summary>累计通过项，失败保留场景信息并使测试进程失败。</summary>
    /// <param name="condition">实际行为是否符合契约。</param>
    /// <param name="message">失败时的中文断言说明。</param>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed++;
    }
}
