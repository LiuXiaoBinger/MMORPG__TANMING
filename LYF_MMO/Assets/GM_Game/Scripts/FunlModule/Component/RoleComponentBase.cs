using System;

/// <summary>角色业务组件的共同基类，保存所属角色，并提供初始化、更新和释放入口。</summary>
public abstract class RoleComponentBase
{
    /// <summary>组件所属客户端角色。</summary>
    protected PlayerEntity Owner { get; private set; }

    #region 构造与初始化
    /// <summary>把 owner 保存为所属角色；owner 为空时不能创建组件。</summary>
    protected RoleComponentBase(PlayerEntity owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException("owner");
        }
        Owner = owner;
    }

    /// <summary>按 deltaMilliseconds（距上帧的毫秒数）更新组件；基类不执行具体业务。</summary>
    public virtual void Update(int deltaMilliseconds)
    {
    }

    /// <summary>初始化组件运行时状态。</summary>
    public virtual void Initialize()
    {
    }

    /// <summary>释放组件资源并解除角色引用。</summary>
    public virtual void Dispose()
    {
        Owner = null;
    }
    #endregion

    #region 核心逻辑
    /// <summary>全部角色快照装载完成后的回调。</summary>
    public virtual void AfterLoad()
    {
    }

    /// <summary>角色加入客户端角色世界后的回调。</summary>
    public virtual void AfterAddedToWorld()
    {
    }

    /// <summary>将 module 的变化交给所属角色记录；fullRefresh 表示整页刷新，slotIndex 为变化格子，context 为来源。</summary>
    protected void MarkDirty(ClientDirtyModule module, bool fullRefresh,
        int slotIndex, ClientChangeContext context)
    {
        if (Owner == null || Owner.DirtyTracker == null)
        {
            return;
        }
        Owner.DirtyTracker.Mark(module, fullRefresh, slotIndex, context);
    }
    #endregion
}
