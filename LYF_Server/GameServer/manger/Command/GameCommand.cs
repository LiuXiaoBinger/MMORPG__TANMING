using System;
using System.Threading;
using Google.Protobuf;

/// <summary>
/// GameServer 网络消息的不可变信封。
/// 网络线程只创建该对象并入队，业务线程通过信封恢复协议包后执行逻辑。
/// </summary>
public sealed class GameCommand
{
    private static long _nextSequence;

    /// <summary>协议号，用于模块路由。</summary>
    public int CommandCode { get; private set; }
    /// <summary>消息来源，只允许对应来源的模块处理。</summary>
    public GameCommandSource Source { get; private set; }
    /// <summary>网关会话和 Unity 会话，用于回包及角色身份校验。</summary>
    public GameCommandSession Session { get; private set; }
    /// <summary>网络线程复制后的协议载荷，避免跨线程持有可变网络包。</summary>
    public ByteString Payload { get; private set; }
    /// <summary>入队时间，用于后续监控消息等待时长。</summary>
    public DateTime EnqueuedAtUtc { get; private set; }
    /// <summary>全局递增序号，便于日志追踪单条命令。</summary>
    public long Sequence { get; private set; }

    // 只读保存发送端，用于主线程执行业务后的转发或回包。
    public ServerBase Transport { get; private set; }

    private GameCommand(
        int commandCode,
        GameCommandSource source,
        GameCommandSession session,
        ByteString payload,
        ServerBase transport)
    {
        CommandCode = commandCode;
        Source = source;
        Session = session;
        Payload = payload ?? ByteString.Empty;
        EnqueuedAtUtc = DateTime.UtcNow;
        Sequence = Interlocked.Increment(ref _nextSequence);
        Transport = transport;
    }

    /// <summary>
    /// 从网络层的 BasePackage 创建信封。BasePackage 本身不会跨线程共享。
    /// </summary>
    public static GameCommand From(
        GameCommandSource source,
        ServerBase transport,
        BasePackage basePackage)
    {
        if (basePackage == null)
        {
            return null;
        }

        return new GameCommand(
            basePackage.ProtoCode,
            source,
            new GameCommandSession(basePackage.UnitySessionId, basePackage.GateSessionId),
            basePackage.Data,
            transport);
    }

    /// <summary>
    /// 在主线程恢复一个可发送的协议包，避免继续使用网络回调中的可变对象。
    /// </summary>
    public BasePackage ToBasePackage()
    {
        return new BasePackage
        {
            ProtoCode = CommandCode,
            UnitySessionId = Session.UnitySessionId,
            GateSessionId = Session.GateSessionId,
            Data = Payload
        };
    }
}
