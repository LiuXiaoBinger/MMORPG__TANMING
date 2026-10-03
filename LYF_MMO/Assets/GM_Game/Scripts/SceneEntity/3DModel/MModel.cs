using System;
using UnityEngine;

/// <summary>
/// RO 风格模型对象，直接由 MEntity 持有并管理 Animator 生命周期。
/// 负责模型预制体的异步加载、GameObject 封装、Animator 初始化和资源回收。
/// 使用会话版本号（_sessionVersion）校验异步回调，防止对象池复用后旧回调串台。
/// </summary>
public sealed class MModel
{
    /// <summary>内部封装的 Unity GameObject 包装器，负责变换和层级管理。</summary>
    private readonly MGameObject _mObj = new MGameObject();
    /// <summary>Animator 封装实例，从 MAnimatorPool 获取。</summary>
    private MAnimator _mAtor;
    /// <summary>模型加载和动画控制器的原始配置数据，用于复用时恢复参数。</summary>
    private MModelOriginData _originData = new MModelOriginData();
    /// <summary>当前加载的模型资源 Location。</summary>
    private string _modelLocation = string.Empty;
    /// <summary>会话版本号，每次加载或重置时递增，用于校验异步回调是否属于当前会话。</summary>
    private int _sessionVersion;
    /// <summary>是否正在异步加载中。</summary>
    private bool _isLoading;
    /// <summary>是否已加载完成。</summary>
    private bool _isLoaded;
    /// <summary>模型是否可见，未加载完成时不激活。</summary>
    private bool _visible = true;

