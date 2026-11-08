using System;
using System.Collections.Generic;
using GameServer.SceneEntity;
using MMoRpgCommon.SceneEntity;

/// <summary>服务端场景实体的基因组件，管理实例及周期结果，不承担角色持久化或网络协议。</summary>
public class GeneComponent : RuntimeEntityComponent
{
    /// <summary>当前实体的独立基因容器；客户端与服务端使用同一份运行算法源码。</summary>
    public GeneRuntime Runtime { get; private set; }
    /// <summary>资源已经写入当前场景实体后发出的结果，供场景通知订阅。</summary>
    public event Action<GenePulse> Pulse;
    /// <summary>最近一次实际周期伤害的施加角色编号，供场景死亡归因使用。</summary>
    public ulong LastDamageFirerId { get; private set; }
    /// <summary>魔神塔共享生命组的原属性增量出口；第二参数已结算承伤/治疗但尚未单体截断，处理成功返回true。</summary>
    internal Func<GenePulse, int, bool> SharedHpChange;

    /// <summary>绑定场景实体，并保留组件类别的稳定编号。</summary>
    /// <param name="owner">持有基因实例的服务器场景实体。</param>
    public GeneComponent(ROObject owner) : base(owner, EntityComponentType.Gene) { }

    /// <summary>从当前 Luban 配置为宿主建立运行容器。</summary>
    public override void Initialize()
    {
        Runtime = new GeneRuntime(LubanMgr.Instance.GetGeneInfos());
        Runtime.Pulse += ApplyPulse;
    }

    /// <summary>由实体固定帧推进寿命并通知周期结果。</summary>
    /// <param name="deltaMilliseconds">本次服务器逻辑帧经过的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        // 场景实体只结算在线资源，周期结果不写入角色裸属性存档。
        List<GenePulse> pulses = Runtime.Update(deltaMilliseconds);
        // 按本次生成结果逐条通知，订阅者在此时增删基因不会影响已完成的遍历。
        for (int i = 0; i < pulses.Count; i++)
        {
            ApplyPulse(pulses[i]);
        }
    }

    /// <summary>把基因资源结果写入实际怪物实体，普通恢复不能复活零血目标。</summary>
    /// <param name="pulse">包含施加角色、装备来源和资源类型的强类型结果。</param>
    private void ApplyPulse(GenePulse pulse)
    {
        if (Owner.CurrentHp > 0)
        {
            // 目标当前最终生命上限，百分比恢复与伤害都以它为基数。
            int maxHp = Math.Max(1, (int)Runtime.CalculateAttribute(GeneAttributeType.MaxHp, Owner.MaxHp));
            // 目标当前最终法力上限，怪物未配置时保持零。
            int maxMp = Math.Max(0, (int)Runtime.CalculateAttribute(GeneAttributeType.MaxMp, Owner.MaxMp));
            // 本次正向资源点数，后续按类型决定扣除还是恢复。
            int amount = pulse.ResolveAmount(maxHp, maxMp);
            if (pulse.EffectType == GeneEffectType.PeriodicDamage)
            {
                amount = Math.Max(0, (int)(amount * Runtime.CalculateAttribute(GeneAttributeType.DamageTaken, 100) / 100));
                if (SharedHpChange == null || !SharedHpChange(pulse, -amount)) Owner.CurrentHp = Math.Max(0, Owner.CurrentHp - amount);
                if (amount > 0) LastDamageFirerId = pulse.FirerId;
            }
            else if (pulse.EffectType == GeneEffectType.PeriodicMana || pulse.EffectType == GeneEffectType.InstantMana)
            {
                Owner.CurrentMp = Math.Min(maxMp, Owner.CurrentMp + amount);
            }
            else
            {
                amount = Math.Max(0, (int)(amount * Runtime.CalculateAttribute(GeneAttributeType.HealingReceived, 100) / 100));
                if (SharedHpChange == null || !SharedHpChange(pulse, amount)) Owner.CurrentHp = Math.Min(maxHp, Owner.CurrentHp + amount);
            }
        }
        Pulse?.Invoke(pulse);
    }

    /// <summary>实体离场时释放全部效果及订阅，然后解除宿主引用。</summary>
    public override void Shutdown()
    {
        Runtime.Pulse -= ApplyPulse;
        Runtime.Clear();
        Pulse = null;
        SharedHpChange = null;
        base.Shutdown();
    }
}
