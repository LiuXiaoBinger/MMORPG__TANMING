using MMoRpgCommon.SceneEntity;

/// <summary>
/// 其他角色场景实体，角色账户数据由角色数据模型独立保存。
/// 继承自 MEntity，用于场景中除本地玩家外的其他玩家角色。
/// PlayerEntity 继承本类，额外承载本地角色业务组件。
/// </summary>
public class RoleEntity : MEntity
{
    /// <summary>
    /// 构造其他角色场景实体（默认 Role 类型）
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">配置表 ID</param>
    public RoleEntity(ulong uid, int configId)
        : this(uid, EntityType.Role, configId)
    {
    }

    /// <summary>
    /// 供玩家和其他角色派生类型保留正确实体类别。
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="entityType">具体角色类型（Role/Player 等）</param>
    /// <param name="configId">配置表 ID</param>
    protected RoleEntity(ulong uid, EntityType entityType, int configId)
        : base(uid, entityType, configId)
    {
    }

    /// <summary>
    /// 兼容角色同步消息创建其他角色实体。
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="baseInfo">角色基础同步信息</param>
    public RoleEntity(ulong uid, RoleBaseInfo baseInfo)
        : this(uid, 0)
    {
        UpdateBaseInfo(baseInfo);
    }

    /// <summary>角色基础同步信息（昵称、外观、等级等）</summary>
    public RoleBaseInfo BaseInfo { get; private set; }

    /// <summary>
    /// 获取角色 ID（来自 BaseInfo，未初始化时返回 0）
    /// </summary>
    /// <returns>角色持久化 ID</returns>
    public virtual int GetID()
    {
        if (BaseInfo == null)
        {
            return 0;
        }
        return BaseInfo.RoleId;
    }

    /// <summary>
    /// 更新角色基础同步信息（深拷贝存储，避免外部修改影响）
    /// </summary>
    /// <param name="baseInfo">新的角色基础信息，null 时清空</param>
    public virtual void UpdateBaseInfo(RoleBaseInfo baseInfo)
    {
        if (baseInfo == null)
        {
            BaseInfo = null;
            return;
        }
        // 深拷贝，防止外部引用被意外修改
        BaseInfo = baseInfo.Clone();
    }

    /// <summary>
    /// 对象池复用时清空角色基础信息
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        BaseInfo = null;
        base.PrepareForReuse(uid, entityType, configId);
    }
}
