/// <summary>
/// 角色当前信息窗口的展示数据。
/// </summary>
public sealed class RoleCurrentViewData
{
    /// <summary>角色昵称。</summary>
    public string Nickname { get; }
    /// <summary>职业显示名称。</summary>
    public string JobName { get; }
    /// <summary>角色等级。</summary>
    public int Level { get; }
    /// <summary>头像图标路径。</summary>
    public string HeadIconPath { get; }
    /// <summary>当前生命比例。</summary>
    public float HpRatio { get; }
    /// <summary>当前法力比例。</summary>
    public float MpRatio { get; }
    /// <summary>生命值显示文本。</summary>
    public string HpText { get; }
    /// <summary>法力值显示文本。</summary>
    public string MpText { get; }

    /// <summary>创建角色当前状态展示数据。</summary>
    public RoleCurrentViewData(string nickname, string jobName, int level, string headIconPath,
        float hpRatio, float mpRatio, string hpText, string mpText)
    {
        Nickname = nickname;
        if (Nickname == null)
        {
            Nickname = string.Empty;
        }
        JobName = jobName;
        if (JobName == null)
        {
            JobName = string.Empty;
        }
        Level = level;
        HeadIconPath = headIconPath;
        if (HeadIconPath == null)
        {
            HeadIconPath = string.Empty;
        }
        HpRatio = hpRatio;
        MpRatio = mpRatio;
        HpText = hpText;
        if (HpText == null)
        {
            HpText = string.Empty;
        }
        MpText = mpText;
        if (MpText == null)
        {
            MpText = string.Empty;
        }
    }
}
