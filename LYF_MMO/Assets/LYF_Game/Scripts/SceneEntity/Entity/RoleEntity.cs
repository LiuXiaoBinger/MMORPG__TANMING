using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 保存角色基础同步信息的场景实体，也是本地玩家 PlayerEntity 的基类。
/// 直接创建时表示其他角色；本地玩家通过派生类补充输入和本地业务组件。
/// </summary>
public class RoleEntity : MEntity
{
    /// <summary>本次进入视野后、模型绑定前收到的最后一条位姿；离场或应用后清除。</summary>
    private SyncMoveNtf _pendingVisibleMove;

    /// <summary>接收本次入场的远端移动；模型未就绪时仅保留最新一条，已就绪时交给移动组件。</summary>
    /// <param name="notice">此角色的 11181 消息，含世界米坐标、水平角度和当前移动、跳跃状态。</param>
    internal void ApplyVisibleMove(SyncMoveNtf notice)
    {
        if (!Model.IsLoaded)
        {
            _pendingVisibleMove = notice;
            return;
        }
        // 现有移动事件使用水平方向向量，协议提供绕世界 Y 轴的角度。
        Vector3 face = Quaternion.Euler(0f, notice.Face, 0f) * Vector3.forward;
        // 由实体事件池回收，正常移动和绑定首帧都沿用同一套追赶与跳跃表现。
        MMoveEventArgs move = MMoveEventArgs.Create(notice.IsMoving,
            new Vector3(notice.Dx, notice.Dy, notice.Dz), face, notice.IsMoved, notice.Time, false, notice.JumpCount);
        DispatchEvent(MEventType.MEvent_Move, move);
    }

    /// <summary>绑定预制体前，以加载期间最后收到的位姿落地，没有移动更新则使用入场资料。</summary>
    /// <param name="root">本次异步加载完成、即将交给模型系统的角色根节点。</param>
    internal void PlaceVisibleModel(Transform root)
    {
        if (_pendingVisibleMove != null)
        {
            root.SetPositionAndRotation(new Vector3(_pendingVisibleMove.Dx, _pendingVisibleMove.Dy, _pendingVisibleMove.Dz),
                Quaternion.Euler(0f, _pendingVisibleMove.Face, 0f));
            return;
        }
        // 基础资料中的 Pos 使用 x_y_z 格式，转换结果为世界米数。
        System.Numerics.Vector3 position = BaseInfo.Pos.ToVector3();
        root.position = new Vector3(position.X, position.Y, position.Z);
    }

    /// <summary>模型绑定后恢复当前移动姿势及基础资料已有的死亡状态，不重播加载期间的旧动作。</summary>
    internal void RestoreVisibleState()
    {
        if (_pendingVisibleMove != null)
        {
            ApplyVisibleMove(_pendingVisibleMove);
            _pendingVisibleMove = null;
        }
        if (IsDead && CurrentState != StateDefine.kStateDead)
        {
            DispatchEvent(MEventType.Death, null);
        }
    }

    /// <summary>离开视野或切换场景时丢弃未绑定的位姿，再沿实体原流程释放模型和组件。</summary>
    protected override void OnDestroying()
    {
        _pendingVisibleMove = null;
        base.OnDestroying();
    }

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
        // 组件已挂载时立即用权威基础信息加载四项资源；旧包由 ApplyAll 兜底不清空。
        if (AttrComp != null) SyncResourcesFromBaseInfo();
    }

    /// <summary>把当前权威基础信息的四项资源写入已挂载的属性组件。</summary>
    internal void SyncResourcesFromBaseInfo()
    {
        if (BaseInfo == null || AttrComp == null) return;
        AttrComp.ApplyAll(BaseInfo.MaxHp, BaseInfo.CurrHp, BaseInfo.MaxMp, BaseInfo.CurrMp,
            BaseInfo.MaxRage, BaseInfo.CurrRage, BaseInfo.MaxTrueQi, BaseInfo.CurrTrueQi);
    }

    /// <summary>应用本角色的完整权威属性和基因通知，节点显示不再次加减这些最终值。</summary>
    /// <param name="notification">由正式11162处理器直接解析的回包。</param>
    /// <returns>角色身份匹配且应用成功时返回 true。</returns>
    public bool ApplyAttributeUpdate(RoleAttributeUpdateNtf notification)
    {
        if (notification.BaseInfo == null || GetID() != notification.BaseInfo.RoleId) return false;
        // UpdateBaseInfo 已全量应用四项权威资源，这里只补基因同步，避免同帧重复应用。
        UpdateBaseInfo(notification.BaseInfo);
        Gene.ApplySynchronization(notification);
        return true;
    }

    /// <summary>接收怪物对本角色的权威伤害，更新角色资料、实体生命和动作状态。</summary>
    /// <param name="hit">携带剩余生命、上限和本次真实伤害的服务端结果。</param>
    public void ApplyMonsterHit(MonsterPlayerHitData hit)
    {
        if (BaseInfo == null) return;
        // 已应用的相同血量不重复触发受击或死亡。
        bool lostHealth = hit.Hp < BaseInfo.CurrHp;
        // 本地玩家的虚方法会同时更新 MainRoleInfo 与场景 BaseInfo 的独立副本。
        RoleBaseInfo updated = BaseInfo.Clone();
        updated.CurrHp = hit.Hp;
        updated.MaxHp = hit.MaxHp;
        UpdateBaseInfo(updated);
        if (AttrComp != null)
            AttrComp.Apply(hit.MaxHp, hit.Hp, updated.MaxMp, updated.CurrMp);
        // 已绑定的主角旧攻击入口也从控制器上的基础信息检查存活。
        RoleCtrlBase controller = GetEntityCtrl();
        if (controller != null) controller.InitCtrl(controller._roleType, BaseInfo);
        if (!lostHealth) return;
        if (hit.Hp == 0) DispatchEvent(MEventType.Death, null);
        else if (hit.Damage > 0) DispatchEvent(MEventType.DamageReceived, null);
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
