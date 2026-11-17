using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>消息池中的语音气泡，只持有当前行的 audio_id 引用。</summary>
public sealed class ChatVoiceItem : MonoBehaviour
{
    /// <summary>预制体中已有的播放按钮。</summary>
    [SerializeField] private Button _button;
    /// <summary>预制体中的时长标签，单位秒。</summary>
    [SerializeField] private TMP_Text _duration;
    /// <summary>复用后当前行的语音引用。</summary>
    private AudioChat _audio;
    /// <summary>点击后由聊天控制器下载并播放。</summary>
    private Action<AudioChat> _play;
    /// <summary>只注册一次点击，不在滚动复用时叠加监听。</summary>
    private void Awake() { _button.onClick.AddListener(OnClick); }
    /// <summary>解除按钮监听。</summary>
    private void OnDestroy() { _button.onClick.RemoveListener(OnClick); }
    /// <summary>把池化行绑定到本条消息。</summary><param name="audio">当前语音或空。</param><param name="play">播放请求入口。</param>
    public void Bind(AudioChat audio, Action<AudioChat> play)
    {
        _audio = audio; _play = play;
        if (_audio != null) _duration.text = Mathf.CeilToInt(_audio.Duration) + " 秒";
    }
    /// <summary>将点击交给当前行的文件，避免播放上一行内容。</summary>
    private void OnClick() { if (_audio != null) _play?.Invoke(_audio); }
}
