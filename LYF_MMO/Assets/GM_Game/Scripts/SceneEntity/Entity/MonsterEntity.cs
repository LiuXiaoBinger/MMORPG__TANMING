using MMoRpgCommon.SceneEntity;

/// <summary>
/// 普通怪物实体，精英和 Boss 通过强类型包装共享此组件装配。
/// 包含怪物 ID 和等级（Rank）信息，是所有怪物类型的基类。
/// </summary>
public class MonsterEntity : MEntity
{
    /// <summary>
    /// 构造怪物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="entityType">怪物具体类型（普通/精英/Boss 等）</param>
    /// <param name="configId">怪物配置表 ID</param>
    public MonsterEntity(ulong uid, EntityType entityType, int configId)
        : base(uid, entityType, configId)
    {
        // 默认等级为普通怪物
        Rank = MonsterRank.Normal;
    }

    /// <summary>怪物配置表 ID</summary>
    public int MonsterId { get; set; }

    /// <summary>怪物等级档位（普通/精英/BOSS/MVP 等）</summary>
    public MonsterRank Rank { get; set; }

    /// <summary>
    /// 对象池复用时重置怪物 ID 和等级为默认值
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        MonsterId = 0;
        Rank = MonsterRank.Normal;
        base.PrepareForReuse(uid, entityType, configId);
    }
}
