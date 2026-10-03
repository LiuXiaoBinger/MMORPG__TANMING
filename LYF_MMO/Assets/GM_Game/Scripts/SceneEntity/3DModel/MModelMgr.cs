using System.Collections.Generic;

/// <summary>
/// MModel 对象池管理器。
/// 实体销毁时归还 MModel 实例，避免反复创建模型包装对象带来的 GC 开销。
/// 采用单例模式，内部使用 Stack 作为池容器。
/// </summary>
public sealed class MModelMgr
{
    /// <summary>全局单例实例。</summary>
    private static readonly MModelMgr _instance = new MModelMgr();
    /// <summary>空闲 MModel 对象栈。</summary>
    private readonly Stack<MModel> _pool = new Stack<MModel>();

    /// <summary>私有构造函数，禁止外部 new。</summary>
    private MModelMgr()
    {
    }

    /// <summary>获取 MModelMgr 全局单例。</summary>
    public static MModelMgr Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// 从对象池获取一个 MModel 实例。
    /// 池中有空闲实例时直接弹出复用，否则新建实例。
    /// </summary>
    /// <returns>可用的 MModel 实例。</returns>
    public MModel Acquire()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }
        return new MModel();
    }

    /// <summary>
    /// 归还 MModel 实例到对象池。
    /// 归还前调用 model.Reset() 清理已加载的资源和状态。
    /// </summary>
    /// <param name="model">要归还的模型实例，为 null 时直接忽略。</param>
    public void Release(MModel model)
    {
        if (model == null)
        {
            return;
        }
        model.Reset();
        _pool.Push(model);
    }
}
