using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO ItemPrefab 的单个邮件附件，显示背包图标、件数及绑定标记。</summary>
public sealed class EmailAttachmentItem : MonoBehaviour
{
    /// <summary>物品格内的背包图标。</summary>
    [SerializeField] private Image icon;
    /// <summary>本地物品名，缺少配置时保留物品编号。</summary>
    [SerializeField] private Text itemName;
    /// <summary>物品格右下角显示的投递件数。</summary>
    [SerializeField] private Text quantity;
    /// <summary>附件绑定时显示的锁形角标。</summary>
    [SerializeField] private GameObject bound;
    /// <summary>卡片当前绑定的图集路径，异步返回时用来区分已经切换的物品。</summary>
    private string iconPath;

    /// <summary>使用附件数据及背包的图集加载器刷新卡片。</summary>
    /// <param name="item">包含物品编号、件数和绑定标记的单项附件。</param>
    internal void SetData(MailItem item)
    {
        // 名称与图集路径都取自这件物品的正式配置。
        ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(item.ItemId);
        itemName.text = config?.ItemName ?? item.ItemId.ToString();
        quantity.text = item.Count.ToString();
        bound.SetActive(item.IsBind);
        // 相同资源在一行重绘时可以直接延用。
        string path = config?.Icon ?? "";
        if (iconPath == path) return;
        iconPath = path;
        icon.enabled = false;
        if (path.Length == 0) return;
        ResourceMgr.Instance.LoadSpriteAsync(path, sprite =>
        {
            // 回调属于本格仍显示的物品时才显示它的背包图标。
            if (this == null || iconPath != path) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        });
    }
}
