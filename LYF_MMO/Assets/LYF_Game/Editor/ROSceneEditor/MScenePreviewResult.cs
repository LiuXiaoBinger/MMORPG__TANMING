/// <summary>原触发器 Timer 结果所延迟的一个编辑预览动作。</summary>
public sealed class MScenePreviewResult
{
    /// <summary>到期后执行的原触发结果节点。</summary>
    public MSceneEditorNode Node;
    /// <summary>编辑器启动后的执行时刻，单位为秒。</summary>
    public double DueTime;
}
