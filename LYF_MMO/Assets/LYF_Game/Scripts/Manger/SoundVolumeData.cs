using UnityEngine;

/// <summary>保存本机的音乐与游戏音效设置，并把有效音量交给 MAudioMgr。</summary>
public static class SoundVolumeData
{
    /// <summary>音乐开关的本机存档键，1 表示开启。</summary>
    private const string SoundBgmStateKey = "LYF.Audio.BgmEnabled";

    /// <summary>音乐滑杆的本机存档键，保存零至一的线性音量。</summary>
    private const string SoundBgmVolumeKey = "LYF.Audio.BgmVolume";

    /// <summary>游戏音效开关的本机存档键，1 表示开启。</summary>
    private const string SoundSeStateKey = "LYF.Audio.SeEnabled";

    /// <summary>游戏音效滑杆的本机存档键，保存零至一的线性音量。</summary>
    private const string SoundSeVolumeKey = "LYF.Audio.SeVolume";

    /// <summary>音乐的开关，首次运行默认开启，关闭时保留滑杆数值。</summary>
    public static bool SoundBgmVolumeState
    {
        get => PlayerPrefs.GetInt(SoundBgmStateKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(SoundBgmStateKey, System.Convert.ToInt32(value));
            UpdateBGMVolume();
        }
    }

    /// <summary>游戏音效的开关，首次运行默认开启，关闭时保留滑杆数值。</summary>
    public static bool SoundSeVolumeState
    {
        get => PlayerPrefs.GetInt(SoundSeStateKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(SoundSeStateKey, System.Convert.ToInt32(value));
            UpdateSEVolume();
        }
    }

    /// <summary>音乐滑杆保存的音量，默认沿用设置页的 38%。</summary>
    public static float SoundBgmVolume
    {
        get => PlayerPrefs.GetFloat(SoundBgmVolumeKey, 0.38f);
        set
        {
            PlayerPrefs.SetFloat(SoundBgmVolumeKey, value);
            UpdateBGMVolume();
        }
    }

    /// <summary>游戏音效滑杆保存的音量，默认沿用设置页的 60%。</summary>
    public static float SoundSeVolume
    {
        get => PlayerPrefs.GetFloat(SoundSeVolumeKey, 0.6f);
        set
        {
            PlayerPrefs.SetFloat(SoundSeVolumeKey, value);
            UpdateSEVolume();
        }
    }

    /// <summary>混音器就绪后应用本机保存的两个通道音量。</summary>
    public static void LoadVolumeData()
    {
        UpdateBGMVolume();
        UpdateSEVolume();
    }

    /// <summary>根据音乐开关，把保存的音量或静音值交给背景音乐混音组。</summary>
    private static void UpdateBGMVolume()
    {
        if (!SoundBgmVolumeState)
        {
            MAudioMgr.SetBGMVolume(0f);
            return;
        }
        MAudioMgr.SetBGMVolume(SoundBgmVolume);
    }

    /// <summary>根据音效开关，把保存的音量或静音值交给游戏音效混音组。</summary>
    private static void UpdateSEVolume()
    {
        if (!SoundSeVolumeState)
        {
            MAudioMgr.SetSEVolume(0f);
            return;
        }
        MAudioMgr.SetSEVolume(SoundSeVolume);
    }
}
