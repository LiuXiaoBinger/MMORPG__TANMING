using System;
using System.Collections.Generic;

/// <summary>交易商城的分类显示配置，每次打开商城从 JSON 读取。</summary>
[Serializable]
public sealed class TradeMarketVisibility
{
    /// <summary>同名分类在各市场共用的显示规则。</summary>
    [Serializable]
    public sealed class Category
    {
        /// <summary>目录内的一级分类原名。</summary>
        public string group;
        /// <summary>二级货架原名，留空时控制整个一级分类。</summary>
        public string leaf;
        /// <summary>关闭后隐藏该组或该货架。</summary>
        public bool show = true;
    }

    /// <summary>有搜索功能的市场是否展示搜索栏。</summary>
    public bool showSearch = true;
    /// <summary>浏览页是否展示关注入口。</summary>
    public bool showFavorites = true;
    /// <summary>求购页是否展示我的求购入口。</summary>
    public bool showMyRequests = true;
    /// <summary>按目录原名维护的规则，未配置的分类默认显示。</summary>
    public Category[] categories = Array.Empty<Category>();

    /// <summary>按原目录顺序保留可见货架，供首屏、搜索和跨服筛选共用。</summary>
    /// <param name="shelves">完整的交易货架目录。</param>
    public List<TradeMarketCatalog.Shelf> VisibleShelves(TradeMarketCatalog.Shelf[] shelves)
    {
        // 本轮浏览共用的货架，保留原目录对象引用。
        List<TradeMarketCatalog.Shelf> visible = new List<TradeMarketCatalog.Shelf>(shelves.Length);
        // 货架下标按原陈列顺序递增。
        for (int index = 0; index < shelves.Length; index++)
        {
            if (IsVisible(shelves[index])) visible.Add(shelves[index]);
        }
        return visible;
    }

    /// <summary>组规则隐藏全部子项，货架规则只隐藏同组同名的子项。</summary>
    /// <param name="shelf">待判断的目录行。</param>
    private bool IsVisible(TradeMarketCatalog.Shelf shelf)
    {
        // 按文件顺序查找与本货架有关的隐藏项。
        for (int index = 0; index < categories.Length; index++)
        {
            // 本条规则只作用于同名一级分类。
            Category rule = categories[index];
            if (rule.show || rule.group != shelf.Group) continue;
            if (string.IsNullOrEmpty(rule.leaf) || rule.leaf == shelf.Leaf) return false;
        }
        return true;
    }
}
