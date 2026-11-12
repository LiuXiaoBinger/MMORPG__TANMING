using System;
using Google.Protobuf;

/// <summary>在线角色的实际属性组件；节点直接修改修正与资源，裸属性仍按原协议保存。</summary>
public class RoleAttributeComponent : RoleComponentBase, IAttributeComponent
{
    /// <summary>已由节点 OnCall 写入的属性和状态修正，随宿主生命周期释放。</summary>
    private readonly AttributeModifiers modifiers = new AttributeModifiers();
    /// <summary>高级属性编号下界，与 GeneAttributeType 中第一个高级属性 101 对齐。</summary>
    private const int AdvancedAttributeFirstId = 101;
    /// <summary>高级属性编号上界，与 GeneAttributeType 中最后一个高级属性 144 对齐。</summary>
    private const int AdvancedAttributeLastId = 144;
    /// <summary>读取按来源计数并处理霸体冲突后的有效状态。</summary>
    public GeneState States { get { return modifiers.States; } }
    /// <summary>使用属性组件中已生效的修正计算最终属性。</summary>
    /// <param name="attribute">要读取的属性。</param>
    /// <param name="baseValue">不含基因的原始基础值。</param>
    /// <returns>应用固定值和百分比后的结果。</returns>
    public double CalculateAttribute(GeneAttributeType attribute, double baseValue) { return modifiers.CalculateAttribute(attribute, baseValue); }
    /// <summary>节点执行或撤销时直接更新属性组件。</summary>
    /// <param name="attribute">被修改的属性。</param>
    /// <param name="delta">固定点数或百分点增量。</param>
    /// <param name="valueType">0 为固定值，1 为百分比。</param>
    public void ChangeAttribute(GeneAttributeType attribute,
        double delta, int valueType)
    {
        modifiers.ChangeAttribute(attribute, delta, valueType);
        ResourcesChanged = true;

    }
    /// <summary>增加或撤销节点贡献的一份状态来源。</summary>
    /// <param name="state">本节点提供的状态位。</param>
    /// <param name="delta">增加为 1，撤销为 -1。</param>
    public void ChangeState(GeneState state, int delta) { modifiers.ChangeState(state, delta); }

    /// <summary>资源或属性修正已变化，角色基因组件在本帧结束时发送最终面板。</summary>
    internal bool ResourcesChanged { get; private set; }
    /// <summary>当前生效的最大生命值，至少为 1。</summary>
    public int MaxHp { get { return Math.Max(1, (int)CalculateAttribute(GeneAttributeType.MaxHp, Owner.mainRoleInfo.BaseInfo.MaxHp)); } }
    /// <summary>当前生效的最大法力值，至少为 0。</summary>
    public int MaxMp { get { return Math.Max(0, (int)CalculateAttribute(GeneAttributeType.MaxMp, Owner.mainRoleInfo.BaseInfo.MaxMp)); } }
    /// <summary>当前治疗增益，单位百分点。</summary>
    public double HealBonus { get { return CalculateAttribute(GeneAttributeType.HealBonus, Owner.mainRoleInfo.BaseInfo.HealBonus); } }

    /// <summary>绑定真实在线角色，复用其原有裸属性和当前资源。</summary>
    /// <param name="owner">本组件所属的在线角色。</param>
    public RoleAttributeComponent(OnlineRole owner) : base(owner) { }

    /// <summary>属性槽位初始化时同步初始化角色的基因组件，保证节点容器早于首帧生效。</summary>
    public override void Initialize()
    {
        // 角色基因组件的权威引用在 OnlineRole 上，本槽位只代为驱动生命周期。
        Owner.Gene.Initialize();
    }

