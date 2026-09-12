using System;
using System.Threading;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 客户端网络管理器，负责连接生命周期和主线程消息派发。
/// </summary>
public class NetSocketMgr : Singleton<NetSocketMgr>
{
    /// <summary>Unity 主线程同步上下文。</summary>
    private SynchronizationContext _synchronizationContext;

    /// <summary>当前网络客户端。</summary>
    private static NetClient _client;

    /// <summary>单调递增的连接代次种子。</summary>
    private long _connectionGenerationSeed;

    /// <summary>当前有效连接代次，零表示没有有效连接。</summary>
    private long _activeConnectionGeneration;

    /// <summary>获取当前网络客户端。</summary>
    public static NetClient Client
    {
        get { return _client; }
    }

    /// <summary>获取当前有效连接代次。</summary>
    public long CurrentConnectionGeneration
    {
        get { return Volatile.Read(ref _activeConnectionGeneration); }
    }

    /// <summary>初始化网络模块并连接登录服务器。</summary>
    public void Init()
    {
        _synchronizationContext = SynchronizationContext.Current;
        if (_synchronizationContext == null)
        {
            Debug.LogError("网络模块必须在 Unity 主线程初始化。");
            return;
        }

        // 商城登录数据可能在主城控制器创建前返回，因此随网络模块提前注册。
        ShopMgr.Instance.Init();
        NetErrorMsgMgr.Instance.Init();
        ConnectServer(NetDefine.IPHost, NetDefine.LoginServerPort);
    }

    /// <summary>切换到指定服务器，并为新连接分配独立代次。</summary>
    public void ConnectServer(string host, int port, Action connSucced = null,
        Action connFail = null)
    {
        Disconnect();

        long connectionGeneration = Interlocked.Increment(ref _connectionGenerationSeed);
        NetClient client = new NetClient(host, port, ClientType.Unity);
        _client = client;
        Volatile.Write(ref _activeConnectionGeneration, connectionGeneration);

        client.OnReceiveMsg += (protoCode, data) =>
            OnReceiveMsgHandle(client, protoCode, data);
        client.OnConnSucceed += () =>
            OnConnectionSucceeded(client, connSucced);
        client.OnConnFailed += () =>
            OnConnectionFailed(client, connFail);
        client.StartConnect();
    }

    /// <summary>把当前连接收到的消息投递到 Unity 主线程。</summary>
    private void OnReceiveMsgHandle(NetClient sourceClient, int protoCode, ByteString data)
    {
        if (!ReferenceEquals(_client, sourceClient))
        {
            return;
        }

        long receiveGeneration = CurrentConnectionGeneration;
        if (receiveGeneration <= 0)
        {
            return;
        }

        PostToMainThread(() =>
        {
            // 消息排队期间发生断线或重连时，旧连接回调不能进入业务层。
            if (!ReferenceEquals(_client, sourceClient) ||
                receiveGeneration != CurrentConnectionGeneration)
            {
                return;
            }
            SocketDispatcher.Instance.DispatchEvent(protoCode, data, receiveGeneration);
        });
    }

    /// <summary>处理首次连接或底层自动重连成功。</summary>
    private void OnConnectionSucceeded(NetClient sourceClient, Action connSucced)
    {
        if (!ReferenceEquals(_client, sourceClient))
        {
            return;
        }

        long connectionGeneration = CurrentConnectionGeneration;
        if (connectionGeneration <= 0)
        {
            // 同一个 NetClient 自动重连后使用新代次，隔离断线前已排队的回调。
            connectionGeneration = Interlocked.Increment(ref _connectionGenerationSeed);
            Volatile.Write(ref _activeConnectionGeneration, connectionGeneration);
        }

        long callbackGeneration = connectionGeneration;
        PostToMainThread(() =>
        {
            if (!ReferenceEquals(_client, sourceClient) ||
                callbackGeneration != CurrentConnectionGeneration)
            {
                return;
            }
            if (connSucced != null)
            {
                connSucced.Invoke();
            }
        });
    }

    /// <summary>失效当前连接代次，并清理该代次的业务等待状态。</summary>
    private void OnConnectionFailed(NetClient sourceClient, Action connFail)
    {
        if (!ReferenceEquals(_client, sourceClient))
        {
            return;
        }

        long failedGeneration = Interlocked.Exchange(ref _activeConnectionGeneration, 0);
        if (failedGeneration <= 0)
        {
            return;
        }
        PostToMainThread(() =>
        {
            ShopMgr.Instance.HandleDisconnected(failedGeneration);
            if (!ReferenceEquals(_client, sourceClient) ||
                CurrentConnectionGeneration != 0)
            {
                return;
            }
            ClearRoleWorld();
            if (connFail != null)
            {
                connFail.Invoke();
            }
        });
    }

    /// <summary>断开当前客户端，并立即让当前连接代次失效。</summary>
    public void Disconnect()
    {
        NetClient disconnectedClient = _client;
        long disconnectedGeneration = Interlocked.Exchange(ref _activeConnectionGeneration, 0);
        _client = null;

        ShopMgr.Instance.HandleDisconnected(disconnectedGeneration);
        ClearRoleWorld();
        if (disconnectedClient != null)
        {
            // 先移除当前引用，底层断开回调便不会误清理后续新连接状态。
            disconnectedClient._isNeedReconn = false;
            disconnectedClient.Disconnect();
        }
    }

    /// <summary>在 Unity 主线程执行网络生命周期或消息回调。</summary>
    private void PostToMainThread(Action action)
    {
        if (action == null)
        {
            return;
        }
        if (_synchronizationContext == null)
        {
            return;
        }
        _synchronizationContext.Post(_ => action.Invoke(), null);
    }

    /// <summary>清理当前连接对应的客户端角色运行时数据。</summary>
    private static void ClearRoleWorld()
    {
        if (Global.Instance == null || Global.Instance.RoleWorld == null)
        {
            return;
        }
        Global.Instance.RoleWorld.Clear();
        Global.Instance.roleCtrlBase = null;
    }
}
