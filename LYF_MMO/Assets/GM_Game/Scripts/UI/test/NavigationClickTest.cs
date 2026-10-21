#if UNITY_EDITOR
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>仅供编辑器运行时临时挂载的点地导航测试，复用正式算路和移动事件。</summary>
[DisallowMultipleComponent]
public sealed class NavigationClickTest : MonoBehaviour
{
    /// <summary>点击允许的最大屏幕位移平方，100对应10像素，超过即按拖动处理。</summary>
    private const float ClickDistanceSquared = 100f;
    /// <summary>测试点击的最远地形射线长度，单位米。</summary>
    private const float RayDistance = 1000f;
    /// <summary>实体碰撞查询缓冲，与正式选择入口同样最多保留32次命中。</summary>
    private readonly RaycastHit[] _entityHits = new RaycastHit[32];
    /// <summary>点击时复制的当前世界实体原引用，用于避免点实体同时发出地面导航。</summary>
    private readonly List<MEntity> _entities = new List<MEntity>();
    /// <summary>复用的UI命中结果，防止点击技能按钮等控件时点穿。</summary>
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
    /// <summary>当前EventSystem的屏幕指针载荷，系统更换时重建。</summary>
    private PointerEventData _pointerData;
    /// <summary>指针载荷所属的UI事件系统。</summary>
    private EventSystem _eventSystem;
    /// <summary>本轮按下的屏幕像素位置，松开前持续检查拖动幅度。</summary>
    private Vector2 _downPosition;
    /// <summary>本轮按下是否始终满足空白地图点击条件，松开后清除。</summary>
    private bool _canClick;

    /// <summary>用现有Input System鼠标边沿采集测试点击，不注册任何正式输入事件。</summary>
    private void Update()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            _canClick = false;
            return;
        }
        if (PlayerInputCtr.Instance == null || !PlayerInputCtr.Instance.isActiveAndEnabled)
        {
            _canClick = false;
            return;
        }
        // 本帧鼠标设备，桌面测试沿正式选择入口使用相同设备。
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            _canClick = false;
            return;
        }
        // 鼠标当前屏幕像素位置，UI与拖动检测共用。
        Vector2 position = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            _downPosition = position;
            _canClick = !IsPointerOverUI(position) && !HitsEntity(position);
        }
        if (_canClick && (position - _downPosition).sqrMagnitude > ClickDistanceSquared)
        {
            _canClick = false;
        }
        if (!mouse.leftButton.wasReleasedThisFrame)
        {
            return;
        }
        // 松开即结束本轮点击，避免下一次鼠标事件继承旧状态。
        bool clickGround = _canClick;
        _canClick = false;
        if (!clickGround || IsPointerOverUI(position) || HitsEntity(position))
        {
            return;
        }
        // 当前地图相机；未进入游戏相机时不能投射测试目的地。
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }
        // 沿正式导航管理器的Geometry地形射线取得世界目的点。
        Ray ray = camera.ScreenPointToRay(position);
        // 射线命中的世界目的点，单位米；失败不派发导航。
        Vector3 destination;
        if (!MNavigationMgr.singleton.TryRaycastTerrain(ray.origin,
            ray.GetPoint(RayDistance), out destination))
        {
            return;
        }
        // 池化参数同步交给角色分发器回收，测试组件不持有事件生命周期。
        MNavigationEventArgs args = MNavigationEventArgs.Create(destination);
        Global.Instance.SceneWorld.LocalRole.DispatchEvent(MEventType.MEvent_Navigation, args);
    }

    /// <summary>检查当前坐标是否命中UI图形，与正式点击入口保持相同点穿规则。</summary>
    /// <param name="position">当前屏幕像素坐标。</param>
    /// <returns>命中UI Graphic时返回true。</returns>
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
        // PhysicsRaycaster产生的世界碰撞结果不属于UI阻挡。
        for (int index = 0; index < _uiHits.Count; index++)
        {
            if (_uiHits[index].module is UnityEngine.UI.GraphicRaycaster)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>排除实体模型点击，防止测试地面导航覆盖正式角色选择或NPC交互。</summary>
    /// <param name="position">本次按下或松开的屏幕像素坐标。</param>
    /// <returns>射线命中当前世界任一已加载实体模型时返回true。</returns>
    private bool HitsEntity(Vector2 position)
    {
        // 当前主相机，切场景时不保留旧相机引用。
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }
        // 世界射线沿正式选择入口检查全部默认碰撞层及触发器。
        Ray ray = camera.ScreenPointToRay(position);
        // 此次射线实际写入缓冲的有效命中数量。
        int hitCount = Physics.RaycastNonAlloc(ray, _entityHits, RayDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Global.Instance.SceneWorld.CopyEntities(_entities);
        // 按索引定位碰撞体所属的原实体模型。
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            // 碰撞体可以位于模型子节点，沿模型根判断归属。
            Transform hitTransform = _entityHits[hitIndex].collider.transform;
            // 包含本地玩家、其他角色、NPC和敌人，任何实体点击都不用于地面测试。
            for (int entityIndex = 0; entityIndex < _entities.Count; entityIndex++)
            {
                // 当前实体仍为世界集合里的原引用，没有复制业务状态。
                MEntity entity = _entities[entityIndex];
                if (entity.Model.IsLoaded &&
                    hitTransform.IsChildOf(entity.Model.GameObject.Transform))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
#endif
