using cfg;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 保存实体的生命值和魔法值，并在数值变化时通知实体上的监听者。
/// 移动速度取自全局移动配置；角色动画表现表通过角色职业查找。
/// </summary>
public sealed class EntityAttributesComponent : EntityComponentBase, IAttributeComponent
{

    /// <summary>已由节点 OnCall 写入的属性和状态修正，随宿主生命周期释放。</summary>
    private readonly AttributeModifiers modifiers = new AttributeModifiers();
    /// <summary>读取按来源计数并处理霸体冲突后的有效状态。</summary>
    public GeneState States
    {
        get
        {
            // 保留原状态入口，服务器节点状态从客户端展示运行器的变化缓存读取。
            GeneState states = modifiers.States;
            if (Entity.Gene != null && Entity.Gene.Runtime != null) states |= Entity.Gene.Runtime.GetStates();
            if ((states & GeneState.SuperArmor) != 0) states &= ~(GeneState.Stun | GeneState.Root);
            return states;
        }
    }
    /// <summary>使用属性组件中已生效的修正计算最终属性。</summary>
    /// <param name="attribute">要读取的属性。</param>
    /// <param name="baseValue">不含基因的原始基础值。</param>
    /// <returns>应用固定值和百分比后的结果。</returns>
    public double CalculateAttribute(GeneAttributeType attribute, double baseValue) { return modifiers.CalculateAttribute(attribute, baseValue); }
    /// <summary>本帧有属性修正变化，等所有节点执行完后裁切资源并通知。</summary>
    private bool attributesChanged;
    /// <summary>本帧第一次修正前的生命上限，用于合并刷新过程的资源通知。</summary>
    private int previousMaxHp;
    /// <summary>本帧第一次修正前的法力上限。</summary>
    private int previousMaxMp;
    /// <summary>节点立即更新属性修正；资源裁切留到本帧节点执行完，避免刷新中途误扣资源。</summary>
    /// <param name="attribute">要修改的属性。</param>
    /// <param name="delta">固定点数或百分点增量。</param>
    /// <param name="valueType">0 为固定值，1 为百分比。</param>
    public void ChangeAttribute(GeneAttributeType attribute, double delta, int valueType)
    {
        if (!attributesChanged)
        {
            previousMaxHp = MaxHp;
            previousMaxMp = MaxMp;
        }
        modifiers.ChangeAttribute(attribute, delta, valueType);
        attributesChanged = true;
    }
    /// <summary>实体先更新专用 Gene 再更新普通属性，此处按最终上限裁切一次资源。</summary>
    /// <param name="deltaMilliseconds">本帧逻辑毫秒；属性修正不依赖时间跨度。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (!attributesChanged) return;
        // 本帧全部 OnCall 完成后的当前生命，防止把刷新中间值当作最终上限。
        int previousHp = CurrentHp;
        // 裁切前的当前法力。
        int previousMp = CurrentMp;
        CurrentHp = System.Math.Min(CurrentHp, MaxHp);
        CurrentMp = System.Math.Min(CurrentMp, MaxMp);
        attributesChanged = false;
        NotifyResourceChange(previousMaxHp, previousHp, previousMaxMp, previousMp, false, false);
    }
    /// <summary>增加或撤销节点贡献的一份状态来源。</summary>
    /// <param name="state">本节点提供的状态位。</param>
    /// <param name="delta">增加为 1，撤销为 -1。</param>
    public void ChangeState(GeneState state, int delta) { modifiers.ChangeState(state, delta); }
    /// <summary>角色使用同步面板的治疗增益，非角色实体的基础治疗增益为零。</summary>
    public double HealBonus
    {
        get
        {
            // 当前宿主可能是玩家、怪物或其他实体。
            RoleEntity role = Entity as RoleEntity;
            // 非角色没有职业面板，基础增益保持零。
            double baseValue = 0;
            if (role != null) baseValue = role.BaseInfo.HealBonus;
            return CalculateAttribute(GeneAttributeType.HealBonus, baseValue);
        }
    }
    /// <summary>节点直接修改当前生命，沿用原属性事件更新客户端表现。</summary>
    /// <param name="delta">节点已算好的生命增量。</param>
    /// <param name="firerId">施加角色编号；客户端表现不执行服务器死亡归因。</param>
    public void ChangeHp(int delta, ulong firerId)
    {
        if (CurrentHp <= 0) return;
        Apply(baseMaxHp, CurrentHp + delta, baseMaxMp, CurrentMp);
    }
    /// <summary>节点直接修改当前法力，沿用现有属性事件。</summary>
    /// <param name="delta">节点已算好的法力增量。</param>
    public void ChangeMp(int delta)
    {
        if (CurrentHp <= 0) return;
        Apply(baseMaxHp, CurrentHp, baseMaxMp, CurrentMp + delta);
    }
    /// <summary>消耗怒气的客户端入口；余额不足返回失败且不扣减，与服务端权威规则一致。</summary>
    /// <param name="amount">本次要消耗的怒气点数。</param>
    /// <returns>足额扣减返回 true，否则返回 false。</returns>
    public bool TryConsumeRage(int amount)
    {
        if (CurrentHp <= 0) return false;
        if (!CombatResourceDefaults.CanConsume(CurrRage, amount)) return false;
        CurrRage = CombatResourceDefaults.ApplyDelta(CurrRage, MaxRage, -amount);
        return true;
    }
    /// <summary>消耗真气的客户端入口；余额不足返回失败且不扣减。</summary>
    /// <param name="amount">本次要消耗的真气点数。</param>
    /// <returns>足额扣减返回 true，否则返回 false。</returns>
    public bool TryConsumeTrueQi(int amount)
    {
        if (CurrentHp <= 0) return false;
        if (!CombatResourceDefaults.CanConsume(CurrTrueQi, amount)) return false;
        CurrTrueQi = CombatResourceDefaults.ApplyDelta(CurrTrueQi, MaxTrueQi, -amount);
        return true;
    }
    /// <summary>恢复怒气，按 0 至 MaxRage 裁切，过大增量不溢出。</summary>
    /// <param name="amount">本次恢复的怒气点数。</param>
    public void RestoreRage(int amount)
    {
        if (CurrentHp <= 0 || amount <= 0) return;
        CurrRage = CombatResourceDefaults.ApplyDelta(CurrRage, MaxRage, amount);
    }
    /// <summary>恢复真气，按 0 至 MaxTrueQi 裁切，过大增量不溢出。</summary>
    /// <param name="amount">本次恢复的真气点数。</param>
    public void RestoreTrueQi(int amount)
    {
        if (CurrentHp <= 0 || amount <= 0) return;
        CurrTrueQi = CombatResourceDefaults.ApplyDelta(CurrTrueQi, MaxTrueQi, amount);
    }

    #region 字段与属性
    /// <summary>最大生命值。</summary>
    public int MaxHp { get { return System.Math.Max(0, (int)CalculateAttribute(GeneAttributeType.MaxHp, baseMaxHp)); } }
    /// <summary>最近一次同步的生命上限，基因修正不写回该基础值。</summary>
    private int baseMaxHp;

    /// <summary>当前生命值，不会超过 MaxHp。</summary>
    public int CurrentHp { get; private set; }

    /// <summary>最大魔法值。</summary>
    public int MaxMp { get { return System.Math.Max(0, (int)CalculateAttribute(GeneAttributeType.MaxMp, baseMaxMp)); } }
    /// <summary>最近一次同步的法力上限，基因修正独立保存在属性组件。</summary>
    private int baseMaxMp;

    /// <summary>当前魔法值，不会超过 MaxMp。</summary>
    public int CurrentMp { get; private set; }

    /// <summary>最近的怒气上限，基因修正不写回该基础值。</summary>
    private int baseMaxRage;
    /// <summary>当前怒气值，不会超过 MaxRage。</summary>
    public int CurrRage { get; private set; }
    /// <summary>怒气上限，使用资源默认值，不做基因复算。</summary>
    public int MaxRage { get { return System.Math.Max(0, baseMaxRage); } }
    /// <summary>最近的真气上限，基因修正不写回该基础值。</summary>
    private int baseMaxTrueQi;
    /// <summary>当前真气值，不会超过 MaxTrueQi。</summary>
    public int CurrTrueQi { get; private set; }
    /// <summary>真气上限，使用资源默认值，不做基因复算。</summary>
    public int MaxTrueQi { get { return System.Math.Max(0, baseMaxTrueQi); } }

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Attributes"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Attributes; }
    }

    /// <summary>载具或跑步的实际移动速度（米/秒）；载具分支当前未启用，直接返回跑步速度。</summary>
    public float VehicleOrRunSpeed {
        get
        {
            /*if (Entity != null)
            {
                if (Entity.IsRideVehicle)
                {
                    return VehicleSpeed;
                }
            }*/
            return RunSpeed;
        }
    }

    /// <summary>跑步移动顶速（米/秒），读取全局移动配置的奔跑顶速。</summary>
    public float RunSpeed
    { get => PlayStateMgr.Instance.stats.runningTopSpeed; }

    /// <summary>预留的坐骑速度，当前固定返回 0</summary>
    public float VehicleSpeed
    {
        get { return 0; }
    }

    /// <summary>跑步时动画机播放速度倍率；鹰类分支当前未启用，固定为 1。</summary>
    public float RunAtorSpeed {  get
    {
        /*if (Entity.IsEagle)
        {
            return 1;
        }*/
        return 1;
    } }

    /// <summary>角色职业对应的表现表；非角色实体返回 null。</summary>
    public PresentTable PresentDataView
    {
        get
        {
            if (Entity.IsRole)
            {
                // 带有 Role 类别的具体角色对象，提供职业同步信息。
                RoleEntity role = Entity as RoleEntity;
                if (role != null)
                {
                    // 角色当前职业编号，用它查职业配置中的表现表编号。
                    int Jobid = role.BaseInfo.JobId;
                    // 职业配置保存模型与动画所用的表现表编号。
                    ProfessionInfo professionInfo= LubanMgr.Instance.GetProfessionInfoById(Jobid);
                    // 最终返回给动画组件的职业表现表。
                    PresentTable presentTable = LubanMgr.Instance.GetPresentTableById(professionInfo.PresentTableID);
                    return presentTable;
                }
            }

            return null;
        }
    }
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>构造属性组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityAttributesComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>数据由公开方法直接写入，此处不订阅事件。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>本组件没有自行订阅事件，无需额外解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }

    /// <summary>对象池回收时清零全部属性。</summary>
    public override void Shutdown()
    {
        baseMaxHp = 0;
        CurrentHp = 0;
        baseMaxMp = 0;
        modifiers.Clear();
        attributesChanged = false;
        previousMaxHp = 0;
        previousMaxMp = 0;
        CurrentMp = 0;
        baseMaxRage = 0;
        CurrRage = 0;
        baseMaxTrueQi = 0;
        CurrTrueQi = 0;
    }
    #endregion

    #region 核心逻辑
    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }

    /// <summary>一次性应用一组 HP/MP 属性，自动做限制数值范围，并按变化情况派发事件。</summary>
    /// <param name="maxHp">新的最大生命值。</param>
    /// <param name="currentHp">新的当前生命值。</param>
    /// <param name="maxMp">新的最大魔法值。</param>
    /// <param name="currentMp">新的当前魔法值。</param>
    public void Apply(int maxHp, int currentHp, int maxMp, int currentMp)
    {
        // 保存修改前资源，应用同步数据后仍使用原事件通道。
        int previousMaxHp = MaxHp;
        // 修改前生命。
        int previousHp = CurrentHp;
        // 修改前法力上限。
        int previousMaxMp = MaxMp;
        // 修改前法力。
        int previousMp = CurrentMp;
        SetHpMp(maxHp, currentHp, maxMp, currentMp);
        NotifyResourceChange(previousMaxHp, previousHp, previousMaxMp, previousMp, false, false);
    }

    /// <summary>只写回四项生命法力权威值，不派发任何事件，供全量应用统一后再通知。</summary>
    /// <param name="maxHp">生命上限。</param>
    /// <param name="currentHp">当前生命。</param>
    /// <param name="maxMp">法力上限。</param>
    /// <param name="currentMp">当前法力。</param>
    private void SetHpMp(int maxHp, int currentHp, int maxMp, int currentMp)
    {
        // 上限裁到非负，当前值裁到 0 与上限之间。
        baseMaxHp = System.Math.Max(0, maxHp);
        baseMaxMp = System.Math.Max(0, maxMp);
        CurrentHp = System.Math.Max(0, System.Math.Min(MaxHp, currentHp));
        CurrentMp = System.Math.Max(0, System.Math.Min(MaxMp, currentMp));
    }

    /// <summary>一次性应用服务端下发的四项完整资源；旧 Apply 四参入口保持不变。</summary>
    /// <param name="maxHp">生命上限。</param>
    /// <param name="currentHp">当前生命。</param>
    /// <param name="maxMp">法力上限。</param>
    /// <param name="currentMp">当前法力。</param>
    /// <param name="maxRage">怒气上限。</param>
    /// <param name="currentRage">当前怒气。</param>
    /// <param name="maxTrueQi">真气上限。</param>
    /// <param name="currentTrueQi">当前真气。</param>
    public void ApplyAll(int maxHp, int currentHp, int maxMp, int currentMp,
        int maxRage, int currentRage, int maxTrueQi, int currentTrueQi)
    {
        // 保存修改前四项，全部应用完成后再统一派发一次事件。
        int previousMaxHp = MaxHp;
        int previousHp = CurrentHp;
        int previousMaxMp = MaxMp;
        int previousMp = CurrentMp;
        int previousMaxRage = MaxRage;
        int previousRage = CurrRage;
        int previousMaxTrueQi = MaxTrueQi;
        int previousTrueQi = CurrTrueQi;
        // 旧协议缺字段或仅 HP/MP 更新时，按共享默认补齐四项，避免清空已有怒气与真气。
        if (maxRage <= 0) { maxRage = CombatResourceDefaults.DefaultRageMax; currentRage = CombatResourceDefaults.DefaultRage; }
        if (maxTrueQi <= 0) { maxTrueQi = CombatResourceDefaults.DefaultTrueQiMax; currentTrueQi = CombatResourceDefaults.DefaultTrueQi; }
        // 先写回四项权威值，不在中途触发事件。
        SetHpMp(maxHp, currentHp, maxMp, currentMp);
        baseMaxRage = System.Math.Max(0, maxRage);
        baseMaxTrueQi = System.Math.Max(0, maxTrueQi);
        CurrRage = CombatResourceDefaults.Clamp(currentRage, MaxRage);
        CurrTrueQi = CombatResourceDefaults.Clamp(currentTrueQi, MaxTrueQi);
        // 四项应用完毕后再按真实变化派发，仅怒气/真气变化也发一次综合事件。
        bool rageChanged = previousMaxRage != MaxRage || previousRage != CurrRage;
        bool qiChanged = previousMaxTrueQi != MaxTrueQi || previousTrueQi != CurrTrueQi;
        NotifyResourceChange(previousMaxHp, previousHp, previousMaxMp, previousMp, rageChanged, qiChanged);
    }

    /// <summary>沿用项目原 HP、MP 和综合属性事件，供血条与面板更新。</summary>
    /// <param name="previousMaxHp">修改前生命上限。</param>
    /// <param name="previousHp">修改前当前生命。</param>
    /// <param name="previousMaxMp">修改前法力上限。</param>
    /// <param name="previousMp">修改前当前法力。</param>
    /// <param name="rageChanged">怒气当前值或上限是否变化。</param>
    /// <param name="qiChanged">真气当前值或上限是否变化。</param>
    private void NotifyResourceChange(int previousMaxHp, int previousHp, int previousMaxMp, int previousMp,
        bool rageChanged, bool qiChanged)
    {
        // 上限或当前值变化均属于资源显示变化。
        bool hpChanged = previousMaxHp != MaxHp || previousHp != CurrentHp;
        // 法力使用同一判断规则。
        bool mpChanged = previousMaxMp != MaxMp || previousMp != CurrentMp;
        if (hpChanged) DispatchEvent(MEventType.HpChanged, null);
        if (mpChanged) DispatchEvent(MEventType.MpChanged, null);
        // 任一项资源变化都派发一次综合事件，供面板与 HUD 统一刷新。
        if (hpChanged || mpChanged || rageChanged || qiChanged) DispatchEvent(MEventType.AttributesChanged, null);
    }
    #endregion
}
