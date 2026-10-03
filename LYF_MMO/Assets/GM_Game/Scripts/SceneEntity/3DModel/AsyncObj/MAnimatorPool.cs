using System.Collections.Generic;

/// <summary>
/// MAnimator 与 MObjectAnimator 的强类型对象池。
/// 区分两种动画器类型分别池化，避免类型转换开销。
/// </summary>
public sealed class MAnimatorPool
{
    /// <summary>全局单例实例。</summary>
    private static readonly MAnimatorPool _instance = new MAnimatorPool();
    /// <summary>完整 MAnimator 对象队列。</summary>
    private readonly Queue<MAnimator> _animators = new Queue<MAnimator>();
    /// <summary>简化版 MObjectAnimator 对象队列。</summary>
    private readonly Queue<MObjectAnimator> _objectAnimators = new Queue<MObjectAnimator>();

    /// <summary>私有构造函数，禁止外部 new。</summary>
    private MAnimatorPool() { }

    /// <summary>获取 MAnimatorPool 全局单例。</summary>
    public static MAnimatorPool Instance { get { return _instance; } }

    /// <summary>
    /// 从对象池获取动画器实例。
    /// </summary>
    /// <param name="useObjectAnimator">是否使用简化版 MObjectAnimator。</param>
    /// <returns>可用的动画器实例。</returns>
    public MAnimator Acquire(bool useObjectAnimator)
    {
        if (useObjectAnimator)
        {
            if (_objectAnimators.Count > 0) return _objectAnimators.Dequeue();
            return new MObjectAnimator();
        }
        if (_animators.Count > 0) return _animators.Dequeue();
        return new MAnimator();
    }

    /// <summary>
    /// 归还动画器实例到对象池。
    /// 归还前调用 Reset() 清理资源引用和参数缓存。
    /// 按实际类型放入对应的池队列。
    /// </summary>
    /// <param name="animator">要归还的动画器实例，为 null 时直接忽略。</param>
    public void Release(MAnimator animator)
    {
        if (animator == null) return;
        animator.Reset();
        MObjectAnimator objectAnimator = animator as MObjectAnimator;
        if (objectAnimator != null) _objectAnimators.Enqueue(objectAnimator);
        else _animators.Enqueue(animator);
    }
}
