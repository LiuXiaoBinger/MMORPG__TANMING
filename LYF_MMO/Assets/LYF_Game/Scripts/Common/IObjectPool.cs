/// <summary>提供对象池的三个计数；清空空闲对象后，累计创建数仍会保留。</summary>
public interface IObjectPool
{
    /// <summary>本池累计创建的实例数，Clear 不重置该计数。</summary>
    int countAll { get; }

    /// <summary>累计创建数减去当前空闲数；Clear 后不等于实际仍在使用的对象数。</summary>
    int countActive { get; }

    /// <summary>当前可供再次借用的实例数。</summary>
    int countInactive { get; }
}
