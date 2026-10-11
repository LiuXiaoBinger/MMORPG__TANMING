using System;

/// <summary>RO镜头后处理单项配置；SkillConfigSO只保存一个对象，启用后按At至End区间制作。</summary>
[Serializable]
public sealed class MCameraPostEffectData
{
    /// <summary>是否启用该后处理配置；关闭后仍保留已编辑参数。</summary>
    public bool UseCameraEffect;

    /// <summary>是否对所有观察者显示，由宿主决定接收范围。</summary>
    public bool EveryOne;

    /// <summary>后处理接收者的作用距离，单位米，非负。</summary>
    public float Range;

    /// <summary>后处理开始时刻，单位秒，非负。</summary>
    public float At;

    /// <summary>后处理结束时刻，单位秒，不小于At。</summary>
    public float End;

    /// <summary>RO后处理类型，径向模糊或波形扭曲。</summary>
    public CameraEffectType EffectType;

    /// <summary>RO后处理第一个参数，具体单位随EffectType由镜头处理器解释。</summary>
    public float Args0;

    /// <summary>RO后处理第二个参数，具体单位随EffectType由镜头处理器解释。</summary>
    public float Args1;

    /// <summary>RO后处理配置索引，供宿主关联镜头规则。</summary>
    public int Index;
}

