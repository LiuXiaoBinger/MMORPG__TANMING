using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武学列表页的单条技能条目，只负责把一条技能展示数据写到图标与文字上。
/// </summary>
public class SkillListItemWidget : MonoBehaviour
{
    /// <summary>条目左侧的圆形技能图标。</summary>
    [SerializeField, Header("技能图标")] private Image _icon;

    /// <summary>技能名称。</summary>
    [SerializeField, Header("技能名称")] private TMP_Text _name;

    /// <summary>等级与两字定位标签，例如“Lv.1 · 突进”。</summary>
    [SerializeField, Header("等级与标签")] private TMP_Text _meta;

    /// <summary>技能简介文本，超出宽度时截断显示。</summary>
    [SerializeField, Header("技能简介")] private TMP_Text _desc;

    /// <summary>条目底板，用于区分选中状态。</summary>
    [SerializeField, Header("底板")] private Image _background;

    /// <summary>条目所属技能编号，异步图标回调用它拒绝过期结果。</summary>
    private int _skillId;

    /// <summary>把一条技能数据写进条目，并异步加载图标。</summary>
    /// <param name="skill">控制器生成的技能展示数据。</param>
    public void SetData(SkillViewData skill)
    {
        if (skill == null)
        {
            return;
        }
        _skillId = skill.SkillId;
        if (_name != null)
        {
            _name.text = skill.Name;
        }
        if (_meta != null)
        {
            if (skill.Level > 0)
            {
                _meta.text = "Lv." + skill.Level + " · " + skill.Tag;
            }
            else
            {
                _meta.text = "未习得 · " + skill.Tag;
            }
        }
        if (_desc != null)
        {
            _desc.text = skill.Description;
        }
        if (_icon == null || string.IsNullOrEmpty(skill.IconPath))
        {
            return;
        }
        // 图标未回来前先隐藏，避免出现白色方块。
        _icon.enabled = false;
        ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
        {
            if (_icon == null || sprite == null)
            {
                return;
            }
            // 条目可能已被复用到别的技能，编号一致才回填图标。
            if (_skillId == skill.SkillId)
            {
                _icon.sprite = sprite;
                _icon.enabled = true;
            }
        });
    }

    /// <summary>切换条目选中底色，选中项提亮以便和其余条目区分。</summary>
    /// <param name="selected">true 表示当前正在查看该技能。</param>
    public void SetSelected(bool selected)
    {
        if (_background == null)
        {
            return;
        }
        if (selected)
        {
            _background.color = new Color(0.2f, 0.34f, 0.52f, 0.95f);
        }
        else
        {
            _background.color = new Color(0.1f, 0.14f, 0.19f, 0.92f);
        }
    }
}
