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
public class NetClient : ServerBase
{

    /// <summary>对端主机地址。</summary>
    private string _host;
    /// <summary>对端端口。</summary>
    private int _port;

    /// <summary>断线后的重连定时器。</summary>
    private Timer _reconnectTimer;
    /// <summary>是否需要自动重连。</summary>
    public bool _isNeedReconn = true;

    /// <summary>连接服务端成功回调。</summary>
    public Action OnConnSucceed;
    /// <summary>连接服务端失败回调。</summary>
    public Action OnConnFailed;
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
    /// 连接服务端成功
    /// </summary>
    /// <param name="ar"></param>
    /// <exception cref="NotImplementedException"></exception>
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
    /// 注册指令
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="container"></param>
    public void RegistCommand(int cmd, IContainer container)
    {
        _cmdDic.Add(cmd, container);
    }


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

    /// <summary>
    /// 断开连接
    /// </summary>
    public override void Disconnect()
    {
        OnConnFailed?.Invoke();
        SetReconnectTimer();
        base.Disconnect();
    }


    /// <summary>
    /// 设置重连时间
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
    /// <param name="state"></param>
    private void ReConn(object state)
    {
        if (_isNeedReconn)
        {
            StartConnect();
        }
    }
}
