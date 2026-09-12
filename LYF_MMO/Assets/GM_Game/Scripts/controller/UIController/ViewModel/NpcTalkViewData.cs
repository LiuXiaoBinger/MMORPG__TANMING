/// <summary>
/// NPC 对话展示数据，不向 View 暴露角色领域对象。
/// </summary>
public sealed class NpcTalkViewData
{
    /// <summary>NPC 配置编号。</summary>
    public int NpcId { get; }

    /// <summary>NPC 显示名称。</summary>
    public string Name { get; }

    /// <summary>NPC 对话文本。</summary>
    public string Talk { get; }

    /// <summary>创建 NPC 对话展示数据。</summary>
    public NpcTalkViewData(int npcId, string name, string talk)
    {
        NpcId = npcId;
        Name = name;
        Talk = talk;
    }
}
