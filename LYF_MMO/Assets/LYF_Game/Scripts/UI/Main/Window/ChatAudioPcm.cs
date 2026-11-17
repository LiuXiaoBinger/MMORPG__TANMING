using UnityEngine;

/// <summary>把设备采样和自托管语音服务约定的 16 位小端单声道 PCM 相互转换。</summary>
public static class ChatAudioPcm
{
    /// <summary>语音网络格式的单声道采样率，单位 Hz。</summary>
    public const int Frequency = 16000;

    /// <summary>按设备实际采样率重采样，多声道取均值。</summary><param name="input">交错排列的设备浮点采样。</param>
    /// <param name="channels">每帧的声道数。</param><param name="rate">设备实际采样率，单位 Hz。</param>
    public static byte[] Encode(float[] input, int channels, int rate)
    {
        // 输出长度由设备实际采样帧数换算，保留秒数。
        int frames = (int)((long)(input.Length / channels) * Frequency / rate);
        // 每个输出采样按低字节在前占两字节。
        byte[] pcm = new byte[frames * 2];
        for (int frame = 0; frame < frames; frame++)
        {
            // 在原始设备数据中定位这一帧对应的交错声道起点。
            int source = (int)((long)frame * rate / Frequency) * channels;
            // 将同一时刻采到的多个声道混为一个单声道采样。
            float sum = 0;
            for (int channel = 0; channel < channels; channel++) sum += input[source + channel];
            // 限幅到有符号 16 位范围，正峰值 32767，负峰值 -32768。
            int value = Mathf.Clamp(Mathf.RoundToInt(sum / channels * 32768f), -32768, 32767);
            pcm[frame * 2] = (byte)value;
            pcm[frame * 2 + 1] = (byte)(value >> 8);
        }
        return pcm;
    }

    /// <summary>将小端网络采样恢复为 AudioClip 接受的浮点振幅。</summary><param name="pcm">完整的偶数字节 PCM。</param>
    public static float[] Decode(byte[] pcm)
    {
        // 每两个连续字节还原一个有符号单声道采样。
        float[] output = new float[pcm.Length / 2];
        for (int frame = 0; frame < output.Length; frame++)
            output[frame] = (short)(pcm[frame * 2] | pcm[frame * 2 + 1] << 8) / 32768f;
        return output;
    }
}