    /// <summary>逐帧推进基因节点；变化由基因组件自行合并进本组件的属性通知。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧经过的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        // 同一份基因实例由属性槽位推进，角色不再单独占用组件槽位。
        Owner.Gene.Update(deltaMilliseconds);
    }

    /// <summary>从裸属性生成最终面板，不把装备加成回写到裸属性或数据库。</summary>
    /// <returns>独立的最终属性协议对象，当前资源不超过生效后的上限。</returns>
    public virtual RoleBaseInfo CalculateAttribute()
    {
        // Clone 保留角色身份、位置与当前资源；只在返回对象中合成属性。
        RoleBaseInfo result = Owner.mainRoleInfo.BaseInfo.Clone();
        result.MaxHp = Math.Max(1, (int)CalculateAttribute(GeneAttributeType.MaxHp, result.MaxHp));
        result.MaxMp = Math.Max(0, (int)CalculateAttribute(GeneAttributeType.MaxMp, result.MaxMp));
        result.AtkExternalMin = (int)CalculateAttribute(GeneAttributeType.AtkExternalMin, result.AtkExternalMin);
        result.AtkExternalMax = (int)CalculateAttribute(GeneAttributeType.AtkExternalMax, result.AtkExternalMax);
        result.AtkInternalMin = (int)CalculateAttribute(GeneAttributeType.AtkInternalMin, result.AtkInternalMin);
        result.AtkInternalMax = (int)CalculateAttribute(GeneAttributeType.AtkInternalMax, result.AtkInternalMax);
        // 裸面板防御已包含其 Delta，新增 Delta 基因只补本次变动部分。
        result.DefExternalDelta = (int)CalculateAttribute(GeneAttributeType.DefExternalDelta, result.DefExternalDelta);
        result.DefInternalDelta = (int)CalculateAttribute(GeneAttributeType.DefInternalDelta, result.DefInternalDelta);
        result.DefExternal = (int)CalculateAttribute(GeneAttributeType.DefExternal, result.DefExternal)
            + result.DefExternalDelta - Owner.mainRoleInfo.BaseInfo.DefExternalDelta;
        result.DefInternal = (int)CalculateAttribute(GeneAttributeType.DefInternal, result.DefInternal)
            + result.DefInternalDelta - Owner.mainRoleInfo.BaseInfo.DefInternalDelta;
        result.HitPoint = (int)CalculateAttribute(GeneAttributeType.HitPoint, result.HitPoint);
        result.BlockPoint = (int)CalculateAttribute(GeneAttributeType.BlockPoint, result.BlockPoint);
        result.ZhenJi = (int)CalculateAttribute(GeneAttributeType.ZhenJi, result.ZhenJi);
        result.WuGu = (int)CalculateAttribute(GeneAttributeType.WuGu, result.WuGu);
        result.CritPoint = (int)CalculateAttribute(GeneAttributeType.CritPoint, result.CritPoint);
        result.CritResistPoint = (int)CalculateAttribute(GeneAttributeType.CritResistPoint, result.CritResistPoint);
        result.CritDamage = (float)CalculateAttribute(GeneAttributeType.CritDamage, result.CritDamage);
        result.CritDamageReduce = (float)CalculateAttribute(GeneAttributeType.CritDamageReduce, result.CritDamageReduce);
        result.HealStrength = (int)CalculateAttribute(GeneAttributeType.HealStrength, result.HealStrength);
        result.HealBonus = (float)CalculateAttribute(GeneAttributeType.HealBonus, result.HealBonus);
        result.ZhuXin = (int)CalculateAttribute(GeneAttributeType.ZhuXin, result.ZhuXin);
        result.JianRen = (int)CalculateAttribute(GeneAttributeType.JianRen, result.JianRen);
        // 高级属性以角色原有数值为基础，未配置时按 0 计算，再叠加已生效基因修正。
        for (int id = AdvancedAttributeFirstId; id <= AdvancedAttributeLastId; id++)
        {
            // 原面板该编号没有配置时，基础值就是 0。
            double baseValue;
            result.AdvancedAttributes.TryGetValue(id, out baseValue);
            // 固定值与百分点修正都已在属性组件内累计完成。
            double value = CalculateAttribute((GeneAttributeType)id, baseValue);
            // 只保留非零项，客户端缺省显示 0，避免每帧下发 44 项空值。
            if (value != 0)
            {
                result.AdvancedAttributes[id] = value;
                continue;
            }
            result.AdvancedAttributes.Remove(id);
        }
        result.CurrHp = Math.Min(result.CurrHp, result.MaxHp);
        result.CurrMp = Math.Min(result.CurrMp, result.MaxMp);
        return result;
    }

    /// <summary>直接改变角色生命，负责上下限和既有死亡状态处理。</summary>
    /// <param name="delta">已由具体节点算好的生命增量，伤害为负。</param>
    /// <param name="firerId">实际施加角色编号，当前角色资源流程不额外记录仇恨。</param>
    public void ChangeHp(int delta, ulong firerId)
    {
        // 沿用真实资源对象，不创建另一份待结算数据。
        RoleBaseInfo resources = Owner.mainRoleInfo.BaseInfo;
        if (resources.CurrHp <= 0) return;
        resources.CurrHp = Math.Max(0, Math.Min(MaxHp, resources.CurrHp + delta));
        ResourcesChanged = true;
        if (resources.CurrHp == 0)
        {
            Owner.GetComponent<RoleSkillComponent>().InterruptActiveCast(true);
            Owner.GetComponent<StateMachineComponent>().ForceToDefaultState(true);
        }
    }

    /// <summary>复活入口专用：按最终装备和基因属性恢复满生命、满法力，不改变裸属性上限。</summary>
    public void RestoreReviveResources()
    {
        Owner.mainRoleInfo.BaseInfo.CurrHp = MaxHp;
        Owner.mainRoleInfo.BaseInfo.CurrMp = MaxMp;
        ResourcesChanged = true;
    }

    /// <summary>直接改变角色法力，普通资源恢复不影响已死亡角色。</summary>
    /// <param name="delta">已由具体节点算好的法力增量。</param>
    public void ChangeMp(int delta)
    {
        // 当前资源始终写入角色原数据，最终上限由本属性组件计算。
        RoleBaseInfo resources = Owner.mainRoleInfo.BaseInfo;
        if (resources.CurrHp <= 0) return;
        resources.CurrMp = Math.Max(0, Math.Min(MaxMp, resources.CurrMp + delta));
        ResourcesChanged = true;
    }

    /// <summary>同步给角色自身；只把上限裁切后的当前资源写回权威数据，保留裸属性。</summary>
    public virtual void NotifyAttributeUpdate()
    {
        // 最终面板用于下行，不能覆盖 BaseInfo 中的裸攻击、防御和资源上限。
        RoleBaseInfo attributes = CalculateAttribute();
        Owner.mainRoleInfo.BaseInfo.CurrHp = attributes.CurrHp;
        Owner.mainRoleInfo.BaseInfo.CurrMp = attributes.CurrMp;
        ResourcesChanged = false;
        // 原属性通知同时承载本次节点变化，保持网关和角色UI的既有入口。
        RoleAttributeUpdateNtf notification = new RoleAttributeUpdateNtf { BaseInfo = attributes };
        Owner.Gene.AppendSynchronization(notification);
        Owner.SendToClient(NetDefine.CMD_RoleAttributeUpdateCode, notification.ToByteString());
    }

    /// <summary>角色离线时先撤销基因节点效果，再释放已生效的属性修正与宿主引用。</summary>
    public override void Dispose()
    {
        // 基因节点会直接改属性修正，必须先于修正清理撤销，避免悬空状态。
        Owner.Gene.Shutdown();
        modifiers.Clear();
        base.Dispose();
    }
}
