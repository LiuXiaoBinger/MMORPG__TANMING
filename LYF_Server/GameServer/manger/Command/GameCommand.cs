using System;
using System.Threading;
using Google.Protobuf;

/// <summary>
/// GameServer 跨线程传递的一条消息记录，创建后不再改写其中的协议和会话数据。
/// 网络线程只创建该对象并入队，业务线程把记录还原成协议包后执行逻辑。
/// </summary>
public sealed class GameCommand
{
    /// <summary>跨网络线程递增的消息序号，只用于追踪本进程内的命令。</summary>
    private static long _nextSequence;

    /// <summary>协议号，用于模块路由。</summary>
    public int CommandCode { get; private set; }
    /// <summary>消息来源，只允许对应来源的模块处理。</summary>
    public GameCommandSource Source { get; private set; }
    /// <summary>网关会话和 Unity 会话，用于回包及角色身份校验。</summary>
    public GameCommandSession Session { get; private set; }
    /// <summary>不可变的 ByteString 消息体；与原包共享字节对象，但不共享可改写的 BasePackage。</summary>
    public ByteString Payload { get; private set; }
    /// <summary>入队时间，用于后续监控消息等待时长。</summary>
    public DateTime EnqueuedAtUtc { get; private set; }
    /// <summary>全局递增序号，便于日志追踪单条命令。</summary>
    public long Sequence { get; private set; }

    // 只读保存发送端，用于主线程执行业务后的转发或回包。
    public BaseLink Transport { get; private set; }

    /// <summary>记录消息内容、返回链路、入队时间和日志序号。</summary>
    /// <param name="commandCode">用于路由的协议号。</param>
    /// <param name="source">发送此包的服务器类别。</param>
    /// <param name="session">客户端与网关两层会话编号。</param>
    /// <param name="payload">protobuf 消息字节，ByteString 本身不可变。</param>
    /// <param name="transport">接收请求时的传输对象，供后续回包。</param>
    private GameCommand(
        int commandCode,
        GameCommandSource source,
        GameCommandSession session,
        ByteString payload,
        BaseLink transport)
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
    /// 提取网络包的协议号、会话和不可变消息体，建立逻辑线程要处理的记录。BasePackage 本身不跨线程共享。
    /// </summary>
    public static GameCommand From(
        GameCommandSource source,
        BaseLink transport,
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
