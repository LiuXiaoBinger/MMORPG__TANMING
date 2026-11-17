/// <summary>为每一种 T 共用一个对象池；借出和归还时不自动重置对象字段。</summary>
/// <typeparam name="T">可通过公开无参构造函数创建的类。</typeparam>
public static class MCommonObjectPool<T> where T : class, new()
{
    /// <summary>该具体类型的唯一对象池，生命周期随客户端进程。</summary>
    private static readonly MObjectPool<T> s_pool = new MObjectPool<T>(Create, null, null);

    /// <summary>空闲栈耗尽时构造指定类型的新实例。</summary>
    /// <returns>刚创建、尚未交给调用方的对象。</returns>
    private static T Create()
    {
        return new T();
    }

    /// <summary>借出一个由调用方负责初始化的实例。</summary>
    /// <returns>当前调用方独占的实例。</returns>
    public static T Get()
    {
        return s_pool.Get();
    }

    /// <summary>归还已由调用方重置的实例。</summary>
    /// <param name="toRelease">当前调用方独占的实例。</param>
    public static void Release(T toRelease)
    {
        if (toRelease == null)
        {
            // 空对象不能放回池中，记录错误后结束归还。
            UnityEngine.Debug.LogError("归还的池对象为空。");
            return;
        }
#if WINDOWS && DEBUG
        if (typeof(T) != toRelease.GetType())
        {
            UnityEngine.Debug.LogError("归还的池对象类型与对象池类型不匹配。");
            return;
        }
#endif
        s_pool.Release(toRelease);
    }

    /// <summary>清除该类型当前空闲的实例。</summary>
    public static void Clear()
    {
        s_pool.Clear();
    }
}
