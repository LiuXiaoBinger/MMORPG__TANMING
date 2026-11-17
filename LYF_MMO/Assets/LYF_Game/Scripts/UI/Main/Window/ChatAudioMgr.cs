using System;
using System.Collections;
using Google.Protobuf;
using UnityEngine;

/// <summary>参照 RO 的 ChatAudioMgr 管理麦克风录音、按块上传和按 audio_id 下载播放。</summary>
public sealed class ChatAudioMgr : MonoBehaviour
{
    /// <summary>正式窗口用于播放语音留言的二维音源。</summary>
    [SerializeField] private AudioSource _speaker;
    /// <summary>可替换的转写入口，默认实现立即返回空文本。</summary>
    [SerializeField] private ChatAudioTranscriber _transcriber;
    /// <summary>录音上传完成后发布到开始按住时的频道。</summary>
    public event Action<ChatChannel, AudioChat> Ready;
    /// <summary>当前打开的麦克风录音片段。</summary>
    private AudioClip _recording;
    /// <summary>本条留言打算发布的频道。</summary>
    private ChatChannel _channel;
    /// <summary>正在上传的完整单声道 PCM。</summary>
    private byte[] _upload;
    /// <summary>上一上传分块的预计结束字节位置。</summary>
    private int _sentEnd;
    /// <summary>等待当前上传回包的截止时间，使用实际秒数。</summary>
    private float _uploadDeadline;
    /// <summary>当前收听者点选的音频文件 ID。</summary>
    private string _downloadId;
    /// <summary>正在接收的完整 PCM 缓冲区。</summary>
    private byte[] _download;
    /// <summary>接下来请求的字节偏移。</summary>
    private int _received;
    /// <summary>播放后可点击再次收听的最后一个录音片段。</summary>
    private AudioClip _playingClip;

    /// <summary>窗口启用时订阅针对当前角色的两个媒体 RPC。</summary>
    private void OnEnable()
    {
        SocketDispatcher.Instance.AddEventHandler(NetDefine.RpcC2M_UploadChatAudio, OnUpload);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.RpcC2M_DownloadChatAudio, OnDownload);
    }

    /// <summary>窗口退出时解除订阅并释放麦克风及播放资源。</summary>
    private void OnDisable()
    {
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.RpcC2M_UploadChatAudio, OnUpload);
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.RpcC2M_DownloadChatAudio, OnDownload);
        FinishRecord(false);
        _upload = null;
        _downloadId = null;
        _download = null;
        if (_playingClip != null) { _speaker.Stop(); _speaker.clip = null; Destroy(_playingClip); _playingClip = null; }
    }

    /// <summary>断线后收不到上传回包时，解除本次录音占用。</summary>
    private void Update()
    {
        if (_upload != null && Time.unscaledTime > _uploadDeadline)
        {
            _upload = null;
            TipsMgr.Instance.ShowSystemTips("语音上传超时，请重新录制");
        }
    }

    /// <summary>开始捕获设备输入，多申请一秒以便在三十秒边界读取最终位置。</summary>
    /// <param name="channel">长按开始时所选频道。</param><returns>麦克风是否开始工作。</returns>
    public bool StartRecord(ChatChannel channel)
    {
        if (_recording != null || _upload != null) { TipsMgr.Instance.ShowSystemTips("上一条语音正在处理"); return false; }
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            TipsMgr.Instance.ShowSystemTips("允许麦克风后，请重新按住说话");
            return false;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            StartCoroutine(RequestMicrophone());
            return false;
        }
#endif
        if (Microphone.devices.Length == 0) { TipsMgr.Instance.ShowSystemTips("未找到麦克风"); return false; }
        _speaker.Stop();
        _channel = channel;
        _recording = Microphone.Start(null, false, 31, ChatAudioPcm.Frequency);
        if (_recording == null) TipsMgr.Instance.ShowSystemTips("无法打开麦克风");
        return _recording != null;
    }

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>通过 iOS 原生授权提示申请麦克风，用户再次长按即可录音。</summary>
    private IEnumerator RequestMicrophone()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        TipsMgr.Instance.ShowSystemTips("允许麦克风后，请重新按住说话");
    }
