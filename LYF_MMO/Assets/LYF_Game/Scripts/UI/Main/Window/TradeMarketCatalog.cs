using System;
using UnityEngine;

/// <summary>交易窗口的陈列目录，关联本项目物品配置与对应的现有图标。</summary>
public sealed class TradeMarketCatalog : ScriptableObject
{
    /// <summary>录屏右侧六个市场的排列顺序，也用于区分各自的关注列表。</summary>
    public enum Market
    {
        Stall, // 本服摆摊。
        Local, // 本服珍品集市。
        Global, // 全服集市。
        Kitchen, // 神厨菜品。
        Auction, // 三个风雷拍场。
        Credit // 交易信誉。
    }

    /// <summary>左侧一个可选货架，同组货架在展开时成为二级分类。</summary>
    [Serializable]
    public sealed class Shelf
    {
        /// <summary>此货架所属的右侧市场。</summary>
        public Market Market;
        /// <summary>拍场代号，零至二对应三个圆形入口，其余市场忽略。</summary>
        public int Venue;
        /// <summary>左侧一级分类的显示名称。</summary>
        public string Group;
        /// <summary>二级分类名，空串代表可直接选中的一级分类。</summary>
        public string Leaf;
        /// <summary>按陈列顺序保存的 Luban 物品编号，挂单数量独立于目录。</summary>
        public int[] Items;
    }

    /// <summary>按物品配置编号绑定原物品表使用的 Unity 精灵。</summary>
    [Serializable]
    private struct Illustration
    {
        /// <summary>道具或装备配置的编号。</summary>
        public int ItemId;
        /// <summary>背包、美术图集或秘笈图集里的实际切片。</summary>
        public Sprite Sprite;
    }

    /// <summary>从右侧市场到左侧子分类的陈列顺序。</summary>
    [SerializeField] private Shelf[] _shelves;
    /// <summary>可直接编辑的中文分类 JSON，跟随交易目录一起打包。</summary>
    [SerializeField] private TextAsset _visibilityFile;
    /// <summary>按物品编号升序保存，供图标快速查找。</summary>
    [SerializeField] private Illustration[] _art;

    /// <summary>供窗口构造当前市场的分类按钮。</summary>
    public Shelf[] Shelves => _shelves;

    /// <summary>打开商城时读取显示配置，旧目录未绑定文件时保留全部货架。</summary>
    public TradeMarketVisibility ReadVisibility()
    {
        // 先保留默认值，允许文件只列出本项目实际需要覆盖的字段。
        TradeMarketVisibility visibility = new TradeMarketVisibility();
        if (_visibilityFile != null) JsonUtility.FromJsonOverwrite(_visibilityFile.text, visibility);
        return visibility;
    }

    /// <summary>取陈列图标，未配图的物品沿用调用端的背包加载流程。</summary>
    /// <param name="itemId">物品配置编号。</param>
    public Sprite GetIcon(int itemId)
    {
        // 首尾下标限制本次查找的有序图标区间。
        int first = 0;
        // 末端为当前可搜索区间的最后一个有效下标。
        int last = _art.Length - 1;
        while (first <= last)
        {
            // 区间中点对应的配置编号用于排除一半图标。
            int middle = first + (last - first) / 2;
            if (_art[middle].ItemId == itemId) return _art[middle].Sprite;
            if (_art[middle].ItemId < itemId) first = middle + 1;
            else last = middle - 1;
        }
        return null;
    }
}
