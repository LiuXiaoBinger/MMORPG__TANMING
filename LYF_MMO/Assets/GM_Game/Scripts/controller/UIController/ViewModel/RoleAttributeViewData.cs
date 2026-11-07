/// <summary>角色属性窗口的纯展示数据，数值全部来自服务器最终属性通知。</summary>
public sealed class RoleAttributeViewData
{
    /// <summary>角色当前昵称。</summary>
    public string Nickname;
    /// <summary>职业的界面名称。</summary>
    public string JobName;
    /// <summary>当前生命与上限的正文。</summary>
    public string HpText;
    /// <summary>当前法力与上限的正文。</summary>
    public string MpText;
    /// <summary>生命条比例，范围0至1。</summary>
    public float HpRatio;
    /// <summary>法力条比例，范围0至1。</summary>
    public float MpRatio;
    /// <summary>原16行依次为外攻、内攻、外防、内防、命中、格挡、振击、无咎、暴击、暴抗、暴伤、暴伤减免、治疗强度、治疗增益、诛心、坚韧。</summary>
    public string[] AttributeValues;
}
