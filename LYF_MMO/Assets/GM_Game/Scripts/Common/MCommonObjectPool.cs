/// <summary>参考 RO 为单一具体类型提供全局共享的借还入口。</summary>
/// <typeparam name="T">具有公开无参构造函数的池化引用类型。</typeparam>
public static class MCommonObjectPool<T> where T : class, new()
{
    /// <summary>该具体类型的唯一对象池，生命周期随客户端进程。</summary>
    private static readonly MObjectPool<T> s_pool = new MObjectPool<T>(Create, null, null);

    /// <summary>空闲栈耗尽时构造指定类型的新实例。</summary>
    /// <returns>尚未借出的池化引用类型实例。</returns>
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
            // 本项目未接入 RO MDebug，以 Unity 日志承接通用对象池错误。
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
