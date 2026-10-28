using System;

/// <summary>
/// 主题副本配置中的单条副本数据，字段与 theme_dungeon.json 的 items 元素一一对应。
/// </summary>
[Serializable]
public class ThemeDungeonEntry
{
    /// <summary>副本唯一编号，对应配置表的 id 列。</summary>
    public int id;

    /// <summary>副本显示名称，如"北方异变之森"。</summary>
    public string themeName;

    /// <summary>章节文案，如"章节·一"。</summary>
    public string chapterText;

    /// <summary>进入该副本所需的角色等级，低于此等级显示锁定遮罩。</summary>
    public int chapterLevel;

    /// <summary>副本场景缩略图与背景大图的资源相对路径，相对 UI/Texture/ 目录，带 .png 后缀。</summary>
    public string scenePic;

    /// <summary>首领怪物编号。</summary>
    public int leaderId;

    /// <summary>首领名称，如"凯思利亚斯"。</summary>
    public string leaderName;

    /// <summary>首领立绘的资源相对路径，相对 UI/Texture/ 目录，带 .png 后缀。</summary>
    public string painting;

    /// <summary>剧情难度副本编号。</summary>
    public int storyDungeonId;

    /// <summary>普通难度副本编号。</summary>
    public int normalDungeonId;

    /// <summary>困难难度副本编号。</summary>
    public int hardDungeonId;

    /// <summary>开启该副本所需的服务器世界等级。</summary>
    public int serverLv;

    /// <summary>副本背景说明文案，含 \n 换行转义。</summary>
    public string chapterContent;
}
