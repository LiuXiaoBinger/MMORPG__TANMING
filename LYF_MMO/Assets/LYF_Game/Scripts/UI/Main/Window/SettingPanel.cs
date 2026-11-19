using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>控制设置窗口的画幅、页签展示和基础页的声音设置控件。</summary>
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

    /// <summary>每个设置窗口实例仅注册一次声音控件的值变化回调。</summary>
    private void Awake()
    {
        _musicToggle.onValueChanged.AddListener(ChangeMusicEnabled);
        _effectsToggle.onValueChanged.AddListener(ChangeEffectsEnabled);
        _musicVolume.onValueChanged.AddListener(ChangeBGMVolume);
        _effectsVolume.onValueChanged.AddListener(ChangeSEVolume);
    }

    /// <summary>打开时置于聊天等主界面窗口上方，回到基础页和页签列表顶部。</summary>
    private void OnEnable()
    {
        transform.SetAsLastSibling();
        OnRectTransformDimensionsChange();
        ScrollTabs(1);
        SelectOneHandler(0);
        RefreshAudioControls();
    }

    /// <summary>窗口关闭时保存已改动的声音设置，拖动音量滑杆期间只更新内存数值。</summary>
    private void OnDisable()
    {
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
