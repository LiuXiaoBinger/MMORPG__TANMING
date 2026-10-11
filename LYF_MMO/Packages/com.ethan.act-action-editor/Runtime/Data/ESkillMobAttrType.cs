/// <summary>RO召唤属性来源，快照在召唤时机冻结而非持续读取。</summary>
public enum ESkillMobAttrType
{
    /// <summary>从召唤模板配置读取属性。</summary>
    kMobAttrType_Config = 0,
    /// <summary>使用施法者属性快照。</summary>
    kMobAttrType_FirerSnapShot = 1,
    /// <summary>使用目标当前属性。</summary>
    kMobAttrType_Target = 2,
    /// <summary>使用Buff宿主当前属性。</summary>
    kMobAttrType_BuffHoster = 3,
    /// <summary>使用Buff宿主属性快照。</summary>
    kMobAttrType_BuffHosterSnapShot = 4,
}

