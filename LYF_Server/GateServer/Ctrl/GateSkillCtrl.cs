/// <summary>网关技能协议入口，只转发施法请求与服务器权威表现通知。</summary>
public sealed class GateSkillCtrl : IContainer
{
    /// <summary>技能网关入口没有需要预加载的状态。</summary>
    public void OnInit()
    {
    }

    /// <summary>将客户端施法请求透传到 GameServer，保留网关与 Unity 会话。</summary>
    /// <param name="serverBase">收到 Unity 请求的网关传输对象。</param>
    /// <param name="basePackage">携带原会话标识和技能请求载荷的协议包。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage)
    {
        if (serverBase == null || serverBase._client == null || basePackage == null)
        {
            return;
        }

        serverBase._client.Send(basePackage);
    }

    /// <summary>校验并向原 Unity 会话转发权威施法结果与起止通知。</summary>
    /// <param name="serverBase">收到 GameServer 回包的网关传输对象。</param>
    /// <param name="basePackage">携带目标 Unity 会话标识的权威回包。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
    {
        if (basePackage == null)
        {
            return;
        }

        // Unity 会话仅用于寻址，结果码与 protobuf 格式统一在转发器中校验。
        Session unitySession = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        GateClientResponseForwarder.ForwardToUnity(unitySession, basePackage);
    }
}
