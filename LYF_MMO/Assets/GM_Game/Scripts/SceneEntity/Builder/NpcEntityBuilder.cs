using MMoRpgCommon.SceneEntity;

/// <summary>
/// NPC Builder，对应参考项目 NpcBuilder 的场景、交互和 AI 组件。
/// NPC 不需要战斗组件，但需要 AI 和导航来实现巡路和交互表现。
/// </summary>
public sealed class NpcEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 构建 NPC 实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">NPC 配置表 ID。</param>
    /// <returns>已装配完组件的 NPC 实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取 NPC 实体实例
        NpcEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Npc,
            configId, delegate(ulong id, int config)
            {
                return new NpcEntity(id, config);
            });
        // 基础公共组件
        AttachCommonComponents(entity);
        // NPC 专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Npc));
        // AI 标记：NPC 有自动行为逻辑
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.AI));
        // 导航标记：NPC 需要寻路巡路
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Navigation));
        return entity;
    }
}
