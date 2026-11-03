using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能列表条目，只显示技能展示数据。
/// </summary>
public class SkillItemWidget : MonoBehaviour
{
    /// <summary>异步加载的当前技能图标。</summary>
    [SerializeField, Header("技能图标")] private Image _imgIcon;
    /// <summary>技能等级不大于零时显示的未学习遮罩。</summary>
    [SerializeField, Header("技能Mask")] private Image _imgMask;
    /// <summary>技能展示名称。</summary>
    [SerializeField, Header("技能名称")] private TMP_Text _texSkillName;
    /// <summary>当前已学等级文本。</summary>
    [SerializeField, Header("技能等级")] private TMP_Text _texSkillLevel;
    /// <summary>配置提供的技能说明。</summary>
    [SerializeField, Header("技能简介")] private TMP_Text _texSkillDesc;

    /// <summary>技能重绑编号，避免旧技能图标覆盖新条目内容。</summary>
    private int _bindingVersion;

    /// <summary>按 skill 更新名称、等级、说明及未学习遮罩，再异步加载图标。</summary>
    public void RefreshUI(SkillViewData skill)
    {
        // 本次绑定编号；即使传入空数据，也使上一轮图标回调失效。
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
