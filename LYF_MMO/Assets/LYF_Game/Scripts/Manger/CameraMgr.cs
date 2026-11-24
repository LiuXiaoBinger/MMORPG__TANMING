using Cinemachine;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/**
 * Title:
 * Description:
 */


/// <summary>本地主角的自由视角相机管理器，同时管理世界与UI相机之间的声音监听所有权。</summary>
public partial class CameraMgr : MonoBehaviour
{
    /// <summary>本地招式反馈使用的 Cinemachine 脉冲通道掩码。</summary>
    private const int CombatShakeChannel = 1 << 1;

    /// <summary>技能起手的轻震曲线，持续 0.10 秒。</summary>
    private static readonly CinemachineImpulseDefinition SkillCastImpulse = new CinemachineImpulseDefinition
    {
        m_ImpulseChannel = CombatShakeChannel,
        m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
        m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
        m_ImpulseDuration = 0.10f,
    };

    /// <summary>实际命中的快起衰减曲线，持续 0.14 秒。</summary>
    private static readonly CinemachineImpulseDefinition SkillHitImpulse = new CinemachineImpulseDefinition
    {
        m_ImpulseChannel = CombatShakeChannel,
        m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
        m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil,
        m_ImpulseDuration = 0.14f,
    };

    /// <summary>当前 FreeLook 虚拟相机的战斗脉冲监听器，随相机一起销毁。</summary>
    private CinemachineImpulseListener _combatShakeListener;

    /// <summary>当前实际世界输出相机的声音监听器，三维角色声音必须以此位置计算衰减。</summary>
    private AudioListener _worldAudioListener;


    /// <summary>当前UI相机原有监听器，世界声音接管期间暂时禁用以保持唯一监听器。</summary>
    private AudioListener _uiAudioListener;


    /// <summary>接管前世界相机监听器的启用状态，释放时恢复用户原值。</summary>
    private bool _worldAudioListenerWasEnabled;


    /// <summary>接管前UI监听器的启用状态，世界相机销毁后恢复登录界面的声音。</summary>
    private bool _uiAudioListenerWasEnabled;


    /// <summary>世界监听器是否由本管理器创建，销毁时只移除本管理器新增的组件。</summary>
    private bool _createdWorldAudioListener;


    /// <summary>本管理器创建的世界监听器，切相机时暂禁保留，最终销毁管理器才移除。</summary>
    private readonly List<AudioListener> _ownedAudioListeners = new List<AudioListener>();


    [SerializeField] private float _yAxisSpeed = 0.1f;

    [SerializeField] private float _xAxisSpeed = 10f;

    [SerializeField] private float _delayTimer = 0.6f;

    /// <summary>当前本地主角模型根节点，用于角色属性窗口旋转展示。</summary>
    private Transform _roleMainTransform;


    /// <summary>当前相机注视节点，通常为主角模型下的 LookAtPos。</summary>
    private Transform _lookAtTransform;


    public static CameraMgr Instance;

    private CinemachineFreeLook _cinemachine;

    private Mouse _mouse;

    /// <summary>把原桌面相机灵敏度换算为每像素增量的参考帧率。</summary>
    private const float RotationReferenceFps = 60f;

    /// <summary>当前轨道渐变，连续缩放或切换展示时从当前高度接续。</summary>
    private Tween _orbitTween;

    #region 设置角色属性window视角

    private float _oldOrbitesOffet;

    private float _oldeulerAnglesY;

    private float _oldcimeYaxis;

    private RectTransform _roleAtttWindowRectTransform;

    [HideInInspector]public bool _isOpenRoleAtttWindow;

    //是否打开了背包背包window
    private bool isOpenKnapsackWindow;

    private void Awake()
    {
        Instance = this;
        _cinemachine = GetComponent<CinemachineFreeLook>();
        _sceneFieldOfView = _cinemachine.m_Lens.FieldOfView;
        _mouse = Mouse.current;
        // 两个世界场景原有的 SimpleFollow 会逐帧清空水平轴，改用世界航向保存玩家转过的角度。
        _cinemachine.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
    }


