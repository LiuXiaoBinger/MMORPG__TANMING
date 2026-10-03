using MMoRpgCommon.SceneEntity;

/// <summary>
/// 精英怪物实体，根类型仍属于 Monster 类别。
/// 继承自 MonsterEntity，共享怪物组件装配，标记为精英等级。
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
