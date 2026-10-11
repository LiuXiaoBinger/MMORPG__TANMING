using System;

/// <summary>RO AI事件制作数据，按技能时刻记录事件名称；不在编辑器执行AI逻辑。</summary>
[Serializable]
public sealed class MSkillAIEventData
{
    /// <summary>发送给AI系统的事件名称，完成配置后不能为空。</summary>
    public string Event = string.Empty;

    /// <summary>从本次技能开始计时的AI事件触发时刻，单位秒，非负。</summary>
    public float At;

    /// <summary>RO事件配置索引，供宿主关联AI规则，不是列表下标。</summary>
    public int Index;
}

