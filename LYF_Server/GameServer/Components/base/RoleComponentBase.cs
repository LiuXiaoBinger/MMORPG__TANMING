using System;

/// <summary>角色组件公共基类，统一角色绑定与持久化生命周期。</summary>
public abstract class RoleComponentBase
{
    /// <summary>组件所属角色，仅子类可访问。</summary>
    protected OnlineRole Owner { get; private set; }

    // 组件自身的脏标记开关；历史移植字段，当前未被读取，仅 SetModify 写入。
    private Switch modified_ =new Switch();

    #region 构造与初始化
    /// <summary>创建组件并绑定角色。</summary>
    /// <param name="owner">拥有本组件的角色，释放时解除引用。</param>
    protected RoleComponentBase(OnlineRole owner)
    {
        if (owner == null) throw new ArgumentNullException("owner");
        Owner = owner;
    }

    /// <summary>组件初始化。</summary>
    public virtual void Initialize() { }

    /// <summary>固定帧更新。</summary>
    /// <param name="deltaMilliseconds">本帧经过的毫秒数，基类不使用。</param>
    public virtual void Update(int deltaMilliseconds) { }

    /// <summary>
    /// 固定帧后置更新：在同一逻辑帧内所有组件的 Update 之后执行。
    /// 对应 RO 实体 Update 之后单独驱动的 LateUpdate 阶段（如 RoleSingingComponent::LateUpdate）。
    /// </summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数，与同一帧 Update 收到的是同一跨度。</param>
    public virtual void LateUpdate(int deltaMilliseconds) { }

    /// <summary>释放组件资源。</summary>
    public virtual void Dispose() { Owner = null; }
    #endregion

    /// <summary>保存成功后通知组件确认版本。</summary>
    public virtual void OnSaveAcknowledged(long saveVersion) { }

    #region 查询与判定
    /// <summary>返回组件模块编号。</summary>
    public virtual RoleModuleType GetModuleId() { return RoleModuleType.kRoleModuleTypeNone; }

    /// <summary>返回所属角色当前的持久化版本，版本唯一由 OnlineRole 管理。</summary>
    public long GetSaveVersion()
    {
        if (Owner == null)
        {
            return 0L;
        }
        return Owner.GetSaveVersion();
    }
    #endregion

    #region 数据读写
    /// <summary>将组件快照写入角色保存请求。</summary>
    /// <param name="fieldManager">收集这批组件存档数据的请求构造器。</param>
    /// <param name="saveVersion">当前角色保存版本，确认时用于区分不同批次。</param>
    public virtual bool Save(RoleDataFieldManager fieldManager, long saveVersion) { return false; }

    /// <summary>旧版登录装载入口；具体组件决定如何解释数据，基类没有装载行为。</summary>
    public virtual void Load(object roleData) { }
    #endregion

    #region 核心逻辑
    /// <summary>判断组件是否有未确认脏数据。</summary>
    public virtual bool NeedSave() { return false; }

    /// <summary>角色数据加载完成。</summary>
    public virtual void AfterLoad() { }

    /// <summary>角色加入在线列表。</summary>
    public virtual void AfterAddedToRole() { }

    /// <summary>
    /// 标记本组件发生业务修改：置位自身脏开关并记录模块修改时间。
    /// </summary>
    public void SetModify()
    {
        modified_.Set();
        Owner.SetModifyTime(GetModuleId());
    }
    #endregion
}
