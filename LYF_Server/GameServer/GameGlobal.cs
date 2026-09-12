


using System;
using System.Collections.Concurrent;
using System.Diagnostics;

public class GameGlobal:Singleton<GameGlobal>
{
    // 逻辑帧间隔，50 毫秒对应每秒 20 次逻辑更新。
    private const int LogicFrameMilliseconds = 50;
    // 单次主循环最多追赶的逻辑帧数量，剩余时间保留到下一轮。
    private const int MaxCatchUpFrameCount = 5;

    // 上一次主循环采样的单调时钟刻度，避免系统校时影响间隔计算。
    private long _lastUpdateTimestamp;

    // 尚未累积到一帧的时间，达到固定帧间隔后才驱动游戏逻辑。
    private long _deltaMilliseconds;
    // 网络线程产生的网关断开事件，延迟到逻辑线程统一处理。
    private readonly ConcurrentQueue<int> _disconnectedGateSessions =
        new ConcurrentQueue<int>();

    /// <summary>
    /// 从服务器启动起累计的逻辑 Tick，用于 Buff、冷却和定时任务的服务端时间基准。
    /// </summary>
    public long LogicTick { get; private set; }

    /// <summary>
    /// 初始化游戏全局状态和逻辑计时基准。
    /// </summary>
    public void Init()
    {
        // 初始化时间基准，避免服务器启动前的时间差被计入第一帧。
        _lastUpdateTimestamp = Stopwatch.GetTimestamp();
        _deltaMilliseconds = 0;
        LogicTick = 0;
        //商店
        ShopManager.Instance.Initialize();
    }

    /// <summary>
    /// 网络线程只负责投递不可变命令信封，所有游戏业务在主循环中统一执行。
    /// </summary>
    public void EnqueueGameCommand(GameCommandSource source, ServerBase transport, BasePackage basePackage)
    {
        GameCommandDispatcher.Instance.Enqueue(GameCommand.From(source, transport, basePackage));
    }

    /// <summary>网络线程只记录网关会话断开，避免直接修改角色状态。</summary>
    public void EnqueueGateSessionDisconnected(int gateSessionId)
    {
        if (gateSessionId > 0)
        {
            _disconnectedGateSessions.Enqueue(gateSessionId);
        }
    }

    /// <summary>
    /// 主循环入口：先处理网络消息，再按固定时间步驱动逻辑模块。
    /// </summary>
    public void Update()
    {
        ProcessNetMessage();

        long nowTimestamp = Stopwatch.GetTimestamp();
        long deltaMilliseconds = GetElapsedMilliseconds(nowTimestamp, _lastUpdateTimestamp);
        _lastUpdateTimestamp = nowTimestamp;
        if (deltaMilliseconds < 0)
        {
            deltaMilliseconds = 0;
        }

        if (deltaMilliseconds > long.MaxValue - _deltaMilliseconds)
        {
            _deltaMilliseconds = long.MaxValue;
        }
        else
        {
            _deltaMilliseconds += deltaMilliseconds;
        }

        int catchUpFrameCount = 0;
        while (_deltaMilliseconds >= LogicFrameMilliseconds &&
               catchUpFrameCount < MaxCatchUpFrameCount)
        {
            // 每次只消费一个固定步长，50 到 99 毫秒时仍保留不足一帧的余量。
            _deltaMilliseconds -= LogicFrameMilliseconds;
            LogicTick++;
            UpdateLogicFrame(LogicFrameMilliseconds);
            catchUpFrameCount++;
        }
    }

    /// <summary>将单调时钟刻度差转换为毫秒，时钟未前进时返回零。</summary>
    private static long GetElapsedMilliseconds(long currentTimestamp, long previousTimestamp)
    {
        if (currentTimestamp <= previousTimestamp)
        {
            return 0;
        }

        long timestampDelta = currentTimestamp - previousTimestamp;
        double milliseconds = timestampDelta * 1000.0 / Stopwatch.Frequency;
        if (milliseconds >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)milliseconds;
    }

    /// <summary>
    /// 网络阶段：从并发队列取出消息并派发到各协议所属模块；每轮最多处理 3000 条。
    /// </summary>
    public int ProcessNetMessage()
    {
        int count = GameCommandDispatcher.Instance.DispatchPending();
        ClientMessageManager.Instance.ProcessPendingCommands();
        int gateSessionId;
        while (_disconnectedGateSessions.TryDequeue(out gateSessionId))
        {
            RoleManager.Instance.HandleGateSessionDisconnected(gateSessionId);
        }
        return count;
    }

    /// <summary>
    /// 执行一次固定逻辑帧，并隔离模块异常，避免单个模块中断主循环。
    /// </summary>
    /// <param name="deltaMilliseconds">本次逻辑帧使用的时间跨度，单位为毫秒。</param>
    private void UpdateLogicFrame(int deltaMilliseconds)
    {
        // 固定帧只执行一次；网络消息处理已在本轮前完成。
        // 公共定时器统一在逻辑线程推进，角色保存等业务只实现 ITimer 回调。
        CTimerMgr.Instance.Update(deltaMilliseconds);
        UpdateManager("RedisManager", RedisManager.Instance.Update, deltaMilliseconds);
        UpdateManager("RoGamesManager", RoGamesManager.Instance.Update, deltaMilliseconds);
        UpdateManager("RoleManager", RoleManager.Instance.Update, deltaMilliseconds);
        UpdateManager("DecoupleManager", DecoupleManager.Instance.Update, deltaMilliseconds);
        //UpdateManager("HttpTaskManager", HttpTaskManager.Instance.Update, deltaMilliseconds);
        UpdateManager("MonsterWaveManager", MonsterWaveManager.Instance.Update, deltaMilliseconds);
        UpdateManager("LimitedTimeOfferManager", LimitedTimeOfferManager.Instance.Update, deltaMilliseconds);
        UpdateManager("MerchantEventManager", MerchantEventManager.Instance.Update, deltaMilliseconds);
        UpdateManager("MallRefreshManager", MallRefreshManager.Instance.Update, deltaMilliseconds);
        UpdateManager("OpenSystemManager", OpenSystemManager.Instance.Update, deltaMilliseconds);
        UpdateManager("BackstageActivityManager", BackstageActivityManager.Instance.Update, deltaMilliseconds);
        UpdateManager("TimeLimitManager", TimeLimitManager.Instance.Update, deltaMilliseconds);
        //UpdateManager("FrameManager", FrameManager.Instance.Update, deltaMilliseconds);
        UpdateManager("SceneManager", SceneManager.Instance.Update, deltaMilliseconds);
        UpdateManager("WorldEventManager", WorldEventManager.Instance.Update, deltaMilliseconds);
        UpdateManager("GvgManager", GvgManager.Instance.Update, deltaMilliseconds);
    }

    private static void UpdateManager(string managerName, Action<int> update, int deltaMilliseconds)
    {
        try
        {
            update(deltaMilliseconds);
        }
        catch (Exception ex)
        {
            LogMsg.Info(managerName + " 更新失败: " + ex, LogMsgType.Error);
        }
    }
}
