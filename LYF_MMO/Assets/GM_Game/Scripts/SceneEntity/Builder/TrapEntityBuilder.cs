using MMoRpgCommon.SceneEntity;

/// <summary>
/// 陷阱 Builder，对应参考项目陷阱对象的场景、同步和战斗组件。
/// 陷阱是场景中的触发式战斗对象，挂载战斗组和战斗标记但不包含受击和死亡组件。
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
