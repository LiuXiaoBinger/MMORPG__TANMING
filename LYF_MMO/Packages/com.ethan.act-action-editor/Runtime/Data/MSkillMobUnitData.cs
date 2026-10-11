using System;

/// <summary>RO召唤单位事件，字段名与pbl_skill.proto一致；只保存制作数据，召唤与属性结算由后续宿主实现。</summary>
[Serializable]
public sealed class MSkillMobUnitData
{
    /// <summary>召唤原点来源：施法者、目标或场景配置点。</summary>
    public ESkillCastPointType CastPointType;

    /// <summary>从本次技能开始计时的召唤时刻，单位秒，必须非负。</summary>
    public float At;

    /// <summary>是否按LoopCycle重复召唤；未启用时只执行一次。</summary>
    public bool Loop;

    /// <summary>循环召唤次数，启用Loop时必须为正整数。</summary>
    public int LoopCount;

    /// <summary>重复召唤的间隔，单位秒；启用Loop时必须大于零。</summary>
    public float LoopCycle;

    /// <summary>被召唤单位的RO模板ID；完成配置后必须为正整数，不是预制体GUID。</summary>
    public int TemplateID;

    /// <summary>召唤点采用固定偏移或环形区域随机取点。</summary>
    public ESkillMobPositionType MobPositionType;

    /// <summary>召唤单位寿命与技能、召唤者、Buff或拥有者的绑定规则。</summary>
    public ESkillMobLifeType MobLifeType;

    /// <summary>是否复制召唤来源的仇恨记录；本编辑器只保留该规则。</summary>
    public bool IsCopyHated;

    /// <summary>是否复制召唤来源的属性；具体属性计算由宿主决定。</summary>
    public bool IsCopyAttr;

    /// <summary>召唤对象类别：怪物、替身、陷阱、阻挡或生命体。</summary>
    public ESkillMobMonsterType MobMonsterType;

    /// <summary>召唤对象配置寿命，单位秒；零的具体策略由MobLifeType和宿主解释。</summary>
    public float LifeTime;

    /// <summary>相对召唤原点的横向偏移，单位米。</summary>
    public float OffsetAtX;

    /// <summary>相对召唤原点的高度偏移，单位米。</summary>
    public float OffsetAtY;

    /// <summary>相对召唤原点的纵向偏移，单位米。</summary>
    public float OffsetAtZ;

    /// <summary>随机环形召唤区域的内半径，单位米，非负。</summary>
    public float MinRadius;

    /// <summary>随机环形召唤区域的外半径，单位米，不小于内半径。</summary>
    public float MaxRadius;

    /// <summary>RO事件配置索引，用于关联技能规则；不是编辑器列表下标。</summary>
    public int Index;

    /// <summary>召唤归属来源：施法者、目标或Buff宿主。</summary>
    public ESkillMobFirerType MobFirerType;

    /// <summary>召唤属性来源及是否使用来源快照。</summary>
    public ESkillMobAttrType MobAttrType;

    /// <summary>场景召唤点的配置键；使用SceneBase时必须填写。</summary>
    public string SceneCastKey = string.Empty;

    /// <summary>召唤对象与拥有者允许的最大距离，单位米，非负。</summary>
    public float MaxDistanceWithOwner;

    /// <summary>召唤对象是否允许被技能影响，具体筛选由宿主处理。</summary>
    public bool CanBeAffected;
}

