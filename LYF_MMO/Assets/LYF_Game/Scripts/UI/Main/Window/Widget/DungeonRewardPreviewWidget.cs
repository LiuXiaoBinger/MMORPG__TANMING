using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>显示首领掉落或终绝宝箱中的一种物品，按背包配置取图及名称。</summary>
public sealed class DungeonRewardPreviewWidget : MonoBehaviour
{
    /// <summary>正式预制体中的物品图标。</summary>
    [SerializeField] private Image icon;
    /// <summary>图标右下角的单次获得数量。</summary>
    [SerializeField] private Text quantity;
    /// <summary>周赏格下方的名称。普通掉落的名字由纪事主窗口统一显示。</summary>
    [SerializeField] private Text caption;
    /// <summary>预制体直接引用的常用奖励图集切片，名称与背包图标路径末段一致。</summary>
    [SerializeField] private Sprite[] rewardIcons;
    /// <summary>当前展示的物品编号，异步图集回调以此识别已切换的格子。</summary>
    private int itemId;

    /// <summary>用当前奖励的背包图标、名称和单次件数刷新格子。</summary>
    /// <param name="item">本格物品的正式目录行。</param><param name="amount">每次结算或抽取这一项获得的件数。</param>
    public void Bind(ItemConfigBase item, int amount)
    {
        itemId = item.ItemId;
        quantity.text = "×" + amount;
        if (caption != null) caption.text = item.ItemName;
        // 常用图标可以直接显示，其他配置通过背包使用的资源加载器读取。
        for (int index = 0; index < rewardIcons.Length; index++)
        {
            // 切片的名称包含物品表路径末尾的扩展名。
            Sprite candidate = rewardIcons[index];
            if (item.Icon.EndsWith("/" + candidate.name, StringComparison.Ordinal))
            {
                icon.sprite = candidate;
                icon.enabled = true;
                return;
            }
        }
        icon.enabled = false;
        ResourceMgr.Instance.LoadSpriteAsync(item.Icon, sprite =>
        {
            if (itemId != item.ItemId) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        });
    }
}
