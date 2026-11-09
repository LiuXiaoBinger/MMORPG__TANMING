using System;
using System.Collections.Generic;
using System.IO;

/// <summary>原AITimeAxisProcess毫秒调度，Lua/Code仅替换事件函数执行器。</summary>
internal abstract class MoshentaTimeline : IDisposable
{
    /// <summary>绑定的真实机制实体。</summary>
    protected readonly MoshentaActor Actor;
    /// <summary>按原轴号保存完整事件序列，事件编号从1开始。</summary>
    protected readonly Dictionary<int, List<MoshentaTimePoint>> Plans = new Dictionary<int, List<MoshentaTimePoint>>();
    /// <summary>当前原轴号，初始为1。</summary>
    internal int Axis { get; private set; } = 1;
    /// <summary>原axis_start_time，0代表未开始。</summary>
    private long start;
    /// <summary>上次有效采样的实例毫秒。</summary>
    private long last;
    /// <summary>初始化共享调度器。</summary><param name="actor">真实宿主。</param>
    protected MoshentaTimeline(MoshentaActor actor) { Actor = actor; }
    /// <summary>读取正式可编辑配置选择两种执行器，默认Lua。</summary><param name="actor">真实宿主。</param>
    internal static MoshentaTimeline Create(MoshentaActor actor)
    {
        // 未登记时间轴的实体只运行自己的行为树，不推断魔神塔剧情身份。
        DungeonTimelineBinding binding = actor.Owner.Battle.Timeline(actor.EntityId);
        if (binding == null) return null;
        // 用户修改所选配置的TimelineMode后，新建实例按同一映射切换执行器。
        string mode = actor.Owner.Battle.TimelineMode;
        if (mode == "Code") return new MoshentaCodeTimeline(actor, binding);
        if (mode == "Lua") return new MoshentaLuaTimeline(actor, binding);
        throw new InvalidDataException("TimelineMode必须为Lua或Code：" + mode);
    }
    /// <summary>原SwitchAxis同轴成功但不重启；真实换轴重置时钟并请求AI再运行。</summary><param name="axis">原轴号。</param>
    internal bool Switch(int axis)
    {
        if (axis == Axis) return true;
        if (!Plans.ContainsKey(axis)) return false;
        Axis = axis; start = 0; last = 0; Actor.RequestThink(); return true;
    }
    /// <summary>原时间轴tick；使用原事件窗口判断而非擅自补执行全部逾期事件。</summary><param name="now">实例逻辑毫秒。</param>
    internal void Update(long now)
    {
        if (Actor.IsDead) return;
        if (start == 0) { start = now; last = now; return; }
        // 本次采样轴先保存，StepInterface切轴不改变已取出的事件队列身份。
        int axis = Axis;
        List<MoshentaTimePoint> plan = Plans[axis];
        int current = EventIndex(plan, now - start);
        int previous = EventIndex(plan, last - start);
        // 原UpdateTime仅在跨事件窗口或单窗口等待超过当前事件长度时触发。
        bool run = current != previous;
        if (!run && current >= 1 && current <= plan.Count) run = now - last > plan[current - 1].Delay;
        if (!run) return;
        last = now;
        if (previous < 1 || previous > plan.Count) return;
        // GetEvent先取当前事件，随后合并零等待事件，最多走一圈。
        List<int> events = new List<int> { previous };
        int cursor = previous;
        if (plan[cursor - 1].Action != "SwitchAxisBySelf")
        {
            for (int index = 0; index < plan.Count; index++)
            {
                // 原事件号按计划长度循环，不额外制造重试。
                int next = cursor % plan.Count + 1;
                if (plan[next - 1].Delay != 0) break;
                events.Add(next); cursor = next;
                if (plan[cursor - 1].Action == "SwitchAxisBySelf") break;
            }
        }
        for (int index = 0; index < events.Count; index++) Invoke(axis, events[index]);
    }
    /// <summary>原DiffTimeToEventId/GetAxisLoopEventId，包括边界等于累计等待时推进下一窗口。</summary><param name="plan">当前轴事件。</param><param name="elapsed">轴内经过毫秒。</param>
    private static int EventIndex(List<MoshentaTimePoint> plan, long elapsed)
    {
        if (elapsed == 0) return 1;
        // 原每轮时长为各前置等待之和。
        long total = 0;
        for (int index = 0; index < plan.Count; index++) total += plan[index].Delay;
        if (total > 0 && elapsed > total) elapsed %= total;
        // 原先减时间再增加event_id，保留其边界行为。
        int result = 0;
        for (int index = 0; index < plan.Count; index++) { if (elapsed < 0) break; elapsed -= plan[index].Delay; result++; }
        return result;
    }
    /// <summary>执行原轴/事件对应函数。</summary><param name="axis">原轴号。</param><param name="number">从1开始事件号。</param>
    protected abstract bool Invoke(int axis, int number);
    /// <summary>资源生命周期要求使用.NET IDisposable；代码路径无额外资源。</summary>
    public virtual void Dispose() { }
}
