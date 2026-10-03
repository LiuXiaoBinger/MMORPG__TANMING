using MMoRpgCommon.SceneEntity;

/// <summary>
/// 佣兵实体，对应参考项目 MercenaryBuilder。
/// 继承自 MonsterEntity，作为玩家召唤的佣兵单位使用。
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
