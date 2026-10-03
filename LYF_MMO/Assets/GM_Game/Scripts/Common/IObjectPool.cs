/// <summary>提供 RO 对象池统计，活跃数采用累计创建数减当前空闲数。</summary>
public interface IObjectPool
{
    /// <summary>本池累计创建的实例数，Clear 不重置该计数。</summary>
    int countAll { get; }

    /// <summary>累计创建数与当前空闲数之差，沿用 RO 的统计口径。</summary>
    int countActive { get; }

    /// <summary>当前可供再次借用的实例数。</summary>
    int countInactive { get; }
}
