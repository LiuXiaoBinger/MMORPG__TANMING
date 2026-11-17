using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用鹰实体，添加公共组件、鹰类别和召唤物标记。
/// 这里不实现飞行或攻击行为。
/// </summary>
public sealed class EagleEntityBuilder : MonsterEntityBuilder
{
    /// <summary>
    /// 构建鹰类召唤物实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">鹰配置表 ID。</param>
    /// <returns>已装配完组件的鹰实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取鹰实体实例
        EagleEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Eagle,
            configId, delegate(ulong id, int config)
            {
                return new EagleEntity(id, config);
            });
        // 基础公共组件
        AttachCommonComponents(entity);
        // 鹰专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Eagle));
        // 召唤物标记：鹰由玩家召唤产生
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Summon));
        return entity;
    }
}
