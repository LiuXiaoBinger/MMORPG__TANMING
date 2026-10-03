using MMoRpgCommon.SceneEntity;

/// <summary>
/// 宠物场景实体，由玩家召唤的宠物单位。
/// 继承自 MEntity，当前为轻量占位实体，后续可扩展宠物 AI 和跟随逻辑。
/// </summary>
public sealed class PetEntity : MEntity
{
    /// <summary>
    /// 构造宠物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">宠物配置表 ID</param>
    public PetEntity(ulong uid, int configId)
        : base(uid, EntityType.Pet, configId)
    {
    }
}
