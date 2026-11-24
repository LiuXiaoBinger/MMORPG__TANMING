using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>交易目录商品与包裹格子，整格、关注和求购三个点击区域独立。</summary>
public sealed class TradeMarketCard : MonoBehaviour
{
    /// <summary>点选商品或包裹格子进入详情。</summary>
    [SerializeField] private Button _open;
    /// <summary>原物品或装备图标。</summary>
    [SerializeField] private Image _icon;
    /// <summary>按物品品质着色的图标底板。</summary>
    [SerializeField] private Image _quality;
    /// <summary>目录中的物品名，包裹小格无需显示。</summary>
    [SerializeField] private Text _name;
    /// <summary>目录状态或该包裹堆叠的实际数量。</summary>
    [SerializeField] private Text _summary;
    /// <summary>加入或移除关注的心形按钮。</summary>
    [SerializeField] private Button _heart;
    /// <summary>心形的高亮颜色跟随关注状态。</summary>
    [SerializeField] private Image _heartInk;
    /// <summary>打开物品求购单或拍品出价单的小按钮。</summary>
    [SerializeField] private Button _order;
    /// <summary>单据操作的显示文字。</summary>
    [SerializeField] private Text _orderText;

    /// <summary>以配置及当前页面状态更新整格内容。</summary>
    /// <param name="item">道具或装备配置。</param>
    /// <param name="icon">项目物品表使用的实际精灵。</param>
    /// <param name="summary">在售状态、求购状态或背包数量。</param>
    /// <param name="open">点整格时打开当前物品。</param>
    /// <param name="favorite">是否关注该商品。</param>
    /// <param name="toggle">允许关注的页面传入切换操作。</param>
    /// <param name="action">按钮的求购或出价标题。</param>
    /// <param name="order">打开对应单据的操作。</param>
    public void Bind(ItemConfigBase item, Sprite icon, string summary, Action open, bool favorite, Action toggle, string action, Action order)
    {
        _icon.sprite = icon;
        _icon.enabled = icon != null;
        if (_name != null) _name.text = item.ItemName;
        _summary.text = summary;
        // 道具品质取实际表值，装备图标沿用录屏的暖铜底板。
        var info = LubanMgr.Instance.GetItemInfoById(item.ItemId);
        _quality.color = new Color32(117, 82, 54, 255);
        if (info != null)
        {
            switch (info.Quality)
            {
                case 1: _quality.color = new Color32(76, 91, 94, 255); break;
                case 2: _quality.color = new Color32(57, 97, 73, 255); break;
                case 3: _quality.color = new Color32(55, 81, 108, 255); break;
                case 4: _quality.color = new Color32(94, 72, 112, 255); break;
            }
        }
        _open.onClick.RemoveAllListeners();
        _open.onClick.AddListener(() => open());
        if (_heart != null)
        {
            _heart.gameObject.SetActive(toggle != null);
            _heart.onClick.RemoveAllListeners();
            if (toggle != null) _heart.onClick.AddListener(() => toggle());
            _heartInk.color = Color.white;
            if (favorite) _heartInk.color = new Color32(255, 123, 131, 255);
        }
        if (_order != null)
        {
            _order.gameObject.SetActive(order != null);
            _orderText.text = action;
            _order.onClick.RemoveAllListeners();
            if (order != null) _order.onClick.AddListener(() => order());
        }
    }
}
