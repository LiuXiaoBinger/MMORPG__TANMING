using System.Collections.Generic;
using UnityEngine;

/// <summary>客户端主线程时间轴计时器，保留 RO 接口名称与毫秒时钟，使用强类型技能参数。</summary>
public sealed class MTimerMgr
{
    /// <summary>全局唯一计时器，由 Global 每帧推进一次，不按角色或技能数量重复推进。</summary>
    public static readonly MTimerMgr singleton = new MTimerMgr();

    /// <summary>已到期事件回调；零秒事件使用 token=0 同步执行，正延迟使用非零票据。</summary>
    /// <param name="token">本次一次性计时器票据，取消后不会再次触发。</param>
    /// <param name="args">本次施法 UUID 和具体表现指令，不允许通用弱类型载荷。</param>
    public delegate void TimerEventHandler(uint token, MSkillTimerArgs args);

    /// <summary>按到期时刻稳定排序的待触发列表；帧推进不创建快照或临时集合。</summary>
    private readonly List<MTimer> _timers = new List<MTimer>();

    /// <summary>最近分配票据，初始化不归零，防止跨初始化旧票据误杀新注册。</summary>
    private uint _token;

    /// <summary>已经推进的非负整数毫秒，与实体世界使用相同帧间隔来源。</summary>
    private long _elapsedTime;

    /// <summary>最近一次 Init 的会话版本，时钟重新归零并推进到相同毫秒也能拒绝旧程序运动。</summary>
    private ulong _sessionVersion;

    /// <summary>全局计时器初始化会话的只读身份，不参与计时排序或票据分配。</summary>
    public ulong SessionVersion { get { return _sessionVersion; } }

    /// <summary>暂停时不推进正延迟计时器；零秒同步回调沿 RO 原语义仍立即执行。</summary>
    public bool IsPause { get; set; }

    /// <summary>管理器运行累计毫秒数，初始化后从零开始。</summary>
    public long ElapsedTime { get { return _elapsedTime; } }

    /// <summary>只允许创建 singleton 时构造管理器，避免角色或技能另建时钟产生重复推进。</summary>
    private MTimerMgr()
    {
    }

    /// <summary>按 RO 的秒参数推进整数毫秒，每个到期事件仅触发一次。</summary>
    /// <param name="fDeltaT">Global 从实体帧间隔毫秒换算的非负秒数。</param>
    public void Update(float fDeltaT)
    {
        // 帧间隔为 NaN、无穷或负值时沿 RO 语义跳过本帧推进，避免污染累计时钟。
        bool deltaTInvalid = float.IsNaN(fDeltaT) || float.IsInfinity(fDeltaT) || fDeltaT < 0f;
        if (IsPause || deltaTInvalid)
        {
            return;
        }
        _elapsedTime += Mathf.RoundToInt(fDeltaT * 1000f);
        while (!IsPause && _timers.Count > 0 && _timers[0].TriggerTime <= _elapsedTime)
        {
            // 先从管理器移除条目，回调内取消、重注册或重置施法不会重复触发该票据。
            MTimer timer = _timers[0];
            _timers.RemoveAt(0);
            try
            {
                timer.Handler(timer.Token, timer.Args);
            }
            catch (System.Exception exception)
            {
                // exception 为单条表现抛出的异常，记录后继续其他角色或轨道的到期回调。
                Debug.LogException(exception);
            }
        }
    }

    /// <summary>清空旧计时器并启动新的全局时钟，返回初始化成功状态。</summary>
    /// <returns>主线程初始化完成时返回 true。</returns>
    public bool Init()
    {
        KillTimerAll();
        unchecked { _sessionVersion++; }
        _elapsedTime = 0;
        IsPause = false;
        return true;
    }

    /// <summary>客户端退出时解除全部技能委托与载荷引用并暂停。</summary>
    public void Uninit()
    {
        KillTimerAll();
        IsPause = true;
    }

