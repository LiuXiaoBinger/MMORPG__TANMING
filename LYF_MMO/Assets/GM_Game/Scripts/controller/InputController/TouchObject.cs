using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>沿RO点击入口选择角色或敌人，点击NPC时交给目标导航；空白地面不产生业务指令。</summary>
public sealed class TouchObject
{
    /// <summary>RO 同名点击入口，由现有玩家输入组件每帧采集鼠标驱动。</summary>
    public static readonly TouchObject singleton = new TouchObject();
    /// <summary>沿 RO 复用三十二条射线命中缓冲，点击时不创建命中数组。</summary>
    private readonly RaycastHit[] _hitEntityBuffer = new RaycastHit[32];
    /// <summary>当前世界的实体引用快照容器，仅点击时更新。</summary>
    private readonly List<MEntity> _entities = new List<MEntity>();
    /// <summary>UI 命中结果复用列表，用于防止点击技能或瞄准按钮时点穿到场景。</summary>
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
    /// <summary>同一 EventSystem 的指针载荷，切换 EventSystem 时重建。</summary>
    private PointerEventData _pointerData;
    /// <summary>指针载荷所属的 EventSystem，确保切场景后不复用旧系统。</summary>
    private EventSystem _eventSystem;
    /// <summary>鼠标按下命中的原实体引用，松开或禁用输入后清除。</summary>
    private MEntity _downEntity;
    /// <summary>本次按下的屏幕像素坐标，用于区分拖动镜头与点击实体。</summary>
    private Vector2 _lastPos;

    /// <summary>单例只处理选择输入，不挂载正式场景组件。</summary>
    private TouchObject() { }

    /// <summary>清除未结束的点击状态，禁用输入与换角色时调用。</summary>
    public void Clear()
    {
        _downEntity = null;
        _entities.Clear();
        _uiHits.Clear();
    }

