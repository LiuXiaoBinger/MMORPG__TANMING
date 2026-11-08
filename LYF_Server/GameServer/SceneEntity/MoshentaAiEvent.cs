using System.Numerics;

/// <summary>当前实例发送给原AI树的一次具名事件，字段对应ROAIEventActionArgs。</summary>
internal sealed class MoshentaAiEvent
{
    /// <summary>原配置事件名，空字符串表示已消费。</summary>
    internal string Name = "";
    /// <summary>原事件的整型业务参数，例如副本目标ID。</summary>
    internal int IntArg;
    /// <summary>原事件的浮点业务参数。</summary>
    internal float FloatArg;
    /// <summary>本实例事件发送实体的UID。</summary>
    internal ulong SenderId;
    /// <summary>事件发生时的世界坐标。</summary>
    internal Vector3 Position;
    /// <summary>事件发送者绕Y轴角度。</summary>
    internal float Face;
}
