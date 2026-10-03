using MMoRpgCommon.SceneEntity;

/// <summary>
/// Dummy Builder，使用怪物公共组件但保留 Dummy 实体类别。
/// Dummy 通常用于场景摆设、训练假人等非战斗 NPC，不挂载完整战斗组件。
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
