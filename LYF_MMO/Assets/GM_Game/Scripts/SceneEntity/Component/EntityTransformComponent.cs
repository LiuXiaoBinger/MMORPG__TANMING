using MMoRpgCommon.SceneEntity;

/// <summary>
/// 接收一组位置和朝向数据，写入宿主实体的逻辑坐标，再发送位置变更事件。
/// 这里不直接修改 UnityEngine.Transform，也不执行平滑移动。
/// </summary>
public sealed class EntityTransformComponent : EntityComponentBase
{

    #region 构造与事件注册与注销
    /// <summary>构造位置组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityTransformComponent(ROObject owner) : base(owner)
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
    #endregion

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Scene"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Scene; }
    }

    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }

    /// <summary>把位置与朝向一次性写入实体的逻辑 Transform，再派发 TransformChanged 事件。</summary>
    /// <param name="snapshot">位置与朝向快照，为空直接忽略。</param>
    public void Apply(EntityTransformSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }
        // 直接覆盖实体坐标与朝向，不走移动插值
        Entity.Transform.X = snapshot.X;
        Entity.Transform.Y = snapshot.Y;
        Entity.Transform.Z = snapshot.Z;
        Entity.Transform.Yaw = snapshot.Yaw;
        DispatchEvent(MEventType.TransformChanged, null);
    }
}
