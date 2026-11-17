using MMoRpgCommon.SceneEntity;

/// <summary>
/// 场景实体的装配入口：子类从对象池取实体，再为它添加所需组件。
/// 公共方法区分实际的数据组件和只声明类别的标记组件。
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
        // 基础实体标记；它不改变实体自身的 EntityType
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Entity));
        // 变换组件：负责实体位置、旋转等空间变换
        MComponentMgr.Instance.CreateComponent<EntityTransformComponent>(entity);
        // 属性组件：负责实体基础属性数据
        MComponentMgr.Instance.CreateComponent<EntityAttributesComponent>(entity);
        // 登记同步类别；标记组件本身不发送网络消息
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Sync));
    }

    /// <summary>
    /// 为可战斗实体登记战斗分组、受击、死亡和战斗四类标记。
    /// 这些标记不执行战斗逻辑，具体行为仍由动作组件等代码处理。
    /// </summary>
    /// <param name="entity">待装配的实体实例。</param>
    protected void AttachCombatComponents(ROObject entity)
    {
        // 登记战斗分组类别
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.FightGroup));
        // 登记受击类别
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.BeHit));
        // 登记死亡类别
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Dead));
        // 登记战斗类别
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Combat));
    }

}
