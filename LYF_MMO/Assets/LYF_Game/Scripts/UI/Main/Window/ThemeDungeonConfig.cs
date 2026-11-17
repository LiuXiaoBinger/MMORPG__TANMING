using System;

/// <summary>
/// 主题副本配置根节点，用于把 theme_dungeon.json 反序列化为对象。
/// JsonUtility 不支持直接反序列化数组，因此用 items 字段包裹。
/// </summary>
[Serializable]
public class ThemeDungeonConfig
{
    /// <summary>全部主题副本条目，顺序与配置文件一致，决定书位排列顺序。</summary>
    public ThemeDungeonEntry[] items;
}
