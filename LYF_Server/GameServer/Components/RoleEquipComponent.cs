using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>在线角色的穿戴属性与装备特技；裸属性仍保存在 BaseInfo，运行加成不进入裸属性存档。</summary>
public class RoleEquipComponent : RoleComponentBase
{
    /// <summary>当前真正挂载了基础词条的装备 UID，换装只增删发生变化的来源。</summary>
    private readonly HashSet<long> attachedEquipment = new HashSet<long>();
    /// <summary>按特技族保存当前唯一生效等级，未变等级保留原次数和冷却。</summary>
    private readonly Dictionary<int, GeneBase> activeSpecialSkills = new Dictionary<int, GeneBase>();
    /// <summary>即时恢复已改变资源，本逻辑帧结束或事件处理结束时发布一次最终属性。</summary>
    private bool resourcesChanged;
    /// <summary>角色持有的唯一装备及临时基因容器，随在线角色释放。</summary>
    public GeneRuntime Runtime { get; private set; }

    /// <summary>绑定实际在线角色，不依赖尚未建立的玩家场景实体。</summary>
    /// <param name="owner">持有裸属性、当前资源和背包的在线角色。</param>
    public RoleEquipComponent(OnlineRole owner) : base(owner) { }

    /// <summary>连接共享基因节点工厂并订阅即时资源结果。</summary>
    public override void Initialize()
    {
        Runtime = new GeneRuntime(LubanMgr.Instance.GetGeneFactory());
        Runtime.Pulse += ApplyPulse;
    }

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

