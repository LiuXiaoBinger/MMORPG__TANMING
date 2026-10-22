using System;

/// <summary>
/// KCP 外层数据报编解码：版本、类型、conv、载荷长度、认证码与载荷。
/// 头部固定 28 字节，认证码覆盖版本到载荷的全部内容。
/// </summary>
public static class KcpDatagram
{
    /// <summary>认证码在缓冲区中的固定起点；紧随 12 字节可变头部之后。</summary>
    private const int MacOffset = 12;

    /// <summary>被认证的可变头部长度：版本 1 + 类型 1 + conv 4 + 载荷长度 2。</summary>
    private const int SignedHeaderLength = 8;

    /// <summary>
    /// 把一条数据报写入目标缓冲区，并在写入后补齐认证码。
    /// </summary>
    /// <param name="buffer">目标缓冲区，长度不得小于数据报总长。</param>
    /// <param name="type">数据报类型，取值见 KcpDefine 的四种类型。</param>
    /// <param name="conv">会话号，仅用于网关查表，不承担身份认证。</param>
    /// <param name="token">会话令牌，作为 HMAC 密钥；绑定请求阶段使用待绑定令牌。</param>
    /// <param name="payload">KCP 段或绑定请求载荷，可为 null。</param>
    /// <param name="payloadOffset">载荷起始下标。</param>
    /// <param name="payloadLength">载荷字节数，含外层头后不得超 1200 字节。</param>
    /// <returns>写入的总字节数；参数越界返回 -1。</returns>
    public static int Write(byte[] buffer, byte type, uint conv, byte[] token,
        byte[] payload, int payloadOffset, int payloadLength)
    {
        if (buffer == null || token == null)
        {
            return -1;
        }
        if (payloadLength < 0)
        {
            return -1;
        }
        // 数据报总字节数：固定头部 28 字节 + 载荷长度。
        int totalLength = KcpDefine.DatagramHeaderBytes + payloadLength;
        if (buffer.Length < totalLength)
        {
            return -1;
        }
        if (totalLength > KcpDefine.MaxDatagramBytes)
        {
            return -1;
        }

        // 固定魔术字用于快速丢弃非本协议报文。
        Buffer.BlockCopy(BitConverter.GetBytes(KcpDefine.DatagramMagic), 0, buffer, 0, 4);
        buffer[4] = KcpDefine.DatagramVersion;
        buffer[5] = type;
        Buffer.BlockCopy(BitConverter.GetBytes(conv), 0, buffer, 6, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)payloadLength), 0, buffer, 10, 2);

        if (payloadLength > 0 && payload != null)
        {
            Buffer.BlockCopy(payload, payloadOffset, buffer, KcpDefine.DatagramHeaderBytes, payloadLength);
        }

        // 计算出的本数据报认证码，随后写回缓冲区第 12 字节起的 16 字节位置。
        byte[] mac = KcpAuth.Compute(token, buffer, 4, SignedHeaderLength,
            buffer, KcpDefine.DatagramHeaderBytes, payloadLength);
        if (mac == null)
        {
            return -1;
        }
        Buffer.BlockCopy(mac, 0, buffer, MacOffset, KcpDefine.MacBytes);
        return totalLength;
    }

    /// <summary>
    /// 校验魔术字、版本与载荷长度，并输出头部字段。
    /// 本方法不验证认证码，调用方拿到令牌后必须再调用 VerifyMac。
    /// </summary>
    /// <param name="buffer">接收缓冲区。</param>
    /// <param name="length">本次收到的实际字节数。</param>
    /// <param name="type">输出数据报类型。</param>
    /// <param name="conv">输出会话号。</param>
    /// <param name="payloadOffset">输出载荷起始下标。</param>
    /// <param name="payloadLength">输出载荷字节数。</param>
    /// <returns>结构合法时返回 true。</returns>
    public static bool TryReadHeader(byte[] buffer, int length, out byte type, out uint conv,
        out int payloadOffset, out int payloadLength)
    {
        type = 0;
        conv = 0u;
        payloadOffset = 0;
        payloadLength = 0;
        if (buffer == null)
        {
            return false;
        }
        if (length < KcpDefine.DatagramHeaderBytes)
        {
            return false;
        }
        // 报文前 4 字节的魔术字，用于快速识别非本协议数据并丢弃。
        uint magic = BitConverter.ToUInt32(buffer, 0);
        if (magic != KcpDefine.DatagramMagic)
        {
            return false;
        }
        if (buffer[4] != KcpDefine.DatagramVersion)
        {
            return false;
        }

        type = buffer[5];
        conv = BitConverter.ToUInt32(buffer, 6);
        ushort declaredLength = BitConverter.ToUInt16(buffer, 10);
        // 声明的载荷长度必须与实际收到长度一致，避免截断或拼接包。
        if (declaredLength != length - KcpDefine.DatagramHeaderBytes)
        {
            return false;
        }
        payloadOffset = KcpDefine.DatagramHeaderBytes;
        payloadLength = declaredLength;
        return true;
    }

    /// <summary>
    /// 校验数据报认证码。
    /// </summary>
    /// <param name="buffer">接收缓冲区，与 TryReadHeader 使用同一份数据。</param>
    /// <param name="payloadLength">TryReadHeader 输出的载荷长度。</param>
    /// <param name="token">该会话的 16 字节令牌。</param>
    /// <returns>认证码一致时返回 true。</returns>
    public static bool VerifyMac(byte[] buffer, int payloadLength, byte[] token)
    {
        if (buffer == null || token == null)
        {
            return false;
        }
        // 本地按同一令牌重算的认证码，用于与报文携带值做固定时间比较。
        byte[] expected = KcpAuth.Compute(token, buffer, 4, SignedHeaderLength,
            buffer, KcpDefine.DatagramHeaderBytes, payloadLength);
        return KcpAuth.EqualsFixedTime(expected, buffer, MacOffset);
    }
}
