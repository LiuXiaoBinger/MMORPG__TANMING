using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用 Boss 实体，装配怪物共用组件，再设置 Boss 档位和类别标记。
/// </summary>
public sealed class BossEntityBuilder : MonsterEntityBuilder
{
    /// <summary>
    /// 构建 Boss 怪物实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">Boss 配置表 ID。</param>
    /// <returns>已装配完组件的 Boss 实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取 Boss 实体实例
        BossEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Boss,
            configId, delegate(ulong id, int config)
            {
                return new BossEntity(id, config);
            });
        // 设置怪物等级为 Boss
        entity.Rank = MonsterRank.Boss;
        // 挂载怪物公共组件集合
        AttachMonsterComponents(entity);
        // 登记 Boss 类别，供按组件类型查询的代码识别
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Boss));
        return entity;
    }
}
