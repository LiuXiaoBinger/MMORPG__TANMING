using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;

/// <summary>
/// 测试用回环 TCP 会话：按正式 Session 构造签名注册到 SessionMgr，
/// 从对端 Socket 读取服务端真实写出的帧并按 Network 原帧头格式解码为 BasePackage。
/// 接收缓冲跨调用保留半帧，只有完整解码的帧才被消费，避免异步写出导致帧被后续断言吞掉。
/// 仅存在于测试进程，不注入运行中的服务器，生产 Send 未加任何 Mock 钩子。
/// </summary>
public sealed class LoopbackLink
{
    /// <summary>监听回环端口的监听器，端口 0 由系统分配。</summary>
    private readonly TcpListener _listener;

    /// <summary>测试侧客户端连接，用于读取服务端写出的字节。</summary>
    private readonly TcpClient _client;

    /// <summary>已接受的服务端侧 Socket，交由正式 Session 持有。</summary>
    private readonly Socket _accepted;

    /// <summary>正式 Session 实例，SessionMgr 自动分配会话 ID。</summary>
    private readonly Session _session;

    /// <summary>跨多次读取累积的原始字节，未解出完整帧的部分会保留到下一次读取。</summary>
    private readonly List<byte> _pending = new List<byte>();

    /// <summary>建立回环连接并按正式 Session 构造签名注册。</summary>
    /// <param name="asGameServerLink">
    /// true 表示模拟游戏服→网关的服务间连接：上游 NetClient 的 ClientType 为 GameServer，
    /// 使正式 Session.IsGameServerLink 判定为 true，实时协议继续走 TCP；
    /// false 表示模拟玩家终端会话：上游为空，实时协议在 KCP 未绑定时按设计被丢弃。
    /// </param>
    /// <param name="registeredServer">可选的真实 GS 注册表，由全链路回归传入。</param>
    public LoopbackLink(bool asGameServerLink, NetServer registeredServer = null)
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _client = new TcpClient();
        _client.Connect((IPEndPoint)_listener.LocalEndpoint);
        _accepted = _listener.AcceptSocket();
        // 只构造连接器保存类型，不调用 StartConnect，因此不会真的连接任何服务。
        NetClient upstream = null;
        if (asGameServerLink)
        {
            upstream = new NetClient(NetDefine.IPHost, NetDefine.GameServerPort, ClientType.GameServer);
        }
        // 捕获下行时使用空表，端到端测试直接复用正式 GS 的网络注册表。
        Dictionary<int, IContainer> handlers = new Dictionary<int, IContainer>();
        if (registeredServer != null)
        {
            handlers = (Dictionary<int, IContainer>)typeof(NetServer).GetField("_cmdDic",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(registeredServer);
        }
        _session = new Session(handlers, upstream);
        _session.ReceiveData(_accepted);
    }

    /// <summary>在回环连接对端模拟网关，向正式 GS 收包会话写入一帧。</summary>
    /// <param name="unityId">已登录玩家在网关的会话号。</param>
    /// <param name="code">原组队协议号。</param>
    /// <param name="request">与协议号对应的 protobuf 请求。</param>
    public void SendFromGate(int unityId, ushort code, IMessage request)
    {
        // 真实 Session 收到后会根据连接设置 GateSessionId。
        BasePackage packet = new BasePackage { UnitySessionId = unityId, ProtoCode = code, Data = request.ToByteString() };
        // 沿用正式帧格式写入测试网关侧 TCP。
        byte[] frame = NetUtils.Instance.MakeData(packet.ToByteArray());
        _client.GetStream().Write(frame, 0, frame.Length);
    }

    /// <summary>正式 Session 分配到的会话 ID，用于写入角色的 GateSessionId 或协议包目标会话。</summary>
    public int SessionId
    {
        get { return _session.SessionID; }
    }

    /// <summary>
    /// 在触发下一个动作前清空已完成帧：把缓冲里已完整解出的帧丢弃，半帧继续保留。
    /// 这样后续断言只会看到本次动作产生的帧，不会被上一次动作的残留帧冒充。
    /// </summary>
    public void ClearFrames()
    {
        // 单次短读即可把已到达的字节并入缓冲，再整体丢弃已完整解出的帧。
        ReadOnce(80);
    }