#endif

    /// <summary>读取有效采样，松开后上传，移出取消时直接释放。</summary><param name="send">是否保留录音。</param>
    public void FinishRecord(bool send)
    {
        if (_recording == null) return;
        // End 会重置设备位置，先读取本次实际写入的采样帧数。
        int frames = Microphone.GetPosition(null);
        Microphone.End(null);
        if (send)
        {
            if (frames < _recording.frequency) TipsMgr.Instance.ShowSystemTips("录音至少需要一秒");
            else
            {
                frames = Mathf.Min(frames, 30 * _recording.frequency);
                // 麦克风可能返回双声道或其他实际采样率。
                float[] samples = new float[frames * _recording.channels];
                if (_recording.GetData(samples, 0))
                {
                    _upload = ChatAudioPcm.Encode(samples, _recording.channels, _recording.frequency);
                    UploadChunk(0);
                }
                else TipsMgr.Instance.ShowSystemTips("读取录音失败");
            }
        }
        Destroy(_recording);
        _recording = null;
    }

    /// <summary>顺序发送一块录音，保留原 TCP 帧长上限。</summary><param name="offset">本分块起始字节。</param>
    private void UploadChunk(int offset)
    {
        // 普通帧最多承载 24 KiB 媒体数据。
        int count = Math.Min(NetDefine.ChatAudioChunkBytes, _upload.Length - offset);
        _sentEnd = offset + count;
        _uploadDeadline = Time.unscaledTime + 20f;
        // 尾块标志让 MS 在完整接收后签发文件 ID。
        UploadChatAudioArg arg = new UploadChatAudioArg { Offset = offset, Chunk = ByteString.CopyFrom(_upload, offset, count), Complete = _sentEnd == _upload.Length };
        if (!MNetClient.Instance.Send(NetDefine.RpcC2M_UploadChatAudio, arg.ToByteString()))
        {
            _upload = null;
            TipsMgr.Instance.ShowSystemTips("语音上传连接不可用");
        }
    }

    /// <summary>确认连续分块，末块携带可以发言的 audio_id 和真实时长。</summary><param name="data">MS 上传回包。</param>
    private void OnUpload(ByteString data)
    {
        // 直接按协议解码，窗口已取消的上传不再发布。
        UploadChatAudioRes res = UploadChatAudioRes.Parser.ParseFrom(data);
        if (_upload == null) return;
        if (res.Result != CmdCode.Succeed || res.NextOffset != _sentEnd)
        {
            _upload = null;
            TipsMgr.Instance.ShowSystemTips("语音上传失败");
            return;
        }
        if (_sentEnd < _upload.Length) { UploadChunk(_sentEnd); return; }
        // 保存本条留言的媒体和频道，转写回调允许异步返回。
        byte[] pcm = _upload;
        _upload = null;
        if (res.Audio == null) { TipsMgr.Instance.ShowSystemTips("未收到录音编号"); return; }
        // 即使用户在等待期间切换页签，仍发布到录音时选择的频道。
        ChatChannel channel = _channel;
        _transcriber.SpeechToText(pcm, ChatAudioPcm.Frequency, transcript =>
        {
            res.Audio.Text = transcript ?? string.Empty;
            Ready?.Invoke(channel, res.Audio);
        });
    }

    /// <summary>点击语音气泡，重复点击同一片段可停止或重播。</summary><param name="audio">该消息的录音 ID 和时长。</param>
    public void PlayRecordedFile(AudioChat audio)
    {
        _downloadId = audio.AudioId;
        _download = null;
        _received = 0;
        if (_playingClip != null && _playingClip.name == _downloadId)
        {
            if (_speaker.isPlaying) _speaker.Stop();
            else _speaker.Play();
            return;
        }
        _speaker.Stop();
        DownloadChunk();
    }

    /// <summary>按玩家选中的 ID 和已接收的位置请求下一块。</summary>
    private void DownloadChunk()
    {
        // 只有正在收听的这台客户端请求媒体数据。
        DownloadChatAudioArg arg = new DownloadChatAudioArg { AudioId = _downloadId, Offset = _received };
        if (!MNetClient.Instance.Send(NetDefine.RpcC2M_DownloadChatAudio, arg.ToByteString()))
        {
            _downloadId = null;
            _download = null;
            TipsMgr.Instance.ShowSystemTips("语音下载连接不可用");
        }
    }

    /// <summary>拼接连续分块并将完整录音交给当前窗口音源。</summary><param name="data">MS 下载回包。</param>
    private void OnDownload(ByteString data)
    {
        // 切换气泡后，原 ID 的晚到回包不参与当前播放。
        DownloadChatAudioRes res = DownloadChatAudioRes.Parser.ParseFrom(data);
        if (res.AudioId != _downloadId || res.Offset != _received) return;
        if (res.Result != CmdCode.Succeed || res.TotalBytes <= 0 || res.TotalBytes > NetDefine.ChatAudioMaxBytes)
        {
            _downloadId = null; _download = null;
            TipsMgr.Instance.ShowSystemTips("录音不可用或已过期");
            return;
        }
        if (_download == null) _download = new byte[res.TotalBytes];
        if (res.Chunk.Length == 0 || res.Chunk.Length > _download.Length - _received) return;
        res.Chunk.CopyTo(_download, _received);
        _received += res.Chunk.Length;
        if (_received < _download.Length) { DownloadChunk(); return; }
        // 完整采样转为 Unity 音频片段，只在当前音源保留最后一条。
        float[] samples = ChatAudioPcm.Decode(_download);
        _download = null;
        if (_playingClip != null) Destroy(_playingClip);
        _playingClip = AudioClip.Create(_downloadId, samples.Length, 1, ChatAudioPcm.Frequency, false);
        _playingClip.SetData(samples, 0);
        _speaker.clip = _playingClip;
        _speaker.Play();
        _downloadId = null;
    }
}
