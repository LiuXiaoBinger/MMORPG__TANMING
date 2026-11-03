using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用怪物，并装配公共数据、怪物数据和战斗相关标记。
/// 精英、Boss 和佣兵复用同一个装配方法；AI、仇恨等在这里仍是标记组件。
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
    /// 装配怪物公共组件，当前由普通怪物、Boss、精英和佣兵构建器调用。
    /// 包含公共组件、战斗组件、怪物数据组件以及仇恨、生命周期、AI、导航标记。
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
        // 登记仇恨类别；此标记不维护仇恨列表
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Hatred));
        // 登记生命周期类别；实体销毁由实体世界处理
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Lifetime));
        // 登记 AI 类别；此标记不执行 AI
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.AI));
        // 登记导航类别；此标记不执行寻路
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Navigation));
    }
}
