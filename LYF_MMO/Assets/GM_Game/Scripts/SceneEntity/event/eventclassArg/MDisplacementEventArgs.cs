/// <summary>RO 同名位移开始参数；直接交移动组件持有的驱动，不进入通用弱类型事件总线。</summary>
public sealed class MDisplacementEventArgs
{
    /// <summary>发起该段的原技能对象，当前动作槽和池复用校验均基于此引用。</summary>
    public MSkill Source;
    /// <summary>原非零施法 UUID，不能用技能主表 ID 替代。</summary>
    public ulong skillUuid;
    /// <summary>注册时锁定的池实例施法代次，同 UUID 重借也必须区分。</summary>
    public ulong fireGeneration;
    /// <summary>本段完整强类型指令，由当前运行资产持有。</summary>
    public MSkillChargeData Data;
    /// <summary>冻结的水平世界朝向，单位度；与 RO angle 的实际计算单位一致。</summary>
    public float angle;
    /// <summary>本段开始在 MTimerMgr 全局时钟中的标称毫秒数，不能使用回调到达时间替代。</summary>
    public long startTimeMilliseconds;
    /// <summary>冷配置登记时已过的段内秒数；负一表示正常未来回调，可计算本帧跨界增量。</summary>
    public float initialElapsedSeconds = -1f;
}
