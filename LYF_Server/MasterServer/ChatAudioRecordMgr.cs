using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;

/// <summary>按角色接收真实 PCM 录音，完整后才签发 audio_id，收听者凭消息中的 ID 分块读取。</summary>
internal sealed class ChatAudioRecordMgr
{
    /// <summary>该 GS 分区的内部连接，响应保留发起玩家的原始会话地址。</summary>
    private readonly BaseLink _game;
    /// <summary>每个在线发言人最多一条尚未传完的录音。</summary>
    private readonly Dictionary<ulong, MemoryStream> _uploading = new Dictionary<ulong, MemoryStream>();
    /// <summary>每个发言人最近一次上传成功、可发布的文件，其他角色不能冒用它发布。</summary>
    private readonly Dictionary<ulong, AudioChat> _ready = new Dictionary<ulong, AudioChat>();
    /// <summary>程序相邻的语音缓存，所有文件名均由服务端生成。</summary>
    private static readonly string Folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatAudioCache");
    /// <summary>最多每小时检查一次缓存的 24 小时保留期。</summary>
    private static DateTime _nextCleanup;

    /// <summary>为一个 GS 分区接入媒体传输。</summary><param name="game">该分区的可靠 TCP 连接。</param>
    internal ChatAudioRecordMgr(BaseLink game) { _game = game; }

    /// <summary>处理已通过 GS 认证的上传或按 ID 下载。</summary><param name="actor">内网身份及原始请求。</param>
    internal void Handle(MasterTeamRolePacket actor)
    {
        // 复用真实会话定位客户端，不把语音块广播给全频道。
        BasePackage packet = actor.Packet;
        if (packet.ProtoCode == NetDefine.RpcC2M_UploadChatAudio)
        {
            // 协议错误直接由现有网络框架处理，文件异常只在文件操作处捕获。
            UploadChatAudioArg arg = UploadChatAudioArg.Parser.ParseFrom(packet.Data);
            packet.Data = Upload(actor.RoleId, arg).ToByteString();
        }
        else if (packet.ProtoCode == NetDefine.RpcC2M_DownloadChatAudio)
        {
            // 收听者按收到的文件 ID 请求单块录音。
            DownloadChatAudioArg arg = DownloadChatAudioArg.Parser.ParseFrom(packet.Data);
            packet.Data = Download(arg).ToByteString();
        }
        else return;
        _game.Send(packet);
    }

    /// <summary>以实际上传数据为准补全语音消息时长。</summary>
    /// <param name="uid">登录会话确认的发言人。</param><param name="audio">待发送的文件引用。</param><returns>文件是否由此角色完成上传。</returns>
    internal bool PrepareMessage(ulong uid, AudioChat audio)
    {
        // 上一次完整上传的服务端结果。
        AudioChat saved;
        if (!_ready.TryGetValue(uid, out saved) || saved.AudioId != audio.AudioId) return false;
        audio.Duration = saved.Duration;
        return true;
    }

    /// <summary>释放下线角色的部分上传与发布资格，已发布文件在保留期内仍可收听。</summary><param name="uid">下线角色。</param>
    internal void Offline(ulong uid) { _uploading.Remove(uid); _ready.Remove(uid); }

