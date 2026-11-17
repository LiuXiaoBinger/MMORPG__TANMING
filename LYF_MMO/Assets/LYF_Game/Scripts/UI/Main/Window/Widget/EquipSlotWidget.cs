using TMPro;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 装备槽控件，只显示控制器解析后的装备展示数据。
/// </summary>
// Unity 指针分发要求实现 IPointerClickHandler，业务数据仍通过具名 UID 事件交给控制器。
public class EquipSlotWidget : MonoBehaviour, IPointerClickHandler
{
    /// <summary>当前穿戴装备图标，空槽时隐藏。</summary>
    [SerializeField, Header("装备图标")] private Image _imgIcon;

    /// <summary>当前装备名称，清空槽位时同时清空文字。</summary>
    [SerializeField, Header("装备名称")] private TMP_Text _texName;

    /// <summary>正数强化等级显示为加号加等级，零级不显示。</summary>
    [SerializeField, Header("强化等级")] private TMP_Text _texStrengthen;

    /// <summary>当前绑定的物品类型编号，用于拦截不属于当前装备的图标结果。</summary>
    private int _itemTypeId;

    /// <summary>每次清空或重绑递增的图标请求编号。</summary>
    private int _loadVersion;

    /// <summary>当前槽位的真实装备 UID，空槽为 0。</summary>
    private long _itemUid;

    /// <summary>玩家点击已穿戴装备时通知所属窗口。</summary>
    public event Action<long> EquipmentSelected;

    /// <summary>点击现有装备格时只派发 UID，不执行穿脱业务。</summary>
    /// <param name="eventData">Unity UI 指针数据，只接受左键点击。</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && _itemUid > 0)
        {
            EquipmentSelected?.Invoke(_itemUid);
        }
    }

    /// <summary>补找旧预制体中尚未绑定的图标和文本引用。</summary>
    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>供装备窗口主动初始化时补齐控件引用。</summary>
    public void Initialize()
    {
        ResolveReferences();
    }

    /// <summary>让旧图标请求失效，清空并隐藏装备内容及整个槽位对象。</summary>
    public void Clear()
    {
        _loadVersion++;
        _itemTypeId = 0;
        _itemUid = 0;
        if (_imgIcon != null)
        {
            _imgIcon.sprite = null;
            _imgIcon.gameObject.SetActive(false);
        }
        if (_texName != null)
        {
            _texName.text = string.Empty;
            _texName.gameObject.SetActive(false);
        }
        if (_texStrengthen != null)
        {
            _texStrengthen.text = string.Empty;
            _texStrengthen.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    /// <summary>先清旧内容，再显示 equipment 装备摘要并异步加载它的图标。</summary>
    public void RefreshUI(EquipmentViewData equipment)
    {
        Clear();
        if (equipment == null || equipment.ItemTypeId <= 0)
        {
            return;
        }
        _itemTypeId = equipment.ItemTypeId;
        _itemUid = equipment.ItemUid;
        // 此次装备图标请求编号，切换或清空后不得再回填。
        int currentLoadVersion = ++_loadVersion;
        gameObject.SetActive(true);
        if (_texName != null)
        {
            _texName.text = equipment.Name;
            _texName.gameObject.SetActive(true);
        }
        if (_texStrengthen != null)
        {
            // 强化为零时保留空文字并隐藏该标记。
            string strengthenText = string.Empty;
            if (equipment.StrengthenLevel > 0)
            {
                strengthenText = "+" + equipment.StrengthenLevel;
            }
            _texStrengthen.text = strengthenText;
            _texStrengthen.gameObject.SetActive(!string.IsNullOrEmpty(strengthenText));
        }
        if (_imgIcon == null || string.IsNullOrEmpty(equipment.IconPath))
        {
            return;
        }
        ResourceMgr.Instance.LoadSpriteAsync(equipment.IconPath, sprite =>
        {
            if (sprite == null || currentLoadVersion != _loadVersion ||
                _itemTypeId != equipment.ItemTypeId)
            {
                return;
            }
            _imgIcon.sprite = sprite;
            _imgIcon.gameObject.SetActive(true);
        });
    }

    /// <summary>优先沿用序列化引用，缺少时按旧预制体节点名寻找控件。</summary>
    private void ResolveReferences()
    {
        if (_imgIcon == null)
        {
            // 旧预制体约定的图标节点。
            Transform iconTransform = transform.Find("img_icon");
            if (iconTransform != null)
            {
                _imgIcon = iconTransform.GetComponent<Image>();
            }
        }
        if (_texName == null)
        {
            _texName = FindText("name");
        }
        if (_texStrengthen == null)
        {
            _texStrengthen = FindText("label");
        }
    }

    /// <summary>按 childName 查找直接子节点的 TMP 文本，找不到节点时返回空。</summary>
    private TMP_Text FindText(string childName)
    {
        // 与指定名字匹配的子节点。
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return null;
        }
        return child.GetComponent<TMP_Text>();
    }
}
