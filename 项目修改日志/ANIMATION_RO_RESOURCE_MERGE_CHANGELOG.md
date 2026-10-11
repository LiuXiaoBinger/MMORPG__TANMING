# 动画资源入口与 RO 剪辑状态合并说明

## 第一次审核与返工结论

- 第一次正式审核不通过：缺少启动阶段动画索引预加载、播放就绪门控、混合树关系、关键动画准备状态、全局 LRU 驱动和完整反初始化。
- 本文件后续内容已按第一次审核全部问题完成返工，不以首次构建通过作为功能验收结论。

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimationMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEntity.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/Global.cs`
- `LYF_MMO/GM_Game.csproj`
- 删除 `MAnimationAssetProvider.cs` 及其 `.meta`
- 删除 `MAnimationClipInfo.cs` 及其 `.meta`
- 删除 `MAnimInfo.cs` 及其 `.meta`

## 实现内容

- 删除同步 `Resources.Load` 动画入口，动画剪辑统一由 `MAnimationMgr` 通过 YooAsset 管理。
- 剪辑共享信息统一使用 `MAnimationMgr.ClipInfo`，移除重复的顶级 `MAnimationClipInfo`。
- `MAnimInfo` 按 RO 结构迁入 `MAnimator`，外部签名统一为 `MAnimator.MAnimInfo`。
- `MAnimator.MAnimInfo` 分别保存已就绪 `_clipInfo` 和正在加载 `_preparingClipInfo`。
- 路径、时长和循环标记优先返回准备中剪辑；`Clip` 仅返回已就绪剪辑。
- 切换目标剪辑时先取消旧准备中回调并释放引用，再持有新 `ClipInfo`。
- 加载成功后释放旧就绪引用、提升准备中剪辑并刷新覆盖控制器；加载失败时释放准备中引用。
- 重置时同时释放准备中和已就绪剪辑，取消未完成回调，旧回调通过对象一致性检查隔离。
- `MAnimator.OverrideAnim` 改为返回动画信息并异步准备剪辑；`MEntity.OverrideAnim` 返回真实结果。
- `Global.Awake` 在 YooAsset 包就绪后立即预加载动画索引；成功前不创建 `SceneWorld`，失败保持阻断并每 3 秒重试。
- `MAnimationMgr.GetClipPath` 在索引未成功预加载时拒绝解析短名，避免实体绕过启动门控。
- `Play`、`CrossFade` 接入剪辑就绪门控并写入归一化时间；混合树要求所有子剪辑均已就绪。
- 新增混合树状态到去重覆盖键列表的映射，支持覆盖键迁移、首条信息查询和重置清理。
- 新增待机、战斗待机及四种移动状态的关键动画准备集合与 `IsPreloadFinished`。
- `Global.Update` 在 `SceneWorld` 判空前稳定驱动动画空闲 LRU；退出或销毁时调用 `MAnimationMgr.Uninit`。
- `MAnimationMgr.Uninit` 释放索引句柄、全部剪辑句柄，清空等待回调、引用计数、索引和 LRU 状态。

## 设计思路

- 资源路径索引、YooAsset 句柄、共享引用计数和空闲 LRU 只归 `MAnimationMgr` 管理。
- `MAnimator.MAnimInfo` 只管理某个逻辑状态的切换过程，不直接加载或卸载资源。
- 新剪辑加载期间保留旧就绪剪辑，避免异步窗口内当前状态失去可播资源。
- 使用稳定回调实例和准备对象一致性判断，防止快速换动作或对象复用后的旧结果污染新状态。

## 验证结果

- 返工后执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework1`。
- 构建结果：0 个错误，39 个既有警告。
- 静态扫描确认动画相关正式代码不存在 `MAnimationAssetProvider`、顶级 `MAnimationClipInfo` 或顶级 `MAnimInfo` 引用。
- 静态扫描确认本次修改文件未新增命名空间、`foreach` 或三目运算符。
- 静态扫描确认存在启动 `Preload` 调用、`IsClipReady` 播放消费、混合树映射、关键动画准备集合、全局 `UpdateCache` 驱动和退出 `Uninit`。
- 目标文件行尾空白扫描无结果。

## 未解决问题

- 未在 Unity Play Mode 中实际触发 YooAsset 动画切换，因此仍需运行时验证快速连续覆盖、加载失败和模型回池场景。
- 构建中的程序集版本冲突、类型隐藏和未使用字段警告为项目既有问题，本次未扩大范围处理。
# 第二次审核返工

