using MMoRpgCommon.SceneEntity;

/// <summary>
/// 采集物 Builder，对应 CollectionBuilder 的采集与场景组件。
/// 采集物是场景中的可交互静态对象，不需要战斗和 AI 组件。
/// </summary>
public sealed class CollectionEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 构建采集物实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">采集物配置表 ID。</param>
    /// <returns>已装配完组件的采集物实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取采集物实体实例
        CollectionEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Collection,
            configId, delegate(ulong id, int config)
            {
                return new CollectionEntity(id, config);
            });
        // 基础公共组件
        AttachCommonComponents(entity);
        // 采集物专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Collection));
        return entity;
    }
}
