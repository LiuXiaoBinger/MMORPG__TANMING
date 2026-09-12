using System;
using System.Threading;

/// <summary>
/// 服务端进程内的 64 位 UID 生成工具。
/// 位布局为：类型 3 位 | 自定义纪元毫秒 40 位 | 逻辑服实例 ID 8 位 | 同毫秒序列 12 位。
/// </summary>
public static class UIDHelper
{
    private const int TypeBits = 3;
    private const int TimestampBits = 40;
    private const int ServerInstanceBits = 8;
    private const int SequenceBits = 12;

    private const int ServerInstanceShift = SequenceBits;
    private const int TimestampShift = ServerInstanceBits + SequenceBits;
    private const int TypeShift = TimestampBits + ServerInstanceBits + SequenceBits;

    private const long TypeMask = (1L << TypeBits) - 1L;
    private const long TimestampMask = (1L << TimestampBits) - 1L;
    private const long ServerInstanceMask = (1L << ServerInstanceBits) - 1L;
    private const long SequenceMask = (1L << SequenceBits) - 1L;

    private static readonly object SyncRoot = new object();
    private static readonly DateTime CustomEpochUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static bool _isServerInstanceConfigured;
    private static bool _hasGeneratedUid;
    private static int _serverInstanceId;
    private static long _lastTimestampMilliseconds = -1L;
    private static int _sequence;

    /// <summary>
    /// 配置当前逻辑服的实例 ID，取值范围为 0 到 255。
    /// </summary>
    public static void ConfigureServerInstanceId(int serverInstanceId)
    {
        if (serverInstanceId < 0 || serverInstanceId > ServerInstanceMask)
        {
            throw new ArgumentOutOfRangeException(nameof(serverInstanceId), "逻辑服实例 ID 必须在 0 到 255 之间。");
        }

        lock (SyncRoot)
        {
            if (_hasGeneratedUid && _serverInstanceId != serverInstanceId)
            {
                throw new InvalidOperationException("当前进程已生成 UID，禁止切换逻辑服实例 ID 以避免重复。");
            }

            _serverInstanceId = serverInstanceId;
            _isServerInstanceConfigured = true;
        }
    }

    /// <summary>
    /// 获取 UID 使用的自定义纪元（UTC）。
    /// </summary>
    public static DateTime EpochUtc
    {
        get { return CustomEpochUtc; }
    }

    /// <summary>
    /// 生成一个正数 64 位 UID。
    /// </summary>
    public static long CreateUID(UIDType type)
    {
        if (!IsCreatableUIDType(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "UID 类型必须是已定义的业务类型。");
        }

        int typeValue = (int)type;
        lock (SyncRoot)
        {
            if (!_isServerInstanceConfigured)
            {
                throw new InvalidOperationException("生成 UID 前必须先配置逻辑服实例 ID。");
            }

            long timestampMilliseconds = GetCurrentTimestampMilliseconds();
            if (timestampMilliseconds < _lastTimestampMilliseconds)
            {
                // 系统时钟回拨时固定使用逻辑时钟，保证本进程内 UID 单调且不重复。
                timestampMilliseconds = _lastTimestampMilliseconds;
            }

            if (timestampMilliseconds == 0L)
            {
                // Item 类型编码为 0，纪元起始毫秒也为 0 时需推进一毫秒，确保结果始终为正数。
                timestampMilliseconds = 1L;
            }

            if (timestampMilliseconds == _lastTimestampMilliseconds)
            {
                if (_sequence >= SequenceMask)
                {
                    // 同一毫秒已用完 4096 个序列号，至少等待一个毫秒后推进逻辑时钟。
                    timestampMilliseconds = WaitNextTimestampMilliseconds(_lastTimestampMilliseconds);
                    _sequence = 0;
                }
                else
                {
                    _sequence++;
                }
            }
            else
            {
                _sequence = 0;
            }

            if (timestampMilliseconds > TimestampMask)
            {
                throw new InvalidOperationException("UID 时间位已耗尽，需要更换 UID 纪元或位布局。");
            }

            _lastTimestampMilliseconds = timestampMilliseconds;
            _hasGeneratedUid = true;

            // 最高符号位未参与编码，返回值始终是正数 long。
            return ((long)typeValue << TypeShift)
                   | (timestampMilliseconds << TimestampShift)
                   | ((long)_serverInstanceId << ServerInstanceShift)
                   | (uint)_sequence;
        }
    }

    private static bool IsCreatableUIDType(UIDType type)
    {
        // UID_Max 是类型位容量边界，不是实际业务类型；未定义的保留值同样禁止生成。
        switch (type)
        {
            case UIDType.UID_Item:
            case UIDType.UID_Shop:
            case UIDType.UID_Reward:
            case UIDType.UID_Pet:
            case UIDType.UID_Mail:
            case UIDType.UID_Common:
                return true;
            default:
                return false;
        }
    }

    private static long GetCurrentTimestampMilliseconds()
    {
        long timestampMilliseconds = (DateTime.UtcNow.Ticks - CustomEpochUtc.Ticks) / TimeSpan.TicksPerMillisecond;
        if (timestampMilliseconds < 0L)
        {
            throw new InvalidOperationException("当前时间早于 UID 自定义纪元。");
        }

        return timestampMilliseconds;
    }

    private static long WaitNextTimestampMilliseconds(long lastTimestampMilliseconds)
    {
        Thread.Sleep(1);
        long timestampMilliseconds = GetCurrentTimestampMilliseconds();

        // 时钟仍未前进时使用下一逻辑毫秒，避免长时间回拨导致服务阻塞或产生重复 UID。
        return timestampMilliseconds > lastTimestampMilliseconds
            ? timestampMilliseconds
            : lastTimestampMilliseconds + 1L;
    }
}
