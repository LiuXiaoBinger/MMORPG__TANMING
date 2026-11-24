using System;
using UnityEngine;

/// <summary>按 NPC 配置编号保存对话右侧可选择的货架，数组顺序就是显示顺序。</summary>
public sealed class NpcDialogueCatalog : ScriptableObject
{
    /// <summary>一项可陈列整间商店或精选商品的对话服务。</summary>
    [Serializable]
    public sealed class Service
    {
        /// <summary>右侧按钮显示的中文服务名称。</summary>
        public string Label;
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
        /// <summary>该 NPC 在对话中提供的全部货架。</summary>
        public Service[] Services;
    }

    /// <summary>策划在资源中填写的 NPC 对话服务。</summary>
    [SerializeField] private Entry[] _npcs;

    /// <summary>取指定 NPC 的服务；未单独配置的旧商人继续使用其默认商店。</summary>
    /// <param name="npcId">当前实体对应的 NPC 配置编号。</param>
    /// <param name="defaultShopId">NPC 表原有商店编号，零表示没有默认商店。</param>
    public Service[] GetServices(int npcId, int defaultShopId)
    {
        // 按资源中的行号查找当前说话者。
        for (int index = 0; index < _npcs.Length; index++)
        {
            // 当前配置行；明确配置为空时也直接采用。
            Entry entry = _npcs[index];
            if (entry.NpcId == npcId) return entry.Services;
        }
        if (defaultShopId > 0)
            return new[] { new Service { Label = "浏览商品", ShopId = defaultShopId } };
        return Array.Empty<Service>();
    }
}
