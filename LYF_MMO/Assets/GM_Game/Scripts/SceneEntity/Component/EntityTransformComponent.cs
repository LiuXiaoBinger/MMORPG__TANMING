using MMoRpgCommon.SceneEntity;

/// <summary>
/// 实体场景位置组件，对应参考项目 ROSceneComponent。
/// 负责把外部传入的变换快照直接应用到宿主实体 Transform，并派发变换变更事件。
/// </summary>
public sealed class EntityTransformComponent : EntityComponentBase
{
    /// <summary>构造位置组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityTransformComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Scene"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Scene; }
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

    /// <summary>把变换快照一次性写入宿主实体 Transform，并派发 TransformChanged 事件。</summary>
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
