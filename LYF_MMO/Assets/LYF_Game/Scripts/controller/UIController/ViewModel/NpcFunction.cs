/// <summary>对应 RO 的 NPC 功能表，服务选项决定打开哪个已有业务入口。</summary>
public enum NpcFunction
{
    /// <summary>使用 ShopId 和货架商品清单进入已有商店，兼容原有数据默认值。</summary>
    PropertyShop = 0,
    /// <summary>显示本 NPC 在任务表中发布或接收的委托。</summary>
    Task = 1,
    /// <summary>向游戏服申请本地图的指定交通入口。</summary>
    Transfer = 2,
    /// <summary>从委托对白进入角色已有的任务簿。</summary>
    TaskJournal = 3,
    /// <summary>办理同一按钮绑定的角色委托。</summary>
    QuestAction = 4
}