    /// <summary>RO 同名实体命中入口，适配本项目现有屏幕坐标与 Unity 点击阶段。</summary>
    /// <param name="position">点击的屏幕像素坐标。</param>
    /// <param name="phase">Began 记录按下对象，Ended 对账并选择。</param>
    /// <param name="selectOnly">保留RO参数；角色点击仅选择，NPC点击导航交互，均不自动攻击。</param>
    /// <returns>本次点击完成角色选择或提交NPC导航时为true。</returns>
    public bool JudegeType(Vector2 position, TouchPhase phase, bool selectOnly = true)
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            Clear();
            return false;
        }
        if (IsPointerOverUI(position))
        {
            Clear();
            return false;
        }
        // 当前活动主相机，Single 切场景后不持有旧相机。
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }
        // 相机发射的世界射线，沿 RO 只检查可选择实体的碰撞体。
        Ray ray = camera.ScreenPointToRay(position);
        // 使用全部层后按真实实体模型映射筛选，避免依赖本项目未配置的 RO 选择层。
        int hitCount = Physics.RaycastNonAlloc(ray, _hitEntityBuffer,
            Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Global.Instance.SceneWorld.CopyEntities(_entities);
        // 最近有效实体，同距离优先首次射线命中。
        MEntity selectEnt = null;
        // 当前最近实体碰撞点距相机的米数。
        float selectDistance = float.MaxValue;
        // 逐项按碰撞体模型层级定位世界中的原实体。
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            // 本次命中碰撞体的变换，可位于角色模型子节点。
            Transform hitTransform = _hitEntityBuffer[hitIndex].collider.transform;
            // 原实体快照的索引，查询不修改实体世界集合。
            for (int entityIndex = 0; entityIndex < _entities.Count; entityIndex++)
            {
                // 当前候选沿现有实体模型定位，NPC独立于战斗目标筛选。
                MEntity entity = _entities[entityIndex];
                // 当前命中就是本地玩家自身，不能作为点击选择目标。
                bool isSelfEntity = entity == Global.Instance.SceneWorld.LocalRole;
                if (isSelfEntity)
                {
                    continue;
                }
                if (entity is NpcEntity)
                {
                    // NPC没有战斗死亡判定，点击只要求原世界实体活跃且模型可见。
                    if (entity.LifecycleState != EntityLifecycleState.Active ||
                        !entity.Model.IsLoaded || !entity.Model.Visible)
                    {
                        continue;
                    }
                    if (!entity.Model.GameObject.NativeObject.activeInHierarchy)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!(entity is RoleEntity) && !MSkillTargetDefine.IsEnemy(entity))
                    {
                        continue;
                    }
                    if (!MSkillTargetDefine.IsValidTarget(entity))
                    {
                        continue;
                    }
                }
                if (hitTransform.IsChildOf(entity.Model.GameObject.Transform) &&
                    _hitEntityBuffer[hitIndex].distance < selectDistance)
                {
                    selectEnt = entity;
                    selectDistance = _hitEntityBuffer[hitIndex].distance;
                }
            }
        }
        if (phase == TouchPhase.Began)
        {
            _downEntity = selectEnt;
            _lastPos = position;
            return false;
        }
        if (phase == TouchPhase.Ended)
        {
            // 角色与敌人保持原按下松开同一实体的选择规则。
            bool sameEntity = selectEnt != null && selectEnt == _downEntity;
            _downEntity = null;
            if (sameEntity)
            {
                if (selectEnt is NpcEntity)
                {
                    // 新增NPC交互仅接受10像素内的点击，拖动镜头不能发起导航。
                    if ((position - _lastPos).sqrMagnitude > 100f)
                    {
                        return false;
                    }
                    return ClickNpc(selectEnt);
                }
                return setPlayerTarget(selectEnt, selectOnly);
            }
        }
        return false;
    }

    /// <summary>点击现有NPC时沿目标导航走进原对话距离，不改写战斗选择目标。</summary>
    /// <param name="entity">当前点击的原NPC实体；通过基类读取世界坐标，避开NPC旧字符串位置字段。</param>
    /// <returns>已提交本次NPC导航事件时为true。</returns>
    private bool ClickNpc(MEntity entity)
    {
        // 只在同步派发期间借用事件；导航组件保存目标原引用，不保存池化载荷。
        MNavigationEventArgs args = MNavigationEventArgs.Create(entity.Position);
        args.NeedTarget = true;
        args.Target = entity;
        args.UpperRange = NpcCtrl.TalkDistance;
        Global.Instance.SceneWorld.LocalRole.DispatchEvent(MEventType.MEvent_Navigation,
            args);
        return true;
    }

    /// <summary>检查点击位置上的 UI Graphic，包含本帧鼠标位置而不依赖旧指针缓存。</summary>
    /// <param name="position">当前点击的屏幕像素坐标。</param>
    /// <returns>命中 UI 时为 true，世界点击应被取消。</returns>
    private bool IsPointerOverUI(Vector2 position)
    {
        if (EventSystem.current == null)
        {
            return false;
        }
        if (_eventSystem != EventSystem.current)
        {
            _eventSystem = EventSystem.current;
            _pointerData = new PointerEventData(_eventSystem);
        }
        _pointerData.Reset();
        _pointerData.position = position;
        _uiHits.Clear();
        _eventSystem.RaycastAll(_pointerData, _uiHits);
        // 仅 GraphicRaycaster 属于 UI；PhysicsRaycaster 的场景命中不阻断实体选择。
        for (int index = 0; index < _uiHits.Count; index++)
        {
            if (_uiHits[index].module is UnityEngine.UI.GraphicRaycaster)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>沿 RO 点击写入目标前检查最大选择距离，普通角色可被查看但不会被自动认作敌人。</summary>
    /// <param name="entity">按下与松开均命中的当前世界实体。</param>
    /// <param name="selectOnly">保留点击选择参数，本轮不触发自动普攻。</param>
    /// <returns>在三十米内且已写入目标时为 true。</returns>
    private bool setPlayerTarget(MEntity entity, bool selectOnly)
    {
        if ((entity.Position - Global.Instance.SceneWorld.LocalRole.Position).sqrMagnitude >
            MSkillTargetMgr.MaxChooseDistance * MSkillTargetMgr.MaxChooseDistance)
        {
            return false;
        }
        MSkillTargetMgr.singleton.ResetTarget(entity, true);
        return true;
    }
}
