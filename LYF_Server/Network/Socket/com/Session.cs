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
    private readonly Action<int> _onDisconnected;

    public Session(Dictionary<int, IContainer> cmdDic, NetClient client, Action<int> onDisconnected = null)
    {
        _client = client;
        _cmdDic = cmdDic;
        _onDisconnected = onDisconnected;
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

        IContainer container;
        if (!_cmdDic.TryGetValue(basePackage.ProtoCode, out container) || container == null)
        {
            // 未注册协议只记录并丢弃，不能因为单个未知包中断客户端连接。
            LogMsg.Info("command not regist, protoCode=" + basePackage.ProtoCode, LogMsgType.Warn);
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

    public override void Disconnect()
    {
        LogMsg.Info("客户端会话断开，SessionID=" + SessionID);
        base.Disconnect();
        // 仅通知宿主，不在 Socket 线程直接修改角色状态；宿主应投递到逻辑线程。
        _onDisconnected?.Invoke(SessionID);
    }

    /// <summary>兼容历史调用名称，统一进入可重写的断开流程。</summary>
    public void DisConnect() { Disconnect(); }



}
