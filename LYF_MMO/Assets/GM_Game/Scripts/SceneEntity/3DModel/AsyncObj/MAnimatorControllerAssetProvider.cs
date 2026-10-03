using System;
using UnityEngine;
using YooAsset;

/// <summary>
/// Animator Controller 资源适配器。
/// 路径按 YooAsset Location 解析，失败时不伪造租约，回调 null。
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
    /// 回调在 YooAsset 主线程完成回调中触发。
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
