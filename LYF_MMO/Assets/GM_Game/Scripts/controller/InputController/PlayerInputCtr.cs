using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.TouchPhase;

/// <summary>玩家输入控制器，负责采集 Input System 回调并转为实体输入事件。</summary>
public class PlayerInputCtr : MonoBehaviour
{
    /// <summary>当前场景中的输入控制器单例。</summary>
    public static PlayerInputCtr Instance;

    /// <summary>Input System 生成的玩家动作集合。</summary>
    public PlayerInput playerInput;

    /// <summary>技能快捷键字符通知。</summary>
    public event Action<string> SkillKeyEvent;

    /// <summary>主界面快捷键字符通知。</summary>
    public event Action<string> MainUIKeyHandler;

    /// <summary>移动状态检查的固定时间间隔，单位为秒，约等于 30Hz。</summary>
    private const float MovementCheckIntervalSeconds = 0.033f;

    /// <summary>方向键从按下到允许停止的最短保持时间，单位为秒；用于完整走完动画混合过渡。</summary>
    private const float MinimumMovementHoldSeconds = 0.2f;

    /// <summary>自上次移动检查后累计的非缩放时间，禁用组件时清零。</summary>
    private float _movementCheckElapsedSeconds;

    /// <summary>当前移动动作是否保持非零输入；用于在周期采样时校正遗漏的取消回调。</summary>
    private bool _isMovementInputActive;

    /// <summary>最近一次收到的非零移动输入，生命周期持续到取消输入或组件禁用。</summary>
    private Vector2 _latestMovementInput;

    /// <summary>最近一次移动取消事件的 Input System 时间戳，单位为秒，用于拒绝取消事件之前排队的旧方向回调。</summary>
    private double _lastMovementCanceledTime = -1d;

    /// <summary>本轮非零方向输入开始时间，使用不受 Time.timeScale 影响的秒数。</summary>
    private float _movementInputStartedTime = -1f;

    /// <summary>方向松开后是否正在等待最短保持时间结束。</summary>
    private bool _movementStopPending;

    /// <summary>延迟停止的目标时间，达到该时间后才派发停止事件。</summary>
    private float _movementStopTime = -1f;

    /// <summary>本轮输入是否已经收到取消；用于区分同一时间戳的真实 Started 与迟到 Performed。</summary>
    private bool _movementInputCanceled;

    /// <summary>最近一次派发技能快捷键所在的 Unity 帧编号，用于过滤同帧文本与物理事件重复通知。</summary>
    private int _lastSkillDispatchFrame = -1;

    /// <summary>最近一次派发的技能快捷键字符；仅与同帧编号组合使用。</summary>
    private string _lastSkillDispatchKey;

    /// <summary>读取当前 Movement 动作向量，供角色控制器进行移动计算。</summary>
    public Vector2 Movement
    {
        get
        {
            if (playerInput == null)
            {
                return Vector2.zero;
            }
            return playerInput.player.Movement.ReadValue<Vector2>();
        }
    }

