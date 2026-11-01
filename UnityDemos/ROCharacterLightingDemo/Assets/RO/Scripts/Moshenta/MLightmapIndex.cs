using UnityEngine;

/// <summary>
/// 一组渲染器的烘焙光照绑定集合。对应 RO 源工程同名类型：
/// MEnvorimentSetting.lightmapIndex 是它的数组，MLightmapData.lightmapIndex 选中其中一组，
/// 用于在不同烘焙方案（如白天/夜晚两套光照图）之间切换。
/// </summary>
[System.Serializable]
public class MLightmapIndex
{
    /// <summary>
    /// 本组内的逐渲染器绑定记录。初始为空数组，由环境组件按场景实际渲染器填充。
    /// </summary>
    public MRendererLightmapScale[] lightmapScales = new MRendererLightmapScale[0];
}
