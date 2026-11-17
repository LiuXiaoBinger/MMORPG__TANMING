using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用宠物，添加公共组件、战斗标记、宠物和召唤物标记。
/// 没有调用怪物装配方法，也没有在此添加宠物移动或 AI 逻辑。
/// </summary>
public sealed class PetEntityBuilder : MonsterEntityBuilder
{
    /// <summary>
    /// 构建宠物实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">宠物配置表 ID。</param>
    /// <returns>已装配完组件的宠物实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取宠物实体实例
        PetEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Pet,
            configId, delegate(ulong id, int config)
            {
                return new PetEntity(id, config);
            });
        // 基础公共组件
        AttachCommonComponents(entity);
        // 战斗组件标记
        AttachCombatComponents(entity);
        // 宠物专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Pet));
        // 召唤物标记：宠物由玩家召唤产生
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Summon));
        return entity;
    }
}
