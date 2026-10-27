using UnityEngine;

/// <summary>
/// 角色光照颜色数据（对应 RO 源工程 MoonSerializable/Effect/MCreatureLightData.cs，字段与默认值原样复制后去掉 namespace）。
/// 一个资产实例就是一套「角色光四色 + 反射来源」，由 MDayTimeData.creatureLightData 引用；
/// 四个 Color 的 rgb 是颜色，a 在 Shader 里另有语义（见各字段注释），不是统一的透明度。
///
/// 可编辑性适配（本 Demo 追加，非源类内容）：源类没有任何 Inspector 特性，源侧这些数值由美术资产/工具直接写入；
/// 这里给四个颜色字段加 [ColorUsage(true, true)]，作用只是让 Inspector 的颜色选择器允许输入大于 1 的分量（HDR 选择器），
/// 否则 Unity 的普通 ColorField 会把输入夹到 0~1，导致无法在 Inspector 里手工复现"背光放大到 1.49"这类源数值。
/// 该特性只影响 Inspector 输入上限，不改变 Shader、不改变任何算法、也不新增任何"亮度滑杆"。
/// </summary>
public class MCreatureLightData : ScriptableObject
{
    /// <summary>主光颜色：rgb 被角色 Shader 当作直射项颜色乘进漫反射（CreatureCommon.cginc:1090、:1100）；
    /// a 是"主光强度" lightInten，只参与 directFront = 2*a*rgb，而 directFront 只用于背光/环境补偿（:1085、:1088），不缩放直射亮度。</summary>
    [ColorUsage(true, true)]
    public Color mainLightColor = new Color(1f, 0.96f, 0.84f, 1f);

    /// <summary>背光颜色：rgb 是背光（暗部补偿）颜色，a=backTint 决定它和主光/环境结果混合的权重（:1086、:1089）。
    /// 把 rgb 同乘一个系数是"同色相下整体变暗/变亮"的手工入口（实测 ×0.35 → 57.6%、×1.7 → 149.6%）。</summary>
    [ColorUsage(true, true)]
    public Color backLightColor = new Color(0.9f, 0.45f, 1f, 1f);

    /// <summary>环境色：rgb 用来替换球谐（SH）环境结果，a 是替换比例，0=全用 SH，1=全用本颜色（:560）。</summary>
    [ColorUsage(true, true)]
    public Color ambientColor = new Color(0.7f, 0.95f, 1f, 1f);

    /// <summary>边缘光颜色：rgb 只写进顶点插值量 f.rim（:483），当前 Player 主路径片元阶段没有消费者，改它不会改变画面。</summary>
    [ColorUsage(true, true)]
    public Color rimColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);

    /// <summary>反射立方图：源 :470 读入、:523 赋给 RenderSettings.customReflection（2023 等价属性 customReflectionTexture），
    /// 进而成为 unity_SpecCube0。角色 Shader 在 CreatureCommon.cginc:1051 用 ReflectionIBL 采样它、
    /// :1103 把 indirectSpecular 合成进最终颜色，属于 PBR 间接高光，与 UTS2 的 matcap 分支无关。
    /// 该通道只在 defaultReflectionMode=Custom 时被 Unity 采用（源 gi_*.asset 里就是 Custom）。</summary>
    public Cubemap reflectMap;
}
