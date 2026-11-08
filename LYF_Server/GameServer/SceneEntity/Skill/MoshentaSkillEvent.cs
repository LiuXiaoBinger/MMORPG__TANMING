using System.Numerics;
using System.Xml.Linq;

/// <summary>原动作中的一次结果、场景事件、召唤或延迟伤害。</summary>
internal sealed class MoshentaSkillEvent
{
    /// <summary>结果事件。</summary>
    internal const int Result = 0;
    /// <summary>原AIEvent。</summary>
    internal const int Scene = 1;
    /// <summary>原MobUnit。</summary>
    internal const int Mob = 2;
    /// <summary>原DelayDamageTime到期。</summary>
    internal const int Delayed = 3;
    /// <summary>原Charge位移完成。</summary>
    internal const int Charge = 4;
    /// <summary>原Time截止的技能结束通知。</summary>
    internal const int Finish = 5;
    /// <summary>原事件种类。</summary>
    internal int Kind;
    /// <summary>绝对逻辑毫秒。</summary>
    internal long At;
    /// <summary>原动作上下文。</summary>
    internal MoshentaSkillCast Cast;
    /// <summary>原XML配置节点。</summary>
    internal XElement Data;
    /// <summary>零基ResultList序号。</summary>
    internal int Index;
    /// <summary>延迟结果冻结的中心。</summary>
    internal Vector3 Point;
    /// <summary>延迟结果实际选中目标。</summary>
    internal ulong TargetId;
}
