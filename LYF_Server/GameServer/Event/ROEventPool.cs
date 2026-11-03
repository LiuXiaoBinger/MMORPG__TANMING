using System.Collections.Generic;

/// <summary>
/// 实体事件对象池，对应参考工程 rogamelibs/rocommongamelibs/utility/object_pool/object_pool.h 的 ObjectPool&lt;T&gt;。
/// 每个事件类型各一份池，取用时重置字段并换发新令牌，引用归零时压回空闲栈等待复用。
/// 池只承担"复用实例"这一件事：不提供任何面向测试的计数或清空入口，
/// 复用是否生效由外部通过实例同一性与分配量观测。
/// </summary>
/// <typeparam name="T">池化的事件类型，必须继承 ROEvent 并提供无参构造。</typeparam>
public sealed class ROEventPool<T> where T : ROEvent, new()
{
    /// <summary>
    /// 私有构造：对象池只允许通过 Instance 取用，避免业务侧 new 出第二份池导致实例散落。
    /// </summary>
    private ROEventPool()
    {
    }

    /// <summary>单类型空闲实例上限；超出部分交给垃圾回收，限制池本身长期保留的对象数量。</summary>
    private const int MaxIdleCount = 128;

    /// <summary>进程内该事件类型的唯一对象池，对应 RO 的 ObjectPool&lt;T&gt;::Instance()。</summary>
    private static readonly ROEventPool<T> instance_ = new ROEventPool<T>();

    /// <summary>空闲实例栈，优先复用最近归还的事件。</summary>
    private readonly Stack<T> idle_stack_ = new Stack<T>();

    /// <summary>获取该事件类型的唯一对象池实例。</summary>
    public static ROEventPool<T> Instance
    {
        get { return instance_; }
    }

    /// <summary>
    /// 取出一个事件实例：空闲栈非空时复用，否则新建。
    /// 取用时统一调用 OnGetFromPool 重置字段、换发令牌并把引用计数置为一。
    /// </summary>
    /// <returns>引用计数为一的事件实例，调用方负责最终 Release。</returns>
    public T Get()
    {
        if (idle_stack_.Count > 0)
        {
            return TakeFromIdle();
        }
        // created 是池空时新建的实例，同样要过一次取用初始化，保证与复用路径完全一致。
        T created = new T();
        created.OnGetFromPool();
        return created;
    }

    /// <summary>
    /// 从空闲栈取一个实例并完成取用初始化。
    /// </summary>
    /// <returns>已完成 OnGetFromPool 的事件实例。</returns>
    private T TakeFromIdle()
    {
        // evt 是从空闲栈弹出的实例，取用初始化会把它的引用计数重新置为一并清空上一次的载荷。
        T evt = idle_stack_.Pop();
        evt.OnGetFromPool();
        return evt;
    }

    /// <summary>
    /// 归还一个事件实例，超出空闲上限时直接丢弃交给垃圾回收。
    /// 入口保持 public，是为了让派生事件（含其它程序集里的派生事件）能自己回池；
    /// 但真正的入栈权在 ROEvent.TryEnterPool 的所有权标记上：
    /// 活跃实例与已入池实例都会被拒绝，所以不需要第二份"已在池中"的账本，
    /// 也不存在"调一次归还口就能把活跃实例塞进空闲栈"的绕过路径。
    /// </summary>
    /// <param name="evt">待归还的事件实例，必须已把活跃引用释放到零。</param>
    public void Release(T evt)
    {
        if (evt == null)
        {
            return;
        }
        if (!evt.TryEnterPool())
        {
            // 活跃实例（还有引用）或已入池实例被再次归还：拒绝入栈，避免同一实例在池中出现两份。
            LogMsg.Info("对象池拒绝非归零或重复归还的事件: " + typeof(T).Name, LogMsgType.Error);
            return;
        }
        if (idle_stack_.Count >= MaxIdleCount)
        {
            // 超出上限的实例直接丢弃：引用计数停在池内哨兵，后续既不能被引用也不能再次归还。
            return;
        }
        idle_stack_.Push(evt);
    }
}
