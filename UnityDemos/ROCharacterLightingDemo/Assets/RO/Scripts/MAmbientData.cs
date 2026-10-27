using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 世界环境数据（对应 RO 源工程 MoonSerializable/Effect/MAmbientData.cs，原样复制后去掉 namespace）。
/// 源实现的 MEnvorimentSetting.ApplyWorldAmbient 用它的字段整体设置 RenderSettings（环境模式/三色环境光/强度/天空盒/反射模式），
/// 这是角色 Shader 里 ShadeSH9 环境项的上游来源，因此必须跟着迁入，不能只留一个"环境光强度"滑杆。
/// </summary>
public class MAmbientData : ScriptableObject
{
    /// <summary>天空盒材质，赋给 RenderSettings.skybox。
    /// 注意真实行为：ApplyWorldAmbient 是直接赋值，字段为空时会把 RenderSettings.skybox 置为 null（清空场景天空盒），
    /// 不是"保持原有天空盒"。源资产 gi_sc_pulongdela_001_baitian 的该字段就是空值（{fileID: 0}），Demo 照原样保留空。</summary>
    public Material skybox;

    /// <summary>Unity 环境光模式：Skybox=0、Trilight=1、Flat=3、Custom=4。源资产默认 Trilight。</summary>
    public AmbientMode ambientMode = AmbientMode.Trilight;

    /// <summary>环境光天空色（Flat 模式下就是唯一的常量环境色），赋给 RenderSettings.ambientLight 与 ambientSkyColor。
    /// 想调 Flat 的整体亮度就改这个值（乘性放大整条环境项），不要改 ambientIntensity。</summary>
    public Color ambientSkyColor = Color.gray;

    /// <summary>环境光赤道色（Trilight 模式使用）。</summary>
    public Color ambientEquatorColor = Color.gray;

    /// <summary>环境光地面色（Trilight 模式使用）。</summary>
    public Color ambientGroundColor = Color.gray;

    /// <summary>Unity 环境光强度系数，赋给 RenderSettings.ambientIntensity，语义随 ambientMode 变化：
    /// 只有 Skybox 模式（ambientMode=0）它才乘在"由天空盒生成的环境光"上；Flat 模式（ambientMode=3）用的常量色是
    /// ambientLight（ApplyWorldAmbient 里等于 ambientSkyColor），ambientIntensity 不参与，改它不会改变画面。
    /// Trilight（1）的实测结果见 README §2.4，不在此凭经验断言。</summary>
    public float ambientIntensity = 1f;

    /// <summary>默认反射来源模式：Custom 时用场景/资产指定的立方图，Skybox 时用天空盒生成。</summary>
    public DefaultReflectionMode reflectionMode = DefaultReflectionMode.Custom;
}
