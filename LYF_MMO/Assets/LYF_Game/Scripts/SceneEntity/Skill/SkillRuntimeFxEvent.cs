using System;
using UnityEngine;

/// <summary>
/// 技能时间轴上的一次粒子特效：何时生成、使用哪个预制体，以及生成位置和朝向。
/// </summary>
[Serializable]
public sealed class SkillRuntimeFxEvent
{
    /// <summary>编辑粒子列表中的原序号，用于同帧事件稳定排序；原剪辑事件序号另存于编辑绑定参考。</summary>
    public int SourceEventIndex;

    /// <summary>Y 编辑器时间轴上的触发帧，从零开始。</summary>
    public int Frame;

    /// <summary>从本次技能表现开始计时的触发秒数，非负。</summary>
    public float TimeSeconds;

    /// <summary>要生成的粒子根预制体，由表现资源保持依赖引用。</summary>
    public ParticleSystem Particle;

    /// <summary>粒子相对角色、挂点或世界的定位规则。</summary>
    public SkillFxPlacementKind PlacementKind;

    /// <summary>NamedSocket 模式下相对角色模型根节点的完整 Transform 路径。</summary>
    public string SocketPath;

    /// <summary>相对定位节点或世界落点的位移，单位为 Unity 米。</summary>
    public Vector3 LocalPosition;

    /// <summary>粒子生成时相对定位节点或世界轴的欧拉角，单位为度。</summary>
    public Vector3 LocalEulerAngles;

    /// <summary>粒子实例的局部缩放，逐轴倍率；默认三个轴均为 1。</summary>
    public Vector3 LocalScale = Vector3.one;

    /// <summary>CasterForwardWorld 模式沿角色朝向额外移动的距离，单位为 Unity 米。</summary>
    public float ForwardDistance;
}
