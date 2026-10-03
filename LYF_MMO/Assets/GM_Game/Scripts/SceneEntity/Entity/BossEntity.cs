using MMoRpgCommon.SceneEntity;

/// <summary>
/// BOSS 怪物实体，保留参考项目 MonsterRank.Boss 等级信息。
/// 继承自 MonsterEntity，共享怪物组件装配逻辑，仅标记为 BOSS 等级。
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
