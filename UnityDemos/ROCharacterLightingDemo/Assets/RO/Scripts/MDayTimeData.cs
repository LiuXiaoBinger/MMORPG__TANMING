using UnityEngine;

/// <summary>
/// 时段（天色）数据（对应 RO 源工程 MoonSerializable/Effect/MDayTimeData.cs，按字段原样复制、去掉 namespace）。
/// 源工程里它是 Assets/artres/Resources/SceneEnviroData/dt_*.asset 的资产类型，一个资产 = 一个场景的一个时段；
/// 本 Demo 用它的角色光字段 + 主光/可见光方向驱动 RO 角色 Shader，其余字段（后处理/天空球/地形）只保留数据不接实现。
/// </summary>
public class MDayTimeData : ScriptableObject
{
    /// <summary>时段分类（白天/夜晚/黄昏/清晨），源实现用它选天气雾色与物件隔离，角色光照链只用于记录。</summary>
    public MEnvorimentData.DaytimeType daytimeType;

    /// <summary>主光方向：x = 俯角，y = 方位角（单位：度）。源实现换算成世界方向 Quaternion.Euler(mainLightDir) * Vector3.back 写进 _ROCLightDir0。</summary>
    public Vector2 mainLightDir;

    /// <summary>可见光方向（度），写进 _ROCLightDir1，只被雾/米氏散射读取，角色 Shader 不读它。</summary>
    public Vector2 visLightDir;

    /// <summary>月亮位置参数（xyz），源实现供天空球/天气链使用，角色光照链不读。</summary>
    public Vector3 moonParam = new Vector3(20f, 120f, 1f);

    /// <summary>场景整体色偏，写进 _ROCSceneColor 给场景 Shader（地面/物件）用，角色 Shader 不读。</summary>
    public Color sceneColor = Color.gray;

    /// <summary>角色光信息（四色 + 反射来源），源实现里作为子资产被本资产引用，是角色光照的真正数据源。</summary>
    public MCreatureLightData creatureLightData;

    /// <summary>暖阳色调，写进 _ROCSunColor 给场景 Shader，角色 Shader 不读。</summary>
    public Color sunColor = new Color(1f, 0.96f, 0.84f, 0.1f);

    /// <summary>阴影色，写进 _ROCShadowColor 给场景 Shader 的阴影项，角色 Shader 不读。</summary>
    public Color shadowColor = new Color(0x53 / 255f, 0x5A / 255f, 0x5A / 255f, 1f);

    /// <summary>云的环境色，写进 _ROCloudEnvorimentColor 给场景 Shader，角色 Shader 不读。</summary>
    public Color cloudEnvorimentColor = Color.white;

    /// <summary>草的环境色，写进 _ROGrassEnvorimentColor 给场景 Shader，角色 Shader 不读。</summary>
    public Color grassEnvorimentColor = Color.white;

    /// <summary>草的暗部环境色，写进 _ROGrassEnvorimentDarkColor 给场景 Shader，角色 Shader 不读。</summary>
    public Color grassEnvorimentDarkColor = Color.white;

    /// <summary>树的环境色，写进 _ROTreeEnvorimentColor 给场景 Shader，角色 Shader 不读。</summary>
    public Color treeEnvorimentColor = Color.white;

    /// <summary>是否由相机天空盒接管背景，源实现传给 ApplyCamSetting；角色光照链无影响。</summary>
    public bool useCamSkybox = true;

    /// <summary>相机背景色（useCamSkybox 为 false 时生效），源实现 ApplyCamSetting 直接用；角色光照链无影响。</summary>
    public Color camSkyboxColor = Color.black;

    /// <summary>泛光强度，源实现交给 MPostEffectMgr.Bloom（后处理链未迁入）。</summary>
    public float bloomIntensity = 4f;

    /// <summary>色温偏移（-100~100），源实现交给 ColorGrading（后处理链未迁入）。</summary>
    [Range(-100f, 100f)]
    public float temperature = 0f;

    /// <summary>色调偏移（-100~100），后处理链未迁入。</summary>
    [Range(-100f, 100f)]
    public float tint = 5f;

    /// <summary>饱和度（-100~100），后处理链未迁入。</summary>
    [Range(-100f, 100f)]
    public float saturation = 0f;

    /// <summary>曝光（-8~8），后处理链未迁入；本 Demo 不做任何曝光调整，亮度差异只能来自 RO 参数。</summary>
    [Range(-8f, 8f)]
    public float exposure = 1.33f;

    /// <summary>对比度（-100~100），后处理链未迁入。</summary>
    [Range(-100f, 100f)]
    public float contrast = -9f;

    /// <summary>是否开启热扰动，后处理链未迁入。</summary>
    public bool enableHeatDistortion;

    /// <summary>地形烘焙亮色，写进 _ROTerrainLightColor 给场景 Shader。</summary>
    [ColorUsageAttribute(false, true)]
    public Color terrainLightColor = Color.white;

    /// <summary>地形烘焙暗色，写进 _ROTerrainDarkColor 给场景 Shader。</summary>
    [ColorUsageAttribute(false, true)]
    public Color terrainDarkColor = Color.white;

    /// <summary>地形明暗阈值，写进 _ROTerrainThreshold 给场景 Shader。</summary>
    public float terrainThreshold = 0.25f;

    /// <summary>是否使用特殊大气参数包，供天空球/天气链使用，角色光照链不读。</summary>
    public bool useSpecialDayTimeParamPack = false;

    /// <summary>天空球色调，天空球链未迁入。</summary>
    public Color skyboxTintColor = Color.gray;

    /// <summary>天空云亮色，天空球链未迁入。</summary>
    public Color skyboxCloudBrightColor = Color.white;

    /// <summary>是否启用月亮，天空球链未迁入。</summary>
    public bool enableMoon;

    /// <summary>地平线色，天空球链未迁入。</summary>
    [ColorUsageAttribute(false, true)]
    public Color horizonColor;

    /// <summary>天空色，天空球链未迁入。</summary>
    [ColorUsageAttribute(false, true)]
    public Color skyColor;

    /// <summary>天顶色，天空球链未迁入。</summary>
    [ColorUsageAttribute(false, true)]
    public Color zenithColor;

    /// <summary>地平线位置（0~1），天空球链未迁入。</summary>
    public float horizonPos = 0.1f;

    /// <summary>天顶位置（0~1），天空球链未迁入。</summary>
    public float zenithPos = 0.3f;
}
