using System;
using System.Collections.Generic;

/// <summary>在线角色的穿戴属性与装备特技；裸属性仍保存在 BaseInfo，运行加成不进入裸属性存档。</summary>
public class RoleEquipComponent : RoleComponentBase
{
    /// <summary>当前真正挂载了基础词条的装备 UID，换装只增删发生变化的来源。</summary>
    private readonly HashSet<long> attachedEquipment = new HashSet<long>();
    /// <summary>按特技族保存当前唯一生效等级，未变等级保留原次数和冷却。</summary>
    private readonly Dictionary<int, GeneBase> activeSpecialSkills = new Dictionary<int, GeneBase>();
    /// <summary>装备操作访问角色自己的基因容器，本装备组件不再持有通用基因集合。</summary>
    public GeneRuntime Runtime { get { return Owner.Gene.Runtime; } }

    /// <summary>绑定实际在线角色，不依赖尚未建立的玩家场景实体。</summary>
    /// <param name="owner">持有裸属性、当前资源和背包的在线角色。</param>
    public RoleEquipComponent(OnlineRole owner) : base(owner) { }

    /// <summary>沿用项目已经定义的装备模块编号。</summary>
    /// <returns>装备模块的稳定编号。</returns>
    public override RoleModuleType GetModuleId() { return RoleModuleType.kRoleModuleTypeEquip; }

    /// <summary>装载、穿脱或启用特技变更后，按当前穿戴容器更新来源及同名特技等级。</summary>
    public virtual void OnEquipChange()
    {
        // 原物品引用的可索引列表；挂载期间不会修改背包事务。
        List<ItemBase> items = new List<ItemBase>(Owner.GetComponent<RoItemComponent>().GetContainer(KnapsackType.RoleCurrtEquipPack).GetItemMap().Values);
        // 当前实际穿戴的 UID，不包含背包中尚未装备的物品。
        HashSet<long> currentIds = new HashSet<long>();
        // 同名特技族的启用件数决定本次生效等级。
        Dictionary<int, int> specialCounts = new Dictionary<int, int>();
        // 同名族采用最小装备 UID 作为可追溯来源，换装后不伪造角色来源。
        Dictionary<int, ulong> specialSources = new Dictionary<int, ulong>();
        for (int i = 0; i < items.Count; i++)
        {
            // 穿戴容器只接受正式装备实例。
            EquipBase equipment = (EquipBase)items[i];
            // UID 属于物品实例，不能拿来当施加角色编号。
            long uid = equipment.GetItemUID();
            currentIds.Add(uid);
            if (!attachedEquipment.Contains(uid)) AddEquipGenes(equipment);
            // 每件仅启用一个保存的特技族；其余两个槽不参与件数。
            int specialId = equipment.GetActiveSpecialSkillId();
            if (specialId == 0) continue;
            if (!specialCounts.ContainsKey(specialId))
            {
                specialCounts.Add(specialId, 0);
                specialSources.Add(specialId, (ulong)uid);
            }
            specialCounts[specialId]++;
            specialSources[specialId] = Math.Min(specialSources[specialId], (ulong)uid);
        }
        // 已卸下装备的所有基础槽和自身触发效果只按这个装备来源移除。
        List<long> oldIds = new List<long>(attachedEquipment);
        for (int i = 0; i < oldIds.Count; i++)
        {
            if (!currentIds.Contains(oldIds[i])) Runtime.RemoveGenesBySource((ulong)oldIds[i]);
        }
        attachedEquipment.Clear();
        attachedEquipment.UnionWith(currentIds);
        RefreshSpecialSkills(specialCounts, specialSources);
        NotifyAttributeUpdate();
    }

