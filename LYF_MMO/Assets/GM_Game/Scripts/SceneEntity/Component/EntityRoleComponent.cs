using MMoRpgCommon.SceneEntity;

/// <summary>
/// 在实体组件中保存角色编号和显示名，回收时清空，避免复用旧角色信息。
/// </summary>
public sealed class EntityRoleComponent : EntityComponentBase
{

    #region 构造、初始化与事件注册
    /// <summary>构造角色组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityRoleComponent(ROObject owner) : base(owner)
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

    /// <summary>对象池回收时清空角色身份字段，避免复用时残留。</summary>
    public override void Shutdown()
    {
        RoleId = 0;
        UserName = null;
    }
    #endregion

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Role"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Role; }
    }

    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }

    /// <summary>角色持久化 RoleId，区分不同玩家/角色实例。</summary>
    public int RoleId { get; set; }

    /// <summary>角色显示名（玩家昵称），用于头顶名等表现。</summary>
    public string UserName { get; set; }
}
