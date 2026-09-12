using System;

/// <summary>客户端角色组件基类，统一角色绑定与运行时生命周期。</summary>
public abstract class RoleComponentBase
{
    /// <summary>组件所属客户端角色。</summary>
    protected ClientRole Owner { get; private set; }

    /// <summary>创建组件并绑定所属角色。</summary>
    protected RoleComponentBase(ClientRole owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException("owner");
        }
        Owner = owner;
    }

    /// <summary>初始化组件运行时状态。</summary>
    public virtual void Initialize()
    {
    }

    /// <summary>装载组件协议数据。</summary>
    public virtual void Load(object roleData)
    {
    }

    /// <summary>全部角色快照装载完成后的回调。</summary>
    public virtual void AfterLoad()
    {
    }

    /// <summary>角色加入客户端角色世界后的回调。</summary>
    public virtual void AfterAddedToWorld()
    {
    }

    /// <summary>推进组件客户端逻辑。</summary>
    public virtual void Update(int deltaMilliseconds)
    {
    }

    /// <summary>释放组件资源并解除角色引用。</summary>
    public virtual void Dispose()
    {
        Owner = null;
    }

    /// <summary>记录组件变化，统一交给角色级脏标记聚合器。</summary>
    protected void MarkDirty(ClientDirtyModule module, bool fullRefresh,
        int slotIndex, ClientChangeContext context)
    {
        if (Owner == null || Owner.DirtyTracker == null)
        {
            return;
        }
        Owner.DirtyTracker.Mark(module, fullRefresh, slotIndex, context);
    }
}
