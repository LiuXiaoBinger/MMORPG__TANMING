using System;

/// <summary>RO模型变色区间制作数据，保留渐变资源路径供后续表现处理器加载。</summary>
[Serializable]
public sealed class MSkillChangeColorData
{
    /// <summary>变色区间开始时刻，单位秒，非负。</summary>
    public float At;

    /// <summary>变色区间结束时刻，单位秒，不小于At。</summary>
    public float End;

    /// <summary>RO颜色渐变资源路径；不是编辑器直接序列化的Gradient对象。</summary>
    public string colorGradientPath = string.Empty;

    /// <summary>RO变色配置索引，供宿主关联材质表现。</summary>
    public int Index;
}