- 第二次独立审核未通过：`IsPreloadFinished` 未检查当前普通状态和混合树全部子剪辑；正式动作播放前未统一安装真实剪辑。
- `MActionComponent.PlayAnim` 现按 RO 表现表映射，在 `Play/CrossFade` 前安装 Idle、FIdle、四移动、受击、眩晕、吟唱、死亡和出生真实剪辑。
- 技能、特殊动作及其他没有通用表现表动画来源的状态不再尝试播放 Controller 占位剪辑，改为明确记录失败并安装真实 Idle 回退剪辑。
- 专用技能或交互系统已提前安装真实剪辑时，动作基类复用该绑定；只有绑定和通用表现表字段都缺失时才回退。
- `MAnimator.IsPreloadFinished` 现同时检查关键准备集合与当前状态；混合树逐项检查全部子剪辑，且只读检查不会修改归一化播放进度。
- 已执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework2`，结果为 0 个错误、39 个既有警告。
- 静态扫描确认 SceneEntity 正式播放入口只剩 `MActionComponent.PlayAnim` 两处，且均位于真实剪辑准备之后；返工文件未命中命名空间、`foreach`、三目运算符、弱类型业务载荷或行尾空白。
- 当前未执行 Unity Play Mode；本轮已交由同一审核 Agent 进行第三次完整审核。

## 第三次审核返工

- 第三次独立审核不通过：正式本地角色和其他角色仍使用旧 JX Controller，缺少 RO 同名覆盖槽位；槽位查找失败时 preparing 剪辑仍被错误提升为 ready；旧兼容器未识别四个实际移动状态。
- `MainCityMgr` 的两条正式角色创建链统一向 `MModel.SetGameObject` 传入 `Assets/artres/Resources/Controller/MAnimator.controller`，不修改用户现有角色预制体。
- `MAnimator` 在显式 Controller 异步加载期间不使用预制体旧 Controller 处理覆盖；已加载剪辑保持 preparing，Controller 绑定后统一重试。
- preparing 剪辑只有在同名槽位实际写入成功后才释放旧 ready 并完成提升；槽位缺失或写入异常会释放 preparing、保留旧 ready、移除关键预加载标记并记录明确错误。
- Controller 确定加载失败时会释放全部 preparing 引用并完成关键预加载标记收尾，避免资源和等待状态永久残留。
- `MAnimatorStateCompatibility` 补齐 `ZdMoveWalk`、`ZdMoveRun`、`NonZdMoveWalk`、`NonZdMoveRun` 到旧 Movement 参数的兼容映射。
- 静态核对 `MAnimator.controller` 的 GUID 为 `e452ce068bb40b341852fc497e2c8426`，文件内存在 Idle、FIdle、四移动、BeHit、Born、Dead、Singing、Stun 同名槽位。
- 静态核对 `Assets/artres` 已由 YooAsset `CollectAll` 收集；YooAsset 1.5.7 清单反序列化会把每个资源的完整原生 AssetPath 加入 Location 映射，因此上述完整 Location 可被 `CheckLocationValid` 验证。
- 已执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework3`，结果为 0 个错误、39 个既有警告。
- 返工文件未命中命名空间、`foreach`、三目运算符或弱类型业务载荷，行尾空白扫描为 0。
- 未执行 Unity Play Mode 的真实 YooAsset Controller/Clip 完成顺序测试；本轮完成后交由同一审核 Agent 进行第四次完整审核。

## 第四次审核返工

