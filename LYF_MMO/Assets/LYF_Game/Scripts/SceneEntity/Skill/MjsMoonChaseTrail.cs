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
    /// <param name="origin">角色起手时的世界坐标，用于保留残影。</param>
    /// <param name="forward">起手时冻结的水平单位方向，指向敌人或空放的前方。</param>
    /// <param name="end">锁敌时是敌人脚下坐标，空放时是前冲终点，单位米。</param>
    internal void Aim(Vector3 origin, Vector3 forward, Vector3 end)
    {
        // 立体距离足够时从角色身上起剑，紧贴目标时把起笔向后延长，残影仍在施法处。
        float distance = Vector3.Distance(end, origin);
        // 主剑锋网格的根在局部胸口高度，起笔与目标星芒以世界空间胸口对齐。
        Vector3 start = origin + Vector3.up * 0.91f - forward * Mathf.Max(0f, 4f - distance);
        // 剑锋实际瞄准的三维向量，支持正上方、正下方和斜向目标。
        Vector3 ray = end + Vector3.up * 0.98f - start;
        // 用施法者水平右方确定剑身滚转，垂直出剑也有稳定的局部上方。
        Vector3 bladeUp = Vector3.Cross(ray, Vector3.Cross(Vector3.up, forward));
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
        echo.SetPositionAndRotation(origin, Quaternion.LookRotation(forward));
        for (int i = 0; i < sparks.Length; i++)
        {
            // 目标闪点先于追身，在快速折返起步前标出剑光落点。
            ParticleSystem spark = sparks[i];
            spark.transform.position = end + Vector3.up * 0.98f;
            // 相对剑光出生的粒子模拟秒数，按预制体 1.5 倍速约合实际 43 毫秒。
            ParticleSystem.MainModule main = spark.main;
            main.startDelay = 0.065f;
        }
    }
}
