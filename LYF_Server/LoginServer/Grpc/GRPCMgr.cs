using Grpc.Core;
using Message;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
    /// 登录服调用独立邮箱验证码服务的客户端；负责创建连接、发起请求和关闭连接。
    /// </summary>
    public class GRPCMgr : Singleton<GRPCMgr>
    {
        /// <summary>到验证码服务的 gRPC 连接通道，初始化时创建，ShutdownAsync 时释放。</summary>
        private Channel _channel;

        /// <summary>基于通道创建的验证码 RPC 客户端。</summary>
        public VarifyService.VarifyServiceClient VarifyClient { get; private set; }

        /// <summary>通道和 RPC 客户端均已创建时为 true，不代表远端服务一定可达。</summary>
        public bool IsInitialized
        {
            get { return _channel != null && VarifyClient != null; }
        }

        /// <summary>
        /// 初始化验证码服务客户端；重复调用不会替换已有连接。
        /// </summary>
        /// <param name="host">验证码服务地址，连接前去掉首尾空白。</param>
        /// <param name="port">验证码服务监听端口，范围为 1 到 65535。</param>
        public void InitVarifyClient(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("gRPC host 不能为空", "host");
            }

            if (port <= 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException("port", "gRPC port 范围必须是 1-65535");
            }

            if (IsInitialized)
            {
                return;
            }

            _channel = new Channel(
                string.Format("{0}:{1}", host.Trim(), port),
                ChannelCredentials.Insecure);
            VarifyClient = new VarifyService.VarifyServiceClient(_channel);
        }

        /// <summary>
        /// 请求远端发送邮箱验证码，并等待 RPC 返回结果。
        /// </summary>
        /// <param name="email">接收验证码的邮箱。</param>
        /// <param name="deadline">可选的请求截止时间。</param>
        /// <param name="cancellationToken">调用方用来取消等待的令牌。</param>
        public async Task<GetVarifyRsp> GetVarifyCodeAsync(
            string email,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("请先调用 InitVarifyClient 初始化 gRPC 客户端");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("邮箱不能为空", "email");
            }

            // 去掉邮箱首尾空白后交给验证码服务，验证码生成与发送由远端完成。
            GetVarifyReq request = new GetVarifyReq
            {
                Email = email.Trim()
            };

            return await VarifyClient
                .GetVarifyCodeAsync(request, deadline: deadline, cancellationToken: cancellationToken)
                .ResponseAsync;
        }

        /// <summary>
        /// 关闭 gRPC 客户端连接。
        /// </summary>
        public async Task ShutdownAsync()
        {
            if (_channel == null)
            {
                return;
            }

            await _channel.ShutdownAsync();
            VarifyClient = null;
            _channel = null;
        }
    }
