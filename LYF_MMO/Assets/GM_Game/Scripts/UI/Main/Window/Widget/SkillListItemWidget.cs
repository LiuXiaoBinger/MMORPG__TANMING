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

    /// <summary>条目底板，用于区分选中状态。</summary>
    [SerializeField, Header("底板")] private Image _background;

    /// <summary>条目所属技能编号，异步图标回调用它拒绝过期结果。</summary>
    private int _skillId;

    /// <summary>把一条技能数据写进条目，只有技能变化时才请求图标。</summary>
    /// <param name="skill">控制器生成的技能展示数据。</param>
    /// <param name="level">当前展示等级，包含窗口原有的本地升级结果。</param>
    public void SetData(SkillViewData skill, int level)
    {
        _name.text = skill.Name;
        SetLevel(level, skill.Tag);
        // 只在对象池条目改绑技能时加载，反复切页不会重复请求已显示的图片。
        if (_skillId == skill.SkillId)
        {
            return;
        }
        _skillId = skill.SkillId;
        _icon.enabled = false;
        if (string.IsNullOrEmpty(skill.IconPath))
        {
            return;
        }
        // 这一请求的技能编号，用于条目切换数据后丢弃旧回调。
        int skillId = skill.SkillId;
        ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
        {
            if (_icon == null || sprite == null || _skillId != skillId)
            {
                return;
            }
            _icon.sprite = sprite;
            _icon.enabled = true;
        });
    }

    /// <summary>显示等级和技能定位，升级后可单独刷新文字，不重新加载图标。</summary>
    /// <param name="level">当前展示等级，0 表示尚未习得。</param>
    /// <param name="tag">用于区分突进、群攻、绝学等定位的简短标签。</param>
    public void SetLevel(int level, string tag)
    {
        if (level > 0)
        {
            _meta.text = level + "级 · " + tag;
        }
        else
        {
            _meta.text = "未习得 · " + tag;
        }
    }

    /// <summary>切换条目选中底色，选中项提亮以便和其余条目区分。</summary>
    /// <param name="selected">true 表示当前正在查看该技能。</param>
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            _background.color = new Color(0.29f, 0.44f, 0.43f, 0.99f);
            _name.color = new Color(1f, 0.99f, 0.91f, 1f);
            _meta.color = new Color(0.91f, 0.97f, 0.91f, 1f);
        }
        else
        {
            _background.color = new Color(0.08f, 0.15f, 0.18f, 0.94f);
            _name.color = new Color(0.89f, 0.94f, 0.94f, 1f);
            _meta.color = new Color(0.62f, 0.76f, 0.76f, 1f);
        }
    }
}
