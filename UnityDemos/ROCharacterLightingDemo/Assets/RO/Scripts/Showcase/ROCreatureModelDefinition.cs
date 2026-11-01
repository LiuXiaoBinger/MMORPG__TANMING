using System;
using UnityEngine;

/// <summary>一只怪物或 NPC 的展示配置，保存模型及其可播放动作。</summary>
[Serializable]
public sealed class ROCreatureModelDefinition
{
    /// <summary>模型列表和当前模型标题使用的中文名称。</summary>
    public string displayName;

    /// <summary>已导入并保留原始骨骼、材质的 RO 模型预制体。</summary>
    public GameObject prefab;

    /// <summary>只属于该模型的动作列表，按钮顺序与此数组一致。</summary>
    public ROCreatureAnimationDefinition[] animations;

    /// <summary>切换模型后自动播放的动作索引，优先使用原版待机动作。</summary>
    public int defaultAnimation;
}
