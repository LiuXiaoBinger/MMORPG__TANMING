using System;
using System.Threading;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 客户端网络管理器（对齐 RO 的 MNetClient）：负责连接生命周期、协议发送与主线程消息派发。
/// 业务统一经 Send 上行；TCP 与 KCP 两路下行汇入同一个 OnRecv 后再进入业务分发。
/// </summary>
public class MNetClient : Singleton<MNetClient>
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

    /// <summary>
    /// 业务公共上行入口（对齐 RO 的 MNetClient::Send）：按协议号与载荷交给当前连接。
    /// 实时协议是否改走 KCP 由连接层按协议号分流，调用方不需要感知通道。
    /// 需要把发送固定到某个已捕获连接与代次的业务（例如商城），继续使用自己捕获的连接发送，不受本入口影响。
    /// </summary>
    /// <param name="protoCode">协议号。</param>
    /// <param name="data">协议载荷字节串。</param>
    /// <returns>本地提交结果：已交给通道返回 true，无连接或写出失败返回 false；不代表服务端业务处理完成。</returns>
    public bool Send(int protoCode, ByteString data)
    {
        // 当前 TCP 连接，为空说明尚未连接或已断开。
        NetClient client = _client;
        if (client == null)
        {
            return false;
        }
        return client.Send(protoCode, data);
    }

    /// <summary>获取当前有效连接代次。</summary>
    public long CurrentConnectionGeneration
    {
        get { return Volatile.Read(ref _activeConnectionGeneration); }
    }

    /// <summary>底层 Socket 是否已经收到连接成功回调；连接代次分配阶段仍为 false。</summary>
    private int _isConnected;

    /// <summary>当前连接的主机地址，UDP 握手复用同一主机，避免另配地址。</summary>
    private string _serverHost;

    /// <summary>当前连接的可靠 KCP 通道；为空表示实时协议没有可用通道，此时实时上行直接丢弃。</summary>
    private KcpClient _realtimeClient;

    /// <summary>本次连接是否已发起绑定请求，避免重复握手。</summary>
    private bool _realtimeBindRequested;

    /// <summary>本次绑定使用的会话号；客户端放弃绑定时据此回传，供网关清理残留 Peer。</summary>
    private uint _realtimeBindConv;

    /// <summary>获取底层 TCP 连接状态，不代表登录业务已经完成。</summary>
    public bool IsConnected
    {
        get { return Volatile.Read(ref _isConnected) == 1; }
    }

    /// <summary>
    /// 当前实时通道是否已经就绪（网关启用确认到达且本端已切换）。移动与技能只走 KCP，
    /// 未就绪时业务不得开始，用于确认本地主角能否创建模型并上报实时协议。
    /// </summary>
    public bool IsRealtimeReady
    {
        get
        {
            // 本地缓存当前通道，避免两次读取之间被断线流程替换。
            KcpClient realtimeClient = _realtimeClient;
            if (realtimeClient == null)
            {
                return false;
            }
            return realtimeClient.IsReady;
        }
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
        // UDP 握手与 TCP 使用同一主机，端口由服务端绑定响应下发。
        _serverHost = host;
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

    /// <summary>
    /// TCP 下行接入：连接层已拆帧并反序列化，这里补上收包时刻的连接代次后汇入公共 OnRecv。
    /// </summary>
    /// <param name="sourceClient">消息所属的 TCP 连接。</param>
    /// <param name="protoCode">协议号。</param>
    /// <param name="data">协议载荷字节串。</param>
    private void OnReceiveMsgHandle(NetClient sourceClient, int protoCode, ByteString data)
    {
        // 收到该包时所属的连接代次，用于隔离排队期间发生的断线重连。
        long receiveGeneration = CurrentConnectionGeneration;
        OnRecv(sourceClient, receiveGeneration, protoCode, data, false);
    }

    /// <summary>
    /// 协议接收公共入口（对齐 RO 的 MNetClient 主线程派发）：TCP 与 KCP 两路下行在此汇合，
    /// 统一完成归属校验、主线程投递与业务分发，不再各留一份实际派发逻辑。
    /// </summary>
    /// <param name="sourceClient">消息所属的 TCP 连接，注册回调时固定。</param>
    /// <param name="sourceGeneration">消息所属的连接代次，注册回调时固定。</param>
    /// <param name="protoCode">协议号。</param>
    /// <param name="data">协议载荷字节串。</param>
    /// <param name="fromRealtime">是否来自 KCP 实时通道；决定沿用哪一路既有归属校验，以及绑定控制包是否由本路消费。</param>
    private void OnRecv(NetClient sourceClient, long sourceGeneration, int protoCode, ByteString data,
        bool fromRealtime)
    {
        // 只保留 TCP 原来源检查；KCP 路的归属检查仍在解析前与主线程执行前各一次。
        if (!fromRealtime)
        {
            // TCP 原来源检查：旧连接的迟到回调不能进入业务层。
            if (!ReferenceEquals(_client, sourceClient))
            {
                return;
            }
            if (sourceGeneration <= 0)
            {
                return;
            }
        }

        PostToMainThread(() =>
        {
            if (fromRealtime)
            {
                // KCP 主线程内再次按通道归属过滤：排队期间断线或重连时丢弃旧包。
                if (!IsRealtimeOwner(sourceClient, sourceGeneration))
                {
                    return;
                }
            }
            else
            {
                // TCP 主线程内再次按来源与代次过滤：排队期间断线或重连时不投递。
                if (!ReferenceEquals(_client, sourceClient))
                {
                    return;
                }
                if (sourceGeneration != CurrentConnectionGeneration)
                {
                    return;
                }
            }
            // 绑定控制包只由 TCP 原路径消费，KCP 下行不处理绑定响应。
            if (!fromRealtime && protoCode == NetDefine.CMD_KcpBindCode)
            {
                HandleKcpBindResponse(data);
                return;
            }
            SocketDispatcher.Instance.DispatchEvent(protoCode, data, sourceGeneration);
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
            // 只有仍属于失败代次的通道才释放：自动重连可能已建立新通道，旧回调不得误关新通道。
            ReleaseRealtimeClientOfGeneration(failedGeneration);
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
        // 先释放 UDP 通道，旧会话的实时数据不能落到后续连接。
        ReleaseRealtimeClient();
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

    /// <summary>
    /// 请求把移动与技能实时协议绑定到 KCP 通道；在本地主角创建模型前调用。
    /// 实时协议只走 KCP，绑定失败按现有断线流程恢复，不保持 TCP 继续游戏。
    /// </summary>
    public void RequestRealtimeBind()
    {
        if (!IsConnected)
        {
            return;
        }
        if (_realtimeBindRequested)
        {
            return;
        }
        // 本地缓存当前 TCP 连接，为空说明未连接，直接不发绑定请求。
        NetClient client = _client;
        if (client == null)
        {
            return;
        }

        _realtimeBindRequested = true;
        // 绑定请求只带协议号，参数由服务端生成的会话号和令牌决定。
        client.Send(NetDefine.CMD_KcpBindCode, ByteString.Empty);
    }

    /// <summary>
    /// 处理绑定响应：拿到参数后启动客户端 KCP，只有网关经 TCP 确认启用才算就绪。
    /// 任何失败都清理通道并沿用既有断线恢复，不允许带着 TCP 继续游戏。
    /// </summary>
    /// <param name="data">绑定响应载荷，固定 25 字节。</param>
    private void HandleKcpBindResponse(ByteString data)
    {
        if (data == null)
        {
            return;
        }
        // 解码出的绑定响应载荷，含结果码、conv、令牌与 UDP 端口。
        KcpBindPayload payload = KcpBindPayload.Decode(data.ToByteArray());
        if (payload == null)
        {
            Debug.LogWarning("KCP 绑定响应长度不合法，按绑定失败处理。");
            FailRealtimeBind();
            return;
        }
        // 当前 TCP 连接作为失败恢复的入口，后续释放与断开都以它为准。
        NetClient client = _client;
        if (client == null)
        {
            return;
        }
        if (payload.Result == KcpBindResult.Enabled)
        {
            // 网关经可靠 TCP 确认通道已启用，这是客户端唯一的切换依据。
            KcpClient enabledClient = _realtimeClient;
            if (enabledClient == null)
            {
                FailRealtimeBind();
                return;
            }
            if (!enabledClient.ConfirmEnabled(payload.Conv))
            {
                Debug.LogWarning("KCP 启用确认与当前绑定不匹配，按绑定失败处理。");
                FailRealtimeBind();
            }
            return;
        }
        if (payload.Result != KcpBindResult.Granted)
        {
            // 服务端明确拒绝（未启用、参数错误等）：实时协议没有可用通道，直接走断线恢复。
            Debug.LogWarning("KCP 绑定被拒绝，结果=" + payload.Result + "，按现有断线流程恢复。");
            FailRealtimeBind();
            return;
        }
        if (_realtimeClient != null)
        {
            return;
        }

        // 注册回调时固定所属连接与连接代次：旧通道回调即使排到重连之后，也不能借用新代次进入业务层。
        NetClient bindClient = client;
        // 本次绑定所属的连接代次，回调里用于识别旧连接的过期通知。
        long bindGeneration = CurrentConnectionGeneration;
        // 本次新建的客户端 KCP 通道，负责绑定握手、重传与实时协议收发。
        KcpClient realtimeClient = new KcpClient();
        // 记录通道归属代次：旧连接的失败回调据此判断是否仍应释放本通道。
        realtimeClient.OwnerGeneration = bindGeneration;
        realtimeClient.OnPackageReceived = message => HandleRealtimePackage(bindClient, bindGeneration, message);
        realtimeClient.OnBindFailed = () => HandleRealtimeBindFailed(bindClient, bindGeneration);
        realtimeClient.OnFailed = () => HandleRealtimeFailed(bindClient, bindGeneration);
        if (!realtimeClient.Start(_serverHost, payload.UdpPort, payload.Conv, payload.Token))
        {
            Debug.LogWarning("KCP 客户端启动失败，按现有断线流程恢复。");
            FailRealtimeBind();
            return;
        }

        _realtimeBindConv = payload.Conv;
        _realtimeClient = realtimeClient;
        client.RealtimeClient = realtimeClient;
    }

    /// <summary>
    /// 初次绑定失败：本次 TCP 会话确定没有可用实时通道，清理本地通道、通知网关释放残留 Peer，
    /// 再走既有断线恢复重建连接。绑定完成前不会创建本地主角，角色因此不可操作。
    /// </summary>
    /// <param name="sourceClient">发起绑定的连接。</param>
    /// <param name="sourceGeneration">发起绑定时的连接代次。</param>
    private void HandleRealtimeBindFailed(NetClient sourceClient, long sourceGeneration)
    {
        PostToMainThread(() =>
        {
            if (!IsRealtimeOwner(sourceClient, sourceGeneration))
            {
                return;
            }
            Debug.LogWarning("KCP 初次绑定未完成，按现有断线流程恢复。");
            FailRealtimeBind();
        });
    }

    /// <summary>
    /// 绑定失败的统一收尾：释放本端通道，经可靠 TCP 回传放弃结果，再触发现有断线恢复。
    /// 可重复调用：通道已释放时只跳过回传放弃通知，但只要当前连接仍在，仍会再次断开该连接，
    /// 因此重复调用表现为“继续走断线恢复”，不是空操作。
    /// </summary>
    private void FailRealtimeBind()
    {
        // 当前 TCP 连接，为重连入口；为空说明连接已断开，无需再次触发。
        NetClient client = _client;
        // 本次被放弃的会话号，随放弃结果回传给网关用于清理。
        uint abandonedConv = _realtimeBindConv;
        // 仅当本端确实持有通道时才需要回传放弃通知，避免未分配会话号时误发。
        bool hasChannel = _realtimeClient != null || abandonedConv != 0u;
        ReleaseRealtimeClient();
        if (hasChannel)
        {
            SendRealtimeBindAbandoned(client, abandonedConv);
        }
        if (client == null)
        {
            return;
        }
        // 走底层既有断线恢复：断开后由 NetClient 的自动重连与新代次重新建链。
        client.Disconnect();
    }

    /// <summary>
    /// 经可靠 TCP 告知网关本次绑定已放弃；载荷只带结果码与会话号，不携带令牌。
    /// </summary>
    /// <param name="sourceClient">发送放弃通知的 TCP 连接。</param>
    /// <param name="conv">被放弃的会话号，为零表示没有实际发起过绑定。</param>
    private void SendRealtimeBindAbandoned(NetClient sourceClient, uint conv)
    {
        if (sourceClient == null)
        {
            return;
        }
        if (conv == 0u)
        {
            return;
        }
        // 放弃通知载荷：只填结果码与会话号，令牌保持全零。
        KcpBindPayload payload = new KcpBindPayload();
        payload.Result = KcpBindResult.Abandoned;
        payload.Conv = conv;
        // 放弃通知不带密钥，保持全零。
        payload.Token = new byte[KcpDefine.BindTokenBytes];
        sourceClient.Send(NetDefine.CMD_KcpBindCode, ByteString.CopyFrom(payload.Encode()));
    }

    /// <summary>
    /// 运行期通道失效：不把已发出的实时协议改投 TCP，直接复用既有 TCP 断线恢复流程。
    /// </summary>
    /// <param name="sourceClient">通道所属连接。</param>
    /// <param name="sourceGeneration">通道所属连接代次。</param>
    private void HandleRealtimeFailed(NetClient sourceClient, long sourceGeneration)
    {
        PostToMainThread(() =>
        {
            if (!IsRealtimeOwner(sourceClient, sourceGeneration))
            {
                return;
            }
            Debug.LogWarning("KCP 通道运行期失效，按现有断线流程恢复。");
            // 先释放本端通道，避免关闭回调再次进入断线流程。
            ReleaseRealtimeClient();
            // 走底层既有断线恢复：连接断开后由 NetClient 的自动重连与新代次重新建链。
            sourceClient.Disconnect();
        });
    }

    /// <summary>
    /// 判断回调是否仍属于当前连接与当前代次；旧通道或旧连接的回调一律忽略。
    /// </summary>
    /// <param name="sourceClient">回调登记时的连接。</param>
    /// <param name="sourceGeneration">回调登记时的连接代次。</param>
    /// <returns>仍属于当前连接且代次一致时返回 true。</returns>
    private bool IsRealtimeOwner(NetClient sourceClient, long sourceGeneration)
    {
        if (!ReferenceEquals(_client, sourceClient))
        {
            return false;
        }
        if (sourceGeneration <= 0)
        {
            return false;
        }
        return sourceGeneration == CurrentConnectionGeneration;
    }

    /// <summary>
    /// KCP 下行薄接入：只做“载荷字节 → BasePackage”这一段 KCP 专属解析，
    /// 解析成功后即汇入公共 OnRecv，主线程投递与业务分发由后者统一完成。
    /// </summary>
    /// <param name="sourceClient">通道所属连接，注册时固定。</param>
    /// <param name="sourceGeneration">通道所属连接代次，注册时固定。</param>
    /// <param name="message">BasePackage 序列化字节。</param>
    private void HandleRealtimePackage(NetClient sourceClient, long sourceGeneration, byte[] message)
    {
        if (message == null)
        {
            return;
        }
        // 解析前的第一次归属检查：旧通道的包不解析。
        if (!IsRealtimeOwner(sourceClient, sourceGeneration))
        {
            return;
        }

        // 待解析出的下行协议包，解析成功后交给公共派发入口。
        BasePackage basePackage;
        try
        {
            basePackage = BasePackage.Parser.ParseFrom(message);
        }
        catch (InvalidProtocolBufferException exception)
        {
            // 反序列化失败的异常对象，仅记录其消息，不影响通道。
            // 单条坏包只丢弃，不断开已认证的可靠通道。
            Debug.LogWarning("KCP 下行包解析失败：" + exception.Message);
            return;
        }

        // 解析出的协议号与载荷，随公共入口一起完成主线程派发。
        int protoCode = basePackage.ProtoCode;
        ByteString data = basePackage.Data;
        OnRecv(sourceClient, sourceGeneration, protoCode, data, true);
    }

    /// <summary>
    /// 仅当通道仍属于失败代次时释放。自动重连可能已建立新代次的新通道，
    /// 旧连接的失败回调若直接释放，会把正在使用的新 KCP 关掉。
    /// </summary>
    /// <param name="failedGeneration">失败回调携带的连接代次。</param>
    private void ReleaseRealtimeClientOfGeneration(long failedGeneration)
    {
        // 本地缓存当前通道，为空说明已无通道可释放。
        KcpClient realtimeClient = _realtimeClient;
        if (realtimeClient == null)
        {
            return;
        }
        if (!realtimeClient.ShouldReleaseOnFailure(failedGeneration))
        {
            // 旧代次的迟到回调只记录，不触碰当前通道。
            Debug.Log("忽略旧连接失败回调的通道释放请求，失败代次=" + failedGeneration);
            return;
        }
        ReleaseRealtimeClient();
    }

    /// <summary>释放当前连接的 KCP 通道，TCP 断开或重连时调用。</summary>
    private void ReleaseRealtimeClient()
    {
        // 待关闭的当前通道引用，清空字段后在末尾关闭。
        KcpClient realtimeClient = _realtimeClient;
        _realtimeClient = null;
        _realtimeBindRequested = false;
        _realtimeBindConv = 0u;
        if (_client != null)
        {
            _client.RealtimeClient = null;
        }
        if (realtimeClient != null)
        {
            realtimeClient.Close();
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
