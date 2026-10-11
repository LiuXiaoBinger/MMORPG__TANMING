# RO 风格实体、MModel、MAnimator 与角色状态机设计

## 1. 本轮结论

本轮重新按 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient` 的真实职责设计，废弃上一版独立表现中间层方案。

本项目采用以下固定关系：

```text
MObject
  └── MSceneObject
        └── MEntity
              ├── MModel Model
              │     └── MAnimator Ator
              └── MStateMachine Machine
```

`MModel` 是实体的模型表现对象，直接由 `MEntity` 创建和持有；`MAnimator` 是 `MModel` 的动画封装。实体普通组件、动作状态和模型表现组件是三条不同的职责链，不能相互替代。

本轮只做设计，不修改客户端、服务端、协议、配置表、场景或预制体业务代码。实现时仍遵守：新增代码不声明命名空间、不使用 `foreach` 和三目运算符；新增顶级类型一类型一文件；客户端和服务端业务代码由对应子 Agent 修改。

## 2. RO 参考结构核对

参考项目的实体层级为：

```text
MObject
  └── MSceneObject
        └── MEntity
              ├── RoleEntity             对应 RO 的 MRole
              │     ├── PlayerEntity      对应 RO 的 MPlayer
              │     └── MirrorRoleEntity  对应 RO 的 MMirrorRole
              ├── MonsterEntity           对应 RO 的 MMonster
              │     ├── EliteEntity
              │     └── BossEntity
              ├── NpcEntity               对应 RO 的 MNpc
              ├── DummyEntity             对应 RO 的 MDummy
              ├── TrapEntity
              ├── PetEntity / EagleEntity / MercenaryEntity
              └── CollectionEntity
```

飞行物使用独立的实体派生类，但仍挂在同一实体世界中。`MSceneObject` 负责场景位置、旋转和显示；参考代码中的 `_model` 位于 `MSceneObject`，本项目按用户要求上移为 `MEntity` 的直接成员，以便所有实体统一拥有模型对象。

参考 `MModel.cs` 的职责包括模型加载、`MGameObject` 持有、Animator 查找、显示隐藏、位置/旋转/缩放、阴影、挂点、换装、材质和回收；参考 `MAnimator.cs` 不是实体，也不是状态机，而是对 Unity `Animator` 的可池化封装。

## 3. 完整类结构

### 3.1 实体继承树

```text
ROObject                         兼容契约，不作为新的表现层
  └── MObject                    事件、生命周期、普通组件
        └── MSceneObject         场景 Transform 与可见性
              └── MEntity        实体 ID、配置、MModel、专用组件、状态机
                    ├── MRoleEntity
                    │     ├── PlayerEntity
                    │     ├── RoleEntity
                    │     └── MirrorRoleEntity
                    ├── MonsterEntity
                    │     ├── EliteEntity
                    │     └── BossEntity
                    ├── NpcEntity
                    ├── DummyEntity
                    ├── TrapEntity
                    ├── PetEntity
                    ├── EagleEntity
                    ├── CollectionEntity
                    ├── MercenaryEntity
                    └── ProjectileEntity
                          ├── AnimProjectileEntity
                          ├── RectProjectileEntity
                          ├── RingProjectileEntity
                          └── TargetProjectileEntity
```

`OnlineRole` 仍然是服务器在线角色数据根，不继承客户端实体；服务器只同步实体 ID、类型、配置和逻辑状态。

RO 中还存在 `MPubVehicle`、`MSoul`、`MSceneTriggerObject`、`MDrop` 等场景对象。本项目本轮只纳入用户明确列出的角色、怪物、NPC、陷阱、宠物、采集物、佣兵和飞行物；上述对象若后续接入，统一直接继承 `MSceneObject` 或 `MEntity`，不改变 `MEntity → MModel → MAnimator` 关系。

### 3.2 `MObject`、`MSceneObject`、`MEntity`

```text
MObject
 ├── EntityId
 ├── LifecycleState
 ├── MEventDispatcher EventDispatcher
 ├── EntityComponentContainer Components
 ├── Initialize/Dispose/Destroy
 └── Update(int deltaMilliseconds)

MSceneObject : MObject
 ├── Position / Rotation / Scale
 ├── Visible / Active
 ├── SceneId
 └── SetPosition/SetRotation/SetVisible

