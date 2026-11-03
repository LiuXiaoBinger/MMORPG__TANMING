using MMoRpgCommon.SceneEntity;

/// <summary>
/// 佣兵实体，沿用怪物数据与生命周期，并把实体类别设为 Mercenary。
/// </summary>
public sealed class MercenaryEntity : MonsterEntity
{
    /// <summary>
    /// 构造佣兵实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">佣兵配置表 ID</param>
    public MercenaryEntity(ulong uid, int configId)
        : base(uid, EntityType.Mercenary, configId)
    {
    }
}
