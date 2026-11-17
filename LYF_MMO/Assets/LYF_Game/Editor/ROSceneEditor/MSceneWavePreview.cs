using System.Collections.Generic;

/// <summary>一个原 RO 波次在编辑预览中的状态，不代表服务器战斗结果。</summary>
public sealed class MSceneWavePreview
{
    /// <summary>对应原波次节点，编号和字段由编辑文档提供。</summary>
    public MSceneEditorNode Node;
    /// <summary>是否已经收到事件或满足前置波次条件。</summary>
    public bool Requested;
    /// <summary>是否已经完成该波次模型生成。</summary>
    public bool Spawned;
    /// <summary>是否由用户在预览中标记完成。</summary>
    public bool Completed;
    /// <summary>允许创建波次的编辑器时间，单位为秒。</summary>
    public double DueTime;
    /// <summary>是否还有尚未按 interval_rush_data 出场的批次。</summary>
    public bool Spawning;
    /// <summary>下一批怪物允许创建的编辑器时间，单位为秒。</summary>
    public double NextSpawnTime;
    /// <summary>预览中剩余怪物数量占初始数量的百分比，用于检查原前置波次条件。</summary>
    public int AlivePercent = 100;
    /// <summary>预览中波次总剩余血量百分比，用于检查原前置波次条件。</summary>
    public int HealthPercent = 100;
    /// <summary>该波次在本轮预览生成的所有表现对象。</summary>
    public readonly List<MScenePreviewInstance> Instances = new List<MScenePreviewInstance>();
}
