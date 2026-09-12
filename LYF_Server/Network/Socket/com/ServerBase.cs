using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


public class ServerBase
{
    //客户端类型
    public ClientType _clientType;

    public Action<int, ByteString> OnReceiveMsg;

    //指令集
    protected Dictionary<int, IContainer> _cmdDic = new Dictionary<int, IContainer>();

    // TCP 单次接收可能只有 4KB，保留半包并支持协议允许的最大帧。
    private const int FrameHeaderSize = 2;
    private const int MaxFrameLength = ushort.MaxValue;
    private readonly byte[] _buffer = new byte[FrameHeaderSize + MaxFrameLength];
    private int _receiveLength;

    protected Socket _socket;

    //连接状态
    protected ConnState _connState;

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

    protected virtual void HandleCommand(BasePackage basePackage)
    {

    }

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

            LogMsg.Info($"{_socket.RemoteEndPoint}发送了数据::{basePackage.ToString()}");

            _socket.Send(NetUtils.Instance.MakeData(basePackage.ToByteArray()));
        }
        catch (Exception ex) { SocketException se = ex as SocketException; LogMsg.Info($"[ServerBase] SendData failed, socketErrorCode={(se != null ? (int)se.SocketErrorCode : ex.HResult)}", LogMsgType.Error); }
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
