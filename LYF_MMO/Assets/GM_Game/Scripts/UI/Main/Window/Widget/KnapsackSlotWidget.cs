using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KnapsackSlotWidget : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IDropHandler
{
    [SerializeField, Header("物品图标")] private Image _imgIcon;
    [SerializeField, Header("物品数量")] private TMP_Text _texCount;
    [SerializeField, Header("鼠标进入效果")] private Image _imgEnter;
    [SerializeField, Header("物品特效")] private Image _imgFx;

    public int _count;
    public string _spriteName;

    private int _itemTypeId;

    private static string GetIconName(int itemTypeId)
    {
        // 这两张现有资源使用小写文件名，其余物品图标均为 Item_ 前缀。
        return itemTypeId == 2003 || itemTypeId == 2005
            ? $"item_{itemTypeId}"
            : $"Item_{itemTypeId}";
    }

    public void Clear()
    {
        _count = 0;
        _itemTypeId = 0;
        _imgIcon.gameObject.Show(false);
        _texCount.gameObject.Show(false);
        _imgEnter.gameObject.Show(false);
        _imgFx.gameObject.Show(false);
    }

    /// <summary>
    /// 使用服务端返回的物品类型 ID 加载对应图标。
    /// </summary>
    public void RefreshUI(RoleItemInfo roleKanpsackSlot)
    {
        Clear();
        if (roleKanpsackSlot == null)
        {
            return;
        }

        _count = roleKanpsackSlot.Count;
        _itemTypeId = roleKanpsackSlot.ItemTypeId;
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

        int requestedItemTypeId = _itemTypeId;
        ResourceMgr.Instance.LoadSpriteAsync($"Icon/Item/{GetIconName(requestedItemTypeId)}", sprite =>
        {
            if (_itemTypeId == requestedItemTypeId && sprite != null)
            {
                _imgIcon.sprite = sprite;
            }
        });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_count > 0)
        {
            _imgEnter.gameObject.Show();
            TipsMgr.Instance.ShowItmeTips(eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _imgEnter.gameObject.Show(false);
        TipsMgr.Instance.CloseItemTips();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        UIDragWidget uiDragWidget = eventData.pointerDrag.GetComponent<UIDragWidget>();
        if (uiDragWidget != null && uiDragWidget._dragType == DragType.KanpsackSlot)
        {
            // TODO: 将背包位置交换请求提交给服务器。
        }
    }
}
