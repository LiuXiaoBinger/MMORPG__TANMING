using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用 NPC，添加公共组件以及 NPC、AI、导航标记。
/// 这里的 AI 和导航标记本身不执行巡逻或寻路。
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
        // 登记 AI 类别；此标记不执行 NPC 行为
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.AI));
        // 登记导航类别；此标记不执行寻路
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Navigation));
        return entity;
    }
}