    /// <summary>
    /// 等待本轮期望的帧出现，或到达有限截止时间。
    /// 返回窗口内实际收到的全部帧，供调用方同时断言"期望帧存在"和"其他会话零收件"。
    /// </summary>
    /// <param name="expectedProtoCode">期望协议号。</param>
    /// <param name="expectedUnitySessionId">期望目标 Unity 会话号。</param>
    /// <param name="timeoutMs">等待上限毫秒数，通常 1000-2000。</param>
    /// <returns>窗口内收到的全部帧，可能为空。</returns>
    public List<DecodedFrame> WaitFrames(int expectedProtoCode, int expectedUnitySessionId, int timeoutMs)
    {
        List<DecodedFrame> collected = new List<DecodedFrame>();
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            List<DecodedFrame> fresh = ReadOnce(120);
            for (int index = 0; index < fresh.Count; index++)
            {
                collected.Add(fresh[index]);
            }
            if (Contains(collected, expectedProtoCode, expectedUnitySessionId))
            {
                return collected;
            }
            if (DateTime.UtcNow >= deadline)
            {
                return collected;
            }
        }
    }

    /// <summary>在给定窗口内收集全部帧，用于"不应收件"的负向断言。</summary>
    /// <param name="timeoutMs">观察窗口毫秒数。</param>
    /// <returns>窗口内收到的全部帧。</returns>
    public List<DecodedFrame> WaitWindow(int timeoutMs)
    {
        List<DecodedFrame> collected = new List<DecodedFrame>();
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            List<DecodedFrame> fresh = ReadOnce(120);
            for (int index = 0; index < fresh.Count; index++)
            {
                collected.Add(fresh[index]);
            }
            if (DateTime.UtcNow >= deadline)
            {
                return collected;
            }
        }
    }

    /// <summary>把正式 Session 从 SessionMgr 移除并关闭回环连接。</summary>
    public void Close()
    {
        SessionMgr.Instance.RemoveSession(_session.SessionID);
        _client.Close();
        _listener.Stop();
    }

    /// <summary>读取一次已到达的字节，并把缓冲中所有完整帧解码返回。</summary>
    /// <param name="timeoutMs">单次读取超时毫秒数。</param>
    /// <returns>本次新解码的完整帧。</returns>
    private List<DecodedFrame> ReadOnce(int timeoutMs)
    {
        NetworkStream stream = _client.GetStream();
        stream.ReadTimeout = timeoutMs;
        byte[] buffer = new byte[8192];
        while (true)
        {
            try
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }
                for (int index = 0; index < read; index++)
                {
                    _pending.Add(buffer[index]);
                }
            }
            catch (System.IO.IOException)
            {
                break;
            }
        }

        List<DecodedFrame> fresh = new List<DecodedFrame>();
        int offset = 0;
        while (_pending.Count - offset >= 3)
        {
            // 帧头：2 字节小端总长（含压缩标志与 CRC）+ 1 字节压缩标志 + 2 字节 CRC + 协议体。
            int messageLength = _pending[offset] | (_pending[offset + 1] << 8);
            int frameLength = 2 + messageLength;
            if (messageLength < 5 || _pending.Count - offset < frameLength)
            {
                // 半帧或长度异常：保留缓冲，等待下一次读取补齐。
                break;
            }
            // 整帧交给正式解码器，覆盖副本怪物通知可能触发的 gzip 压缩。
            byte[] networkFrame = new byte[frameLength];
            _pending.CopyTo(offset, networkFrame, 0, frameLength);
            // 校验 CRC，按压缩标志解压后再直接解析 protobuf。
            BasePackage package = BasePackage.Parser.ParseFrom(NetUtils.Instance.ParseData(networkFrame, messageLength));
            DecodedFrame frame = new DecodedFrame();
            frame.Package = package;
            frame.ProtoCode = package.ProtoCode;
            frame.UnitySessionId = package.UnitySessionId;
            fresh.Add(frame);
            offset += frameLength;
        }
        if (offset > 0)
        {
            _pending.RemoveRange(0, offset);
        }
        return fresh;
    }

    /// <summary>判断帧集合中是否存在指定协议号与目标会话的帧。</summary>
    /// <param name="frames">已解码帧列表。</param>
    /// <param name="protoCode">协议号。</param>
    /// <param name="unitySessionId">目标 Unity 会话号。</param>
    /// <returns>存在返回 true。</returns>
    private static bool Contains(List<DecodedFrame> frames, int protoCode, int unitySessionId)
    {
        for (int index = 0; index < frames.Count; index++)
        {
            if (frames[index].ProtoCode == protoCode && frames[index].UnitySessionId == unitySessionId)
            {
                return true;
            }
        }
        return false;
    }
}
