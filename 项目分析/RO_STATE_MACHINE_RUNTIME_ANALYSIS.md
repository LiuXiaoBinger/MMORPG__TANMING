# Ro_cs422nb 状态机与动作安装运行机制解析

## 结论先说

参考项目的 Animator Controller 节点不是“每个角色都提前挂好最终动画”的节点，而是统一的动作插槽。`Idle`、`Move`、`Skill`、`Singing`、`Dead` 等节点主要负责稳定的状态名称和状态机结构；不同角色、武器、坐骑、时装、怪物的真实 `AnimationClip` 在运行时通过 `MAnimator.OverrideAnim` 写入 `AnimatorOverrideController`。

因此，截图中节点看起来没有具体动作，不代表运行时没有动作。真正的调用链是：

```text
网络/本地输入/场景事件
        ↓
状态事件 MEventType
        ↓
MActionComponent.OnActionEvent
        ↓
MStateMachine.TransferToState
        ↓
MActionComponent.OnGetPermission / OnStart
        ↓
MStateMachine.MarkTriggered
        ↓（延迟到 LateUpdate）
当前状态 PlayAnim
        ↓
MAnimator.Play 或 MAnimator.CrossFade
        ↓
Unity Animator Controller 的同名节点
        ↓
AnimatorOverrideController 中已经安装的实际 AnimationClip
```

## 一、参考项目实际存在两层状态机

### 1. `ROGameLibs.StateMachineComponent`：逻辑/同步状态机

参考项目通过 SWIG 暴露的 `StateMachineComponent` 位于：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/SwigTools/CppInterface/rogamelibs/StateMachineComponent.cs`

它来自 `rogamelibs` 原生库，负责保存和同步实体的权威状态，公开能力包括：

- `GetCurrentState()`、`GetPreviousState()`、`GetDefaultState()`：查询当前、上一个和默认状态。
- `TryTransferToState(...)`：按照状态许可和事件尝试切换。
- `CanAction(...)`：判断目标状态是否允许执行。
- `ForceToDefaultState(...)`：强制回默认状态。
- `GetCurrent().GetStateDate()`：读取吟唱、钓鱼、死亡、受击、采集等状态的业务数据。
- `NeedSyncState()`：判断是否需要把原生状态同步到客户端表现层。

这层不是 Unity Animator。它不负责直接播放 `AnimationClip`，而是决定实体当前处于技能、吟唱、死亡、受击等哪一种逻辑状态，并携带技能 ID、目标、开始时间、采集物等状态数据。

### 2. `MoonClient.MStateMachine`：客户端动作表现状态机

参考文件：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/State/MStateMachine.cs`

它挂在 `MEntity` 的组件容器中，保存 `IMStateTransform` 状态对象。具体状态由动作组件实现，例如：

- `MDefaultActionComponent`：默认待机/移动表现。
- `MSingingComponent`：吟唱。
- `MSkillComponent`：技能。
- `MBeHitComponent`：受击。
- `MDeadComponent`：死亡。
- `MStunComponent`：眩晕。
- `MBornComponent`：出生。
- `MSpecialComponent`：特殊动作。
- `MFearRunComponent`：恐惧逃跑。
- `MClimbComponent`、`MCollectComponent`、`MFishingComponent`、`MDanceComponent`：玩家生活动作。

这层负责状态对象生命周期和播放时机，但状态许可仍参考统一的状态许可表；它也可以由原生状态机同步结果驱动。

## 二、状态节点为什么可以没有角色专属动作

参考项目的 Controller 路径在 `MModel.cs` 中定义：

- `Controller/MAnimator`：普通角色控制器。
- `Controller/MMonsterAnimator`：怪物控制器。

模型加载完成后，`MModel.onModelLoadCallback()` 获取模型上的 Unity `Animator`，再按模型类型调用：

```text
MModel.onModelLoadCallback
    → MAnimator.Init(this, _mObj, animator, controllerPath)
```

对应代码位置：

- `Model3D/MModel.cs:1117`：模型加载完成回调。
- `Model3D/MModel.cs:1122`：从 GameObject 获取 Unity Animator。
- `Model3D/MModel.cs:1128`：普通角色绑定 `Controller/MAnimator`。
- `Model3D/MModel.cs:1131`：怪物绑定 `Controller/MMonsterAnimator`。
- `AsyncObj/MAnimator.cs:392-478`：初始化 Animator、绑定 Controller、重放缓存参数和当前状态。

