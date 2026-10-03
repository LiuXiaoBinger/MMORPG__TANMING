using MMoRpgCommon.SceneEntity;

/// <summary>
/// 佣兵 Builder，对应 MercenaryBuilder 的怪物公共装配和召唤组件。
/// 佣兵使用完整的怪物组件集合并额外添加佣兵和召唤物标记。
/// </summary>
public sealed class MercenaryEntityBuilder : MonsterEntityBuilder
{
    /// <summary>
    /// 构建佣兵实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">佣兵配置表 ID。</param>
    /// <returns>已装配完组件的佣兵实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取佣兵实体实例
        MercenaryEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Mercenary,
            configId, delegate(ulong id, int config)
            {
                return new MercenaryEntity(id, config);
            });
        // 挂载怪物完整组件集合（含仇恨、AI、导航等）
        AttachMonsterComponents(entity);
        // 佣兵专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Mercenary));
        // 召唤物标记：佣兵由玩家召唤产生
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Summon));
        return entity;
    }
}
