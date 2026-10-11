using System;

/// <summary>RO震屏制作数据，保留各轴振幅、坐标系和结果关联语义；不同于Y通用镜头偏移。</summary>
[Serializable]
public sealed class MCameraShakeData
{
    /// <summary>从技能开始计时的震屏触发时刻，单位秒，非负。</summary>
    public float At;

    /// <summary>震屏持续时长，单位秒，非负；终点为At加Time。</summary>
    public float Time;

    /// <summary>各轴振幅采用世界、角色自身或镜头坐标系。</summary>
    public CameraMotionSpace Coordinate;

    /// <summary>镜头视场角的有符号震动振幅，单位度；非随机模式下负值反转初始震动方向。</summary>
    public float FovAmp;

    /// <summary>整个Time持续时长内的震动频数，非负次数；RO按Time/Frequency求间隔，零值原样保存，不改写为默认频数。</summary>
    public float Frequency;

    /// <summary>震动X轴的有符号位移振幅，单位米；非随机模式下负值反转此轴初始方向。</summary>
    public float AmplitudeX;

    /// <summary>震动Y轴的有符号位移振幅，单位米；非随机模式下负值反转此轴初始方向。</summary>
    public float AmplitudeY;

    /// <summary>震动Z轴的有符号位移振幅，单位米；非随机模式下负值反转此轴初始方向。</summary>
    public float AmplitudeZ;

    /// <summary>是否随机选择震动方向或相位，由镜头处理器解释。</summary>
    public bool Random;

    /// <summary>是否由命中结果触发；启用时不是普通技能时钟直接触发。</summary>
    public bool WithResult;

    /// <summary>是否允许非本地施法者也触发本机镜头震动。</summary>
    public bool ShakeEveryOne;

    /// <summary>RO震屏曲线资源路径，空值表示采用宿主默认曲线。</summary>
    public string Curve = string.Empty;

    /// <summary>RO震屏配置索引，可由ResultList关联使用。</summary>
    public int Index;
}