    /// <summary>登记单次时间轴回调，非正延迟沿 RO 语义立即回调并返回零。</summary>
    /// <param name="interval">距当前管理器时钟的延迟秒数，正数换算为至少一毫秒。</param>
    /// <param name="handler">到期回调，必须在主线程登记。</param>
    /// <param name="args">具名施法载荷，取消后不会继续持有。</param>
    /// <returns>正延迟注册的非零票据；同步执行或无效登记返回零。</returns>
    public uint SetTimer(float interval, TimerEventHandler handler, MSkillTimerArgs args)
    {
        // 回调或载荷为空，属于无效登记。
        bool callbackInvalid = handler == null || args == null;
        // 延迟秒数为 NaN 或无穷，无法换算成整数毫秒。
        bool intervalInvalid = float.IsNaN(interval) || float.IsInfinity(interval);
        if (callbackInvalid || intervalInvalid)
        {
            return 0;
        }
        if (interval <= 0f)
        {
            handler(0, args);
            return 0;
        }
        // 秒转毫秒沿 RO 取整；正数小于半毫秒仍保留一次异步触发，不误成同步事件。
        int delayMilliseconds = Mathf.Max(1, Mathf.RoundToInt(interval * 1000f));
        // 本次正延迟登记条目；同一时刻的条目按插入顺序稳定派发。
        MTimer timer = new MTimer
        {
            Token = CreateToken(),
            RegisterTime = _elapsedTime,
            TriggerTime = _elapsedTime + delayMilliseconds,
            Handler = handler,
            Args = args
        };
        // 插入位置从尾部寻找，只移动更晚到期的条目，同毫秒保持先注册者在前。
        int insertIndex = _timers.Count;
        while (insertIndex > 0 && _timers[insertIndex - 1].TriggerTime > timer.TriggerTime)
        {
            insertIndex--;
        }
        _timers.Insert(insertIndex, timer);
        return timer.Token;
    }

    /// <summary>生成未被在用条目占据的非零票据，包含 uint 回绕保护。</summary>
    /// <returns>可用于取消和查询的新票据。</returns>
    private uint CreateToken()
    {
        do
        {
            unchecked { _token++; }
            if (_token == 0)
            {
                _token = 1;
            }
        } while (FindTimer(_token) >= 0);
        return _token;
    }

    /// <summary>按票据定位仍待触发的条目，不接收零秒同步回调票据。</summary>
    /// <param name="token">要查询或取消的非零票据。</param>
    /// <returns>列表中的零起始位置；不存在时返回 -1。</returns>
    private int FindTimer(uint token)
    {
        // index 按稳定列表次序查找，取消不触发任何回调。
        for (int index = 0; index < _timers.Count; index++)
        {
            if (_timers[index].Token == token)
            {
                return index;
            }
        }
        return -1;
    }

    /// <summary>查询票据距离到期的剩余秒数，触发或取消后返回零。</summary>
    /// <param name="token">正延迟登记返回的票据。</param>
    /// <returns>尚未触发时的剩余秒数。</returns>
    public float TimeLeft(uint token)
    {
        // 当前票据在待触发列表的位置，取消或到期后为 -1。
        int index = FindTimer(token);
        if (index >= 0)
        {
            return (_timers[index].TriggerTime - _elapsedTime) * 0.001f;
        }
        return 0f;
    }

    /// <summary>查询票据登记至今的经过秒数，触发或取消后返回零。</summary>
    /// <param name="token">正延迟登记返回的票据。</param>
    /// <returns>当前管理器时钟减去登记时钟的秒数。</returns>
    public float TimeDuration(uint token)
    {
        // 当前票据的零起始列表位置，不存在时不读取条目。
        int index = FindTimer(token);
        if (index >= 0)
        {
            return (_elapsedTime - _timers[index].RegisterTime) * 0.001f;
        }
        return 0f;
    }

    /// <summary>取消尚未触发的一次性回调，零票据和重复取消均无副作用。</summary>
    /// <param name="token">本次需要解除引用的计时器票据。</param>
    public void KillTimer(uint token)
    {
        // 取消目标在列表中的位置；不存在表示已同步执行、到期或已取消。
        int index = FindTimer(token);
        if (index >= 0)
        {
            _timers.RemoveAt(index);
        }
    }

    /// <summary>全局退出或重新初始化时解除全部尚未触发回调，普通施法取消只调用 KillTimer。</summary>
    public void KillTimerAll()
    {
        _timers.Clear();
    }

    /// <summary>待触发计时器，排序列表持有到触发或显式取消；同到期时刻保留注册顺序。</summary>
    private sealed class MTimer
    {
        /// <summary>当前注册的非零票据，取消和查询均以该值定位。</summary>
        public uint Token;

        /// <summary>在本管理器时钟中的到期毫秒数。</summary>
        public long TriggerTime;

        /// <summary>登记时的管理器毫秒数，供 TimeDuration 查询。</summary>
        public long RegisterTime;

        /// <summary>到期时调用的主线程委托，取消后随条目一起解除引用。</summary>
        public TimerEventHandler Handler;

        /// <summary>施法身份与表现指令的不可变引用，随条目取消或触发后解除。</summary>
        public MSkillTimerArgs Args;
    }
}
