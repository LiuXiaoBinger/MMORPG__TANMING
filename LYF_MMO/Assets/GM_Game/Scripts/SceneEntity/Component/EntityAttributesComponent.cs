using cfg;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 实体战斗属性组件，对应参考项目 AttributeComponent。
/// 维护实体最大/当前生命值与魔法值，并在数值变化时派发 HP/MP/综合属性变更事件。
/// </summary>
public sealed class EntityAttributesComponent : EntityComponentBase
{
    /// <summary>最大生命值。</summary>
    public int MaxHp { get; private set; }

    /// <summary>当前生命值，不会超过 MaxHp。</summary>
    public int CurrentHp { get; private set; }

    /// <summary>最大魔法值。</summary>
    public int MaxMp { get; private set; }

    /// <summary>当前魔法值，不会超过 MaxMp。</summary>
    public int CurrentMp { get; private set; }

    /// <summary>构造属性组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityAttributesComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Attributes"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Attributes; }
    }

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

    public float RunSpeed
    { get => PlayStateMgr.Instance.stats.runningTopSpeed; }

    /// <summary>骑乘坐骑速度</summary>
    public float VehicleSpeed
    {
        get { return 0; }
    }

    public float RunAtorSpeed {  get
    {
        /*if (Entity.IsEagle)
        {
            return 1;
        }*/
        return 1;
    } }

    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }

    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }

    /// <summary>一次性应用一组 HP/MP 属性，自动做范围钳制，并按变化情况派发事件。</summary>
    /// <param name="maxHp">新的最大生命值。</param>
    /// <param name="currentHp">新的当前生命值。</param>
    /// <param name="maxMp">新的最大魔法值。</param>
    /// <param name="currentMp">新的当前魔法值。</param>
    public void Apply(int maxHp, int currentHp, int maxMp, int currentMp)
    {
        // 范围钳制：不允许负数
        if (maxHp < 0) maxHp = 0;
        if (maxMp < 0) maxMp = 0;
        if (currentHp < 0) currentHp = 0;
        if (currentMp < 0) currentMp = 0;
        // 范围钳制：当前值不得超过最大值
        if (currentHp > maxHp) currentHp = maxHp;
        if (currentMp > maxMp) currentMp = maxMp;

        // 记录变化前是否与新值不同，避免无变化时重复派发事件
        bool hpChanged = MaxHp != maxHp || CurrentHp != currentHp;
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

    /// <summary>对象池回收时清零全部属性。</summary>
    public override void Shutdown()
    {
        MaxHp = 0;
        CurrentHp = 0;
        MaxMp = 0;
        CurrentMp = 0;
    }
    
    
    /// <summary>当前生效外观表现表（无视图外观时回退真实外观）。</summary>
    public PresentTable PresentDataView
    {
        get
        {
            if (Entity.IsRole)
            {
                RoleEntity role = Entity as RoleEntity;
                if (role != null)
                {
                    //职业id
                    int Jobid = role.BaseInfo.JobId;
                    ProfessionInfo professionInfo= LubanMgr.Instance.GetProfessionInfoById(Jobid);
                    PresentTable presentTable = LubanMgr.Instance.GetPresentTableById(professionInfo.PresentTableID);
                    return presentTable;
                }
            }

            return null;
        }
    }
}
