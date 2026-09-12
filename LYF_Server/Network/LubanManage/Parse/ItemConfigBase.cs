using cfg;

/// <summary>
/// 道具配置基类，所有道具表配置数据的抽象父类
/// 封装道具通用基础字段，子类继承扩展各自特有配置
/// 构造函数protected，仅允许子类实例化，外部不能直接new
/// </summary>
public abstract class ItemConfigBase
{
    /// <summary>道具唯一ID</summary>
    public int ItemId { get; private set; }

    /// <summary>道具主类型（消耗品/武器/装备等）</summary>
    public ItemMainType ItemMainType { get; private set; }

    /// <summary>道具子类型，主类型下细分分类</summary>
    public int ItemSubType { get; private set; }

    /// <summary>道具名称</summary>
    public string ItemName { get; private set; }

    /// <summary>道具描述文本</summary>
    public string Description { get; private set; }

    /// <summary>图标资源路径/ID</summary>
    public string Icon { get; private set; }

    /// <summary>属性字符串，存放道具属性原始配置文本，后续解析成属性列表</summary>
    public string Attribute { get; private set; }

    /// <summary>使用所需角色等级</summary>
    public int NeedLevel { get; private set; }

    /// <summary>道具获取来源描述</summary>
    public string Source { get; private set; }

    /// <summary>商店售卖价格</summary>
    public int SellPrice { get; private set; }

    /// <summary>售卖货币类型</summary>
    public int SellCurrencyType { get; private set; }

    /// <summary>单个物品实例允许的最大堆叠数量，不可堆叠物品为1。</summary>
    public int MaxStackCount { get; private set; }

    /// <summary>物品所属背包类别，与 KnapsackType 枚举值对应</summary>
    public KnapsackType PackType { get; private set; }

    /// <summary>物品生命周期类型，由配置表 TimeType 定义</summary>
    public int TimeType { get; private set; }

    /// <summary>
    /// 受保护构造函数，仅子类可以调用赋值全部基础属性
    /// </summary>
    /// <param name="itemId">道具ID</param>
    /// <param name="itemMainType">道具主类型</param>
    /// <param name="itemSubType">道具子类型</param>
    /// <param name="itemName">道具名称</param>
    /// <param name="description">道具描述</param>
    /// <param name="icon">图标资源</param>
    /// <param name="attribute">属性原始字符串</param>
    /// <param name="needLevel">使用等级限制</param>
    /// <param name="source">获取来源</param>
    /// <param name="sellPrice">售卖价格</param>
    /// <param name="sellCurrencyType">售卖货币类型</param>
    /// <param name="maxStackCount">单个实例最大堆叠数量</param>
    /// <param name="packType">物品所属背包类别</param>
    /// <param name="timeType">物品生命周期类型</param>
    protected ItemConfigBase(
        int itemId,
        ItemMainType itemMainType,
        int itemSubType,
        string itemName,
        string description,
        string icon,
        string attribute,
        int needLevel,
        string source,
        int sellPrice,
        int sellCurrencyType,
        int maxStackCount,
        KnapsackType packType,
        int timeType)
    {
        ItemId = itemId;
        ItemMainType = itemMainType;
        ItemSubType = itemSubType;
        ItemName = itemName;
        Description = description;
        Icon = icon;
        Attribute = attribute;
        NeedLevel = needLevel;
        Source = source;
        SellPrice = sellPrice;
        SellCurrencyType = sellCurrencyType;
        if (maxStackCount <= 0)
        {
            MaxStackCount = 1;
        }
        else
        {
            MaxStackCount = maxStackCount;
        }
        PackType = packType;
        TimeType = timeType;
    }

    /// <summary>
    /// 规范化校验道具主类型，只允许指定合法类型，非法值返回降级兜底类型
    /// 当前仅校验：消耗品、武器，其他类型直接使用fallback兜底
    /// </summary>
    /// <param name="value">原始表格读取的int数值</param>
    /// <param name="fallback">校验失败时返回的兜底类型</param>
    /// <returns>校验过后安全的ItemMainType</returns>
    protected static ItemMainType NormalizeMainType(int value, ItemMainType fallback)
    {
        return (ItemMainType)value;
    }

    /// <summary>
    /// 校验表中的背包类别，只允许物品可存放的四类普通背包和虚拟物品背包；非法值使用调用方指定的默认类别。
    /// </summary>
    protected static KnapsackType NormalizePackType(int value, KnapsackType fallback, int itemId)
    {
        if (value == (int)KnapsackType.RolePackPlain || value == (int)KnapsackType.RolePackEquip ||
            value == (int)KnapsackType.RolePackConsume || value == (int)KnapsackType.RolePackMaterial ||
            value == (int)KnapsackType.RoleVirtualItemPack)
        {
            return (KnapsackType)value;
        }

        LogMsg.Info($"物品配置 ID {itemId} 的包类别 {value} 无效，已回退为 {fallback}。", LogMsgType.Warn);
        return fallback;
    }

    /// <summary>
    /// 获取第一个非空字符串，按优先级依次取值
    /// primary优先，primary为空取secondary，secondary为空返回fallback兜底
    /// </summary>
    /// <param name="primary">第一优先级字符串</param>
    /// <param name="secondary">第二优先级字符串</param>
    /// <param name="fallback">兜底返回值，允许传入空字符串</param>
    /// <returns>不为空的字符串</returns>
    protected static string GetFirstNonEmpty(string primary, string secondary, string fallback)
    {
        if (!string.IsNullOrEmpty(primary))
        {
            return primary;
        }

        if (!string.IsNullOrEmpty(secondary))
        {
            return secondary;
        }

        return fallback;
    }
}
