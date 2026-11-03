using cfg;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 保存实体的生命值和魔法值，并在数值变化时通知实体上的监听者。
/// 移动速度取自全局移动配置；角色动画表现表通过角色职业查找。
/// </summary>
public sealed class EntityAttributesComponent : EntityComponentBase
{

    #region 字段与属性
    /// <summary>最大生命值。</summary>
    public int MaxHp { get; private set; }

    /// <summary>当前生命值，不会超过 MaxHp。</summary>
    public int CurrentHp { get; private set; }

    /// <summary>最大魔法值。</summary>
    public int MaxMp { get; private set; }

    /// <summary>当前魔法值，不会超过 MaxMp。</summary>
    public int CurrentMp { get; private set; }

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
        MaxHp = 0;
        CurrentHp = 0;
        MaxMp = 0;
        CurrentMp = 0;
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
        // 限制数值范围：不允许负数
        if (maxHp < 0) maxHp = 0;
        if (maxMp < 0) maxMp = 0;
        if (currentHp < 0) currentHp = 0;
        if (currentMp < 0) currentMp = 0;
        // 限制数值范围：当前值不得超过最大值
        if (currentHp > maxHp) currentHp = maxHp;
        if (currentMp > maxMp) currentMp = maxMp;

        // 记录变化前是否与新值不同，避免无变化时重复派发事件
        bool hpChanged = MaxHp != maxHp || CurrentHp != currentHp;
        // 魔法上限或当前值是否变化，决定是否发送 MpChanged。
        bool mpChanged = MaxMp != maxMp || CurrentMp != currentMp;
        MaxHp = maxHp;
        CurrentHp = currentHp;
        MaxMp = maxMp;
        CurrentMp = currentMp;
        if (hpChanged)
        {
            DispatchEvent(MEventType.HpChanged, null);
        }
        if (mpChanged)
        {
            DispatchEvent(MEventType.MpChanged, null);
        }
        // 综合属性变更事件用于血条/蓝条等整体刷新
        if (hpChanged || mpChanged)
        {
            DispatchEvent(MEventType.AttributesChanged, null);
        }
    }
    #endregion
}
