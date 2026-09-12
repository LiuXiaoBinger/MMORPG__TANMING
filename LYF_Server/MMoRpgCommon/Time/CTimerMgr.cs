using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 进程内逻辑定时器管理器。
/// 所有接口只能由逻辑线程调用；倒计时与回调派发集中在 Update 中。
/// </summary>
public sealed class CTimerMgr
{
    /// <summary>无效定时器句柄。</summary>
    public const long InvalidTimer = 0L;

    /// <summary>无限循环触发标记。</summary>
    public const int InfiniteRepeatCount = -1;

    private sealed class TimerEntry
    {
        /// <summary>定时器句柄。</summary>
        private long _handle;
        /// <summary>定时器回调对象。</summary>
        private ITimer _timer;
        /// <summary>业务标识。</summary>
        private uint _timerId;
        /// <summary>触发间隔，单位为毫秒。</summary>
        private int _intervalMilliseconds;
        /// <summary>剩余触发次数，负数表示无限触发。</summary>
        private int _repeatCount;
        /// <summary>距离下一次触发的剩余时间。</summary>
        private int _remainingMilliseconds;
        /// <summary>当前生命周期已经触发的次数。</summary>
        private uint _triggerCount;
        /// <summary>本次回调执行后是否移除。</summary>
        private bool _removeAfterCallback;

        /// <summary>创建一个定时器条目。</summary>
        internal TimerEntry(long handle, ITimer timer, uint timerId, int intervalMilliseconds, int repeatCount)
        {
            _handle = handle;
            _timer = timer;
            _timerId = timerId;
            _intervalMilliseconds = intervalMilliseconds;
            _repeatCount = repeatCount;
            _remainingMilliseconds = intervalMilliseconds;
            _triggerCount = 0;
            _removeAfterCallback = false;
        }

        /// <summary>获取定时器句柄。</summary>
        internal long Handle
        {
            get { return _handle; }
        }

        /// <summary>获取下一次触发的剩余时间。</summary>
        internal int RemainingMilliseconds
        {
            get { return _remainingMilliseconds; }
        }

        /// <summary>获取本次回调后是否应该回收。</summary>
        internal bool RemoveAfterCallback
        {
            get { return _removeAfterCallback; }
        }

        /// <summary>推进定时器并返回本轮是否到期。</summary>
        internal bool Advance(int deltaMilliseconds)
        {
            _remainingMilliseconds -= deltaMilliseconds;
            if (_remainingMilliseconds > 0)
            {
                return false;
            }

            _remainingMilliseconds = _intervalMilliseconds;
            _triggerCount++;
            if (_repeatCount != InfiniteRepeatCount)
            {
                _repeatCount--;
                if (_repeatCount <= 0)
                {
                    _removeAfterCallback = true;
                }
            }

            return true;
        }

        /// <summary>执行定时器回调。</summary>
        internal void Invoke()
        {
            _timer.OnTimer(_timerId, _triggerCount);
        }
    }

    /// <summary>进程内唯一的定时器管理器实例。</summary>
    private static readonly CTimerMgr _instance = new CTimerMgr();
    /// <summary>当前所有已注册的定时器条目。</summary>
    private readonly Dictionary<long, TimerEntry> _timerEntries =
        new Dictionary<long, TimerEntry>();
    /// <summary>本次 Update 到期的条目缓存，重复使用以避免热路径分配。</summary>
    private readonly List<TimerEntry> _dueEntries = new List<TimerEntry>();
    /// <summary>下一个待分配的定时器句柄。</summary>
    private long _nextHandle;

    private CTimerMgr()
    {
    }

