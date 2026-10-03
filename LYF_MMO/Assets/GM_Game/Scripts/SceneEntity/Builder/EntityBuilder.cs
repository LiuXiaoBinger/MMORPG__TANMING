using MMoRpgCommon.SceneEntity;

/// <summary>
/// 实体 Builder 基类，对应参考项目 ObjectBuilder 的装配职责。
/// 子类负责通过对象池获取实体实例，并按实体类型挂载对应的组件标记和业务组件。
/// </summary>
public abstract class EntityBuilder
{
    /// <summary>
    /// 构建实体实例的抽象方法，子类必须实现。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识（ulong）。</param>
    /// <param name="configId">实体配置表 ID（如怪物表 ID、NPC 表 ID）。</param>
    /// <returns>已装配完组件的场景实体实例。</returns>
    public abstract ROObject Build(ulong uid, int configId);

    /// <summary>
    /// 所有场景实体共享的最小组件，不默认附带战斗职责。
    /// 包括实体标记、变换组件和属性组件。
    /// </summary>
    /// <param name="entity">待装配的实体实例。</param>
    protected void AttachCommonComponents(ROObject entity)
    {
        // 实体基础标记，标识该实体属于普通实体类型
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Entity));
        // 变换组件：负责实体位置、旋转等空间变换
        MComponentMgr.Instance.CreateComponent<EntityTransformComponent>(entity);
        // 属性组件：负责实体基础属性数据
        MComponentMgr.Instance.CreateComponent<EntityAttributesComponent>(entity);
        // 同步标记：标识该实体需要参与网络同步
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Sync));
    }

    /// <summary>
    /// 只有可参与战斗并可受击死亡的实体才追加战斗组件。
    /// 包括仇恨组、受击、死亡和战斗标记。
    /// </summary>
    /// <param name="entity">待装配的实体实例。</param>
    protected void AttachCombatComponents(ROObject entity)
    {
        // 战斗组标记：用于仇恨分组和队伍归属
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.FightGroup));
        // 受击标记：该实体可被攻击
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.BeHit));
        // 死亡标记：该实体可进入死亡状态
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Dead));
        // 战斗标记：该实体参与战斗逻辑
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Combat));
    }

}
