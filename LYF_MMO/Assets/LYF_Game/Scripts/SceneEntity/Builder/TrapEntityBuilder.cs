using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用陷阱，添加公共组件以及战斗分组、战斗和陷阱标记。
/// 这里只登记类别，不实现触发检测或伤害结算。
/// </summary>
public sealed class TrapEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 构建陷阱实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">陷阱配置表 ID。</param>
    /// <returns>已装配完组件的陷阱实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取陷阱实体实例
        TrapEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Trap,
            configId, delegate(ulong id, int config)
            {
                return new TrapEntity(id, config);
            });
        // 基础公共组件
        AttachCommonComponents(entity);
        // 战斗组标记：陷阱参与战斗分组
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.FightGroup));
        // 战斗标记：陷阱可触发战斗效果
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Combat));
        // 陷阱专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Trap));
        return entity;
    }
}
