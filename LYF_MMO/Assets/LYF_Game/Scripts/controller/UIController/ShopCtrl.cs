using System;
using System.Collections.Generic;
using cfg;
using MMoRpgCommon;

/// <summary>从当前 NPC、商品配置和角色背包准备商店页面，并提交玩家确认的购买份数。</summary>
public sealed class ShopCtrl : CtrlBase
{
    /// <summary>主界面已注册的 NPC 商店窗口。</summary>
    private readonly NpcShopWindow _shopWindow;
    /// <summary>当前商人实体，商店开放期间用于重读商品配置。</summary>
    private NpcEntity _currentNpc;
    /// <summary>当前角色背包，库存及货币变化时刷新正在浏览的商品。</summary>
    private RoItemComponent _items;

    /// <summary>连接商店窗口和购买回包。</summary>
    /// <param name="mainView">持有商店窗口的主城视图。</param>
    public ShopCtrl(MainView mainView) : base(mainView)
    {
        _shopWindow = mainView.ShopWindow;
        if (_shopWindow != null)
        {
            _shopWindow.BuyRequested += OnBuyRequested;
            _shopWindow.FrameUpdated += OnShopWindowFrameUpdated;
        }
        ShopMgr.Instance.PurchaseCompleted += OnPurchaseCompleted;
    }

    /// <summary>离开主城时解除与窗口、角色背包和购买模型的订阅。</summary>
    public override void Dispose()
    {
        ShopMgr.Instance.PurchaseCompleted -= OnPurchaseCompleted;
        if (_items != null) _items.KnapsackChangedDetailed -= OnBagChanged;
        if (_shopWindow != null)
        {
            _shopWindow.BuyRequested -= OnBuyRequested;
            _shopWindow.FrameUpdated -= OnShopWindowFrameUpdated;
        }
    }

    /// <summary>从 NPC 对话或主界面入口打开货架，默认选中首件商品。</summary>
    /// <param name="npcEntity">玩家正在交互的商人。</param>
    public void OpenNpcShop(NpcEntity npcEntity)
    {
        if (_shopWindow == null) return;
        _currentNpc = npcEntity;
        if (_items != null) _items.KnapsackChangedDetailed -= OnBagChanged;
        // 场景角色持有的背包与商城购买校验共用同一份虚拟物品余额。
        PlayerEntity role = Global.Instance?.SceneWorld?.LocalRole;
        _items = null;
        if (role != null) role.TryGetComponent(out _items);
        if (_items != null) _items.KnapsackChangedDetailed += OnBagChanged;
        _shopWindow.Show(true);
        RefreshShopWindow(true);
    }

    /// <summary>从当前商店表读取货架，刷新期间保留搜索词、选中商品和购买份数。</summary>
    /// <param name="opening">新开商店时重置上次浏览位置。</param>
    private void RefreshShopWindow(bool opening)
    {
        // 无商人或无有效配置时交给窗口展示空货架。
        List<ShopGoodsViewData> goods = new List<ShopGoodsViewData>();
        string title = "商店";
        if (_currentNpc != null)
        {
            // 接入 NPC 所属的商店表，具体货币以每行商品的支付物品为准。
            ShopTable shop = LubanMgr.Instance.GetShopTableById(_currentNpc.ShopId);
            if (shop != null && shop.ShopType == (int)ShopType.Npc)
            {
                // 表里的 NPC 前缀供配置分类；窗口标题显示商店名称，缺省时显示商人的名字。
                title = shop.Name ?? string.Empty;
                if (title.StartsWith("NPC", StringComparison.Ordinal)) title = title.Substring(3).Trim();
                if (string.IsNullOrEmpty(title)) title = _currentNpc.Name ?? "商店";
                // 当前所有商店的商品字典，按 GoodsId 排序使每次打开的陈列顺序一致。
                Dictionary<int, ShopItemInfo> entries = LubanMgr.Instance.GetShopItemInfos();
                if (entries != null)
                {
                    // 快照保留原配置对象，只陈列当前商店里实际存在的物品。
                    List<ShopItemInfo> rows = new List<ShopItemInfo>(entries.Values);
                    for (int index = 0; index < rows.Count; index++)
                    {
                        // 货架配置及其对应的物品定义。
                        ShopItemInfo row = rows[index];
                        if (row == null || row.ShopId != shop.Id) continue;
                        ItemConfigBase item = LubanMgr.Instance.GetItemConfigById(row.ItemId);
                        if (item != null) goods.Add(BuildGoods(row, item));
                    }
                }
            }
        }
        goods.Sort((left, right) => left.GoodsId.CompareTo(right.GoodsId));
        _shopWindow.SetGoods(title, goods, opening);
    }

