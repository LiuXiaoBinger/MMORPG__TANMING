/// <summary>
/// 预留的技能运行类型编号。
/// 当前 MSkillFactory.CreateSkill 固定创建 MArtSkill，尚未按这些编号选择组合或引导技能。
/// </summary>
public static class SkillLogicType
{
    /// <summary>普通技能及普通攻击的运行类型编号。</summary>
    public const int SkillTypeArt = 0;

    /// <summary>预留的组合技能类型编号，当前工厂未实现该分支。</summary>
    public const int SkillTypeCombined = 1;

    /// <summary>预留的持续引导技能类型编号，当前工厂未实现该分支。</summary>
    public const int SkillTypeChannelling = 2;
}
