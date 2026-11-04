using System;
using UnityEngine;

/// <summary>预留语音转写接口，默认返回空字符串，发送和播放均不依赖第三方服务。</summary>
public class ChatAudioTranscriber : MonoBehaviour
{
    /// <summary>未来的转写实现可在获得文本后异步回调。</summary>
    /// <param name="pcm">单声道、小端有符号 16 位 PCM。</param><param name="frequency">采样率，单位 Hz。</param>
    /// <param name="completed">完成回调，默认无转写正文。</param>
    public virtual void SpeechToText(byte[] pcm, int frequency, Action<string> completed)
    {
        completed(string.Empty);
    }
}
