/// <summary>
/// 角色状态枚举定义
/// </summary>
public enum StateDefine
{
    /// <summary>
    /// 默认空闲状态
    /// </summary>
    kStateDefault = 0,

    /// <summary>
    /// 吟唱状态（读条施法、唱歌等）
    /// </summary>
    kStateSinging,

    /// <summary>
    /// 释放技能状态
    /// </summary>
    kStateSkill,

    /// <summary>
    /// 特殊动作状态（必杀、大招、特殊表演动作）
    /// </summary>
    kStateSpecial,

    /// <summary>
    /// 死亡状态
    /// </summary>
    kStateDead,

    /// <summary>
    /// 恐惧逃跑状态
    /// </summary>
    kStateFearRun,

    /// <summary>
    /// 受击状态
    /// </summary>
    kStateBehit,

    /// <summary>
    /// 眩晕状态
    /// </summary>
    kStateStun,

    /// <summary>
    /// 攀爬状态
    /// </summary>
    kStateClimbing,

    /// <summary>
    /// 采集状态
    /// </summary>
    kStateCollect,

    /// <summary>
    /// 出生/复活生成状态
    /// </summary>
    kStateBorn,

    /// <summary>
    /// 钓鱼状态
    /// </summary>
    kStateFishing,

    /// <summary>
    /// 双动作动画片段状态（同时播放两层动作）
    /// </summary>
    kStateDoubleActionClip,

    /// <summary>
    /// 跳舞状态
    /// </summary>
    kStateDance,

    /// <summary>
    /// 场景触发物体交互状态
    /// </summary>
    kStateSceneTriggerObject,

    /// <summary>
    /// 状态总数，用于数组/循环边界（不代表实际角色状态）
    /// </summary>
    kStateCount
}