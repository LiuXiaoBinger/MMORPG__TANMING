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

    /// <summary>正在建立连接的待定代次；连接成功前不对业务暴露。</summary>
    private long _pendingConnectionGeneration;

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

    /// <summary>底层 Socket 是否已经收到连接成功回调；连接代次分配阶段仍为 false。</summary>
    private int _isConnected;

    /// <summary>获取底层 TCP 连接状态，不代表登录业务已经完成。</summary>
    public bool IsConnected
    {
        get { return Volatile.Read(ref _isConnected) == 1; }
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
        Volatile.Write(ref _isConnected, 0);

        long connectionGeneration = Interlocked.Increment(ref _connectionGenerationSeed);
        NetClient client = new NetClient(host, port, ClientType.Unity);
        _client = client;
        Volatile.Write(ref _pendingConnectionGeneration, connectionGeneration);
        Volatile.Write(ref _activeConnectionGeneration, 0);

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

        long connectionGeneration = Volatile.Read(ref _pendingConnectionGeneration);
        if (connectionGeneration <= 0)
        {
            // 同一个 NetClient 自动重连后使用新代次，隔离断线前已排队的回调。
            connectionGeneration = Interlocked.Increment(ref _connectionGenerationSeed);
        }

        // 连接成功后才把待定代次提升为业务可见的有效代次。
        Volatile.Write(ref _activeConnectionGeneration, connectionGeneration);
        Volatile.Write(ref _pendingConnectionGeneration, 0);
        // 只有收到连接成功回调后才标记已连接，避免连接尝试阶段被当成有效会话。
        Volatile.Write(ref _isConnected, 1);

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
        Volatile.Write(ref _pendingConnectionGeneration, 0);
        Volatile.Write(ref _isConnected, 0);
        if (failedGeneration <= 0)
        {
            // 连接尚未成功时不触发商城和角色会话清理，但失败回调仍必须回到 Unity 主线程执行。
            PostToMainThread(() =>
            {
                if (!ReferenceEquals(_client, sourceClient))
                {
                    return;
                }
                // 失败回调排队期间若已有新连接成功，忽略这次过期的初次连接失败通知。
                if (CurrentConnectionGeneration != 0)
                {
                    return;
                }
                if (connFail != null)
                {
                    connFail.Invoke();
                }
            });
            return;
        }
        PostToMainThread(() =>
        {
            ShopMgr.Instance.HandleDisconnected(failedGeneration);
            if (!ReferenceEquals(_client, sourceClient))
            {
                return;
            }
            // 即使自动重连已经建立新代次，也要定向清理仍属于失败代次的旧角色会话。
            ClearSceneWorld(failedGeneration);
            if (CurrentConnectionGeneration != 0)
            {
                return;
            }
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
        Volatile.Write(ref _pendingConnectionGeneration, 0);
        Volatile.Write(ref _isConnected, 0);
        _client = null;

        ShopMgr.Instance.HandleDisconnected(disconnectedGeneration);
        ClearSceneWorld();
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
    private static void ClearSceneWorld()
    {
        // 当前全局生命周期实例同时拥有待加载角色和已创建场景角色，断线时必须一起清理。
        Global global = Global.Instance;
        if (global == null)
        {
            return;
        }
        global.ClearLocalRoleSession();
    }

    /// <summary>仅清理由指定失败连接代次拥有的客户端角色会话。</summary>
    /// <param name="failedConnectionGeneration">底层连接失败时失效的正数代次。</param>
    private static void ClearSceneWorld(long failedConnectionGeneration)
    {
        // 当前全局实例会核对角色会话代次，旧失败回调不会删除已由新连接接管的角色。
        Global global = Global.Instance;
        if (global == null)
        {
            return;
        }
        global.ClearLocalRoleSession(failedConnectionGeneration);
    }
}
