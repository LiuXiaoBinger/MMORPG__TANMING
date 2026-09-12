using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能列表条目，只显示技能展示数据。
/// </summary>
public class SkillItemWidget : MonoBehaviour
{
    [SerializeField, Header("技能图标")] private Image _imgIcon;
    [SerializeField, Header("技能Mask")] private Image _imgMask;
    [SerializeField, Header("技能名称")] private TMP_Text _texSkillName;
    [SerializeField, Header("技能等级")] private TMP_Text _texSkillLevel;
    [SerializeField, Header("技能简介")] private TMP_Text _texSkillDesc;

    private int _bindingVersion;

    public void RefreshUI(SkillViewData skill)
    {
        int bindingVersion = ++_bindingVersion;
        if (skill == null)
        {
            return;
        }
        if (_imgMask != null)
        {
            _imgMask.gameObject.Show(skill.Level <= 0);
        }
        if (_texSkillLevel != null)
        {
            _texSkillLevel.SetText("技能等级:" + skill.Level);
        }
        if (_texSkillName != null)
        {
            _texSkillName.text = skill.Name;
        }
        if (_texSkillDesc != null)
        {
            _texSkillDesc.text = skill.Description;
        }
        if (string.IsNullOrEmpty(skill.IconPath))
        {
            return;
        }
        ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
        {
            if (_imgIcon != null && sprite != null && bindingVersion == _bindingVersion)
            {
                _imgIcon.sprite = sprite;
            }
        });
    }
}
