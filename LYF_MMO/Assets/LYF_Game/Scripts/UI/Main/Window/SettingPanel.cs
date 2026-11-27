using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>控制设置窗口的画幅和页签，将声音和世界镜头选项交给对应设置。</summary>
public sealed class SettingPanel : WindowBase
{
    /// <summary>保留八页原始布局的窗体，按主界面可用宽高缩放到全屏。</summary>
    [SerializeField] private RectTransform _frame;

    /// <summary>右侧页签列表，可在基础、镜头、画质、偏好、热键、观战、状态和跳字之间滚动。</summary>
    [SerializeField] private ScrollRect _tabScroll;

    /// <summary>按右侧页签顺序排列的八个页面根节点。</summary>
    [SerializeField] private GameObject[] _pages;

    /// <summary>各页签选中时显示的浅色底板与深色字图标，与页面下标一一对应。</summary>
    [SerializeField] private GameObject[] _tabOn;

    /// <summary>各页签未选中时显示的浅色文字与图标，与页面下标一一对应。</summary>
    [SerializeField] private GameObject[] _tabOff;

    /// <summary>选择自定义画质时显示的细项滚动区域。</summary>
    [SerializeField] private GameObject _customGraphics;

    /// <summary>选择预设画质时提示如何打开细项的说明。</summary>
    [SerializeField] private GameObject _graphicsHint;

    /// <summary>基础页音乐通道的开关。</summary>
    [SerializeField] private Toggle _musicToggle;

    /// <summary>基础页游戏音效通道的开关。</summary>
    [SerializeField] private Toggle _effectsToggle;

    /// <summary>基础页音乐通道的零至一音量滑杆。</summary>
    [SerializeField] private Slider _musicVolume;

    /// <summary>基础页游戏音效通道的零至一音量滑杆。</summary>
    [SerializeField] private Slider _effectsVolume;

    /// <summary>显示音乐当前开启或关闭的状态文字。</summary>
    [SerializeField] private TMP_Text _musicState;

    /// <summary>显示游戏音效当前开启或关闭的状态文字。</summary>
    [SerializeField] private TMP_Text _effectsState;

    /// <summary>按 WorldCameraMode 顺序绑定的五个互斥选项。</summary>
    [SerializeField] private Toggle[] _cameraModes;
    /// <summary>从功能列到基础功能列排列的十二个选项，与 WorldCameraFeature 的位序一致。</summary>
    [SerializeField] private Toggle[] _cameraFeatures;
    /// <summary>两列功能共用的交互区域，固定预设会将整个区域淡化并锁定。</summary>
    [SerializeField] private CanvasGroup _cameraOptions;
    /// <summary>贴在选中模式下方的说明，随后各行按说明实际高度下移。</summary>
    [SerializeField] private TMP_Text _cameraHint;
    /// <summary>镜头模式的操作说明，顺序与左侧五个模式一致。</summary>
    private static readonly string[] CameraHints =
    {
        "战斗中自动拉远镜头，右键拖动可自由环绕。",
        "锁定当前俯角。可先在 3D 模式调好角度，再切回此模式。",
        "普通战斗保持当前视野，首领战自动拉高镜头。",
        "锁定默认俯角，仍可水平环绕和滚轮缩放。",
        "可逐项调整镜头功能，切换模式后仍保留此组合。"
    };