Controller 中的状态名称必须稳定，因为代码通过状态名播放：

- `MAnimator.Play(stateName, ...)`。
- `MAnimator.CrossFade(stateName, ...)`。
- `Animator.HasState(layer, hash)` 检查同名状态是否存在。

具体角色动画不直接固定在这些状态节点上，而是由 `MAnimator.OverrideAnim` 安装：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/AsyncObj/MAnimator.cs:634`

其核心过程是：

1. 以逻辑 key 查找或创建 `MAnimInfo`。
2. 通过 `MAnimationMgr.singleton.GetClipInfo(clipPath)` 获取真实动画片段。
3. 记录动画路径、长度、是否循环和归一化时间。
4. 若 Animator 已准备好，则创建或复用 `AnimatorOverrideController`。
5. 找到 Controller 中对应的原始占位 Clip。
6. 用角色实际 Clip 替换占位 Clip。
7. 当前正播放该状态时，异步加载完成后调用 `PlayCurrent()` 重新播放。

所以节点的职责是“状态入口”，实际 Clip 的职责是“可替换动作资源”。

需要注意：如果某个 Controller 状态真的没有任何原始 Motion/占位 Clip，`AnimatorOverrideController` 就没有可替换的源 Clip，`OverrideAnim` 也无法完成映射。参考项目的正确前提是 Controller 中存在可被 key 找到的基础 Clip，或者代码走兼容状态/参数路径；不能把“节点没有角色专属 Clip”理解成“节点完全不需要源 Clip”。

## 三、状态和动作组件是什么时候安装的

这里要区分“安装状态组件”和“安装动画片段”。两者不是同一件事。

### 1. 状态组件安装：实体初始化阶段

参考文件：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Entity/MEntity.cs:944`

`MEntity.InitComponents()` 会创建实体通用组件，并在具体场景条件满足时创建：

```text
MStateMachine
MDefaultActionComponent
MSkillComponent
MBuffComponent
MSingingComponent
MBeHitComponent
MDeadComponent
MStunComponent
MBornComponent
MFearRunComponent
以及攀爬、采集、钓鱼、跳舞等动作组件
```

默认状态安装位置：

`MEntity.cs:974-976`

```text
CreateComponent<MStateMachine>
CreateComponent<MDefaultActionComponent>
Machine.SetDefaultState(defaultAction)
```

此时安装的是 C# 状态对象和组件，不是把角色的每个具体动画 Clip 写进 Controller。

### 2. 默认待机动作安装：组件 `Attached()` 阶段

待机组件：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Common/MIdleComponent.cs`

- `:55-59` 的 `Attached()` 调用 `prepairAnim()`。
- `:177` 的 `prepairDefaultAnim()` 根据表现表、武器类型、性别、时装、坐骑准备 `Idle/FIdle`。
- `:231-235` 通过 `Entity.OverrideAnim(MAnimator.STATE_IDLE/FIDLE, idleAnim)` 覆盖待机动作。
- `:67-88` 注册换装、上下坐骑、武器变化、携带物和连招相关事件；发生变化时重新准备动画。

移动组件：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Common/MMoveComponent.cs`

- `:158-165` 的 `Attached()` 启用移动组件并调用 `prepairAnim()`。
- `:887` 的 `prepairDefaultAnim()` 根据表现表配置 `Move/FMove`。
- `:905`、`:921` 使用 `Entity.OverrideAnim` 覆盖移动 Clip。
- `:168-174` 的 `SetMoving()` 在默认状态下发现移动标记变化时调用 `Entity.Machine.MarkTriggered()`，让默认状态下一帧刷新 `Idle/Move`。

因此，移动不是单独的高层逻辑状态。它通常仍然处于 `kStateDefault`，只是 `MDefaultActionComponent.StateName` 根据 `Entity.IsMoving` 返回 `Idle` 或 `Move`。

### 3. 技能/受击/死亡等动作安装：进入动作前或动作开始时

技能、吟唱、受击、死亡等状态不会在实体创建时把所有可能 Clip 全部安装完。它们通常在收到事件、确定动作 ID 或进入状态时准备对应 Clip：