    /// <summary>用现有 Input System 采集鼠标按下松开与 Tab，Tab 和瞄准按钮走同一切换方法。</summary>
    private void PollTargetSelection()
    {
        // 当前鼠标设备，桌面点击以左键的真实边沿为准。
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            // 本帧屏幕像素位置，同时用于 UI 阻挡和世界射线检测。
            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                TouchObject.singleton.JudegeType(position, TouchPhase.Began);
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                TouchObject.singleton.JudegeType(position, TouchPhase.Ended);
            }
        }
        // Tab 是桌面便捷入口，不修改已有技能字符和移动动作绑定。
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            MSkillTargetMgr.singleton.FindEnemyByAimBtn();
        }
    }

    /// <summary>初始化 Input System 并注册动作回调。</summary>
    private void Awake()
    {
        Instance = this;
        playerInput = new PlayerInput();
        RegisterInputEvents();
    }

    /// <summary>启用 Input System 动作资产。</summary>
    public void OnEnable()
    {
        // 新的组件启用周期不继承上次禁用前的取消时间，允许第一轮按键立即生效。
        _lastMovementCanceledTime = -1d;
        _movementInputCanceled = false;
        ResetMovementInputState();
        if (playerInput != null && playerInput.asset != null)
        {
            playerInput.asset.Enable();
        }
    }

    /// <summary>按固定时间间隔对账移动输入，补发持续移动或遗漏的停止事件。</summary>
    private void Update()
    {
        // 文本输入事件并不保证由 QueueStateEvent 或所有键盘布局产生，物理边沿作为统一技能入口的补充来源。
        // 选择输入先检查目标有效性，技能快捷键使用同帧最新的当前目标。
        MSkillTargetMgr.singleton.UpdateToCheckTarget();
        PollTargetSelection();
        PollPhysicalSkillKeys();
        _movementCheckElapsedSeconds += Time.unscaledDeltaTime;
        if (_movementCheckElapsedSeconds < MovementCheckIntervalSeconds)
        {
            return;
        }

        _movementCheckElapsedSeconds = 0f;
        // 只把仍处于按下状态的动作值视为有效输入，避免已释放按键的缓存向量重新启动移动。
        Vector2 movement = ReadCurrentMovementInput();
        if (movement != Vector2.zero)
        {
            BeginMovementInput(movement);
            // 按住期间重试当前方向，避免按键边沿在角色暂时不可移动时被丢弃后落地仍停在 Idle。
            DispatchMovement(_latestMovementInput);
            return;
        }

        if (_movementStopPending)
        {
            if (Time.unscaledTime > _movementStopTime)
            {
                // 停止事件到期后只派发一次，避免同一帧重复清理移动状态。
                FinalizeMovementStop();
                return;
            }
            return;
        }
        // 当前本地角色只在本次检查中读取，输入控制器不持有跨场景角色引用。
        PlayerEntity localRole = GetLocalRole();
        // 移动组件可能保留上一角色或遗漏取消回调留下的方向，零输入时必须主动收敛。
        bool hasResidualMovement = localRole != null
                                   && localRole.MoveComp != null &&
            localRole.MoveComp.HasDirectionalInput;
        if (_isMovementInputActive || hasResidualMovement)
        {
            ResetMovementInputState();
            DispatchStopMovement();
        }
    }

    /// <summary>禁用 Input System 动作资产。</summary>
    public void OnDisable()
    {
        // 禁用前先停止当前角色，防止切换场景或关闭组件后残留移动方向。
        _movementInputCanceled = true;
        TouchObject.singleton.Clear();
        DispatchStopMovement();
        ResetMovementInputState();
        if (playerInput != null && playerInput.asset != null)
        {
            playerInput.asset.Disable();
        }
    }

    /// <summary>销毁输入资产，避免控制器销毁后继续处理输入。</summary>
    private void OnDestroy()
    {
        // 销毁前再次收敛角色状态；重复停止不会产生额外业务副作用。
        _movementInputCanceled = true;
        DispatchStopMovement();
        ResetMovementInputState();
        if (playerInput != null)
        {
            playerInput.Disable();
        }
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
    }

    /// <summary>注册 Shift、Jump、Movement 和文本输入回调。</summary>
    private void RegisterInputEvents()
    {
        playerInput.player.Shift.started += context =>
        {
            ApplyRunningState(context.ReadValueAsButton());
        };
        playerInput.player.Shift.canceled += context =>
        {
            ApplyRunningState(context.ReadValueAsButton());
        };
        playerInput.player.Jump.started += OnJumpStarted;
        playerInput.player.Movement.started += OnMovementChanged;
        //playerInput.player.Movement.performed += OnMovementChanged;
        playerInput.player.Movement.canceled += OnMovementCanceled;

        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInput;
        }
    }

    /// <summary>处理移动开始或方向变化，立即把最新非零输入派发给本地角色。</summary>
    /// <param name="context">Input System 提供的移动动作阶段和二维输入值。</param>
    private void OnMovementChanged(InputAction.CallbackContext context)
    {
        // 回调时间早于最近一次移动取消时间，属于取消之前的旧回调。
        bool isBeforeCancelTime = context.time < _lastMovementCanceledTime;
        // 与取消时间同戳、取消闸门仍开启且本次不是 Started 阶段，仍视为排队旧回调。
        bool isSameStampLegacy = context.time <= _lastMovementCanceledTime &&
            _movementInputCanceled && !context.started;
        if (isBeforeCancelTime || isSameStampLegacy)
        {
            // 取消后可能仍有旧 Performed 回调排队；同时间戳只有新的 Started 才能开启下一轮输入。
            return;
        }

        // 本次回调携带的完整二维方向；快速点按时 performed 也可能读到零值。
        Vector2 movement = context.ReadValue<Vector2>();
        if (movement == Vector2.zero)
        {
            // 零值进入延迟停止，避免输入持续时间短于动画混合时间。
            _movementInputCanceled = true;
            ScheduleMovementStop();
            return;
        }

        // 回调队列可能在按键释放后仍携带非零值；只有动作当前仍处于按下状态才允许恢复移动。
        if (playerInput == null || !playerInput.player.Movement.IsPressed())
        {
            _movementInputCanceled = true;
            ScheduleMovementStop();
            return;
        }

        // 新的非零 started/performed 回调代表新一轮真实按键，解除取消闸门。
        BeginMovementInput(movement);
        // 按键边沿即时派发，确保角色在空中按下方向时能在落地前记录输入。
        DispatchMovement(_latestMovementInput);
    }

    /// <summary>处理移动取消，立即清除输入缓存并停止本地角色水平移动。</summary>
    /// <param name="context">Input System 提供的取消阶段信息；当前逻辑不读取其中的零向量。</param>
    private void OnMovementCanceled(InputAction.CallbackContext context)
    {
        // 记录取消事件的设备时间，后续只接受时间更晚的新一轮按键输入。
        _lastMovementCanceledTime = context.time;
        ScheduleMovementStop();
    }

    /// <summary>读取移动动作的实时按压状态，按键已经释放时不返回 Input System 中残留的向量缓存。</summary>
    /// <returns>仍保持按压时的二维移动向量；动作未启用或已经取消时返回零向量。</returns>
    private Vector2 ReadCurrentMovementInput()
    {
        if (playerInput == null || !playerInput.player.Movement.IsPressed())
        {
            return Vector2.zero;
        }

        return playerInput.player.Movement.ReadValue<Vector2>();
    }

    /// <summary>清除移动输入保持状态和周期累计时间，避免旧输入在后续帧重新启动移动。</summary>
    private void ResetMovementInputState()
    {
        _isMovementInputActive = false;
        _latestMovementInput = Vector2.zero;
        _movementCheckElapsedSeconds = 0f;
        _movementInputStartedTime = -1f;
        _movementStopPending = false;
        _movementStopTime = -1f;
    }

    /// <summary>开始或更新一轮有效方向输入，并取消尚未到期的停止请求。</summary>
    /// <param name="movement">Input System 当前读取的非零二维移动向量。</param>
    private void BeginMovementInput(Vector2 movement)
    {
        if (!_isMovementInputActive || _movementStopPending)
        {
            // 延迟停止期间重新按键代表新一轮输入，必须从本次按下重新计算 0.1 秒保持窗口。
            _movementInputStartedTime = Time.unscaledTime;
        }

        _movementInputCanceled = false;
        _isMovementInputActive = true;
        _latestMovementInput = movement;
        _movementStopPending = false;
        _movementStopTime = -1f;
    }

    /// <summary>安排移动停止，在本轮输入已保持至少 0.1 秒后执行。</summary>
    private void ScheduleMovementStop()
    {
        _movementInputCanceled = true;
        if (!_isMovementInputActive)
        {
            return;
        }

        float minimumStopTime = Time.unscaledTime + MinimumMovementHoldSeconds;
        _movementStopPending = true;
        _movementStopTime = minimumStopTime;
    }

    /// <summary>到达最短保持时间后派发停止事件并清理本轮输入状态。</summary>
    private void FinalizeMovementStop()
    {
        ResetMovementInputState();
        DispatchStopMovement();
    }

    /// <summary>按下跳跃键时直接请求本地玩家移动组件开始下一段跳跃。</summary>
    /// <param name="context">Input System 提供的本次跳跃按键阶段信息。</param>
    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        // 跳跃只在按下沿执行；松开按键不再连接旧角色控制器或结束跳跃。
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null || localRole.MoveComp == null)
        {
            return;
        }

        localRole.MoveComp.TryJump();
    }

    /// <summary>处理原始文本输入并转发实体事件及快捷键通知。</summary>
    /// <param name="inputCharacter">Input System 产生的字符。</param>
    private void OnTextInput(char inputCharacter)
    {
        if (playerInput == null || !playerInput.asset.enabled)
        {
            return;
        }

        DispatchTextInput(inputCharacter);
        // 将文本字符转为大写字符串，用于技能和界面快捷键匹配。
        string key = inputCharacter.ToString().ToUpperInvariant();
        if (IsSkillKey(key))
        {
            DispatchSkillShortcut(key);
        }
        else if (key == "L" || key == "B" || key == "I")
        {
            MainUIKeyHandler?.Invoke(key);
        }
    }

    /// <summary>读取 Q/E/R/F/数字键的物理按下边沿并派发技能快捷键。</summary>
    private void PollPhysicalSkillKeys()
    {
        // 每帧读取当前键盘设备；设备不存在时测试输入和真实输入都不应抛异常。
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }
        if (playerInput == null || playerInput.asset == null)
        {
            return;
        }
        if (!playerInput.asset.enabled)
        {
            return;
        }

        DispatchPhysicalKeyIfPressed(keyboard, Key.Q, "Q");
        DispatchPhysicalKeyIfPressed(keyboard, Key.E, "E");
        DispatchPhysicalKeyIfPressed(keyboard, Key.R, "R");
        DispatchPhysicalKeyIfPressed(keyboard, Key.F, "F");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit1, "1");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit2, "2");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit3, "3");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit4, "4");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit5, "5");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit6, "6");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit7, "7");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit8, "8");
        DispatchPhysicalKeyIfPressed(keyboard, Key.Digit9, "9");
    }

    /// <summary>检查单个物理按键的本帧按下边沿并转为技能字符。</summary>
    /// <param name="keyboard">Input System 当前键盘设备。</param>
    /// <param name="key">需要检测的物理按键。</param>
    /// <param name="skillKey">该按键对应的技能快捷键字符。</param>
    private void DispatchPhysicalKeyIfPressed(Keyboard keyboard, Key key, string skillKey)
    {
        if (keyboard[key].wasPressedThisFrame)
        {
            DispatchSkillShortcut(skillKey);
        }
    }

    /// <summary>派发技能快捷键并过滤同一帧由文本和物理链路产生的重复事件。</summary>
    /// <param name="skillKey">已经规范化为大写或数字的技能快捷键。</param>
    private void DispatchSkillShortcut(string skillKey)
    {
        if (!IsSkillKey(skillKey))
        {
            return;
        }

        // 当前 Unity 帧编号，用于合并同一帧的文本输入与物理边沿。
        int currentFrame = Time.frameCount;
        if (_lastSkillDispatchFrame == currentFrame && _lastSkillDispatchKey == skillKey)
        {
            return;
        }

        _lastSkillDispatchFrame = currentFrame;
        _lastSkillDispatchKey = skillKey;
        if (SkillKeyEvent != null)
        {
            SkillKeyEvent.Invoke(skillKey);
        }
    }

    /// <summary>判断字符是否为技能快捷键。</summary>
    /// <param name="key">转为大写后的单字符字符串。</param>
    /// <returns>属于技能快捷键时返回 true。</returns>
    private bool IsSkillKey(string key)
    {
        // 字母快捷键分组 Q/E/R。
        bool isLetterQer = key == "Q" || key == "E" || key == "R";
        // 字母 F 与数字快捷键 1/2。
        bool isLetterF12 = key == "F" || key == "1" || key == "2";
        // 字母快捷键组合。
        bool isLetterGroup = isLetterQer || isLetterF12;
        // 数字快捷键 3/4/5。
        bool isDigit345 = key == "3" || key == "4" || key == "5";
        // 数字快捷键 6/7/8。
        bool isDigit678 = key == "6" || key == "7" || key == "8";
        // 数字快捷键 9。
        bool isDigit9 = key == "9";
        // 数字快捷键组合。
        bool isDigitGroup = isDigit345 || isDigit678 || isDigit9;
        return isLetterGroup || isDigitGroup;
    }

    /// <summary>把 Shift 的按住状态交给本地玩家移动组件，切换普通移动与跑步。</summary>
    /// <param name="isRunning">true 表示 Shift 正在按住并使用跑步；false 表示恢复普通移动。</param>
    private void ApplyRunningState(bool isRunning)
    {
        // 当前本地玩家实体；输入层只读取本次引用，不保存角色生命周期。
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null || localRole.MoveComp == null)
        {
            return;
        }

        localRole.MoveComp.SetRunning(isRunning);
    }

    /// <summary>根据当前相机朝向把移动输入封装为池化移动事件并发送给本地玩家实体。</summary>
    /// <param name="movement">Input System 读取的二维移动向量，x/y 分别代表水平和前后输入。</param>
    private void DispatchMovement(
        Vector2 movement)
    {
        // 本地角色由场景世界管理，输入控制器不缓存其生命周期引用。
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null)
        {
            return;
        }

        // 相机为空时不能把屏幕输入转换为世界朝向，必须在取得池对象前结束。
        Transform mainCameraTransform = localRole.GetMainCamera();
        if (mainCameraTransform == null)
        {
            return;
        }

        // 参数所有权在成功派发后移交分发器；发生异常时由当前方法归还尚未移交的池对象。
        // 尚未交接给分发器的移动参数；异常时据此判断是否需要由当前方法回收。
        MMoveEventArgs moveArgs = null;
        // true 表示已把参数所有权交给桥接器，后续回收由实体事件分发器负责。
        bool moveArgsOwnershipTransferred = false;
        try
        {
            // 先计算世界朝向，计算失败时尚未从对象池取得实例，不需要执行回收。
            // 按主相机水平朝向换算出的世界空间移动朝向。
            Vector3 faceDirection = GetMovementCameraDirection(mainCameraTransform,
                movement, InputSystem.settings.defaultDeadzoneMin);
            moveArgs = MMoveEventArgs.Create(false, Vector3.zero, faceDirection,
                true, 0f, true);
            // 调用桥接器前明确移交所有权，避免分发器异常后当前方法重复回收。
            moveArgsOwnershipTransferred = true;
            EntityInputEventBridge.DispatchInputMove(localRole, moveArgs);
            // DispatchInputMove 返回后分发器已完成回收，当前方法不再拥有该参数。
            moveArgs = null;
        }
        catch (Exception exception)
        {
            // 分发前或分发过程中异常时，归还仍由当前方法持有的参数，避免池对象泄漏。
            if (!moveArgsOwnershipTransferred && moveArgs != null)
            {
                moveArgs.Recycle();
            }
            Debug.LogError("本地移动事件派发失败：" + exception);
        }
    }

    /// <summary>向本地玩家派发停止移动事件，清理移动组件中的方向和预测状态。</summary>
    private void DispatchStopMovement()
    {
        // 获取当前本地玩家实体，场景未进入游戏时无需创建事件参数。
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null)
        {
            return;
        }

        // 停止事件没有业务载荷，分发器不会创建或回收额外对象池参数。
        localRole.DispatchEvent(MEventType.MEvent_StopMove, null);
    }

    /// <summary>把文本字符封装为统一输入参数并发送给本地玩家实体。</summary>
    /// <param name="inputCharacter">本次输入字符。</param>
    private void DispatchTextInput(char inputCharacter)
    {
        // 获取当前本地玩家实体，场景未进入游戏时为空。
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null)
        {
            return;
        }
        // 文本事件也从输入参数对象池获取，避免每个字符输入产生新的托管对象。
        InputEventArgs eventArgs = InputEventArgs.CreateText(inputCharacter);
        EntityInputEventBridge.DispatchInput(localRole, eventArgs);
    }

    /// <summary>获取场景中的本地玩家实体。</summary>
    /// <returns>本地玩家实体；场景未准备好时返回 null。</returns>
    private PlayerEntity GetLocalRole()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return null;
        }
        return Global.Instance.SceneWorld.LocalRole;
    }

    public virtual Vector3 GetMovementCameraDirection(Transform mainCameraTransform,
        Vector2 value, float deadzone)
    {
        // 1. 获取移动方向（通常是玩家输入的水平/垂直方向，比如 WSAD 或摇杆）
        Vector3 direction = GetMovementDirection(value, deadzone);

        // 2. 如果有输入（不是零向量）
        if (direction.sqrMagnitude > 0)
        {
            // 3. 构建一个旋转：根据摄像机的 Y 轴角度（水平朝向）
            // Quaternion.AngleAxis(angle, axis) 表示绕某个轴旋转一个角度
            Quaternion rotation = Quaternion.AngleAxis(
                mainCameraTransform.eulerAngles.y, Vector3.up);

            // 4. 把原始输入方向旋转到摄像机的朝向下
            direction = rotation * direction;

            // 5. 归一化，保持方向向量的长度为 1（只保留方向）
            direction = direction.normalized;
        }

        // 6. 返回最终的世界空间移动方向
        return direction;
    }

    /// <summary>
    /// 获取移动方向输入（带十字型死区判断）
    /// 如果在锁定时间内，则返回 Vector3.zero
    /// </summary>
    public virtual Vector3 GetMovementDirection(Vector2 value, float deadzone)
    {
        //if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;
        return GetAxisWithCrossDeadZone(value,deadzone);
    }

    /// <summary>
    /// 根据十字形死区修正输入值（Input System 默认是圆形死区）
    /// </summary>
    /// <param name="axis">输入轴</param>
    public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis, float deadzone)
    {
        //Debug.Log("axis " + axis.ToString());
        if (Mathf.Abs(axis.x) > deadzone) axis.x = RemapToDeadzone(axis.x, deadzone);
        else axis.x = 0f;
        if (Mathf.Abs(axis.y) > deadzone) axis.y = RemapToDeadzone(axis.y, deadzone);
        else axis.y = 0f;
        return new Vector3(axis.x, 0, axis.y);
    }

    /// <summary>
    /// 将输入值按给定死区重新映射到 0-1
    /// </summary>
    //protected float RemapToDeadzone(float value,float deadzone)=>(value - deadzone) / (1-deadzone);
    protected float RemapToDeadzone(float value, float deadzone)
    {
        // 输入值为正时使用负偏移，输入值为负时使用正偏移，使死区外数值映射回完整区间。
        float offset = deadzone;
        if (value > 0)
        {
            offset = -deadzone;
        }
        return (value - offset) / (1f - deadzone);
    }
}
