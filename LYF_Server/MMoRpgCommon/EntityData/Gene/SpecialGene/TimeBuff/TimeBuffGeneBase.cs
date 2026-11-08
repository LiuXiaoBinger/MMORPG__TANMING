using System.Collections.Generic;

/// <summary>周期资源节点的公共计时行为；普通组合子节点共享根寿命，各自保留周期进度。</summary>
public abstract class TimeBuffGeneBase : GeneBase
{
    /// <summary>周期效果等待计时到点，不在初次挂载时提前执行。</summary>
    public override bool CallsOnAttach { get { return false; } }
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

    /// <summary>在有效寿命内补齐周期，每次到点都执行具体子类自己的 OnCall。</summary>
    /// <param name="milliseconds">本帧有效生效毫秒数。</param>
    /// <param name="role">当前节点所属的真实角色。</param>
    public override void AdvanceTime(long milliseconds, IGeneOwner role)
    {
        ElapsedMilliseconds += milliseconds;
        while (ElapsedMilliseconds >= IntervalMilliseconds)
        {
            ElapsedMilliseconds -= IntervalMilliseconds;
            OnCall(role);
        }
    }
}
