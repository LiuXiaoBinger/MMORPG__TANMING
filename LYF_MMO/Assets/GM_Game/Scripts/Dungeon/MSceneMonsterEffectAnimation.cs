using System;
using UnityEngine;

/// <summary>保留四个原特效实际使用的FxAnimationHelper序列化字段和时间轴语义。</summary>
public sealed class MSceneMonsterEffectAnimation : MonoBehaviour
{
    /// <summary>原特效内部轨道；Unity对象引用属于预制体序列化边界。</summary>
    [Serializable]
    private sealed class Track
    {
        /// <summary>原控制种类：0位置、2缩放、3显隐、4颜色、7材质浮点。</summary>
        public int m_ControlType;
        /// <summary>原轨道绑定的Transform、GameObject或Renderer；仅用于Unity序列化适配。</summary>
        public UnityEngine.Object m_Body;
        /// <summary>原材质属性名称。</summary>
        public string m_Property;
        /// <summary>原延迟秒数。</summary>
        public float m_Delay;
        /// <summary>原单轮持续秒数。</summary>
        public float m_Duration;
        /// <summary>原循环次数，负数持续循环，当前资源只用-1和1。</summary>
        public int m_Loops;
        /// <summary>0为线性插值、1为原单曲线乘初始向量。</summary>
        public int m_LerpType;
        /// <summary>原初始标量/向量；显隐类型存相对秒数列表。</summary>
        public float[] m_From;
        /// <summary>原结束标量/向量；显隐类型存每个时刻的开关。</summary>
        public float[] m_To;
        /// <summary>原颜色渐变，仅颜色轨道使用首项。</summary>
        public Gradient[] m_ColorGrad;
        /// <summary>原自定义变化曲线。</summary>
        public AnimationCurve[] m_Curves;
        /// <summary>下一条未消费显隐事件，避免每帧重新开关已到时的对象。</summary>
        [NonSerialized] public int NextVisible;
    }

    /// <summary>保持原m_Helpers字段名，使导入预制体的轨道数据原样绑定。</summary>
    [SerializeField] private Track[] m_Helpers;
    /// <summary>原特效是否在激活时播放。</summary>
    [SerializeField] private bool m_PlayOnEnable = true;
    /// <summary>当前启用时刻，回收后再次启用从零播放。</summary>
    private float startedAt;
    /// <summary>材质轨道共享临时属性块，每次先读取当前Renderer已有值。</summary>
    private MaterialPropertyBlock block;

    /// <summary>材质属性块含Unity原生句柄，必须在主线程Awake建立。</summary>
    private void Awake()
    {
        block = new MaterialPropertyBlock();
    }

    /// <summary>记录原特效本轮起点，预制体实例不跨技能共享。</summary>
    private void OnEnable()
    {
        startedAt = Time.time;
        for (int index = 0; index < m_Helpers.Length; index++) m_Helpers[index].NextVisible = 0;
    }

    /// <summary>按原轨道更新位置、缩放、颜色、显隐和材质值。</summary>
    private void Update()
    {
        if (!m_PlayOnEnable) return;
        // 原时间轴已经过去的秒数。
        float elapsed = Time.time - startedAt;
        for (int index = 0; index < m_Helpers.Length; index++)
        {
            // 本资源闭包中真实存在的一条原轨道。
            Track track = m_Helpers[index];
            if (track.m_Body == null || elapsed < track.m_Delay) continue;
            // 原显隐轨道From为事件秒数，不是布尔插值端点。
            if (track.m_ControlType == 3)
            {
                // 真实原轨道绑定的对象，按时间顺序只消费一次。
                GameObject target = (GameObject)track.m_Body;
                while (track.NextVisible < track.m_From.Length && elapsed >= track.m_From[track.NextVisible])
                {
                    target.SetActive(track.m_To[track.NextVisible] > 0.5f);
                    track.NextVisible++;
                }
                continue;
            }
            // 当前轮归一化时间；原资源只有线性Ease及单曲线。
            float progress = (elapsed - track.m_Delay) / track.m_Duration;
            if (track.m_Loops < 0) progress = Mathf.Repeat(progress, 1f);
            else progress = Mathf.Clamp01(progress);
            if (track.m_ControlType == 0 || track.m_ControlType == 2)
            {
                // 原位置和缩放都绑定Transform，保持局部坐标语义。
                Transform target = (Transform)track.m_Body;
                // 原轨道初始局部向量。
                Vector3 from = new Vector3(track.m_From[0], track.m_From[1], track.m_From[2]);
                // 原轨道结束局部向量。
                Vector3 to = new Vector3(track.m_To[0], track.m_To[1], track.m_To[2]);
                // 按原线性或曲线插值得到的当前局部向量。
                Vector3 value = Vector3.LerpUnclamped(from, to, progress);
                if (track.m_LerpType == 1) value = from * track.m_Curves[0].Evaluate(progress);
                if (track.m_ControlType == 0) target.localPosition = value;
                else target.localScale = value;
                continue;
            }
            // Renderer属性块保留同材质其它属性，避免修改共享材质。
            Renderer renderer = (Renderer)track.m_Body;
            renderer.GetPropertyBlock(block);
            if (track.m_ControlType == 4) block.SetColor(track.m_Property, track.m_ColorGrad[0].Evaluate(progress));
            else if (track.m_ControlType == 7)
            {
                // 原MatFloat单曲线以From作倍率，线性轨道使用To端点。
                float value = Mathf.LerpUnclamped(track.m_From[0], track.m_To[0], progress);
                if (track.m_LerpType == 1) value = track.m_From[0] * track.m_Curves[0].Evaluate(progress);
                block.SetFloat(track.m_Property, value);
            }
            renderer.SetPropertyBlock(block);
        }
    }
}
