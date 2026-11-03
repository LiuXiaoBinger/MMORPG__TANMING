using System;
using UnityEngine;
using YooAsset;

/// <summary>
/// 按 YooAsset 资源地址加载 Animator Controller。
/// 成功后把控制器和句柄一起交给回调管理；失败返回 null，没有接收者时立即释放。
/// </summary>
public sealed class MAnimatorControllerAssetProvider
{
    /// <summary>全局单例实例。</summary>
    private static readonly MAnimatorControllerAssetProvider _instance =
        new MAnimatorControllerAssetProvider();

    /// <summary>私有构造函数，禁止外部 new。</summary>
    private MAnimatorControllerAssetProvider()
    {
    }

    /// <summary>获取 MAnimatorControllerAssetProvider 全局单例。</summary>
    public static MAnimatorControllerAssetProvider Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// 异步加载 Animator Controller。
    /// 地址或资源包无效时立即回调 null；发起加载后，在 YooAsset 完成回调中交付结果。
    /// </summary>
    /// <param name="location">YooAsset 资源 Location。</param>
    /// <param name="callback">加载完成回调，参数为 Controller 资源租约，失败时为 null。</param>
    public void LoadAsync(string location, Action<MAnimatorControllerAssetLease> callback)
    {
        if (string.IsNullOrEmpty(location) || Global.Instance == null ||
            Global.Instance.YooPackage == null)
        {
            if (callback != null)
            {
                callback(null);
            }
            return;
        }

        // 校验 Location 有效性，避免无效地址触发加载错误
        if (!Global.Instance.YooPackage.CheckLocationValid(location))
        {
            Debug.LogWarning("Animator Controller 地址无效：" + location);
            if (callback != null)
            {
                callback(null);
            }
            return;
        }

        // 本次控制器加载的句柄，成功后交给资源持有对象负责释放。
        AssetOperationHandle operation =
            Global.Instance.YooPackage.LoadAssetAsync<RuntimeAnimatorController>(location);
        operation.Completed += handle =>
        {
            if (handle.Status != EOperationStatus.Succeed)
            {
                handle.Release();
                if (callback != null)
                {
                    callback(null);
                }
                return;
            }

            // 从完成句柄中取得的控制器资源，为空同样按加载失败处理。
            RuntimeAnimatorController controller =
                handle.GetAssetObject<RuntimeAnimatorController>();
            if (controller == null)
            {
                handle.Release();
                if (callback != null)
                {
                    callback(null);
                }
                return;
            }

            // 包装为资源租约返回，确保后续释放句柄
            MAnimatorControllerAssetLease lease =
                new MAnimatorControllerAssetLease(handle, controller);
            if (callback != null)
            {
                callback(lease);
            }
            else
            {
                lease.Release();
            }
        };
    }
}
