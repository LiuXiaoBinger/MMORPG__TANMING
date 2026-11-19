using UnityEngine;
using UnityEngine.Audio;

/// <summary>管理背景音乐和游戏音效混音组的运行时音量。</summary>
public static class MAudioMgr
{
    /// <summary>在 UI 根启动时从 Resources 加载的游戏混音器。</summary>
    private static AudioMixer _mixer;

    /// <summary>技能、脚步、战斗及界面点击声共用的 SFX 混音组。</summary>
    public static AudioMixerGroup SEOutput { get; private set; }

    /// <summary>在 UI 根的 Start 中接好混音器并恢复本机的声音设置。</summary>
    public static void Init()
    {
        _mixer = Resources.Load<AudioMixer>("Audio/GameAudio");
        SEOutput = _mixer.FindMatchingGroups("SFX")[0];
        SoundVolumeData.LoadVolumeData();
    }

    /// <summary>设置预留背景音乐混音组的有效音量。</summary>
    /// <param name="volume">零至一的线性音量，零表示静音。</param>
    public static void SetBGMVolume(float volume)
    {
        SetVolume("BGMVolume", volume);
    }

    /// <summary>设置游戏音效混音组的有效音量，对正在播放的声音立即生效。</summary>
    /// <param name="volume">音效滑杆的零至一档位，零表示静音。</param>
    public static void SetSEVolume(float volume)
    {
        // 用平方根提高低档位的增益，10% 从 -20 分贝提高到 -10 分贝，满档仍为 0 分贝。
        SetVolume("SEVolume", Mathf.Sqrt(Mathf.Max(0f, volume)));
    }

    /// <summary>将线性音量换成 Unity 混音器使用的分贝值。</summary>
    /// <param name="parameter">混音器公开的分贝参数名。</param>
    /// <param name="volume">零至一的通道有效音量。</param>
    private static void SetVolume(string parameter, float volume)
    {
        if (_mixer == null)
        {
            return;
        }

        // Unity 混音器以 -80 分贝作为静音档，限制取对数前的最低值。
        float decibels = 20f * Mathf.Log10(Mathf.Max(volume, 0.0001f));
        _mixer.SetFloat(parameter, decibels);
    }
}
