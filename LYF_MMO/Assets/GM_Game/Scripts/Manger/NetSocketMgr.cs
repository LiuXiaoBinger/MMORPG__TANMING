using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using UnityEngine;
/**
* Title:网络模块管理类
* Descrpiton:
*/

public class NetSocketMgr : Singleton<NetSocketMgr>
{
   SynchronizationContext synchronizationContextcontext;
   
   private static NetClient _client;

   public static NetClient Client
   {
      get
      {
         return _client;
      }
   }

   public void Init()
   {
      synchronizationContextcontext  = SynchronizationContext.Current;
      
      ConnectServer(NetDefine.IPHost,NetDefine.LoginServerPort);
      NetErrorMsgMgr.Instance.Init();
   }

   public void ConnectServer(string host, int port,Action connSucced = null,Action connFail = null)
   {
      Disconnect();
      
      _client = new NetClient(host, port,ClientType.Unity);
      _client.OnReceiveMsg += OnReceiveMsgHandle;
      if (connSucced != null)
      {
         _client.OnConnSucceed += connSucced;
      }

      if (connFail != null)
      {
         _client.OnConnFailed += connFail;
      }
      _client.StartConnect();
   }

   private void OnReceiveMsgHandle(int protoCode, ByteString data)
   {
      synchronizationContextcontext.Post(_ =>
      {
         SocketDispatcher.Instance.DispatchEvent(protoCode, data);
      }, null);
      
   }

   public void Disconnect()
   {
      if (_client != null)
      {
         _client._isNeedReconn =false;
         _client.Disconnect();
         _client = null;
      }
   }
}
