using System.Collections.Generic;
using System.Xml.Linq;

/// <summary>本副本一项原Buff实例；原编号、来源、寿命与其实际Gene叶子共同存亡。</summary>
internal sealed class MoshentaBuffInstance
{
    /// <summary>包含等级的原BuffId。</summary>
    internal int Id;
    /// <summary>用于精确增删和晚加入重建的原Buff实例身份，移除后不复用。</summary>
    internal ulong InstanceId;
    /// <summary>施加者真实怪物实体；null为原SceneAI场景来源，不伪借某只怪身份。</summary>
    internal MoshentaActor Caster;
    /// <summary>原部署表行。</summary>
    internal XElement Row;
    /// <summary>原效果叶子按声明顺序保存。</summary>
    internal readonly List<XElement> Effects = new List<XElement>();
    /// <summary>只删除本实例产生的Gene，不按状态或来源批量删除。</summary>
    internal readonly List<GeneInstance> Genes = new List<GeneInstance>();
    /// <summary>原Buff寿命截止毫秒，常驻为最大值。</summary>
    internal long End;
    /// <summary>DestroyTiming种类3的剩余成功触发次数；0表示非次数寿命。</summary>
    internal int RemainingTriggers;
    /// <summary>各周期叶子的下一次执行时点，单位毫秒。</summary>
    internal readonly Dictionary<int, long> Next = new Dictionary<int, long>();
}
