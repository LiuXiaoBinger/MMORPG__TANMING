using System;
using UnityEngine;

/// <summary>原FxList或SingingFxList映射出的单次特效指令。</summary>
[Serializable]
public sealed class MSceneMonsterFxData
{
    /// <summary>原EffectTable的预制体正式地址。</summary>
    public string Path;
    /// <summary>当前阶段开始后的原触发秒数。</summary>
    public float At;
    /// <summary>原End或EffectTable.PlayTime决定的存活秒数，负数随当前阶段结束。</summary>
    public float Duration;
    /// <summary>是否随原施法者挂点移动。</summary>
    public bool Follow;
    /// <summary>相对原挂点的米制偏移。</summary>
    public Vector3 Offset;
    /// <summary>原配置逐轴缩放；未指定时为单位缩放。</summary>
    public Vector3 Scale;
    /// <summary>原受击挂点高度比例；Down为0、Middle为0.5。</summary>
    public float HeightRatio;
    /// <summary>原骨骼名，空值使用模型根。</summary>
    public string Bone;
    /// <summary>相对施法者朝向的偏转角度。</summary>
    public float Face;
    /// <summary>原预警Fx编号，零表示普通特效；原尺寸参数写入Shader的_Params。</summary>
    public int WarningType;
    /// <summary>原预警外径、内径和角度/宽度参数，不能当Transform缩放。</summary>
    public Vector3 WarningSize;
}
