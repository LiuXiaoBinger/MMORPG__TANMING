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
    /// <summary>当前生命条比例，调用方通常传入 0 到 1。</summary>
    public float HpRatio { get; }
    /// <summary>当前法力条比例，调用方通常传入 0 到 1。</summary>
    public float MpRatio { get; }
    /// <summary>生命值显示文本。</summary>
    public string HpText { get; }
    /// <summary>法力值显示文本。</summary>
    public string MpText { get; }

    /// <summary>创建角色当前状态展示数据。</summary>
    /// <param name="nickname">角色昵称，null 转为空串。</param>
    /// <param name="jobName">配置表解析后的职业名称。</param>
    /// <param name="level">角色当前等级。</param>
    /// <param name="headIconPath">头像资源路径。</param>
    /// <param name="hpRatio">生命条填充比例，本类型不再限制范围。</param>
    /// <param name="mpRatio">法力条填充比例，本类型不再限制范围。</param>
    /// <param name="hpText">控制器已经排好的生命值显示文字。</param>
    /// <param name="mpText">控制器已经排好的法力值显示文字。</param>
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
