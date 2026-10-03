using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 实体对象池单例。
/// 按具体运行时类型缓存已销毁实体，复用时重新写入 UID 并联通组件池和模型池。
/// 避免频繁创建销毁实体带来的 GC 开销，提升场景实体切换性能。
/// </summary>
public sealed class MEntityPool
{
    /// <summary>单例实例（懒汉式，首次访问时初始化）</summary>
    private static readonly MEntityPool _instance = new MEntityPool();

    /// <summary>按实体运行时类型分桶的空闲队列，键为具体类型，值为可复用实体队列</summary>
    private readonly Dictionary<Type, Queue<MEntity>> _pools =
        new Dictionary<Type, Queue<MEntity>>();

    /// <summary>私有构造函数，禁止外部直接实例化，强制使用 Instance 单例访问</summary>
    private MEntityPool()
    {
    }

    /// <summary>实体池全局单例入口</summary>
    public static MEntityPool Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// 从对象池获取指定类型实体；池中无可用实例时通过工厂方法新建。
    /// </summary>
    /// <typeparam name="T">实体具体类型，必须继承 MEntity</typeparam>
    /// <param name="uid">新的运行时唯一 UID</param>
    /// <param name="entityType">实体类型枚举</param>
    /// <param name="configId">配置表 ID</param>
    /// <param name="creator">池中未命中时调用的新建工厂回调</param>
    /// <returns>可直接使用的实体实例</returns>
    /// <exception cref="ArgumentNullException">creator 为空时抛出</exception>
    public T Acquire<T>(ulong uid, EntityType entityType, int configId, Func<ulong, int, T> creator)
        where T : MEntity
    {
        if (creator == null)
        {
            throw new ArgumentNullException("creator");
        }

        // 先尝试从池中取出已销毁的同类型实体复用
        Queue<MEntity> queue;
        if (_pools.TryGetValue(typeof(T), out queue) && queue.Count > 0)
        {
            T reused = queue.Dequeue() as T;
            if (reused != null)
            {
                // 复用前重置身份、状态和组件，确保不会残留上一次使用的数据
                reused.PrepareForReuse(uid, entityType, configId);
                return reused;
            }
        }

        // 池内无可用实例，走新建流程
        return creator(uid, configId);
    }

    /// <summary>
    /// 将已销毁实体回收进对象池，等待下次复用时取出。
    /// 仅接受生命周期为 Destroyed 的实体，防止误回收活跃对象。
    /// </summary>
    /// <param name="entity">待回收实体，null 或未销毁时直接忽略</param>
    public void Release(MEntity entity)
    {
        if (entity == null || entity.LifecycleState != EntityLifecycleState.Destroyed)
        {
            return;
        }

        // 按实际运行时类型分桶，保证复用类型严格匹配
        Type type = entity.GetType();
        Queue<MEntity> queue;
        if (!_pools.TryGetValue(type, out queue))
        {
            queue = new Queue<MEntity>();
            _pools.Add(type, queue);
        }
        queue.Enqueue(entity);
    }
}
