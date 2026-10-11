using System;

/// <summary>旧剪辑事件的只读来源记录，帮助策划核对整数帧量化产生的时间差。</summary>
[Serializable]
public sealed class SkillEditorFxSource
{
    /// <summary>AnimationUtility 返回的完整事件数组下标，包含非特效事件。</summary>
    public int SourceEventIndex;

    /// <summary>Unity 导入后旧事件的触发时间，单位为秒，生成后不随时间轴编辑改写。</summary>
    public float OriginalTimeSeconds;

    /// <summary>首次导入 Y 时间轴时的帧号；运行导出使用当前编辑帧而非此参考帧。</summary>
    public int ImportedFrame;

    /// <summary>旧 PlayEffect 的阶段标记，例如 skill03_2 或 skill04_2。</summary>
    public string Stage;
}
