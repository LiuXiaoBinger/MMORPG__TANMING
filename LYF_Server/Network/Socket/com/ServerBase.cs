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

    //缓冲区
    private byte[] _buffer = new byte[1024 * 4];

    protected Socket _socket;

    //连接状态
    protected ConnState _connState;

    public NetClient _client;

    /// <summary>
    /// 开始接收服务端发来的数据
    /// </summary>
    protected void BeginReceive()
    {
        _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceiveCB, null);
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
            //返回的字节数
            int len = _socket.EndReceive(ar);
            if (len > 0)
            {

                while (true)
                {

                    ushort msgLen = BitConverter.ToUInt16(_buffer, 0);//无符号16位整数，范围从0-65535
                    if (len >= msgLen + 2)
                    {
                        //拿到了解析后的最终数据。
                        byte[] data = NetUtils.Instance.ParseData(_buffer, msgLen);
                        if (data != null)
                        {
                            BasePackage basePackage = BasePackage.Parser.ParseFrom(data);
                            Console.WriteLine("basePackage::" + basePackage.ToString());
                            HandleCommand(basePackage);
                        }

                        len -= (msgLen + 2);
                        //如果len还大于0 ，则发生了粘包
                        if (len > 0)
                        {
                            Buffer.BlockCopy(_buffer, msgLen + 2, _buffer, 0, len);
                        }

                    }
                    else
                    {
                        break;
                    }

                }
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
            LogMsg.Info(ex.Message);
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
        catch (Exception ex) { Console.WriteLine(ex.Message); }
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
