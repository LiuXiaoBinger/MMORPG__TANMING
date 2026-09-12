/// <summary>
/// 定时器回调接口。
/// 定时器只在调用 CTimerMgr.Update 的逻辑线程中触发。
/// </summary>
public interface ITimer
{
    /// <summary>
    /// 定时器到期回调。
    /// </summary>
    /// <param name="timerId">创建定时器时传入的业务标识。</param>
    /// <param name="triggerCount">当前定时器在本次生命周期内的触发次数，从 1 开始。</param>
    void OnTimer(uint timerId, uint triggerCount);
}
