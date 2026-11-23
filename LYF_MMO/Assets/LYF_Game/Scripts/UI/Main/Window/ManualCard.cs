using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>列表书卡与槽位共用的视图，Unity 拖放事件要求实现对应的事件系统接口。</summary>
public sealed class ManualCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    /// <summary>卡片可点击区域。</summary>
    [SerializeField] private Button button;
    /// <summary>封面，空槽时隐藏。</summary>
    [SerializeField] private Image cover;
    /// <summary>书名或空槽标志。</summary>
    [SerializeField] private TMP_Text title;
    /// <summary>等级、残页进度或已装备状态。</summary>
    [SerializeField] private TMP_Text subtitle;
    /// <summary>当前选择的描边。</summary>
    [SerializeField] private GameObject highlight;
    /// <summary>这张卡片当前展示的书籍编号。</summary>
    private int book;
    /// <summary>拖入此槽时接收的书籍编号。</summary>
    private Action<int> dropped;
    /// <summary>浏览目录时把纵向拖动交回所在滚动区域。</summary>
    private ScrollRect scroll;
    /// <summary>当前拖动的封面副本，随拖动结束释放。</summary>
    private Image dragIcon;

    /// <summary>以同一套预制体显示目录、配置列表与出战槽。</summary>
    /// <param name="id">书籍编号，零为空槽。</param><param name="sprite">书籍图像。</param>
    /// <param name="nameText">主标题。</param><param name="note">第二行进度。</param>
    /// <param name="active">是否描边。</param><param name="click">单击行为。</param><param name="drop">配置槽放置回调。</param>
    public void Bind(int id, Sprite sprite, string nameText, string note, bool active, Action click, Action<int> drop)
    {
        book = id;
        dropped = drop;
        title.text = nameText;
        subtitle.text = note;
        cover.sprite = sprite;
        cover.enabled = sprite != null;
        highlight.SetActive(active);
        button.onClick.RemoveAllListeners();
        if (click != null) button.onClick.AddListener(click.Invoke);
    }

    /// <summary>复制当前封面用于指针拖动。</summary><param name="eventData">Unity 指针事件。</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!GetComponentInParent<MartialManualPanel>().Editing)
        {
            scroll = GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.OnBeginDrag(eventData);
            return;
        }
        if (book == 0) return;
        dragIcon = Instantiate(cover, GetComponentInParent<Canvas>().rootCanvas.transform);
        dragIcon.raycastTarget = false;
        dragIcon.rectTransform.sizeDelta = new Vector2(80, 80);
        OnDrag(eventData);
    }

    /// <summary>在根画布平面跟随指针。</summary><param name="eventData">当前屏幕坐标与相机。</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (scroll != null) scroll.OnDrag(eventData);
        if (dragIcon == null) return;
        // 将屏幕像素转换成当前根画布的世界坐标。
        Vector3 world;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)dragIcon.transform.parent,
            eventData.position, eventData.pressEventCamera, out world)) dragIcon.transform.position = world;
    }

    /// <summary>清理临时封面。</summary><param name="eventData">释放指针。</param>
    public void OnEndDrag(PointerEventData eventData) { if (scroll != null) scroll.OnEndDrag(eventData); scroll = null; ClearDrag(); }

    /// <summary>窗口在拖动中被关闭时也收起封面。</summary>
    private void OnDisable() { ClearDrag(); }

    /// <summary>移除本次拖动创建的装饰图。</summary>
    private void ClearDrag() { if (dragIcon != null) Destroy(dragIcon.gameObject); dragIcon = null; }

    /// <summary>把源卡片的书籍编号交给该槽。</summary><param name="eventData">此次拖放的源。</param>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || dropped == null) return;
        // 拖动源可能是卡片中的子物体。
        ManualCard source = eventData.pointerDrag.GetComponentInParent<ManualCard>();
        if (source != null && source.book > 0) dropped.Invoke(source.book);
    }
}
