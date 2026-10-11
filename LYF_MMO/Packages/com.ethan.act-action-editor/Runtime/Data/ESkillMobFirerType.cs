/// <summary>RO召唤单位归属来源，保留与Buff宿主关联的区别。</summary>
public enum ESkillMobFirerType
{
    /// <summary>归属于施法者。</summary>
    kMobFirerType_Firer = 0,
    /// <summary>归属于技能目标。</summary>
    kMobFirerType_Target = 1,
    /// <summary>归属于Buff宿主。</summary>
    kMobFirerType_BuffHoster = 2,
}

