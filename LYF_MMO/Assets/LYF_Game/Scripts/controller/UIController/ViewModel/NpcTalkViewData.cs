using System;

/// <summary>本次 NPC 对话的名字、分句和可选服务；窗口只接收展示数据。</summary>
public sealed class NpcTalkViewData
{
    /// <summary>当前说话者的配置编号。</summary>
    public int NpcId { get; }
    /// <summary>对白区显示的 NPC 名称。</summary>
    public string Name { get; }
    /// <summary>按 NPC 表中下划线拆出的对白，点击继续时逐句展示。</summary>
    public string[] Lines { get; }
    /// <summary>当前说话者按顺序提供的商店服务。</summary>
    public NpcDialogueCatalog.Service[] Services { get; }
    /// <summary>魔神塔波次 47 使用原剧情的帮助和离开选项。</summary>
    public bool ShowMoshentaBossChoices { get; }

    /// <summary>把配置对白分句，并保留明确给当前说话者的服务。</summary>
    /// <param name="npcId">NPC 配置编号。</param>
    /// <param name="name">画面左下角的说话者名字。</param>
    /// <param name="talk">NPC 表中以下划线分隔的对白。</param>
    /// <param name="showMoshentaBossChoices">当前交互是否为魔神塔开战剧情。</param>
    /// <param name="services">该 NPC 对话期间可进入的货架。</param>
    public NpcTalkViewData(int npcId, string name, string talk, bool showMoshentaBossChoices = false,
        NpcDialogueCatalog.Service[] services = null)
    {
        NpcId = npcId;
        Name = name;
        Lines = (talk ?? string.Empty).Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        Services = services ?? Array.Empty<NpcDialogueCatalog.Service>();
        ShowMoshentaBossChoices = showMoshentaBossChoices;
    }
}
