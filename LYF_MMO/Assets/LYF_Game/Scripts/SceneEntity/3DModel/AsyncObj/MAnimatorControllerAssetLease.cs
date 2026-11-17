using YooAsset;
using UnityEngine;

/// <summary>
/// 把已经加载的动画控制器与 YooAsset 加载句柄放在一起交给使用者。
/// 使用结束调用 Release，一次释放后同时清空控制器和句柄引用。
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
    /// 重复释放不会影响资源引用计数，通过 _released 标记保证只释放一次。
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
