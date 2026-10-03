using MMoRpgCommon.SceneEntity;

/// <summary>
/// 鹰类召唤物实体。
/// 继承自 DummyEntity，用于战斗中召唤的鹰型单位（如侦察、俯冲攻击）。
/// </summary>
public sealed class EagleEntity : DummyEntity
{
    /// <summary>
    /// 构造鹰类召唤物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">鹰类召唤物配置表 ID</param>
    public EagleEntity(ulong uid, int configId)
        : base(uid, EntityType.Eagle, configId)
    {
    }
}
