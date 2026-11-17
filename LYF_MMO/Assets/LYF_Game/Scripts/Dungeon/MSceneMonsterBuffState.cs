using UnityEngine;

/// <summary>收到原Buff实例时记录本地时刻，异步模型完成后恢复实际剩余寿命。</summary>
public sealed class MSceneMonsterBuffState
{
    /// <summary>服务器实例身份、目标、原BuffId和剩余毫秒。</summary>
    public MonsterBuffData Data;
    /// <summary>接收该次快照的真实秒数。</summary>
    public double ReceivedAt;
    /// <summary>按收到时刻换算的剩余秒数；原常驻为正无穷。</summary>
    public double RemainingSeconds
    {
        get
        {
            if (Data.RemainingMs < 0) return double.PositiveInfinity;
            return Data.RemainingMs * 0.001d - (Time.realtimeSinceStartupAsDouble - ReceivedAt);
        }
    }
}
