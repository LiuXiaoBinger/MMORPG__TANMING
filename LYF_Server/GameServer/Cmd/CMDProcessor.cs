/// <summary>RO 命令处理器基类。游戏服按命令名注册，返回内容直接用于本次 RPC 回执。</summary>
internal abstract class CMDProcessor
{
    /// <summary>从命令行首词查找此处理器使用的名称。</summary>
    internal abstract string Name { get; }

    /// <summary>在游戏逻辑线程执行当前玩家的指令。</summary>
    /// <param name="role">发起请求的在线角色。</param>
    /// <param name="args">包含命令名在内的各个词。</param>
    internal abstract string Process(OnlineRole role, string[] args);
}
