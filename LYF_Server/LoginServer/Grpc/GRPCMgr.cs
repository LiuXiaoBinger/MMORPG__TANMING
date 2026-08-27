using Grpc.Core;
using Message;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
    /// gRPC 客户端管理器。
    /// </summary>
    public class GRPCMgr : Singleton<GRPCMgr>
    {
        private Channel _channel;

        public VarifyService.VarifyServiceClient VarifyClient { get; private set; }

        public bool IsInitialized
        {
            get { return _channel != null && VarifyClient != null; }
        }

        /// <summary>
        /// 初始化验证码服务客户端。
        /// </summary>
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
        /// 请求邮箱验证码。
        /// </summary>
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
