using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>世界 FreeLook 的桌面输入、自动构图和轻功落点表现，与属性展示共用同一个 CameraMgr。</summary>
public partial class CameraMgr
{
    /// <summary>复位及初次入场时的顶部轨道高度，单位米。</summary>
    private const float DefaultWorldZoom = 12f;
    /// <summary>场景 FreeLook 自带的垂直视野角，单位为度，供自由镜头和角色展示使用。</summary>
    private float _sceneFieldOfView;
    /// <summary>手动缩放的目标高度，单位米；战斗构图只在此基础上临时放大。</summary>
    private float _worldZoom = DefaultWorldZoom;
    /// <summary>玩家选定的 FreeLook 俯角，零为底层轨道，一为顶层轨道。</summary>
    private float _worldPitch = 0.5f;
    /// <summary>当前战斗构图叠加的竖轴偏移，脱战归零并恢复手动距离。</summary>
    private float _combatLift;
    /// <summary>最近一次攻击或受击后的构图截止时刻，单位为游戏秒。</summary>
    private float _combatUntil;
    /// <summary>最近实际击中主角的首领，在没有选中目标时用于首领构图。</summary>
    private MonsterEntity _attackingBoss;
    /// <summary>复位或双击地面后要转向的世界航向角，完成或右键接管后清除。</summary>
    private float? _requestedYaw;
    /// <summary>由世界画面开始的右键拖动，负责与此次按下配对地释放光标。</summary>
    private bool _worldDrag;
    /// <summary>当前世界相机复用的轻功地面圆环，离开场景时销毁。</summary>
    private GameObject _groundHint;

    /// <summary>切换到定制 2.5D 时看到的俯角；角色展示期间读取进入展示前的世界俯角。</summary>
    public float WorldPitch
    {
        get
        {
            if (_isOpenRoleAtttWindow) return Mathf.Clamp01(_oldcimeYaxis);
            return Mathf.Clamp01(_cinemachine.m_YAxis.Value);
        }
    }

    /// <summary>切换到 2.5D 时应用对应的固定俯角，切回自由模式时从当前视角接手。</summary>
    public void ApplyWorldMode()
    {
        _requestedYaw = null;
        _worldPitch = WorldPitch;
        switch (WorldCameraSettings.Mode)
        {
            case WorldCameraMode.Tuned25D: _worldPitch = WorldCameraSettings.TunedPitch; break;
            case WorldCameraMode.Default25D: _worldPitch = WorldCameraSettings.DefaultOverheadPitch; break;
            default: _worldPitch = Mathf.Clamp01(_worldPitch - _combatLift); break;
        }
        ApplyWorldFieldOfView();
        if (!_isOpenRoleAtttWindow) _cinemachine.m_YAxis.Value = _worldPitch;
    }

    /// <summary>默认 2.5D 用较宽的垂直视野保留身前街景，角色展示和其余模式使用场景原值。</summary>
    private void ApplyWorldFieldOfView()
    {
        _cinemachine.m_Lens.FieldOfView = _sceneFieldOfView;
        if (!_isOpenRoleAtttWindow && WorldCameraSettings.Mode == WorldCameraMode.Default25D)
            _cinemachine.m_Lens.FieldOfView = WorldCameraSettings.Default25DFieldOfView;
    }

    /// <summary>一个世界帧内先处理玩家输入，再用本地主角的实际状态调整距离和航向。</summary>
    private void UpdateWorldView()
    {
        if (_roleMainTransform == null || _isOpenRoleAtttWindow) return;
        ReadWorldPointer();
        // 当前世界的玩家实体，角色绑定早于实体入场时仍可手动观察。
        PlayerEntity player = Global.Instance?.SceneWorld?.LocalRole;
        // 技能主表中的 1、2 为攻击，4 为轻功；退出主动作槽的技能尾迹不影响构图。
        int skillType = 0;
        // 只取仍占据动作槽的本地技能。
        MSkill skill = player?.Skill?.CurrentSkill;
        if (skill != null && skill.IsActive)
        {
            // 实际技能编号对应的主表配置，普通走跑不查表。
            cfg.SkillInfo definition = LubanMgr.Instance.GetSkillInfoById(skill.SkillId);
            if (definition != null) skillType = definition.Type;
        }
        if (skillType == 1 || skillType == 2) _combatUntil = Time.time + 3f;
        // 以当前选中的首领优先，未选敌时仍能看到刚刚击中自己的首领。
        MonsterEntity boss = null;
        if (player != null && !player.IsDead && Time.time < _combatUntil)
        {
            boss = player.Target as MonsterEntity;
            if (player.Target == null) boss = _attackingBoss;
            if (boss != null && (boss.Rank != MonsterRank.Boss || !MSkillTargetDefine.IsValidTarget(boss))) boss = null;
        }
        else
        {
            _attackingBoss = null;
        }
        // 两档构图互斥，首领开关和普通战斗开关可在自定义中独立控制。
        float lift = 0f;
        if (boss != null)
        {
            if (WorldCameraSettings.Allows(WorldCameraFeature.RaiseForBoss)) lift = 0.20f;
        }
        else if (player != null && !player.IsDead && Time.time < _combatUntil)
        {
            if (WorldCameraSettings.Allows(WorldCameraFeature.RaiseForCombat)) lift = 0.12f;
        }
        if (_combatLift != lift)
        {
            _combatLift = lift;
            SetOrbites(_worldZoom * (1f + 2f * lift));
        }
        _cinemachine.m_YAxis.Value = Mathf.MoveTowards(_cinemachine.m_YAxis.Value,
            Mathf.Clamp01(_worldPitch + lift), Time.deltaTime);
        UpdateWorldHeading(player, boss);
        UpdateGroundHint(skillType == 4 && WorldCameraSettings.Allows(WorldCameraFeature.QinggongGround));
    }

