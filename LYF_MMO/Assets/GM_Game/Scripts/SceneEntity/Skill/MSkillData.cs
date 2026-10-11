/// <summary>同一技能在当前职业和武器下的一段动作表现，核心归还时重置并回池。</summary>
public sealed class MSkillData
{
    /// <summary>本段对应的表现表主键，用于加载独立编辑器表现资产。</summary>
    public int PresentationId { get; private set; }

    /// <summary>该动作在武器表现列表中的顺序，从零开始。</summary>
    public int VariantIndex { get; private set; }

    /// <summary>该动作对应的 YooAsset 动画资源 Location。</summary>
    public string AnimationLocation { get; private set; }

    /// <summary>该动作配置的完整播放时长，单位为秒。</summary>
    public float DurationSeconds { get; private set; }

    /// <summary>RO 技能动作状态占用时长，单位为秒；可以短于完整剪辑时长。</summary>
    public float SkillStateTimeSeconds { get; private set; }

    /// <summary>创建空表现段，正式配置在借出后由 Initialize 写入。</summary>
    public MSkillData()
    {
    }

    /// <summary>从表现表行写入可复用动作段的配置快照。</summary>
    /// <param name="presentationId">表现表中的正数主键，零表示旧调用没有编辑资产。</param>
    /// <param name="variantIndex">表现表中的非负动作序号。</param>
    /// <param name="animationLocation">动画资源的完整 Location。</param>
    /// <param name="durationSeconds">完整动画时长，单位为秒。</param>
    /// <param name="skillStateTimeSeconds">动作状态占用时长，单位为秒。</param>
    public void Initialize(int presentationId, int variantIndex,
        string animationLocation, float durationSeconds,
        float skillStateTimeSeconds)
    {
        PresentationId = presentationId;
        VariantIndex = variantIndex;
        AnimationLocation = animationLocation;
        DurationSeconds = durationSeconds;
        SkillStateTimeSeconds = skillStateTimeSeconds;
        SkillRuntimePresentationCache.Retain(presentationId);
    }

    /// <summary>归还前解除动画资源路径引用和全部段时长。</summary>
    public void Reset()
    {
        SkillRuntimePresentationCache.Release(PresentationId);
        PresentationId = 0;
        VariantIndex = 0;
        AnimationLocation = null;
        DurationSeconds = 0f;
        SkillStateTimeSeconds = 0f;
    }
}
