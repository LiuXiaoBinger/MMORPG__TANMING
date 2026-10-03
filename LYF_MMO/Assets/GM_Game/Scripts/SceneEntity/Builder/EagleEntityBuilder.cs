using MMoRpgCommon.SceneEntity;

/// <summary>
/// 鹰类召唤物 Builder。
/// 鹰是飞行类召唤物，使用公共组件但不挂载完整战斗组件集，
/// 仅挂载鹰专属标记和召唤物标记。
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
