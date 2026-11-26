using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RO 赠礼窗口的可复用行，联系人点击选择，物品行通过加减按钮指定件数。</summary>
public sealed class GiftRow : MonoBehaviour
{
    /// <summary>好友昵称或物品配置名。</summary>
    [SerializeField] private Text caption;
    /// <summary>好友状态或当前选择件数。</summary>
    [SerializeField] private Text value;
    /// <summary>高亮当前收礼人和已选的物品。</summary>
    [SerializeField] private GameObject highlight;
    /// <summary>好友选择或物品数量加一的按钮。</summary>
    [SerializeField] private Button select;
    /// <summary>仅物品行使用的减一按钮。</summary>
    [SerializeField] private Button minus;
    /// <summary>物品行中的真实背包图标，好友行留空。</summary>
    [SerializeField] private Image itemIcon;
    /// <summary>允许直接输入寄出件数，较大堆叠无需重复点击加号。</summary>
    [SerializeField] private InputField amount;
    /// <summary>当前图标加载路径，用于识别复用后过期的异步结果。</summary>
    private string iconPath;
    /// <summary>把编辑好的数量交给控制器按本堆实际持有量裁剪。</summary>
    private Action<int> quantityEdited;
    /// <summary>点击当前行的业务回调，在重用时替换。</summary>
    private Action clicked;
    /// <summary>当前物品减少一件的业务回调。</summary>
    private Action decreased;

    /// <summary>只需注册一次点击；重绘时沿用当前行的回调。</summary>
    private void Awake()
    {
        select.onClick.AddListener(() => clicked?.Invoke());
        if (minus != null) minus.onClick.AddListener(() => decreased?.Invoke());
        if (amount != null) amount.onEndEdit.AddListener(EditQuantity);
    }

    /// <summary>物品行按背包正式图标和所选数刷新。</summary>
    /// <param name="path">背包物品表中的图集和子图路径。</param><param name="count">准备寄出的本堆件数。</param>
    /// <param name="changed">玩家结束输入时的数量更新操作。</param>
    internal void SetItem(string path, int count, Action<int> changed)
    {
        quantityEdited = changed;
        amount.SetTextWithoutNotify(count.ToString());
        if (iconPath == path) return;
        iconPath = path;
        itemIcon.enabled = false;
        if (string.IsNullOrEmpty(path)) return;
        ResourceMgr.Instance.LoadSpriteAsync(path, sprite =>
        {
            if (this == null || iconPath != path) return;
            itemIcon.sprite = sprite;
            itemIcon.enabled = sprite != null;
        });
    }

    /// <summary>把输入框中的非负整数交给当前物品堆的回调。</summary><param name="text">玩家提交的十进制件数。</param>
    private void EditQuantity(string text)
    {
        // 空串和无效输入清除该堆的选择。
        int.TryParse(text, out int count);
        quantityEdited?.Invoke(count);
    }

    /// <summary>显示联系人或物品并替换本行交互。</summary>
    /// <param name="name">当前好友名或物品名。</param><param name="detail">好友关系或已选数与持有数。</param>
    /// <param name="selected">是否绘制选中态。</param><param name="click">选择或加一操作。</param><param name="decrease">可减时传入对应操作。</param>
    internal void Bind(string name, string detail, bool selected, Action click, Action decrease)
    {
        caption.text = name;
        value.text = detail;
        highlight.SetActive(selected);
        clicked = click;
        decreased = decrease;
        select.interactable = click != null;
        if (minus != null) minus.interactable = decrease != null;
    }
}