    /// <summary>组装一格商品与其详情，价格、余额和图标使用同一货币物品编号。</summary>
    /// <param name="row">当前 NPC 货架的商品行。</param>
    /// <param name="item">购买后实际发放的物品配置。</param>
    private ShopGoodsViewData BuildGoods(ShopItemInfo row, ItemConfigBase item)
    {
        // 非通用货币沿用本身的物品图标和显示名。
        ItemConfigBase money = LubanMgr.Instance.GetItemConfigById(row.CurrencyItemId);
        string icon = money?.Icon ?? string.Empty;
        string currencyName = money?.ItemName ?? "货币 " + row.CurrencyItemId;
        // 公共物品枚举与服务端扣款编号一致，显示名称与背包货币栏一致。
        const string iconPrefix = "Icon/Currency/Jianghu_";
        switch ((ItemEnum)row.CurrencyItemId)
        {
            case ItemEnum.ZENY:
                currencyName = "铜币";
                icon = iconPrefix + "Copper.png";
                break;
            case ItemEnum.ROMONEY:
                currencyName = "银两";
                icon = iconPrefix + "Silver.png";
                break;
            case ItemEnum.SILVER_TICKET:
                currencyName = "银票";
                icon = iconPrefix + "Ticket.png";
                break;
            case ItemEnum.DIAMOND:
                currencyName = "元宝";
                icon = iconPrefix + "Yuanbao.png";
                break;
        }
        // 包裹持有量包含绑定物品，支付余额遵循 ShopMgr 的可扣款口径。
        int owned = 0;
        long balance = 0;
        if (_items != null)
        {
            owned = Math.Max(0, _items.GetItemNum(row.ItemId, ItemBind.kItemBindIs));
            balance = _items.GetVirtualItemCount(row.CurrencyItemId);
        }
        // 次数限制和角色余额同时约束“满”的份数，服务端逐行使用 int 结算总价。
        int remaining = ShopMgr.Instance.GetRemainingPurchaseCount(row.GoodsId);
        int maximum = remaining;
        if (row.Price > 0)
            maximum = (int)Math.Min(remaining, Math.Min(balance, int.MaxValue) / row.Price);
        // 同店内可混售每日限购和永久限购商品，说明按选中商品生成。
        CountInfo limit = LubanMgr.Instance.GetCountInfo(RoleCountAction.ShopPurchase, row.GoodsId);
        bool unlimited = limit == null || limit.Limit <= 0;
        string limitText = "不限购";
        if (!unlimited)
        {
            limitText = "还可购买　" + remaining;
            if (limit.RefreshType == 1) limitText = "今日还可购　" + remaining;
        }
        return new ShopGoodsViewData(row.ShopId, row.GoodsId, item.ItemName, item.Icon,
            row.Price, row.CurrencyItemId, currencyName, icon, remaining, maximum, unlimited)
        {
            Description = item.Description,
            BindAfterPurchase = row.BindAfterPurchase == 1,
            IsEquipment = item.ItemMainType == ItemMainType.ItemType_Equip,
            OwnedCount = owned,
            Balance = balance,
            LimitText = limitText
        };
    }

    /// <summary>背包推送到账时更新持有量与可支付份数。</summary>
    /// <param name="change">触发刷新的背包变更，可能同时影响商品数量和支付余额。</param>
    private void OnBagChanged(KnapsackChangedEventArgs change)
    {
        if (_shopWindow != null && _shopWindow.isActiveAndEnabled) RefreshShopWindow(false);
    }

    /// <summary>继续沿用次数脏标记接收服务端的限购更新。</summary>
    private void OnShopWindowFrameUpdated()
    {
        // 只有可见商店发出帧事件；次数变化时再重读货架。
        PlayerEntity role = Global.Instance?.SceneWorld?.LocalRole;
        if (role == null || role.DirtyTracker == null) return;
        // 本轮商店次数变化，货架读完后沿用原控制器的清理时机。
        if (!role.DirtyTracker.TryGet(ClientDirtyModule.RoleCount, out ClientDirtySnapshot snapshot)) return;
        RefreshShopWindow(false);
        role.DirtyTracker.Clear(ClientDirtyModule.RoleCount);
    }

    /// <summary>右侧购买按钮确认份数后，沿现有商店请求提交。</summary>
    /// <param name="goods">确认时右侧详情对应的商品。</param>
    /// <param name="quantity">玩家选择的购买份数。</param>
    private void OnBuyRequested(ShopGoodsViewData goods, int quantity)
    {
        if (ShopMgr.Instance.IsPurchasePending())
        {
            TipsMgr.Instance.ShowSystemTips("购买请求处理中");
            return;
        }
        ShopMgr.Instance.SendBuyRequest(goods.ShopId, goods.GoodsId, quantity);
    }

    /// <summary>展示原购买模型返回的结果提示，实际到账由角色背包和次数推送驱动。</summary>
    /// <param name="success">服务器是否接受本次购买。</param>
    /// <param name="goodsId">本次回包对应的商品编号。</param>
    /// <param name="message">购买结果的玩家可读说明。</param>
    private void OnPurchaseCompleted(bool success, int goodsId, string message)
    {
        TipsMgr.Instance.ShowSystemTips(message);
    }
}
