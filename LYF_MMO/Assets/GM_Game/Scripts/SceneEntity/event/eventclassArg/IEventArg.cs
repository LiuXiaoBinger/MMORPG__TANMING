using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实体事件的强类型载荷契约。
/// 所有实体事件参数都必须实现此接口，确保事件分发器可以统一复制和回收载荷。
/// </summary>
public interface IEventArg
{
    /// <summary>
    /// 复制事件载荷，供需要跨回调保存数据的监听器使用。
    /// 需要保存或延迟处理参数时由调用方显式复制；事件分发本身共享同一参数实例。
    /// </summary>
    /// <returns>当前事件参数的独立副本。</returns>
    IEventArg Clone();

    /// <summary>
    /// 结束事件载荷生命周期并归还对应参数对象池。
    /// 分发器会在全部监听器回调结束后调用；监听器若需跨帧保存数据，必须先调用 Clone 并自行在使用结束后回收副本。
    /// </summary>
    void Recycle();

    /// <summary>对象池生命周期标记，true 表示参数已归还池中。</summary>
    bool Deprecated { get; set; }
}

/// <summary>事件参数对象池，按具体参数类型维护可复用实例并防止同一实例重复入池。</summary>
public class MEventPool<T> where T : class, IEventArg, new()
{
    /// <summary>当前参数类型的空闲实例队列。</summary>
    private static Queue<T> _pool = null;

    /// <summary>清空当前参数类型的对象池。</summary>
    public static void Clear()
    {
        if (_pool != null)
        {
            _pool.Clear();
            _pool = null;
        }
    }

    /// <summary>获取一个可复用的事件参数实例。</summary>
    /// <returns>已重置生命周期标记的参数实例。</returns>
    public static T GetEvent()
    {
        if (_pool == null)
        {
            _pool = new Queue<T>();
            
        }
        if (_pool.Count > 0)
        {
            // 从空闲队列取出一个实例并恢复为可用状态。
            T t = _pool.Dequeue();
            t.Deprecated = false;
#if WINDOWS && DEBUG
                if (t == null)
                {
                    MDebug.singleton.AddErrorLog("XEvent Type should be ", typeof(T).ToString(), " but is ", t.ToString());
                }
#endif
            return t;
        }
        else
        {
            return new T();
        }
    }

    /// <summary>将事件参数归还对象池。</summary>
    /// <param name="args">需要回收的参数实例。</param>
    public static void Recycle(T args)
    {
        // 空参数没有可回收的实例，直接结束本次回收调用。
        if (args == null)
        {
            return;
        }
        // 首次回收时先创建队列，兼容未调用 GetEvent 直接回收的参数实例。
        if (_pool == null)
        {
            _pool = new Queue<T>();
        }
        if (args.Deprecated)
        {
           // MDebug.singleton.AddErrorLogF("Duplicate Recycle args, name={0}", typeof(T).Name);
            return;
        }
        args.Deprecated = true;
        _pool.Enqueue(args);
    }
}

/// <summary>实体移动事件参数，记录移动目标、朝向和移动阶段状态。</summary>
public class MMoveEventArgs : IEventArg
{
    /// <summary>是否继续移动。</summary>
    public bool keepMoving;
    /// <summary>移动目标位置。</summary>
    public Vector3 targetPos;
    /// <summary>移动时的朝向向量。</summary>
    public Vector3 face;
    /// <summary>是否已经发生位移。</summary>
    public bool moved;
    /// <summary>移动开始前的延迟时间，单位为秒。</summary>
    public float delay;
    /// <summary>是否由本地 NPC 移动逻辑产生。</summary>
    public bool isLocalMove; //本地npc等才需要置位true

    /// <summary>同步目标处的跳跃段号，零为落地，1 至 3 为本轮滞空的起跳次数。</summary>
    public int jumpCount;

    /// <summary>对象池生命周期标记。</summary>
    public bool Deprecated { get; set; } = false;

    /// <summary>从移动事件参数对象池获取并填写一次移动同步数据。</summary>
    /// <param name="keepMoving">本次同步后是否保持移动状态。</param>
    /// <param name="targetPosition">移动目标世界坐标，单位为 Unity 世界单位。</param>
    /// <param name="faceDirection">移动朝向的归一化世界方向向量。</param>
    /// <param name="hasMoved">本次事件是否已经产生位移。</param>
    /// <param name="delaySeconds">移动生效前延迟秒数，非负值。</param>
    /// <param name="isLocalMovement">是否由本地输入或本地 NPC 逻辑产生。</param>
    /// <param name="airborneJumpCount">同步目标的跳跃段号；本地输入和非跳跃移动默认使用零。</param>
    /// <returns>本次分发使用的池化移动参数，分发器完成派发后会自动回收。</returns>
    public static MMoveEventArgs Create(bool keepMoving, Vector3 targetPosition,
        Vector3 faceDirection, bool hasMoved, float delaySeconds,
        bool isLocalMovement, int airborneJumpCount = 0)
    {
        // 从移动参数专属对象池取得可复用实例，避免高频移动输入分配临时对象。
        MMoveEventArgs eventArgs = MEventPool<MMoveEventArgs>.GetEvent();
        eventArgs.ResetData();
        eventArgs.keepMoving = keepMoving;
        eventArgs.targetPos = targetPosition;
        eventArgs.face = faceDirection;
        eventArgs.moved = hasMoved;
        eventArgs.delay = delaySeconds;
        eventArgs.isLocalMove = isLocalMovement;
        eventArgs.jumpCount = airborneJumpCount;
        return eventArgs;
    }

    /// <summary>复制移动事件参数。</summary>
    /// <returns>独立的移动事件参数实例。</returns>
    public IEventArg Clone()
    {
        // 克隆也通过工厂取得独立池实例，监听器跨帧使用完成后必须自行调用 Recycle。
        MMoveEventArgs clone = Create(keepMoving, targetPos, face, moved, delay,
            isLocalMove, jumpCount);
        return clone;
    }

    /// <summary>清理并归还移动事件参数对象池。</summary>
    public void Recycle()
    {
        ResetData();
        MEventPool<MMoveEventArgs>.Recycle(this);
    }

    /// <summary>清空上一次移动事件的全部字段，避免对象池复用后携带旧目标点或朝向。</summary>
    private void ResetData()
    {
        keepMoving = false;
        targetPos = Vector3.zero;
        face = Vector3.zero;
        moved = false;
        delay = 0f;
        isLocalMove = false;
        jumpCount = 0;
    }
}
