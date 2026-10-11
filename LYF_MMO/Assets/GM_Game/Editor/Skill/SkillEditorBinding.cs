using System.Collections.Generic;
using UnityEngine;

/// <summary>将 Y 编辑资产明确绑定到旧表现行；编辑器程序集隔离包内和游戏的同名 Global。</summary>
public sealed class SkillEditorBinding : ScriptableObject
{
    /// <summary>旧表现表唯一行 ID；独立 JX 参考资产为零，不允许发布到游戏。</summary>
    public int PresentationId;

    /// <summary>该表现所属旧主技能 ID，不代替效果/模型/武器/变体键。</summary>
    public int SkillId;

    /// <summary>旧表现所属的效果行 ID，导出时重新对照二进制表。</summary>
    public int EffectId;

    /// <summary>旧表现所属职业 ID，导出时不得跨职业发布。</summary>
    public int ProfessionId;

    /// <summary>旧表现对应角色模型资源键，导出时核对原表。</summary>
    public string ModelKey;

    /// <summary>旧表现武器类型，零表示原表默认武器。</summary>
    public int WeaponType;

    /// <summary>旧表现变体索引，Q 的三段仍是三个独立绑定。</summary>
    public int VariantIndex;

    /// <summary>可用原 SkillEditorWindow 反复编辑保存的完整 Y 资产。</summary>
    public SkillConfigSO Config;

    /// <summary>只供隔离编辑器预览使用的现有角色预制体，不挂新运行播放器。</summary>
    public GameObject PreviewModel;

    /// <summary>旧动画剪辑真实引用，用于拒绝裁剪、替换或错误模型资源。</summary>
    public AnimationClip SourceClip;

    /// <summary>旧技能状态窗口的毫秒值，仅供核对；不由编辑器时间轴改写。</summary>
    public int SkillStateTimeMs;

    /// <summary>原剪辑事件的来源与量化参考，编辑后仍可查看原始时间。</summary>
    public List<SkillEditorFxSource> FxSources = new List<SkillEditorFxSource>();
}
