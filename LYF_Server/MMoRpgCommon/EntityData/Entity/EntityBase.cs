/// <summary>
/// 游戏世界实体基类。
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// 实体唯一 ID。
    /// </summary>
    public int EntityID { get; set; }

    /// <summary>
    /// 实体名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 所在地图 ID。
    /// </summary>
    public int MapID { get; set; }

    /// <summary>
    /// 世界坐标。格式由游戏逻辑统一定义。
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// 最大生命值。
    /// </summary>
    public int MaxHP { get; set; }

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public int CurrHP { get; set; }

    /// <summary>
    /// 最大法力值。
    /// </summary>
    public int MaxMP { get; set; }

    /// <summary>
    /// 当前法力值。
    /// </summary>
    public int CurrMP { get; set; }
}
