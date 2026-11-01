using UnityEngine;

/// <summary>驱动单个展示特效的挂点跟随、预警材质进度和原生粒子播放。</summary>
public sealed class ROCreatureEffectPlayback : MonoBehaviour
{
    /// <summary>本实例采用的原技能特效事件，只在本实例存活期间使用。</summary>
    private ROCreatureEffectDefinition definition;

    /// <summary>施法者或目标预览点，供原配置要求跟随时更新位置。</summary>
    private Transform anchor;

    /// <summary>生成时刻的游戏时间，用于计算预警填充比例，单位为秒。</summary>
    private float startedAt;

    /// <summary>实例中的渲染器缓存，用于更新 _Params 与 _Progress。</summary>
    private Renderer[] effectRenderers;

    /// <summary>复用的材质属性块，避免为播放预警复制共享材质。</summary>
    private MaterialPropertyBlock properties;

    /// <summary>配置实例的初始位置、原版材质参数、粒子状态和回收时间。</summary>
    /// <param name="effect">本次动作保存的强类型特效事件。</param>
    /// <param name="effectAnchor">按事件选择的施法者根节点或目标预览点。</param>
    public void Initialize(ROCreatureEffectDefinition effect, Transform effectAnchor)
    {
        properties = new MaterialPropertyBlock();
        definition = effect;
        anchor = effectAnchor;
        startedAt = Time.time;
        transform.position = anchor.position + anchor.rotation * definition.offset;
        transform.rotation = anchor.rotation * Quaternion.Euler(0f, definition.yaw, 0f);
        transform.localScale = definition.scale;
        effectRenderers = GetComponentsInChildren<Renderer>(true);
        // i 是当前实例渲染器的索引；预警的尺寸写入 Shader 参数而非放大整个网格。
        for (int i = 0; i < effectRenderers.Length; i++)
        {
            // 当前渲染器的原版共享材质，仅查询属性，不修改资源文件。
            Material material = effectRenderers[i].sharedMaterial;
            if (material == null)
            {
                continue;
            }
            if (material.HasProperty("_Params"))
            {
                effectRenderers[i].GetPropertyBlock(properties);
                // 保留原属性块的 w 通道，只更新原 MFx 写入的 xyz 尺寸参数。
                Vector4 parameters = properties.GetVector("_Params");
                parameters.x = definition.shaderParameters.x;
                parameters.y = definition.shaderParameters.y;
                parameters.z = definition.shaderParameters.z;
                properties.SetVector("_Params", parameters);
                effectRenderers[i].SetPropertyBlock(properties);
                properties.Clear();
                transform.localScale = Vector3.one;
            }
        }
        ApplyWarningProgress();
        // 实例包含的所有原生粒子系统；逐个启动自身，保留各自原有的开始延迟。
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        // i 是粒子系统索引，false 避免父系统重复重启子系统。
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play(false);
        }
        Destroy(gameObject, definition.duration);
    }

    /// <summary>按原配置跟随挂点并推进范围预警，普通粒子由 Unity 自行更新。</summary>
    private void Update()
    {
        if (definition.followPosition)
        {
            transform.position = anchor.position + anchor.rotation * definition.offset;
        }
        if (definition.followRotation)
        {
            transform.rotation = anchor.rotation * Quaternion.Euler(0f, definition.yaw, 0f);
        }
        ApplyWarningProgress();
    }

    /// <summary>沿用 RO MFx 的线性预警进度，只向支持 _Progress 的材质写入实例属性。</summary>
    private void ApplyWarningProgress()
    {
        if (definition.warningDuration <= 0f)
        {
            return;
        }
        // 从配置的初始内外径比例线性增长，经过 warningDuration 秒后填满。
        float progress = Mathf.Clamp01(definition.warningInitialRatio + (Time.time - startedAt) * (1f - definition.warningInitialRatio) / definition.warningDuration);
        // i 是支持预警显示的候选渲染器索引。
        for (int i = 0; i < effectRenderers.Length; i++)
        {
            // 当前原版材质，缺少预警通道的渲染器保持原有显示。
            Material material = effectRenderers[i].sharedMaterial;
            if (material == null)
            {
                continue;
            }
            if (!material.HasProperty("_Progress"))
            {
                continue;
            }
            effectRenderers[i].GetPropertyBlock(properties);
            properties.SetFloat("_Progress", progress);
            effectRenderers[i].SetPropertyBlock(properties);
            properties.Clear();
        }
    }
}