    /// <summary>按六个配置槽与三个实例词条挂载，重复编号使用独立槽累计。</summary>
    /// <param name="equipment">已经进入穿戴容器的原装备实例。</param>
    private void AddEquipGenes(EquipBase equipment)
    {
        // 装备基础值只从正式配置读取，不修改实例词条或裸角色属性。
        cfg.EquipInfo config = LubanMgr.Instance.GetEquipInfoById(equipment.GetItemID());
        // 与数值数组一一对应的六个基础槽和三个实例槽。
        int[] ids = { config.GeneId0, config.GeneId1, config.GeneId2, config.GeneId3, config.GeneId4, config.GeneId5,
            equipment.GeneID0, equipment.GeneID1, equipment.GeneID2 };
        // 每槽的实际数值；相同编号也不互相覆盖。
        int[] values = { config.GeneValue0, config.GeneValue1, config.GeneValue2, config.GeneValue3, config.GeneValue4, config.GeneValue5,
            equipment.GeneValue0, equipment.GeneValue1, equipment.GeneValue2 };
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] != 0) Runtime.AddGene(ids[i], (ulong)equipment.GetItemUID(), values[i], i + 1, Owner.GetID());
        }
    }

    /// <summary>按同族启用件数选择最高可用等级；未变化的实例保留冷却和次数。</summary>
    /// <param name="counts">各特技族当前启用的装备件数。</param>
    /// <param name="sources">各族当前最小装备 UID，供来源归因。</param>
    private void RefreshSpecialSkills(Dictionary<int, int> counts, Dictionary<int, ulong> sources)
    {
        // 先移除已没有任何装备启用的特技族。
        List<int> oldFamilies = new List<int>(activeSpecialSkills.Keys);
        for (int i = 0; i < oldFamilies.Count; i++)
        {
            if (counts.ContainsKey(oldFamilies[i])) continue;
            Runtime.RemoveGene(activeSpecialSkills[oldFamilies[i]]);
            activeSpecialSkills.Remove(oldFamilies[i]);
        }
        // 配置是族与等级的扁平表，仅检查当前启用的族。
        List<cfg.EquipSpecialSkillInfo> definitions = LubanMgr.Instance.GetEquipSpecialSkillInfos();
        // 字典键转为原值列表，按索引遍历实际启用的特技族。
        List<int> families = new List<int>(counts.Keys);
        for (int i = 0; i < families.Count; i++)
        {
            // 当前族编号与最终选择的等级行；超过最高等级时自然封顶。
            int family = families[i];
            // 选择不超过启用件数的最高配置等级。
            cfg.EquipSpecialSkillInfo selected = null;
            for (int j = 0; j < definitions.Count; j++)
            {
                // 当前可选行，仅属于同一族时才比较等级。
                cfg.EquipSpecialSkillInfo row = definitions[j];
                if (row.SpecialSkillId != family || row.Level > counts[family]) continue;
                if (selected == null || row.Level > selected.Level) selected = row;
            }
            if (selected == null) continue;
            // 现有原实例决定是否真正需要重新挂载，重复刷新不会重置冷却。
            GeneBase existing;
            if (activeSpecialSkills.TryGetValue(family, out existing))
            {
                if (existing.GeneID == selected.GeneId && existing.SourceId == sources[family]) continue;
                Runtime.RemoveGene(existing);
            }
            activeSpecialSkills[family] = Runtime.AddGene(selected.GeneId, sources[family], null, 100 + family, Owner.GetID());
        }
    }

    /// <summary>既有装备面板入口转交角色属性组件计算。</summary>
    /// <returns>包含当前所有生效修正的最终属性。</returns>
    public virtual RoleBaseInfo CalculateAttribute() { return Owner.AttrComp.CalculateAttribute(); }

    /// <summary>既有装备特技入口转交角色自己的基因组件。</summary>
    /// <param name="args">实际战斗事件及目标。</param>
    /// <returns>成功施加的关联效果数。</returns>
    public virtual int Trigger(GeneTriggerArgs args) { return Owner.Gene.Trigger(args); }

    /// <summary>换装完成后由角色属性组件发布最终属性。</summary>
    public virtual void NotifyAttributeUpdate() { Owner.AttrComp.NotifyAttributeUpdate(); }

    /// <summary>释放装备挂载索引；角色通用基因集合由角色 Gene 组件释放。</summary>
    public override void Dispose()
    {
        attachedEquipment.Clear();
        activeSpecialSkills.Clear();
        base.Dispose();
    }
}
