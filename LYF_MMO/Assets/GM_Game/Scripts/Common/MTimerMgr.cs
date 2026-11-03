using System.Collections.Generic;
using UnityEngine;

/// <summary>在 Unity 主线程累计时间，到点后执行技能回调；外部传秒，内部按整数毫秒计时。</summary>
public sealed class MTimerMgr
{

    #region 字段与属性
    /// <summary>全局唯一计时器，由 Global 每帧推进一次，不按角色或技能数量重复推进。</summary>
    public static readonly MTimerMgr singleton = new MTimerMgr();

    /// <summary>计时结束时执行的方法；立即执行时 token 为 0，延迟执行时为可查询、取消的编号。</summary>
    /// <param name="token">本次计时器编号；每次登记最多触发一次。</param>
    /// <param name="args">本次施法的 UUID 和要播放的技能表现指令。</param>
    public delegate void TimerEventHandler(uint token, MSkillTimerArgs args);

    /// <summary>尚未执行的计时器，按到期时间排列；到期时间相同的按登记先后执行。</summary>
    private readonly List<MTimer> _timers = new List<MTimer>();

    /// <summary>上次分配的计时器编号；Init 不将它归零，避免旧编号立即被新计时器复用。</summary>
    private uint _token;

    /// <summary>已经推进的非负整数毫秒，与实体世界使用相同帧间隔来源。</summary>
    private long _elapsedTime;

    /// <summary>每次 Init 递增的编号，供使用者区分计时器重启前后的记录。</summary>
    private ulong _sessionVersion;

    /// <summary>本轮初始化编号；使用者可以据此识别时钟重置前留下的数据。</summary>
    public ulong SessionVersion { get { return _sessionVersion; } }

    /// <summary>暂停累计时间和延迟回调；SetTimer 的非正延迟回调仍会当场执行。</summary>
    public bool IsPause { get; set; }

    /// <summary>管理器运行累计毫秒数，初始化后从零开始。</summary>
    public long ElapsedTime { get { return _elapsedTime; } }
    #endregion

    #region 构造与初始化
    /// <summary>只允许创建 singleton 时构造管理器，避免角色或技能另建时钟产生重复推进。</summary>
    private MTimerMgr()
    {
    }

    /// <summary>将本帧秒数换成毫秒累计，再依次移除并执行到期回调。</summary>
    /// <param name="fDeltaT">Global 从实体帧间隔毫秒换算的非负秒数。</param>
    public void Update(float fDeltaT)
    {
        // 无效或负的帧间隔不能计入时钟，本帧直接跳过。
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
    #endregion

    /// <summary>生成一个未使用的非零编号；编号溢出回到 0 时跳过 0。</summary>
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

    #region 核心逻辑
    /// <summary>退出时清空所有未执行的技能回调及参数，并暂停计时。</summary>
    public void Uninit()
    {
        KillTimerAll();
        IsPause = true;
    }

    /// <summary>登记一个只执行一次的技能回调；延迟小于等于 0 时立即调用并返回 0。</summary>
    /// <param name="interval">距当前管理器时钟的延迟秒数，正数换算为至少一毫秒。</param>
    /// <param name="handler">到期回调，必须在主线程登记。</param>
    /// <param name="args">这次回调使用的施法参数；取消时随计时器一起移除。</param>
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
        // 将秒数四舍五入为毫秒；只要延迟为正，至少等 1 毫秒再由 Update 执行。
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
    #endregion

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

        /// <summary>要交给回调的施法标识和表现指令，计时器取消或执行后不再由列表保存。</summary>
        public MSkillTimerArgs Args;
    }
}
