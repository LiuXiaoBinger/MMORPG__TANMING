using System.IO;
using UnityEditor.Build;
using UnityEngine;

/// <summary>把 GM_Game 中的导航网格随 Player 构建放到现有导航读取目录。</summary>
public sealed class NavigationStreamingAssetsBuildProcessor : BuildPlayerProcessor
{
    /// <summary>使用默认顺序收录导航文件，数据不依赖其他构建回调的产物。</summary>
    public override int callbackOrder => 0;

    /// <summary>为本轮 Player 构建登记源目录中的各场景导航网格。</summary>
    /// <param name="buildPlayerContext">当前 Player 构建的文件收录上下文。</param>
    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
    {
        // Unity 项目中导航源文件的绝对目录。
        string navigationDirectory = Path.Combine(Application.dataPath, "GM_Game", "NavigationData");
        // 源目录中按场景保存的导航三角网二进制文件。
        string[] navigationFiles = Directory.GetFiles(navigationDirectory, "*.navmesh.bytes");
        // 当前导航源文件在构建列表中的索引。
        for (int index = 0; index < navigationFiles.Length; index++)
        {
            // Player 的 StreamingAssets 相对路径，沿用场景名和原读取目录。
            string streamingAssetPath = "NavigationData/" + Path.GetFileName(navigationFiles[index]);
            buildPlayerContext.AddAdditionalPathToStreamingAssets(navigationFiles[index], streamingAssetPath);
        }
    }
}
