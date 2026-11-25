using System;
using UnityEngine;

/// <summary>珍宝阁陈列配置。商品键仅在该目录内使用，由以后接入的服务映射为正式业务编号。</summary>
[Serializable]
public sealed class PavilionEntry
{
    /// <summary>跨筛选、换页和改名仍然稳定的陈列键。</summary>
    public string key;
    /// <summary>商城、绑元商城或商会。</summary>
    public string channel;
    /// <summary>商城页签或商会一级货架。</summary>
    public string group;
    /// <summary>商会可选二级货架，没有时为空。</summary>
    public string shelf;
    /// <summary>卡片和详情共用的物品名。</summary>
    public string name;
    /// <summary>用于类型筛选的品类。</summary>
    public string kind;
    /// <summary>详情正文，独立于价格与库存状态。</summary>
    public string description;
    /// <summary>每份价格所用的货币，绑定元宝单独标识。</summary>
    public string currency;
    /// <summary>一份商品的陈列单价。</summary>
    public int price;
    /// <summary>显示的每日总限额，零表示配置未设限。</summary>
    public int limit;
    /// <summary>使用等级，高于角色时可用筛选器隐藏。</summary>
    public int level;
    /// <summary>项目内提供物品图标的配置编号。</summary>
    public int art;
    /// <summary>商城图集里的专用插画名称，设置时优先使用。</summary>
    public string picture;
    /// <summary>商品上角的折扣文字。</summary>
    public string discount;
    /// <summary>背包生成的出售行携带原堆叠 UID。</summary>
    [NonSerialized] public long stack;
    /// <summary>出售行可选择的实际件数。</summary>
    [NonSerialized] public int available;

    /// <summary>本地目录与货架排序。</summary>
    [Serializable]
    public sealed class Catalog
    {
        /// <summary>所有可购买的展示条目。</summary>
        public PavilionEntry[] products;
        /// <summary>商城分类的展示顺序。</summary>
        public string[] mall;
        /// <summary>绑元商城分类顺序。</summary>
        public string[] bound;
        /// <summary>商会一级及二级分类。</summary>
        public Shelf[] chamber;
        /// <summary>充值六档的展示数据。</summary>
        public Pack[] packs;
        /// <summary>累计与年度返利的门槛和奖品展示。</summary>
        public Reward[] rewards;
    }

    /// <summary>商会的一级目录和有序子目录。</summary>
    [Serializable]
    public sealed class Shelf
    {
        /// <summary>一级分类标题。</summary>
        public string name;
        /// <summary>二级分类名称，空数组表示直接打开一级分类。</summary>
        public string[] children;
    }

    /// <summary>以人民币元展示的充值档位。</summary>
    [Serializable]
    public sealed class Pack
    {
        /// <summary>面值元宝数量，也是该档位的展示编号。</summary>
        public int amount;
        /// <summary>录屏显示的人民币整元标价。</summary>
        public int yuan;
        /// <summary>额外展示的赠送元宝。</summary>
        public int bonus;
        /// <summary>随档位展示的红包个数。</summary>
        public int envelopes;
    }

    /// <summary>一个返利档位及其展示奖品。</summary>
    [Serializable]
    public sealed class Reward
    {
        /// <summary>属于今年累计充值页。</summary>
        public bool annual;
        /// <summary>领取该档所需的累计额度。</summary>
        public int threshold;
        /// <summary>按录屏排列的奖品图标编号。</summary>
        public int[] icons;
        /// <summary>与图标一一对应的展示数量。</summary>
        public int[] counts;
    }

    /// <summary>提交给外部控制器的买卖意图，不持有或修改角色物品。</summary>
    public sealed class Order
    {
        /// <summary>目录稳定键，出售时为项目物品编号字符串。</summary>
        public string Key;
        /// <summary>意图来自的频道。</summary>
        public string Channel;
        /// <summary>出售时为真实背包堆叠 UID，购买时为零。</summary>
        public long Stack;
        /// <summary>玩家在详情中选择的份数。</summary>
        public int Quantity;
        /// <summary>当前页面显示的货币名。</summary>
        public string Currency;
        /// <summary>点击时显示的每份价格。</summary>
        public int UnitPrice;
    }
}
