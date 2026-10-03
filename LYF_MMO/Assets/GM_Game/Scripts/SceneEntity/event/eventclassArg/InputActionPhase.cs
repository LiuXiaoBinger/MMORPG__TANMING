/// <summary>输入动作的生命周期阶段，作为动作参数数据使用。</summary>
public enum InputActionPhase
{
    /// <summary>动作开始触发。</summary>
    Started,
    /// <summary>动作值变化或执行。</summary>
    Performed,
    /// <summary>动作结束或取消。</summary>
    Canceled
}