    /// <summary>已有角色的世界相机重新启用时重新绑定监听器，首次启用仍等待角色初始化。</summary>
    private void OnEnable()
    {
        if (_roleMainTransform != null)
        {
            BindAudioListener();
        }
    }


    /// <summary>逐帧处理本地主角的世界镜头，属性展示期间由展示窗口接管。</summary>
    private void Update()
    {
        UpdateWorldView();
    }

    /// <summary>相机管理器临时停用时归还UI监听器，避免停用的世界相机长期占用声音位置。</summary>
    private void OnDisable()
    {
        _talkLost?.Invoke();
        ReleaseNpcTalkCamera();
        ReleaseWorldDrag();
        UpdateGroundHint(false);
        if (_uiAudioListener != null)
        {
            _uiAudioListener.enabled = _uiAudioListenerWasEnabled;
        }
        if (_worldAudioListener != null)
        {
            if (_createdWorldAudioListener)
            {
                // 临时停用只禁用自建监听器并保留所有权，同帧重新启用复用唯一原生组件。
                _worldAudioListener.enabled = false;
            }
            else
            {
                _worldAudioListener.enabled = _worldAudioListenerWasEnabled;
            }
        }
    }



    /// <summary>相机场景销毁后解除声音监听接管，并清理属于自身的静态实例入口。</summary>
    private void OnDestroy()
    {
        if (_talkCamera != null) Destroy(_talkCamera.gameObject);
        if (_groundHint != null) Destroy(_groundHint);
        ReleaseAudioListener();
        // index覆盖本管理器所有自建组件，旧相机场景已经销毁的项无需再次处理。
        for (int index = 0; index < _ownedAudioListeners.Count; index++)
        {
            if (_ownedAudioListeners[index] != null)
            {
                _ownedAudioListeners[index].enabled = false;
                Destroy(_ownedAudioListeners[index]);
            }
        }
        _ownedAudioListeners.Clear();
        if (Instance == this)
        {
            Instance = null;
        }
    }


    /// <summary>本地主角成功起手时，沿画面平面播放轻微冲量。</summary>
    public void PlaySkillCastShake()
    {
        PlayCombatShake(SkillCastImpulse, new Vector3(0.04f, -0.03f, 0f));
    }

    /// <summary>在本机角色的动作重音处播放短振屏，沿用起手冲量的衰减曲线。</summary>
    /// <param name="offset">相机平面上的右向与上向位移，单位米。</param>
    public void PlaySkillAccentShake(Vector2 offset)
    {
        if (WorldCameraSettings.Allows(WorldCameraFeature.SkillAccent)) PlayCombatShake(SkillCastImpulse, offset);
    }

    /// <summary>本地主角命中目标时，沿画面平面播放更强、更快起峰的冲量。</summary>
    public void PlaySkillHitShake()
    {
        _combatUntil = Time.time + 3f;
        PlayCombatShake(SkillHitImpulse, new Vector3(0.075f, -0.065f, 0f));
    }

    /// <summary>以 Cinemachine 的噪声阶段偏移世界镜头，保留跟随、瞄准和碰撞求解使用的原始位姿。</summary>
    /// <param name="impulse">本次反馈使用的预设曲线和持续秒数。</param>
    /// <param name="offset">镜头平面内的位移分量，单位米。</param>
    private void PlayCombatShake(CinemachineImpulseDefinition impulse, Vector3 offset)
    {
        if (_cinemachine == null || !_cinemachine.isActiveAndEnabled)
        {
            return;
        }
        if (_combatShakeListener == null)
        {
            _combatShakeListener = gameObject.AddComponent<CinemachineImpulseListener>();
            _combatShakeListener.m_ChannelMask = CombatShakeChannel;
            _combatShakeListener.m_ApplyAfter = CinemachineCore.Stage.Noise;
            _combatShakeListener.m_Gain = 1f;
            _combatShakeListener.m_UseCameraSpace = true;
        }
        impulse.CreateEvent(_cinemachine.State.FinalPosition, offset);
    }