MEntity : MSceneObject
 ├── ConfigId / EntityType
 ├── protected MModel _model
 ├── public MModel Model
 ├── EntityMoveComponent MoveComp
 ├── EntityBuffComponent Buff
 ├── EntitySkillComponent Skill
 ├── MStateMachine Machine
 ├── MActionStateContainer ActionStates
 ├── AttachModel/DetachModel
 ├── Update/LateUpdate
 └── ResetForPool
```

`MEntity.Initialize` 必须先创建 `MModel`，再由 `MComponentMgr` 获取 `MoveComp`、`Buff`、`Skill`、`Machine` 四个快捷组件。四个快捷组件不进入 `EntityComponentContainer`，也不允许通过普通组件查询接口重复添加。

### 3.3 模型表现类

```text
MGameObject
 ├── UnityEngine.GameObject NativeObject
 ├── Transform Transform
 ├── SetActive/SetParent
 └── Destroy/Release

MModel
 ├── MGameObject _mObj
 ├── MAnimator _mAtor
 ├── MModelOriginData OriginData
 ├── MModelLoadState LoadState
 ├── MModelLoadMgr LoadMgr
 ├── MModelShadow Shadow
 ├── MModelCombineMount CombineMount
 ├── MModelCombineMesh CombineMesh
 ├── MModelBodyExtension BodyExtension
 ├── MModelSimpleMesh SimpleMesh
 ├── MModelChangeTexture ChangeTexture
 ├── MModelChangeTextureBySprite ChangeTextureBySprite
 ├── MModelDisplayComponent Display
 ├── MCloseEyeComponent CloseEye
 ├── MTransfigureComponent Transfigure
 ├── SoundComponent Sound
 ├── MGameObject GameObject
 ├── MAnimator Ator
 ├── Init/LoadAsync/Reset/Recycle
 ├── SetPosition/SetRotation/SetScale
 ├── SetVisible/SetLayer
 └── Update/LateUpdate
```

模型表现组件由 `MModel` 持有或挂载，负责挂点、材质、阴影、换装和显示，不属于实体业务组件集合。`MModel` 没有实体状态判断职责，也不接收键盘或网络事件。

### 3.4 动画类

```text
MAnimator
 ├── MModel _model
 ├── MGameObject _host
 ├── UnityEngine.Animator _animator
 ├── RuntimeAnimatorController _controller
 ├── AnimatorOverrideController _overrideController
 ├── Dictionary<string, MAnimInfo> _animInfos
 ├── Float/Int/Bool 参数缓存
 ├── string _playStateName
 ├── int _sessionVersion
 ├── Init(MModel model, MGameObject host, Animator animator, string controllerLocation)
 ├── Play/CrossFade/PlayCurrent
 ├── OverrideAnim/GetAnimInfo/GetCurrentAnimInfo
 ├── SetFloat/GetFloat、SetBool/GetBool、SetInteger/GetInteger
 ├── Rebind/Update(float deltaSeconds)
 ├── Enabled/Speed/CullingMode
 └── Reset/Recycle

MObjectAnimator : MAnimator
 └── 非角色物体的简化动画初始化和播放策略

MAnimInfo
 ├── string Key / BlendTreeKey / ClipLocation
 ├── MAnimationClipInfo ClipInfo
 ├── ClipLease CurrentLease / PendingLease
 ├── NormalizedTime
 ├── IsLoop / IsClipReady / IsStale
 └── RequestLoad/Apply/Reset

MAnimationClipInfo
 ├── string Location
 ├── AnimationClip Clip
 ├── float Length
 ├── bool IsLoop
 ├── int RefCount
 └── Retain/Release

MAnimationAssetProvider
 ├── LoadClipAsync
 ├── LoadControllerAsync
 ├── ReleaseClip
 ├── ReleaseController
 └── 请求合并、引用计数和主线程回调

MAnimationStateNames
 └── Idle、Move、FIdle、FMove、Born、Dead、BeHit、Stun、Singing、Skill、Special、Climb、Fishing 等状态名常量

MAnimationKeyResolver
 └── 根据实体类型、模型、职业、性别、武器和配置把逻辑动作映射为状态名/Clip key
```

`MModel.Ator` 是唯一动画访问入口。动作状态只能调用 `Entity.Model.Ator.Play`、`CrossFade`、`OverrideAnim` 等 API，禁止在状态组件、实体组件或控制器中直接获取 Unity `Animator`。

### 3.5 状态机和动作状态类

```text
MStateMachine
 ├── MEntity Owner
 ├── MActionComponent Current
 ├── MActionComponent Previous
 ├── MActionStateContainer States
 ├── MStateTransitionRequestQueue Requests
 ├── SetDefaultState/Enqueue/TransferToState
 ├── UpdateSeconds(float deltaSeconds)
 ├── LateUpdate(float deltaSeconds)
 └── Stop/ResetForPool

