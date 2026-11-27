using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>背包物品卡。显示物品表中的介绍、来源和按当前堆叠筛选的操作。</summary>
public sealed class MartialManualItemPopup : MonoBehaviour
{
    /// <summary>独立于背包格的卡面，用于适配横屏可用空间。</summary>
    [SerializeField] private RectTransform card;
    /// <summary>操作栏左侧的分隔线，仅有操作按钮时展示。</summary>
    [SerializeField] private GameObject railRule;
    /// <summary>随物品品质变化的标题底色。</summary>
    [SerializeField] private Image header;
    /// <summary>所选物品图标，由配置的图集或资源路径异步取得。</summary>
    [SerializeField] private Image icon;
    /// <summary>物品表里的显示名称。</summary>
    [SerializeField] private TMP_Text title;
    /// <summary>使用所需等级和具体物品类别。</summary>
    [SerializeField] private TMP_Text category;
    /// <summary>正文标题，在物品简介和获取途径之间切换。</summary>
    [SerializeField] private TMP_Text heading;
    /// <summary>容纳当前物品简介、绑定限制或获取途径的正文。</summary>
    [SerializeField] private TMP_Text description;
    /// <summary>库存合计或同名秘笈碎片的合成进度。</summary>
    [SerializeField] private TMP_Text stock;
    /// <summary>右侧操作栏从上到下排列的按钮。</summary>
    [SerializeField] private Button[] buttons;
    /// <summary>与操作按钮逐一对应的标题。</summary>
    [SerializeField] private TMP_Text[] labels;
    /// <summary>页脚的获取途径入口，物品表未填写来源时隐藏。</summary>
    [SerializeField] private Button sourceButton;
    /// <summary>切换来源前后的页脚按钮文案。</summary>
    [SerializeField] private TMP_Text sourceLabel;
    /// <summary>当前展示的物品，快速换格时也用于甄别图标回调。</summary>
    private cfg.ItemInfo selected;
    /// <summary>展示本堆叠的绑定或交易状态。</summary>
    private string restrictions;
    /// <summary>当前背包视口，切换简介和来源时让卡片继续贴着格子。</summary>
    private RectTransform viewport;
    /// <summary>为真时正显示获取途径，再次点击回到简介。</summary>
    private bool showingSource;

    /// <summary>注册固定按钮，换格时只更新具体物品的操作。</summary>
    private void Awake()
    {
        sourceButton.onClick.AddListener(() => { showingSource = !showingSource; RefreshBody(); });
    }

