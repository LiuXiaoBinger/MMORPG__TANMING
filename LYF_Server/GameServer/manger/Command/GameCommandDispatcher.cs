using System;
using System.Collections.Concurrent;

/// <summary>
/// GameServer 网络消息队列。
/// 网络线程只生产 GameCommand；主线程取出后交由客户端消息管理器消费。
/// </summary>
public sealed class GameCommandDispatcher : Singleton<GameCommandDispatcher>
{
    // ConcurrentQueue 只负责跨线程传递，命令内容本身由 GameCommand 保证不可变。
    private readonly ConcurrentQueue<GameCommand> _pendingCommands =
        new ConcurrentQueue<GameCommand>();

    // 参考服务器按逻辑帧限制网络输入，避免突发消息拖垮全服逻辑。
    // 单轮消费上限防止突发网络流量挤占逻辑帧；总容量上限防止内存无界增长。
    private const int MaxCommandsPerTick = 3000;
    private const int MaxPendingCommands = 20000;
    // 使用独立计数器避免对 ConcurrentQueue.Count 进行高成本统计。
    private int _pendingCount;
    // 停服时先禁止生产者入队，再由逻辑线程按策略排空队列。
    private volatile bool _accepting = true;

    public int PendingCount
    {
        get { return _pendingCount; }
    }

    /// <summary>
    /// 将网络命令放入待处理队列。
    /// 仅允许网络回调线程调用，游戏状态由逻辑帧统一修改。
    /// </summary>
    /// <param name="command">已完成协议解析的游戏命令。</param>
    /// <returns>命令成功入队时返回 true；停止接收、命令为空或队列已满时返回 false。</returns>
    public bool Enqueue(GameCommand command)
    {
        // 网络回调线程只执行入队，不在这里触碰角色、场景或背包状态。
        if (!_accepting || command == null)
        {
            return false;
        }

        // 先原子增加待处理数量，避免多个网络线程同时入队时突破容量上限。
        int pendingCount = System.Threading.Interlocked.Increment(ref _pendingCount);
        if (pendingCount > MaxPendingCommands)
        {
            // 本次命令不入队，必须回滚刚才预占的待处理数量。
            System.Threading.Interlocked.Decrement(ref _pendingCount);
            LogMsg.Info("GameServer 命令队列已满，拒绝命令 " + command.CommandCode, LogMsgType.Error);
            return false;
        }

        // 容量校验通过后再写入并发队列，等待下一逻辑帧消费。
        _pendingCommands.Enqueue(command);
        return true;
    }

    /// <summary>
    /// 只允许游戏主循环调用。网络消息在此阶段派发给对应游戏模块。
    /// </summary>
    public int DispatchPending()
    {
        // 此方法运行在 GameServer 主线程，模块可以安全地修改游戏状态。
        int processedCount = 0;
        GameCommand command;
        while (processedCount < MaxCommandsPerTick &&
               _pendingCommands.TryDequeue(out command))
        {
            System.Threading.Interlocked.Decrement(ref _pendingCount);
            try
            {
                ClientMessageManager.Instance.EnqueueNetMessage(command);
            }
            catch (Exception ex)
            {
                LogMsg.Info(
                    "GameCommand 执行失败，序号=" + command.Sequence + ", 命令=" +
                    command.CommandCode + ": " + ex.Message,
                    LogMsgType.Error);
            }

            processedCount++;
        }

        if (_pendingCount > 0)
        {
            LogMsg.Info("GameServer 命令队列积压: " + _pendingCount, LogMsgType.Warn);
        }

        return processedCount;
    }

    /// <summary>
    /// 停止接收新命令，停服时由主线程排空剩余关键消息。
    /// </summary>
    public void StopAccepting()
    {
        _accepting = false;
    }
}
