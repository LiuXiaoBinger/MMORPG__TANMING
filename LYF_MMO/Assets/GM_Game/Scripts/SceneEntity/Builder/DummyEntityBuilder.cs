using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用 Dummy 实体，只添加公共组件和 Dummy 标记。
/// 虽然继承怪物构建器，这里没有调用怪物整套组件的装配方法。
/// </summary>
public sealed class DummyEntityBuilder : MonsterEntityBuilder
{
    /// <summary>
    /// 构建 Dummy 实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">Dummy 配置表 ID。</param>
    /// <returns>已装配完组件的 Dummy 实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取 Dummy 实体实例
        DummyEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Dummy,
            configId, delegate(ulong id, int config)
            {
                return new DummyEntity(id, config);
            });
        // 基础公共组件
        AttachCommonComponents(entity);
        // Dummy 专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Dummy));
        return entity;
    }
}
