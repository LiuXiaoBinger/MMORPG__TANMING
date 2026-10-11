using System;

/// <summary>RO Buff事件制作数据；RO原结构通过Index关联规则，没有额外BuffId字段。</summary>
[Serializable]
public sealed class MSkillBuffData
{
    /// <summary>从本次技能开始计时的Buff生效时刻，单位秒，非负。</summary>
    public float At;

    /// <summary>RO Buff规则索引，具体Buff绑定由技能规则决定；不是Buff模板ID。</summary>
    public int Index;
}

