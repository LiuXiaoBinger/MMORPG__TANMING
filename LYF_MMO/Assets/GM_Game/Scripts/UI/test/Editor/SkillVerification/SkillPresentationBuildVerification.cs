using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

/// <summary>专用技能资源验收：真实 YooAsset 清单加实际相关 Bundle 构建/读取，不创建角色或改正式场景。</summary>
public static class SkillPresentationBuildVerification
{
    /// <summary>验证当前保存资产、生成真实全包模拟清单，并实际构建技能所需的完整依赖 Bundle 子集。</summary>
    public static void BuildAndVerify()
    {
        SkillEditorAssetTools.ValidateAll();
        // 所有本轮产物统一放到项目根 _temp；保留历史验证目录，不递归删除。
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
        // 时间戳独立输出，避免 YooAsset 构建器覆盖或清理任何既有结果。
        string output = Path.Combine(projectRoot, "_temp", "ycombat-integration", "bundle-verification-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(output);
        // 使用现有 DefaultPackage 正式收集设置，不建立替代游戏资源包。
        BuildParameters parameters = new BuildParameters
        {
            BuildOutputRoot = Path.Combine(output, "yoo-manifest"),
            StreamingAssetsRoot = Path.Combine(output, "streaming-unused"),
            BuildTarget = EditorUserBuildSettings.activeBuildTarget,
            BuildPipeline = EBuildPipeline.BuiltinBuildPipeline,
            BuildMode = EBuildMode.SimulateBuild,
            PackageName = "DefaultPackage", PackageVersion = "SkillVerification",
            SharedPackRule = new ZeroRedundancySharedPackRule(),
            CopyBuildinFileOption = ECopyBuildinFileOption.None, EnableLog = true
        };
        // 完整 YooAsset 管线实际生成清单文件；此阶段未生成原生 Bundle。
        BuildResult result = new AssetBundleBuilder().Run(parameters);
        if (!result.Success) throw new InvalidOperationException("YooAsset 技能清单生成失败：" + result.FailedTask + " " + result.ErrorInfo);
        // 内部 YooAssetSettingsData 不作为可调用 API；只扫描本次新输出，按真实清单头识别唯一结果。
        string manifestPath;
        // JsonUtility 是第三方清单序列化边界，具名测试结构只读取所需字段。
        SkillManifestSnapshot manifest = ReadGeneratedManifest(result.OutputPackageDirectory, parameters.PackageName, parameters.PackageVersion, out manifestPath);
        if (manifest == null || manifest.PackageName != "DefaultPackage" || manifest.AssetList == null || manifest.BundleList == null)
            throw new InvalidOperationException("YooAsset 生成清单结构无效。");
        // 此七份运行资产是正式缓存每次 Retain 请求的精确路径。
        string[] runtimeGuids = AssetDatabase.FindAssets("t:SkillRuntimePresentation", new[] { SkillEditorAssetTools.RuntimeDirectory });
        if (runtimeGuids.Length != 7) throw new InvalidOperationException("清单验收需要七份正式运行资产。");
        // 实际 Bundle 输入以 YooAsset 正式构建映射为准，选中包的其他资产不会被删改。
        parameters.BuildMode = EBuildMode.IncrementalBuild;
        BuildMapContext map = new TaskGetBuildMap().CreateBuildMap(parameters);
        // 字典值快照按索引扫描，引用仍对应原构建映射。
        List<BuildBundleInfo> bundles = new List<BuildBundleInfo>(map.Collection);
        // 主资源到 Bundle 的确定性索引，依赖资源仍按 YooAsset 规则分配。
        Dictionary<string, BuildAssetInfo> assetsByPath = new Dictionary<string, BuildAssetInfo>();
        for (int bundleIndex = 0; bundleIndex < bundles.Count; bundleIndex++)
        {
            // 当前完整 Bundle 的原主资源数组。
            List<BuildAssetInfo> mainAssets = bundles[bundleIndex].AllMainAssets;
            for (int assetIndex = 0; assetIndex < mainAssets.Count; assetIndex++)
                assetsByPath.Add(mainAssets[assetIndex].AssetPath, mainAssets[assetIndex]);
        }
        // 待真实构建的 Bundle 名列表同时作为队列，迭代加入完整依赖闭包。
        List<string> selectedBundles = new List<string>();
        for (int index = 0; index < runtimeGuids.Length; index++)
        {
            // 当前运行资产完整路径，必须存在于新生成清单及原构建映射。
            string runtimePath = AssetDatabase.GUIDToAssetPath(runtimeGuids[index]);
            SkillManifestAssetSnapshot item = FindManifestAsset(manifest, runtimePath);
            ValidateManifestDependencies(manifest, item);
            AddBundleForAsset(assetsByPath, selectedBundles, runtimePath);
            // 所有技能统一由运行时间轴持有粒子依赖。
            SkillRuntimePresentation runtime = AssetDatabase.LoadAssetAtPath<SkillRuntimePresentation>(runtimePath);
            for (int eventIndex = 0; eventIndex < runtime.FxEvents.Count; eventIndex++)
            {
                // 每条 FX 的根预制体必须真实进入主资源/依赖收集图。
                string particlePath = AssetDatabase.GetAssetPath(runtime.FxEvents[eventIndex].Particle);
                if (!Contains(AssetDatabase.GetDependencies(runtimePath, true), particlePath))
                    throw new InvalidOperationException("正式运行资产没有真实粒子依赖：" + particlePath);
            }
        }
        for (int queueIndex = 0; queueIndex < selectedBundles.Count; queueIndex++)
        {
            // 队列的每个 Bundle 均完整打包，使用其全部主资产的依赖，保证输出可真正加载。
            BuildBundleInfo bundle = map.GetBundleInfo(selectedBundles[queueIndex]);
            for (int assetIndex = 0; assetIndex < bundle.AllMainAssets.Count; assetIndex++)
            {
                // YooAsset 已计算过每个主资产的完整依赖与共享 Bundle 名。
                List<BuildAssetInfo> dependencies = bundle.AllMainAssets[assetIndex].AllDependAssetInfos;
                if (dependencies == null) continue;
                for (int dependencyIndex = 0; dependencyIndex < dependencies.Count; dependencyIndex++)
                {
                    // 没有独立 BundleName 的冗余/零依赖资源由 Unity 随主资产打包。
                    string bundleName = dependencies[dependencyIndex].BundleName;
                    if (!string.IsNullOrEmpty(bundleName) && !selectedBundles.Contains(bundleName)) selectedBundles.Add(bundleName);
                }
            }
        }
        // 仅为完整技能依赖子集构建真实原生 Bundle，不构建无关场景或整个 Player。
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
        for (int index = 0; index < selectedBundles.Count; index++)
        {
            // 此测试只处理可由 Unity AssetBundle 直接加载的普通资源包。
            BuildBundleInfo bundle = map.GetBundleInfo(selectedBundles[index]);
            if (bundle.IsRawFile) throw new InvalidOperationException("技能依赖不应包含原生文件：" + bundle.BundleName);
            builds.Add(bundle.CreatePipelineBuild());
        }
        // 实际构建输出目录独立于游戏正式资源包和 StreamingAssets。
        string bundleOutput = Path.Combine(output, "actual-skill-bundles");
        Directory.CreateDirectory(bundleOutput);
        AssetBundleManifest unityManifest = BuildPipeline.BuildAssetBundles(bundleOutput, builds.ToArray(),
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, parameters.BuildTarget);
        if (unityManifest == null) throw new InvalidOperationException("技能相关 Bundle 实际构建失败。");
        LoadAndVerifyBundles(bundleOutput, selectedBundles, runtimeGuids, assetsByPath);
        Debug.Log("YCOMBAT_BUNDLE_VERIFY_SUCCESS full-manifest=" + manifestPath + " actual-bundles=" + selectedBundles.Count + " runtime-assets=7 output=" + bundleOutput);
    }

    /// <summary>从本次 YooAsset 新输出中按真实清单字段识别唯一 JSON，避免依赖内部文件命名 API。</summary>
    /// <param name="directory">本次唯一构建输出目录，不扫描历史构建目录。</param>
    /// <param name="package">预期的正式资源包名称。</param>
    /// <param name="version">本次专用验证资源版本。</param>
    /// <param name="manifestPath">唯一匹配的实际清单文件路径。</param>
    /// <returns>反序列化的具名清单数据；缺失或多个匹配均报错。</returns>
    private static SkillManifestSnapshot ReadGeneratedManifest(string directory, string package, string version, out string manifestPath)
    {
        manifestPath = null;
        // YooAsset 同时输出报告与清单，不能任取首个 JSON 文件。
        string[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
        // 仅完整满足本次包/版本与两份资源列表的文件可成为结果。
        SkillManifestSnapshot result = null;
        for (int index = 0; index < files.Length; index++)
        {
            // 第三方序列化边界只读本次实际 JSON 字段，不修改包内公开级别。
            SkillManifestSnapshot candidate = JsonUtility.FromJson<SkillManifestSnapshot>(File.ReadAllText(files[index]));
            if (candidate == null || candidate.PackageName != package || candidate.PackageVersion != version || candidate.AssetList == null || candidate.BundleList == null) continue;
            if (result != null) throw new InvalidOperationException("本次输出存在多个匹配清单，拒绝任意选择。");
            result = candidate;
            manifestPath = files[index];
        }
        if (result == null) throw new InvalidOperationException("本次 YooAsset 输出没有唯一有效清单。");
        return result;
    }

    /// <summary>从实际构建文件加载完整依赖闭包并验证运行资产粒子引用。</summary>
    /// <param name="output">本轮 _temp 下的实际 Bundle 输出目录。</param>
    /// <param name="bundleNames">已经实际构建的完整依赖包名称列表。</param>
    /// <param name="runtimeGuids">七份源运行资产的 AssetDatabase GUID 数组。</param>
    /// <param name="assetsByPath">正式 YooAsset 构建映射的主资源索引。</param>
    private static void LoadAndVerifyBundles(string output, List<string> bundleNames, string[] runtimeGuids,
        Dictionary<string, BuildAssetInfo> assetsByPath)
    {
        // 加载租约仅属于本次验证，成功或失败均在 finally 卸载。
        Dictionary<string, AssetBundle> loaded = new Dictionary<string, AssetBundle>();
        try
        {
            for (int index = 0; index < bundleNames.Count; index++)
            {
                // 每个相关 Bundle 从真实文件读取，不使用 AssetDatabase 替代加载。
                string name = bundleNames[index];
                AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(output, name));
                if (bundle == null) throw new InvalidOperationException("无法加载真实技能 Bundle：" + name);
                loaded.Add(name, bundle);
            }
            for (int index = 0; index < runtimeGuids.Length; index++)
            {
                // 先根据原构建映射确定本体包，然后从实际 Bundle 读取强类型 SO。
                string path = AssetDatabase.GUIDToAssetPath(runtimeGuids[index]);
                BuildAssetInfo asset = assetsByPath[path];
                SkillRuntimePresentation runtime = loaded[asset.BundleName].LoadAsset<SkillRuntimePresentation>(path);
                if (runtime == null || runtime.PresentationId <= 0) throw new InvalidOperationException("实际 Bundle 缺少运行表现：" + path);
                for (int eventIndex = 0; eventIndex < runtime.FxEvents.Count; eventIndex++)
                {
                    if (runtime.FxEvents[eventIndex].Particle == null)
                        throw new InvalidOperationException("真实 Bundle 粒子依赖为空：" + path + "/" + eventIndex);
                }
                Debug.Log("YCOMBAT_BUNDLE_ASSET " + path + " fx=" + runtime.FxEvents.Count);
            }
        }
        finally
        {
            // 值快照允许统一按索引释放，即使验证中抛异常也归还所有已加载包。
            List<AssetBundle> snapshot = new List<AssetBundle>(loaded.Values);
            for (int index = 0; index < snapshot.Count; index++) snapshot[index].Unload(true);
        }
    }

    /// <summary>按准确资产路径选择其正式构建 Bundle，不用自定打包名。</summary>
    /// <param name="assets">正式构建映射中主资源的精确索引。</param>
    /// <param name="selected">本轮待构建的包名称列表。</param>
    /// <param name="path">必须被正式收集的实际 Assets 路径。</param>
    private static void AddBundleForAsset(Dictionary<string, BuildAssetInfo> assets, List<string> selected, string path)
    {
        // 精确路径命中的主资源，缺失即表示正式收集设置有漏洞。
        BuildAssetInfo asset;
        if (!assets.TryGetValue(path, out asset)) throw new InvalidOperationException("技能资产未进入正式 YooAsset 构建映射：" + path);
        if (!selected.Contains(asset.BundleName)) selected.Add(asset.BundleName);
    }

    /// <summary>读取本轮真实 YooAsset 清单中的精确主资源记录。</summary>
    /// <param name="manifest">本轮实际生成并读取的完整清单。</param>
    /// <param name="path">运行缓存请求的完整资源路径。</param>
    /// <returns>清单中的唯一资源记录；缺失时抛出错误。</returns>
    private static SkillManifestAssetSnapshot FindManifestAsset(SkillManifestSnapshot manifest, string path)
    {
        for (int index = 0; index < manifest.AssetList.Count; index++)
        {
            if (manifest.AssetList[index].AssetPath == path) return manifest.AssetList[index];
        }
        throw new InvalidOperationException("真实 YooAsset 清单缺少运行请求路径：" + path);
    }

    /// <summary>检查主资源本体与所有依赖索引都能引用本轮清单的实际包记录。</summary>
    /// <param name="manifest">本轮真实资源清单。</param>
    /// <param name="asset">需要加载的运行主资产记录。</param>
    private static void ValidateManifestDependencies(SkillManifestSnapshot manifest, SkillManifestAssetSnapshot asset)
    {
        if (asset.BundleID < 0 || asset.BundleID >= manifest.BundleList.Count || asset.DependIDs == null)
            throw new InvalidOperationException("清单本体 Bundle ID 无效：" + asset.AssetPath);
        for (int index = 0; index < asset.DependIDs.Length; index++)
        {
            if (asset.DependIDs[index] < 0 || asset.DependIDs[index] >= manifest.BundleList.Count)
                throw new InvalidOperationException("清单依赖 Bundle ID 无效：" + asset.AssetPath);
        }
    }

    /// <summary>核对 AssetDatabase 依赖图的精确路径。</summary>
    /// <param name="paths">Unity 实际计算的依赖数组。</param>
    /// <param name="path">粒子预制体实际路径。</param>
    /// <returns>路径精确存在时为 true。</returns>
    private static bool Contains(string[] paths, string path)
    {
        for (int index = 0; index < paths.Length; index++) if (paths[index] == path) return true;
        return false;
    }
}
