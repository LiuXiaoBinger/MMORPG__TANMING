using System.Xml.Linq;

/// <summary>单条 RO 波次在当前副本实例中的出生进度；不保存战斗血量。</summary>
public class MSceneRuntimeWave
{
    /// <summary>编辑器原始波次数据，生命周期与副本根组件一致。</summary>
    public XElement Data;
    /// <summary>原 base_info/id，用于前置关系与 Inspector 定位。</summary>
    public int Id;
    /// <summary>前置条件是否已触发，触发后开始计算延迟。</summary>
    public bool Requested;
    /// <summary>本波所有配置实例是否已提交创建。</summary>
    public bool Spawned;
    /// <summary>下批出生的场景运行秒数，使用 Time.time。</summary>
    public float DueTime;
    /// <summary>已提交创建的实例数，包括异步加载中的模型。</summary>
    public int SpawnCount;
}
