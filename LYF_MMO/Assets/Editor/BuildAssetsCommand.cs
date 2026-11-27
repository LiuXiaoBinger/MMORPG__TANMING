using HybridCLR.Editor.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor {
    // 编辑器构建工具：编译热更 DLL、复制运行所需程序集，并保留示例 AssetBundle 的构建入口。
    public static class BuildAssetsCommand {
        // HybridCLR 构建缓存根目录，位于 Unity 工程 Assets 下。
        public static string HybridCLRBuildCacheDir => Application.dataPath + "/HybridCLRBuildCache";

        // 示例 AssetBundle 的输出根目录，下级按构建平台分开保存。
        public static string AssetBundleOutputDir => $"{HybridCLRBuildCacheDir}/AssetBundleOutput";

        // 示例资源构建时准备的临时目录根路径。
        public static string AssetBundleSourceDataTempDir => $"{HybridCLRBuildCacheDir}/AssetBundleSourceData";

        #region 查询与判定
        // 取得目标平台 target 的 AssetBundle 输出目录。
        public static string GetAssetBundleOutputDirByTarget(BuildTarget target) {
            return $"{AssetBundleOutputDir}/{target}";
        }

        // 取得目标平台 target 的资源构建临时目录。
        public static string GetAssetBundleTempDirByTarget(BuildTarget target) {
            return $"{AssetBundleSourceDataTempDir}/{target}";
        }
        #endregion

        #region 核心逻辑
        // 从路径 s 的 Assets/ 开始截取 Unity 资源路径；调用方必须传入包含该片段的路径。
        public static string ToRelativeAssetPath(string s) {
            return s.Substring(s.IndexOf("Assets/"));
        }

        /// <summary>
        /// 把 Assets/Prefabs/Cube.prefab 打入名为 prefabs 的示例资源包。
        /// 这是独立的示例构建入口，当前 Build/BuildDll 菜单没有调用它。
        /// </summary>
        /// <param name="tempDir">预留的构建临时目录；当前只创建目录，没有向其中写资源。</param>
        /// <param name="outputDir">生成 AssetBundle 的输出目录。</param>
        /// <param name="target">本次资源包对应的 Unity 构建平台。</param>
        private static void BuildAssetBundles(string tempDir, string outputDir, BuildTarget target) {
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(outputDir);

            // 交给 Unity 构建管线的分包清单，当前只包含 prefabs 包。
            List<AssetBundleBuild> abs = new List<AssetBundleBuild>();

            {
                // 当前示例包包含的预制体路径，构建前转换成 Assets/ 开头的相对路径。
                var prefabAssets = new List<string>();
                // 示例 Cube 预制体的绝对路径。
                string testPrefab = $"{Application.dataPath}/Prefabs/Cube.prefab";
                prefabAssets.Add(testPrefab);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                abs.Add(new AssetBundleBuild {
                    assetBundleName = "prefabs",
                    assetNames = prefabAssets.Select(s => ToRelativeAssetPath(s)).ToArray(),
                });
            }

            BuildPipeline.BuildAssetBundles(outputDir, abs.ToArray(), BuildAssetBundleOptions.None, target);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        // 使用 target 对应的临时目录和输出目录构建示例资源包。
        public static void BuildAssetBundleByTarget(BuildTarget target) {
            BuildAssetBundles(GetAssetBundleTempDirByTarget(target), GetAssetBundleOutputDirByTarget(target), target);
        }

        // 菜单入口：按编辑器当前平台编译热更代码，复制 DLL 后刷新 Unity 资源。
        [MenuItem("Build/BuildDll")]
        public static void BuildAndCopyABAOTHotUpdateDlls() {
            // 编辑器当前选择的目标平台，决定热更程序集的编译条件。
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            //   BuildAssetBundleByTarget(target);
            CompileDllCommand.CompileDll(target);
            CopyABAOTHotUpdateDlls(target);
            AssetDatabase.Refresh();
        }

        // 复制 AOT 和热更程序集；target 是保留参数，两个复制函数实际读取编辑器当前平台。
        public static void CopyABAOTHotUpdateDlls(BuildTarget target) {
            // CopyAssetBundlesToStreamingAssets(target);
            CopyAOTAssembliesToStreamingAssets();
            CopyHotUpdateAssembliesToStreamingAssets();
        }

        //[MenuItem("HybridCLR/Build/BuildAssetbundle")]
        // 按编辑器当前平台构建示例包；上方菜单标记目前被注释，不能从该菜单直接触发。
        public static void BuildSceneAssetBundleActiveBuildTargetExcludeAOT() {
            BuildAssetBundleByTarget(EditorUserBuildSettings.activeBuildTarget);
        }

        // 复制主包裁剪后的 AOT 程序集到 Assets/LYF_Game/Dlls，并加 .bytes 后缀供资源系统加载。
        public static void CopyAOTAssembliesToStreamingAssets() {
            // 当前编辑器平台决定读取哪一份裁剪后的 AOT 产物。
            var target = EditorUserBuildSettings.activeBuildTarget;
            // 上一次构建主包后产生的裁剪版 AOT DLL 目录。
            string aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            // 本项目收集 DLL 资源的目录；函数名称中的 StreamingAssets 不是当前实际目标。
            string aotAssembliesDstDir = Application.dataPath + "/LYF_Game/Dlls";
            Debug.Log($"aotAssembliesDstDir::{aotAssembliesDstDir}");
            // 本次需要补充元数据的程序集名，由 HybridCLR 配置提供。
            List<string> dllNames = SettingsUtil.AOTAssemblyNames;
            // i 是当前待复制的 AOT 程序集索引。
            for (int i = 0; i < dllNames.Count; i++) {
                // AOT 配置名不带 .dll 后缀，源文件和目标资源共用此名。
                string dll = dllNames[i];
                // 当前程序集的裁剪后 DLL 路径；缺失时提示先构建主包。
                string srcDllPath = $"{aotAssembliesSrcDir}/{dll}.dll";
                if (!File.Exists(srcDllPath)) {
                    Debug.LogError($"ab中添加AOT补充元数据dll:{srcDllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                    continue;
                }
                // 复制后的资源路径，以 .dll.bytes 结尾，保留 DLL 原始字节。
                string dllBytesPath = $"{aotAssembliesDstDir}/{dll}.dll.bytes";
                CopyAssemblyBytes(srcDllPath, dllBytesPath);
                Debug.Log($"[CopyAOTAssembliesToStreamingAssets] copy AOT dll {srcDllPath} -> {dllBytesPath}");
            }
        }

        // 把当前平台的热更 DLL 复制到 Assets/LYF_Game/Dlls，作为后续 YooAsset 打包输入。
        public static void CopyHotUpdateAssembliesToStreamingAssets() {
            // 当前编辑器平台决定读取哪一份热更编译产物。
            var target = EditorUserBuildSettings.activeBuildTarget;

            // HybridCLR 编译出的当前平台热更程序集目录。
            string hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);

            // 热更 DLL 对应的 Unity 资源目录。
            string hotfixAssembliesDstDir = Application.dataPath + "/LYF_Game/Dlls";
            // 本次实际分发的热更 DLL 文件名，已排除保留程序集。
            List<string> dllFiles = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved;
            // i 是当前待复制的热更文件索引。
            for (int i = 0; i < dllFiles.Count; i++) {
                // 热更文件名自带 .dll 后缀，目标资源只需再追加 .bytes。
                string dll = dllFiles[i];
                // 本次需要复制的热更程序集源路径。
                string dllPath = $"{hotfixDllSrcDir}/{dll}";
                // 给 DLL 文件名加 .bytes 后缀，使 Unity 将其作为二进制资源导入。
                string dllBytesPath = $"{hotfixAssembliesDstDir}/{dll}.bytes";
                CopyAssemblyBytes(dllPath, dllBytesPath);
                Debug.Log($"[CopyHotUpdateAssembliesToStreamingAssets] copy hotfix dll {dllPath} -> {dllBytesPath}");
            }
        }

        /// <summary>复制供 Unity 导入的 DLL，遇到 Windows 文件映射占用时短暂等待读取方释放。</summary>
        /// <param name="sourcePath">当前平台生成的 AOT 或热更 DLL 路径。</param>
        /// <param name="destinationPath">保留原有资源 GUID 的 .dll.bytes 文件路径。</param>
        private static void CopyAssemblyBytes(string sourcePath, string destinationPath) {
            // Windows ERROR_USER_MAPPED_FILE（1224）在当前 Unity Mono 中对应的 HRESULT。
            const int mappedFileHResult = unchecked((int)0x800704C8);
            // 单个文件最多重试五次，耗尽后由 File.Copy 抛出原异常。
            const int maxRetries = 5;
            // 映射占用时每次等待的毫秒数，五次合计一秒。
            const int retryDelayMilliseconds = 200;
            // 首次复制时为 0，之后记录该文件因映射占用而重试的次数。
            for (int retry = 0; ; retry++) {
                try {
                    File.Copy(sourcePath, destinationPath, true);
                    return;
                }
                // 只有目标仍有打开的映射才等待；其他读写错误直接交给构建入口。
                catch (IOException error) when (error.HResult == mappedFileHResult && retry < maxRetries) {
                    AssetDatabase.ReleaseCachedFileHandles();
                    System.Threading.Thread.Sleep(retryDelayMilliseconds);
                }
            }
        }

        // 把目标平台 target 的示例 prefabs 资源包复制到 StreamingAssets；当前 DLL 构建入口未调用。
        public static void CopyAssetBundlesToStreamingAssets(BuildTarget target) {
            // 安装包内 StreamingAssets 的目标目录。
            string streamingAssetPathDst = Application.streamingAssetsPath;
            Directory.CreateDirectory(streamingAssetPathDst);
            // 待复制资源包所在的平台输出目录。
            string outputDir = GetAssetBundleOutputDirByTarget(target);
            // 需要随安装包交付的示例资源包名。
            var abs = new string[] { "prefabs" };
            // ab 是当前要复制的资源包名。
            foreach (var ab in abs) {
                // AssetDatabase 能识别的资源包源路径。
                string srcAb = ToRelativeAssetPath($"{outputDir}/{ab}");
                // AssetDatabase 能识别的 StreamingAssets 目标路径。
                string dstAb = ToRelativeAssetPath($"{streamingAssetPathDst}/{ab}");
                Debug.Log($"[CopyAssetBundlesToStreamingAssets] copy assetbundle {srcAb} -> {dstAb}");
                AssetDatabase.CopyAsset(srcAb, dstAb);
            }
        }
        #endregion
    }
}