- `MSingingComponent` 收到 `MEvent_Singing` 后切入吟唱状态，并在状态开始/技能数据确定后调用 `Model.Ator.OverrideAnim(StateName, anim)`。
- `MBeHitComponent` 收到 `MEvent_BeHit` 后切入受击状态，按受击类型准备受击动画，再由 `PlayAnim` 播放。
- `MDeadComponent` 收到 `MEvent_Dead` 后切入死亡状态，在死亡动作选择逻辑中调用 `Entity.OverrideAnim(StateName, anim)`；参考位置约为 `MDeadComponent.cs:311-330`。
- `MSpecialComponent`、`MCollectComponent`、`MClimbComponent`、`MDanceComponent` 等在动作 ID 或表格行确定后覆盖对应状态 key。

换句话说，动作 Clip 的安装点不是统一的一个函数，而是“各动作组件掌握自己的表现配置”，统一落到 `MEntity.OverrideAnim → MAnimator.OverrideAnim`。

## 四、状态切换和动画播放的准确时序

### 1. 事件进入状态组件

`MActionComponent` 的公共入口在：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Action/MActionComponent.cs:172`

`OnActionEvent(IEventArg e)` 做三件事：

1. 读取当前状态。
2. 调用具体动作组件的 `OnGetEvent(e, lastState)` 判断能否切入。
3. 允许时调用 `Entity.Machine.TransferToState(this)`；不允许时回默认状态。

### 2. 状态机切换只做逻辑切换，不立即操作 Animator

`MStateMachine.TransferToState` 位于：

`Components/State/MStateMachine.cs:174`

它会：

1. 停止旧状态。
2. 保存 `Previous`。
3. 设置 `Current`。
4. 调用新状态的 `OnGetPermission()`。

`MActionComponent.OnGetPermission()` 位于 `MActionComponent.cs:67`，会把状态设为激活、调用 `Entity.Machine.MarkTriggered()`，然后执行具体状态的 `OnStart()`。

### 3. 真正播放动画延迟到 `LateUpdate`

`MStateMachine.LateUpdate()` 位于 `Components/State/MStateMachine.cs:147`。

只有 `_isTriggered` 为 true 且 `Entity.Model.Ator` 有效时，才会调用：

```text
_preState = _current.PlayAnim(_preState)
```

这就是“状态节点不是在状态切换函数里直接播放”的关键。RO 先在 Update 阶段处理状态、移动、技能和事件，再在 LateUpdate 统一更新 Animator，避免同一帧多次事件导致 Animator 重复切换或顺序不稳定。

### 4. `MActionComponent.PlayAnim` 才调用 MAnimator

默认实现位于 `MActionComponent.cs:134` 附近：

- 根据 `_isOverrideSuccess` 决定使用当前状态名还是回退到 `Idle`。
- 根据 `Entity.IsMixToDefaultAction` 选择 `CrossFade` 或 `Play`。
- 对实体模型和战斗坐骑模型分别调用 `Ator.CrossFade/Play`。

状态组件可以覆写 `PlayAnim`，例如默认动作会根据待机/移动、战斗/自拍/搬运/射击模式返回不同状态名；攀爬、死亡、特殊动作则会加入自身的非循环、后摇、坐骑或动作完成处理。

## 五、原生状态同步如何驱动客户端状态

参考文件：

`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Local/MLocalSyncComponent.cs:285`

`StateSync()` 的过程是：

1. 从 `Entity.cppObject` 获取原生 `StateMachineComponent`。
2. 判断 `NeedSyncState()`。
3. 读取 `GetCurrentState()`。
4. 根据状态枚举读取原生状态数据。
5. 转成客户端具名事件参数并派发，例如 `MEvent_Fishing`、`MEvent_SyncSinging`、`MEvent_Dead`、受击、出生、采集等。
6. 对应的 C# 动作组件监听这些事件，再通过 `OnActionEvent` 切入客户端表现状态。

所以网络同步不是直接调用 `Animator.Play`，而是：

```text
服务器/原生状态数据
    → StateMachineComponent
    → MLocalSyncComponent.StateSync
    → MEventType 事件
    → MActionComponent
    → MStateMachine
    → MAnimator
