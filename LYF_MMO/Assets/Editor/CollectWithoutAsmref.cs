using System;
using UnityEditor;
using YooAsset.Editor;

/// <summary>排除构建日志已确认不能进入 AssetBundle 的编辑器资源。YooAsset 的自定义规则要求实现 IFilterRule。</summary>
[DisplayName("收集资源（排除编辑器对象）")]
public sealed class CollectWithoutAsmref : IFilterRule
{
    /// <summary>过滤程序集引用、模型导入模板、Shader 子图源文件及变体收集配置，保留生成的运行时资源。</summary>
    /// <param name="data">YooAsset 提供的资源路径、收集目录和分组数据。</param>
    /// <returns>资源不是上述编辑器对象时继续收集。</returns>
    public bool IsCollectAsset(FilterRuleData data)
    {
        if (data.AssetPath.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase)
            || data.AssetPath.EndsWith(".ht", StringComparison.OrdinalIgnoreCase)
            || data.AssetPath.EndsWith(".shadersubgraph", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 收集工具的配置属于编辑器；真正的 ShaderVariantCollection 仍然保留。
        return AssetDatabase.GetMainAssetTypeAtPath(data.AssetPath) != typeof(ShaderVariantCollectorSetting);
    }
}
