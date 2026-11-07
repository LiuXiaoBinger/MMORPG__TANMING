/// <summary>实例中的单个叶子效果；组合效果分别保留自己的周期累计时间。</summary>
public class GeneEffect
{
    /// <summary>来自 Luban 的只读叶子定义；父实例控制其寿命。</summary>
    public cfg.Gene Definition { get; private set; }
    /// <summary>本次应用采用的每层数值，可由装备或技能调用方覆盖表内默认值。</summary>
    public float Value { get; internal set; }
    /// <summary>距上一次周期效果发生经过的毫秒数；刷新寿命时不重置。</summary>
    internal long ElapsedMilliseconds;

    /// <summary>创建一个与父基因共同存亡的效果。</summary>
    /// <param name="definition">具体属性、周期或状态定义，不接受组合类型。</param>
    /// <param name="value">每层效果数值，百分比时 10 表示 10%。</param>
    public GeneEffect(cfg.Gene definition, float value)
    {
        Definition = definition;
        Value = value;
    }
}
