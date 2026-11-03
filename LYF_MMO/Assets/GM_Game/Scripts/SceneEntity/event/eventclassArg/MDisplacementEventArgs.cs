/// <summary>
/// 一次技能位移的开始数据，直接传给移动组件持有的位移驱动。
/// 保存技能对象、施法编号和复用次数，以便异步回调确认自己仍属于这次施法。
/// </summary>
public sealed class MDisplacementEventArgs
{
    /// <summary>发起该段的原技能对象，当前动作槽和池复用校验均基于此引用。</summary>
    public MSkill Source;
    /// <summary>原非零施法 UUID，不能用技能主表 ID 替代。</summary>
    public ulong skillUuid;
    /// <summary>登记回调时的技能复用次数；同一对象或 UUID 再次使用时，仍能识别旧回调。</summary>
    public ulong fireGeneration;
    /// <summary>本段位移配置，包含时间、速度或距离曲线等；由当前技能运行数据持有。</summary>
    public MSkillChargeData Data;
    /// <summary>本段开始时确定的水平世界朝向，单位为度，段内使用同一方向。</summary>
    public float angle;
    /// <summary>本段开始在 MTimerMgr 全局时钟中的标称毫秒数，不能使用回调到达时间替代。</summary>
    public long startTimeMilliseconds;
    /// <summary>配置异步加载完成时，本段已经过去的秒数；-1 表示按正常定时回调计算。</summary>
    public float initialElapsedSeconds = -1f;
}
