/// <summary>
/// 角色数据定时保存触发器。
/// 该类属于 GameServer 业务层，只把到期事件转换为角色保存标记。
/// </summary>
public sealed class SaveRoleDataTimer : ITimer
{
    /// <summary>角色保存周期，单位为毫秒。</summary>
    private const int SaveIntervalMilliseconds = 60000;

    /// <summary>所属在线角色。</summary>
    private OnlineRole _role;
    /// <summary>在 CTimerMgr 中注册的定时器句柄。</summary>
    private long _timerHandle = CTimerMgr.InvalidTimer;
    /// <summary>是否已经到达保存时间。</summary>
    private bool _needSave;

    /// <summary>绑定定时器所属角色。</summary>
    /// <param name="role">在线角色。</param>
    public void SetRole(OnlineRole role)
    {
        _role = role;
    }

    /// <summary>向公共定时器管理器注册无限循环保存定时器。</summary>
    public void Start()
    {
        if (_role == null || !_role.IsRoleDataReady())
        {
            return;
        }
        if (_timerHandle != CTimerMgr.InvalidTimer)
        {
            return;
        }

        _timerHandle = CTimerMgr.Instance.SetTimer(
            this,
            0,
            SaveIntervalMilliseconds,
            CTimerMgr.InfiniteRepeatCount);
    }

    /// <summary>取消定时器并清除待保存标记。</summary>
    public void Stop()
    {
        if (_timerHandle != CTimerMgr.InvalidTimer)
        {
            CTimerMgr.Instance.KillTimer(_timerHandle);
            _timerHandle = CTimerMgr.InvalidTimer;
        }

        _needSave = false;
    }

    /// <summary>定时器到期回调，只设置标记，不在回调中组包或访问网络。</summary>
    public void OnTimer(uint timerId, uint triggerCount)
    {
        if (_role == null || !_role.IsRoleDataReady())
        {
            return;
        }
        _role.SetFlag(RoleStateFlag.RSF_ISRoleDataNeedSave, true);
        _needSave = true;
    }

    /// <summary>消费一次待保存标记。</summary>
    /// <returns>存在待保存标记时返回 true，否则返回 false。</returns>
    public bool ConsumeNeedSave()
    {
        if (!_needSave)
        {
            return false;
        }

        _needSave = false;
        return true;
    }
}