    /// <summary>获取内部 GameObject 包装器。</summary>
    public MGameObject GameObject { get { return _mObj; } }
    /// <summary>获取 Animator 封装实例，动作状态组件通过此访问动画播放接口。</summary>
    public MAnimator Ator { get { return _mAtor; } }
    /// <summary>模型根对象上的碰撞控制器，由移动组件请求位移时负责碰撞约束。</summary>
    private CharacterController _characterController;
    /// <summary>获取模型原始配置数据。</summary>
    public MModelOriginData OriginData { get { return _originData; } }
    /// <summary>获取模型是否已加载完成。</summary>
    public bool IsLoaded { get { return _isLoaded; } }
    /// <summary>获取模型是否正在加载中。</summary>
    public bool IsLoading { get { return _isLoading; } }
    /// <summary>获取当前模型资源 Location。</summary>
    public string ModelLocation { get { return _modelLocation; } }
    /// <summary>模型可见性，设置时同时控制 GameObject 激活状态（需已加载）。</summary>
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; _mObj.IsActive = value && _isLoaded; }
    }
    /// <summary>模型世界坐标位置。</summary>
    public Vector3 Position
    {
        get { return _mObj.Position; }
        set { _mObj.Position = value; }
    }
    /// <summary>模型世界旋转。</summary>
    public Quaternion Rotation
    {
        get { return _mObj.Rotation; }
        set { _mObj.Rotation = value; }
    }
    /// <summary>模型缩放。</summary>
    public Vector3 Scale
    {
        get { return _mObj.Scale; }
        set { _mObj.Scale = value; }
    }

    //当前速度
    public Vector3 velocity { get; set; }
    //2d速度
    public Vector3 lastVelocity
    {
        get { return new Vector3(velocity.x, 0, velocity.z); }
        set { velocity = new Vector3(value.x, velocity.y, value.z); }
    }
    //垂直速度
    public Vector3 verticalVelocity
    {
        get { return new Vector3(0,velocity.y,0); }
        set { velocity = new Vector3(velocity.x, value.y, velocity.z); }
    }

    public Vector3 Forward { get { return _mObj.Forward; } set { _mObj.Forward = value; } }

    /// <summary>
    /// 异步加载模型预制体。
    /// 回调通过会话号校验，避免对象池复用时旧回调操作新模型。
    /// </summary>
    /// <param name="location">模型预制体资源 Location。</param>
    /// <param name="controllerLocation">动画控制器资源路径，为空时使用预制体自带 Controller。</param>
    /// <param name="useObjectAnimator">是否使用简化版 MObjectAnimator。</param>
    /// <param name="callback">加载完成回调，参数为 this。</param>
    public void Load(string location, string controllerLocation,
        bool useObjectAnimator, Action<MModel> callback)
    {
        Reset();
        _sessionVersion++;
        int callbackSession = _sessionVersion;
        _modelLocation = location;
        _originData.ModelLocation = location;
        _originData.ControllerLocation = controllerLocation;
        _originData.UseObjectAnimator = useObjectAnimator;
        _isLoading = true;
        if (string.IsNullOrEmpty(location))
        {
            FinishLoad(null, callbackSession, callback);
            return;
        }
        ResourceMgr.Instance.LoadPrefabAsync(location, gameObject =>
        {
            // 会话号不匹配说明已被回收或重新加载，销毁临时实例并丢弃回调
            if (callbackSession != _sessionVersion)
            {
                if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
                return;
            }
            FinishLoad(gameObject, callbackSession, callback);
        });
    }

    /// <summary>
    /// 绑定外部已实例化 GameObject，适合场景预制体和测试资源。
    /// 不会触发异步加载，直接完成初始化。
    /// </summary>
    /// <param name="gameObject">外部已实例化的 GameObject。</param>
    /// <param name="controllerLocation">动画控制器资源路径。</param>
    /// <param name="useObjectAnimator">是否使用简化版 MObjectAnimator。</param>
    public void SetGameObject(GameObject gameObject, string controllerLocation,
        bool useObjectAnimator)
    {
        Reset();
        _sessionVersion++;
        _mObj.CaptureNativeTransform(gameObject);
        _originData.ControllerLocation = controllerLocation;
        _originData.UseObjectAnimator = useObjectAnimator;
        FinishLoad(gameObject, _sessionVersion, null);
    }

    /// <summary>设置模型的父级节点。</summary>
    /// <param name="parent">父级 GameObject，为 null 时挂到场景根。</param>
    public void SetParent(GameObject parent)
    {
        _mObj.SetParent(parent);
    }

    /// <summary>
    /// 逐帧更新驱动，转发到 Animator 封装的 Update。
    /// </summary>
    /// <param name="deltaSeconds">距上一帧时间（秒）。</param>
    public void Update(float deltaSeconds)
    {
        if (_mAtor != null)
        {
            _mAtor.Update(deltaSeconds);
        }

        //Move();
    }

    public void Move()
    {
        if (_characterController != null && velocity.magnitude != 0)
        {
            _characterController.Move(velocity);
        }
    }

    /// <summary>查询 Unity 碰撞控制器上一帧记录的接地结果。</summary>
    public bool IsGrounded
    {
        get
        {
            return _characterController != null && _characterController.isGrounded;
        }
    }

    /// <summary>模型是否已经加载并挂载 CharacterController。</summary>
    public bool HasCharacterController
    {
        get
        {
            return _characterController != null && _characterController.enabled;
        }
    }

    /// <summary>使用 CharacterController 应用本帧位移；模型未加载碰撞器时直接移动 Transform。</summary>
    /// <param name="movement">本帧世界空间位移，单位为米。</param>
    public void MoveCharacter(Vector3 movement)
    {
        if (_characterController != null && _characterController.enabled)
        {
            _characterController.Move(movement);
            return;
        }

        Position += movement;
    }

    /// <summary>帧末更新钩子，预留用于后续动画状态收尾。</summary>
    /// <param name="deltaSeconds">距上一帧时间（秒）。</param>
    public void LateUpdate(float deltaSeconds)
    {
    }

    /// <summary>
    /// 回收 Animator 和 GameObject，异步回调使用的会话号同时失效。
    /// 对象池复用时调用，确保旧资源被正确释放。
    /// </summary>
    public void Reset()
    {
        _sessionVersion++;
        _isLoading = false;
        _isLoaded = false;
        if (_mAtor != null)
        {
            MAnimatorPool.Instance.Release(_mAtor);
        }
        _mAtor = null;
        _mObj.Release(true);
        _modelLocation = string.Empty;
        _originData.Reset();
        _visible = true;
    }

    /// <summary>
    /// 加载完成后的内部收尾：绑定 GameObject、初始化 Animator、触发回调。
    /// 再次校验会话号，确保异步加载期间没有发生过 Reset。
    /// </summary>
    /// <param name="gameObject">实例化的 GameObject，可能为 null。</param>
    /// <param name="callbackSession">发起加载时的会话号。</param>
    /// <param name="callback">外部完成回调。</param>
    private void FinishLoad(GameObject gameObject, int callbackSession,
        Action<MModel> callback)
    {
        if (callbackSession != _sessionVersion)
        {
            if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
            return;
        }
        _isLoading = false;
        if (gameObject == null)
        {
            _isLoaded = false;
            if (callback != null) callback(this);
            return;
        }
        _mObj.SetGameObject(gameObject);
        Animator animator = gameObject.GetComponentInChildren<Animator>(true);
        _mAtor = MAnimatorPool.Instance.Acquire(_originData.UseObjectAnimator);
        _mAtor.Init(this, _mObj, animator, _originData.ControllerLocation);
        _isLoaded = true;
        _mObj.IsActive = _visible;
        _characterController = gameObject.GetComponent<CharacterController>();
        if (callback != null) callback(this);
    }
}
