using TMPro;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>背包格子视图，只负责显示控制器提供的物品数据和指针反馈。</summary>
public class KnapsackSlotWidget : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IDropHandler
{

    #region 字段
    /// <summary>当前格子物品图标；数量不大于零时隐藏。</summary>
    [SerializeField, Header("物品图标")] private Image _imgIcon;

    /// <summary>显示当前堆叠数量的文本。</summary>
    [SerializeField, Header("物品数量")] private TMP_Text _texCount;

    /// <summary>有物品时鼠标悬停显示的格子高亮。</summary>
    [SerializeField, Header("鼠标进入效果")] private Image _imgEnter;

    /// <summary>物品效果图片，本组件清空时隐藏，当前刷新没有开启它。</summary>
    [SerializeField, Header("物品特效")] private Image _imgFx;

    /// <summary>当前格子显示的物品数量。</summary>
    private int _count;

    /// <summary>兼容旧资源记录的图标名称。</summary>
    private string _spriteName;

    /// <summary>当前物品类型编号，图标回调只在编号仍一致时写入。</summary>
    private int _itemTypeId;

    /// <summary>当前格子物品唯一编号，用于点击真实装备实例。</summary>
    private long _itemUid;

    /// <summary>玩家点击格子时上报实例 UID，具体业务由控制器判断。</summary>
    public event Action<long> ItemSelected;
    #endregion

    #region 界面刷新
    /// <summary>
    /// 使用控制器生成的物品展示数据刷新格子。
    /// </summary>
    public void RefreshUI(KnapsackSlotViewData itemData)
    {
        Clear();
        if (itemData == null)
        {
            return;
        }

        _count = itemData.Count;
        _itemTypeId = itemData.ItemTypeId;
        _itemUid = itemData.ItemUid;
        if (_count <= 0)
        {
            return;
        }

        _imgIcon.gameObject.Show();
        _texCount.gameObject.Show();
        _texCount.SetText($"{_count}");

        if (_itemTypeId <= 0)
        {
            return;
        }

        // 本次请求所属物品类型，不是背包格号或物品唯一 ID。
        int requestedItemTypeId = _itemTypeId;
        // 优先用展示数据路径，未提供时按既有物品图标命名规则拼接。
        string iconPath = itemData.IconPath;
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            iconPath = $"Icon/Item/{GetIconName(requestedItemTypeId)}";
        }
        ResourceMgr.Instance.LoadSpriteAsync(iconPath, sprite =>
        {
            if (_itemTypeId == requestedItemTypeId && sprite != null)
            {
                _imgIcon.sprite = sprite;
            }
        });
    }

    /// <summary>只更新数量文本，避免数量变化时重复加载图标和重置格子状态。</summary>
    /// <param name="count">最新堆叠数量。</param>
    public void RefreshCount(int count)
    {
        _count = count;
        if (_count <= 0)
        {
            Clear();
            return;
        }

        _texCount.gameObject.Show();
        _texCount.SetText($"{_count}");
    }
    #endregion

    #region 交互响应
    /// <summary>鼠标进入格子时只显示选中高亮，不再弹出旧物品详情；详情由左键点击链路打开。</summary>
    /// <param name="eventData">Unity 指针进入事件；本函数不读取按键与坐标，参数仅为满足 IPointerEnterHandler 接口签名而保留。</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_count > 0)
        {
            _imgEnter.gameObject.Show();
        }
    }

    /// <summary>鼠标离开格子时关闭选中效果和物品提示。</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _imgEnter.gameObject.Show(false);
        TipsMgr.Instance.CloseItemTips();
    }
    #endregion

    #region 事件回调
    /// <summary>Unity 指针按下接口的空实现；本方法不记录状态。</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
    }

    /// <summary>左键在未拖拽的物品格上抬起时通知上层打开详情。</summary>
    /// <param name="eventData">Unity 指针事件，提供按键和拖拽状态。</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging || _itemUid <= 0)
        {
            return;
        }
        ItemSelected?.Invoke(_itemUid);
    }

    /// <summary>识别背包拖拽来源；交换位置仍是 TODO，当前不会提交移动请求。</summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        // 被拖动对象上的类型标记，用于判断它是否来自背包。
        UIDragWidget uiDragWidget = eventData.pointerDrag.GetComponent<UIDragWidget>();
        if (uiDragWidget != null && uiDragWidget._dragType == DragType.KanpsackSlot)
        {
            // TODO: 将背包位置交换请求提交给服务器。
        }
    }
    #endregion

    /// <summary>为 itemTypeId 生成现有资源文件名；2003 和 2005 使用小写 item_，其他使用 Item_。</summary>
    private static string GetIconName(int itemTypeId)
    {
        // 这两张现有资源使用小写文件名，其余物品图标均为 Item_ 前缀。
        if (itemTypeId == 2003 || itemTypeId == 2005)
        {
            return $"item_{itemTypeId}";
        }
        return $"Item_{itemTypeId}";
    }

    /// <summary>清空格子显示并隐藏所有状态图片。</summary>
    public void Clear()
    {
        _count = 0;
        _itemTypeId = 0;
        _itemUid = 0;
        _imgIcon.gameObject.Show(false);
        _texCount.gameObject.Show(false);
        _imgEnter.gameObject.Show(false);
        _imgFx.gameObject.Show(false);
    }
}
