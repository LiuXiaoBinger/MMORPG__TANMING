using System.Collections.Generic;
using UnityEngine;

/// <summary>RO 风格 Animator 封装，统一承担播放、参数缓存和对象池重置。</summary>
public class MAnimator
{
    /// <summary>准备中剪辑尝试写入当前 Controller 后的处理结果。</summary>
    private enum PreparedClipApplyResult
    {
        /// <summary>Controller 尚未完成异步绑定，保留 preparing 引用等待重试。</summary>
        Pending = 0,

        /// <summary>真实剪辑已成功写入同名覆盖槽位，可以提升为 ready。</summary>
        Applied = 1,

        /// <summary>Controller 已就绪但缺少槽位或写入异常，必须释放 preparing。</summary>
        Failed = 2
    }


    // 普通待机
    /// <summary>待机动画状态名。</summary>
    public const string STATE_IDLE = "Idle";
    public const string STATE_FIDLE = "FIdle";       // 向前待机（Forward Idle）
    public const string STATE_CARRY_IDLE = "CarryIdle"; // 负重/搬运待机
    public const string STATE_SELFY_IDLE = "SelfyIdle"; // 自拍模式待机
    public const string STATE_SHOOT_IDLE = "ShootIdle"; // 持枪/射击姿态待机
    public const string STATE_COMBO_IDLE = "ComboIdle"; // 连击战斗待机

    /// <summary>战斗行走动画状态名。</summary>
    public const string STATE_ZD_MOVE_WALK = "ZdMoveWalk";

    /// <summary>战斗奔跑动画状态名。</summary>
    public const string STATE_ZD_MOVE_RUN = "ZdMoveRun";

    /// <summary>非战斗行走动画状态名。</summary>
    public const string STATE_NONZD_MOVE_WALK = "NonZdMoveWalk";

    /// <summary>非战斗奔跑动画状态名。</summary>
    public const string STATE_NONZD_MOVE_RUN = "NonZdMoveRun";

    /// <summary>出生/登场动画状态名。</summary>
    public const string STATE_BORN = "Born";
    /// <summary>死亡动画状态名。</summary>
    public const string STATE_DEAD = "Dead";
    /// <summary>眩晕动画状态名。</summary>
    public const string STATE_STUN = "Stun";
    /// <summary>受击动画状态名。</summary>
    public const string STATE_BEHIT = "BeHit";
    /// <summary>吟唱动画状态名。</summary>
    public const string STATE_SINGING = "Singing";
    /// <summary>技能动画状态名。</summary>
    public const string STATE_SKILL = "Skill";
    /// <summary>特殊动作动画状态名。</summary>
    public const string STATE_SPECIAL = "Special";

    /// <summary>跳跃起跳动画状态名；一段、二段和三段跳共用该剪辑。</summary>
    public const string STATE_JUMP_START = "Jump_Start";

    /// <summary>跳跃空中循环动画状态名；用于起跳完成后的空中阶段。</summary>
    public const string STATE_JUMP_LOOP = "Jump_Loop";

    /// <summary>跳跃落地动画状态名；落地事件触发后播放一次。</summary>
    public const string STATE_JUMP_END = "Jump_End";

    /// <summary>
    /// 单个逻辑动画状态的剪辑绑定信息。
    /// 对齐 RO 的两阶段资源状态：已就绪剪辑负责当前播放，准备中剪辑负责异步替换。
    /// </summary>
    public sealed class MAnimInfo
    {
        /// <summary>所属动画器，用于在剪辑加载完成后更新覆盖控制器和续播当前状态。</summary>
        private MAnimator _animatorOwner;

        /// <summary>AnimatorOverrideController 中需要替换的原始剪辑键。</summary>
        private string _overrideKey = string.Empty;

        /// <summary>逻辑播放状态键；混合树动画可与覆盖键不同。</summary>
        private string _stateKey = string.Empty;

        /// <summary>当前已完成加载并可用于播放的共享剪辑信息。</summary>
        private MAnimationMgr.ClipInfo _clipInfo;

        /// <summary>当前正在异步准备、尚未提升为可播状态的共享剪辑信息。</summary>
        private MAnimationMgr.ClipInfo _preparingClipInfo;

        /// <summary>稳定的加载完成回调实例，用于注册和取消同一个异步等待者。</summary>
        private readonly MAnimationMgr.LoadClipCallback _loadClipCallback;

        /// <summary>当前动画的归一化播放时间；循环动画保持在 [0,1)，非循环动画封顶为 1。</summary>
        private float _normalizedTime;

        /// <summary>创建空动画信息并固定加载回调实例，后续通过 Init 绑定所属动画器和状态键。</summary>
        public MAnimInfo()
        {
            _loadClipCallback = OnLoadClipCompleted;
        }

        /// <summary>逻辑播放状态键；混合树时为树状态名，否则与覆盖键一致。</summary>
        public string StateKey
        {
            get { return _stateKey; }
        }

        /// <summary>AnimatorOverrideController 中被替换的原始剪辑键。</summary>
        public string OverrideKey
        {
            get { return _overrideKey; }
        }

        /// <summary>仅返回已经完成加载的可播剪辑；准备中的剪辑不会提前暴露。</summary>
        public AnimationClip Clip
        {
            get
            {
                if (_clipInfo != null)
                {
                    return _clipInfo.Clip;
                }
                return null;
            }
        }

        /// <summary>目标剪辑路径；切换期间优先返回正在准备的新剪辑路径。</summary>
        public string ClipPath
        {
            get
            {
                if (_preparingClipInfo != null)
                {
                    return _preparingClipInfo.ClipPath;
                }
                if (_clipInfo != null)
                {
                    return _clipInfo.ClipPath;
                }
                return string.Empty;
            }
        }

        /// <summary>当前已安装剪辑的 Location；准备新剪辑期间仍指向正在播放的旧版本。</summary>
        internal string ReadyClipPath
        {
            get
            {
                if (_clipInfo != null)
                {
                    return _clipInfo.ClipPath;
                }
                return string.Empty;
            }
        }

        /// <summary>目标剪辑时长，单位为秒；切换期间优先返回正在准备的新剪辑元数据。</summary>
        public float Length
        {
            get
            {
                if (_preparingClipInfo != null)
                {
                    return _preparingClipInfo.Length;
                }
                if (_clipInfo != null)
                {
                    return _clipInfo.Length;
                }
                return 0f;
            }
        }

        /// <summary>目标剪辑是否循环；切换期间优先返回正在准备的新剪辑元数据。</summary>
        public bool IsLooping
        {
            get
            {
                if (_preparingClipInfo != null)
                {
                    return _preparingClipInfo.IsLooping;
                }
                if (_clipInfo != null)
                {
                    return _clipInfo.IsLooping;
                }
                return false;
            }
        }

        /// <summary>仅在存在已就绪剪辑且没有替换剪辑正在准备时返回 true。</summary>
        public bool IsClipReady
        {
            get { return _clipInfo != null && _preparingClipInfo == null; }
        }

        /// <summary>当前动画的归一化播放时间，循环动画自动折返，非循环动画封顶为 1。</summary>
        public float NormalizedTime
        {
            get { return _normalizedTime; }
            set
            {
                _normalizedTime = value;
                if (_normalizedTime < 1f)
                {
                    return;
                }
                if (IsLooping)
                {
                    _normalizedTime -= Mathf.Floor(_normalizedTime);
                }
                else
                {
                    _normalizedTime = 1f;
                }
            }
        }

