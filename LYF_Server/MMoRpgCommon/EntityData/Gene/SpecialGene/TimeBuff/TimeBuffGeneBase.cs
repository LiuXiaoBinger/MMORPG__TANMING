/// <summary>
/// 记录带持续效果的装备词条；持续时间以秒保存，不在此执行计时。
/// </summary>
public abstract class TimeBuffGeneBase : GeneBase
{
    /// <summary>
    /// Buff 持续时间，单位为秒。
    /// </summary>
    public float DurationSeconds { get; set; }
}
