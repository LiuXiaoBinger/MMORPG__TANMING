using MMoRpgCommon.SceneEntity;

/// <summary>
/// 采集物实体，对应参考项目 CollectionBuilder。
/// 场景中可被玩家采集的资源点（矿石、草药、木材等）。
/// </summary>
public sealed class CollectionEntity : MEntity
{
    /// <summary>
    /// 构造采集物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">采集物配置表 ID</param>
    public CollectionEntity(ulong uid, int configId)
        : base(uid, EntityType.Collection, configId)
    {
    }

    /// <summary>采集物配置 ID</summary>
    public int CollectionId { get; set; }

    /// <summary>
    /// 对象池复用时重置采集物 ID
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        CollectionId = 0;
        base.PrepareForReuse(uid, entityType, configId);
    }
}
