using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>客户端实体的基因组件；复用共享运行模块，具体技能与网络同步由业务接入。</summary>
public class MGeneComponent : EntityComponentBase
{
    /// <summary>当前宿主独享的实例容器，在组件初始化时创建、回收时清空。</summary>
    public GeneRuntime Runtime { get; private set; }
    /// <summary>周期结果通知；订阅者可接入表现或结算，组件不擅自修改服务器同步的生命值。</summary>
    public event Action<GenePulse> Pulse;
    /// <summary>沿用原组件编号 4，改用本项目 Gene 命名。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Gene; } }

    /// <summary>绑定宿主，实例数据在正式初始化时创建。</summary>
    /// <param name="owner">拥有此组件的场景实体。</param>
    public MGeneComponent(ROObject owner) : base(owner) { }

    /// <summary>连接共享节点工厂，建立当前宿主的独立运行容器。</summary>
    public override void Initialize()
    {
        Runtime = new GeneRuntime(LubanMgr.Instance.GetGeneFactory());
    }

    /// <summary>推进实例寿命，并通知当前帧产生的周期效果。</summary>
    /// <param name="deltaMilliseconds">本帧实际经过的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        // 先完成计时与到期移除，再让订阅方处理独立的强类型结果。
        List<GenePulse> pulses = Runtime.Update(deltaMilliseconds);
        // 每条结果只在生成它的逻辑帧通知一次。
        for (int i = 0; i < pulses.Count; i++)
        {
            Pulse?.Invoke(pulses[i]);
        }
    }

    /// <summary>对象池回收时移除效果并解绑旧宿主订阅，防止下次借出继承旧状态。</summary>
    public override void Shutdown()
    {
        Runtime.Clear();
        Pulse = null;
    }
}
