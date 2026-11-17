using System;
using UnityEngine;

/// <summary>本副本原LongAttackData实际使用的四类弹道表现参数，不参与碰撞与伤害。</summary>
[Serializable]
public sealed class MSceneMonsterProjectileData
{
    /// <summary>原导出曲线的单个采样点，保持服务器资源秒数和值。</summary>
    [Serializable]
    public sealed class CurvePoint
    {
        /// <summary>曲线横轴，普通弹道为秒，Anim为归一化时间。</summary>
        public float Time;
        /// <summary>原曲线采样值。</summary>
        public float Value;
    }
    /// <summary>原弹道预制体正式资源地址。</summary>
    public string Path;
    /// <summary>1为Unreal、2为Anim贝塞尔、3为Real直线、4为Rect公转。</summary>
    public int Kind;
    /// <summary>施法者局部坐标出生偏移，单位米。</summary>
    public Vector3 Start;
    /// <summary>原局部终点偏移；Real用来确定固定发射方向。</summary>
    public Vector3 End;
    /// <summary>曲线起始速度，单位米每秒。</summary>
    public float StartSpeed;
    /// <summary>无曲线时的速度或曲线终值速度，单位米每秒。</summary>
    public float MaxSpeed;
    /// <summary>是否瞄准实际目标模型中部。</summary>
    public bool AimTargetCenter;
    /// <summary>Anim贝塞尔飞行时间，单位秒。</summary>
    public float InitTime;
    /// <summary>Real/Rect原生存秒数。</summary>
    public float TotalTime;
    /// <summary>Anim第一个局部控制点。</summary>
    public Vector3 P1;
    /// <summary>Anim第二个局部控制点。</summary>
    public Vector3 P2;
    /// <summary>Anim第三个局部控制点。</summary>
    public Vector3 P3;
    /// <summary>原贝塞尔控制点绕Z轴初始角度。</summary>
    public float InitAngle;
    /// <summary>原RO导出曲线采样，缺省时无曲线。</summary>
    public CurvePoint[] Curve;
    /// <summary>Rect是否围绕原施法中心公转。</summary>
    public bool Revolution;
    /// <summary>原公转角速度，单位度每秒。</summary>
    public float RevolutionSpeed;
    /// <summary>原公转中心相对施法者的位置。</summary>
    public Vector3 RevolutionCenter;
    /// <summary>原飞行声音导出的真实事件资源。</summary>
    public string Audio;

    /// <summary>按原导出线性速度曲线积分，晚加入直接定位当前已飞行距离。</summary>
    /// <param name="seconds">实际已过的弹道秒数。</param>
    public float DistanceAt(float seconds)
    {
        if (Curve.Length == 0) return MaxSpeed * seconds;
        // 从零到当前秒数的曲线面积，端点外保留原末值。
        float area = 0f;
        // 上一积分区间的终点。
        float start = 0f;
        for (int index = 0; index < Curve.Length; index++)
        {
            // 仅取当前时间之前的原采样横轴。
            float end = Mathf.Min(seconds, Curve[index].Time);
            if (end <= start) continue;
            area += (Evaluate(start) + Evaluate(end)) * 0.5f * (end - start);
            start = end;
            if (start >= seconds) break;
        }
        if (start < seconds) area += Curve[Curve.Length - 1].Value * (seconds - start);
        return StartSpeed * seconds + (MaxSpeed - StartSpeed) * area;
    }

    /// <summary>按原导出采样线性插值，端点保持原值。</summary>
    /// <param name="time">原曲线横轴值。</param>
    public float Evaluate(float time)
    {
        if (time <= Curve[0].Time) return Curve[0].Value;
        for (int index = 1; index < Curve.Length; index++)
        {
            if (time > Curve[index].Time) continue;
            // 当前覆盖该横轴的相邻原采样点。
            CurvePoint from = Curve[index - 1];
            // 该区间的右侧采样点。
            CurvePoint to = Curve[index];
            return Mathf.LerpUnclamped(from.Value, to.Value, (time - from.Time) / (to.Time - from.Time));
        }
        return Curve[Curve.Length - 1].Value;
    }
}
