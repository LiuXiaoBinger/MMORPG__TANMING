using System;
using System.Collections.Generic;

/// <summary>专用验收读取真实生成的 YooAsset JSON 清单；只声明本验证需要的强类型字段。</summary>
[Serializable]
public sealed class SkillManifestSnapshot
{
    /// <summary>本次生成清单的正式资源包名称。</summary>
    public string PackageName;

    /// <summary>本次验证版本，用于排除构建报告或其他清单文件。</summary>
    public string PackageVersion;

    /// <summary>真实清单的全部主动收集资源，用于核对完整请求路径。</summary>
    public List<SkillManifestAssetSnapshot> AssetList;

    /// <summary>真实清单的资源包记录，用于检查依赖索引有效范围。</summary>
    public List<SkillManifestBundleSnapshot> BundleList;
}
