using UnityEngine;

/// <summary>
/// 角色光照数据。对应 RO 源工程 MCreatureLightData：
/// 承载角色（主角与 NPC）卡通着色需要的四路颜色与反射立方图，
/// 由 MEnvorimentSetting 写入对应的 RO 全局 Shader 变量。
/// </summary>
public class MCreatureLightData : ScriptableObject
{
    /// <summary>
    /// 角色主光颜色。rgb 为光色，a 为亮度权重（0~1），
    /// 应用时写入全局 _ROCLightColor0，供 UTS/卡通角色着色器取样。
    /// </summary>
    public Color mainLightColor = new Color(1f, 0.96f, 0.84f, 1f);

    /// <summary>
    /// 角色背光颜色。应用时写入全局 _ROCBackColor，
    /// 用于卡通着色器的背面补光，避免背光面死黑。
    /// </summary>
    public Color backLightColor = new Color(0.9f, 0.45f, 1f, 1f);

    /// <summary>
    /// 角色环境光颜色。应用时写入全局 _ROCAmbientColor，
    /// 决定角色暗部的基础色偏（本场景为偏蓝紫）。
    /// </summary>
    public Color ambientColor = new Color(0.7f, 0.95f, 1f, 1f);

    /// <summary>
    /// 角色边缘光颜色。rgb 为边缘光色，a 为边缘光强度权重（0~1），
    /// 应用时写入全局 _ROCRimColor。
    /// </summary>
    public Color rimColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);

    /// <summary>
    /// 场景反射立方图。非空时赋给 RenderSettings.customReflection，
    /// 供依赖反射探针的材质取样；为空表示不改动当前反射设置。
    /// </summary>
    public Cubemap reflectMap;
}
