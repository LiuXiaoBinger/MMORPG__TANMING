using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// TCP 连接基类：统一负责异步接收、按 2 字节长度帧拆包、ProtoBuf 反序列化与发送封包。
/// </summary>
public class ServerBase
{
    /// <summary>本连接的客户端类型，决定消息如何分发。</summary>
    public ClientType _clientType;

    /// <summary>Unity 类型连接收到消息时的回调：参数为协议号与消息体字节串。</summary>
    public Action<int, ByteString> OnReceiveMsg;

    /// <summary>协议号到命令处理器的映射。</summary>
    protected Dictionary<int, IContainer> _cmdDic = new Dictionary<int, IContainer>();

    // TCP 单次接收可能只有 4KB，保留半包并支持协议允许的最大帧。
    private const int FrameHeaderSize = 2;
    private const int MaxFrameLength = ushort.MaxValue;
    /// <summary>接收累积缓存，长度为帧头 + 最大帧。</summary>
    private readonly byte[] _buffer = new byte[FrameHeaderSize + MaxFrameLength];
    /// <summary>缓存中当前已累积的字节数。</summary>
    private int _receiveLength;

    /// <summary>底层 Socket。</summary>
    protected Socket _socket;

    /// <summary>
    /// TCP 单帧写入锁。业务回包、转发线程与 UDP 绑定线程都可能对同一连接写帧，
    /// 并发 Send 会交错截断帧，必须串行；锁内只做一次完整帧写入。
    /// </summary>
    private readonly object _sendSync = new object();

    /// <summary>连接状态。</summary>
    protected ConnState _connState;

    /// <summary>本连接所属的上游连接器引用，可为 null。</summary>
    public NetClient _client;

    /// <summary>
    /// 开始接收服务端发来的数据
    /// </summary>
    protected void BeginReceive()
    {
        if (_socket == null)
        {
            return;
        }

        int availableLength = _buffer.Length - _receiveLength;
        if (availableLength <= 0)
        {
            Disconnect();
            LogMsg.Info("网络接收缓存已满，丢弃异常连接", LogMsgType.Error);
            return;
        }

        _socket.BeginReceive(_buffer, _receiveLength, availableLength, SocketFlags.None, OnReceiveCB, null);
    }

    /// <summary>
    /// 处理服务端发来的数据
    /// </summary>
    /// <param name="ar"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnReceiveCB(IAsyncResult ar)
    {
        try
        {
            int receivedLength = _socket.EndReceive(ar);
            if (receivedLength > 0)
            {
                _receiveLength += receivedLength;
                ProcessReceiveBuffer();
                BeginReceive();
            }
            else
            {
                Disconnect();
                LogMsg.Info("OnReceiveCB收到数据小于等于0");
            }
        }
        catch (Exception ex)
        {
            Disconnect();
            SocketException se = ex as SocketException;
            LogMsg.Info($"[ServerBase] OnReceiveCB failed, socketErrorCode={(se != null ? (int)se.SocketErrorCode : ex.HResult)}", LogMsgType.Error);
        }
    }

    /// <summary>从累积缓存中提取完整帧，半包保留到下一次 Socket 接收。</summary>
    private void ProcessReceiveBuffer()
    {
        int parseOffset = 0;
        while (_receiveLength - parseOffset >= FrameHeaderSize)
        {
            ushort messageLength = BitConverter.ToUInt16(_buffer, parseOffset);
            if (messageLength < 3)
            {
                throw new InvalidOperationException("网络消息长度小于协议头和校验字段。");
            }

            int frameLength = FrameHeaderSize + messageLength;
            if (_receiveLength - parseOffset < frameLength)
            {
                break;
            }

            byte[] data = NetUtils.Instance.ParseData(_buffer, messageLength, parseOffset);
            if (data != null)
            {
                BasePackage basePackage = BasePackage.Parser.ParseFrom(data);
                HandleCommand(basePackage);
            }
            parseOffset += frameLength;
        }

        if (parseOffset > 0)
        {
            int remainingLength = _receiveLength - parseOffset;
            if (remainingLength > 0)
            {
                Buffer.BlockCopy(_buffer, parseOffset, _buffer, 0, remainingLength);
            }
            _receiveLength = remainingLength;
        }
    }

    /// <summary>子类实现：处理一个已拆包的协议包。</summary>
    protected virtual void HandleCommand(BasePackage basePackage)
    {

    }

    /// <summary>断开连接并清理 Socket 与缓存。</summary>
    public virtual void Disconnect()
    {

        _connState = ConnState.Disconnected;

        if (_socket != null)
        {
            _socket.Close();
            _socket = null;
        }

        _receiveLength = 0;

    }


    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data"></param>