    /// <summary>每个窗口实例仅注册一次声音、镜头模式与自定义勾选回调。</summary>
    private void Awake()
    {
        _musicToggle.onValueChanged.AddListener(ChangeMusicEnabled);
        _effectsToggle.onValueChanged.AddListener(ChangeEffectsEnabled);
        _musicVolume.onValueChanged.AddListener(ChangeBGMVolume);
        _effectsVolume.onValueChanged.AddListener(ChangeSEVolume);

        // modeIndex 对应预制体的一行；分别捕获模式值，避免回调共用循环末尾的下标。
        for (int modeIndex = 0; modeIndex < _cameraModes.Length; modeIndex++)
        {
            // 本行代表的唯一镜头模式，互斥组取消旧行时不切换设置。
            WorldCameraMode mode = (WorldCameraMode)modeIndex;
            _cameraModes[modeIndex].onValueChanged.AddListener(selected =>
            {
                if (!selected) return;
                WorldCameraSettings.SelectMode(mode);
                RefreshCameraControls();
            });
        }
        // featureIndex 从复位键到轻功地面提示依次映射为单个开关位。
        for (int featureIndex = 0; featureIndex < _cameraFeatures.Length; featureIndex++)
        {
            // 此控件独立持有的功能位，勾选时仅更新这一项。
            WorldCameraFeature feature = (WorldCameraFeature)(1 << featureIndex);
            _cameraFeatures[featureIndex].onValueChanged.AddListener(enabled => WorldCameraSettings.SetCustomFeature(feature, enabled));
        }
    }

    /// <summary>打开时置于聊天等主界面窗口上方，回到基础页和页签列表顶部。</summary>
    private void OnEnable()
    {
        GameHotkeys.SettingsVisible = true;
        transform.SetAsLastSibling();
        OnRectTransformDimensionsChange();
        ScrollTabs(1);
        SelectOneHandler(0);
        RefreshAudioControls();
    }

    /// <summary>关闭窗口时统一将已改动的音量与镜头设置写入本机存档。</summary>
    private void OnDisable()
    {
        GameHotkeys.SettingsVisible = false;
        PlayerPrefs.Save();
    }

    /// <summary>打开窗口时无通知地把本机保存的声音开关和音量填回控件。</summary>
    private void RefreshAudioControls()
    {
        _musicToggle.SetIsOnWithoutNotify(SoundVolumeData.SoundBgmVolumeState);
        _effectsToggle.SetIsOnWithoutNotify(SoundVolumeData.SoundSeVolumeState);
        _musicVolume.SetValueWithoutNotify(SoundVolumeData.SoundBgmVolume);
        _effectsVolume.SetValueWithoutNotify(SoundVolumeData.SoundSeVolume);
        SetVoiceState(_musicVolume, _musicState, _musicToggle.isOn);
        SetVoiceState(_effectsVolume, _effectsState, _effectsToggle.isOn);
    }

    /// <summary>应用音乐开关并刷新这一行的状态文字。</summary>
    /// <param name="enabled">音乐选项的当前开启状态。</param>
    private void ChangeMusicEnabled(bool enabled)
    {
        SoundVolumeData.SoundBgmVolumeState = enabled;
        SetVoiceState(_musicVolume, _musicState, enabled);
    }

    /// <summary>应用游戏音效开关并刷新这一行的状态文字。</summary>
    /// <param name="enabled">音效选项的当前开启状态。</param>
    private void ChangeEffectsEnabled(bool enabled)
    {
        SoundVolumeData.SoundSeVolumeState = enabled;
        SetVoiceState(_effectsVolume, _effectsState, enabled);
    }

    /// <summary>将音乐滑杆数值写入本机声音设置。</summary>
    /// <param name="volume">音乐滑杆提供的零至一音量。</param>
    private static void ChangeBGMVolume(float volume)
    {
        SoundVolumeData.SoundBgmVolume = volume;
    }

    /// <summary>将游戏音效滑杆数值写入本机声音设置。</summary>
    /// <param name="volume">游戏音效滑杆提供的零至一音量。</param>
    private static void ChangeSEVolume(float volume)
    {
        SoundVolumeData.SoundSeVolume = volume;
    }

    /// <summary>与 RO 的开关交互一致，关闭时锁住滑杆并保留其数值。</summary>
    /// <param name="slider">当前声音通道的音量滑杆。</param>
    /// <param name="state">当前声音选项的状态文字控件。</param>
    /// <param name="enabled">为真显示开启，为假显示关闭。</param>
    private static void SetVoiceState(Slider slider, TMP_Text state, bool enabled)
    {
        slider.interactable = enabled;
        if (enabled)
        {
            state.text = "开启";
        }
        else
        {
            state.text = "关闭";
        }
    }