        /// <summary>
        /// 绑定动画信息所属动画器及状态键。
        /// </summary>
        /// <param name="animatorOwner">负责应用剪辑覆盖的动画器实例，生命周期覆盖本动画信息的使用期。</param>
        /// <param name="overrideKey">AnimatorOverrideController 中的原始剪辑键。</param>
        /// <param name="stateKey">逻辑播放状态键，混合树时可与覆盖键不同。</param>
        internal void Init(MAnimator animatorOwner, string overrideKey,
            string stateKey)
        {
            _animatorOwner = animatorOwner;
            _overrideKey = overrideKey;
            _stateKey = stateKey;
        }

        /// <summary>
        /// 更新逻辑播放状态键；覆盖键复用但改挂其他混合树时由所属动画器同步调用。
        /// </summary>
        /// <param name="stateKey">新的逻辑状态键，普通状态等于覆盖键，混合树状态等于树名。</param>
        internal void UpdateStateKey(string stateKey)
        {
            _stateKey = stateKey;
        }

        /// <summary>
        /// 开始准备新的共享剪辑；先取消上一份准备中剪辑的回调和引用，再持有新剪辑。
        /// </summary>
        /// <param name="clipInfo">由 MAnimationMgr 管理且与目标 Location 唯一对应的共享剪辑信息。</param>
        internal void PrepairClip(MAnimationMgr.ClipInfo clipInfo)
        {
            if (_preparingClipInfo != null)
            {
                _preparingClipInfo.ReleaseClip(_loadClipCallback);
                _preparingClipInfo = null;
            }

            if (clipInfo == null)
            {
                if (_animatorOwner != null)
                {
                    _animatorOwner.OnClipPreparationFinished(_overrideKey);
                }
                return;
            }

            _preparingClipInfo = clipInfo;
            _preparingClipInfo.RetainClip(_loadClipCallback);
        }

        /// <summary>释放准备中和已就绪的全部剪辑引用，并取消尚未完成的加载回调。</summary>
        internal void ReleaseClip()
        {
            if (_preparingClipInfo != null)
            {
                _preparingClipInfo.ReleaseClip(_loadClipCallback);
                _preparingClipInfo = null;
            }

            if (_clipInfo != null)
            {
                _clipInfo.ReleaseClip(_loadClipCallback);
                _clipInfo = null;
            }
        }

        /// <summary>
        /// 处理共享剪辑加载结果；只接受当前准备对象，避免旧回调污染快速切换或复用后的状态。
        /// </summary>
        /// <param name="clipInfo">本次完成加载的共享剪辑信息；Clip 为空表示加载失败。</param>
        private void OnLoadClipCompleted(MAnimationMgr.ClipInfo clipInfo)
        {
            if (!ReferenceEquals(_preparingClipInfo, clipInfo))
            {
                return;
            }

            if (clipInfo == null || clipInfo.Clip == null)
            {
                if (_preparingClipInfo != null)
                {
                    _preparingClipInfo.ReleaseClip(_loadClipCallback);
                    _preparingClipInfo = null;
                }
                if (_animatorOwner != null)
                {
                    _animatorOwner.OnClipPreparationFinished(_overrideKey);
                }
                return;
            }

            TryPromotePreparingClip(clipInfo);
        }

        /// <summary>
        /// 在 Controller 同名槽位实际写入成功后提升准备中剪辑；
        /// Controller 未绑定时保留引用等待绑定回调重试。
        /// </summary>
        /// <param name="clipInfo">当前已完成资源加载、等待应用到 Controller 的共享剪辑信息。</param>
        private void TryPromotePreparingClip(MAnimationMgr.ClipInfo clipInfo)
        {
            if (!ReferenceEquals(_preparingClipInfo, clipInfo) ||
                clipInfo == null || clipInfo.Clip == null)
            {
                return;
            }

            // 本次真实剪辑写入 Controller 的结果，用于区分等待绑定和确定性槽位失败。
            PreparedClipApplyResult applyResult = PreparedClipApplyResult.Failed;
            if (_animatorOwner != null)
            {
                applyResult = _animatorOwner.TryApplyPreparedClip(this,
                    clipInfo.Clip);
            }
            if (applyResult == PreparedClipApplyResult.Pending)
            {
                return;
            }
            if (applyResult == PreparedClipApplyResult.Failed)
            {
                ReleasePreparingClip();
                if (_animatorOwner != null)
                {
                    _animatorOwner.OnClipPreparationFinished(_overrideKey);
                }
                return;
            }

            if (_clipInfo != null)
            {
                _clipInfo.ReleaseClip(_loadClipCallback);
            }
            _clipInfo = clipInfo;
            _preparingClipInfo = null;

            if (_animatorOwner != null)
            {
                _animatorOwner.OnPreparedClipPromoted(this);
                _animatorOwner.OnClipPreparationFinished(_overrideKey);
            }
        }

        /// <summary>Controller 完成异步绑定后重试已经加载完成但尚未应用的 preparing 剪辑。</summary>
        internal void RetryPreparingClip()
        {
            if (_preparingClipInfo == null || _preparingClipInfo.Clip == null)
            {
                return;
            }
            TryPromotePreparingClip(_preparingClipInfo);
        }

        /// <summary>释放准备中剪辑并保留旧 ready 剪辑，用于槽位失败或 Controller 加载失败收尾。</summary>
        internal void CancelPreparingClip()
        {
            if (_preparingClipInfo == null)
            {
                return;
            }
            ReleasePreparingClip();
            if (_animatorOwner != null)
            {
                _animatorOwner.OnClipPreparationFinished(_overrideKey);
            }
        }

        /// <summary>释放当前 preparing 共享引用并清空准备状态，不影响旧 ready 剪辑。</summary>
        private void ReleasePreparingClip()
        {
            if (_preparingClipInfo != null)
            {
                _preparingClipInfo.ReleaseClip(_loadClipCallback);
                _preparingClipInfo = null;
            }
        }

        /// <summary>释放资源引用并清空状态键，供动画器重置或对象复用前彻底清理。</summary>
        internal void Reset()
        {
            ReleaseClip();
            _normalizedTime = 0f;
            _animatorOwner = null;
            _overrideKey = string.Empty;
            _stateKey = string.Empty;
        }
    }

    /// <summary>逻辑动画 key 到动画信息的映射，记录每个状态对应的片段覆盖信息。</summary>
    private readonly Dictionary<string, MAnimInfo> _animInfos =
        new Dictionary<string, MAnimInfo>();

    /// <summary>混合树状态名到去重覆盖键列表的映射，用于查询首条信息和完整就绪门控。</summary>
    private readonly Dictionary<string, List<string>> _blendTreeDict =
        new Dictionary<string, List<string>>();