    public void SendData(BasePackage basePackage, int protoCode = -1, ByteString data = null)
    {
        try
        {
            if (protoCode != -1)
            {
                basePackage.ProtoCode = protoCode;
            }

            if (data != null)
            {
                basePackage.Data = data;
            }

            LogMsg.Info($"{_socket.RemoteEndPoint}发送了数据::{DescribeSend(basePackage)}");

            // 协议包序列化字节，同时供实时通道承载与 TCP 帧封装。
            byte[] payload = basePackage.ToByteArray();
            // 实时协议在通道就绪时改走 KCP，其余业务保持原 TCP 帧格式不变。
            RealtimeSendResult realtimeResult = TrySendRealtime(basePackage, payload);
            if (realtimeResult == RealtimeSendResult.Sent)
            {
                return;
            }
            if (realtimeResult == RealtimeSendResult.Dropped)
            {
                // 曾经启用的实时通道已失效：本条丢弃，禁止改投 TCP 造成重复执行或跨通道乱序。
                LogMsg.Info("[ServerBase] 实时通道已失效，丢弃实时协议，protoCode=" +
                    basePackage.ProtoCode, LogMsgType.Warn);
                return;
            }

            // 加协议头后的 TCP 发送帧，交给 Socket 写出。
            byte[] frame = NetUtils.Instance.MakeData(payload);
            lock (_sendSync)
            {
                // 关闭与写入可能并发，取锁后重新确认 Socket 仍可用。
                Socket sendSocket = _socket;
                if (sendSocket == null)
                {
                    return;
                }
                sendSocket.Send(frame);
            }
        }
        catch (Exception ex)
        {
            // 识别出的 SocketException 记录系统错误码，其余异常记录 HResult。
            SocketException se = ex as SocketException;
            LogMsg.Info($"[ServerBase] SendData failed, socketErrorCode={(se != null ? (int)se.SocketErrorCode : ex.HResult)}", LogMsgType.Error);
        }
    }

    /// <summary>
    /// 传输分流钩子：子类在具备实时通道时接管发送。
    /// 返回值必须区分“未启用”与“启用后失效”，后者不允许回退 TCP。
    /// </summary>
    /// <param name="basePackage">已填好协议号与载荷的协议包。</param>
    /// <param name="payload">BasePackage 序列化字节，供 KCP 直接承载。</param>
    /// <returns>实时通道发送结果；默认未接管，沿用 TCP。</returns>
    protected virtual RealtimeSendResult TrySendRealtime(BasePackage basePackage, byte[] payload)
    {
        return RealtimeSendResult.KeepTcp;
    }

    /// <summary>
    /// 生成发送日志正文。绑定协议携带认证密钥，日志只记录结果码与会话号，绝不输出载荷内容。
    /// </summary>
    /// <param name="basePackage">待发送的协议包。</param>
    /// <returns>可安全写入日志的描述文本。</returns>
    private static string DescribeSend(BasePackage basePackage)
    {
        if (basePackage.ProtoCode != NetDefine.CMD_KcpBindCode)
        {
            return basePackage.ToString();
        }
        if (basePackage.Data == null)
        {
            return "KCP 绑定响应::无载荷，令牌已省略";
        }
        // 由绑定响应数据解码出的载荷，用于只记录结果码与会话号。
        KcpBindPayload payload = KcpBindPayload.Decode(basePackage.Data.ToByteArray());
        if (payload == null)
        {
            return "KCP 绑定响应::载荷长度不合法，令牌已省略";
        }
        // conv 只是查表用的会话号，不是凭据；令牌与整体载荷一律不写日志。
        return "KCP 绑定响应::result=" + (byte)payload.Result + " conv=" + payload.Conv +
            " 令牌已省略";
    }


    public void SendData(int protoCode = -1, ByteString data = null)
    {
        BasePackage basePackage = new BasePackage();
        SendData(basePackage, protoCode, data);
    }
    public void SendData( int unitySessionID,int protoCode = -1, ByteString data = null)
    {
        BasePackage basePackage = new BasePackage();
        basePackage.UnitySessionId = unitySessionID;
        SendData(basePackage, protoCode, data);
    }
    public void SendError(BasePackage basePackage, CmdCode cmdCode)
    {

        ErrMsg errMsg = new ErrMsg()
        {
            CmdCode = cmdCode,
        };
        SendData(basePackage, NetDefine.CMD_ErrCode, errMsg.ToByteString());
    }


}
