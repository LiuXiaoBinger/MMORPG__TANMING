using System;
using UnityEngine;

/// <summary>一次编辑会话中的真实音频包络；按采样帧保留所有声道峰值，绝不改写音频资源或技能事件时长。</summary>
public sealed class SkillEditorAudioWaveform
{
    /// <summary>包络最多保存八千一百九十二个时间桶，限制长声音的编辑缓存内存。</summary>
    private const int MaxBins = 8192;
    /// <summary>每次读取四千零九十六个采样帧，避免为长声音一次分配完整 PCM。</summary>
    private const int ReadFrames = 4096;
    /// <summary>音频各时间桶的最小有符号振幅，所有声道合并后仍保留反相峰值。</summary>
    private float[] _minimum;
    /// <summary>音频各时间桶的最大有符号振幅，缩小显示时不会平均掉瞬态峰值。</summary>
    private float[] _maximum;
    /// <summary>源音频采样帧数，每帧包含全部声道，空剪辑为零。</summary>
    public int Samples { get; private set; }
    /// <summary>源音频每秒采样帧数；用于把峰值样本换算成精确秒数。</summary>
    public int Frequency { get; private set; }
    /// <summary>源音频声道数量，双声道按相同采样帧时间合并显示。</summary>
    public int Channels { get; private set; }
    /// <summary>按源音频样本数和采样率计算的真实完整时长，单位秒。</summary>
    public float DurationSeconds { get; private set; }
    /// <summary>全部声道绝对振幅最大值首次出现的采样帧号，零起始。</summary>
    public int PeakSample { get; private set; }
    /// <summary>最大瞬态的有符号样本振幅，零表示静音。</summary>
    public float PeakAmplitude { get; private set; }
    /// <summary>读取失败的本地化原因；非空时不绘制伪波形、不自动修改导入方式。</summary>
    public string Error { get; private set; }
    /// <summary>读取时音频的加载状态；异步加载完成后窗口可丢弃尚未就绪的失败缓存。</summary>
    public AudioDataLoadState LoadState { get; private set; }
    /// <summary>真实 PCM 读取成功后才允许显示和以峰值进行对齐。</summary>
    public bool IsReadable { get { return _minimum != null; } }
    /// <summary>源音频主峰相对音频开始的精确秒数，未读取时返回零。</summary>
    public float PeakSeconds
    {
        get
        {
            if (Frequency <= 0) return 0f;
            return (float)PeakSample / Frequency;
        }
    }

