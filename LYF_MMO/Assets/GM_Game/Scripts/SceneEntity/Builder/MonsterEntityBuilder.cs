using MMoRpgCommon.SceneEntity;

/// <summary>
/// 普通怪物 Builder，对应 MonsterBuilder 和 MonsterCommonAssemble。
/// 怪物实体需要公共组件、战斗组件、怪物 AI 组件以及仇恨、生命周期、导航等标记。
/// </summary>
public class MonsterEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 构建普通怪物实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">怪物配置表 ID。</param>
    /// <returns>已装配完组件的怪物实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取怪物实体实例
        MonsterEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Monster,
            configId, delegate(ulong id, int config)
            {
                return new MonsterEntity(id, EntityType.Monster, config);
            });
        // 挂载怪物专属组件集合
        AttachMonsterComponents(entity);
        return entity;
    }

    /// <summary>
    /// 装配怪物公共组件，供 Boss、精英、宠物、佣兵、Dummy 等子类复用。
    /// 包含公共组件、战斗组件、怪物业务组件以及仇恨/生命周期/AI/导航标记。
    /// </summary>
    /// <param name="entity">待装配的怪物实体实例。</param>
    protected void AttachMonsterComponents(ROObject entity)
    {
        // 基础公共组件
        AttachCommonComponents(entity);
        // 战斗相关组件标记
        AttachCombatComponents(entity);
        // 怪物业务数据组件
        MComponentMgr.Instance.CreateComponent<EntityMonsterComponent>(entity);
        // 仇恨标记：参与仇恨列表管理
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Hatred));
        // 生命周期标记：怪物有出生和销毁时机
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Lifetime));
        // AI 标记：怪物由 AI 驱动行为
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.AI));
        // 导航标记：怪物需要寻路
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Navigation));
    }
}
