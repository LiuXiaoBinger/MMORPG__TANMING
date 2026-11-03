using System;

/// <summary>魔神塔 EntityTable 配置到具体表现模型的映射。</summary>
[Serializable]
public sealed class ROSceneCreatureEntity
{
    /// <summary>RO 怪物实体表编号，与 wave 的 entity_id 一致。</summary>
    public int id;
    /// <summary>RO 怪物表中的名称，包含不可见机制控制实体。</summary>
    public string displayName;
    /// <summary>PresentTable 的模型名；Monster_Empty 表示原配置本就不可见。</summary>
    public string modelName;
    /// <summary>EntityTable 的 PresentRatio，无单位。</summary>
    public float scale;
    /// <summary>PresentTable 指定的默认待机动作名称。</summary>
    public string idleAnimation;
    /// <summary>PresentTable 指定的出场动作名称，允许为空。</summary>
    public string bornAnimation;
}
