using MMoRpgCommon.SceneEntity;

/// <summary>
/// Boss 怪物 Builder，保持 MonsterCommonAssemble 组件集合。
/// 在普通怪物组件基础上额外设置怪物等级为 Boss，并挂载 Boss 专属标记。
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
        // Boss 专属标记，用于 UI 表现和特殊逻辑
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Boss));
        return entity;
    }
}
