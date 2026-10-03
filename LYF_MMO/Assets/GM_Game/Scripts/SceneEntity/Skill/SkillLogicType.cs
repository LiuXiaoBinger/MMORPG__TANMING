/// <summary>RO 技能运行子类的类型编号，工厂按编号借出对应具体类型。</summary>
public static class SkillLogicType
{
    /// <summary>普通技能及普通攻击的运行类型编号。</summary>
    public const int SkillTypeArt = 0;

    /// <summary>按子技能列表依次启动的组合技能运行类型编号。</summary>
    public const int SkillTypeCombined = 1;

    /// <summary>拥有持续引导阶段的技能运行类型编号。</summary>
    public const int SkillTypeChannelling = 2;
}
