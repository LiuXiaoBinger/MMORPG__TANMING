using System;
using UnityEngine;

/// <summary>技能编辑器碰撞轨道的正式数据，仅驱动客户端受击表现。</summary>
[Serializable]
public class SkillRuntimeCollision
{
    /// <summary>包含的起始帧，按该记录 FrameRate 换算秒数。</summary>
    public int StartFrame;
    /// <summary>包含的结束帧，单帧判定时与起始帧相同。</summary>
    public int EndFrame;
    /// <summary>作者时间轴帧率，与技能编辑器一致。</summary>
    public float FrameRate;
    /// <summary>0 为球体，1 为有向盒体，与原编辑器枚举一致。</summary>
    public int Shape;
    /// <summary>球体半径，单位世界米，不乘角色缩放。</summary>
    public float Radius;
    /// <summary>盒体完整宽高深，单位世界米。</summary>
    public Vector3 BoxSize;
    /// <summary>相对施法者旋转的盒体欧拉角，单位度。</summary>
    public Vector3 Rotation;
    /// <summary>从起始帧到结束帧的本地中心偏移，包含编辑器采样的骨骼轨迹。</summary>
    public Vector3[] Offsets;
    /// <summary>零表示每个目标单次命中，正数表示允许再次命中的帧间隔。</summary>
    public int RepeatFrames;
    /// <summary>是否在本次施法选定的目标落点做范围查询。</summary>
    public bool AtTarget;
    /// <summary>目标不存在时相对施法者的前方距离，单位米。</summary>
    public float ForwardDistance;
}
