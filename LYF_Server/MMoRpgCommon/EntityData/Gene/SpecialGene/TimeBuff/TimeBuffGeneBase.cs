using System.Collections.Generic;

/// <summary>周期资源节点的公共计时行为；普通组合子节点共享根寿命，各自保留周期进度。</summary>
public abstract class TimeBuffGeneBase : GeneBase
{
    /// <summary>相邻两次结算的间隔，单位毫秒，工厂保证大于零。</summary>
    public int IntervalMilliseconds { get; set; }
    /// <summary>距上次周期结算累计的毫秒数；重复施加不重置。</summary>
    public long ElapsedMilliseconds { get; private set; }

    /// <summary>独立施加的周期副本从零开始计时，不继承原宿主的周期进度。</summary>
    /// <returns>效果参数相同但周期进度独立的具体节点。</returns>
    public override GeneBase Clone()
    {
        // 基类保留具体伤害、治疗或回蓝类型，当前副本只清理自己的计时值。
        TimeBuffGeneBase copy = (TimeBuffGeneBase)base.Clone();
        copy.ElapsedMilliseconds = 0;
        return copy;
    }

    /// <summary>补齐本帧跨过的周期，只处理运行容器裁切后的有效寿命。</summary>
    /// <param name="milliseconds">本帧实际生效的毫秒数。</param>
    /// <param name="instance">提供层数、来源与施加角色的根实例。</param>
    /// <param name="pulses">宿主稍后结算的周期结果。</param>
    public override void AdvanceTime(long milliseconds, GeneBase instance, List<GenePulse> pulses)
    {
        ElapsedMilliseconds += milliseconds;
        while (ElapsedMilliseconds >= IntervalMilliseconds)
        {
            ElapsedMilliseconds -= IntervalMilliseconds;
            pulses.Add(new GenePulse(instance, this));
        }
    }
}
