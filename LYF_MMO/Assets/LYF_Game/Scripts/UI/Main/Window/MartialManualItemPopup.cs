using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>背包内秘笈整本与同名碎片的详情卡，按钮交给背包控制器处理。</summary>
public sealed class MartialManualItemPopup : MonoBehaviour
{
    /// <summary>物品头部底板，随配置品级着色。</summary>
    [SerializeField] private Image header;
    /// <summary>当前背包物品的图集切片。</summary>
    [SerializeField] private Image icon;
    /// <summary>书名，碎片物品另带碎片后缀。</summary>
    [SerializeField] private TMP_Text title;
    /// <summary>物品说明中的使用等级和类别。</summary>
    [SerializeField] private TMP_Text category;
    /// <summary>合成配方、现有被动说明与已知兑换点数。</summary>
    [SerializeField] private TMP_Text description;
    /// <summary>碎片数量或当前整本数量。</summary>
    [SerializeField] private TMP_Text stock;
    /// <summary>培养按钮打开当前书的升级进阶页。</summary>
    [SerializeField] private Button cultivate;
    /// <summary>碎片合成一本，整本用于领悟。</summary>
    [SerializeField] private Button useItem;
    /// <summary>根据物品种类显示合成或领悟。</summary>
    [SerializeField] private TMP_Text useLabel;
    /// <summary>点卡片外或关闭标记收起详情。</summary>
    [SerializeField] private Button backdrop, close;
    /// <summary>当前请求的图标地址，防止快速切换时旧回调覆盖。</summary>
    private string iconPath;

    /// <summary>绑定本次物品的数据与按钮，名称、配方及品级来自配置。</summary>
    /// <param name="item">背包物品配置。</param><param name="book">该物品对应的书。</param>
    /// <param name="fragment">是否为合成碎片。</param><param name="held">持有的物品件数。</param>
    /// <param name="train">打开培养的当前书。</param><param name="use">提交本物品的合成或领悟操作。</param>
    public void Show(cfg.ItemInfo item, cfg.MartialManualInfo book, bool fragment, int held, Action train, Action use)
    {
        gameObject.SetActive(true);
        title.text = item.Name;
        category.text = "整本秘笈";
        useLabel.text = "领悟";
        stock.text = "◆  持有整本  " + held;
        if (fragment)
        {
            category.text = "碎片";
            useLabel.text = "合成";
            stock.text = "◆  合成《" + book.Name + "》    " + held + " / " + book.FragmentCount;
        }
        if (item.NeedLevel > 0) category.text = "使用等级：" + item.NeedLevel + "级\n" + category.text;
        description.text = item.Desc;
        header.color = new Color32(77, 85, 87, 255);
        switch (item.Quality)
        {
            case 2: header.color = new Color32(48, 97, 77, 255); break;
            case 3: header.color = new Color32(48, 79, 123, 255); break;
            case 4: header.color = new Color32(99, 57, 119, 255); break;
            case 5: header.color = new Color32(126, 93, 49, 255); break;
        }
        icon.enabled = false;
        iconPath = item.Icon;
        ResourceMgr.Instance.LoadSpriteAsync(item.Icon, sprite =>
        {
            if (this == null || item.Icon != iconPath) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        });
        cultivate.onClick.RemoveAllListeners();
        useItem.onClick.RemoveAllListeners();
        cultivate.onClick.AddListener(() => { Hide(); train(); });
        useItem.onClick.AddListener(() => { Hide(); use(); });
    }

    /// <summary>为固定关闭入口绑定一次回调。</summary>
    private void Awake()
    {
        backdrop.onClick.AddListener(Hide);
        close.onClick.AddListener(Hide);
    }

    /// <summary>关闭物品卡并保留背包的滚动位置。</summary>
    private void Hide() { gameObject.SetActive(false); }
}
