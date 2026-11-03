using System.Collections.Generic;

/// <summary>
/// 道具扩展数据。用于承载不同道具类型的附加属性。
/// </summary>
public sealed class ItemExtraData
{
    /// <summary>
    /// 扩展键值；没有扩展数据时为空字典。
    /// </summary>
    public Dictionary<string, string> Values { get; private set; }

    /// <summary>为一件物品建立独立的扩展键值字典。</summary>
    public ItemExtraData()
    {
        Values = new Dictionary<string, string>();
    }
}
