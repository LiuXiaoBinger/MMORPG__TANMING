using MMoRpgCommon.SceneEntity;

/// <summary>
/// 宠物的场景实体类型，沿用 MEntity 的模型和组件生命周期。
/// 本类只指定 Pet 类别，没有宠物 AI 或跟随逻辑。
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
