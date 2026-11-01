using System;
using UnityEngine;

/// <summary>展示界面中的一个动作，将按钮名称与原版动画片段对应起来。</summary>
[Serializable]
public sealed class ROCreatureAnimationDefinition
{
    /// <summary>动作按钮显示的名称，例如待机、跑步或技能名称。</summary>
    public string displayName;

    /// <summary>原版 RO 动画片段，使用对应模型的骨骼路径。</summary>
    public AnimationClip clip;

    /// <summary>按触发秒数排序的原技能特效事件；待机等无特效动作保持空数组。</summary>
    public ROCreatureEffectDefinition[] effects = Array.Empty<ROCreatureEffectDefinition>();
}
