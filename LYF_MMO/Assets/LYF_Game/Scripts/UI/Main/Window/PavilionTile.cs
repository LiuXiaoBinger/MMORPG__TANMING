using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>珍宝阁的可复用货架卡片，布局与可选内容均由对应的预制体模板决定。</summary>
public sealed class PavilionTile : MonoBehaviour
{
    /// <summary>整张卡片的点选区域。</summary>
    public Button pick;
    /// <summary>商品、礼包或分类的主标题。</summary>
    public Text title;
    /// <summary>礼包赠送、限额或其他补充信息。</summary>
    public Text note;
    /// <summary>价格、档位或领取状态。</summary>
    public Text price;
    /// <summary>商品或充值档位的主图。</summary>
    public Image picture;
    /// <summary>与价格配对的货币，空时隐藏。</summary>
    public Image coin;
    /// <summary>当前选中或当前分类的边框。</summary>
    public GameObject selection;
    /// <summary>商城卡片的关注按钮。</summary>
    public Button favorite;
    /// <summary>收藏的空心和实心图标。</summary>
    public Sprite[] stars;
    /// <summary>返利行的奖品图标槽。</summary>
    public Image[] prizes;
    /// <summary>每个奖品槽的数量标签。</summary>
    public Text[] counts;
    /// <summary>绑定商品的锁定标记。</summary>
    public GameObject bound;

    /// <summary>复用卡片时替换上一次的点选回调。</summary>
    /// <param name="action">本次选中的商品、类别或档位操作。</param>
    public void OnPick(Action action)
    {
        pick.onClick.RemoveAllListeners();
        pick.onClick.AddListener(() => action());
    }

    /// <summary>更新可选图层，空图隐藏其渲染。</summary>
    /// <param name="target">模板上的图层。</param>
    /// <param name="sprite">该商品的实际图标或货币图标。</param>
    public static void SetPicture(Image target, Sprite sprite)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
    }
}
