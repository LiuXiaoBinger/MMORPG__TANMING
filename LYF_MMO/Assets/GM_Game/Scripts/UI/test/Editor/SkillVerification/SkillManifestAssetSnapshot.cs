using System;

/// <summary>专用测试读取 YooAsset 清单的主资源记录，属于第三方 JSON 序列化边界。</summary>
[Serializable]
public sealed class SkillManifestAssetSnapshot
{
    /// <summary>清单中完整 Assets 资源路径，必须匹配运行缓存请求路径。</summary>
    public string AssetPath;

    /// <summary>资源本体所在 BundleList 数组下标。</summary>
    public int BundleID;

    /// <summary>当前主资源依赖的 BundleList 下标列表。</summary>
    public int[] DependIDs;
}
