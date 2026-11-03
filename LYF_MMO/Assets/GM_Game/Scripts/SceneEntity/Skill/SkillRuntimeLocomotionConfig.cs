using UnityEngine;

/// <summary>
/// 保存普通移动和跳跃动作的声音配置，分别提供走、跑、战斗走跑、起跳与落地轨道。
/// 这些动作不是技能释放，不使用服务器技能表。
/// </summary>
public sealed class SkillRuntimeLocomotionConfig : ScriptableObject
{
    /// <summary>行走动画及随步点循环触发的声音轨道。</summary>
    public SkillRuntimeLocomotionAction Walk = new SkillRuntimeLocomotionAction();
    /// <summary>跑步动画及随步点循环触发的声音轨道。</summary>
    public SkillRuntimeLocomotionAction Run = new SkillRuntimeLocomotionAction();
    /// <summary>战斗行走原动画与脚步声音轨道，施法后仍覆盖现有战斗移动状态。</summary>
    public SkillRuntimeLocomotionAction BattleWalk = new SkillRuntimeLocomotionAction();
    /// <summary>战斗跑步原动画与脚步声音轨道，不与普通跑步的不同长度混用。</summary>
    public SkillRuntimeLocomotionAction BattleRun = new SkillRuntimeLocomotionAction();
    /// <summary>每次起跳动画的单次声音轨道。</summary>
    public SkillRuntimeLocomotionAction JumpStart = new SkillRuntimeLocomotionAction();
    /// <summary>每次落地动作的单次声音轨道。</summary>
    public SkillRuntimeLocomotionAction JumpEnd = new SkillRuntimeLocomotionAction();
}
