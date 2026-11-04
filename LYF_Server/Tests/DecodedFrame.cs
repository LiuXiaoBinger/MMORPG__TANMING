/// <summary>从回环连接解码出的一帧下行协议：协议号与目标 Unity 会话号。</summary>
public sealed class DecodedFrame
{
    /// <summary>协议号，对应 NetDefine 中的 CMD_* 常量。</summary>
    public int ProtoCode;

    /// <summary>协议包携带的目标 Unity 会话号，用于区分同一条网关连接上的不同客户端。</summary>
    public int UnitySessionId;

    /// <summary>通过正式 CRC 和解压后的协议包，可继续验证实际业务字段。</summary>
    public BasePackage Package;
}
