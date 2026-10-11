/// <summary>RO召唤位置生成方式，固定偏移和环形随机保持原值。</summary>
public enum ESkillMobPositionType
{
    /// <summary>按OffsetAtX/Y/Z生成固定偏移点。</summary>
    ESkillMobPositionType_FIXED_OFFSET = 0,
    /// <summary>在MinRadius至MaxRadius环形区域内取点。</summary>
    ESkillMobPositionType_RANDOM_CIRCLE = 1,
}

