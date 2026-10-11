/// <summary>RO召唤对象寿命绑定策略，具体销毁行为由未来宿主实现。</summary>
public enum ESkillMobLifeType
{
    /// <summary>由普通寿命规则管理。</summary>
    ESkillMobLifeType_ESMLT_Normal = 0,
    /// <summary>与所属技能生命周期绑定。</summary>
    ESkillMobLifeType_ESMLT_WithinSkill = 1,
    /// <summary>与召唤者生命周期绑定。</summary>
    ESkillMobLifeType_ESMLT_WithinSummoner = 2,
    /// <summary>与关联Buff生命周期绑定。</summary>
    ESkillMobLifeType_ESMLT_WithinBuff = 3,
    /// <summary>与拥有者生命周期绑定。</summary>
    ESkillMobLifeType_ESMLT_WithinOwner = 4,
}

