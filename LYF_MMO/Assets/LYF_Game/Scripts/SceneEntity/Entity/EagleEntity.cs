using MMoRpgCommon.SceneEntity;

/// <summary>
/// 鹰类场景实体，沿用 DummyEntity 的生命周期，并把实体类别设为 Eagle。
/// 本类没有实现侦察、飞行或攻击。
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