    /// <summary>读取桌面滚轮、中键和右键；从 UI 开始的点击留给窗口处理。</summary>
    private void ReadWorldPointer()
    {
        _mouse = Mouse.current;
        if (_mouse == null || !_mouse.rightButton.isPressed) ReleaseWorldDrag();
        if (_mouse == null) return;
        // 当前帧滚轮原始增量，Windows 原生一格常为 120，编辑器也可能提供单位格。
        float wheel = _mouse.scroll.y.ReadValue();
        // 仅在需要决定输入归属的帧对实际屏幕位置做 UI 射线检测。
        bool pressed = _mouse.rightButton.wasPressedThisFrame || _mouse.middleButton.wasPressedThisFrame || wheel != 0f;
        if (pressed && !_worldDrag && TouchObject.singleton.IsPointerOverUI(_mouse.position.ReadValue())) return;
        if (wheel != 0f && WorldCameraSettings.Allows(WorldCameraFeature.WheelZoom))
        {
            // 保留高精度小增量，也把同帧多个 Windows 刻度按实际格数累加。
            float steps = Mathf.Clamp(wheel, -1f, 1f);
            if (Mathf.Abs(wheel) >= 120f) steps = wheel / 120f;
            _worldZoom = Mathf.Clamp(_worldZoom - steps * 2f, 3f, 50f);
            SetOrbites(_worldZoom * (1f + 2f * _combatLift));
        }
        if (_mouse.middleButton.wasPressedThisFrame && WorldCameraSettings.Allows(WorldCameraFeature.Reset))
        {
            _worldZoom = DefaultWorldZoom;
            _worldPitch = 0.5f;
            if (WorldCameraSettings.Mode == WorldCameraMode.Tuned25D) _worldPitch = WorldCameraSettings.TunedPitch;
            if (WorldCameraSettings.Mode == WorldCameraMode.Default25D) _worldPitch = WorldCameraSettings.DefaultOverheadPitch;
            SetOrbites(_worldZoom * (1f + 2f * _combatLift));
            _requestedYaw = _roleMainTransform.eulerAngles.y;
        }
        // 水平与俯仰分别响应开关，没有可拖动的轴时保留系统光标。
        bool yaw = WorldCameraSettings.Allows(WorldCameraFeature.DragYaw);
        // 两种 2.5D 预设关闭竖轴拖动，切换过程中也不会改变锁定俯角。
        bool pitch = WorldCameraSettings.Allows(WorldCameraFeature.DragPitch);
        if (_mouse.rightButton.wasPressedThisFrame && (yaw || pitch))
        {
            _requestedYaw = null;
            _worldDrag = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (!_worldDrag) return;
        // 每帧的鼠标位移已经是累计像素，按参考灵敏度换算一次即可。
        Vector2 delta = _mouse.delta.ReadValue();
        if (yaw) _cinemachine.m_XAxis.Value += delta.x * (_xAxisSpeed / RotationReferenceFps);
        if (pitch)
        {
            _worldPitch = Mathf.Clamp01(_worldPitch - delta.y * (_yAxisSpeed / RotationReferenceFps));
            _cinemachine.m_YAxis.Value = Mathf.Clamp01(_worldPitch + _combatLift);
        }
    }

    /// <summary>按手动指令、首领、寻路、左右移动的顺序选择水平构图，每帧只应用一种转向。</summary>
    /// <param name="player">当前本地玩家，尚未进入世界时为空。</param>
    /// <param name="boss">战斗期间有效的首领目标，其余时间为空。</param>
    private void UpdateWorldHeading(PlayerEntity player, MonsterEntity boss)
    {
        if (_worldDrag) return;
        // 当前帧需要靠近的世界航向，空值表示没有自动转向目标。
        float? heading = _requestedYaw;
        if (!heading.HasValue && boss != null && WorldCameraSettings.Allows(WorldCameraFeature.FaceBoss))
        {
            // 以角色为构图中心，仅绕水平轨道看向首领。
            Vector3 direction = boss.Position - _roleMainTransform.position;
            heading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
        if (!heading.HasValue && player?.NavComp?.IsNavigation == true && WorldCameraSettings.Allows(WorldCameraFeature.TurnOnPath))
            heading = _roleMainTransform.eulerAngles.y;
        if (heading.HasValue)
        {
            _cinemachine.m_XAxis.Value = Mathf.MoveTowardsAngle(_cinemachine.m_XAxis.Value, heading.Value, 180f * Time.deltaTime);
            if (Mathf.Abs(Mathf.DeltaAngle(_cinemachine.m_XAxis.Value, heading.Value)) < 0.01f) _requestedYaw = null;
        }
        else if (WorldCameraSettings.Allows(WorldCameraFeature.TurnWithMovement) && player?.MoveComp?.HasDirectionalInput == true)
        {
            // 沿玩家实际的左右输入转动，正前/正后移动不会改变水平角。
            PlayerInputCtr input = PlayerInputCtr.Instance;
            if (input != null) _cinemachine.m_XAxis.Value += input.Movement.x * 65f * Time.deltaTime;
        }
    }

    /// <summary>将主角实际受击纳入当前战斗镜头，选敌与战斗目标由原控制器维护。</summary>
    /// <param name="attacker">副本回包对应的施害者，可在模型尚未载入时为空。</param>
    public void RecordIncomingHit(MEntity attacker)
    {
        _combatUntil = Time.time + 3f;
        // 保存首领身份，用于主角还没有选中目标的受击时刻。
        MonsterEntity monster = attacker as MonsterEntity;
        if (monster != null && monster.Rank == MonsterRank.Boss) _attackingBoss = monster;
    }

    /// <summary>向已通过场景点击检查的地面点平滑旋转，不改变角色朝向或发起移动。</summary>
    /// <param name="point">鼠标双击命中的世界地面坐标，单位米。</param>
    public void TurnToGround(Vector3 point)
    {
        if (_roleMainTransform == null || _isOpenRoleAtttWindow) return;
        // 角色到点击点的水平分量决定朝向，脚下的零长度方向无须旋转。
        Vector3 direction = point - _roleMainTransform.position;
        if (direction.x != 0f || direction.z != 0f) _requestedYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }

    /// <summary>轻功期间把圆环贴在角色脚下的真实地形上，收招或落点离开地形时隐藏。</summary>
    /// <param name="show">本帧是否正在施放轻功且启用了落点提示。</param>
    private void UpdateGroundHint(bool show)
    {
        // 只命中已有 Geometry 场景层，忽略角色控制器和触发器。
        RaycastHit ground;
        if (show && Physics.Raycast(_roleMainTransform.position + Vector3.up * 0.3f, Vector3.down, out ground,
            100f, LayerMask.GetMask("Geometry"), QueryTriggerInteraction.Ignore))
        {
            if (_groundHint == null)
            {
                _groundHint = Instantiate(Resources.Load<GameObject>("QinggongGroundHint"));
                SceneManager.MoveGameObjectToScene(_groundHint, gameObject.scene);
            }
            _groundHint.transform.SetPositionAndRotation(ground.point + ground.normal * 0.025f,
                Quaternion.FromToRotation(Vector3.back, ground.normal));
            if (!_groundHint.activeSelf) _groundHint.SetActive(true);
        }
        else if (_groundHint != null && _groundHint.activeSelf) _groundHint.SetActive(false);
    }

    /// <summary>仅释放从世界画面开始的拖动，窗口与其他界面保留各自的光标状态。</summary>
    private void ReleaseWorldDrag()
    {
        if (!_worldDrag) return;
        _worldDrag = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>切出游戏时结束右键拖动，回到窗口后需要再次按下右键。</summary>
    /// <param name="focused">游戏窗口拥有桌面焦点时为真。</param>
    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ReleaseWorldDrag();
    }
}
