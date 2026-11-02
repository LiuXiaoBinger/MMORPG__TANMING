using System.Reflection;

/// <summary>
/// 独立测试进程中的正吟唱状态夹具。正式技能表尚无吟唱时长，不能再用普攻 CastMs 伪造读条。
/// 仅用反射准备一个已经开始的 2000 毫秒吟唱，保留计时、打断和队列测试；不进入正式服务程序集。
/// 此夹具不验证正式 OnStart 的正吟唱配置入口，真实技能入口由零吟唱回归单独验证。
/// </summary>
public static class SingingStateFixture
{
    /// <summary>合成吟唱的持续时间，单位毫秒，与任何正式技能的动作时长无关。</summary>
    public const int DurationMilliseconds = 2000;

    /// <summary>将测试角色置于已开始、尚未扣蓝或写冷却的合成吟唱状态。</summary>
    /// <param name="role">仅存在于测试进程、当前未施法的角色。</param>
    /// <param name="skillId">吟唱完成后交给真实技能组件校验的技能主表 ID。</param>
    /// <param name="skillUuid">本次测试请求的施法标识，用于核对终态回包。</param>
    public static void Start(OnlineRole role, int skillId, ulong skillUuid)
    {
        // singing 是角色原有的正式吟唱组件，测试不替换其计时或回包实现。
        RoleSingingComponent singing;
        role.TryGetComponent(out singing);
        // machine 仍由角色每帧推进，仅在本夹具中设置初始状态引用。
        StateMachineComponent machine;
        role.TryGetComponent(out machine);
        // skills 用来解析该角色真实学习等级对应的技能核心。
        RoleSkillComponent skills;
        role.TryGetComponent(out skills);
        // core 在合成吟唱存续期间由原组件持有，完成或打断后按正式逻辑释放引用。
        ROSkillCore core = skills.ResolveSkillCore(skillId);
        typeof(SingingComponent).GetField("skill_id_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, skillId);
        typeof(SingingComponent).GetField("skill_effect_id_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, core.SkillEffectID());
        typeof(SingingComponent).GetField("skill_uuid_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, skillUuid);
        typeof(SingingComponent).GetField("core_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, core);
        typeof(SingingComponent).GetField("duration_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, 0);
        typeof(SingingComponent).GetField("interval_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, DurationMilliseconds);
        typeof(SingingComponent).GetField("start_singing_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, true);
        typeof(ActionStateComponent<ROSingingArgs>).GetField("is_finished_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(singing, false);
        typeof(StateMachineComponent).GetField("current_", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(machine, singing);
    }
}