    /// <summary>顺序拼接语音块，长度从实际字节数计算，收齐后写入私有缓存。</summary>
    /// <param name="uid">发言角色号。</param><param name="arg">字节偏移和本次媒体片。</param><returns>下一偏移或最终录音引用。</returns>
    private UploadChatAudioRes Upload(ulong uid, UploadChatAudioArg arg)
    {
        // 错误返回不会签发任何可供播放的 ID。
        UploadChatAudioRes result = new UploadChatAudioRes { Result = CmdCode.ReqParamError };
        if (arg.Chunk.Length == 0 || arg.Chunk.Length > NetDefine.ChatAudioChunkBytes) return result;
        // offset 为 0 开始该角色的一次新上传。
        MemoryStream audio;
        if (arg.Offset == 0) { audio = new MemoryStream(); _uploading[uid] = audio; }
        else if (!_uploading.TryGetValue(uid, out audio)) return result;
        if (arg.Offset != audio.Length || audio.Length + arg.Chunk.Length > NetDefine.ChatAudioMaxBytes) return result;
        arg.Chunk.WriteTo(audio);
        result.NextOffset = (int)audio.Length;
        if (arg.Complete)
        {
            _uploading.Remove(uid);
            if (audio.Length < NetDefine.ChatAudioBytesPerSecond || (audio.Length & 1) != 0) return result;
            // GUID 用作不可枚举的媒体引用，不接受上传端自定义文件名。
            string id = Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Folder);
                CleanOldFiles();
                File.WriteAllBytes(Path.Combine(Folder, id + ".pcm"), audio.ToArray());
            }
            catch (IOException e) { LogMsg.Info("保存语音失败：" + e.Message, LogMsgType.Error); result.Result = CmdCode.ServerError; return result; }
            catch (UnauthorizedAccessException e) { LogMsg.Info("语音目录不可写：" + e.Message, LogMsgType.Error); result.Result = CmdCode.ServerError; return result; }
            result.Audio = new AudioChat { AudioId = id, Duration = (float)audio.Length / NetDefine.ChatAudioBytesPerSecond };
            _ready[uid] = result.Audio;
        }
        result.Result = CmdCode.Succeed;
        return result;
    }

    /// <summary>仅按合法的服务器 GUID 在指定目录读取一块音频。</summary>
    /// <param name="arg">媒体 ID 和所需偏移。</param><returns>数据、总长度或明确错误。</returns>
    private static DownloadChatAudioRes Download(DownloadChatAudioArg arg)
    {
        // 在读取到数据之前始终按失败返回。
        DownloadChatAudioRes result = new DownloadChatAudioRes { Result = CmdCode.ReqParamError, AudioId = arg.AudioId, Offset = arg.Offset };
        // 严格限制为服务端签发的文件名，不使用用户提供的路径。
        Guid id;
        if (!Guid.TryParseExact(arg.AudioId, "N", out id) || arg.Offset < 0) return result;
        // 只打开当前应用的录音缓存。
        string path = Path.Combine(Folder, id.ToString("N") + ".pcm");
        try
        {
            if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddHours(-24)) return result;
            using (FileStream stream = File.OpenRead(path))
            {
                if (arg.Offset >= stream.Length || stream.Length > NetDefine.ChatAudioMaxBytes) return result;
                stream.Position = arg.Offset;
                result.TotalBytes = (int)stream.Length;
                // 每次读取一个能通过现有协议头及 CRC 校验的片段。
                int count = Math.Min(NetDefine.ChatAudioChunkBytes, result.TotalBytes - arg.Offset);
                using (BinaryReader reader = new BinaryReader(stream)) { result.Chunk = ByteString.CopyFrom(reader.ReadBytes(count)); }
            }
        }
        catch (IOException) { return result; }
        catch (UnauthorizedAccessException) { return result; }
        result.Result = CmdCode.Succeed;
        return result;
    }

    /// <summary>仅清理录音缓存目录内过期、符合本服务生成规则的文件。</summary>
    private static void CleanOldFiles()
    {
        // 各 GS 分区共用服务内的媒体目录，业务回调在同一分区锁下执行。
        DateTime now = DateTime.UtcNow;
        if (now < _nextCleanup) return;
        _nextCleanup = now.AddHours(1);
        // 文件名必须符合随机 ID，其他文件不属于本程序。
        string[] files = Directory.GetFiles(Folder, "*.pcm");
        for (int i = 0; i < files.Length; i++)
        {
            // 清理通过验证的旧媒体文件。
            Guid id;
            if (Guid.TryParseExact(Path.GetFileNameWithoutExtension(files[i]), "N", out id) &&
                File.GetLastWriteTimeUtc(files[i]) < now.AddHours(-24)) File.Delete(files[i]);
        }
    }
}
