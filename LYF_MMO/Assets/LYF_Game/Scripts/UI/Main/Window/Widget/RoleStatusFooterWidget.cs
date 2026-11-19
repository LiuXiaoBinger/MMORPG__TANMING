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

    /// <summary>位于延迟左侧的网络状态图标，颜色跟随网关延迟档位。</summary>
    [SerializeField] private Image _latencyIcon;

    /// <summary>底栏采用的月份、日和 24 小时时分格式。</summary>
    private const string ClockFormat = "M'月'd HH:mm";

    /// <summary>未收到网关探测结果时的延迟占位文本。</summary>
    private const string UnavailableLatencyText = "-- ms";

    /// <summary>沿用 RO 的快速档上限，单位为毫秒。</summary>
    private const double FastLatencyLimitMilliseconds = 100d;

    /// <summary>沿用 RO 的正常档上限，单位为毫秒。</summary>
    private const double NormalLatencyLimitMilliseconds = 250d;

    /// <summary>沿用 RO 的慢速档上限，单位为毫秒，超过该值为很慢。</summary>
    private const double SlowLatencyLimitMilliseconds = 500d;

    /// <summary>延迟快速档的文字和网络图标绿色。</summary>
    private static readonly Color FastLatencyColor = new Color(0.48f, 0.91f, 0.76f);

    /// <summary>延迟正常档的文字和网络图标黄色。</summary>
    private static readonly Color NormalLatencyColor = new Color(0.96f, 0.83f, 0.37f);

    /// <summary>延迟慢速档的文字和网络图标橙色。</summary>
    private static readonly Color SlowLatencyColor = new Color(0.98f, 0.58f, 0.28f);

    /// <summary>延迟很慢档的文字和网络图标红色，沿用 RO 的红色状态颜色。</summary>
    private static readonly Color VerySlowLatencyColor = new Color(1f, 0.3125f, 0.3125f);

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

    /// <summary>每秒用本机时间和网络模块最近一次网关 RTT 刷新底栏。</summary>
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

        // 当前网关最近一次往返毫秒数，保留小数用于判断亚毫秒显示和 RO 档位。
        double latencyMilliseconds = MNetClient.Instance.GatewayLatencyMilliseconds;
        if (latencyMilliseconds < 0d)
        {
            _latencyText.text = UnavailableLatencyText;
            _latencyText.color = UnavailableLatencyColor;
            _latencyIcon.color = UnavailableLatencyColor;
            return;
        }

        if (latencyMilliseconds < 1d)
        {
            _latencyText.text = "<1 ms";
        }
        else
        {
            // 向上取整使紧邻档位上限的整数毫秒文本与对应颜色一致。
            _latencyText.SetText("{0} ms", (float)Math.Ceiling(latencyMilliseconds));
        }

        // 当前有效 RTT 的档位颜色，超过慢速档上限时使用红色。
        Color latencyColor = VerySlowLatencyColor;
        if (latencyMilliseconds <= FastLatencyLimitMilliseconds)
        {
            latencyColor = FastLatencyColor;
        }
        else if (latencyMilliseconds <= NormalLatencyLimitMilliseconds)
        {
            latencyColor = NormalLatencyColor;
        }
        else if (latencyMilliseconds <= SlowLatencyLimitMilliseconds)
        {
            latencyColor = SlowLatencyColor;
        }
        _latencyText.color = latencyColor;
        _latencyIcon.color = latencyColor;
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
