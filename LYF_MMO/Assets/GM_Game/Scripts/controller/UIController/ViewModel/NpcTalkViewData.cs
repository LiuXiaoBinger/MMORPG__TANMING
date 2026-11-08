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

    /// <summary>当前原魔神塔NPC20186是否展示帮助/暂离两项，普通NPC保持原功能按钮。</summary>
    public bool ShowMoshentaBossChoices { get; }

    /// <summary>创建 NPC 对话展示数据。</summary>
    /// <param name="npcId">NPC 配置编号，供后续商店入口识别 NPC。</param>
    /// <param name="name">对话窗口标题显示的名字。</param>
    /// <param name="talk">正文；窗口会把下划线替换为换行。</param>
    /// <param name="showMoshentaBossChoices">仅原魔神塔波47的NPC20186对话为true。</param>
    public NpcTalkViewData(int npcId, string name, string talk, bool showMoshentaBossChoices = false)
    {
        NpcId = npcId;
        Name = name;
        Talk = talk;
        ShowMoshentaBossChoices = showMoshentaBossChoices;
    }
}
