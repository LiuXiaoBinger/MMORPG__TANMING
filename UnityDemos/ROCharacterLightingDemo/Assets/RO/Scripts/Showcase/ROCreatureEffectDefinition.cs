using System;
using UnityEngine;

/// <summary>一条从 RO 技能与特效表转换的展示事件，时间统一使用动作开始后的秒数。</summary>
[Serializable]
public sealed class ROCreatureEffectDefinition
{
    /// <summary>原 EffectTable 的配置编号，供资源来源和展示验收定位。</summary>
    public int effectId;

    /// <summary>已复制到测试工程的原版特效预制体。</summary>
    public GameObject prefab;

    /// <summary>动作开始后触发本事件的时间，单位为秒。</summary>
    public float startTime;

    /// <summary>实例从生成到回收的展示时长，单位为秒。</summary>
    public float duration;

    /// <summary>是否使用模型正前方的技能目标预览点；否则以施法模型为原点。</summary>
    public bool targetBased;

    /// <summary>原配置要求实例在播放期间跟随施法者或目标的位置。</summary>
    public bool followPosition;

    /// <summary>原特效表要求实例在播放期间跟随挂点朝向。</summary>
    public bool followRotation;

    /// <summary>特效表处理零值后的实际 Transform 缩放；预警使用 Vector3.one。</summary>
    public Vector3 scale = Vector3.one;

    /// <summary>相对施法者或目标的配置偏移，单位为 Unity 世界米。</summary>
    public Vector3 offset;

    /// <summary>特效相对挂点的水平转角，单位为度。</summary>
    public float yaw;

    /// <summary>原 ScaleX/Y/Z 经过表默认值填充后的 Shader _Params.xyz，保存范围预警的尺寸和角度。</summary>
    public Vector3 shaderParameters;

    /// <summary>预警填充从初始比例推进到一的时间，单位为秒；零表示普通特效。</summary>
    public float warningDuration;

    /// <summary>范围预警的初始内外径比例，来自原 ScaleY / ScaleX。</summary>
    public float warningInitialRatio;
}
