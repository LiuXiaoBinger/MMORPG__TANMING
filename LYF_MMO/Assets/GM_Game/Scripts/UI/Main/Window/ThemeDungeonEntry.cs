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

    /// <summary>配置中的章节等级，当前选择页和详情页用于显示 Lv. 文案，不据此锁定入口。</summary>
    public int chapterLevel;

    /// <summary>书位缩略图和背景大图路径；相对选择窗口配置的纹理根目录，默认 UI/Texture/，加载时移除扩展名。</summary>
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

    /// <summary>配置记录的服务器等级条件，当前这两个展示窗口没有用它限制进入。</summary>
    public int serverLv;

    /// <summary>配置中的副本背景文案；当前详情页使用 BuildTabText 固定文案，尚未显示此字段。</summary>
    public string chapterContent;
}
