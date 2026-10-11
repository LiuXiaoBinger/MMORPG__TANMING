/// <summary>RO召唤原点来源，序号用于保留原配置语义。</summary>
public enum ESkillCastPointType
{
    /// <summary>以施法者为原点。</summary>
    ESkillCastPointType_FirerBase = 0,
    /// <summary>以技能目标为原点。</summary>
    ESkillCastPointType_TargetBase = 1,
    /// <summary>以SceneCastKey对应的场景点为原点。</summary>
    ESkillCastPointType_SceneBase = 2,
}