MActionStateContainer
 ├── ActionStateKey → MActionComponent 唯一主索引
 ├── StateDefine → ActionStateKey 列表的辅助索引
 ├── Attach/Detach/Get/Release
 └── 不参与普通实体组件查询

MActionComponent
 ├── MEntity Entity
 ├── StateDefine State
 ├── StateName / Speed / CollisionLayer
 ├── OnGetPermission/OnStart/OnUpdate/Finish/Stop/AfterFinish
 ├── OnGetEvent
 ├── RegisterEvent/UnregisterEvent
 ├── PlayAnim
 └── ResetForPool

MDefaultActionComponent       Idle/Move/FMove
MMoveActionComponent          移动与速度参数
MSkillActionComponent         技能前摇、命中窗口、后摇
MSingingActionComponent       读条
MSpecialActionComponent       传送、剧情、必杀
MBeHitActionComponent         受击
MStunActionComponent          眩晕
MDeadActionComponent          死亡
MBornActionComponent          出生/复活
MClimbActionComponent         攀爬
MCollectActionComponent       采集
MFishingActionComponent       钓鱼
MDanceActionComponent         跳舞
MFearRunActionComponent       恐惧逃跑
```

动作状态不是普通实体组件，必须由 `MStateMachine` 的 `MActionStateContainer` 单独管理。动作状态的 `PlayAnim` 只向 `Entity.Model.Ator` 提交表现请求；逻辑状态切换仍由 `MStateMachine` 仲裁。

### 3.6 实体组件类

```text
EntityComponentBase
 ├── Entity MEntity Host
 ├── Attach/Detach
 ├── Update(int deltaMilliseconds)
 ├── RegisterEvent/UnregisterEvent
 └── ResetForPool

EntityMoveComponent       移动、速度、位置同步
EntityBuffComponent       Buff、控制效果和属性修正
EntitySkillComponent      技能请求、冷却和命中数据
```

`MoveComp`、`Buff`、`Skill`、`Machine` 是 `MEntity` 的固定快捷访问属性，初始化时必定存在。它们与 `MModel` 的表现组件、`MActionComponent` 的状态对象完全分离。

## 4. MModel 与 MAnimator 的使用方式

### 4.1 创建和初始化

```text
SceneEntityWorld.Add
  → SceneEntityFactory.Build(entityType, configId)
      → MEntity.Initialize
          → _model = new MModel()
          → MComponentMgr.Acquire(Move/Buff/Skill/Machine)
          → MActionStateRegistry.AttachStates(entity)
          → Machine.SetDefaultState
      → Model.LoadAsync(modelLocation)
          → 创建 MGameObject
          → 查找 Animator
          → 根据 EModelAnimatorType 创建 MAnimator 或 MObjectAnimator
          → Ator.Init(Model, GameObject, Animator, controllerLocation)
```

状态机可以早于模型加载运行；模型完成后，`MModel` 调用 `Ator.PlayCurrent` 补播当前状态。模型和动画直接在实体初始化链路中绑定。

### 4.2 每帧更新

```text
SceneWorldSystem.Update(deltaMilliseconds)
  → SceneEntityWorld.Update
      → MEntity.Update(deltaMilliseconds)
          → MoveComp/Buff/Skill.Update(deltaMilliseconds)
          → Machine.UpdateSeconds(deltaMilliseconds * 0.001f)
          → Model.Update(deltaSeconds)

SceneWorldSystem.LateUpdate(deltaMilliseconds)
  → SceneEntityWorld.LateUpdate
      → MEntity.LateUpdate(deltaSeconds)
          → Machine.LateUpdate(deltaSeconds)
          → Model.LateUpdate(deltaSeconds)
              → Ator.Update(deltaSeconds)
