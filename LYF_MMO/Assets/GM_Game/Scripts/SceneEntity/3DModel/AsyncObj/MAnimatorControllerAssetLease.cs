using YooAsset;
using UnityEngine;

/// <summary>
/// Animator Controller 的资源租约。
/// 封装 YooAsset 异步加载句柄，确保异步加载完成后由 Animator 池回收时正确释放句柄。
/// 防止对象池复用时旧实体的异步回调泄露资源引用。
/// </summary>
public sealed class MAnimatorControllerAssetLease
{
    /// <summary>YooAsset 资源操作句柄。</summary>
    private AssetOperationHandle _handle;
    /// <summary>是否已释放标记，防止重复释放。</summary>
    private bool _released;

    /// <summary>获取已加载的 RuntimeAnimatorController 对象。</summary>
    public RuntimeAnimatorController Controller { get; private set; }

    /// <summary>
    /// 构造 Controller 资源租约。
    /// </summary>
    /// <param name="handle">YooAsset 资源操作句柄。</param>
    /// <param name="controller">已加载的 Animator Controller 对象。</param>
    internal MAnimatorControllerAssetLease(AssetOperationHandle handle,
        RuntimeAnimatorController controller)
    {
        _handle = handle;
        Controller = controller;
    }

    /// <summary>
    /// 释放 YooAsset 句柄。
    /// 重复释放不会影响资源引用计数，通过 _released 标记保证幂等。
    /// </summary>
    public void Release()
    {
        if (_released)
        {
            return;
        }
        _released = true;
        if (_handle != null)
        {
            _handle.Release();
        }
        _handle = null;
        Controller = null;
    }
}