    /// <summary>让自由视角相机跟随本地主角模型，不依赖旧 MainRoleCtrl。</summary>
    /// <param name="roleTransform">本地主角模型根节点。</param>
    /// <param name="lookAtTransform">模型内的相机注视节点；为空时暂时使用模型根节点。</param>
    public void InitCamera(Transform roleTransform, Transform lookAtTransform)
    {
        if (roleTransform != null && _cinemachine != null)
        {
            _roleMainTransform = roleTransform;
            _lookAtTransform = lookAtTransform;
            if (_lookAtTransform == null)
            {
                _lookAtTransform = roleTransform;
            }
            _cinemachine.Follow = _roleMainTransform;
            _cinemachine.LookAt = _lookAtTransform;
            _cinemachine.m_XAxis.Value = roleTransform.eulerAngles.y;
            _worldZoom = DefaultWorldZoom;
            _combatLift = 0f;
            _combatUntil = 0f;
            _attackingBoss = null;
            ApplyWorldMode();
            SetOrbites(_worldZoom);
        }
        BindAudioListener();
    }



    /// <summary>将三维声音监听绑定到实际世界输出相机，避免远离角色的UI相机吞掉声音。</summary>
    private void BindAudioListener()
    {
        // 真实渲染世界画面的相机；FreeLook是虚拟相机，不能作为三维监听位置。
        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            return;
        }
        if (_worldAudioListener != null && _worldAudioListener.gameObject == worldCamera.gameObject)
        {
            if (_uiAudioListener != null && _uiAudioListener != _worldAudioListener)
            {
                _uiAudioListener.enabled = false;
            }
            _worldAudioListener.enabled = true;
            return;
        }
        ReleaseAudioListener();
        // index从尾部清理随原相机场景销毁的组件，保留仍存活自建监听器供切回时复用。
        for (int index = _ownedAudioListeners.Count - 1; index >= 0; index--)
        {
            if (_ownedAudioListeners[index] == null)
            {
                _ownedAudioListeners.RemoveAt(index);
            }
        }
        _worldAudioListener = worldCamera.GetComponent<AudioListener>();
        if (_worldAudioListener == null)
        {
            _worldAudioListener = worldCamera.gameObject.AddComponent<AudioListener>();
            _ownedAudioListeners.Add(_worldAudioListener);
        }
        _createdWorldAudioListener = _ownedAudioListeners.Contains(_worldAudioListener);
        _worldAudioListenerWasEnabled = _worldAudioListener.enabled;
        if (UIRoot.Instance != null)
        {
            // 使用UIRoot现成接口精准定位UI相机，不禁用其他场景或用户配置的监听器。
            Camera uiCamera = UIRoot.Instance.GetUICamera();
            if (uiCamera != null)
            {
                _uiAudioListener = uiCamera.GetComponent<AudioListener>();
            }
            if (_uiAudioListener != null && _uiAudioListener != _worldAudioListener)
            {
                _uiAudioListenerWasEnabled = _uiAudioListener.enabled;
                _uiAudioListener.enabled = false;
            }
        }
        _worldAudioListener.enabled = true;
    }



    /// <summary>退出世界相机时归还监听所有权，恢复UI和原有组件状态。</summary>
    private void ReleaseAudioListener()
    {
        if (_uiAudioListener != null)
        {
            _uiAudioListener.enabled = _uiAudioListenerWasEnabled;
        }
        if (_worldAudioListener != null)
        {
            if (_createdWorldAudioListener)
            {
                // 切相机只禁用自建项，保留供同帧切回复用；Unity不允许同对象再添加第二监听器。
                _worldAudioListener.enabled = false;
            }
            else
            {
                _worldAudioListener.enabled = _worldAudioListenerWasEnabled;
            }
        }
        _worldAudioListener = null;
        _uiAudioListener = null;
        _createdWorldAudioListener = false;
        _worldAudioListenerWasEnabled = false;
        _uiAudioListenerWasEnabled = false;
    }


    /// <summary>从当前高度平滑过渡到新的 FreeLook 轨道，连续滚轮共用一个可中断动画。</summary>
    /// <param name="offset">顶部轨道目标高度，限制在 3 至 50 米。</param>
    private void SetOrbites(float offset)
    {
        _orbitTween.Kill();
        _orbitTween = DOTween.To(() => _cinemachine.m_Orbits[0].m_Height, SetOrbitHeight,
            Mathf.Clamp(offset, 3f, 50f), _delayTimer).SetLink(gameObject);
    }

    /// <summary>按项目原有三层高度及半径比例调整环绕距离，保留底层原始高度和避障扩展。</summary>
    /// <param name="height">当前插值后的顶部轨道高度，单位米。</param>
    private void SetOrbitHeight(float height)
    {
        _cinemachine.m_Orbits[0].m_Height = height;
        _cinemachine.m_Orbits[0].m_Radius = height * 0.25f;
        _cinemachine.m_Orbits[1].m_Height = height * 0.5f;
        _cinemachine.m_Orbits[1].m_Radius = height * 0.7f;
        _cinemachine.m_Orbits[2].m_Radius = height * 0.15f;
    }

    /// <summary>
    /// 设置角色属性window视角
    /// </summary>
    /// <param name="roleAtttWindow"></param>
    public void RoleAttrWindowAngle(WindowBase roleAtttWindow)
    {
        if (roleAtttWindow.gameObject.activeSelf)
        {
            ReleaseWorldDrag();
            UpdateGroundHint(false);
            _isOpenRoleAtttWindow = true;
            _roleAtttWindowRectTransform =roleAtttWindow.transform as RectTransform;
            _oldOrbitesOffet = _worldZoom * (1f + 2f * _combatLift);
            _oldeulerAnglesY = Camera.main.transform.eulerAngles.y;
            _oldcimeYaxis = _cinemachine.m_YAxis.Value;
            if (isOpenKnapsackWindow)
                
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(0,0.85f),460);
            }
            else
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(1.1f,0.85f),800);
            }
            
        }
        else
        {
            RecoverAngle();

        }
    }

    /// <summary>
    /// 恢复视角
    /// </summary>
    public void RecoverAngle()
    {
        if (_isOpenRoleAtttWindow)
        {
            _isOpenRoleAtttWindow = false;
            ToggleAngle(_oldOrbitesOffet, _oldeulerAnglesY, _oldcimeYaxis,new Vector3(0,1.17f),460);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cinemOffset">轨道</param>
    /// <param name="roleRotaY"></param>
    /// <param name="cinemYAxis"></param>
    private void ToggleAngle(float cinemOffset, float roleRotaY, float cinemYAxis,Vector3 lookatpos,float roleAttrWindowOffset)
    {
        if (_roleMainTransform == null || _cinemachine == null)
        {
            return;
        }
        // 展示时使用场景原视野，返回世界时恢复当前镜头预设。
        ApplyWorldFieldOfView();
        // 设置展示或返回世界时的轨道距离。
        SetOrbites(cinemOffset);
        //设置角色选择，让角色正对摄像机 Camera.main.transform.eulerAngles.y
        _roleMainTransform.eulerAngles = new Vector3(0, roleRotaY, 0);
        //设置相机旋转x轴旋转
        _cinemachine.m_YAxis.Value = cinemYAxis;
        
        //设置Lookat点属性
        if (_lookAtTransform != null && _lookAtTransform != _roleMainTransform)
        {
            _lookAtTransform.localPosition = lookatpos;
        }

        _roleAtttWindowRectTransform.DOAnchorPos(new Vector2(roleAttrWindowOffset, 0), _delayTimer);
    }

    public void KnapsackWindowAngle(WindowBase window)
    {
        isOpenKnapsackWindow = window.gameObject.activeSelf;
        if (_isOpenRoleAtttWindow)//如果角色属性wind已经打开
        {
            if (isOpenKnapsackWindow) //在打开背包
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(0,0.85f),460);
            }
            else
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(1.1f,0.85f),800);
            }
        }
    }
    
    #endregion
}