    /// <summary>关键常驻动画中仍在准备的覆盖键集合，成功、失败、取消或重置时移除。</summary>
    private readonly HashSet<string> _preLoadAnimInfos =
        new HashSet<string>();
    /// <summary>已经提示过缺少覆盖槽位的唯一键，避免异步重试期间重复刷屏。</summary>
    private readonly HashSet<string> _missingOverrideSlotWarnings =
        new HashSet<string>();
    /// <summary>Float 类型 Animator 参数缓存，用于对象池复用时重放。</summary>
    private readonly Dictionary<string, float> _floatParameters =
        new Dictionary<string, float>();
    /// <summary>Int 类型 Animator 参数缓存。</summary>
    private readonly Dictionary<string, int> _intParameters =
        new Dictionary<string, int>();
    /// <summary>Bool 类型 Animator 参数缓存。</summary>
    private readonly Dictionary<string, bool> _boolParameters =
        new Dictionary<string, bool>();
    /// <summary>持有本 Animator 的 MModel 引用。</summary>
    private MModel _model;
    /// <summary>宿主 GameObject 包装器。</summary>
    private MGameObject _host;
    /// <summary>Unity 原生 Animator 组件引用。</summary>
    private Animator _animator;
    /// <summary>当前绑定的 RuntimeAnimatorController。</summary>
    private RuntimeAnimatorController _controller;
    /// <summary>兜底 Controller：预制体自带的原始 Controller。</summary>
    private RuntimeAnimatorController _fallbackController;
    /// <summary>动画覆盖控制器，用于运行时替换动画片段。</summary>
    private AnimatorOverrideController _overrideController;
    /// <summary>Controller 资源租约，用于释放 YooAsset 句柄。</summary>
    private MAnimatorControllerAssetLease _controllerLease;
    /// <summary>当前 Controller 资源路径。</summary>
    private string _controllerLocation = string.Empty;
    /// <summary>当前播放的动画状态名。</summary>
    private string _currentState = string.Empty;
    /// <summary>等待 Skill 状态及过渡退出后归还的真实技能剪辑 Location，空值表示无待释放项。</summary>
    private string _pendingSkillReleaseLocation = string.Empty;
    /// <summary>新 Skill 剪辑准备期间暂存的旧 ready Location，失败时恢复延迟释放。</summary>
    private string _supersededSkillReleaseLocation = string.Empty;
    /// <summary>会话版本号，每次 Init 或 Reset 时递增，用于校验异步回调。</summary>
    private int _sessionVersion;

    /// <summary>获取是否已完成初始化（Animator 组件是否已绑定）。</summary>
    public bool IsInitialized { get { return _animator != null; } }
    /// <summary>获取运行时 Controller 是否已经绑定，未绑定时动画覆盖仍处于等待阶段。</summary>
    public bool IsControllerReady { get { return _controller != null; } }
    /// <summary>获取是否完全加载就绪（Animator 和宿主 GameObject 均有效）。</summary>
    public bool IsLoaded { get { return _animator != null && _host != null && _host.IsValid; } }
    /// <summary>获取宿主 GameObject 包装器。</summary>
    public MGameObject Host { get { return _host; } }
    /// <summary>获取 Unity 原生 Animator 组件。</summary>
    public Animator UnityAnimator { get { return _animator; } }
    /// <summary>获取当前播放的动画状态名。</summary>
    public string CurrentState { get { return _currentState; } }

    /// <summary>关键动画准备队列为空，且当前普通状态或混合树的全部剪辑都已就绪时返回 true。</summary>
    public bool IsPreloadFinished
    {
        get
        {
            if (_preLoadAnimInfos.Count > 0)
            {
                return false;
            }
            if (string.IsNullOrEmpty(_currentState))
            {
                return true;
            }
            return AreStateClipsReady(_currentState);
        }
    }
    /// <summary>动画播放速度倍率，未绑定 Animator 时返回 1。</summary>
    public float Speed
    {
        get { if (_animator != null) return _animator.speed; return 1f; }
        set { if (_animator != null) _animator.speed = value; }
    }
    /// <summary>Animator 组件启用状态。</summary>
    public bool Enabled
    {
        get { if (_animator != null) return _animator.enabled; return false; }
        set { if (_animator != null) _animator.enabled = value; }
    }
    /// <summary>动画剔除模式，控制相机不可见时是否仍更新动画。</summary>
    public AnimatorCullingMode CullingMode
    {
        get { if (_animator != null) return _animator.cullingMode; return AnimatorCullingMode.CullUpdateTransforms; }
        set { if (_animator != null) _animator.cullingMode = value; }
    }

    /// <summary>绑定模型、宿主 GameObject 和 Unity Animator。</summary>
    public virtual void Init(MModel model, MGameObject host, Animator animator,
        string controllerLocation)
    {
        ResetInternal(false);
        _sessionVersion++;
        _model = model;
        _host = host;
        _animator = animator;
        _controller = null;
        if (animator != null)
        {
            _fallbackController = animator.runtimeAnimatorController;
        }
        _controllerLocation = controllerLocation;
        if (_controllerLocation == null) _controllerLocation = string.Empty;
        if (_animator != null)
        {
            if (!string.IsNullOrEmpty(_controllerLocation))
            {
                int loadSession = _sessionVersion;
                MAnimatorControllerAssetProvider.Instance.LoadAsync(_controllerLocation,
                    lease => OnControllerLoaded(loadSession, lease));
            }
            else
            {
                _controller = _fallbackController;
                BindController(_controller);
            }
        }
    }

    /// <summary>只接受当前初始化会话的异步结果，旧实体回调统一释放租约。</summary>
    private void OnControllerLoaded(int loadSession, MAnimatorControllerAssetLease lease)
    {
        if (loadSession != _sessionVersion || _animator == null)
        {
            if (lease != null)
            {
                lease.Release();
            }
            return;
        }
        if (lease == null)
        {
            if (_fallbackController != null)
            {
                // YooAsset 清单暂时不可用时继续使用预制体自带 Controller，异步覆盖仍可重试。
                Debug.LogWarning("Animator Controller 加载失败，已回退预制体 Controller：" +
                    _controllerLocation);
                BindController(_fallbackController);
                return;
            }
            Debug.LogWarning("Animator Controller 加载失败且没有预制体兜底：" +
                _controllerLocation);
            CancelPreparingClips();
            return;
        }
        if (_controllerLease != null)
        {
            _controllerLease.Release();
        }
        _controllerLease = lease;
        BindController(lease.Controller);
    }

    /// <summary>绑定 Controller 并重放缓存参数及当前逻辑动作。</summary>
    private void BindController(RuntimeAnimatorController controller)
    {
        if (_animator == null)
        {
            return;
        }
        _controller = controller;
        _animator.runtimeAnimatorController = controller;
        if (_overrideController != null)
        {
            Object.Destroy(_overrideController);
            _overrideController = null;
        }
        ApplyOverrideClips();
        ApplyCachedParameters();
        _animator.Rebind();
        _animator.Update(0f);
        RetryPreparingClips();
        PlayCurrent();
    }

    /// <summary>
    /// 直接播放指定动画状态，无过渡。
    /// </summary>
    /// <param name="stateName">目标动画状态名。</param>
    /// <returns>true 表示播放成功。</returns>
    public virtual bool Play(string stateName)
    {
        return Play(stateName, 0, 0f);
    }

