using UnityEngine;

/// <summary>逐月起手剑光的预制体锚点，在粒子启动前分别放置剑锋、原地残影和目标星芒。</summary>
public sealed class MjsMoonChaseTrail : MonoBehaviour
{
    /// <summary>原版剑锋网格从根节点到闪点的轴向长度，单位米。</summary>
    private const float AuthoredLength = 7.91f;
    /// <summary>三条沿局部 Z 轴延伸的剑锋，只沿长度轴伸缩。</summary>
    [SerializeField] private Transform[] blades;
    /// <summary>目标胸前的一簇星芒，保持原始尺寸。</summary>
    [SerializeField] private ParticleSystem[] sparks;
    /// <summary>停留在施法位置的人形残影，保持角色原始比例。</summary>
    [SerializeField] private Transform echo;

    /// <summary>按实际距离把剑光从起手处送到目标，先亮星芒再让人物追身。</summary>
    /// <param name="path">本次锁定目标的起点、方向与命中位置。</param>
    internal void Aim(MjsMoonChasePath path)
    {
        // 立体距离足够时从角色身上起剑，紧贴目标时把起笔向后延长，残影仍在施法处。
        float distance = Vector3.Distance(path.LightTarget, path.Origin);
        // 主剑锋网格的根在局部胸口高度，起笔与目标星芒以世界空间胸口对齐。
        Vector3 start = path.Origin + Vector3.up * 0.91f - path.Forward * Mathf.Max(0f, 4f - distance);
        // 剑锋实际瞄准的三维向量，支持正上方、正下方和斜向目标。
        Vector3 ray = path.LightTarget + Vector3.up * 0.98f - start;
        // 用施法者水平右方确定剑身滚转，垂直出剑也有稳定的局部上方。
        Vector3 bladeUp = Vector3.Cross(ray, Vector3.Cross(Vector3.up, path.Forward));
        // 将网格自身胸口偏移从根节点位置扣除，陡角度时仍从手边发光。
        Quaternion rotation = Quaternion.LookRotation(ray, bladeUp);
        transform.SetPositionAndRotation(start - rotation * (Vector3.up * 0.91f), rotation);
        // 世界距离与制作距离之比，作用于三层丝光的纵向尺寸和起笔偏移。
        float scale = ray.magnitude / AuthoredLength;
        for (int i = 0; i < blades.Length; i++)
        {
            // 保留该层的横向宽度及高度，让近远施放看起来是同一把剑。
            Transform blade = blades[i];
            // 各层的局部轴向起点也随施法距离缩放。
            Vector3 local = blade.localPosition;
            local.z *= scale;
            blade.localPosition = local;
            blade.localScale = Vector3.Scale(blade.localScale, new Vector3(1f, 1f, scale));
        }
        echo.SetPositionAndRotation(path.Origin, Quaternion.LookRotation(path.Forward));
        for (int i = 0; i < sparks.Length; i++)
        {
            // 目标闪点比角色追身提前约八十毫秒。
            ParticleSystem spark = sparks[i];
            spark.transform.position = path.LightTarget + Vector3.up * 0.98f;
            // 时间相对剑光出生，以秒计。
            ParticleSystem.MainModule main = spark.main;
            main.startDelay = 0.105f;
        }
    }
}
