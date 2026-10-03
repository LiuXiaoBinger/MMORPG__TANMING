using MMoRpgCommon.SceneEntity;

/// <summary>
/// 角色实体业务组件，对应参考项目 RORoleComponent。
/// 保存场景实体对应的角色持久化身份（RoleId）与显示名（UserName）。
/// </summary>
public sealed class EntityRoleComponent : EntityComponentBase
{
    /// <summary>构造角色组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityRoleComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Role"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Role; }
    }

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

    /// <summary>角色持久化 RoleId，区分不同玩家/角色实例。</summary>
    public int RoleId { get; set; }

    /// <summary>角色显示名（玩家昵称），用于头顶名等表现。</summary>
    public string UserName { get; set; }

    /// <summary>对象池回收时清空角色身份字段，避免复用时残留。</summary>
    public override void Shutdown()
    {
        RoleId = 0;
        UserName = null;
    }
}
