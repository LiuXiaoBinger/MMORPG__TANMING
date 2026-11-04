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
    /// <summary>正式副本使用的已配置控制器与实体组件的预制体地址。</summary>
    public string runtimePrefabPath;
    /// <summary>原 PresentTable 受击动作名，空值表示原模型没有受击动作。</summary>
    public string hitAnimation;
    /// <summary>非战斗 NPC 标记，与怪物使用独立表编号。</summary>
    public bool isNpc;
    /// <summary>副本剧情怪物为 true 时只参与场景表现，旧配置默认参与战斗。</summary>
    public bool nonCombatant;
}
