/// <summary>一类实体的时间轴资源与代码实现选择，避免按实体编号写死加载入口。</summary>
internal sealed class DungeonTimelineBinding
{
    /// <summary>承载此时间轴的EntityTable编号。</summary>
    internal int EntityId;
    /// <summary>Lua文件与原全局脚本表使用的共同名称。</summary>
    internal string Script;
    /// <summary>明确选择已实现的代码时间轴；未知名称不会回退其他副本。</summary>
    internal string CodeProfile;
}
