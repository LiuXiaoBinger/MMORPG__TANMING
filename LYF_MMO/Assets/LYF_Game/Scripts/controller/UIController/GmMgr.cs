using System;
using Google.Protobuf;

/// <summary>对应 RO 的 GmMgr，负责发送命令以及解析和分发 GM RPC 回执。</summary>
internal sealed class GmMgr
{
    /// <summary>当前主视图中用于回显游戏服结果的入口。</summary>
    private readonly Action<string> showGMInfo;

    /// <summary>在主界面控制器的生命周期内监听同号 RPC 回包。</summary>
    /// <param name="showGMInfo">把执行结果送到 GM 面板的回调。</param>
    internal GmMgr(Action<string> showGMInfo)
    {
        this.showGMInfo = showGMInfo;
        SocketDispatcher.Instance.AddEventHandler(NetDefine.RpcC2G_GSLocalGMCmd, OnGmRpcReceived);
    }

    /// <summary>只提交原始命令字符串，角色身份由登录会话决定。</summary>
    /// <param name="msg">已经在面板中确认的命令。</param>
    internal bool SendCommand(string msg)
    {
        return MNetClient.Instance.Send(NetDefine.RpcC2G_GSLocalGMCmd, new GSLocalGMCmdArg { Args = msg }.ToByteString());
    }

    /// <summary>直接解析游戏服返回的文字并交给控制器。</summary>
    /// <param name="msg">本次 RPC 的原始回包内容。</param>
    private void OnGmRpcReceived(ByteString msg)
    {
        showGMInfo(GSLocalGMCmdRes.Parser.ParseFrom(msg).ResultMsg);
    }

    /// <summary>主界面释放时解除本实例的协议监听。</summary>
    internal void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.RpcC2G_GSLocalGMCmd, OnGmRpcReceived);
    }
}
