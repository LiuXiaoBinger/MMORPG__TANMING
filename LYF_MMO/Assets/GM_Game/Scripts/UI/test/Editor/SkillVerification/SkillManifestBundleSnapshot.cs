using System;

/// <summary>专用测试读取 YooAsset 清单的资源包记录，不参与正式业务接口。</summary>
[Serializable]
public sealed class SkillManifestBundleSnapshot
{
    /// <summary>YooAsset 根据正式收集器计算的完整 Bundle 名。</summary>
    public string BundleName;
}
