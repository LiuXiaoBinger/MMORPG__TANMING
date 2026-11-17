using MMoRpgCommon.SceneEntity;

/// <summary>
/// 陷阱的场景实体类型，沿用 MEntity 的模型与组件生命周期。
/// 本类只指定 Trap 类别，触发检测与战斗效果不在这里执行。
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
