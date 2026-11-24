using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>商店货架上的一格，展示商品和单价，点击后切换右侧详情。</summary>
public sealed class NpcShopGoodsCard : MonoBehaviour
{
    /// <summary>货架整张卡片上的选择按钮。</summary>
    [SerializeField] private Button _button;
    /// <summary>选中时覆盖卡片的描边与底色。</summary>
    [SerializeField] private GameObject _selection;
    /// <summary>商品图标，右侧详情复用同一张已加载的图。</summary>
    [SerializeField] private Image _product;
    /// <summary>单价旁的结算货币图标。</summary>
    [SerializeField] private Image _currency;
    /// <summary>物品配置中的中文名。</summary>
    [SerializeField] private Text _name;
    /// <summary>一份商品的货币数量。</summary>
    [SerializeField] private Text _price;
    /// <summary>商品购买后绑定时显示的锁形图标。</summary>
    [SerializeField] private GameObject _lock;
    /// <summary>剩余限购份数为零时显示的售罄标记。</summary>
    [SerializeField] private GameObject _soldOut;
    /// <summary>当前格子实际代表的商品。</summary>
    public ShopGoodsViewData Goods { get; private set; }
    /// <summary>选中货物时供详情区直接使用的物品精灵。</summary>
    public Sprite ProductSprite => _product.sprite;
    /// <summary>选中货物时供结算区使用的货币精灵。</summary>
    public Sprite CurrencySprite => _currency.sprite;
    /// <summary>玩家在货架上选择此格时通知窗口。</summary>
    public event Action<NpcShopGoodsCard> Selected;
    /// <summary>图标异步到达后通知正在展示此商品的详情区。</summary>
    public event Action<NpcShopGoodsCard> IconsChanged;

    /// <summary>把整格点击绑定到窗口的选择事件。</summary>
    private void Awake()
    {
        _button.onClick.AddListener(() => Selected?.Invoke(this));
    }

    /// <summary>重绑商品或更新限购状态，同地址的图标直接复用。</summary>
    /// <param name="goods">当前格子要陈列的商品数据。</param>
    public void Bind(ShopGoodsViewData goods)
    {
        // 保留上次绑定的资源地址，购买后刷新余额时无需重新读图。
        ShopGoodsViewData previous = Goods;
        Goods = goods;
        _name.text = goods.ProductName;
        _price.text = goods.Price.ToString("N0");
        _lock.SetActive(goods.BindAfterPurchase);
        _soldOut.SetActive(!goods.IsUnlimitedPurchase && goods.RemainingPurchaseCount == 0);
        if (previous == null || previous.ProductIconPath != goods.ProductIconPath)
            LoadIcon(_product, goods.ProductIconPath);
        if (previous == null || previous.CurrencyIconPath != goods.CurrencyIconPath)
            LoadIcon(_currency, goods.CurrencyIconPath);
    }

    /// <summary>绑定完成前隐藏空图，只把当前地址返回的精灵交给卡片与详情。</summary>
    /// <param name="target">接收精灵的商品或货币控件。</param>
    /// <param name="path">该控件本次请求的 UISprites 相对地址。</param>
    private void LoadIcon(Image target, string path)
    {
        target.sprite = null;
        target.enabled = false;
        if (string.IsNullOrEmpty(path)) return;
        ResourceMgr.Instance.LoadSpriteAsync(path, sprite =>
        {
            // 控件复用时按当前商品的地址确认归属，避免迟到的另一种货币覆盖价格。
            if (this == null) return;
            string currentPath = Goods.ProductIconPath;
            if (target == _currency) currentPath = Goods.CurrencyIconPath;
            if (currentPath != path) return;
            target.sprite = sprite;
            target.enabled = sprite != null;
            IconsChanged?.Invoke(this);
        });
    }

    /// <summary>显示当前商品的选中底色，其余商品保持普通陈列。</summary>
    /// <param name="selected">此格是否与右侧详情对应。</param>
    public void SetSelected(bool selected)
    {
        _selection.SetActive(selected);
    }
}
