using System;
using System.Security.Cryptography;

/// <summary>
/// KCP 数据报认证：使用绑定令牌作密钥的 HMAC-SHA256 截断认证码。
/// CRC16 与 conv 不承担认证职责，只有认证通过的报文才允许进入 KCP。
/// </summary>
public static class KcpAuth
{
    /// <summary>
    /// 计算数据报认证码。
    /// </summary>
    /// <param name="token">该会话绑定时下发的 16 字节随机令牌，作为 HMAC 密钥。</param>
    /// <param name="header">不含认证码的头部区段（版本、类型、conv、载荷长度）。</param>
    /// <param name="headerOffset">头部在缓冲区中的起始下标。</param>
    /// <param name="headerLength">头部字节数。</param>
    /// <param name="payload">KCP 或绑定载荷。</param>
    /// <param name="payloadOffset">载荷起始下标。</param>
    /// <param name="payloadLength">载荷字节数。</param>
    /// <returns>16 字节截断认证码；参数非法时返回 null。</returns>
    public static byte[] Compute(byte[] token, byte[] header, int headerOffset, int headerLength,
        byte[] payload, int payloadOffset, int payloadLength)
    {
        if (token == null || header == null)
        {
            return null;
        }
        if (token.Length != KcpDefine.BindTokenBytes)
        {
            return null;
        }
        // 认证范围覆盖头部与载荷，任一字节被改动都会导致校验失败。
        int signedLength = headerLength + payloadLength;
        if (signedLength < 0)
        {
            return null;
        }

        // 拼接后的待认证字节：头部区段在前，载荷紧随其后，作为 HMAC 的唯一输入。
        byte[] signedBytes = new byte[signedLength];
        Buffer.BlockCopy(header, headerOffset, signedBytes, 0, headerLength);
        if (payloadLength > 0)
        {
            Buffer.BlockCopy(payload, payloadOffset, signedBytes, headerLength, payloadLength);
        }

        // HMAC-SHA256 实例，使用绑定令牌作密钥，用完即释放非托管资源。
        using (HMACSHA256 hmac = new HMACSHA256(token))
        {
            // 未截断的完整 HMAC-SHA256 摘要，取其前 16 字节作为报文认证码。
            byte[] fullMac = hmac.ComputeHash(signedBytes);
            // 截断后的 16 字节认证码，写入报文尾部并供对端复核。
            byte[] truncated = new byte[KcpDefine.MacBytes];
            Buffer.BlockCopy(fullMac, 0, truncated, 0, KcpDefine.MacBytes);
            return truncated;
        }
    }

    /// <summary>
    /// 以固定时间比较认证码，避免按字节提前返回泄露时间差。
    /// </summary>
    /// <param name="expected">本地计算的认证码。</param>
    /// <param name="actual">报文携带的认证码。</param>
    /// <param name="actualOffset">报文认证码在接收缓冲区中的起始下标。</param>
    /// <returns>长度一致且全部字节相同时返回 true。</returns>
    public static bool EqualsFixedTime(byte[] expected, byte[] actual, int actualOffset)
    {
        if (expected == null || actual == null)
        {
            return false;
        }
        if (expected.Length != KcpDefine.MacBytes)
        {
            return false;
        }
        if (actual.Length - actualOffset < KcpDefine.MacBytes)
        {
            return false;
        }

        // 差异累积后一次比较，循环中不提前返回。
        int difference = 0;
        for (int index = 0; index < KcpDefine.MacBytes; index++)
        {
            difference |= expected[index] ^ actual[actualOffset + index];
        }
        return difference == 0;
    }

    /// <summary>
    /// 生成密码学安全的随机字节。
    /// </summary>
    /// <param name="length">需要的随机字节数。</param>
    /// <returns>随机字节数组；不写入日志以确保令牌不外泄。</returns>
    public static byte[] CreateRandomBytes(int length)
    {
        // 接收随机填充结果的字节数组，长度由调用方指定。
        byte[] buffer = new byte[length];
        // 密码学安全随机数生成器，用于填充令牌与 conv，用完释放。
        using (RNGCryptoServiceProvider random = new RNGCryptoServiceProvider())
        {
            random.GetBytes(buffer);
        }
        return buffer;
    }

    /// <summary>
    /// 生成非零随机会话号，零值保留为未绑定标记。
    /// </summary>
    /// <returns>可用于 conv 的随机无符号整数。</returns>
    public static uint CreateRandomConv()
    {
        // 4 字节随机源，按小端解释为无符号整数前先取原始字节。
        byte[] random = CreateRandomBytes(4);
        // 由 4 字节随机源解释出的候选 conv 值。
        uint value = BitConverter.ToUInt32(random, 0);
        // conv 为零表示未绑定，随机到零时改用固定非零值。
        if (value == 0u)
        {
            return 1u;
        }
        return value;
    }
}