    /// <summary>
    /// 在指定层播放指定动画状态，支持归一化时间偏移。
    /// 优先尝试当前 Controller 的原生状态播放；只有检测到兼容参数协议时才使用参数驱动。
    /// </summary>
    /// <param name="stateName">目标动画状态名。</param>
    /// <param name="layer">动画层索引。</param>
    /// <param name="normalizedTime">归一化播放时间（0~1）。</param>
    /// <returns>true 表示播放成功。</returns>
    public virtual bool Play(string stateName, int layer, float normalizedTime)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            return false;
        }
        _currentState = stateName;
        if (!IsClipReady(stateName, normalizedTime))
        {
            // 剪辑异步准备期间先播放 Controller 自带的同名状态，避免保留上一段移动动画。
            // 真实剪辑完成后会由 OnPreparedClipPromoted 重新播放并覆盖该默认片段。
            if (_animator != null && TryPlayNativeState(stateName, layer, normalizedTime))
            {
                return true;
            }
            // 只有当前 Animator 明确声明兼容协议时才写入参数，不能把普通嵌套状态机当作旧混合树。
            if (MAnimatorStateCompatibility.Apply(_animator, stateName))
            {
                return true;
            }
            return false;
        }
        if (_animator == null)
        {
            return false;
        }
        if (TryPlayNativeState(stateName, layer, normalizedTime))
        {
            return true;
        }
        return MAnimatorStateCompatibility.Apply(_animator, stateName);
    }

    /// <summary>
    /// 按 RO 的秒制过渡到指定动画状态，同状态接段或过渡中切段直接播放。
    /// </summary>
    /// <param name="stateName">目标动画状态名。</param>
    /// <param name="transitionDuration">过渡时长（秒）。</param>
    /// <returns>true 表示过渡成功。</returns>
    public virtual bool CrossFade(string stateName, float transitionDuration)
    {
        return CrossFade(stateName, transitionDuration, 0, 0f);
    }

    /// <summary>
    /// 在指定层按 RO 切换动画，同状态或过渡中直接播放，其他情况按固定秒数融合。
    /// 先解析当前 Controller 的原生状态；不存在时沿用兼容参数驱动。
    /// </summary>
    /// <param name="stateName">目标动画状态名。</param>
    /// <param name="transitionDuration">过渡时长（秒）。</param>
    /// <param name="layer">动画层索引。</param>
    /// <param name="normalizedTime">目标剪辑的归一化起点，通常位于 0 到 1；固定时间融合时换算为秒。</param>
    /// <returns>true 表示过渡成功。</returns>
    public virtual bool CrossFade(string stateName, float transitionDuration,
        int layer, float normalizedTime)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            return false;
        }
        _currentState = stateName;
        // 保存异步加载完成后的续播起点；准备期间仍沿用当前已安装的原生剪辑。
        IsClipReady(stateName, normalizedTime);
        if (_animator == null)
        {
            return false;
        }

        // 当前控制器中的完整目标路径，包含本项目 Idle、Move、Jump 的嵌套状态机。
        string nativeStatePath = GetNativeStatePath(stateName, layer);
        if (string.IsNullOrEmpty(nativeStatePath))
        {
            return MAnimatorStateCompatibility.Apply(_animator, stateName);
        }

        // 当前层正在采样的状态，不能把共享 Skill 槽的新剪辑与它自身的旧进度融合。
        AnimatorStateInfo currentStateInfo = _animator.GetCurrentAnimatorStateInfo(layer);
        if (_animator.IsInTransition(layer) || currentStateInfo.IsName(nativeStatePath))
        {
            // RO 在连击复用同状态或打断过渡时直接接到新动作起点，不创建同槽自过渡。
            _animator.Play(nativeStatePath, layer, normalizedTime);
            return true;
        }

        // 固定时间 API 的起点单位是秒，保持现有公共接口的归一化参数语义。
        float fixedTimeOffset = 0f;
        if (normalizedTime != 0f)
        {
            // 目标状态绑定信息；准备中 Length 属于新资源，不能用于当前仍安装的旧剪辑。
            MAnimInfo targetAnimInfo = GetAnimInfo(stateName);
            // 当前实际安装的 ready 剪辑；尚无 ready 时使用控制器原始占位剪辑。
            AnimationClip targetClip = null;
            if (targetAnimInfo != null)
            {
                targetClip = targetAnimInfo.Clip;
            }
            if (targetClip == null)
            {
                targetClip = FindOriginalClip(stateName);
            }
            if (targetClip != null)
            {
                fixedTimeOffset = normalizedTime * targetClip.length;
            }
        }
        _animator.CrossFadeInFixedTime(nativeStatePath, transitionDuration, layer,
            fixedTimeOffset);
        return true;
    }

    /// <summary>按当前归一化进度续播已记录状态，用于 Controller 绑定或剪辑覆盖后恢复动画。</summary>
    public virtual void PlayCurrent()
    {
        if (string.IsNullOrEmpty(_currentState))
        {
            return;
        }

        // 当前状态的动画绑定信息，用于在替换剪辑后恢复替换前的播放进度。
        MAnimInfo currentAnimInfo = GetCurrentAnimInfo();
        // 当前状态续播使用的归一化时间；无绑定信息时从动画起点开始。
        float normalizedTime = 0f;
        if (currentAnimInfo != null)
        {
            normalizedTime = currentAnimInfo.NormalizedTime;
        }
        Play(_currentState, 0, normalizedTime);
    }

    /// <summary>
    /// 查询当前是否处于指定动画状态。
    /// </summary>
    /// <param name="stateName">要检查的动画状态名。</param>
    /// <returns>true 表示当前 Animator 正处于该状态。</returns>
    public virtual bool InState(string stateName)
    {
        if (_animator == null || string.IsNullOrEmpty(stateName)) return false;
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        string nativeStatePath = GetNativeStatePath(stateName, 0);
        if (!string.IsNullOrEmpty(nativeStatePath) && stateInfo.IsName(nativeStatePath))
        {
            return true;
        }
        return stateInfo.IsName(stateName);
    }

    /// <summary>
    /// 尝试播放原生 Animator 状态，状态不存在时返回 false。
    /// </summary>
    /// <param name="stateName">目标动画状态名。</param>
    /// <param name="layer">动画层索引。</param>
    /// <param name="normalizedTime">归一化播放时间。</param>
    /// <returns>true 表示原生状态存在并已播放。</returns>
    private bool TryPlayNativeState(string stateName, int layer, float normalizedTime)
    {
        if (!HasNativeState(stateName, layer))
        {
            return false;
        }
        // 嵌套 Jump 状态机需要使用完整路径，避免短名称无法解析到子状态机。
        string nativeStatePath = GetNativeStatePath(stateName, layer);
        if (string.IsNullOrEmpty(nativeStatePath))
        {
            return false;
        }
        _animator.Play(nativeStatePath, layer, normalizedTime);
        return true;
    }

    /// <summary>将已加载的动作片段重新应用到新绑定的 Controller。</summary>
    private void ApplyOverrideClips()
    {
        if (_animator == null || _controller == null || _animInfos.Count == 0)
        {
            return;
        }
        // 使用快照列表，避免应用覆盖期间的回调修改原字典枚举状态。
        List<MAnimInfo> infos = new List<MAnimInfo>(_animInfos.Values);
        for (int index = 0; index < infos.Count; index++)
        {
            // 当前待重新应用到新控制器的动画绑定信息。
            MAnimInfo info = infos[index];
            if (info == null || info.Clip == null)
            {
                continue;
            }
            if (_overrideController == null)
            {
                _overrideController = new AnimatorOverrideController(_controller);
                _animator.runtimeAnimatorController = _overrideController;
            }
            // 当前覆盖键在基础控制器中对应的原始剪辑。
            AnimationClip original = FindOriginalClip(info.OverrideKey);
            if (original != null)
            {
                _overrideController[original] = info.Clip;
            }
            else
            {
                // Controller 已绑定但缺少原始槽位时只提示一次，避免吞掉已就绪剪辑的配置错误。
                string warningKey = _controller.name + "|" + info.OverrideKey;
                if (!_missingOverrideSlotWarnings.Contains(warningKey))
                {
                    _missingOverrideSlotWarnings.Add(warningKey);
                    Debug.LogWarning("Animator Controller 缺少动画覆盖槽位，已就绪剪辑无法重新应用。" +
                        "Controller=" + _controller.name + "，槽位=" + info.OverrideKey);
                }
            }
        }
    }

    /// <summary>尝试把已加载剪辑写入当前 Controller 的同名覆盖槽位。</summary>
    /// <param name="animInfo">仍处于 preparing 阶段的动画绑定信息。</param>
    /// <param name="preparedClip">已经由共享 ClipInfo 完成加载的候选剪辑。</param>
    /// <returns>Controller 未绑定时返回 Pending，写入成功返回 Applied，确定性失败返回 Failed。</returns>
    private PreparedClipApplyResult TryApplyPreparedClip(MAnimInfo animInfo,
        AnimationClip preparedClip)
    {
        if (animInfo == null || preparedClip == null)
        {
            return PreparedClipApplyResult.Failed;
        }
        if (_animator == null || _controller == null)
        {
            return PreparedClipApplyResult.Pending;
        }

        // 已完成加载的目标剪辑在基础控制器中对应的原始剪辑。
        AnimationClip originalClip = FindOriginalClip(animInfo.OverrideKey);
        if (originalClip == null)
        {
            string warningKey = _controller.name + "|" + animInfo.OverrideKey;
            if (!_missingOverrideSlotWarnings.Contains(warningKey))
            {
                _missingOverrideSlotWarnings.Add(warningKey);
                Debug.LogWarning("Animator Controller 缺少动画覆盖槽位，剪辑保持旧版本。" +
                    "Controller=" + _controller.name + "，槽位=" + animInfo.OverrideKey);
            }
            return PreparedClipApplyResult.Failed;
        }

        if (_overrideController == null)
        {
            _overrideController = new AnimatorOverrideController(_controller);
            _animator.runtimeAnimatorController = _overrideController;
        }

        try
        {
            _overrideController[originalClip] = preparedClip;
        }
        // 当前异常只表示候选剪辑应用失败，旧 ready 剪辑及其共享引用必须继续保留。
        catch (System.Exception exception)
        {
            Debug.LogError("动画剪辑写入覆盖槽位失败，剪辑保持旧版本。槽位=" +
                animInfo.OverrideKey + "，" + exception);
            return PreparedClipApplyResult.Failed;
        }
        return PreparedClipApplyResult.Applied;
    }

    /// <summary>准备中剪辑成功提升为 ready 后，当前状态按保存的归一化进度续播。</summary>
    /// <param name="animInfo">已经完成 Controller 应用和两阶段提升的动画绑定信息。</param>
    private void OnPreparedClipPromoted(MAnimInfo animInfo)
    {
        if (_currentState == animInfo.StateKey)
        {
            PlayCurrent();
        }
    }

    /// <summary>Controller 绑定完成后重试全部已加载完成但仍等待槽位应用的剪辑。</summary>
    private void RetryPreparingClips()
    {
        // 动画信息快照保证同步提升和回调修改集合状态时遍历稳定。
        List<MAnimInfo> infos = new List<MAnimInfo>(_animInfos.Values);
        for (int index = 0; index < infos.Count; index++)
        {
            // 当前可能等待 Controller 的动画绑定信息。
            MAnimInfo info = infos[index];
            if (info != null)
            {
                info.RetryPreparingClip();
            }
        }
    }

    /// <summary>Controller 确定加载失败时释放全部 preparing 引用并完成关键预加载标记收尾。</summary>
    private void CancelPreparingClips()
    {
        // 动画信息快照保证释放回调期间原字典保持可遍历。
        List<MAnimInfo> infos = new List<MAnimInfo>(_animInfos.Values);
        for (int index = 0; index < infos.Count; index++)
        {
            // 当前需要取消 preparing 阶段的动画绑定信息。
            MAnimInfo info = infos[index];
            if (info != null)
            {
                info.CancelPreparingClip();
            }
        }
    }

    /// <summary>
    /// 检查 Animator 中是否存在指定名称的状态节点。
    /// 同时尝试短名、Base Layer 全路径和 Jump 子状态机全路径。
    /// </summary>
    /// <param name="stateName">动画状态名。</param>
    /// <param name="layer">动画层索引。</param>
    /// <returns>true 表示状态节点存在。</returns>
    private bool HasNativeState(string stateName, int layer)
    {
        string nativeStatePath = GetNativeStatePath(stateName, layer);
        return !string.IsNullOrEmpty(nativeStatePath);
    }

    /// <summary>
    /// 按当前 Controller 解析可播放的状态路径。
    /// MAnimator.controller 的待机、移动、跳跃状态均位于对应的嵌套子状态机，
    /// 必须使用完整路径，不能把子状态机名称误当成状态路径。
    /// </summary>
    /// <param name="stateName">逻辑状态名，例如 Jump_Start。</param>
    /// <param name="layer">Animator 层索引，通常为 0。</param>
    /// <returns>Unity 可识别的状态路径；不存在时返回空字符串。</returns>
    private string GetNativeStatePath(string stateName, int layer)
    {
        if (_animator == null || string.IsNullOrEmpty(stateName))
        {
            return string.Empty;
        }

        // 当前项目的正式 MAnimator Controller 使用 Idle 子状态机。
        string nestedStatePath = GetFormalNestedStatePath(stateName);
        if (!string.IsNullOrEmpty(nestedStatePath) &&
            _animator.HasState(layer, Animator.StringToHash(nestedStatePath)))
        {
            return nestedStatePath;
        }

        // 直接状态短名称兼容其他正式 Controller 中的普通状态。
        int shortHash = Animator.StringToHash(stateName);
        if (_animator.HasState(layer, shortHash))
        {
            return stateName;
        }

        // Base Layer 全路径兼容 Unity 对状态哈希的完整路径要求。
        string baseLayerPath = "Base Layer." + stateName;
        int fullHash = Animator.StringToHash(baseLayerPath);
        if (_animator.HasState(layer, fullHash))
        {
            return baseLayerPath;
        }

        // 跳跃状态位于独立 Jump 子状态机，使用三级全路径定位。
        string jumpStatePath = "Base Layer.Jump." + stateName;
        int jumpHash = Animator.StringToHash(jumpStatePath);
        if (_animator.HasState(layer, jumpHash))
        {
            return jumpStatePath;
        }

        return string.Empty;
    }

    /// <summary>
    /// 将逻辑状态映射到正式 MAnimator Controller 的嵌套状态路径。
    /// 当前控制器把 Idle、Move 和 Jump 分别作为子状态机，路径必须与资源层级一致。
    /// </summary>
    /// <param name="stateName">逻辑动画状态名。</param>
    /// <returns>正式控制器中的完整状态路径；未映射状态返回空字符串。</returns>
    private string GetFormalNestedStatePath(string stateName)
    {
        if (stateName == STATE_IDLE)
        {
            return "Base Layer.Idle.Idle";
        }
        if (stateName == STATE_FIDLE)
        {
            return "Base Layer.Idle.FIdle";
        }
        if (stateName == STATE_CARRY_IDLE)
        {
            return "Base Layer.Idle.CarryIdle";
        }
        if (stateName == STATE_SELFY_IDLE)
        {
            return "Base Layer.Idle.SelfyIdle";
        }
        if (stateName == STATE_SHOOT_IDLE)
        {
            return "Base Layer.Idle.ShootIdle";
        }
        if (stateName == STATE_ZD_MOVE_WALK)
        {
            return "Base Layer.Move.ZdMoveWalk";
        }
        if (stateName == STATE_ZD_MOVE_RUN)
        {
            return "Base Layer.Move.ZdMoveRun";
        }
        if (stateName == STATE_NONZD_MOVE_WALK)
        {
            return "Base Layer.Move.NonZdMoveWalk";
        }
        if (stateName == STATE_NONZD_MOVE_RUN)
        {
            return "Base Layer.Move.NonZdMoveRun";
        }
        if (stateName == STATE_JUMP_START)
        {
            return "Base Layer.Jump.Jump_Start";
        }
        if (stateName == STATE_JUMP_LOOP)
        {
            return "Base Layer.Jump.Jump_Loop";
        }
        if (stateName == STATE_JUMP_END)
        {
            return "Base Layer.Jump.Jump_End";
        }
        return string.Empty;
    }

    /// <summary>重新绑定 Animator 并应用缓存参数，用于 Controller 切换后恢复状态。</summary>
    public virtual void Rebind()
    {
        if (_animator == null) return;
        _animator.Rebind();
        _animator.Update(0f);
        ApplyCachedParameters();
    }

    /// <summary>
    /// 写入目标状态的归一化时间并检查全部剪辑是否就绪。
    /// 普通状态只检查自身；混合树状态要求登记的所有覆盖键都存在且完成准备。
    /// </summary>
    /// <param name="stateName">准备播放的普通状态名或混合树状态名。</param>
    /// <param name="normalizedTime">本次播放使用的归一化起点，通常位于 0 到 1。</param>
    /// <returns>目标状态全部剪辑均可播放时返回 true。</returns>
    private bool IsClipReady(string stateName, float normalizedTime)
    {
        // 混合树状态下参与门控的全部覆盖键，列表已在 OverrideAnim 中保持去重。
        List<string> blendAnimKeys;
        if (_blendTreeDict.TryGetValue(stateName, out blendAnimKeys))
        {
            if (blendAnimKeys.Count == 0)
            {
                return false;
            }

            // 全部混合树子剪辑的聚合就绪结果；即使某项未就绪也继续写入其他项的播放起点。
            bool areAllClipsReady = true;
            for (int index = 0; index < blendAnimKeys.Count; index++)
            {
                // 当前混合树子剪辑对应的覆盖键。
                string animKey = blendAnimKeys[index];
                // 当前混合树子剪辑的异步绑定信息。
                MAnimInfo blendAnimInfo;
                if (!_animInfos.TryGetValue(animKey, out blendAnimInfo) ||
                    blendAnimInfo == null)
                {
                    return false;
                }

                blendAnimInfo.NormalizedTime = normalizedTime;
                if (!blendAnimInfo.IsClipReady)
                {
                    areAllClipsReady = false;
                }
            }
            return areAllClipsReady;
        }

        // 普通状态对应的单条动画绑定信息。
        MAnimInfo animInfo;
        if (!_animInfos.TryGetValue(stateName, out animInfo) || animInfo == null)
        {
            return false;
        }

        animInfo.NormalizedTime = normalizedTime;
        return animInfo.IsClipReady;
    }

    /// <summary>只读检查普通状态或混合树的全部剪辑是否就绪，不改写任何归一化播放进度。</summary>
    /// <param name="stateName">需要检查的普通状态名或混合树状态名。</param>
    /// <returns>普通状态剪辑就绪，或混合树全部子剪辑就绪时返回 true。</returns>
    private bool AreStateClipsReady(string stateName)
    {
        // 混合树状态登记的全部覆盖键；存在该映射时必须逐项检查，不能以首项代表整棵树。
        List<string> blendAnimKeys;
        if (_blendTreeDict.TryGetValue(stateName, out blendAnimKeys))
        {
            if (blendAnimKeys.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < blendAnimKeys.Count; index++)
            {
                // 当前混合树子项对应的 AnimatorOverrideController 覆盖键。
                string animKey = blendAnimKeys[index];
                // 当前混合树子项的异步动画信息，用于判断真实剪辑是否已经完成准备。
                MAnimInfo blendAnimInfo;
                if (!_animInfos.TryGetValue(animKey, out blendAnimInfo) ||
                    blendAnimInfo == null || !blendAnimInfo.IsClipReady)
                {
                    return false;
                }
            }
            return true;
        }

        // 普通状态唯一对应的异步动画信息。
        MAnimInfo animInfo;
        if (!_animInfos.TryGetValue(stateName, out animInfo) || animInfo == null)
        {
            return false;
        }
        return animInfo.IsClipReady;
    }

    /// <summary>
    /// 获取当前动画状态的信息，包括归一化时间。
    /// 如果该状态尚未登记，自动创建并加入映射表。
    /// </summary>
    /// <returns>当前动画状态的嵌套动画信息对象。</returns>
    public virtual MAnimInfo GetCurrentAnimInfo()
    {
        if (string.IsNullOrEmpty(_currentState)) return null;
        // 当前逻辑状态对应的动画绑定信息。
        MAnimInfo info = GetAnimInfo(_currentState);
        if (info == null)
        {
            info = new MAnimInfo();
            info.Init(this, _currentState, _currentState);
            _animInfos.Add(_currentState, info);
        }
        if (_animator != null)
        {
            // Unity Animator 当前状态快照，用于续播时恢复归一化进度。
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(_currentState))
            {
                info.NormalizedTime = stateInfo.normalizedTime;
            }
        }
        return info;
    }

    /// <summary>
    /// 登记逻辑动画键并通过 MAnimationMgr 异步准备目标剪辑。
    /// 当前就绪剪辑会一直保留到新剪辑成功完成加载，避免切换期间播放空引用。
    /// </summary>
    /// <param name="key">AnimatorOverrideController 中需要替换的原始剪辑键。</param>
    /// <param name="clipLocation">已经由 MAnimationMgr 解析并登记的完整 YooAsset Location。</param>
    /// <param name="blendTreeKey">可选混合树状态键；为空时使用覆盖键作为播放状态键。</param>
    /// <returns>对应的嵌套动画信息；参数或路径无效时返回 null。</returns>
    public virtual MAnimInfo OverrideAnim(string key, string clipLocation,
        string blendTreeKey = null)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(clipLocation))
        {
            return null;
        }

        // 目标 Location 对应的共享剪辑信息，资源加载和引用计数只由 MAnimationMgr 管理。
        MAnimationMgr.ClipInfo clipInfo =
            MAnimationMgr.singleton.GetClipInfo(clipLocation);
        if (clipInfo == null)
        {
            Debug.LogError("动画剪辑未登记：" + clipLocation);
            return null;
        }

        if (key == STATE_SKILL)
        {
            // 新剪辑尚未成功安装时保留旧 ready 的释放目标，失败后仍可安全归还。
            MAnimInfo currentSkillInfo;
            if (!string.IsNullOrEmpty(_pendingSkillReleaseLocation) &&
                _animInfos.TryGetValue(STATE_SKILL, out currentSkillInfo) &&
                currentSkillInfo != null &&
                currentSkillInfo.ReadyClipPath == _pendingSkillReleaseLocation &&
                _pendingSkillReleaseLocation != clipLocation)
            {
                _supersededSkillReleaseLocation = _pendingSkillReleaseLocation;
            }
            if (_supersededSkillReleaseLocation == clipLocation)
            {
                // 同一旧剪辑被新施法重新采用，其旧释放请求不再属于待回收项。
                _supersededSkillReleaseLocation = string.Empty;
            }
            _pendingSkillReleaseLocation = string.Empty;
        }

        // 本次覆盖所属的逻辑播放状态键；普通状态等于覆盖键，混合树使用树状态名。
        string stateKey = key;
        if (!string.IsNullOrEmpty(blendTreeKey))
        {
            stateKey = blendTreeKey;
        }

        // 当前覆盖键对应的动画绑定信息，同一个键在后续切换时复用该状态对象。
        MAnimInfo info;
        if (!_animInfos.TryGetValue(key, out info))
        {
            info = new MAnimInfo();
            info.Init(this, key, stateKey);
            _animInfos.Add(key, info);
        }
        else if (info.StateKey != stateKey)
        {
            RemoveBlendTreeKey(info.StateKey, key);
            info.UpdateStateKey(stateKey);
        }
        // 登记混合树关系
        if (!string.IsNullOrEmpty(blendTreeKey))
        {
            RegisterBlendTreeKey(blendTreeKey, key);
        }

        // 剪辑路径发生变化时重新准备加载
        if (info.ClipPath != clipLocation)
        {
            if (NeedPreload(key))
            {
                _preLoadAnimInfos.Add(key);
            }
            info.PrepairClip(clipInfo);
        }

        return info;
    }

    /// <summary>
    /// 根据 key 获取已登记的动画信息。
    /// </summary>
    /// <param name="key">逻辑动画 key。</param>
    /// <returns>对应的嵌套动画信息，不存在返回 null。</returns>
    public virtual MAnimInfo GetAnimInfo(string key)
    {
        // 普通状态或直接覆盖键对应的动画绑定信息。
        MAnimInfo info;
        if (_animInfos.TryGetValue(key, out info))
        {
            return info;
        }

        // 混合树状态对应的去重覆盖键列表，查询语义与 RO 一致，返回首条子动画信息。
        List<string> blendAnimKeys;
        if (_blendTreeDict.TryGetValue(key, out blendAnimKeys) &&
            blendAnimKeys.Count > 0)
        {
            // 混合树第一条覆盖键，用于时长、循环和当前播放进度等代表性查询。
            string firstAnimKey = blendAnimKeys[0];
            if (_animInfos.TryGetValue(firstAnimKey, out info))
            {
                return info;
            }
        }
        return null;
    }

    /// <summary>
    /// 释放指定状态的真实剪辑覆盖及其异步等待者，恢复 Controller 原始占位剪辑。
    /// 只接受仍绑定预期 Location 的状态，避免旧技能结束回调清除新技能动画。
    /// </summary>
    /// <param name="key">需要释放的 Animator 覆盖槽位名。</param>
    /// <param name="expectedClipLocation">本次施法安装的完整剪辑 Location。</param>
    /// <returns>找到并释放对应剪辑绑定时返回 true。</returns>
    public bool ReleaseAnim(string key, string expectedClipLocation)
    {
        // 当前槽位的动画绑定，可能已经被下一次施法替换。
        MAnimInfo info;
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(expectedClipLocation) ||
            !_animInfos.TryGetValue(key, out info) || info == null ||
            info.ClipPath != expectedClipLocation)
        {
            return false;
        }

        // 原始占位剪辑在释放后重新交给 Controller，避免其仍引用已释放的资源。
        AnimationClip originalClip = FindOriginalClip(info.OverrideKey);
        if (_overrideController != null && originalClip != null)
        {
            _overrideController[originalClip] = originalClip;
        }
        // 释放前保存逻辑状态名，Reset 会清空动画信息的键。
        string stateKey = info.StateKey;
        RemoveBlendTreeKey(stateKey, key);
        info.Reset();
        _animInfos.Remove(key);
        _preLoadAnimInfos.Remove(key);
        if (_currentState == stateKey)
        {
            _currentState = string.Empty;
        }
        return true;
    }

    /// <summary>在 Skill 动画完全退出后归还真实剪辑，避免过渡期间切回占位动作。</summary>
    /// <param name="expectedClipLocation">本次施法占用的真实剪辑 Location，后续技能不会被旧请求释放。</param>
    public void ReleaseSkillAnimAfterExit(string expectedClipLocation)
    {
        if (string.IsNullOrEmpty(expectedClipLocation))
        {
            return;
        }
        // 只接受仍占用 Skill 槽位的施法，忽略上一轮动画晚到的结束请求。
        MAnimInfo skillInfo;
        if (!_animInfos.TryGetValue(STATE_SKILL, out skillInfo) ||
            skillInfo == null || skillInfo.ClipPath != expectedClipLocation)
        {
            return;
        }
        _pendingSkillReleaseLocation = expectedClipLocation;
        TryReleasePendingSkillAnim();
    }

    /// <summary>当前动画不再使用 Skill 槽位时，归还待释放剪辑及其资源租约。</summary>
    private void TryReleasePendingSkillAnim()
    {
        if (string.IsNullOrEmpty(_pendingSkillReleaseLocation))
        {
            return;
        }
        if (!string.IsNullOrEmpty(_supersededSkillReleaseLocation))
        {
            // 新剪辑仍在准备时不可清理同槽旧 ready，等待成功或失败回调决定归属。
            return;
        }
        if (_animator != null && _animator.enabled &&
            (InState(STATE_SKILL) || _animator.IsInTransition(0)))
        {
            return;
        }
        // 本次待释放的技能剪辑路径，先清除挂起状态以避免释放回调重复进入。
        string expectedClipLocation = _pendingSkillReleaseLocation;
        _pendingSkillReleaseLocation = string.Empty;
        ReleaseAnim(STATE_SKILL, expectedClipLocation);
    }

    /// <summary>将覆盖键登记到指定混合树，列表内已存在时不重复添加。</summary>
    /// <param name="blendTreeKey">Animator 中的混合树逻辑状态名。</param>
    /// <param name="animKey">参与混合树的 AnimatorOverrideController 覆盖键。</param>
    private void RegisterBlendTreeKey(string blendTreeKey, string animKey)
    {
        // 目标混合树当前登记的覆盖键列表。
        List<string> blendAnimKeys;
        if (!_blendTreeDict.TryGetValue(blendTreeKey, out blendAnimKeys))
        {
            blendAnimKeys = new List<string>();
            _blendTreeDict.Add(blendTreeKey, blendAnimKeys);
        }

        if (blendAnimKeys.IndexOf(animKey) < 0)
        {
            blendAnimKeys.Add(animKey);
        }
    }

    /// <summary>从旧混合树移除覆盖键；列表清空后同时删除无效的树映射。</summary>
    /// <param name="blendTreeKey">覆盖键此前所属的逻辑状态名；普通状态不会存在对应列表。</param>
    /// <param name="animKey">需要迁移或移除的覆盖键。</param>
    private void RemoveBlendTreeKey(string blendTreeKey, string animKey)
    {
        // 旧混合树登记的覆盖键列表，不存在表示该键此前属于普通状态。
        List<string> blendAnimKeys;
        if (!_blendTreeDict.TryGetValue(blendTreeKey, out blendAnimKeys))
        {
            return;
        }

        blendAnimKeys.Remove(animKey);
        if (blendAnimKeys.Count == 0)
        {
            _blendTreeDict.Remove(blendTreeKey);
        }
    }

    /// <summary>判断覆盖键是否属于必须跟踪准备完成状态的常驻待机或移动动画。</summary>
    /// <param name="animKey">AnimatorOverrideController 覆盖键。</param>
    /// <returns>常驻待机、移动或跳跃状态之一返回 true。</returns>
    private bool NeedPreload(string animKey)
    {
        return animKey == STATE_IDLE || animKey == STATE_FIDLE ||
            animKey == STATE_ZD_MOVE_WALK || animKey == STATE_ZD_MOVE_RUN ||
            animKey == STATE_NONZD_MOVE_WALK ||
            animKey == STATE_NONZD_MOVE_RUN ||
            animKey == STATE_JUMP_START || animKey == STATE_JUMP_LOOP ||
            animKey == STATE_JUMP_END;
    }

    /// <summary>关键动画加载成功或失败后移除其准备标记。</summary>
    /// <param name="animKey">已经结束本轮准备的 AnimatorOverrideController 覆盖键。</param>
    private void OnClipPreparationFinished(string animKey)
    {
        _preLoadAnimInfos.Remove(animKey);
        if (animKey != STATE_SKILL ||
            string.IsNullOrEmpty(_supersededSkillReleaseLocation))
        {
            return;
        }
        // 加载或安装失败会留下旧 ready；成功时新剪辑已接管槽位，旧请求直接失效。
        MAnimInfo skillInfo;
        if (_animInfos.TryGetValue(STATE_SKILL, out skillInfo) &&
            skillInfo != null &&
            skillInfo.ClipPath == _supersededSkillReleaseLocation)
        {
            _pendingSkillReleaseLocation = _supersededSkillReleaseLocation;
        }
        _supersededSkillReleaseLocation = string.Empty;
        TryReleasePendingSkillAnim();
    }

    /// <summary>
    /// 设置 Float 类型 Animator 参数，同时缓存到本地字典，对象池复用时重放。
    /// </summary>
    /// <param name="parameterName">参数名。</param>
    /// <param name="value">参数值。</param>
    public virtual void SetFloat(string parameterName, float value)
    {
        _floatParameters[parameterName] = value;
        if (_animator != null && _animator.isActiveAndEnabled) _animator.SetFloat(parameterName, value);
    }

    /// <summary>
    /// 获取缓存的 Float 参数值。
    /// </summary>
    /// <param name="parameterName">参数名。</param>
    /// <returns>缓存的参数值，不存在返回 0。</returns>
    public virtual float GetFloat(string parameterName)
    {
        float value;
        if (_floatParameters.TryGetValue(parameterName, out value)) return value;
        return 0f;
    }

    /// <summary>
    /// 设置 Int 类型 Animator 参数，同时缓存到本地字典。
    /// </summary>
    /// <param name="parameterName">参数名。</param>
    /// <param name="value">参数值。</param>
    public virtual void SetInteger(string parameterName, int value)
    {
        _intParameters[parameterName] = value;
        if (_animator != null && _animator.isActiveAndEnabled) _animator.SetInteger(parameterName, value);
    }

    /// <summary>
    /// 获取缓存的 Int 参数值。
    /// </summary>
    /// <param name="parameterName">参数名。</param>
    /// <returns>缓存的参数值，不存在返回 0。</returns>
    public virtual int GetInteger(string parameterName)
    {
        int value;
        if (_intParameters.TryGetValue(parameterName, out value)) return value;
        return 0;
    }

    /// <summary>
    /// 设置 Bool 类型 Animator 参数，同时缓存到本地字典。
    /// </summary>
    /// <param name="parameterName">参数名。</param>
    /// <param name="value">参数值。</param>
    public virtual void SetBool(string parameterName, bool value)
    {
        _boolParameters[parameterName] = value;
        if (_animator != null && _animator.isActiveAndEnabled) _animator.SetBool(parameterName, value);
    }

    /// <summary>
    /// 获取缓存的 Bool 参数值。
    /// </summary>
    /// <param name="parameterName">参数名。</param>
    /// <returns>缓存的参数值，不存在返回 false。</returns>
    public virtual bool GetBool(string parameterName)
    {
        bool value;
        if (_boolParameters.TryGetValue(parameterName, out value)) return value;
        return false;
    }

    /// <summary>
    /// 逐帧更新，同步播放速度到 Animator。
    /// </summary>
    /// <param name="deltaSeconds">距上一帧时间（秒），当前未使用。</param>
    public virtual void Update(float deltaSeconds)
    {
        TryReleasePendingSkillAnim();
        if (_animator == null || !_animator.enabled) return;
        _animator.speed = Speed;
    }

    /// <summary>
    /// 完全重置，释放所有资源并清空参数缓存。
    /// 由对象池调用，确保下一次复用时是干净状态。
    /// </summary>
    public virtual void Reset()
    {
        ResetInternal(true);
    }

    /// <summary>
    /// 内部重置实现：释放 Controller 租约、销毁 OverrideController、释放动画片段引用。
    /// </summary>
    /// <param name="clearParameters">是否同时清空参数缓存字典，完全重置时为 true。</param>
    private void ResetInternal(bool clearParameters)
    {
        _sessionVersion++;
        if (_controllerLease != null)
        {
            _controllerLease.Release();
        }
        _controllerLease = null;
        if (_overrideController != null)
        {
            Object.Destroy(_overrideController);
        }
        _overrideController = null;
        _animator = null;
        _controller = null;
        _fallbackController = null;
        _model = null;
        _host = null;
        _controllerLocation = string.Empty;
        _currentState = string.Empty;
        _pendingSkillReleaseLocation = string.Empty;
        _supersededSkillReleaseLocation = string.Empty;
        // 使用快照逐项释放准备中和已就绪剪辑，取消未完成回调后再清空字典。
        List<MAnimInfo> infos = new List<MAnimInfo>(_animInfos.Values);
        for (int index = 0; index < infos.Count; index++)
        {
            // 当前待清理的动画绑定信息，Reset 会同时归还两阶段的共享资源引用。
            MAnimInfo info = infos[index];
            if (info != null)
            {
                info.Reset();
            }
        }
        _animInfos.Clear();
        _blendTreeDict.Clear();
        _preLoadAnimInfos.Clear();
        _missingOverrideSlotWarnings.Clear();
        if (clearParameters)
        {
            _floatParameters.Clear();
            _intParameters.Clear();
            _boolParameters.Clear();
        }
    }

    /// <summary>
    /// 在当前 Controller 的动画片段列表中按名称查找原始片段。
    /// 用于 OverrideController 替换时定位原始片段。
    /// </summary>
    /// <param name="key">要查找的片段名称。</param>
    /// <returns>找到的 AnimationClip，未找到返回 null。</returns>
    protected virtual AnimationClip FindOriginalClip(string key)
    {
        if (_controller == null || _controller.animationClips == null) return null;
        AnimationClip[] clips = _controller.animationClips;
        for (int index = 0; index < clips.Length; index++)
        {
            if (clips[index] != null && clips[index].name == key)
                return clips[index];
        }
        return null;
    }

    /// <summary>
    /// 判断当前 Controller 是否提供指定覆盖槽位。
    /// Controller 尚未绑定时返回 false，调用方应继续等待而不是记录资源错误。
    /// </summary>
    /// <param name="overrideKey">AnimatorOverrideController 中的原始剪辑名称。</param>
    /// <returns>槽位存在返回 true；Controller 未绑定或槽位不存在返回 false。</returns>
    public bool HasOverrideSlot(string overrideKey)
    {
        if (_controller == null || string.IsNullOrEmpty(overrideKey))
        {
            return false;
        }
        return FindOriginalClip(overrideKey) != null;
    }

    /// <summary>
    /// 将缓存的 Float/Int/Bool 参数全部重新应用到 Animator。
    /// 在 Controller 重新绑定后调用，恢复参数状态。
    /// </summary>
    private void ApplyCachedParameters()
    {
        if (_animator == null) return;
        List<string> floatKeys = new List<string>(_floatParameters.Keys);
        for (int index = 0; index < floatKeys.Count; index++)
        {
            string key = floatKeys[index];
            float value = _floatParameters[key];
            _animator.SetFloat(key, value);
        }
        List<string> intKeys = new List<string>(_intParameters.Keys);
        for (int index = 0; index < intKeys.Count; index++)
        {
            string key = intKeys[index];
            _animator.SetInteger(key, _intParameters[key]);
        }
        List<string> boolKeys = new List<string>(_boolParameters.Keys);
        for (int index = 0; index < boolKeys.Count; index++)
        {
            string key = boolKeys[index];
            _animator.SetBool(key, _boolParameters[key]);
        }
    }
}
