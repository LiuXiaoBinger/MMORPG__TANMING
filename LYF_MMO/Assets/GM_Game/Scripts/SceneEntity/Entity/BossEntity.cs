using MMoRpgCommon.SceneEntity;

/// <summary>
/// Boss 怪物的实体类型，创建时固定 EntityType.Boss 和 Boss 档位。
/// 其他怪物数据与生命周期沿用 MonsterEntity。
/// </summary>
public sealed class BossEntity : MonsterEntity
{
    /// <summary>
    /// 构造 BOSS 怪物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">BOSS 配置表 ID</param>
    public BossEntity(ulong uid, int configId)
        : base(uid, EntityType.Boss, configId)
    {
        // 固定标记为 BOSS 等级
        Rank = MonsterRank.Boss;
    }
}
