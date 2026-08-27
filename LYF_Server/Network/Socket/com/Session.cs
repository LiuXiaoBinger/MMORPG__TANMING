using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


public class Session : ServerBase
{
    public int SessionID{get;set;}
    public Session(Dictionary<int, IContainer> cmdDic, NetClient client)
    {
        _client = client;
        _cmdDic = cmdDic;
        SessionMgr.Instance.AddSession(this);
    }

    /// <summary>
    /// 开始接收客户端发来的数据
    /// </summary>
    /// <param name="socket"></param>
    public void ReceiveData(Socket socket)
    {
        
        _socket = socket;
        BeginReceive();

    }


    protected override void HandleCommand(BasePackage basePackage)
    {

        IContainer container = _cmdDic[basePackage.ProtoCode];
        if (container == null)
        {
            LogMsg.Info("command not regist..");
            return;
        }

        if (_client != null)
        {
            if (_client._clientType == ClientType.LoginServer||_client._clientType == ClientType.GateServer)
            {
                basePackage.UnitySessionId = SessionID;
            }

            if (_client._clientType == ClientType.GameServer)
            {
                basePackage.GateSessionId = SessionID;
            }
        }
        container.OnServerCommand(this, basePackage);
    }

    public void DisConnect()
    {
        LogMsg.Info("Disconnect"+_socket.RemoteEndPoint+"断开了连接..");
        base.Disconnect();
       
    }



}
