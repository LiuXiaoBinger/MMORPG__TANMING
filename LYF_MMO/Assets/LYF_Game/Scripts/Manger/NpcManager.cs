using System.Collections.Generic;
using MMoRpgCommon;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 根据 Luban NPC 配置创建场景 NPC，NPC 实体统一由场景实体世界管理。
/// </summary>
public class NpcManager : MonoBehaviour
{

    #region 字段
    [SerializeField, Header("NPC 实例父节点，可为空")] private Transform _npcRoot;

    /// <summary>与对话窗口共用的配置，按出生编号读取该人物的头顶称谓。</summary>
    [SerializeField] private NpcDialogueCatalog _dialogueCatalog;

    private readonly Dictionary<int, NpcEntity> _npcEntities =
        new Dictionary<int, NpcEntity>();

    private bool _initialized;

    private bool _destroyed;

    private int _lifecycleVersion;

    #region 生命周期
    #endregion

    private void Awake()
    {
        // NPC 仍由现有场景入口显式调用 Initialize，避免改变场景加载时序。
    }

    /// <summary>管理器销毁时按实体 ID 从统一世界移除 NPC，并使未完成异步请求失效。</summary>
    private void OnDestroy()
    {
        if (_destroyed)
        {
            return;
        }
        _destroyed = true;
        _initialized = false;
        _lifecycleVersion++;

        List<NpcEntity> entitySnapshot = new List<NpcEntity>(_npcEntities.Values);
        _npcEntities.Clear();
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return;
        }
        for (int index = 0; index < entitySnapshot.Count; index++)
        {
            NpcEntity entity = entitySnapshot[index];
            if (entity != null)
            {
                Global.Instance.SceneWorld.RemoveEntity(entity.UID);
            }
        }
    }

    public void Initialize()
    {
        if (_initialized || _destroyed)
        {
            return;
        }
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            Debug.LogError("场景实体世界尚未初始化，无法创建 NPC。", this);
            return;
        }

        _initialized = true;
        _lifecycleVersion++;
        Dictionary<int, NpcParseInfo> npcInfos = LubanMgr.Instance.GetNpcInfos();
        if (npcInfos == null || npcInfos.Count == 0)
        {
            Debug.LogWarning("没有可创建的 NPC 配置。", this);
            return;
        }

        // 配置字典先复制为快照，创建实体时不依赖字典迭代器。
        List<NpcParseInfo> npcInfoSnapshot = new List<NpcParseInfo>(npcInfos.Values);
        for (int index = 0; index < npcInfoSnapshot.Count; index++)
        {
            CreateNpc(npcInfoSnapshot[index]);

    #region NPC 生成与移除
    #endregion
        }
    }

    private void CreateNpc(NpcParseInfo info)
    {
        if (info == null || _npcEntities.ContainsKey(info.ID))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(info.PrefabPath))
        {
            Debug.LogWarning("NPC " + info.ID + " 没有配置预制体路径。", this);
            return;
        }
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            Debug.LogError("场景实体世界已经失效，无法创建 NPC " + info.ID + "。", this);
            return;
        }

        // 正式 NPC 必须走 World -> Factory -> Builder，确保实体、组件、状态和模型池完整初始化。
        ROObject sceneObject = Global.Instance.SceneWorld.CreateEntity(
            EntityType.Npc, info.ID);
        NpcEntity entity = sceneObject as NpcEntity;
        if (entity == null)
        {
            Debug.LogError("NPC " + info.ID + " 场景实体创建失败。", this);
            return;
        }

        ApplyEntityConfig(entity, info);
        if (_dialogueCatalog != null) entity.NameplateTitle = _dialogueCatalog.GetNameplateTitle(info.ID);
        Vector3 spawnPosition = ResolveSpawnPosition(info);
        ApplyEntityTransform(entity, spawnPosition, info.RotationY);
        _npcEntities.Add(info.ID, entity);

        ulong requestUID = entity.UID;
        int requestVersion = _lifecycleVersion;
        ResourceMgr.Instance.LoadPrefabAsync(info.PrefabPath, npcObject =>
        {
            HandleNpcLoaded(info, entity, requestUID, requestVersion,
                spawnPosition, npcObject);
        });
    }

    /// <summary>异步完成时同时校验管理器生命周期、实体 ID 和世界内对象身份。</summary>
    /// <param name="info">当前 NPC 的名字、配置编号和出生朝向。</param>
    /// <param name="entity">拥有本次加载结果的场景实体。</param>
    /// <param name="requestUID">发起加载时的实体运行时编号。</param>
    /// <param name="requestVersion">发起加载时所属的管理器生命周期。</param>
    /// <param name="spawnPosition">该 NPC 在地图中的出生世界坐标，单位为米。</param>
    /// <param name="npcObject">资源加载器为该实体实例化的模型。</param>
    private void HandleNpcLoaded(NpcParseInfo info, NpcEntity entity,
        ulong requestUID, int requestVersion, Vector3 spawnPosition,
        GameObject npcObject)
    {
        if (!IsCurrentRequest(info.ID, entity, requestUID, requestVersion))
        {
            if (npcObject != null)
            {
                Object.Destroy(npcObject);
            }
            return;
        }
        if (npcObject == null)
        {
            Debug.LogError("NPC " + info.ID + " 预制体加载失败：" +
                info.PrefabPath, this);
            RemoveNpcEntity(info.ID, entity);
            return;
        }

        if (_npcRoot != null)
        {
            npcObject.transform.SetParent(_npcRoot, true);
        }
        if (string.IsNullOrEmpty(info.Name))
        {
            npcObject.name = "NPC_" + info.ID;
        }
        else
        {
            npcObject.name = info.Name;
        }

        // NPC 模型直接绑定已有实体，点击按实体模型层级定位，模型随实体统一回收。
        entity.Model.SetGameObject(npcObject, string.Empty, false);
        // 绑定待机动画会重置模型根节点，完成后再写入配置的世界位置和朝向。
        npcObject.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, info.RotationY, 0f));
        // 根碰撞体与该模型站立高度匹配，供头顶名牌和对话机位共用，单位为米。
        if (npcObject.TryGetComponent(out CharacterController capsule))
        {
            entity.Height = (capsule.center.y + capsule.height * 0.5f) * npcObject.transform.lossyScale.y;
        }
    }

    private void RemoveNpcEntity(int npcId, NpcEntity expectedEntity)
    {
        NpcEntity entity;
        if (!_npcEntities.TryGetValue(npcId, out entity) ||
            !ReferenceEquals(entity, expectedEntity))
        {
            return;
        }
        _npcEntities.Remove(npcId);
        if (Global.Instance != null && Global.Instance.SceneWorld != null)

    #region 查询与判定
    #endregion
        {
            Global.Instance.SceneWorld.RemoveEntity(entity.UID);
        }
    }

    public NpcEntity GetNpc(int npcId)
    {
        NpcEntity entity;
        _npcEntities.TryGetValue(npcId, out entity);
        return entity;
    }

    private Vector3 ResolveSpawnPosition(NpcParseInfo info)
    {
        Vector3 spawnPosition = new Vector3(
            info.Position.X, info.Position.Y, info.Position.Z);
        if (spawnPosition == Vector3.zero)
        {
            // 坐标未配置时使用管理器所在位置，避免 NPC 在场景原点掉出地图。
            spawnPosition = transform.position;
        }
        return spawnPosition;
    }

    private bool IsCurrentRequest(int npcId, NpcEntity entity,
        ulong requestUID, int requestVersion)
    {
        // 生命周期已失效：管理器已销毁或请求版本与当前版本不一致。
        bool lifecycleStale = _destroyed || requestVersion != _lifecycleVersion;
        // 实体无效：实体为空、UID 不匹配或已脱离活跃状态（内部短路保护空引用）。
        bool entityInvalid = entity == null ||
            entity.UID != requestUID ||
            entity.LifecycleState != EntityLifecycleState.Active;
        if (lifecycleStale || entityInvalid)
        {
            return false;
        }
        NpcEntity registeredEntity;
        if (!_npcEntities.TryGetValue(npcId, out registeredEntity) ||
            !ReferenceEquals(registeredEntity, entity))
        {
            return false;
        }
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return false;
        }
        ROObject worldEntity;

    #region NPC 实体配置与位置应用
    #endregion
        if (!Global.Instance.SceneWorld.TryGetEntity(requestUID, out worldEntity))
        {
            return false;
        }
        return ReferenceEquals(worldEntity, entity);
    }

    /// <summary>写入 NPC 配置字段，保留现有对话、商店和地图业务读取方式。</summary>
    private static void ApplyEntityConfig(NpcEntity entity, NpcParseInfo info)
    {
        entity.NpcId = info.ID;
        entity.NpcID = info.ID;
        entity.NpcType = info.Type;
        entity.MapID = info.MapID;
        entity.MapId = info.MapID;
        entity.ShopId = info.ShopId;

        entity.Name = string.Empty;
        if (info.Name != null)
        {
            entity.Name = info.Name;
        }
        entity.PrefabPath = string.Empty;
        if (info.PrefabPath != null)
        {
            entity.PrefabPath = info.PrefabPath;
        }
        entity.Think = string.Empty;
        if (info.Think != null)
        {
            entity.Think = info.Think;
        }
        entity.Talk = string.Empty;
        if (info.Talk != null)
        {
            entity.Talk = info.Talk;
        }
    }

    private static void ApplyEntityTransform(NpcEntity entity,
        Vector3 spawnPosition, float rotationY)
    {
        EntityTransformComponent transformComponent;
        if (!entity.Components.TryGet(EntityComponentType.Scene, out transformComponent))
        {
            return;
        }
    #endregion
        transformComponent.Apply(new EntityTransformSnapshot
        {
            X = spawnPosition.x,
            Y = spawnPosition.y,
            Z = spawnPosition.z,
            Yaw = rotationY
        });
    }

}
