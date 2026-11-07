using System;
using System.Collections.Generic;
using GameServer.SceneEntity;
using MMoRpgCommon.SceneEntity;

/// <summary>服务端场景实体的基因组件，管理实例及周期结果，不承担角色持久化或网络协议。</summary>
public class GeneComponent : RuntimeEntityComponent
{
    /// <summary>当前实体的独立基因容器；客户端与服务端使用同一份运行算法源码。</summary>
    public GeneRuntime Runtime { get; private set; }
    /// <summary>周期伤害或恢复结果，由接入的战斗业务订阅并结算。</summary>
    public event Action<GenePulse> Pulse;

    /// <summary>绑定场景实体，并保留组件类别的稳定编号。</summary>
    /// <param name="owner">持有基因实例的服务器场景实体。</param>
    public GeneComponent(ROObject owner) : base(owner, EntityComponentType.Gene) { }

    /// <summary>从当前 Luban 配置为宿主建立运行容器。</summary>
    public override void Initialize()
    {
        Runtime = new GeneRuntime(LubanMgr.Instance.GetGeneInfos());
    }

    /// <summary>由实体固定帧推进寿命并通知周期结果。</summary>
    /// <param name="deltaMilliseconds">本次服务器逻辑帧经过的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        // 计时结果不直接写数据库；具体伤害或治疗由战斗模块消费。
        List<GenePulse> pulses = Runtime.Update(deltaMilliseconds);
        // 按本次生成结果逐条通知，订阅者在此时增删基因不会影响已完成的遍历。
        for (int i = 0; i < pulses.Count; i++)
        {
            Pulse?.Invoke(pulses[i]);
        }
    }

    /// <summary>实体离场时释放全部效果及订阅，然后解除宿主引用。</summary>
    public override void Shutdown()
    {
        Runtime.Clear();
        Pulse = null;
        base.Shutdown();
    }
}
