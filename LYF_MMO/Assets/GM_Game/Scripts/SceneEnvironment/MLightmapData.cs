using UnityEngine;

/// <summary>
/// 烘焙光照数据。对应 RO 源工程 MLightmapData：
/// 保存一组光照图贴图、光照探针，以及指向“总控组件”中渲染器分组的下标。
/// 应用时会把贴图写入 LightmapSettings.lightmaps、探针写入 LightmapSettings.lightProbes，
/// 并按 lightmapIndex 选中 MEnvorimentSetting.lightmapIndex 中的一组渲染器绑定。
/// </summary>
public class MLightmapData : ScriptableObject
{
    /// <summary>
    /// 烘焙光照图数组，顺序即 LightmapSettings.lightmaps 的下标顺序。
    /// 每个渲染器的 lightmapIndex 指向本数组；元素为空会导致该槽位无烘焙光。
    /// </summary>
    public Texture2D[] lightmaps;

    /// <summary>
    /// 与该套光照图配套的光照探针数据，赋给 LightmapSettings.lightProbes。
    /// 为空表示场景没有烘焙探针。
    /// </summary>
    public LightProbes lightProbes;

    /// <summary>
    /// 渲染器分组下标，对应 MEnvorimentSetting.lightmapIndex 数组的取值（0 起）。
    /// 本场景为 0，表示 937 条渲染器绑定都在第 0 组。
    /// </summary>
    public int lightmapIndex;
}
