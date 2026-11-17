using System.Collections.Generic;

/// <summary>从正式目标表和当前场景原图提取的一段玩家流程；仅保存展示事实，不执行副本。</summary>
public sealed class ROSceneStage
{
    /// <summary>流程内稳定标识：entry、finish、section-序号、group-组号或unresolved。</summary>
    public string Key = "";
    /// <summary>包含阶段序号或原分支组号的玩家目标标题。</summary>
    public string Title = "";
    /// <summary>本阶段的进入条件和当前需要完成的玩家行动。</summary>
    public string EntryText = "";
    /// <summary>目标表和原事件证明的完成条件，不表示编辑器已经完成该目标。</summary>
    public string CompletionText = "";
    /// <summary>同阶段同时生效的原DungeonTargetTable行号，顺序与正式表一致。</summary>
    public readonly List<int> TargetIds = new List<int>();
    /// <summary>与TargetIds对应的玩家目标文案，保留缺失行的明确说明。</summary>
    public readonly List<string> TargetTexts = new List<string>();
    /// <summary>原目标图中直接后继的Key；多项代表等待选支而不是依次执行。</summary>
    public readonly List<string> NextKeys = new List<string>();
    /// <summary>有原表、事件或清波依赖证据的波次节点引用，不复制或修改原XML。</summary>
    public readonly List<MSceneEditorNode> Waves = new List<MSceneEditorNode>();
    /// <summary>本阶段有证据关联的触发器、时机、条件、结果原节点，供定位与详情复用。</summary>
    public readonly List<MSceneEditorNode> Mechanisms = new List<MSceneEditorNode>();
    /// <summary>关联来源、跨阶段持续存在、运行边界及尚未解析的事实。</summary>
    public readonly List<string> Notes = new List<string>();
    /// <summary>该项是否为玩家进入副本的入口。</summary>
    public bool IsEntry { get { return Key == "entry"; } }
    /// <summary>该项是否为目标结束后的离开流程。</summary>
    public bool IsFinish { get { return Key == "finish"; } }
}
