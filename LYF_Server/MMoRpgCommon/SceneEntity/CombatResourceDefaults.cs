/// <summary>四项通用战斗资源的共享默认值与范围处理；只保存常量，不保存任何实体的真实余额。</summary>
/// <remarks>角色、普通怪与首领的怒气和真气初始值统一取自本类，避免各真实组件各写一份默认数字。</remarks>
public static class CombatResourceDefaults
{
    /// <summary>怒气上限默认点数。</summary>
    public const int DefaultRageMax = 100;
    /// <summary>角色与怪物出生时怒气默认点数，从零开始积累。</summary>
    public const int DefaultRage = 0;
    /// <summary>真气上限默认点数。</summary>
    public const int DefaultTrueQiMax = 100;
    /// <summary>角色与怪物出生时真气默认点数，初始为满。</summary>
    public const int DefaultTrueQi = 100;
    /// <summary>无既有蓝量配置怪物与首领的法力上限默认点数。</summary>
    public const int DefaultMonsterMpMax = 100;
    /// <summary>无既有蓝量配置怪物与首领的出生法力默认点数，初始为满。</summary>
    public const int DefaultMonsterMp = 100;

    /// <summary>把资源点裁切到 0 至上限；加法先提升到长整型，过大增量不会溢出。</summary>
    /// <param name="current">当前资源点。</param>
    /// <param name="max">该资源的上限点，负数按零处理。</param>
    /// <param name="delta">本次增量，负数为消耗。</param>
    /// <returns>裁切后的真实资源点。</returns>
    public static int ApplyDelta(int current, int max, int delta)
    {
        // 上限先约束为非负，避免异常配置产生负余额。
        long cap = max;
        if (cap < 0L) cap = 0L;
        long value = (long)current + (long)delta;
        if (value < 0L) return 0;
        if (value > cap) return (int)cap;
        return (int)value;
    }

    /// <summary>按点数裁切已有资源；只处理范围，不写回任何实体，避免重复余额。</summary>
    /// <param name="current">当前资源点。</param>
    /// <param name="max">该资源的上限点。</param>
    /// <returns>裁切后的真实资源点。</returns>
    public static int Clamp(int current, int max)
    {
        // 复用增量口的零增量路径，保持上下限规则一致。
        return ApplyDelta(current, max, 0);
    }

    /// <summary>判断消耗请求是否可全额支付，供调用入口在扣减前判定，避免余额不足时白扣。</summary>
    /// <param name="current">当前资源点。</param>
    /// <param name="amount">期望消耗点数。</param>
    /// <returns>正数且余额足够时返回 true，否则返回 false。</returns>
    public static bool CanConsume(int current, int amount)
    {
        if (amount <= 0) return false;
        if (current < amount) return false;
        return true;
    }
}
