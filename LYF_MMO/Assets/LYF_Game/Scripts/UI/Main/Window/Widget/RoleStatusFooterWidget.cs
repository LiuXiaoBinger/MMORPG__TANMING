using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗快捷栏下方的角色状态底栏控件，展示职业、等级、经验、本机时钟和网关延迟，并提供竞技设置入口。
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

    /// <summary>底栏第二行的本机月份、日期与时分文本。</summary>
    [SerializeField] private TMP_Text _clockText;

    /// <summary>底栏第一行的网关延迟文本，探测不可用时显示占位。</summary>
    [SerializeField] private TMP_Text _latencyText;

    /// <summary>位于延迟左侧的网络状态图标，颜色跟随延迟可用性。</summary>
    [SerializeField] private Image _latencyIcon;

    /// <summary>底栏采用的月份、日和 24 小时时分格式。</summary>
    private const string ClockFormat = "M'月'd HH:mm";

    /// <summary>未收到网关探测结果时的延迟占位文本。</summary>
    private const string UnavailableLatencyText = "-- ms";

    /// <summary>真实延迟可用时文字和网络图标的青绿色。</summary>
    private static readonly Color AvailableLatencyColor = new Color(0.48f, 0.91f, 0.76f);

    /// <summary>真实延迟不可用时文字和网络图标的灰色。</summary>
    private static readonly Color UnavailableLatencyColor = new Color(0.64f, 0.71f, 0.72f);

    /// <summary>下次允许刷新底栏文字的非缩放时间，单位为秒。</summary>
    private float _nextStatusRefreshAt;

    /// <summary>供主控制器注册竞技设置入口点击事件。</summary>
    public Button SafetyButton => _safetyButton;

    /// <summary>未取得角色等级前的破折号占位文本。</summary>
    private const string PlaceholderLevel = "-";

    /// <summary>剑形标记对应的职业编号，1 为魔剑士。</summary>
    private const int SwordProfessionId = 1;

    /// <summary>底栏重新显示时，使第一帧立即刷新本机时间和网关状态。</summary>
    private void OnEnable()
    {
        _nextStatusRefreshAt = 0f;
    }

    /// <summary>底栏隐藏或销毁时清理网关探测结果。</summary>
    private void OnDisable()
    {
        if (_latencyText != null)
        {
            MNetClient.Instance.ReleaseGatewayLatency();
        }
    }

    /// <summary>每秒用本机时间和当前连接的网关探测结果刷新底栏。</summary>
    private void Update()
    {
        // 使用非缩放秒数，暂停或调整战斗时间倍率时也能更新。
        float now = Time.unscaledTime;
        if (now < _nextStatusRefreshAt)
        {
            return;
        }
        _nextStatusRefreshAt = now + 1f;

        if (_clockText != null)
        {
            _clockText.text = DateTime.Now.ToString(ClockFormat, CultureInfo.InvariantCulture);
        }
        if (_latencyText == null || _latencyIcon == null)
        {
            return;
        }

        // 负值表示未获得有效回包，0 毫秒保留显示。
        int latencyMilliseconds = MNetClient.Instance.PollGatewayLatency(now);
        if (latencyMilliseconds < 0)
        {
            _latencyText.text = UnavailableLatencyText;
            _latencyText.color = UnavailableLatencyColor;
            _latencyIcon.color = UnavailableLatencyColor;
        }
        else
        {
            _latencyText.SetText("{0} ms", latencyMilliseconds);
            _latencyText.color = AvailableLatencyColor;
            _latencyIcon.color = AvailableLatencyColor;
        }
    }

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
