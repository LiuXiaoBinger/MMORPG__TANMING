/// <summary>
/// 持续时间类特殊基因的基类。
/// </summary>
public abstract class TimeBuffGeneBase : GeneBase
{
    /// <summary>
    /// Buff 持续时间，单位为秒。
    /// </summary>
    public float DurationSeconds { get; set; }
}