    /// <summary>从裸属性生成最终面板，不把装备加成回写到裸属性或数据库。</summary>
    /// <returns>独立的最终属性协议对象，当前资源不超过生效后的上限。</returns>
    public virtual RoleBaseInfo CalculateAttribute()
    {
        // Clone 保留角色身份、位置与当前资源；只在返回对象中合成属性。
        RoleBaseInfo result = Owner.mainRoleInfo.BaseInfo.Clone();
        result.MaxHp = Math.Max(1, (int)Runtime.CalculateAttribute(GeneAttributeType.MaxHp, result.MaxHp));
        result.MaxMp = Math.Max(0, (int)Runtime.CalculateAttribute(GeneAttributeType.MaxMp, result.MaxMp));
        result.AtkExternalMin = (int)Runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, result.AtkExternalMin);
        result.AtkExternalMax = (int)Runtime.CalculateAttribute(GeneAttributeType.AtkExternalMax, result.AtkExternalMax);
        result.AtkInternalMin = (int)Runtime.CalculateAttribute(GeneAttributeType.AtkInternalMin, result.AtkInternalMin);
        result.AtkInternalMax = (int)Runtime.CalculateAttribute(GeneAttributeType.AtkInternalMax, result.AtkInternalMax);
        // 裸面板防御已包含其 Delta，新增 Delta 基因只补本次变动部分。
        result.DefExternalDelta = (int)Runtime.CalculateAttribute(GeneAttributeType.DefExternalDelta, result.DefExternalDelta);
        result.DefInternalDelta = (int)Runtime.CalculateAttribute(GeneAttributeType.DefInternalDelta, result.DefInternalDelta);
        result.DefExternal = (int)Runtime.CalculateAttribute(GeneAttributeType.DefExternal, result.DefExternal)
            + result.DefExternalDelta - Owner.mainRoleInfo.BaseInfo.DefExternalDelta;
        result.DefInternal = (int)Runtime.CalculateAttribute(GeneAttributeType.DefInternal, result.DefInternal)
            + result.DefInternalDelta - Owner.mainRoleInfo.BaseInfo.DefInternalDelta;
        result.HitPoint = (int)Runtime.CalculateAttribute(GeneAttributeType.HitPoint, result.HitPoint);
        result.BlockPoint = (int)Runtime.CalculateAttribute(GeneAttributeType.BlockPoint, result.BlockPoint);
        result.ZhenJi = (int)Runtime.CalculateAttribute(GeneAttributeType.ZhenJi, result.ZhenJi);
        result.WuGu = (int)Runtime.CalculateAttribute(GeneAttributeType.WuGu, result.WuGu);
        result.CritPoint = (int)Runtime.CalculateAttribute(GeneAttributeType.CritPoint, result.CritPoint);
        result.CritResistPoint = (int)Runtime.CalculateAttribute(GeneAttributeType.CritResistPoint, result.CritResistPoint);
        result.CritDamage = (float)Runtime.CalculateAttribute(GeneAttributeType.CritDamage, result.CritDamage);
        result.CritDamageReduce = (float)Runtime.CalculateAttribute(GeneAttributeType.CritDamageReduce, result.CritDamageReduce);
        result.HealStrength = (int)Runtime.CalculateAttribute(GeneAttributeType.HealStrength, result.HealStrength);
        result.HealBonus = (float)Runtime.CalculateAttribute(GeneAttributeType.HealBonus, result.HealBonus);
        result.ZhuXin = (int)Runtime.CalculateAttribute(GeneAttributeType.ZhuXin, result.ZhuXin);
        result.JianRen = (int)Runtime.CalculateAttribute(GeneAttributeType.JianRen, result.JianRen);
        result.CurrHp = Math.Min(result.CurrHp, result.MaxHp);
        result.CurrMp = Math.Min(result.CurrMp, result.MaxMp);
        return result;
    }

    /// <summary>把真实事件交给当前角色特技，事件目标仍由战斗入口给出。</summary>
    /// <param name="args">本次权威事件及对方容器。</param>
    /// <returns>成功施加的关联效果数。</returns>
    public virtual int Trigger(GeneTriggerArgs args)
    {
        // 即时资源已由 Pulse 回调结算，事件结束统一通知最终面板。
        int activated = Runtime.Trigger(args);
        if (activated > 0) NotifyAttributeUpdate();
        return activated;
    }

    /// <summary>推进持续效果并在到期或资源改变后同步最终属性。</summary>
    /// <param name="deltaMilliseconds">角色逻辑帧实际经过的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        // 到期移除可能改变生命上限，和周期结算一起在本帧发布。
        int previousCount = Runtime.ActiveGenes.Count;
        // 周期结果由同一套资源入口消费，不再要求外部订阅未使用的事件。
        List<GenePulse> pulses = Runtime.Update(deltaMilliseconds);
        for (int i = 0; i < pulses.Count; i++) ApplyPulse(pulses[i]);
        if (resourcesChanged || previousCount != Runtime.ActiveGenes.Count) NotifyAttributeUpdate();
    }

    /// <summary>按当前最终上限结算资源；治疗增益是百分点，承伤与受疗为百分倍率。</summary>
    /// <param name="pulse">携带真实施加角色和装备来源的即时或周期结果。</param>
    private void ApplyPulse(GenePulse pulse)
    {
        // 死亡角色不由普通装备恢复复活，死亡处理沿用既有技能中断入口。
        RoleBaseInfo resources = Owner.mainRoleInfo.BaseInfo;
        if (resources.CurrHp <= 0) return;
        // 本次恢复或伤害需要的最终生命、法力上限与治疗增益。
        RoleBaseInfo attributes = CalculateAttribute();
        // Percent 以生效后的真实上限计算；数值向零取整。
        int amount = pulse.ResolveAmount(attributes.MaxHp, attributes.MaxMp);
        if (pulse.EffectType == GeneEffectType.PeriodicDamage)
        {
            amount = Math.Max(0, (int)(amount * Runtime.CalculateAttribute(GeneAttributeType.DamageTaken, 100) / 100));
            resources.CurrHp = Math.Max(0, resources.CurrHp - amount);
        }
        else if (pulse.EffectType == GeneEffectType.PeriodicMana || pulse.EffectType == GeneEffectType.InstantMana)
        {
            resources.CurrMp = Math.Min(attributes.MaxMp, resources.CurrMp + amount);
        }
        else
        {
            // 本轮恢复均由自身装备产生；治疗增益不改动原表或裸角色面板。
            amount = Math.Max(0, (int)(amount * (1 + attributes.HealBonus / 100.0)
                * Runtime.CalculateAttribute(GeneAttributeType.HealingReceived, 100) / 100));
            resources.CurrHp = Math.Min(attributes.MaxHp, resources.CurrHp + amount);
        }
        resourcesChanged = true;
        if (resources.CurrHp == 0)
        {
            Owner.GetComponent<RoleSkillComponent>().InterruptActiveCast(true);
            Owner.GetComponent<StateMachineComponent>().ForceToDefaultState(true);
        }
    }

    /// <summary>同步给角色自身；只把上限裁切后的当前资源写回权威数据，保留裸属性。</summary>
    public virtual void NotifyAttributeUpdate()
    {
        // 最终面板用于下行，不能覆盖 BaseInfo 中的裸攻击、防御和资源上限。
        RoleBaseInfo attributes = CalculateAttribute();
        Owner.mainRoleInfo.BaseInfo.CurrHp = attributes.CurrHp;
        Owner.mainRoleInfo.BaseInfo.CurrMp = attributes.CurrMp;
        resourcesChanged = false;
        Owner.SendToClient(NetDefine.CMD_RoleAttributeUpdateCode, new RoleAttributeUpdateNtf { BaseInfo = attributes }.ToByteString());
    }

    /// <summary>离线时解除资源订阅并释放全部在线效果。</summary>
    public override void Dispose()
    {
        Runtime.Pulse -= ApplyPulse;
        Runtime.Clear();
        attachedEquipment.Clear();
        activeSpecialSkills.Clear();
        base.Dispose();
    }
}
