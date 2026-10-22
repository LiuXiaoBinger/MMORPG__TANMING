using Google.Protobuf;
using System.Collections.Generic;
using System.Globalization;
using MMoRpgCommon.SceneEntity;
using UnityEngine;
using SceneObject = ROObject;

/// <summary>主城角色表现管理器，负责把角色模型转换为 Unity 表现对象。</summary>
public class MainCityMgr : MonoBehaviour
{
   /// <summary>正式角色统一使用的 RO 动画控制器 YooAsset Location，包含待机、四移动和常规动作同名槽位。</summary>
   private const string RoleAnimatorControllerLocation =
      "Assets/artres/Resources/Controller/MAnimator.controller";

   /// <summary>主城管理器单例。</summary>
   public static MainCityMgr Instance;

   /// <summary>本主城实例注册的其他玩家同步委托，用于按委托身份精确注销。</summary>
   private OnActionHandler _syncOtherOnlineHandler;

   /// <summary>本主城实例持有的移动同步委托，场景销毁时解除注册。</summary>
   private OnActionHandler _syncMoveHandler;

   /// <summary>本地主角加载完成事件委托，用于在 Global 延迟创建角色后补建主角表现。</summary>
   private System.Action<PlayerEntity> _localRoleLoadCompletedHandler;

   /// <summary>是否已经订阅 Global 的本地主角加载完成事件，避免重复注册。</summary>
   private bool _localRoleLoadSubscribed;

   /// <summary>是否已经发送进入主城请求，避免异步补建模型时重复发送。</summary>
   private bool _enterWorldSent;

   /// <summary>当前已经发起模型加载的本地主角 UID，防止异步完成前重复请求资源。</summary>
   private ulong _localRoleCreateRequestedUid;

   /// <summary>由测试组件首次创建的其他玩家 UID，按角色 ID 索引；真实网络同步同 ID 时会撤销该测试清理所有权。</summary>
   private readonly Dictionary<int, ulong> _testOtherRoleUids =
      new Dictionary<int, ulong>();