- 第四次独立审核不通过：YooAsset 的 `DefaultPackage` 未收集动画索引目录和动画资源目录，导致完整路径 Location 无法进入清单，启动门控会永久阻止 `SceneWorld` 创建。
- `AssetBundleCollectorSetting.asset` 新增启用的 `AnimationIndex` 组，收集 `Assets/GM_Game/Resources/ResInfo`，目录 GUID 为 `34591ac5ba4d44859383b67a97686a`。
- `AssetBundleCollectorSetting.asset` 新增启用的 `Animations` 组，收集 `Assets/GM_Game/Animas`，目录 GUID 为 `7baac35e56629d540bd08b5a784eb203`。
- 两个 Collector 均沿用项目现有的 `AddressByFileName`、`PackDirectory`、`CollectAll` 规则；`Assets/artres` 原 Collector 保持不变并继续覆盖 `MAnimator.controller`。
- 补齐 `Global` 类型用途与生命周期注释，并补齐动画状态兼容器常量、方法参数、返回值、局部变量以及 Animator 状态哈希的具体中文语义注释，未改变业务逻辑。
- 静态验证确认 `animInfo.txt` 可解析为 12 行、每行 3 字段的二维 JSON；12 个完整 `.anim` 路径均存在且位于 `Assets/GM_Game/Animas` Collector 下。
- 静态验证确认 `Assets/artres/Resources/Controller/MAnimator.controller` 存在并受原 `Assets/artres` Collector 覆盖。
- 核对 YooAsset 1.5.7 `DeserializeManifestOperation.cs`：清单反序列化会把 `packageAsset.AssetPath` 原生完整路径加入 `AssetPathMapping1`，因此本次索引、动画和 Controller 使用的完整 AssetPath 可作为 Location。
- 已执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework4`，结果为 0 个错误、39 个既有警告。
- 静态扫描确认目标业务文件未命中命名空间、`foreach`、三目运算符、`object[]` 或 `dynamic`；新增 Collector 配置和新增/修改代码行不存在行尾空白。
- 当前 `LYF_MMO` 已由 Unity Editor 实例打开，未另启同项目 Batch Mode，未执行 YooAsset 模拟清单构建或 Unity Play Mode；未把静态验证表述为运行时 `CheckLocationValid` 结果。
- 本轮完成后交由同一审核 Agent 从验收清单第 1 项开始执行第五次完整审核。

## 第五次与第六次审核返工

- 第五次独立审核的业务代码、资源配置、索引与 Collector 闭环及独立构建均通过，仅因 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md` 未记录第三、第四轮失败与返工而未放行；主 Agent 已补齐记录。
- 第六次独立审核不通过：动画索引未就绪时 `SceneWorld` 按启动门控保持为空，但 `CreateRoleCtrl.StartGame` 在成功回包中直接调用 `SceneWorld.LoadLocalRole`，存在确定性空引用，并且简单判空会吞掉已成功的开始游戏回包。
- `Global` 新增强类型 `MainRoleInfo` 待处理副本和明确的 `LocalRoleLoadSubmitResult`；世界未就绪时覆盖保存最新成功回包，世界已就绪时立即消费，动画预载成功创建 `SceneWorld` 后自动消费一次。
- 本地角色实际创建成功或相同角色已经存在时，`Global` 通过强类型 `Action<PlayerEntity>` 事件通知 UI；`CreateRoleCtrl` 只在收到真实玩家实体后加载主城场景，不再直接读取或调用 `SceneWorld`。
- 相同账号和角色的重复成功回包不会重新创建实体，控制器使用账号 ID、角色 ID 和场景切换中标记阻止重复切场景；显式的新选角请求会重置完成标记，允许断线后重新进入同一角色。
- `CreateRoleCtrl.Dispose` 精确解除其订阅的旧 `Global` 事件；`Global` 销毁时清空待处理角色和事件，账号切换或断线时通过 `ClearLocalRoleSession` 同时清理待处理请求、当前场景角色和主角表现控制器。
- 场景角色创建失败时待处理 `MainRoleInfo` 不会丢失，`Global.Update` 按 3 秒间隔继续尝试；动画索引失败仍沿用原 3 秒预载重试，最终成功后继续已排队角色。
- 正式 `LoadLocalRole` 调用已收口为 `Global.TryConsumePendingLocalRole` 唯一入口；补齐 `RoleInfoCtrl` 和 `MainCtrl` 中相关正式协议入口的 `Global/SceneWorld` 判空，断线后的旧回包不会越过门控。
- 静态顺序推演覆盖五种时序：开始游戏回包先到、动画索引先到、动画预载失败后重试、重复成功回包、断线或 Global 退出；五种路径均有明确的保留、消费、去重或清理闭环。
- 已执行并在最终收口后再次执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework5`；两次结果均为 0 个错误、39 个既有警告。
- 未执行 Unity Play Mode 或 YooAsset 模拟清单构建；运行态首次登录、断线重登和快速重复回包仍需在 Unity 中验证。

## 第七次审核返工

- 第七次独立审核不通过：主城场景异步加载回调没有验证发起请求时的 Global、连接代次和角色实例，断线、换号或换角色后旧回调仍可能隐藏选角界面并初始化主 UI；会话清理又在 `LocalRoleChanged(null)` 之后才清空 `roleCtrlBase`，小地图消费者会重新绑定旧表现对象。
- `CreateRoleCtrl.CompleteStartGame` 在发起场景加载时捕获 Global、SceneWorld、PlayerEntity 引用、实体 UID、账号 ID、角色 ID 和连接代次；回调首先复位 `_isSceneTransitionPending`，随后逐项验证生命周期身份、实体 Active 状态、当前本地角色和登录账号。
- 任一身份已失效时，旧场景回调不会隐藏选角界面，也不会调用 `InitMainCtrl`；UIRoot 中的当前 `CreateRoleCtrl` 还必须与回调控制器为同一实例。
- `Global.ClearLocalRoleSessionCore` 改为先清空 `roleCtrlBase`，再清空待处理请求和连接代次，最后调用 `SceneWorld.Clear` 发出 `LocalRoleChanged(null)`；事件消费者只能观察到空的小地图跟踪目标。
- 每个待加载或已加载本地角色会话现记录所属网络连接代次；`TryConsumePendingLocalRole` 只消费当前有效代次的请求，断线后的旧待处理角色不能在动画重试期间进入世界。
- `OnConnectionFailed` 的主线程回调即使发现同一 NetClient 已自动重连到新代次，也会先按失败代次定向清理旧角色会话；若新代次已收到新的开始游戏回包并接管角色，会话代次不匹配使旧失败回调无法误删新角色。
- rework6 首次构建因缺少 `EntityLifecycleState` 所在公共程序集引用出现 1 个编译错误；补充必要 `using MMoRpgCommon.SceneEntity` 后重新执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework6`，最终为 0 个错误、39 个既有警告。
- 静态顺序断言确认：失效场景回调先复位 transition 再校验且不会进入 UI 初始化；`roleCtrlBase` 在空角色通知前清空；快速自动重连时失败代次清理发生在新代次提前返回之前，并受 Global 会话代次隔离。
- rework6 三个业务文件未命中命名空间、`foreach`、三目运算符、`object[]` 或 `dynamic`，修改行空白检查通过。
- 仍未执行 Unity Play Mode、真实自动重连或 YooAsset 模拟清单构建；上述异步时序当前为编译和静态推演验证。

