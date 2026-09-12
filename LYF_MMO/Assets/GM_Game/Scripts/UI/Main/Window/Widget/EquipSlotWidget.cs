using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 装备槽控件，只显示控制器解析后的装备展示数据。
/// </summary>
public class EquipSlotWidget : MonoBehaviour
{
    [SerializeField, Header("装备图标")] private Image _imgIcon;
    [SerializeField, Header("装备名称")] private TMP_Text _texName;
    [SerializeField, Header("强化等级")] private TMP_Text _texStrengthen;

    private int _itemTypeId;
    private int _loadVersion;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Initialize()
    {
        ResolveReferences();
    }

    public void Clear()
    {
        _loadVersion++;
        _itemTypeId = 0;
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

    public void RefreshUI(EquipmentViewData equipment)
    {
        Clear();
        if (equipment == null || equipment.ItemTypeId <= 0)
        {
            return;
        }
        _itemTypeId = equipment.ItemTypeId;
        int currentLoadVersion = ++_loadVersion;
        gameObject.SetActive(true);
        if (_texName != null)
        {
            _texName.text = equipment.Name;
            _texName.gameObject.SetActive(true);
        }
        if (_texStrengthen != null)
        {
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

    private void ResolveReferences()
    {
        if (_imgIcon == null)
        {
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

    private TMP_Text FindText(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return null;
        }
        return child.GetComponent<TMP_Text>();
    }
}
