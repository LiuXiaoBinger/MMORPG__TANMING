using UnityEngine;

/// <summary>
/// 时间段环境数据。对应 RO 源工程 MDayTimeData，
/// 只保留魔神塔场景实际使用的字段：主光/可视光方向、场景色偏、云草树环境色、
/// 影色/日色、地形叠加色，以及角色光数据引用。
/// 源数据中的天空球、后处理、大气散射字段本场景未接入，故不移植。
/// </summary>
public class MDayTimeData : ScriptableObject
{
    /// <summary>
    /// 时间段类型。决定雾色等参数走哪个分支，本场景为 Dusk（黄昏）。
    /// </summary>
    public MDaytimeType daytimeType = MDaytimeType.Dusk;

    /// <summary>
    /// 主光方向，单位：角度。x 为俯角、y 为方位角（与 RO 源字段语义一致）。
    /// 应用时按 Quaternion.Euler(mainLightDir) * Vector3.back 转成方向向量，
    /// 不能把角度直接当向量使用。
    /// </summary>
    public Vector2 mainLightDir = new Vector2(65f, 135f);

    /// <summary>
    /// 可视光（第二路光）方向，单位：角度。x 为俯角、y 为方位角。
    /// 应用时写入全局 _ROCLightDir1，供场景着色器做辅助补光。
    /// </summary>
    public Vector2 visLightDir = new Vector2(30f, 135f);

    /// <summary>
    /// 场景色偏。应用时写入全局 _ROCSceneColor，
    /// 与场景着色器的贴图颜色相乘，决定整体色调。a 为权重，保留源值语义。
    /// </summary>
    public Color sceneColor = Color.gray;

    /// <summary>
    /// 角色光数据引用。非空时把其四路颜色写入角色相关全局变量，
    /// 并把其 reflectMap 设为场景自定义反射。
    /// </summary>
    public MCreatureLightData creatureLightData;

    /// <summary>
    /// 日色（太阳色）。应用时写入全局 _ROCSunColor；a 为强度权重，保留源值语义。
    /// </summary>
    public Color sunColor = new Color(1f, 0.96f, 0.84f, 0.1f);

    /// <summary>
    /// 影色。应用时写入全局 _ROCShadowColor，决定阴影区域的色偏与浓度（a 为浓度）。
    /// </summary>
    public Color shadowColor = new Color(0.33f, 0.35f, 0.35f, 1f);

    /// <summary>
    /// 云的环境色。应用时写入全局 _ROCloudEnvorimentColor，
    /// 云着色器用它乘环境光/日光，为 (0,0,0,0) 时云会整体变黑。
    /// </summary>
    public Color cloudEnvorimentColor = Color.white;

    /// <summary>
    /// 草地的环境色。应用时写入全局 _ROGrassEnvorimentColor（本场景无草，仅保持通道一致）。
    /// </summary>
    public Color grassEnvorimentColor = Color.white;

    /// <summary>
    /// 草地暗部环境色。应用时写入全局 _ROGrassEnvorimentDarkColor。
    /// </summary>
    public Color grassEnvorimentDarkColor = Color.white;

    /// <summary>
    /// 树木的环境色。应用时写入全局 _ROTreeEnvorimentColor。
    /// </summary>
    public Color treeEnvorimentColor = Color.white;

    /// <summary>
    /// 地形亮部叠加色（HDR，可大于 1）。应用时写入全局 _ROTerrainLightColor，
    /// 与地形光照图叠加，保留源数据的高动态范围数值。
    /// </summary>
    public Color terrainLightColor = Color.white;

    /// <summary>
    /// 地形暗部叠加色。应用时写入全局 _ROTerrainDarkColor。
    /// </summary>
    public Color terrainDarkColor = Color.white;

    /// <summary>
    /// 地形明暗分界阈值，范围 0~1。应用时写入全局 _ROTerrainThreshold，
    /// 决定叠加亮部色还是暗部色。
    /// </summary>
    public float terrainThreshold = 0.25f;
}