## 第八次审核返工

- 第八次独立审核不通过：`SceneMgr` 使用共享 `_obs` 保存逐帧订阅，A、B 场景请求重叠时 A 的闭包会释放 B 的订阅，A 自身继续重复回调；旧 Single 加载还可能在身份检查前真实激活并覆盖新场景。
- `SceneMgr` 新增强类型 `SceneLoadRequest`，每个请求独立持有递增代次、目标场景、一次性回调、YooAsset 场景句柄、逐帧订阅和解除挂起状态；订阅通过请求字段的局部快照清空并释放，旧请求不能再触碰新请求订阅。
- 场景调度改为串行 latest-wins：同一时刻只运行一个 YooAsset 底层加载，等待槽只保留最后一次调用意图；A 运行期间到达 B、C 时，B 被 C 覆盖且从未创建底层句柄，A 清理完成后直接启动 C。
- 底层加载统一使用 `LoadSceneMode.Additive` 和 `suspendLoad=true`。运行请求在 0.9 进度前不会完成加载；若已过期则仅解除挂起、等待完成并立即 `UnloadAsync`，绝不调用 `ActivateScene`、改写 `_currentSceneType`、隐藏 Loading 或执行旧回调。
- 只有同时仍为 running 和 latest 的最终请求可以激活场景；激活成功后先释放自身订阅、清空 running/latest 状态、提交活动句柄与场景类型、隐藏 Loading 并卸载旧场景，再执行一次调用方回调。回调内同步发起新请求时不会被旧请求后续状态覆盖。
- 加载失败按请求身份处理：过期失败请求只清理自身并继续启动最新等待请求；最新请求失败才允许清空等待状态和隐藏 Loading。完成回调异常会记录错误，但不会破坏已提交的场景管理状态。
- 静态时序推演确认：A 主城后断线提交 B 选角时，A 只卸载自身且 B 最终激活；A 先完成时会先清理再启动 B；串行结构下 B 不会在 A 仍运行时先完成；连续 A→B→C 时 B 不启动、A 清理后直接加载 C。
- 首次 rework7 构建发现方法 `LoadSceneMode` 遮蔽 Unity 同名枚举的 1 个编译错误；改用 `UnityEngine.SceneManagement.LoadSceneMode.Additive` 限定名后重新完整构建。
- 最终执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework7`，结果为 0 个错误、39 个既有警告。
- 静态扫描确认 `SceneMgr.cs` 未命中命名空间、`foreach`、三目运算符、`object[]` 或 `dynamic`；旧共享 `_obs` 已移除，`git diff --check` 无新增空白错误。
- 当前未执行 Unity Play Mode 或 YooAsset 模拟清单构建；Additive 激活、旧场景异步卸载和断线期间快速切换仍需在 Unity 运行态验证。

## 第九次审核返工

- 第九次独立审核不通过：YooAsset 1.5.7 的 `UnSuspend()` 会把 Unity `allowSceneActivation` 设为 true，过期 Additive 场景虽然不调用 `ActivateScene()`，仍会实例化激活根并执行 `Awake/OnEnable/Start`；旧主城可注册协议、创建角色表现并发送进入世界请求。
- `Scene_CreateRole.unity` 和 `Scene_MainCity.unity` 各新增唯一 `SceneBusinessRoot`，资源中默认保持禁用；原有全部普通根对象和根 Prefab 实例统一迁入该根，父级为单位变换，不修改角色 Prefab 内部内容和业务对象原本的局部状态。
- `SceneMgr` 在加载完成后先验证目标场景存在唯一且默认禁用的业务根。过期请求从完成加载到卸载结束始终不启用该根，因此根内脚本不会执行 `Awake/OnEnable/Start`，不会注册协议、创建表现或发送进入世界请求。
- latest 请求在启用新业务根前，先等待上一 YooAsset 托管场景和普通 Unity 启动场景全部卸载完成；旧场景 `OnDestroy` 已完成后才设置新活动场景并启用业务根，消除旧主城销毁晚于新主城初始化的交错。
- 过期请求的 `UnloadSceneOperation` 现在由该请求持续持有并等待 `IsDone`，卸载完成前不释放 `_runningRequest`、不启动 latest；YooAsset 卸载失败且场景仍加载时转用 Unity `UnloadSceneAsync` 兜底并继续等待。
- 请求在等待旧场景卸载期间被新请求覆盖时，会先等待旧场景全部销毁，再卸载自身禁用场景并等待完成，最后才启动最新请求。A 主城→B 选角→C 主城时，A 的 `OnDestroy` 完成后才卸载未启用的 B，B 完成销毁后才加载和启用 C。
- `SocketDispatcher.RemoveEventHandler` 改为接收强类型 `OnActionHandler`，仅当协议当前处理器与注册方委托相等时才删除；`MainCityMgr` 保存自身注册委托并按该委托精确注销，旧实例不能按协议号删除新实例处理器。
- `LoginCtrl`、`CreateRoleCtrl`、`MainCtrl` 和 `SkillCtrl` 的全部协议注销同步迁移到委托所有权接口，项目正式代码不再保留按协议号无条件删除的旁路。
- 静态时序推演确认：A 加载中被 B 覆盖时 A 业务根不启用且卸载完成后才启动 B；A 活动主城切 B 时 A 完整销毁后才启用 B；A→B→C 时 A、B、C 的销毁与启用严格串行；加载失败、业务根配置失败和卸载失败均保留请求所有权到清理结束；完成回调前已清空 running/latest，回调同步提交的新请求不会被旧请求覆盖或隐藏 Loading。
- 执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework8`，结果为 0 个错误、39 个既有警告。
- 静态扫描确认本轮 7 个 C# 文件未命中命名空间、`foreach`、三目运算符、`object[]` 或 `dynamic`；全部 `RemoveEventHandler` 调用均传入强类型委托；旧共享 `_obs` 为零；`git diff --check` 无新增空白错误。
- 场景 YAML 静态校验确认两张场景各有一个默认禁用 `SceneBusinessRoot`、一个 SceneRoots 引用、零个缺失根引用，除业务根自身外无普通顶级 Transform 或根 Prefab 残留。
- 当前 Unity Editor 正占用项目，未另启同项目 Batch Mode，未执行 Unity Play Mode 或 YooAsset 模拟清单构建；业务根首次启用顺序、过期场景无生命周期副作用和同场景快速重入仍需在 Unity 运行态验证。

