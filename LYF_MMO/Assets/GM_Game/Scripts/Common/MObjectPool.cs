using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>沿用 RO 通用池，通过创建委托与借还回调复用实例。</summary>
/// <typeparam name="T">由创建委托构造的池元素类型；通用池边界不限制元素的构造方式。</typeparam>
public sealed class MObjectPool<T> : IObjectPool
{
    /// <summary>RO 风格的实例构造回调，返回本池独占的新引用。</summary>
    /// <returns>新构造且尚未借出的池元素。</returns>
    public delegate T CreateObj();

    /// <summary>空闲实例按后进先出顺序保存。</summary>
    private readonly Stack<T> _stack = new Stack<T>();

#if WINDOWS && DEBUG
    /// <summary>RO Windows 调试条件下记录空闲元素，用于识别重复归还。</summary>
    private readonly HashSet<T> _set = new HashSet<T>();
#endif

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

    /// <summary>累计创建数与当前空闲数之差，沿用 RO 的统计口径。</summary>
    public int countActive { get { return countAll - countInactive; } }

    /// <summary>当前可供再次借用的实例数。</summary>
    public int countInactive { get { return _stack.Count; } }

    /// <summary>以 RO 创建委托形式指定实例构造与生命周期钩子。</summary>
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

    /// <summary>执行归还回调并压入空闲栈，调试检查沿用 RO 条件。</summary>
    /// <param name="element">本次归还的池元素。</param>
    public void Release(T element)
    {
#if WINDOWS && DEBUG
        if (_set.Contains(element))
        {
            // 本项目未接入 RO MDebug，以 Unity 日志承接通用池调试错误。
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
