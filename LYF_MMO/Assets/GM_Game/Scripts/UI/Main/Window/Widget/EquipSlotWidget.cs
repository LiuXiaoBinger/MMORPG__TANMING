using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class EquipSlotWidget : MonoBehaviour
{
    // 这些引用既可以在 Inspector 中绑定，也可以由 ResolveReferences 按预制体节点名自动查找。
    [SerializeField, Header("装备图标")] private Image _imgIcon;
    [SerializeField, Header("装备名称")] private TMP_Text _texName;
    [SerializeField, Header("强化等级")] private TMP_Text _texStrengthen;

    private int _itemTypeId;
    // 每次清空或刷新都会递增，防止旧的异步图标回调覆盖新装备。
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
        // 清理显示内容，同时让父部位恢复为空槽状态。
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

    public void RefreshUI(RoleItemInfo itemInfo)
    {
        Clear();
        if (itemInfo == null || itemInfo.ItemTypeId <= 0)
        {
            return;
        }

        EquipInfo equipInfo = LubanMgr.Instance.GetEquipInfoById(itemInfo.ItemTypeId);
        _itemTypeId = itemInfo.ItemTypeId;
        // 记录本次请求版本，换装或清空后旧请求将自动失效。
        int currentLoadVersion = ++_loadVersion;
        gameObject.SetActive(true);

        if (_texName != null)
        {
            _texName.text = equipInfo == null ? itemInfo.ItemTypeId.ToString() : equipInfo.EquipName;
            _texName.gameObject.SetActive(true);
        }

        if (_texStrengthen != null)
        {
            _texStrengthen.text = itemInfo.EquipInfo == null || itemInfo.EquipInfo.StrengthenLevel <= 0
                ? string.Empty
                : $"+{itemInfo.EquipInfo.StrengthenLevel}";
            _texStrengthen.gameObject.SetActive(!string.IsNullOrEmpty(_texStrengthen.text));
        }

        if (_imgIcon == null)
        {
            return;
        }

        string iconPath = equipInfo == null ? $"Icon/Item/Item_{itemInfo.ItemTypeId}" : equipInfo.Icon;
        if (string.IsNullOrEmpty(iconPath))
        {
            return;
        }

        ResourceMgr.Instance.LoadSpriteAsync(iconPath, sprite =>
        {
            // 只有当前装备仍然匹配时，才允许异步结果更新图片。
            if (sprite == null || currentLoadVersion != _loadVersion || _itemTypeId != itemInfo.ItemTypeId)
            {
                return;
            }

            _imgIcon.sprite = sprite;
            _imgIcon.gameObject.SetActive(true);
        });
    }

    private void ResolveReferences()
    {
        // pan_equip 中的每个部位都复用了相同的子节点命名约定。
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
        return child == null ? null : child.GetComponent<TMP_Text>();
    }
}
