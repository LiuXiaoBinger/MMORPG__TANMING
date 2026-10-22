using System;

/// <summary>
/// TCP 绑定响应载荷：会话号、令牌、UDP 端口与结果码。
/// 序列化为固定 25 字节，随 BasePackage 承载，不新增 protobuf 结构。
/// </summary>
public sealed class KcpBindPayload
{
    /// <summary>载荷固定字节数：结果码 1 + conv 4 + 令牌 16 + 端口 4。</summary>
    public const int PayloadBytes = 1 + 4 + KcpDefine.BindTokenBytes + 4;

    /// <summary>绑定结果码，决定客户端是否继续 UDP 握手。</summary>
    public KcpBindResult Result;

    /// <summary>网关分配的会话号，仅用于数据报查表。</summary>
    public uint Conv;

    /// <summary>
    /// 网关下发的 16 字节随机令牌，同时作为数据报认证密钥。
    /// 只在 TCP 绑定响应中出现；Enabled 确认与 UDP 数据报都不携带该密钥本身。
    /// </summary>
    public byte[] Token;

    /// <summary>网关 UDP 监听端口，供客户端发起绑定握手。</summary>
    public int UdpPort;

    /// <summary>
    /// 编码为字节数组。
    /// </summary>
    /// <returns>固定 25 字节载荷。</returns>
    public byte[] Encode()
    {
        // 固定长度的编码缓冲区，依次写入结果码、conv、令牌与端口。
        byte[] buffer = new byte[PayloadBytes];
        buffer[0] = (byte)Result;
        Buffer.BlockCopy(BitConverter.GetBytes(Conv), 0, buffer, 1, 4);
        // 令牌缺失时按全零写出，接收方按拒绝处理，不抛异常。
        if (Token != null)
        {
            // 实际拷贝字节数：令牌不足 16 字节时按真实长度截断，避免越界。
            int copyLength = KcpDefine.BindTokenBytes;
            if (Token.Length < copyLength)
            {
                copyLength = Token.Length;
            }
            Buffer.BlockCopy(Token, 0, buffer, 5, copyLength);
        }
        Buffer.BlockCopy(BitConverter.GetBytes(UdpPort), 0, buffer, 5 + KcpDefine.BindTokenBytes, 4);
        return buffer;
    }

    /// <summary>
    /// 从字节数组解码。
    /// </summary>
    /// <param name="data">长度为 PayloadBytes 的载荷。</param>
    /// <returns>解码结果；长度不足返回 null。</returns>
    public static KcpBindPayload Decode(byte[] data)
    {
        if (data == null)
        {
            return null;
        }
        if (data.Length < PayloadBytes)
        {
            return null;
        }

        // 待填充的解码结果对象，逐字段从字节数组还原。
        KcpBindPayload payload = new KcpBindPayload();
        payload.Result = (KcpBindResult)data[0];
        payload.Conv = BitConverter.ToUInt32(data, 1);
        payload.Token = new byte[KcpDefine.BindTokenBytes];
        Buffer.BlockCopy(data, 5, payload.Token, 0, KcpDefine.BindTokenBytes);
        payload.UdpPort = BitConverter.ToInt32(data, 5 + KcpDefine.BindTokenBytes);
        return payload;
    }
}
