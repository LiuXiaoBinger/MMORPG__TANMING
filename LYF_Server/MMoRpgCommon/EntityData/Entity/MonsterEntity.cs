/// <summary>
/// 怪物实体。
/// </summary>
public class MonsterEntity : EntityBase
{
    /// <summary>怪物实例 ID。</summary>
    public int MonsterID { get; set; }

    /// <summary>怪物配置表 ID。</summary>
    public int MonsterTypeID { get; set; }

    /// <summary>怪物等级。</summary>
    public int Level { get; set; }
}
