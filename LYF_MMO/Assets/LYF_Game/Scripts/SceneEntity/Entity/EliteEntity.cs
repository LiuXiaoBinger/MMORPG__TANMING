using MMoRpgCommon.SceneEntity;

/// <summary>
/// 精英怪物实体，创建时固定 EntityType.Elite 和精英档位。
/// 其他怪物数据与生命周期沿用 MonsterEntity。
/// </summary>
public sealed class EliteEntity : MonsterEntity
{
    /// <summary>
    /// 构造精英怪物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">精英怪物配置表 ID</param>
    public EliteEntity(ulong uid, int configId)
        : base(uid, EntityType.Elite, configId)
    {
        // 固定标记为精英等级
        Rank = MonsterRank.Elite;
    }
}