## 第十次审核返工

- 第十次独立审核不通过：YooAsset 1.5.7 的 `SceneOperationHandle.UnloadAsync()` 会在调用链内立即执行 `ReleaseInternal` 并清空句柄 Provider；原实现随后从失效句柄读取 `SceneObject`，导致请求场景和上一托管场景两条 Unity 兜底卸载都只能得到无效场景。
- `SceneLoadRequest` 现于加载成功且句柄有效时立即保存 `LoadedSceneSnapshot`；卸载上一活动场景前保存 `PreviousManagedSceneSnapshot`。两次 `UnloadAsync()` 之后的销毁确认和失败兜底只读取强类型 `Scene` 快照，不再访问已失效句柄的 `SceneObject`。
- 请求自身开始 YooAsset 卸载后，逐帧入口会在任何 `Handle.Progress/IsDone/Status` 读取之前分流到卸载状态机；句柄被卸载失效后不会再落回加载状态检查。加载阶段若句柄意外提前失效，也会避开其业务属性并按已知场景快照进入清理。
- 新增统一 `UnitySceneUnloadState`：请求自身场景、上一托管场景和普通旧场景都保存场景快照、当前 Unity 卸载操作和启动次数；每次操作必须等待 `isDone`，完成后再次确认快照 `isLoaded=false` 才允许继续。
- Unity `UnloadSceneAsync` 返回空或操作完成后场景仍加载时最多尝试三次。次数耗尽后管理器进入显式清理阻断态，取消运行和等待回调、隐藏 Loading 并拒绝后续切场景；不会继续启用新业务根，也不会每帧无限重试或静默叠加场景。
- 共享 Loading 新增 `_loadingOwnerGeneration`。显示时由最新请求取得代次所有权，刷新和隐藏都校验同一代次；旧请求失败或完成不能隐藏后来请求持有的 Loading。
- 最终提交顺序调整为：提交活动句柄和场景类型、清空 running/latest、由当前代次隐藏旧 Loading、启用 `SceneBusinessRoot`、执行完成回调。业务根 `Awake/OnEnable` 或完成回调同步调用 `LoadSceneMode` 时，新请求在旧隐藏动作之后重新显示 Loading，不存在旧请求事后关闭新 Loading 的路径。
- 静态时序推演覆盖：过期请求 YooAsset 卸载失败后使用 `LoadedSceneSnapshot` 有限兜底；上一托管场景卸载失败后使用 `PreviousManagedSceneSnapshot` 并阻止业务根启用；Unity 兜底返回空或完成但场景仍加载时有限重试并明确阻断；A→B→C 中每一场景都确认销毁后才释放串行槽；业务根生命周期和回调同步重入均由新代次接管 Loading。
- 执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/anim-ro-merge-rework9`，首次构建结果为 0 个错误、39 个既有警告；最终静态收口后再次执行同一构建命令确认结果不变。
- 静态扫描确认 `SceneMgr.cs` 未命中命名空间、`foreach`、三目运算符、`object[]`、`dynamic` 或旧共享 `_obs`；`git diff --check` 无新增空白错误。
- 当前仍未执行 Unity Play Mode 或 YooAsset 模拟清单构建；卸载失败注入、Loading 同步重入和场景业务根运行时生命周期顺序仍需在 Unity 中验证。

## 第十一次独立完整审核结论

- 同一独立检查 Agent 在 rework9 后从验收清单第 1 项重新执行全量审核，最终结论为通过，无阻塞项，不需要再次返工。
- 独立构建输出为 D:/unitypro/LYFMMORGP/_temp/animation-ro-eleventh-review-build，结果为 0 个错误、39 个项目既有警告。
- 旧动画资源入口和顶级类型零残留；MAnimationMgr.ClipInfo、MAnimator.MAnimInfo、ready/preparing、动作门控、混合树、真实剪辑安装、RO Controller、13 个槽位、12 条索引与 Collector 全部通过。
- Global 启动与角色加载时序、连接代次、异步进城、场景业务根隔离、卸载屏障、有限兜底、Loading 所有权和协议委托所有权全部通过静态复核。
- 规则扫描和差异空白检查通过。未执行 Unity Play Mode、YooAsset EditorSimulate、卸载失败注入、Loading 同步重入、真实 A→B→C 和断线重连；这些保留为后续运行态验证范围。