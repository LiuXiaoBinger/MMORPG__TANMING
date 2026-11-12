/// <summary>角色属性窗口的纯展示数据，数值全部来自服务器最终属性通知。</summary>
public sealed class RoleAttributeViewData
{
    /// <summary>高级属性的一项静态定义：协议编号、界面名称与数值单位。</summary>
    public sealed class AdvancedEntry
    {
        /// <summary>协议 AdvancedAttributes 中的属性编号。</summary>
        public readonly int Id;
        /// <summary>相邻两列展示时使用的界面名称。</summary>
        public readonly string Label;
        /// <summary>数值是否按百分数展示。</summary>
        public readonly bool IsPercent;

        /// <summary>构造一项高级属性定义。</summary>
        /// <param name="id">协议属性编号。</param>
        /// <param name="label">界面名称。</param>
        /// <param name="isPercent">true 表示该值以百分数展示。</param>
        public AdvancedEntry(int id, string label, bool isPercent)
        {
            Id = id;
            Label = label;
            IsPercent = isPercent;
        }
    }

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
    /// <summary>高级属性正文，顺序与 AdvancedEntries 一致，缺省值已按 0.0 或 0.0% 格式化。</summary>
    public string[] AdvancedValues;

    /// <summary>六组各自包含的属性项数，合计 44 项。</summary>
    public static readonly int[] AdvancedGroupSizes = { 5, 5, 5, 5, 12, 12 };

    /// <summary>44 项高级属性的编号、名称与单位，顺序与服务器属性编号一致。</summary>
    public static readonly AdvancedEntry[] AdvancedEntries =
    {
        new AdvancedEntry(101, "水属性攻击", false),
        new AdvancedEntry(102, "毒属性攻击", false),
        new AdvancedEntry(103, "火属性攻击", false),
        new AdvancedEntry(104, "风属性攻击", false),
        new AdvancedEntry(105, "元属性攻击", false),
        new AdvancedEntry(106, "水属性穿透", false),
        new AdvancedEntry(107, "毒属性穿透", false),
        new AdvancedEntry(108, "火属性穿透", false),
        new AdvancedEntry(109, "风属性穿透", false),
        new AdvancedEntry(110, "元属性穿透", false),
        new AdvancedEntry(111, "水属性抵抗", false),
        new AdvancedEntry(112, "毒属性抵抗", false),
        new AdvancedEntry(113, "火属性抵抗", false),
        new AdvancedEntry(114, "风属性抵抗", false),
        new AdvancedEntry(115, "元属性抵抗", false),
        new AdvancedEntry(116, "水穿透上限", false),
        new AdvancedEntry(117, "毒穿透上限", false),
        new AdvancedEntry(118, "火穿透上限", false),
        new AdvancedEntry(119, "风穿透上限", false),
        new AdvancedEntry(120, "元穿透上限", false),
        new AdvancedEntry(121, "扬刃·辟奕", true),
        new AdvancedEntry(122, "扬刃·问禅", true),
        new AdvancedEntry(123, "扬刃·无魇", true),
        new AdvancedEntry(124, "扬刃·覆极", true),
        new AdvancedEntry(125, "扬刃·驱鸩", true),
        new AdvancedEntry(126, "扬刃·化丹", true),
        new AdvancedEntry(127, "扬刃·灵卜", true),
        new AdvancedEntry(128, "扬刃·涤尘", true),
        new AdvancedEntry(129, "扬刃·吟风", true),
        new AdvancedEntry(130, "扬刃·雁翎", true),
        new AdvancedEntry(131, "扬刃·衔命", true),
        new AdvancedEntry(132, "扬刃·烹香", true),
        new AdvancedEntry(133, "拒芒·辟奕", true),
        new AdvancedEntry(134, "拒芒·问禅", true),
        new AdvancedEntry(135, "拒芒·无魇", true),
        new AdvancedEntry(136, "拒芒·覆极", true),
        new AdvancedEntry(137, "拒芒·驱鸩", true),
        new AdvancedEntry(138, "拒芒·化丹", true),
        new AdvancedEntry(139, "拒芒·灵卜", true),
        new AdvancedEntry(140, "拒芒·涤尘", true),
        new AdvancedEntry(141, "拒芒·吟风", true),
        new AdvancedEntry(142, "拒芒·雁翎", true),
        new AdvancedEntry(143, "拒芒·衔命", true),
        new AdvancedEntry(144, "拒芒·烹香", true)
    };
}