```

## 六、动作 Clip 的来源和覆盖逻辑

动作 Clip 来源主要有四类：

1. **表现配置表**：角色待机、奔跑、性别、武器、坐骑和时装动作，由 `MIdleComponent`、`MMoveComponent` 根据 `PresentDataView` 读取。
2. **技能/动作配置**：技能、吟唱、特殊动作、采集、钓鱼、跳舞等由对应动作组件按动作 ID 或协议数据读取。
3. **模型类型默认 Controller**：`MModel` 根据普通角色或怪物选择 `Controller/MAnimator`、`Controller/MMonsterAnimator`。
4. **临时动作覆盖**：例如 `TempAnim`、特殊动作、连招待机、双人动作，通过事件或协议临时调用 `OverrideAnim`。

统一覆盖入口：

```text
MIdleComponent / MMoveComponent / MSkillComponent / 其他动作组件
    → MEntity.OverrideAnim(key, clipName)
    → MAnimationMgr.GetClipPath / GetClipInfo
    → MModel.Ator.OverrideAnim(key, clipPath)
    → MAnimInfo 保留 Clip 和播放时间
    → AnimatorOverrideController[key] = 实际 AnimationClip
```

如果覆盖发生在模型异步加载完成之前，`MAnimator` 会先把 `MAnimInfo` 和 Clip 信息缓存起来；`MAnimator.Init` 或 Controller 绑定完成后，再统一执行 `ApplyOverrideClips`。这就是参考项目可以先初始化实体组件、后异步加载模型的原因。

## 七、对当前项目实现的直接启示

当前项目已经具备相同方向的 `MEntity → MModel → MAnimator` 关系和 `MStateMachine → MActionComponent → PlayAnim` 调用链，但需要严格保持以下边界：

- `MStateMachine` 只决定当前动作状态和播放时机，不直接读取技能表或拼接所有动作路径。
- 每个动作组件负责自己的 `StateName`、动作资源准备、状态开始/更新/结束逻辑。
- `MAnimator` 只负责 Controller、Clip 覆盖、参数缓存、Play/CrossFade、异步资源和回收。
- `MModel` 负责模型加载完成后创建/初始化 `MAnimator`，不能让状态组件直接持有 Unity `Animator`。
- `MDefaultActionComponent` 内部根据移动标记选择 `Idle/Move`，不要把普通移动重复注册为一个与恐惧逃跑冲突的高层状态。
- 状态切换后统一由 `LateUpdate` 调用 `PlayAnim`，避免在网络回调或组件事件中直接多次操作 Animator。
- Controller 必须提供稳定的状态节点和可被覆盖的源 Clip；如果节点完全没有 Motion，必须补齐占位 Clip 或明确采用兼容参数驱动，否则 `AnimatorOverrideController` 没有替换目标。

## 八、最终回答用户问题

### “状态机节点都是空的，代码怎么调用？”

代码不依赖节点里预先放入某个具体角色动作，而是依赖节点名称。状态切换后，`MActionComponent.PlayAnim` 将 `Idle/Move/Skill/...` 传给 `MAnimator.Play/CrossFade`；角色真正使用的 Clip 早已通过 `OverrideAnim` 覆盖到这些状态对应的源 Clip 上。

### “动作什么时候安装？”

- 状态组件：实体 `MEntity.InitComponents()` 时安装。
- 默认待机/移动 Clip：组件 `Attached()`、模型就绪事件、换装/坐骑/武器变化事件时安装或重装。
- 技能/吟唱/受击/死亡等 Clip：收到事件、拿到动作配置或进入动作状态前后，由对应动作组件调用 `OverrideAnim` 安装。
- 真正播放：状态切换标记后，在 `MStateMachine.LateUpdate()` 调用当前状态 `PlayAnim` 时播放。

### “安装动作逻辑在哪里？”

核心统一入口是：

`MEntity.OverrideAnim` → `MAnimator.OverrideAnim`。

具体动作准备逻辑分散在：

- `MIdleComponent.prepairAnim/prepairDefaultAnim`
- `MMoveComponent.prepairAnim/prepairDefaultAnim`
- `MSingingComponent` 的吟唱动作准备逻辑
- `MBeHitComponent` 的受击动作准备逻辑
- `MDeadComponent` 的死亡动作选择和覆盖逻辑
- `MSpecialComponent`、`MCollectComponent`、`MClimbComponent`、`MFishingComponent`、`MDanceComponent` 等各自的动作准备逻辑

这正是参考项目的设计：状态机不拥有所有动作资源，动作状态组件按领域准备资源，MAnimator 只做统一安装和播放。