    /// <summary>按下鼠标或触屏时收起卡面外的详情，保留同一次点击给背包格和页签。</summary>
    private void Update()
    {
        // 当前点击设备给出这次按下的屏幕坐标，触屏使用主触点。
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        // 叠加画布的投影相机为空，相机画布使用自身的事件相机。
        Canvas canvas = card.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.worldCamera;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) camera = null;
        if (!RectTransformUtility.RectangleContainsScreenPoint(card, pointer.position.ReadValue(), camera)) Hide();
    }

    /// <summary>读取所点物品的表格字段并将可执行的按钮绑定到右侧。</summary>
    /// <param name="item">当前格的物品行。</param><param name="owned">背包总数或合成进度。</param>
    /// <param name="restriction">该实例的绑定和交易限制。</param><param name="actions">按物品表顺序排列的操作。</param>
    /// <param name="grid">正在使用的背包格视口，卡片避开该区域。</param>
    public void ShowDetails(cfg.ItemInfo item, string owned, string restriction,
        List<KeyValuePair<string, Action>> actions, RectTransform grid)
    {
        gameObject.SetActive(true);
        viewport = grid;
        selected = item;
        restrictions = restriction;
        showingSource = false;
        title.text = item.Name;
        category.text = item.DetailCategory;
        if (item.NeedLevel > 0) category.text = "使用等级：" + item.NeedLevel + "级\n" + item.DetailCategory;
        stock.text = owned;
        sourceButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(item.Source));
        header.color = new Color32(77, 85, 87, 255);
        switch (item.Quality)
        {
            case 2: header.color = new Color32(48, 97, 77, 255); break;
            case 3: header.color = new Color32(48, 79, 123, 255); break;
            case 4: header.color = new Color32(99, 57, 119, 255); break;
            case 5: header.color = new Color32(126, 93, 49, 255); break;
        }
        icon.enabled = false;
        ResourceMgr.Instance.LoadSpriteAsync(item.Icon, sprite =>
        {
            if (this == null || selected != item) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        });
        // 按钮复用时清除旧物品的操作，未配置的按钮收起。
        for (int index = 0; index < buttons.Length; index++)
        {
            buttons[index].onClick.RemoveAllListeners();
            buttons[index].gameObject.SetActive(index < actions.Count);
            if (index >= actions.Count) continue;
            // 将本按钮的标题、业务一起捕获，避免切换物品后仍使用上一堆。
            KeyValuePair<string, Action> action = actions[index];
            labels[index].text = action.Key;
            buttons[index].onClick.AddListener(() => { Hide(); action.Value(); });
        }
        // 没有可执行操作时收起右栏，只占用正文宽度。
        bool hasActions = actions.Count > 0;
        railRule.SetActive(hasActions);
        // 预制体首个按钮的位置定义展开宽度，品质标题栏定义仅阅读时的宽度。
        RectTransform firstButton = (RectTransform)buttons[0].transform;
        float width = header.rectTransform.rect.width + 4f; // 只读卡的宽度含两侧描边。
        float titleWidth = header.rectTransform.rect.width - title.rectTransform.anchoredPosition.x - 8f; // 标题与品质栏右侧留八个画布单位。
        if (hasActions)
        {
            width = firstButton.anchoredPosition.x + firstButton.rect.width + 10f;
        }
        card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, titleWidth);
        RefreshBody();
    }

    /// <summary>在原卡片中切换简介和表内来源，浏览期间仍能选择其他物品。</summary>
    private void RefreshBody()
    {
        if (showingSource)
        {
            heading.text = "获取途径";
            description.text = selected.Source;
            sourceLabel.text = "返回简介";
        }
        else
        {
            heading.text = "物品简介";
            description.text = selected.Desc;
            if (restrictions.Length > 0) description.text += "\n<color=#EF9487>" + restrictions + "</color>";
            sourceLabel.text = "获取途径";
        }
        // 用当前字号和正文列宽测量实际行高，短说明只占一行。
        RectTransform body = description.rectTransform;
        float textHeight = Mathf.Ceil(description.GetPreferredValues(description.text, body.rect.width, Mathf.Infinity).y) + 2f;
        body.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
        // 页脚以底边为锚点，正文末尾与数量栏之间保留十个画布单位。
        float height = -body.anchoredPosition.y + textHeight + stock.rectTransform.anchoredPosition.y + 10f;
        // 三项操作的材料仍须完整显示最后一个按钮。
        for (int index = 0; index < buttons.Length; index++)
        {
            if (!buttons[index].gameObject.activeSelf) continue;
            RectTransform button = (RectTransform)buttons[index].transform; // 当前可见操作的下边缘。
            height = Mathf.Max(height, -button.anchoredPosition.y + button.rect.height + 8f);
        }
        card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        PlaceBeside(viewport);
    }

    /// <summary>按画布和背包视口的实际边界在较宽一侧对齐卡片上沿，保留背包的点击与滚动。</summary>
    /// <param name="grid">本次打开详情的背包视口。</param>
    private void PlaceBeside(RectTransform grid)
    {
        // 将最外层画布和格子视口换算到同一局部坐标系，适配不同宽高比和画布缩放。
        RectTransform screen = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        // 四角按左下、左上、右上、右下排列，读取角点避免把视口中滚动的格子也计入边界。
        Vector3[] corners = new Vector3[4];
        screen.GetWorldCorners(corners);
        Vector3 min = transform.InverseTransformPoint(corners[0]); // 屏幕左下边缘。
        Vector3 max = transform.InverseTransformPoint(corners[2]); // 屏幕右上边缘。
        grid.GetWorldCorners(corners);
        Vector3 left = transform.InverseTransformPoint(corners[1]); // 视口左上角。
        Vector3 right = transform.InverseTransformPoint(corners[2]); // 视口右上角。
        // 两侧留白决定卡片展示的一侧，常规横屏位于背包左边。
        float freeLeft = left.x - min.x;
        float freeRight = max.x - right.x;
        // 边缘间距随当前画布一同缩放，卡面保持原有宽高比。
        float scale = Mathf.Min(1f, (Mathf.Max(freeLeft, freeRight) - 16f) / card.rect.width,
            (max.y - min.y - 16f) / card.rect.height);
        scale = Mathf.Max(0.1f, scale);
        card.localScale = Vector3.one * scale;
        float halfWidth = card.rect.width * scale * 0.5f; // 缩放后的半宽。
        float halfHeight = card.rect.height * scale * 0.5f; // 缩放后的半高。
        float x = left.x - halfWidth - 8f; // 优先靠近视口的左边沿。
        if (freeRight > freeLeft) x = right.x + halfWidth + 8f;
        float y = Mathf.Clamp(left.y - halfHeight, min.y + halfHeight + 8f, max.y - halfHeight - 8f); // 屏内的卡片中心。
        card.localPosition = new Vector3(x, y, 0f);
    }

    /// <summary>关闭详情而不改变当前背包页签和滚动位置。</summary>
    private void Hide() { gameObject.SetActive(false); }
}
