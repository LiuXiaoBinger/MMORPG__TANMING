using MMoRpgCommon.SceneEntity;

/// <summary>
/// Dummy 训练或占位实体。
/// 用于战斗训练假人、场景占位对象和测试替身。
/// EagleEntity 等飞行召唤物继承自本类。
/// </summary>
public class DummyEntity : MEntity
{
    /// <summary>
    /// 构造 Dummy 实体（默认 Dummy 类型）
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">配置表 ID</param>
    public DummyEntity(ulong uid, int configId)
        : this(uid, EntityType.Dummy, configId)
    {
    }

    /// <summary>
    /// 供鹰类等 Dummy 派生实体保留具体实体类别。
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="entityType">具体实体类型（如 Eagle）</param>
    /// <param name="configId">配置表 ID</param>
    protected DummyEntity(ulong uid, EntityType entityType, int configId)
        : base(uid, entityType, configId)
    {
    }
}
