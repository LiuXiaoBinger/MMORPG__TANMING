/// <summary>单次施法时间轴的强类型回调参数，替代 RO 的事件编号加 object[] 载荷。</summary>
public sealed class MSkillTimerArgs
{
    /// <summary>注册事件时锁定的非零施法 UUID，用于阻止回池后旧回调作用于新施法。</summary>
    public ulong SkillUuid { get; private set; }

    /// <summary>注册时锁定的池实例施法代次，同 UUID 同步重新 Fire 时也拒绝旧回调。</summary>
    public ulong FireGeneration { get; private set; }

    /// <summary>RO的EFx/EAudio/ECharge编号，或本项目无载荷滑行残影EGhost编号。</summary>
    public int Id { get; private set; }

    /// <summary>粒子回调持有的本次锁定事件；声音回调中为空。</summary>
    public SkillRuntimeFxEvent FxEvent { get; private set; }

    /// <summary>声音回调持有的本次锁定事件；粒子回调中为空。</summary>
    public SkillRuntimeAudioEvent AudioEvent { get; private set; }

    /// <summary>程序位移回调持有的本次锁定指令；声音和粒子回调中为空。</summary>
    public MSkillChargeData ChargeData { get; private set; }
    /// <summary>冷资产登记时已过的段内秒数；负一表示正常未来回调，不丢本帧跨界运动。</summary>
    public float ChargeInitialElapsedSeconds { get; private set; } = -1f;

    /// <summary>创建无业务载荷的表现计时参数，用于原滑行动作的残影采样。</summary>
    /// <param name="skillUuid">回调所属的非零施法UUID。</param>
    /// <param name="id">本项目残影回调编号，不占用RO既有事件编号。</param>
    /// <param name="fireGeneration">登记时的施法代次，动作结束或对象复用后回调失效。</param>
    public MSkillTimerArgs(ulong skillUuid, int id, ulong fireGeneration)
    {
        SkillUuid = skillUuid;
        Id = id;
        FireGeneration = fireGeneration;
    }

    /// <summary>创建粒子时间轴参数，生命周期从注册延续到成功派发或施法取消。</summary>
    /// <param name="skillUuid">本次注册所属的非零施法 UUID。</param>
    /// <param name="id">RO 粒子回调编号 EFx。</param>
    /// <param name="fxEvent">已经完整校验的锁定粒子指令。</param>
    /// <param name="fireGeneration">技能实例当前施法代次；独立管理器调用可使用默认零代次。</param>
    public MSkillTimerArgs(ulong skillUuid, int id, SkillRuntimeFxEvent fxEvent, ulong fireGeneration = 0)
    {
        SkillUuid = skillUuid;
        FireGeneration = fireGeneration;
        Id = id;
        FxEvent = fxEvent;
    }

    /// <summary>创建声音时间轴参数，生命周期从注册延续到成功派发或施法取消。</summary>
    /// <param name="skillUuid">本次注册所属的非零施法 UUID。</param>
    /// <param name="id">RO 声音回调编号 EAudio。</param>
    /// <param name="audioEvent">已经完整校验的锁定单次声音指令。</param>
    /// <param name="fireGeneration">技能实例当前施法代次；独立管理器调用可使用默认零代次。</param>
    public MSkillTimerArgs(ulong skillUuid, int id, SkillRuntimeAudioEvent audioEvent, ulong fireGeneration = 0)
    {
        SkillUuid = skillUuid;
        FireGeneration = fireGeneration;
        Id = id;
        AudioEvent = audioEvent;
    }
    /// <summary>创建 RO 程序位移时间轴参数，保持 UUID、代次及冷登记基线直到派发。</summary>
    /// <param name="skillUuid">本次施法的非零 UUID。</param>
    /// <param name="id">RO 原编号 ECharge，值为十三。</param>
    /// <param name="chargeData">已完整校验的前向程序位移指令。</param>
    /// <param name="fireGeneration">本池实例当前施法代次。</param>
    /// <param name="initialElapsedSeconds">冷登记时已经经过的段内秒数，正常未来回调传负一。</param>
    public MSkillTimerArgs(ulong skillUuid, int id, MSkillChargeData chargeData,
        ulong fireGeneration, float initialElapsedSeconds)
    {
        SkillUuid = skillUuid;
        Id = id;
        FireGeneration = fireGeneration;
        ChargeData = chargeData;
        ChargeInitialElapsedSeconds = initialElapsedSeconds;
    }
}
