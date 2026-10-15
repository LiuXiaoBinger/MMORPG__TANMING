using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 场景对象层，负责逻辑 Transform 快照及 Unity 表现控制器关联。
/// 表现对象由 MEntity 直属 MModel 持有，模型内部统一管理 MAnimator。
/// 继承自 MObject，在组件和事件基础上增加位置、朝向等场景变换数据。
/// </summary>
public abstract class MSceneObject : MObject
{
    /// <summary>逻辑变换快照（位置 X/Y/Z，朝向 Yaw）</summary>
    private readonly EntityTransformSnapshot _transform = new EntityTransformSnapshot();

    /// <summary>关联的 Unity 表现控制器，负责具体的模型加载和动画驱动</summary>
    private RoleCtrlBase _entityCtrl;

    /// <summary>实体直属模型对象，模型内部持有 MAnimator。</summary>
    protected MModel _model;

    #region 属性变量

    /// <summary>渲染层级（默认 0），用于控制模型渲染层级</summary>
    protected int _layer = 0;

    /// <summary>整体缩放系数（初始化时应用到模型），用于根据配置调整实体大小</summary>
    protected float _scale = 1;

    /// <summary>是否为过场动画（CutScene）中的临时对象</summary>
    protected bool _isCutScene = false;

    /// <summary>默认渲染层级</summary>
    public int DefaultLayer { get { return _layer; } }

    /// <summary>逻辑 Transform 快照（位置、朝向）</summary>
    public override EntityTransformSnapshot Transform { get { return _transform; } }

    public abstract Vector3 Position { get; set; }

    /// <summary>移动速度（跑步速度，供移动组件/动画使用）</summary>
    public virtual float RunSpeed { get; set; }

    #endregion

    /// <summary>
    /// 场景对象构造函数
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="entityType">实体类型枚举</param>
    /// <param name="configId">配置表 ID</param>
    protected MSceneObject(ulong uid, EntityType entityType, int configId)
        : base(uid, entityType, configId)
    {
    }

    /// <summary>实体每帧驱动入口：先推进基类逻辑，再驱动挂载的模型 Animator 与声音等表现。</summary>
    /// <param name="fDeltaT">距上一帧的毫秒数。</param>
    public override void Update(int fDeltaT)
    {
        base.Update(fDeltaT);

        if (_model != null)
        {
            _model.Update(fDeltaT);
        }
    }

    /// <summary>
    /// 关联 Unity 表现控制器。仅在首次绑定时设置，防止重复覆盖。
    /// </summary>
    /// <param name="ctrl">表现控制器实例</param>
    public override void SetEntityCtrl(RoleCtrlBase ctrl)
    {
        if (_entityCtrl == null)
        {
            _entityCtrl = ctrl;
        }
    }

    /// <summary>获取关联的 Unity 表现控制器</summary>
    /// <returns>表现控制器引用，未关联时为 null</returns>
    public override RoleCtrlBase GetEntityCtrl()
    {
        return _entityCtrl;
    }

    /// <summary>
    /// 销毁时清理表现控制器引用，防止悬空引用
    /// </summary>
    protected override void OnDestroying()
    {
        _entityCtrl = null;
        base.OnDestroying();
    }

    /// <summary>
    /// 对象池复用时重置变换数据和表现字段，恢复初始默认值。
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        // 重置位置和朝向上到原点
        _transform.X = 0f;
        _transform.Y = 0f;
        _transform.Z = 0f;
        _transform.Yaw = 0f;
        // 清空表现关联和默认值
        _entityCtrl = null;
        _layer = 0;
        _scale = 1f;
        _isCutScene = false;
        base.PrepareForReuse(uid, entityType, configId);
    }
}