    /// <summary>获取进程内唯一的定时器管理器。</summary>
    public static CTimerMgr Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// 注册一个定时器。
    /// </summary>
    /// <param name="timer">定时器回调对象。</param>
    /// <param name="timerId">传给回调的业务标识。</param>
    /// <param name="intervalMilliseconds">触发间隔，必须大于零。</param>
    /// <param name="repeatCount">触发次数，-1 表示无限循环。</param>
    /// <returns>成功返回句柄，参数无效时返回 InvalidTimer。</returns>
    /// <remarks>该接口只能在 GameServer 逻辑线程调用。</remarks>
    public long SetTimer(ITimer timer, uint timerId, 
        int intervalMilliseconds, int repeatCount)
    {
        if (timer == null || intervalMilliseconds <= 0)
        {
            return InvalidTimer;
        }

        if (repeatCount == 0 || repeatCount < InfiniteRepeatCount)
        {
            return InvalidTimer;
        }

        long handle = CreateHandle();
        TimerEntry entry = new TimerEntry(
            handle,
            timer,
            timerId,
            intervalMilliseconds,
            repeatCount);
        _timerEntries.Add(handle, entry);
        return handle;
    }

    /// <summary>注册一个无限循环定时器，只能在逻辑线程调用。</summary>
    public long SetTimer(ITimer timer, uint timerId, 
        int intervalMilliseconds)
    {
        return SetTimer(timer, timerId, intervalMilliseconds,
            InfiniteRepeatCount);
    }

    /// <summary>
    /// 取消指定定时器。重复取消或传入无效句柄不会产生副作用。
    /// </summary>
    /// <param name="handle">SetTimer 返回的句柄。</param>
    /// <remarks>该接口只能在 GameServer 逻辑线程调用。</remarks>
    public void KillTimer(long handle)
    {
        if (handle == InvalidTimer)
        {
            return;
        }

        _timerEntries.Remove(handle);
    }

    /// <summary>查询距离下一次触发的毫秒数，句柄无效时返回零。</summary>
    /// <remarks>该接口只能在 GameServer 逻辑线程调用。</remarks>
    public int GetTimeLeft(long handle)
    {
        TimerEntry entry;
        if (!_timerEntries.TryGetValue(handle, out entry))
        {
            return 0;
        }

        return entry.RemainingMilliseconds;
    }

    /// <summary>判断定时器句柄当前是否仍处于注册状态。</summary>
    /// <remarks>该接口只能在 GameServer 逻辑线程调用。</remarks>
    public bool IsActive(long handle)
    {
        return handle != InvalidTimer && _timerEntries.ContainsKey(handle);
    }

    /// <summary>
    /// 推进所有定时器。
    /// 每个定时器每次 Update 最多触发一次，超出的间隔不补发，避免掉帧时回调风暴。
    /// </summary>
    /// <param name="deltaMilliseconds">本次逻辑帧经过的毫秒数。</param>
    /// <remarks>该接口只能在 GameServer 逻辑线程调用。</remarks>
    public void Update(int deltaMilliseconds)
    {
        if (deltaMilliseconds <= 0 || _timerEntries.Count == 0)
        {
            return;
        }

        _dueEntries.Clear();
        foreach (TimerEntry entry in _timerEntries.Values)
        {
            if (!entry.Advance(deltaMilliseconds))
            {
                continue;
            }

            _dueEntries.Add(entry);
        }

        foreach (TimerEntry entry in _dueEntries)
        {
            if (!_timerEntries.ContainsKey(entry.Handle))
            {
                continue;
            }

            try
            {
                entry.Invoke();
            }
            catch (Exception exception)
            {
                Trace.TraceError("定时器回调异常，句柄=" + entry.Handle + "，异常=" + exception);
            }

            if (entry.RemoveAfterCallback)
            {
                _timerEntries.Remove(entry.Handle);
            }
        }

        _dueEntries.Clear();
    }

    /// <summary>清理全部定时器，服务器退出时调用。</summary>
    /// <remarks>该接口只能在 GameServer 逻辑线程调用。</remarks>
    public void Clear()
    {
        _timerEntries.Clear();
    }

    /// <summary>生成不重复的正数句柄。</summary>
    private long CreateHandle()
    {
        if (_nextHandle == long.MaxValue)
        {
            _nextHandle = 1L;
        }
        else
        {
            _nextHandle++;
        }

        while (_timerEntries.ContainsKey(_nextHandle))
        {
            if (_nextHandle == long.MaxValue)
            {
                _nextHandle = 1L;
            }
            else
            {
                _nextHandle++;
            }
        }

        return _nextHandle;
    }
}
