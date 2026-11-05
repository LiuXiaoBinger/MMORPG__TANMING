using System;
using YooAsset.Editor;

/// <summary>保留 artres 原有资源收集范围，排除 Unity 无法打入 AssetBundle 的程序集引用文件。YooAsset 的自定义规则要求实现 IFilterRule。</summary>
[DisplayName("收集资源（排除程序集引用）")]
public sealed class CollectWithoutAsmref : IFilterRule
{
    /// <summary>程序集引用只参与脚本编译，其余资源继续由现有收集器处理。</summary>
    /// <param name="data">YooAsset 提供的资源路径、收集目录和分组数据。</param>
    /// <returns>路径不是 .asmref 时收集此资源。</returns>
    public bool IsCollectAsset(FilterRuleData data)
    {
        return !data.AssetPath.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase);
    }
}
