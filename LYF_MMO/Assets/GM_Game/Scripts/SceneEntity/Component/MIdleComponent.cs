using cfg;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 角色待机动画装配组件。
/// 仅为普通角色的 Animator 预先绑定表现表中的普通待机和战斗待机剪辑，
/// 不参与动作状态切换；载具表现和非角色实体不会进入本组件的装配流程。
/// </summary>
public sealed class MIdleComponent : EntityComponentBase
{
    /// <summary>本组件对应的表现组件分类，用于实体容器按类型管理和对象池回收。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.View; }
    }

    /// <summary>两条待机剪辑是否均已提交给 Animator 异步准备，成功后无需重复装配。</summary>
    private bool _isPrepared;

    /// <summary>本轮实体生命周期内是否已记录缺失表现表或动画索引的错误，避免逐帧重复输出。</summary>
    private bool _hasLoggedPrepareFailure;

    /// <summary>
    /// 创建角色待机动画装配组件。
    /// </summary>
    /// <param name="owner">组件所属的场景角色实体。</param>
    public MIdleComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>
    /// 等待角色模型 Animator 就绪后装配两条 RO 风格待机动画。
    /// </summary>
    /// <param name="deltaMilliseconds">当前逻辑帧间隔，单位为毫秒；本组件只检测就绪状态，不使用该数值。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (_isPrepared)
        {
            return;
        }

        TryPrepareIdleAnimations();
    }

    /// <summary>
    /// 初始化时尝试装配待机动画。
    /// 角色模型通常在异步表现创建后才拥有 Animator，因此未就绪时由 Update 继续等待。
    /// </summary>
    public override void Initialize()
    {
        TryPrepareIdleAnimations();
    }

    /// <summary>
    /// 对象池回收前清理本次角色的装配状态，确保下一名角色会重新读取自己的表现表。
    /// </summary>
    public override void Shutdown()
    {
        _isPrepared = false;
        _hasLoggedPrepareFailure = false;
        base.Shutdown();
    }

    /// <summary>
    /// 读取角色表现表并分别将 IdleAnim、IdleFAnim 绑定到 Idle、FIdle 状态槽。
    /// 只有两个状态都成功登记后才标记完成，并请求状态机刷新当前默认待机动作。
    /// </summary>
    private void TryPrepareIdleAnimations()
    {
        if (Entity == null || !Entity.IsRole)
        {
            return;
        }

        // 骑乘期间由载具自身控制表现，禁止覆写角色 Animator 的待机槽。
        if (Entity.IsRideVehicle || Entity.IsRidePubVehicle)
        {
            return;
        }

        if (Entity.Model == null || Entity.Model.Ator == null ||
            Entity.AttrComp == null)
        {
            return;
        }

        // 当前职业对应的外观表现配置，提供普通待机和战斗待机的动画资源名。
        PresentTable presentData = Entity.AttrComp.PresentDataView;
        if (presentData == null || string.IsNullOrEmpty(presentData.IdleAnim) ||
            string.IsNullOrEmpty(presentData.IdleFAnim))
        {
            LogPrepareFailure("角色表现表缺少 IdleAnim 或 IdleFAnim 配置。");
            return;
        }

        // 普通静止状态使用表现表的普通待机动画。
        MAnimator.MAnimInfo idleAnimInfo = Entity.OverrideAnim(
            MAnimator.STATE_IDLE, presentData.IdleAnim);
        // 战斗静止状态使用表现表的向前待机动画。
        MAnimator.MAnimInfo idleFAnimInfo = Entity.OverrideAnim(
            MAnimator.STATE_FIDLE, presentData.IdleFAnim);
        if (idleAnimInfo == null || idleFAnimInfo == null)
        {
            LogPrepareFailure("角色待机动画未登记到 animInfo.txt。");
            return;
        }

        _isPrepared = true;
        _hasLoggedPrepareFailure = false;
        if (Entity.Machine != null)
        {
            // 模型在状态机初始化之后才创建时，主动刷新一次默认状态以开始播放新绑定的待机动画。
            Entity.Machine.MarkTriggered();
        }
    }

    /// <summary>
    /// 输出一次待机动画装配失败日志。
    /// </summary>
    /// <param name="failureReason">本次装配失败的具体原因，用于定位配置或动画索引问题。</param>
    private void LogPrepareFailure(string failureReason)
    {
        if (_hasLoggedPrepareFailure)
        {
            return;
        }

        _hasLoggedPrepareFailure = true;
        UnityEngine.Debug.LogError("角色待机动画装配失败，实体UID=" +
            Entity.UID + "，原因=" + failureReason);
    }
}
