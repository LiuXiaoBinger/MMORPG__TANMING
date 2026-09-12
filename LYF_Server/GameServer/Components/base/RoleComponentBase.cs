using System;

/// <summary>角色组件公共基类，统一角色绑定与持久化生命周期。</summary>
public abstract class RoleComponentBase
{
    /// <summary>组件所属角色，仅子类可访问。</summary>
    protected OnlineRole Owner { get; private set; }
    private Switch modified_ =new Switch();
    /// <summary>创建组件并绑定角色。</summary>
    protected RoleComponentBase(OnlineRole owner)
    {
        if (owner == null) throw new ArgumentNullException("owner");
        Owner = owner;
    }
    /// <summary>组件初始化。</summary>
    public virtual void Initialize() { }
    /// <summary>返回组件模块编号。</summary>
    public virtual RoleModuleType GetModuleId() { return RoleModuleType.kRoleModuleTypeNone; }
    /// <summary>判断组件是否有未确认脏数据。</summary>
    public virtual bool NeedSave() { return false; }
    /// <summary>返回所属角色当前的持久化版本，版本唯一由 OnlineRole 管理。</summary>
    public long GetSaveVersion()
    {
        if (Owner == null)
        {
            return 0L;
        }
        return Owner.GetSaveVersion();
    }
    /// <summary>将组件快照写入角色保存请求。</summary>
    public virtual bool Save(RoleDataFieldManager fieldManager, long saveVersion) { return false; }
    /// <summary>保存成功后通知组件确认版本。</summary>
    public virtual void OnSaveAcknowledged(long saveVersion) { }
    /// <summary>装载角色登录数据。</summary>
    public virtual void Load(object roleData) { }
    /// <summary>角色数据加载完成。</summary>
    public virtual void AfterLoad() { }
    /// <summary>角色加入在线列表。</summary>
    public virtual void AfterAddedToRole() { }
    /// <summary>固定帧更新。</summary>
    public virtual void Update(int deltaMilliseconds) { }
    /// <summary>释放组件资源。</summary>
    public virtual void Dispose() { Owner = null; }

    public void SetModify()
    {
        modified_.Set();
        Owner.SetModifyTime(GetModuleId());
    }
}
