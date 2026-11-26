using System;
using UnityEngine;

/// <summary>按 NPC 编号保存称谓与对话功能，功能顺序就是按钮顺序。</summary>
public sealed class NpcDialogueCatalog : ScriptableObject
{
    /// <summary>一项商店、委托或交通服务。</summary>
    [Serializable]
    public sealed class Service
    {
        /// <summary>右侧按钮显示的中文服务名称。</summary>
        public string Label;
        /// <summary>点击后调用的 RO 功能类型；未填写的旧货架默认打开商店。</summary>
        public NpcFunction Function;
        /// <summary>传送功能选择本地图的门号，与公共地图传送配置对应。</summary>
        public int DoorId;
        /// <summary>用于读表和提交购买的 ShopTable.Id。</summary>
        public int ShopId;
        /// <summary>允许陈列的 ShopItemInfo.GoodsId；留空时展示整间商店。</summary>
        public int[] GoodsIds;

        /// <summary>根据所选货架的商品清单决定是否展示该商品。</summary>
        /// <param name="goodsId">已经过所属商店筛选的商品配置编号。</param>
        public bool Includes(int goodsId)
        {
            return GoodsIds == null || GoodsIds.Length == 0 || Array.IndexOf(GoodsIds, goodsId) >= 0;
        }
    }

    /// <summary>一个 NPC 的服务组合，空列表表示只说话。</summary>
    [Serializable]
    private sealed class Entry
    {
        /// <summary>主城或副本 NPC 的配置编号。</summary>
        public int NpcId;
        /// <summary>头顶显示的职业或身份，填写原文，例如“杂货商人”。</summary>
        public string Title;
        /// <summary>该 NPC 在对话中提供的全部功能。</summary>
        public Service[] Services;
    }

    /// <summary>策划在资源中填写的 NPC 对话服务。</summary>
    [SerializeField] private Entry[] _npcs;

    /// <summary>按编号读取同一位 NPC 的称谓和服务配置。</summary>
    /// <param name="npcId">出生表中的 NPC 编号。</param>
    private Entry FindEntry(int npcId)
    {
        // 同一份角色配置同时供主城出生和对话窗口读取。
        for (int index = 0; index < _npcs.Length; index++)
        {
            if (_npcs[index].NpcId == npcId) return _npcs[index];
        }
        return null;
    }

    /// <summary>在实体出生时生成名牌称谓，沿用玩家名牌的尖括号格式。</summary>
    /// <param name="npcId">当前生成的 NPC 编号。</param>
    public string GetNameplateTitle(int npcId)
    {
        // 缺少称谓的 NPC 继续沿用自身的显示入口。
        Entry entry = FindEntry(npcId);
        if (entry == null || string.IsNullOrEmpty(entry.Title)) return string.Empty;
        return "<" + entry.Title + ">";
    }

    /// <summary>取指定 NPC 的服务；未单独配置的旧商人继续使用其默认商店。</summary>
    /// <param name="npcId">当前实体对应的 NPC 配置编号。</param>
    /// <param name="defaultShopId">NPC 表原有商店编号，零表示没有默认商店。</param>
    public Service[] GetServices(int npcId, int defaultShopId)
    {
        // 已配置的服务包括有意留空的纯对白选项。
        Entry entry = FindEntry(npcId);
        if (entry != null) return entry.Services;
        if (defaultShopId > 0)
            return new[] { new Service { Label = "浏览商品", ShopId = defaultShopId } };
        return Array.Empty<Service>();
    }
}
