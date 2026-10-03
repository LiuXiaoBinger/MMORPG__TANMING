using MMoRpgCommon.SceneEntity;

/// <summary>
/// 陷阱场景实体，对应参考项目 OBJECT_TYPE_TRAP。
/// 放置在场景中的陷阱装置，触发后产生伤害或效果。
/// </summary>
public sealed class TrapEntity : MEntity
{
    /// <summary>
    /// 构造陷阱实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">陷阱配置表 ID</param>
    public TrapEntity(ulong uid, int configId)
        : base(uid, EntityType.Trap, configId)
    {
    }
}
