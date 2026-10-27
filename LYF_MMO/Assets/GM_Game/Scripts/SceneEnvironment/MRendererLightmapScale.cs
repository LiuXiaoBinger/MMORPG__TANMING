using UnityEngine;

/// <summary>
/// 单个渲染器的烘焙光照绑定。对应 RO 源工程同名的序列化结构：
/// 记录渲染器、它使用的光照图下标，以及该渲染器在光照图上的缩放与偏移。
/// 这三项必须成组恢复，只赋贴图而不恢复 scaleOffset 会出现烘焙错位。
/// </summary>
[System.Serializable]
public struct MRendererLightmapScale
{
    /// <summary>
    /// 被绑定的渲染器。为空时应用阶段跳过该条记录。
    /// </summary>
    public Renderer renderer;

    /// <summary>
    /// 该渲染器使用的光照图下标，对应 MLightmapData.lightmaps 数组位置。
    /// 本场景取值为 0 或 1（共两张光照图）。
    /// </summary>
    public int index;

    /// <summary>
    /// 光照图 UV 变换。xy 为缩放、zw 为偏移，写入 Renderer.lightmapScaleOffset，
    /// 决定该渲染器在光照图图集中的位置。
    /// </summary>
    public Vector4 scaleOffset;
}
