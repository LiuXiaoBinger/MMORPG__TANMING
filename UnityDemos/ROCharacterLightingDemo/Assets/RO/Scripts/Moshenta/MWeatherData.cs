using UnityEngine;

/// <summary>
/// 天气环境数据。对应 RO 源工程 MWeatherData，
/// 只保留魔神塔场景静态天气实际使用的字段：静态雾（含黄昏时段雾）、
/// 海拔雾与散射参数、云影色、雾/云流动与风参数。
/// 源数据中的昼夜雾循环、雨雪雷电、彩虹、后处理字段本场景未接入，故不移植。
/// </summary>
public class MWeatherData : ScriptableObject
{
    /// <summary>
    /// 环境纹理覆盖的世界尺寸，单位：世界单位。
    /// 用于把雾/云的平移缩放换算成 UV 空间（应用时会取倒数并缩放），值必须大于 0。
    /// </summary>
    public int worldSize = 100;

    /// <summary>
    /// 是否用天气自带的场景色覆盖时间段色。为 true 时 _ROCSceneColor 取本类的 sceneColor。
    /// </summary>
    public bool keepSceneColor = false;

    /// <summary>
    /// 天气场景色偏。仅在 keepSceneColor 为 true 时写入全局 _ROCSceneColor，
    /// 覆盖 MDayTimeData.sceneColor。
    /// </summary>
    public Color sceneColor = Color.gray;

    /// <summary>
    /// 默认（非分时段）雾色。当不使用分时段雾或时段不匹配时生效，
    /// 写入 RenderSettings.fogColor 与全局 _ROCFogEndColor 的 rgb。
    /// </summary>
    public Color fogColor = Color.gray;

    /// <summary>
    /// 雾的混合色，与雾色按距离插值。写入全局 _ROCFogStartColor 的 rgb。
    /// </summary>
    public Color fogBlendColor = Color.gray;

    /// <summary>
    /// 混合雾的开始距离（世界单位）。写入 _ROCFogStartColor 的 a 通道。
    /// </summary>
    public float fogBlendStart = 0f;

    /// <summary>
    /// 混合雾的结束距离（世界单位）。写入 _ROCFogEndColor 的 a 通道。
    /// </summary>
    public float fogBlendEnd = 0f;

    /// <summary>
    /// 是否按时间段切换雾色。为 true 且时间段为黄昏时，使用下面 Dusk 系列参数。
    /// </summary>
    public bool enableDayTimeFog = false;

    /// <summary>
    /// 黄昏时段雾色。仅在 enableDayTimeFog 为 true 且 daytimeType 为 Dusk 时生效。
    /// </summary>
    public Color fogColorDusk = Color.gray;

    /// <summary>
    /// 黄昏时段雾的混合色。
    /// </summary>
    public Color fogBlendColorDusk = Color.gray;

    /// <summary>
    /// 黄昏时段混合雾开始距离（世界单位）。
    /// </summary>
    public float fogBlendStartDusk = 0f;

    /// <summary>
    /// 黄昏时段混合雾结束距离（世界单位）。
    /// </summary>
    public float fogBlendEndDusk = 0f;

    /// <summary>
    /// 海拔雾起始高度（世界单位）。小于等于 0 表示关闭海拔雾，
    /// 应用时写入 _ROCHeightFogParams.x 的倒数并置启用位为 0。
    /// </summary>
    public float hightFogStart = -1f;

    /// <summary>
    /// 海拔雾浓度指数。写入 _ROCHeightFogParams.y，值越大高度衰减越快。
    /// </summary>
    public float hightFogExp = 1f;

    /// <summary>
    /// 散射是否影响海拔雾。true 时 _ROCHeightFogParams.z 置 1。
    /// </summary>
    public bool camFogAffectHightFog = false;

    /// <summary>
    /// 散射雾起始距离（世界单位）。写入 _ROCRadiusFogParams.x。
    /// </summary>
    public float scatterStart = 45f;

    /// <summary>
    /// Mie 散射各向异性系数，范围约 -0.99~0.999。写入 _ROCRadiusFogParams.y，
    /// 并参与计算 _ROCMieG 的三元组。
    /// </summary>
    public float mieG = 0.85f;

    /// <summary>
    /// 瑞利散射密度。写入 _ROCRadiusFogParams.z，影响天光散射强度。
    /// </summary>
    public float rayleighDensity = 0.0025f;

    /// <summary>
    /// Mie 散射密度。写入 _ROCRadiusFogParams.w，影响主光方向的光晕强度。
    /// </summary>
    public float mieDensity = 0.5f;

    /// <summary>
    /// 雾的流动速度（UV/秒）。x/y 分别写入 _ROCMapFlow 的 x/y。
    /// </summary>
    public Vector2 fogSpeed;

    /// <summary>
    /// 云的流动速度（UV/秒）。x/y 分别写入 _ROCMapFlow 的 z/w。
    /// </summary>
    public Vector2 cloudSpeed;

    /// <summary>
    /// 云影/雪色。写入全局 _ROCMaskColor，a 为遮罩强度（0 表示不投影）。
    /// </summary>
    public Color cloudMaskColor = Color.white;

    /// <summary>
    /// 风向，单位：角度（xy 作为欧拉角输入）。应用时按
    /// Quaternion.Euler(windDirection) * Vector3.forward 转成方向向量并乘风力。
    /// </summary>
    public Vector2 windDirection = Vector2.one;

    /// <summary>
    /// 风力大小，作为风向向量的缩放系数。为 0 时场景无风。
    /// </summary>
    public float windIntensity = 0f;

    /// <summary>
    /// 风的频率。写入 _ROCWindParams.w，供顶点摆动类着色器使用。
    /// </summary>
    public float windFrequency = 0f;
}
