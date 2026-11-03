using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用采集物，添加位置、属性和采集物类别标记。
/// 这里只完成实体装配，采集交互由其他业务入口处理。
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
