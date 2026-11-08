/// <summary>原Lua element_plan的一条前置等待及行为，毫秒时间保持原值。</summary>
internal sealed class MoshentaTimePoint
{
    /// <summary>进入该事件前等待的毫秒。</summary>
    internal int Delay;
    /// <summary>原Perform_BasicAction或Self函数名。</summary>
    internal string Action;
    /// <summary>配置边界的原字符串/整数参数文本，按原顺序。</summary>
    internal string[] Parameters;
}
