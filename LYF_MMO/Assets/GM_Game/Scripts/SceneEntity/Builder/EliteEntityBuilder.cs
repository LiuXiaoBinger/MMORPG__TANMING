using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用精英怪物，装配怪物共用组件，再设置精英档位和类别标记。
/// </summary>
public sealed class EliteEntityBuilder : MonsterEntityBuilder
{
    /// <summary>
    /// 构建精英怪物实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">精英怪配置表 ID。</param>
    /// <returns>已装配完组件的精英实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取精英实体实例
        EliteEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Elite,
            configId, delegate(ulong id, int config)
            {
                return new EliteEntity(id, config);
            });
        // 设置怪物等级为精英
        entity.Rank = MonsterRank.Elite;
        // 挂载怪物公共组件集合
        AttachMonsterComponents(entity);
        // 精英专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Elite));
        return entity;
    }
}
