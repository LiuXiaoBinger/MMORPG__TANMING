/// <summary>
/// 请求停止某一次施法产生的位移。
/// 驱动核对技能对象、施法编号和复用次数后取消，不把实体直接移到原计划终点。
/// </summary>
public sealed class MDisplacementStopEventArgs
{
    /// <summary>开始这段位移的技能对象，避免旧停止请求影响后来启动的位移。</summary>
    public MSkill Source;
    /// <summary>停止请求所属的非零施法 UUID。</summary>
    public ulong skillUuid;
    /// <summary>位移开始时保存的复用次数；即使技能已 Reset，也用原值查找要停止的位移。</summary>
    public ulong fireGeneration;
}
