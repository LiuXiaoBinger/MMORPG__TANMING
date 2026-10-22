using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// 连接基类（对齐 RO 的 BaseLink）：对外只保留两个协议公共入口——发送用 Send、接收用 OnRecv。
/// TCP 按 2 字节长度帧拆包与 KCP 重组后的完整消息都进入同一处解析派发，负责 ProtoBuf 反序列化与发送封包。
/// </summary>
public class BaseLink
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
            // 通信异常对应的套接字错误码，用于日志定位；非套接字异常退回通用 HResult。
            SocketException se = ex as SocketException;
            if (se != null)
            {
                LogMsg.Info($"[BaseLink] OnReceiveCB failed, socketErrorCode={(int)se.SocketErrorCode}", LogMsgType.Error);
            }
            else
            {
                LogMsg.Info($"[BaseLink] OnReceiveCB failed, socketErrorCode={ex.HResult}", LogMsgType.Error);
            }
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

            // 交由 NetUtils 校验帧头、CRC 并解压出的协议包字节；为空说明本帧无效，跳过。
            byte[] data = NetUtils.Instance.ParseData(_buffer, messageLength, parseOffset);
            if (data != null)
            {
                // TCP 与 KCP 共用同一解析派发入口；TCP 不改写包内原有会话字段。
                OnRecv(data);
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

    /// <summary>
    /// 协议接收公共入口（对齐 RO 的 BaseLink::OnRecv）：把一条完整消息反序列化后交给 HandleCommand。
    /// TCP 拆帧与 KCP 重组都汇入此处，同一条消息只解析一次。
    /// </summary>
    /// <param name="payload">BasePackage 的原始序列化字节。</param>
    /// <param name="resetGateSessionId">
    /// 是否清空包内由对端自填的网关会话号。KCP 上行传 true，沿用原 KCP 专属的包身份清理；
    /// TCP 默认 false，内网已有封包的字段一律不由本入口改写。
    /// </param>
    public void OnRecv(byte[] payload, bool resetGateSessionId = false)
    {
        // 由协议字节反序列化出的业务封包。
        BasePackage basePackage = BasePackage.Parser.ParseFrom(payload);
        if (resetGateSessionId)
        {
            // 客户端自填的会话标识不可信，清空后由 HandleCommand 按当前绑定会话补入。
            basePackage.GateSessionId = 0;
        }
        HandleCommand(basePackage);
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
    /// 发送一条协议包（对齐 RO 的 BaseLink::Send）。先按实时分流决定是否交给 KCP，
    /// 未接管时按原单次 TCP 帧写出，不增加确认、重试或发送队列。
    /// </summary>
    /// <param name="basePackage">待发送协议包。</param>
    /// <param name="protoCode">非 -1 时覆盖封包协议号。</param>
    /// <param name="data">非空时覆盖封包载荷。</param>
    /// <returns>本地提交结果：KCP 已接管或 TCP 帧已提交写出返回 true，实时协议被丢弃、无可用 Socket 或发送异常返回 false；不代表帧已完整写出，也不代表对端已收到或业务处理完成。</returns>
    public bool Send(BasePackage basePackage, int protoCode = -1, ByteString data = null)
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
                return true;
            }
            if (realtimeResult == RealtimeSendResult.Dropped)
            {
                // 曾经启用的实时通道已失效：本条丢弃，禁止改投 TCP 造成重复执行或跨通道乱序。
                LogMsg.Info("[BaseLink] 实时通道已失效，丢弃实时协议，protoCode=" +
                    basePackage.ProtoCode, LogMsgType.Warn);
                return false;
            }

            // 加协议头后的 TCP 发送帧，交给 Socket 写出。
            byte[] frame = NetUtils.Instance.MakeData(payload);
            lock (_sendSync)
            {
                // 关闭与写入可能并发，取锁后重新确认 Socket 仍可用。
                Socket sendSocket = _socket;
                if (sendSocket == null)
                {
                    return false;
                }
                sendSocket.Send(frame);
            }
            return true;
        }
        catch (Exception ex)
        {
            // 识别出的 SocketException 记录系统错误码，其余异常记录 HResult。
            SocketException se = ex as SocketException;
            if (se != null)
            {
                LogMsg.Info($"[BaseLink] Send failed, socketErrorCode={(int)se.SocketErrorCode}", LogMsgType.Error);
            }
            else
            {
                LogMsg.Info($"[BaseLink] Send failed, socketErrorCode={ex.HResult}", LogMsgType.Error);
            }
            return false;
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


    /// <summary>
    /// 按协议号与载荷发送，封包不携带显式会话字段。
    /// </summary>
    /// <param name="protoCode">协议号。</param>
    /// <param name="data">协议载荷。</param>
    /// <returns>本地提交结果，语义同 Send(BasePackage,...)。</returns>
    public bool Send(int protoCode = -1, ByteString data = null)
    {
        // 本重载不带目标会话字段，新建空封包只填协议号与载荷。
        BasePackage basePackage = new BasePackage();
        return Send(basePackage, protoCode, data);
    }

    /// <summary>
    /// 指定目标会话后按协议号与载荷发送，供网关向指定 Unity 会话转发下行。
    /// </summary>
    /// <param name="unitySessionID">目标 Unity 会话号，写入封包 UnitySessionId。</param>
    /// <param name="protoCode">协议号。</param>
    /// <param name="data">协议载荷。</param>
    /// <returns>本地提交结果，语义同 Send(BasePackage,...)。</returns>
    public bool Send( int unitySessionID,int protoCode = -1, ByteString data = null)
    {
        // 新建空封包并写入目标会话号，使下游能定位到指定的 Unity 连接。
        BasePackage basePackage = new BasePackage();
        basePackage.UnitySessionId = unitySessionID;
        return Send(basePackage, protoCode, data);
    }

    /// <summary>
    /// 按原协议包回一条错误码，沿用项目既有错误响应格式。
    /// </summary>
    /// <param name="basePackage">请求对应的原协议包，用于确定回包目标。</param>
    /// <param name="cmdCode">出错的命令码。</param>
    public void SendError(BasePackage basePackage, CmdCode cmdCode)
    {

        // 错误响应载荷，只带命令码，沿用项目既有错误回包格式。
        ErrMsg errMsg = new ErrMsg()
        {
            CmdCode = cmdCode,
        };
        Send(basePackage, NetDefine.CMD_ErrCode, errMsg.ToByteString());
    }


}
