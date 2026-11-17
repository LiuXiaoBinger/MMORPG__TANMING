using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用佣兵，装配怪物共用组件，再添加佣兵和召唤物标记。
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
        // 装配怪物数据及仇恨、AI、导航等类别标记
        AttachMonsterComponents(entity);
        // 佣兵专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Mercenary));
        // 登记召唤物类别；召唤者信息不在此设置
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Summon));
        return entity;
    }
}