    /// <summary>窗口可用面积变化后，让原始窗体的四边与主界面可用区域重合。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (_frame == null)
        {
            return;
        }

        // 根节点随主界面拉伸，宽高以 Canvas 单位表示。
        Rect available = ((RectTransform)transform).rect;
        // 窗体原始宽高以 Canvas 单位表示，内部控件保留预制体中的布局。
        Vector2 frameSize = _frame.rect.size;
        _frame.localScale = new Vector3(available.width / frameSize.x, available.height / frameSize.y, 1f);
        _frame.anchoredPosition = Vector2.zero;
    }

    /// <summary>切换内容与对应页签外观，同时仅显示一个主页面。</summary>
    /// <param name="index">预制体传入的页签下标，从 0 基础到 7 跳字。</param>
    public void SelectOneHandler(int index)
    {
        // 使用同一个下标依次更新每一页内容及其亮、暗两种页签外观。
        for (int pageIndex = 0; pageIndex < _pages.Length; pageIndex++)
        {
            _pages[pageIndex].SetActive(pageIndex == index);
            _tabOn[pageIndex].SetActive(pageIndex == index);
            _tabOff[pageIndex].SetActive(pageIndex != index);
        }
        if (index == 1) RefreshCameraControls();
    }

    /// <summary>回显当前模式和全部有效勾选，在选中行下展开说明，仅开放自定义项编辑。</summary>
    private void RefreshCameraControls()
    {
        // 模式枚举和预制体行顺序一致，选项布局以画布单位计。
        int selected = (int)WorldCameraSettings.Mode;
        _cameraHint.text = CameraHints[selected];
        // 按当前字体和说明宽度换行，给后续行预留实际需要的高度。
        float hintHeight = _cameraHint.GetPreferredValues(_cameraHint.text, _cameraHint.rectTransform.rect.width, Mathf.Infinity).y;
        _cameraHint.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hintHeight);
        // 五行共用原始横向对齐，选中行之后才为说明让位。
        for (int row = 0; row < _cameraModes.Length; row++)
        {
            _cameraModes[row].SetIsOnWithoutNotify(row == selected);
            // 行首距页面顶边的画布坐标，说明展开后下方各行同步平移。
            float y = -93f - row * 96f;
            if (row > selected) y -= hintHeight;
            ((RectTransform)_cameraModes[row].transform).anchoredPosition = new Vector2(7f, y);
            if (row == selected) _cameraHint.rectTransform.anchoredPosition = new Vector2(66f, y - 48f);
        }
        // 固定预设可查看各项的开关状态，自定义才能修改。
        bool editable = WorldCameraSettings.Mode == WorldCameraMode.Custom;
        _cameraOptions.interactable = editable;
        _cameraOptions.alpha = 0.48f;
        if (editable) _cameraOptions.alpha = 1f;
        // featureIndex 的位对应当前行，不通知回调以保留自定义存档。
        for (int featureIndex = 0; featureIndex < _cameraFeatures.Length; featureIndex++)
        {
            _cameraFeatures[featureIndex].SetIsOnWithoutNotify(WorldCameraSettings.Allows((WorldCameraFeature)(1 << featureIndex)));
        }
    }

    /// <summary>停止当前页签滑动，并把列表定位到指定端。</summary>
    /// <param name="position">0 为底部，1 为顶部，由右侧箭头传入。</param>
    public void ScrollTabs(int position)
    {
        _tabScroll.StopMovement();
        _tabScroll.verticalNormalizedPosition = position;
    }

    /// <summary>切换画质页的细项面板和预设提示，仅更新窗口显示。</summary>
    /// <param name="isCustom">自定义档位选中时为 true，其他档位选中时为 false。</param>
    public void ShowCustomGraphics(bool isCustom)
    {
        _customGraphics.SetActive(isCustom);
        _graphicsHint.SetActive(!isCustom);
    }
}
