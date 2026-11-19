using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗快捷栏下方的角色状态底栏控件，展示职业剑形标记、当前等级与当前等级经验进度，并提供竞技设置入口。
/// </summary>
public sealed class RoleStatusFooterWidget : MonoBehaviour
{
    /// <summary>左下职业剑形标记的父节点，仅当前职业为剑士时显示。</summary>
    [SerializeField] private GameObject _professionGlyph;

    /// <summary>左下职业等级文本，未取得角色等级前显示破折号占位。</summary>
    [SerializeField] private TMP_Text _levelText;

    /// <summary>底边经验进度条，表示当前等级已获得经验在本级所需经验中的占比，范围 0 到 1。</summary>
    [SerializeField] private Slider _experienceSlider;

    /// <summary>底栏左侧的竞技设置入口按钮，由主控制器注册打开窗口的点击事件。</summary>
    [SerializeField] private Button _safetyButton;

    /// <summary>供主控制器注册竞技设置入口点击事件。</summary>
    public Button SafetyButton => _safetyButton;

    /// <summary>未取得角色等级前的破折号占位文本。</summary>
    private const string PlaceholderLevel = "-";

    /// <summary>剑形标记对应的职业编号，1 为魔剑士。</summary>
    private const int SwordProfessionId = 1;

    /// <summary>用当前角色数据刷新底栏；角色等级未就绪时显示占位并保持空进度。</summary>
    /// <param name="level">当前角色等级，非正数表示尚未取得角色数据。</param>
    /// <param name="jobId">当前角色职业编号，1 时显示剑形标记。</param>
    /// <param name="expRatio">本级经验占比，已裁剪到 0 到 1，配置缺失时传 0。</param>
    public void SetRoleProgress(int level, int jobId, float expRatio)
    {
        if (_levelText != null)
        {
            // 等级未就绪时保留破折号，避免把默认 0 误显示成真实等级。
            if (level <= 0)
            {
                _levelText.text = PlaceholderLevel;
            }
            else
            {
                _levelText.text = level.ToString();
            }
        }
        if (_professionGlyph != null)
        {
            // 剑形标记只在剑士职业显示，其他职业隐藏。
            _professionGlyph.SetActive(jobId == SwordProfessionId);
        }
        if (_experienceSlider != null)
        {
            // 经验条不拦截射线，只按传入比例改变填充。
            _experienceSlider.SetValueWithoutNotify(Mathf.Clamp01(expRatio));
        }
    }
}