    /// <summary>只读音频 PCM 并建立会话缓存；失败仍保留真实长度供轨道显示。</summary>
    /// <param name="clip">当前作者事件引用的真实声音，不创建替代声音或重新导入。</param>
    /// <returns>带完整长度、采样峰值或明确失败原因的强类型只读包络。</returns>
    public static SkillEditorAudioWaveform Read(AudioClip clip)
    {
        // 本次读取结果仅由打开的编辑窗口持有，不序列化进作者资源。
        SkillEditorAudioWaveform result = new SkillEditorAudioWaveform();
        if (clip == null)
        {
            result.Error = SkillEditorLocalization.Text("Assign an AudioClip to view its waveform.", "请指定音频后查看波形。");
            return result;
        }
        result.Samples = clip.samples;
        result.Frequency = clip.frequency;
        result.Channels = clip.channels;
        result.DurationSeconds = clip.length;
        result.LoadState = clip.loadState;
        if (result.Samples <= 0 || result.Frequency <= 0 || result.Channels <= 0)
        {
            result.Error = SkillEditorLocalization.Text("The clip has no readable samples.", "音频没有可读取的样本。");
            return result;
        }
        result.DurationSeconds = (float)result.Samples / result.Frequency;
        if (clip.loadType == AudioClipLoadType.Streaming)
        {
            result.Error = SkillEditorLocalization.Text("Streaming clips do not expose PCM; waveform unavailable. Import settings are unchanged.", "流式音频无法读取 PCM，不能显示波形；未修改导入设置。");
            return result;
        }
        if (result.LoadState != AudioDataLoadState.Loaded)
        {
            result.Error = SkillEditorLocalization.Text("Audio data is not loaded. The waveform will retry when loading completes.", "音频样本尚未加载；加载完成后会自动刷新波形。");
            return result;
        }
        // 包络桶数随真实样本数变化；短音频每采样帧一桶，长音频保留时间桶的极值。
        int binCount = Mathf.Min(MaxBins, result.Samples);
        // 临时桶数组只有读取完整成功后才发布，失败不会留下局部伪波形。
        float[] minimum = new float[binCount];
        // 临时最大振幅数组，与最小振幅使用完全相同的时间划分。
        float[] maximum = new float[binCount];
        // 零基桶下标，每桶先用极限初始化，避免把全正或全负波形强制截到零。
        for (int bin = 0; bin < binCount; bin++)
        {
            minimum[bin] = float.PositiveInfinity;
            maximum[bin] = float.NegativeInfinity;
        }
        // 固定块的采样帧数量；最后一块只处理仍属于源声音的帧数。
        int blockFrames = Mathf.Min(ReadFrames, result.Samples);
        // GetData 的多声道交错 PCM 缓冲，生命周期只覆盖本次读取。
        float[] data = new float[blockFrames * result.Channels];
        try
        {
            // 本次块起点以采样帧计数，而非声道交错数组下标。
            for (int offset = 0; offset < result.Samples; offset += blockFrames)
            {
                if (!clip.GetData(data, offset))
                {
                    result.Error = SkillEditorLocalization.Text("PCM could not be read. Retry after loading; import settings are unchanged.", "PCM 读取失败。音频加载后可刷新重试；未修改导入设置。");
                    return result;
                }
                // 本块有效采样帧数，GetData 尾部可能回绕的数据不会计入包络。
                int validFrames = Mathf.Min(blockFrames, result.Samples - offset);
                // 当前块内零基采样帧号，所有声道共享其时间位置。
                for (int frame = 0; frame < validFrames; frame++)
                {
                    // 当前有效帧在原始完整 PCM 中的绝对采样帧号。
                    int sample = offset + frame;
                    // 用整数比例分桶，避免长音频和小桶数发生浮点边界漂移。
                    int bin = (int)((long)sample * binCount / result.Samples);
                    // 交错 PCM 内当前声道下标，包含所有声道并保留极值。
                    for (int channel = 0; channel < result.Channels; channel++)
                    {
                        // 当前声道原始有符号振幅，通常处于负一到一。
                        float amplitude = data[frame * result.Channels + channel];
                        if (float.IsNaN(amplitude) || float.IsInfinity(amplitude)) continue;
                        minimum[bin] = Mathf.Min(minimum[bin], amplitude);
                        maximum[bin] = Mathf.Max(maximum[bin], amplitude);
                        if (Mathf.Abs(amplitude) > Mathf.Abs(result.PeakAmplitude))
                        {
                            result.PeakAmplitude = amplitude;
                            result.PeakSample = sample;
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            result.Error = SkillEditorLocalization.Text("PCM read failed: ", "PCM 读取失败：") + exception.Message;
            return result;
        }
        result._minimum = minimum;
        result._maximum = maximum;
        return result;
    }

    /// <summary>读取一列像素覆盖时间内的振幅极值；缩放时保持窄瞬态，多声道反相不会相互抵消。</summary>
    /// <param name="beginSeconds">像素列起点相对音频开始的秒数，超界自动截到完整音频范围。</param>
    /// <param name="endSeconds">像素列结束秒数，不包含下一列；至少覆盖一个实际采样帧。</param>
    /// <param name="minimum">此显示时间范围的最小原始振幅，读取失败时为零。</param>
    /// <param name="maximum">此显示时间范围的最大原始振幅，读取失败时为零。</param>
    public void GetEnvelope(float beginSeconds, float endSeconds, out float minimum, out float maximum)
    {
        minimum = 0f;
        maximum = 0f;
        if (!IsReadable || DurationSeconds <= 0f) return;
        // 第一个被显示列覆盖的时间桶，不允许超出完整包络缓存。
        int begin = Mathf.Clamp(Mathf.FloorToInt(beginSeconds / DurationSeconds * _minimum.Length), 0, _minimum.Length - 1);
        // 最后一个被显示列覆盖的时间桶，结束边界采用不包含语义。
        int end = Mathf.Clamp(Mathf.CeilToInt(endSeconds / DurationSeconds * _minimum.Length) - 1, begin, _minimum.Length - 1);
        minimum = float.PositiveInfinity;
        maximum = float.NegativeInfinity;
        // 当前显示列范围内的包络桶下标，以极值合并而非平均保证峰值仍可见。
        for (int bin = begin; bin <= end; bin++)
        {
            minimum = Mathf.Min(minimum, _minimum[bin]);
            maximum = Mathf.Max(maximum, _maximum[bin]);
        }
        if (float.IsInfinity(minimum)) minimum = 0f;
        if (float.IsInfinity(maximum)) maximum = 0f;
    }
}
