using MMoRpgCommon.SceneEntity;

/// <summary>
/// 场景采集物的数据对象，记录采集物编号并沿用实体的模型与组件生命周期。
/// 具体采集交互不在本类实现。
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