```

每帧只能有一个入口调用 `MAnimator.Update`。状态机在 Update 阶段决定最终状态，LateUpdate 阶段才调用 `MModel.Ator.Play/CrossFade`，避免一帧多次切换 Unity Animator。

### 4.3 播放、覆盖和参数

- `Play` 用于出生、死亡、硬切或重置时间；`CrossFade` 用于 Idle/Move 和一般动作过渡。
- `OverrideAnim` 只登记逻辑 key 与资源 location。Clip 未就绪时保留逻辑状态，加载完成后由 `MModel.Ator.PlayCurrent` 补播。
- `MAnimator` 未绑定 Unity Animator 时缓存 Float、Int、Bool、Speed、Enabled 和 CullingMode；绑定后一次回放。
- 模型实例独占 `AnimatorOverrideController`；基础 Controller 和 Clip 均通过 `MAnimationAssetProvider` 租约管理。
- 每次 `Init`/`Reset` 递增 `sessionVersion`。异步回调必须校验会话、宿主、key 和租约，过期回调只能释放自身租约。

## 5. Player、Role、Monster 状态机

### 5.1 PlayerEntity

输入、技能按钮、交互和服务器确认先写入 `MStateTransitionRequestQueue`，再由 `MStateMachine` 仲裁。默认状态根据 `MoveComp.IsMoving` 在 Idle、Move、FMove 间切换；技能、读条、受击、眩晕、死亡、攀爬、采集、钓鱼和跳舞均使用动作组件。

本地预测状态带预测 token；服务器确认保留状态，拒绝或纠正则按服务器状态、序列号和动作起始时间重新入队。输入层不直接调用 `MModel.Ator`。

### 5.2 RoleEntity

远程角色只接受服务器快照、移动插值、技能广播、受击广播和可见性事件。服务器动作序列号递增，旧序列只更新位置缓存，不回退当前动作。休眠时暂停 `Ator.Update`，唤醒后按服务器动作起始时间恢复 normalizedTime。

### 5.3 MonsterEntity 及其派生类

Monster 复用 Role 的默认、移动、技能、受击、眩晕和死亡状态，额外注册巡逻、追击、攻击、恐惧逃跑和出生状态。Elite/Boss 通过配置提高动作优先级或声明不可打断窗口，不复制整套状态机。Pet/Eagle/Mercenary 只替换模型和移动/飞行动画映射。

### 5.4 非角色实体和飞行物

Npc、Dummy、Trap、Collection 仅注册 Default、Interact、Dead 等所需状态。ProjectileEntity 可使用 `MObjectAnimator` 或 `MAnimator`，状态最小化为 Default、Move、Hit、Dead；轨迹和命中由实体逻辑组件驱动，动画只表现飞行方向、尾焰和爆炸。

## 6. 状态切换与回收顺序

状态请求按“服务器权威 > 网络纠正 > AI > 本地输入”排序，再按优先级和入队序号确定唯一结果。切换顺序固定为：

```text
旧状态.Stop(false, selfTransition)
  → Previous = Current
  → Current = 新状态
  → 新状态.OnGetPermission/OnStart
  → MarkTriggered
  → LateUpdate 调用 Entity.Model.Ator.Play/CrossFade
```

实体销毁和对象池回收必须按以下逆序执行：

```text
MEntity.Destroy
  → Machine.Stop
  → ActionStates.DetachAndReleaseAll
  → Model.Reset
      → Ator.Reset
      → 释放 OverrideController、ClipLease、ControllerLease
      → 回收模型表现组件
      → 回收 MGameObject
  → MComponentMgr.Release(Move/Buff/Skill/Machine)
  → MObject.Dispose
      → 清理普通组件和事件监听
```

## 7. 实现边界和验收标准

实现阶段必须满足：

1. 全部实体间接继承 `MEntity`；`MEntity` 直接持有 `MModel Model`，`MModel` 直接持有 `MAnimator Ator`。
2. 代码中不存在独立表现中间层或通过普通组件容器查找 Animator 的路径。
3. 只有 `MAnimator` 调用 Unity Animator 播放 API；状态组件通过 `Entity.Model.Ator` 访问。
4. `MModel` 的表现组件不进入 `EntityComponentContainer`；MoveComp/Buff/Skill/Machine 不进入普通组件集合。
5. `MAnimator.Update` 每帧只执行一次；Update 使用毫秒入口，状态机和动画推进使用秒值。
6. 异步模型/Controller/Clip 回调具备对象池会话保护，回收后不得串台到新实体。
7. 实现轮先完成 MGameObject、MModel、MAnimator 和资源租约，再接入 MStateMachine 与动作状态，最后迁移旧 RoleFSM。

本设计明确废弃上一版独立表现中间层方案，后续实现不得恢复该中间层。
