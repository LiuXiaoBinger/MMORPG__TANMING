using System;

/// <summary>从原FMOD事件输出并沿既有SoundComponent播放的声音指令。</summary>
[Serializable]
public sealed class MSceneMonsterAudioData
{
    /// <summary>正式AudioClip地址；原银行没有事件时为空并记录导入缺口。</summary>
    public string Path;
    /// <summary>原AudioList触发秒数。</summary>
    public float At;
    /// <summary>原End与At的时间差；零表示播放到剪辑自然结束。</summary>
    public float Duration;
    /// <summary>是否按原指令循环至当前阶段结束。</summary>
    public bool Loop;
}
