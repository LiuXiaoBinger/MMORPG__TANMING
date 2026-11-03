using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>保存暂时不用的对象，下一次借用时优先复用；可在借出、归还和清空时执行回调。</summary>
/// <typeparam name="T">由创建委托构造的池元素类型；通用池边界不限制元素的构造方式。</typeparam>
public sealed class MObjectPool<T> : IObjectPool
{
    /// <summary>池中没有空闲对象时调用，负责创建一个新对象。</summary>
    /// <returns>新构造且尚未借出的池元素。</returns>
    public delegate T CreateObj();

    /// <summary>空闲实例按后进先出顺序保存。</summary>
    private readonly Stack<T> _stack = new Stack<T>();

#if WINDOWS && DEBUG
    /// <summary>RO Windows 调试条件下记录空闲元素，用于识别重复归还。</summary>
    private readonly HashSet<T> _set = new HashSet<T>();
#endif

    /// <summary>保存创建对象的方法，以及借出、归还和清空对象时要执行的方法。</summary>
    /// <param name="creator">空闲栈耗尽时创建实例的回调。</param>
    /// <param name="actionOnGet">借出后执行的可选初始化回调。</param>
    /// <param name="actionOnRelease">归还前执行的可选清理回调。</param>
    /// <param name="actionOnDestroy">清空闲置实例时执行的可选销毁回调。</param>
    public MObjectPool(CreateObj creator, UnityAction<T> actionOnGet,
        UnityAction<T> actionOnRelease, UnityAction<T> actionOnDestroy = null)
    {
        _creator = creator;
        _actionOnGet = actionOnGet;
        _actionOnRelease = actionOnRelease;
        _actionOnDestroy = actionOnDestroy;
    }

    /// <summary>空闲栈耗尽时使用的必需实例构造回调。</summary>
    private readonly CreateObj _creator;

    /// <summary>每次借出时执行的可选初始化回调。</summary>
    private readonly UnityAction<T> _actionOnGet;

    /// <summary>每次归还前执行的可选清理回调。</summary>
    private readonly UnityAction<T> _actionOnRelease;

    /// <summary>清空闲置实例时执行的可选销毁回调。</summary>
    private readonly UnityAction<T> _actionOnDestroy;

    /// <summary>本池累计创建的实例数；与 RO 一致，Clear 不重置该计数。</summary>
    public int countAll { get; private set; }

    /// <summary>累计创建数减去当前空闲数；Clear 后不代表实际借出对象数。</summary>
    public int countActive { get { return countAll - countInactive; } }

    /// <summary>当前可供再次借用的实例数。</summary>
    public int countInactive { get { return _stack.Count; } }

    /// <summary>借出一个实例，新建仅发生在空闲栈为空时。</summary>
    /// <returns>由调用方独占至 Release 的实例。</returns>
    public T Get()
    {
        // 本次借出的实例，可能来自空闲栈或新构造。
        T element;
        if (_stack.Count == 0)
        {
            element = _creator();
            countAll++;
        }
        else
        {
            element = _stack.Pop();
#if WINDOWS && DEBUG
            _set.Remove(element);
#endif
        }
        if (_actionOnGet != null)
        {
            _actionOnGet(element);
        }
        return element;
    }

    /// <summary>先执行归还回调，再存入空闲栈；WINDOWS 与 DEBUG 同时启用时检查重复归还。</summary>
    /// <param name="element">本次归还的池元素。</param>
    public void Release(T element)
    {
#if WINDOWS && DEBUG
        if (_set.Contains(element))
        {
            // 同一个对象已在空闲集合中，不能再次入栈。
            UnityEngine.Debug.LogError("对象池元素重复归还。");
            return;
        }
        _set.Add(element);
#endif
        if (_actionOnRelease != null)
        {
            _actionOnRelease(element);
        }
        _stack.Push(element);
    }

    /// <summary>清除当前空闲实例；借出实例仍由原持有方归还。</summary>
    public void Clear()
    {
        if (_actionOnDestroy != null)
        {
            while (_stack.Count > 0)
            {
                _actionOnDestroy(_stack.Pop());
            }
        }
        _stack.Clear();
    }
}