   /// <summary>注册主城网络消息。</summary>
   private void Awake()
   {
      Instance = this;
      _syncOtherOnlineHandler = SyncotherOnlineHandle;
      SocketDispatcher.Instance.AddEventHandler(
         NetDefine.CMD_SyncotherOnlineCode, _syncOtherOnlineHandler);
      _syncMoveHandler = SyncMoveHandle;
      SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_SyncMoveCode, _syncMoveHandler);
      _localRoleLoadCompletedHandler = OnLocalRoleLoadCompleted;
   }

   /// <summary>进入主城后创建本地角色表现，并按已加载的配置创建本城 NPC。</summary>
   private void Start()
   {
      TrySubscribeLocalRoleLoad();
      TryCreateCurrentLocalRole();
      // 正式进城前 Global 已完成配置和实体世界初始化，复用同物体上的 NPC 管理器。
      GetComponent<NpcManager>().Initialize();
   }

   /// <summary>每帧尝试补上延迟启用的 Global 事件订阅，兼容测试场景后启用 Global 的时序。</summary>
   private void Update()
   {
      TrySubscribeLocalRoleLoad();
      TryCreateCurrentLocalRole();
   }

   /// <summary>注销主城网络消息并释放单例引用。</summary>
   private void OnDestroy()
   {
      SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SyncMoveCode, _syncMoveHandler);
      _syncMoveHandler = null;
      if (_syncOtherOnlineHandler != null)
      {
         SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_SyncotherOnlineCode, _syncOtherOnlineHandler);
         _syncOtherOnlineHandler = null;
      }
      if (_localRoleLoadSubscribed && Global.Instance != null &&
          _localRoleLoadCompletedHandler != null)
      {
         Global.Instance.LocalRoleLoadCompleted -= _localRoleLoadCompletedHandler;
      }
      _localRoleLoadSubscribed = false;
      _localRoleLoadCompletedHandler = null;
      if (ReferenceEquals(Instance, this))
      {
         Instance = null;
      }
   }

   /// <summary>把服务器移动通知转换为 RO 移动事件，交由既有远端角色组件追赶目标。</summary>
   /// <param name="data">11181 移动通知的 Protobuf 二进制消息体。</param>
   private void SyncMoveHandle(ByteString data)
   {
      // 服务端协议中的角色 ID 使用持久化 ID，位置为世界米、Time 为秒。
      SyncMoveNtf ptc = SyncMoveNtf.Parser.ParseFrom(data);
      // 当前主城世界已存在的角色；移动消息不负责创建额外实体或缓存等待队列。
      RoleEntity role;
      if (!Global.Instance.SceneWorld.TryGetRole(ptc.RoleId, out role) || role.IsPlayer)
      {
         return;
      }
      // Y 轴角度转换成水平单位朝向，沿用 RO 移动事件的 face 字段。
      Vector3 face = Quaternion.Euler(0f, ptc.Face, 0f) * Vector3.forward;
      // 事件池负责本次同步载荷生命周期，分发结束自动归还。
      MMoveEventArgs moveArgs = MMoveEventArgs.Create(ptc.IsMoving,
         new Vector3(ptc.Dx, ptc.Dy, ptc.Dz), face, ptc.IsMoved, ptc.Time, false,
         ptc.JumpCount);
      role.DispatchEvent(MEventType.MEvent_Move, moveArgs);
   }

   /// <summary>订阅 Global 的本地主角加载完成事件，并立即处理已经存在的本地主角。</summary>
   private void TrySubscribeLocalRoleLoad()
   {
      if (_localRoleLoadSubscribed || Global.Instance == null)
      {
         return;
      }

      Global.Instance.LocalRoleLoadCompleted += _localRoleLoadCompletedHandler;
      _localRoleLoadSubscribed = true;
   }

   /// <summary>处理 Global 通知的本地主角，使用本地角色专用预制体并发送一次进场请求。</summary>
   /// <param name="localRole">已经进入 SceneWorld 的本地玩家实体。</param>
   private void OnLocalRoleLoadCompleted(PlayerEntity localRole)
   {
      CreateLocalRole(localRole);
   }

   /// <summary>
   /// 确保本地主角已经发起表现资源加载。
   /// 该入口供 Global 事件和测试角色提交完成后调用，始终使用本地角色模型路径。
   /// </summary>
   /// <param name="localRole">已经由 Global 加入 SceneWorld 的本地玩家实体。</param>
   /// <returns>模型已加载、正在加载或本次已成功发起加载时返回 true。</returns>
   public bool EnsureLocalRolePresentation(PlayerEntity localRole)
   {
      if (localRole == null || localRole.BaseInfo == null)
      {
         Debug.LogError("本地主角表现创建失败：角色实体或基础信息为空。", this);
         return false;
      }

      Debug.Log("开始确保本地主角表现，角色 ID：" + localRole.BaseInfo.RoleId +
         "，职业：" + localRole.BaseInfo.JobId, this);
      CreateLocalRole(localRole);
      if (localRole.Model.IsLoaded || localRole.Model.IsLoading ||
          _localRoleCreateRequestedUid == localRole.UID)
      {
         return true;
      }

      // 实时通道尚未就绪：本地主角按约定等待 KCP 启用，属于正常等待而非加载失败。
      if (!NetSocketMgr.Instance.IsRealtimeReady)
      {
         return false;
      }

      Debug.LogError("本地主角表现未能发起资源加载，角色 ID：" +
         localRole.BaseInfo.RoleId, this);
      return false;
   }

   /// <summary>检查当前场景世界中的本地主角，兼容事件订阅前角色已经加载完成的情况。</summary>
   private void TryCreateCurrentLocalRole()
   {
      if (Global.Instance == null || Global.Instance.SceneWorld == null)
      {
         return;
      }

      CreateLocalRole(Global.Instance.SceneWorld.LocalRole);
   }

   /// <summary>创建本地主角表现；MJS 职业固定使用 Role_MJS，绝不复用其他玩家模型。</summary>
   /// <param name="localRole">本地主角实体，必须已包含职业基础信息。</param>
   private void CreateLocalRole(PlayerEntity localRole)
   {
      if (localRole == null)
      {
         return;
      }
      if (localRole.BaseInfo == null)
      {
         Debug.LogError("本地主角表现创建失败：基础角色信息为空。", this);
         return;
      }
      if (localRole.Model.IsLoaded || localRole.Model.IsLoading)
      {
         return;
      }
      if (_localRoleCreateRequestedUid == localRole.UID)
      {
         return;
      }

      // 移动与技能只走 KCP：确认本地主角信息后、发起模型加载前先请求绑定。
      NetSocketMgr.Instance.RequestRealtimeBind();
      // 通道未就绪时不创建模型，由 Start/Update 的既有重入在下一次尝试；
      // 绑定失败会触发断线恢复并清理角色会话，因此角色不会带着不可用的通道进入可操作状态。
      if (!NetSocketMgr.Instance.IsRealtimeReady)
      {
         return;
      }

      string prefabPath = GetLocalRolePrefabPath(localRole.BaseInfo.JobId);
      _localRoleCreateRequestedUid = localRole.UID;
      Debug.Log("开始加载本地主角模型：" + prefabPath +
         "，角色 ID：" + localRole.BaseInfo.RoleId, this);
      CreateRole(RoleType.MainRole, localRole, prefabPath);
   }

   /// <summary>按职业配置选择本地主角模型资源路径。</summary>
   /// <param name="jobId">服务器返回的职业配置编号。</param>
   /// <returns>本地主角对应的 YooAsset 预制体路径。</returns>
   private string GetLocalRolePrefabPath(int jobId)
   {
      if (jobId == (int)RoleJobtype.MJS)
      {
         return "Role/Role_MJS";
      }
      return "Role/Role_JX";
   }

   /// <summary>处理其他玩家同步消息并更新角色世界模型。</summary>
   private void SyncotherOnlineHandle(ByteString data)
   {
      // 网络层只负责反序列化，角色世界写入和表现实例化统一委托给正式业务入口。
      RoleBaseInfo roleBaseInfo = RoleBaseInfo.Parser.ParseFrom(data);
      HandleOtherRoleOnline(roleBaseInfo);
   }

   /// <summary>
   /// 按服务器同步的角色基础信息创建或更新其他玩家，并在首次进入主城时绑定角色表现。
   /// 该入口同时供网络协议和专用测试组件调用，保证两者走完全相同的场景实体链路。
   /// </summary>
   /// <param name="roleBaseInfo">服务器下发的角色 ID、昵称、职业等基础同步数据。</param>
   /// <returns>世界中对应的其他玩家实体；数据无效或与本地角色相同则返回 null。</returns>
   public RoleEntity HandleOtherRoleOnline(RoleBaseInfo roleBaseInfo)
   {
      // 真实网络同步到达后，即使实体 UID 未变化也必须撤销测试组件的删除所有权。
      if (roleBaseInfo != null)
      {
         _testOtherRoleUids.Remove(roleBaseInfo.RoleId);
      }
      // 输出值记录本次是否首次创建实体；网络路径只使用它满足共享核心接口，不登记测试所有权。
      bool isCreated;
      return HandleOtherRoleOnlineCore(roleBaseInfo, out isCreated);
   }

   /// <summary>
   /// 通过与网络相同的主城核心链路模拟其他玩家上线，并仅在测试首次创建实体时登记其清理所有权。
   /// 该方法只供 `UI/test` 下的专用测试组件调用，不得挂载到正式场景或正式预制体。
   /// </summary>
   /// <param name="roleBaseInfo">测试构造的角色基础同步数据，字段结构与服务器协议保持一致。</param>
   /// <returns>世界中对应的其他玩家实体；数据无效或与本地角色相同则返回 null。</returns>
   public RoleEntity HandleTestOtherRoleOnline(RoleBaseInfo roleBaseInfo)
   {
      // 核心入口返回是否首次创建，测试所有权只在该情况下登记，绝不接管原有真实角色。
      // 输出值决定是否登记测试所有权，已有真实角色或重复测试同步均不会登记。
      bool isCreated;
      // 共享核心返回当前场景世界中已创建或已更新的其他玩家实体。
      RoleEntity otherRole = HandleOtherRoleOnlineCore(roleBaseInfo, out isCreated);
      if (isCreated && otherRole != null)
      {
         _testOtherRoleUids[otherRole.GetID()] = otherRole.UID;
      }
      return otherRole;
   }

   /// <summary>判断指定角色实体是否仍归当前测试组件创建并拥有清理权。</summary>
   /// <param name="roleId">角色持久化 ID。</param>
   /// <param name="roleUid">角色当前场景实例 UID。</param>
   /// <returns>角色 ID 和 UID 同时匹配测试登记记录时返回 true。</returns>
   public bool IsTestOtherRoleOwner(int roleId, ulong roleUid)
   {
      // 已登记的测试实体 UID，用于避免角色 ID 相同但场景实例已变化时误删。
      ulong ownedRoleUid;
      if (!_testOtherRoleUids.TryGetValue(roleId, out ownedRoleUid))
      {
         return false;
      }
      return ownedRoleUid == roleUid;
   }

   /// <summary>移除仍归测试组件所有的其他玩家实体，任何真实同步接管或 UID 不匹配都会拒绝删除。</summary>
   /// <param name="roleId">测试组件记录的角色持久化 ID。</param>
   /// <param name="roleUid">测试组件记录的首次创建实体 UID。</param>
   /// <returns>成功移除测试角色返回 true；已被真实同步接管或实体不存在时返回 false。</returns>
   public bool RemoveTestOtherRole(int roleId, ulong roleUid)
   {
      // 先验证测试登记 UID，避免测试组件删除同 ID 的真实角色。
      if (!IsTestOtherRoleOwner(roleId, roleUid))
      {
         return false;
      }

      // 当前主城使用的统一场景实体世界，可能因切场景而为空。
      SceneWorldSystem sceneWorld = null;
      if (Global.Instance != null)
      {
         sceneWorld = Global.Instance.SceneWorld;
      }
      if (sceneWorld == null)
      {
         _testOtherRoleUids.Remove(roleId);
         return false;
      }

      // 按角色 ID 查到的当前实体，必须继续校验其 UID。
      RoleEntity currentRole;
      if (!sceneWorld.TryGetRole(roleId, out currentRole) || currentRole == null ||
          currentRole.UID != roleUid)
      {
         // 角色已离线或被替换时丢弃旧测试所有权，禁止继续删除。
         _testOtherRoleUids.Remove(roleId);
         return false;
      }

      // 所有权与世界实体均匹配后才移除，并在移除前清理登记避免重入重复删除。
      _testOtherRoleUids.Remove(roleId);
      return sceneWorld.RemoveOtherRole(roleId);
   }

   /// <summary>
   /// 执行其他玩家同步的共享核心流程：写入场景世界、判断首次创建并绑定 Unity 表现。
   /// 网络入口与测试入口都只能通过本方法访问该业务逻辑。
   /// </summary>
   /// <param name="roleBaseInfo">待同步的角色基础数据。</param>
   /// <param name="isCreated">输出本次是否在场景世界首次创建角色实体。</param>
   /// <returns>世界中对应的其他玩家实体；不可创建时返回 null。</returns>
   private RoleEntity HandleOtherRoleOnlineCore(RoleBaseInfo roleBaseInfo,
      out bool isCreated)
   {
      // 默认标记为未创建，所有提前返回分支都会保留该安全值。
      isCreated = false;
      // 场景世界尚未建立时不能创建实体，等待后续真实同步或测试组件再次触发。
      if (roleBaseInfo == null || Global.Instance == null || Global.Instance.SceneWorld == null)
      {
         return null;
      }

      // 当前角色自身不应作为“其他在线角色”重复创建。
      SceneWorldSystem sceneWorld = Global.Instance.SceneWorld;
      if (sceneWorld.LocalRole != null &&
          sceneWorld.LocalRole.GetID() == roleBaseInfo.RoleId)
      {
         return null;
      }

      // 先记录实体是否已存在；已有角色仅更新同步数据，禁止重复请求预制体和重复实例化。
      RoleEntity existingRole;
      bool alreadyExists = sceneWorld.TryGetRole(
         roleBaseInfo.RoleId, out existingRole);
      RoleEntity otherRole = sceneWorld.AddOrUpdateOtherRole(roleBaseInfo);
      if (otherRole == null)
      {
         return null;
      }

      // 只有本次首次进入世界的角色需要创建 Unity 表现对象。
      if (!alreadyExists)
      {
         CreateRole(RoleType.OtherRole, otherRole, "Role/Role_MJS_Other");
         isCreated = true;
      }
      return otherRole;
   }

   /// <summary>异步加载角色预制体并初始化表现控制器。</summary>
   /// <param name="roleType">决定本地出生、初次入场或远端同步表现的角色类别。</param>
   /// <param name="role">当前主城世界已创建且持有本次模型的角色实体。</param>
   /// <param name="prefabPath">角色表现预制体的YooAsset加载地址。</param>
   private void CreateRole(RoleType roleType, SceneObject role, string prefabPath)
   {
      if (role == null)
      {
         Debug.LogError("角色表现加载失败：场景角色实体为空，资源路径：" + prefabPath, this);
         return;
      }
      RoleBaseInfo baseInfo = null;
      PlayerEntity playerEntity = role as PlayerEntity;
      if (playerEntity != null)
      {
         baseInfo = playerEntity.BaseInfo;
      }
      RoleEntity roleEntity = role as RoleEntity;
      if (roleEntity != null)
      {
         baseInfo = roleEntity.BaseInfo;
      }
      if (baseInfo == null)
      {
         Debug.LogError("角色表现加载失败：角色基础信息为空，资源路径：" + prefabPath, this);
         ResetLocalRoleRequest(roleType, role.UID);
         return;
      }

      SceneWorldSystem requestedWorld = null;
      if (Global.Instance != null)
      {
         requestedWorld = Global.Instance.SceneWorld;
      }
      ulong requestedUID = role.UID;
      if (requestedWorld == null || requestedUID == 0UL ||
          role.LifecycleState != EntityLifecycleState.Active)
      {
         Debug.LogError("角色表现加载失败：场景实体状态无效，角色 UID：" +
            requestedUID + "，资源路径：" + prefabPath, this);
         ResetLocalRoleRequest(roleType, requestedUID);
         return;
      }

      // 异步请求只持有本次实体身份，禁止读取池复用后的可变字段。
      SceneObject requestedEntity = role;
       if (ResourceMgr.Instance == null || Global.Instance == null ||
           Global.Instance.YooPackage == null)
       {
          Debug.LogError("角色表现加载失败：资源管理器或 YooAsset 资源包未就绪，资源路径：" +
             prefabPath, this);
          ResetLocalRoleRequest(roleType, requestedUID);
          return;
       }

       try
       {
          ResourceMgr.Instance.LoadPrefabAsync(prefabPath, obj =>
         {
            if (obj == null)
            {
               Debug.LogError("角色表现资源加载回调为空，资源路径：" + prefabPath,
                  this);
               ResetLocalRoleRequest(roleType, requestedUID);
               return;
            }

         SceneObject currentEntity = null;
         // 全局实例就绪且与请求所指场景世界为同一世界。
         bool worldReady = Global.Instance != null &&
             ReferenceEquals(Global.Instance.SceneWorld, requestedWorld);
         // 请求 UID 仍能取回同一活跃实体（TryGetEntity 为纯字典查询，内部短路保护后续引用）。
         bool entityReady = requestedWorld.TryGetEntity(requestedUID, out currentEntity) &&
             ReferenceEquals(currentEntity, requestedEntity) &&
             currentEntity.LifecycleState == EntityLifecycleState.Active;
         // 仍处于当前请求对应的活跃实体上，才继续绑定模型。
         bool isCurrentRequest = worldReady && entityReady;
         if (!isCurrentRequest)
         {
            DestroyLoadedRole(obj);
            return;
         }

         // 当前仍处于场景世界中的角色实体；模型与移动组件均由该实体直接持有。
         MEntity sceneEntity = currentEntity as MEntity;
         // 角色的场景变换组件，用于保存模型初始位置和朝向快照。
         EntityTransformComponent transformComponent = null;
         bool canBind = sceneEntity != null && sceneEntity.Model != null &&
             currentEntity.Components.TryGet(EntityComponentType.Scene, out transformComponent);
         if (!canBind)
         {
            DestroyLoadedRole(obj);
            return;
         }

          try
          {
            if (roleType == RoleType.MainRole)
            {
               obj.transform.position = new Vector3(62.1493416f, 19.4139996f, 80.6689758f);
            }
            else
            {
               // 远端玩家入场消息已经携带服务器当前位置，沿现有网络坐标转换读取世界米数。
               System.Numerics.Vector3 serverPosition = baseInfo.Pos.ToVector3();
               obj.transform.position = new Vector3(serverPosition.X, serverPosition.Y, serverPosition.Z);
            }
            transformComponent.Apply(new EntityTransformSnapshot
            {
               X = obj.transform.position.x,
               Y = obj.transform.position.y,
               Z = obj.transform.position.z,
               Yaw = obj.transform.eulerAngles.y
            });

            // 所有校验和数据初始化完成后才转交模型对象持有，失败路径由本方法销毁实例。
            sceneEntity.Model.SetGameObject(obj,
               RoleAnimatorControllerLocation, false);
            if (roleType == RoleType.MainRole)
            {
               BindMainRoleCompatibilityController(obj, sceneEntity, baseInfo);
               // 主界面先于异步主角模型创建，在输入组件就绪后补齐技能和界面快捷键订阅。
               UIRoot.Instance.RegisterMainUIKeyHandler();
            }
            else
            {
               // 正式其他玩家预制体上的旧兼容控制器绑定实体后停止自己的重力与根运动写入。
               OtherRoleCtrl otherRoleController = obj.GetComponent<OtherRoleCtrl>();
               otherRoleController.InitCtrl(RoleType.OtherRole, baseInfo);
               otherRoleController.SetROObject(sceneEntity);
            }
            if (roleType == RoleType.MainRole && CameraMgr.Instance != null)
            {
               // 主角相机直接跟踪模型根节点和 LookAtPos，不再依赖旧 MainRoleCtrl。
               Transform lookAtTransform = FindChildTransform(obj.transform, "LookAtPos");
               CameraMgr.Instance.InitCamera(obj.transform, lookAtTransform);
            }
            if (roleType == RoleType.MainRole && !_enterWorldSent)
            {
               // 模型已按出生点定位，出生坐标随这次 TCP 入场请求一起上报：
               // 实时协议已改走 KCP，入场前的移动上报与入场请求没有先后保证，
               // 服务器必须拿到出生坐标才能在附近过滤前写入角色位置。
               // 本次入场使用的出生坐标，取已就位的本地主角实体位置（世界米）。
               Vector3 spawnPosition = sceneEntity.Position;
               // 本次 TCP 入场请求，携带角色 ID 与出生坐标，服务器在附近过滤前写入角色位置。
               EnterWroldReq request = new EnterWroldReq();
               request.RoleId = baseInfo.RoleId;
               // 沿用正式基础快照的下划线坐标格式（世界米），与移动上报写入的位置同一格式。
               request.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}",
                  spawnPosition.x, spawnPosition.y, spawnPosition.z);
               NetSocketMgr.Client.SendData(NetDefine.CMD_EnterWroldCode, request.ToByteString());
               _enterWorldSent = true;
            }
            Debug.Log("角色表现绑定成功，角色 UID：" + requestedUID +
               "，资源路径：" + prefabPath, this);
          }
          catch (System.Exception exception)
          {
            Debug.LogError("角色表现绑定失败，实体ID：" + requestedUID + "，" + exception);
            DestroyLoadedRole(obj);
            ResetLocalRoleRequest(roleType, requestedUID);
          }
         });
       }
       catch (System.Exception exception)
       {
          Debug.LogError("角色表现资源请求异常，资源路径：" + prefabPath +
             "，异常：" + exception, this);
          ResetLocalRoleRequest(roleType, requestedUID);
       }
   }

   /// <summary>
   /// 为本地主角保留旧控制器的技能、选中和界面兼容职责，并绑定新的场景实体。
   /// 场景实体激活后，旧控制器自身会跳过移动、重力和根运动，避免与 MMoveComponent 双重控制。
   /// </summary>
   /// <param name="roleObject">已经实例化且即将交由场景实体持有的主角对象。</param>
   /// <param name="sceneEntity">拥有移动、跳跃和动画权威的本地主角场景实体。</param>
   /// <param name="baseInfo">服务器下发的主角基础信息，供旧技能和选中逻辑读取角色状态。</param>
   private static void BindMainRoleCompatibilityController(GameObject roleObject,
      MEntity sceneEntity, RoleBaseInfo baseInfo)
   {
      if (roleObject == null || sceneEntity == null || baseInfo == null)
      {
         return;
      }

      // 当前主角兼容控制器；预制体未挂载时在运行时补齐一次，不创建重复组件。
      MainRoleCtrl mainRoleController = roleObject.GetComponent<MainRoleCtrl>();
      if (mainRoleController == null)
      {
         mainRoleController = roleObject.AddComponent<MainRoleCtrl>();
      }
      mainRoleController.InitCtrl(RoleType.MainRole, baseInfo);
      mainRoleController.SetROObject(sceneEntity);
   }

   /// <summary>按名称在模型层级中查找相机注视点，找不到时由调用方回退到模型根节点。</summary>
   /// <param name="rootTransform">需要搜索的角色模型根节点。</param>
   /// <param name="targetName">目标子节点名称，例如 LookAtPos。</param>
   /// <returns>找到的子节点；模型为空或不存在该节点时返回 null。</returns>
   private static Transform FindChildTransform(Transform rootTransform, string targetName)
   {
      if (rootTransform == null || string.IsNullOrEmpty(targetName))
      {
         return null;
      }

      // 模型层级内的全部节点快照；数组支持按索引遍历并避免使用 foreach。
      Transform[] childTransforms = rootTransform.GetComponentsInChildren<Transform>(true);
      for (int childIndex = 0; childIndex < childTransforms.Length; childIndex++)
      {
         // 当前待比较的模型节点；引用仍指向真实 Transform，供相机直接跟随。
         Transform childTransform = childTransforms[childIndex];
         if (childTransform != null && childTransform.name == targetName)
         {
            return childTransform;
         }
      }
      return null;
   }

   /// <summary>清理失败的本地主角请求，使后续帧或测试入口可以重新尝试加载。</summary>
   /// <param name="roleType">本次表现所属角色类型。</param>
   /// <param name="roleUid">本次资源请求绑定的场景实体 UID。</param>
   private void ResetLocalRoleRequest(RoleType roleType, ulong roleUid)
   {
      if (roleType == RoleType.MainRole && _localRoleCreateRequestedUid == roleUid)
      {
         _localRoleCreateRequestedUid = 0UL;
      }
   }

   /// <summary>销毁未能绑定到有效场景实体的本次异步加载实例。</summary>
   private static void DestroyLoadedRole(GameObject roleObject)
   {
      if (roleObject != null)
      {
         UnityEngine.Object.Destroy(roleObject);
      }
   }
}
