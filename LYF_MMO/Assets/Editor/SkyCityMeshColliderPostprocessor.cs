using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>在编辑器持续播放天空中之城时，为重新导入的网格重建场景中正在使用的物理形状。</summary>
public sealed class SkyCityMeshColliderPostprocessor : AssetPostprocessor
{
    /// <summary>天空中之城的正式场景路径，用于定位当前已加载的场景实例。</summary>
    private const string CityScenePath = "Assets/LYF_Game/Scenes/SkyCity/天空中之城.unity";

    /// <summary>网格导入结束后刷新引用它的碰撞体，脚本域重载时也恢复当前正在播放的场景。</summary>
    /// <param name="importedAssets">本批已导入的资源路径，用于匹配场景碰撞体引用的网格。</param>
    /// <param name="deletedAssets">Unity 通知的删除路径，删除的资源没有可重建的物理网格。</param>
    /// <param name="movedAssets">Unity 通知的移动目标路径，单纯移动资源不改变网格数据。</param>
    /// <param name="movedFromAssetPaths">被移动资源的旧路径，本次刷新只按当前网格引用匹配。</param>
    /// <param name="didDomainReload">本次导入是否发生脚本域重载，用于修复进入本回调前已丢失的物理形状。</param>
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        if (!EditorApplication.isPlaying) return;
        // 当前播放中的城市实例，新加载的场景由 Unity 正常创建碰撞体。
        Scene city = SceneManager.GetSceneByPath(CityScenePath);
        if (!city.isLoaded) return;
        if (!didDomainReload && !Array.Exists(importedAssets, IsMeshSource)) return;

        // 从本场景的根对象查找使用网格的碰撞体，覆盖多层浮台和建筑子节点。
        GameObject[] roots = city.GetRootGameObjects();
        // 当前城市根节点在场景根对象列表中的下标。
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            // 当前场景根节点下已经序列化的网格碰撞体，包括暂未启用的子节点。
            MeshCollider[] colliders = roots[rootIndex].GetComponentsInChildren<MeshCollider>(true);
            // 当前根节点内待检查的碰撞体下标。
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                // 当前要检查的场景碰撞体，继续沿用它自己的缩放、图层和触发配置。
                MeshCollider collider = colliders[colliderIndex];
                // 碰撞网格可能与渲染网格不同，重建时沿用碰撞体的原始引用。
                Mesh mesh = collider.sharedMesh;
                if (mesh == null) continue;
                if (!didDomainReload && Array.IndexOf(importedAssets, AssetDatabase.GetAssetPath(mesh)) < 0) continue;

                // 热导入保留 Mesh 引用，但旧物理形状已经被释放，先解除再赋回才能重新烘焙。
                collider.sharedMesh = null;
                collider.sharedMesh = mesh;
            }
        }
    }

    /// <summary>识别独立网格及内含网格的模型导入，跳过贴图等无关资源批次。</summary>
    /// <param name="assetPath">Unity 提供的项目内资源路径。</param>
    /// <returns>该资源可以提供 MeshCollider 使用的网格时返回真。</returns>
    private static bool IsMeshSource(string assetPath)
    {
        return AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(Mesh) ||
            AssetImporter.GetAtPath(assetPath) is ModelImporter;
    }
}
