using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// TCP 主动连接器：服务端之间互联（登录/网关/中心/游戏）时使用，支持异步连接与定时自动重连。
/// </summary>
public class NetClient : BaseLink
{

    #region 字段与属性
    /// <summary>对端主机地址。</summary>
    private string _host;

    /// <summary>对端端口。</summary>
    private int _port;

    /// <summary>断线后的重连定时器。</summary>
    private Timer _reconnectTimer;

    /// <summary>是否需要自动重连。</summary>
    public bool _isNeedReconn = true;

    /// <summary>Unity 连接的实时通道；仅客户端类型为 Unity 且绑定成功时非空。绑定前为空，实时协议不使用 TCP 兜底。</summary>
    private KcpClient _realtimeClient;

    /// <summary>当前实时通道引用；为空表示没有可用的 KCP 通道，实时协议只能丢弃。</summary>
    public KcpClient RealtimeClient
    {
        get { return _realtimeClient; }
        set { _realtimeClient = value; }
    }

    /// <summary>连接服务端成功回调。</summary>
    public Action OnConnSucceed;

    /// <summary>连接服务端失败回调。</summary>
    public Action OnConnFailed;
    #endregion

    /// <summary>构造客户端连接器。</summary>
    /// <param name="ip">对端主机地址。</param>
    /// <param name="port">对端端口。</param>
    /// <param name="clientType">本连接的客户端类型，决定消息分发方式。</param>
    public NetClient(string ip, int port, ClientType clientType)
    {
        _host = ip;
        _port = port;
        _clientType = clientType;
        _connState = ConnState.Disconnected;
    }

    #region 事件回调
    /// <summary>
    /// 连接服务端成功
    /// </summary>
    /// <param name="ar"></param>
    /// 连接完成、成功回调或启动接收出现异常时调用 Disconnect。
    private void OnConnectCB(IAsyncResult ar)
    {
        try
        {
            _socket.EndConnect(ar);
            _connState = ConnState.Connected;
            OnConnSucceed?.Invoke();
            LogMsg.Info($"连接服务端成功:{_socket.RemoteEndPoint}");
            //开始接收服务端发来的数据
            BeginReceive();
        }
        catch (Exception ex)
        {
            Disconnect();
            LogMsg.Info("[NetClient] socket connect failed, socketErrorCode=" + GetSocketErrorCode(ex), LogMsgType.Error);
        }
    }

    /// <summary>Unity 类型连接直接通知消息回调；服务之间的主动连接按协议号找到处理器并调用 OnClientCommand。</summary>
    protected override void HandleCommand(BasePackage basePackage)
    {

        if (_clientType == ClientType.Unity)
        {
            OnReceiveMsg?.Invoke(basePackage.ProtoCode, basePackage.Data);
            return;
        }

        IContainer container;
        if (!_cmdDic.TryGetValue(basePackage.ProtoCode, out container) || container == null)
        {
            // 未注册协议不能抛出字典异常，否则接收线程会误判为网络错误并主动断开连接。
            LogMsg.Info("command not regist, protoCode=" + basePackage.ProtoCode, LogMsgType.Warn);
            return;
        }

        container.OnClientCommand(this, basePackage);

    }
    #endregion

    #region 查询与判定
    /// <summary>获取连接异常对应的套接字错误码。</summary>
    private static int GetSocketErrorCode(Exception exception)
    {
        SocketException socketException = exception as SocketException;
        if (socketException != null)
        {
            return (int)socketException.SocketErrorCode;
        }

        return exception.HResult;
    }

    /// <summary>
    /// Unity 连接的实时分流：五类实时协议只允许经 KCP 上行，其余业务与原服务间连接保持 TCP。
    /// </summary>
    /// <param name="basePackage">待发送协议包。</param>
    /// <param name="payload">已序列化的协议包字节。</param>
    /// <returns>KCP 已接管返回 Sent；未就绪或写入失败一律返回 Dropped，不存在 TCP 兜底。</returns>
    protected override RealtimeSendResult TrySendRealtime(BasePackage basePackage, byte[] payload)
    {
        // 服务之间不得误用客户端 UDP 通道。
        if (_clientType != ClientType.Unity)
        {
            return RealtimeSendResult.KeepTcp;
        }
        if (!KcpDefine.IsRealtimeProtocol(basePackage.ProtoCode))
        {
            return RealtimeSendResult.KeepTcp;
        }
        // 本地缓存当前实时通道引用，为空表示尚未绑定、绑定失败或已释放。
        KcpClient realtimeClient = _realtimeClient;
        if (realtimeClient == null)
        {
            // 实时协议只走 KCP：通道不可用时本条丢弃，禁止改投 TCP 造成跨通道乱序或重复执行。
            LogMsg.Info("[NetClient] 实时通道未就绪，丢弃实时协议，protoCode=" +
                basePackage.ProtoCode, LogMsgType.Warn);
            return RealtimeSendResult.Dropped;
        }
        if (realtimeClient.Send(payload))
        {
            return RealtimeSendResult.Sent;
        }
        // 通道路由已建立但本次写入失败（绑定未完成或运行期失效），同样只能丢弃。
        LogMsg.Info("[NetClient] 实时通道写入失败，丢弃实时协议，protoCode=" +
            basePackage.ProtoCode, LogMsgType.Warn);
        return RealtimeSendResult.Dropped;
    }
    #endregion

    #region 核心逻辑
    /// <summary>
    /// 开始连接服务端
    /// </summary>
    public void StartConnect()
    {
        try
        {
            if (_connState != ConnState.Disconnected) { return; }

            if (_socket == null)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            //开始连接服务端
            _socket.BeginConnect(_host, _port, OnConnectCB, null);
        }
        catch (Exception ex)
        {
            Disconnect();
            LogMsg.Info("[NetClient] socket connect failed, socketErrorCode=" + GetSocketErrorCode(ex), LogMsgType.Error);
        }
    }

    /// <summary>
    /// 注册指令
    /// </summary>
    /// <param name="cmd">待接收的协议号，同号重复登记会由字典抛错。</param>
    /// <param name="container">该协议在主动连接端使用的处理器。</param>
    public void RegistCommand(int cmd, IContainer container)
    {
        _cmdDic.Add(cmd, container);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public override void Disconnect()
    {
        // 断开同时释放即时通道，旧绑定不能在新连接上继续发送。
        // 断开前缓存当前实时通道引用，清空字段后关闭旧通道。
        KcpClient realtimeClient = RealtimeClient;
        RealtimeClient = null;
        if (realtimeClient != null)
        {
            realtimeClient.Close();
        }
        OnConnFailed?.Invoke();
        SetReconnectTimer();
        base.Disconnect();
    }

    /// <summary>
    /// 断开后安排首次 3 秒、后续每 10 秒尝试重连；回调内部再检查是否允许重连。
    /// </summary>
    private void SetReconnectTimer()
    {
        if (_reconnectTimer == null)
        {
            _reconnectTimer = new Timer(ReConn);
        }
        _reconnectTimer.Change(3000, 10000);

    }

    /// <summary>
    /// 断开重连
    /// </summary>
    /// <param name="state">System.Threading.Timer 提供的状态对象，当前回调未使用。</param>
    private void ReConn(object state)
    {
        if (_isNeedReconn)
        {
            StartConnect();
        }
    }
    #endregion
}
