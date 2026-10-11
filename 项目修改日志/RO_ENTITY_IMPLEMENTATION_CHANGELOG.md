# RO 风格客户端实体与动作状态实现

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/`：新增动作状态键、状态容器和状态机实现。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/`：新增 RO 默认、移动、吟唱、技能、特殊、受击、死亡及扩展动作状态。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MComponentMgr.cs`：增加动作状态池。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MObject.cs`、`MEntity.cs`、`ROObject.cs`、`SceneEntityWorld.cs`：接通 LateUpdate 生命周期和毫秒/秒转换。
- `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Role/SceneWorldSystem.cs`：转发实体 LateUpdate。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Builder/`：不再挂载废弃的 `EntityViewComponent`。

## 实现内容

- `MEntity` 直接持有 `MModel`，模型内部持有 `MAnimator`。
- `MStateMachine` 初始化时注册 RO 动作状态，默认进入 Idle，支持状态许可、切换、完成回默认和动画延迟播放。
- 动作状态由 `MComponentMgr` 专用池创建和回收，不进入普通实体组件容器。
- 场景世界更新链路为 `Update(毫秒)`、`LateUpdate(秒)`，状态动作按秒推进。
- 原有 `RoleFSM` 保留，不与新状态机共享动画写入入口。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug`：0 错误，31 个既有警告。
- 构建日志：`_temp/client-entity-build3.log`。

## 未解决问题

- 尚未执行 Unity Play Mode 下模型资源加载、Animator 状态切换和输入回归。
- `EntityViewComponent.cs` 文件仍保留以兼容旧工程引用，但 Builder 和实体表现路径已不再使用；后续可在确认无外部引用后删除。
# 2026-09-14 动作状态注册冲突修复

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/MStateMachine.cs`。
- 实现内容：按 RO 的职责将普通移动表现留在 `MDefaultActionComponent`，状态机改为注册独立的 `MFearRunActionComponent`，不再把 `MMoveActionComponent` 当作独立状态注册。
- 设计原因：`StateDefine` 没有普通移动状态；此前两个组件共用 `kStateFearRun`，会造成状态键重复及池对象未正确纳入容器。
- 验证结果：客户端 Debug 构建成功，0 个错误、31 个既有警告；`SceneEntity` 静态扫描结果为 `namespace=0`、`foreach=0`、三目运算符=0。
- 未解决问题：尚未执行 Unity Play Mode、真实模型资源加载及 Animator 状态切换运行验证。
- 附加修正：移除 `UpdateSpecialComponents` 中对状态机 `LateUpdate` 的提前调用，状态动画现在只由 `SceneWorldSystem → SceneEntityWorld → MEntity.LateUpdate` 帧末链路驱动一次。
# 2026-09-14 RO 实体、状态、动画与对象池完整返工

- 修改范围：客户端 `SceneEntity` 全链路、`RoleCtrlBase.cs`、`Global.cs` 与 `GM_Game.csproj`；服务端未修改。
- 实体继承修正为 `PlayerEntity : RoleEntity`、`EagleEntity : DummyEntity`，并为派生类型保留具体 `EntityType` 构造入口；`PlayerEntity.Update` 改为统一世界可调用的 override。
- 新增 `MEntityPool`，Builder 从强类型实体池获取具体实体，`SceneEntityWorld.Remove/Clear` 在 Destroy 后回收；复用时重置身份、生命周期、Transform、模型、事件、组件与具体实体运行数据。
- 普通组件改为通过 `MComponentMgr` 获取并真正回池；Marker 仍是不池化边界。Move/Buff/Skill/Machine 分别使用 RO 名称 `MMoveComponent/MBuffComponent/MSkillComponent/MStateMachine`，且不进入普通组件集合。
- `MSkillComponent` 同时作为技能快捷组件和技能动作状态，只进入动作状态池，并由 `MStateMachine` 唯一回收，避免双池重复持有。
- 动作状态类型按 RO 命名，状态机按角色、怪物、NPC、Dummy/Eagle、采集物/陷阱/飞行物差异装配；各 Builder 拆分最小公共、战斗、怪物组件集合并补齐具体类型 Marker。
- 默认动作订阅移动开始/停止并按 `Entity.IsMoving` 选择 Idle/Move；技能、受击、死亡、特殊、眩晕等动作通过具名事件入口驱动，真实状态变化派发 `EntityStateChangedEventArgs`。
- 旧 `RoleFSM` 保留；绑定 `MEntity` 后 `RoleCtrlBase.ChangeState` 只桥接新状态机事件，不再让旧 FSM 写同一个 Animator。未绑定实体的兼容对象仍可使用旧 FSM。
- `MAnimator` 补齐 Host、IsLoaded、InState、Rebind、GetCurrentAnimInfo 等接口，统一动画常量；新增 `MAnimatorPool`，`MModel` 不再反复 new 包装器，动画片段包装在覆盖和回收时释放。
- 修复异步/外部模型绑定丢失变换、初始 Animator 未加载时过早清除状态刷新标记，以及缺失 `Global.LateUpdate` 的问题；当前只有 `Global → SceneWorld → SceneEntityWorld → MEntity → Machine` 一条帧末动画驱动链。
- 验证：客户端构建 0 错误；最终增量构建 2 个既有程序集冲突警告。目标范围静态扫描 `namespace=0`、`foreach=0`、三目运算符=0；旧同义类型引用为 0。
- 未解决：尚未执行 Unity Play Mode、真实 YooAsset 模型/动画句柄联调、角色/怪物状态切换和实体池多轮运行回归；动画片段适配当前统一经过 `MAnimationAssetProvider`，其底层仍是 Resources，后续需替换为项目 YooAsset 句柄实现。

## 2026-09-14 最终测试复验

- 三组返工均通过静态验收：本地玩家入世界通知顺序正确；实体、NPC、普通组件和动作状态池在失败/销毁路径闭环；状态许可矩阵与 Animator Controller lease/兼容映射已接通；旧 FSM 与新状态机隔离。
- 客户端独立 Debug 构建 0 错误、31 个既有警告；规则扫描、工程登记和 `git diff --check` 通过。
- 最终结论：静态验收通过，尚未进行 Unity Play Mode、真实资源/动画、池多轮运行及四端网络联调。
- 测试产物：`_temp/ro-state-final-retest-20260914/`，等待产品经理确认清理。

# 2026-09-14 独立测试结论与返工项

- 测试结论：不通过。客户端 Debug 构建为 0 个错误、31 个既有警告，规则扫描和 95 个实体源码工程登记检查通过。
- 阻塞原因：`SceneWorldSystem.LoadLocalRole` 在初始化本地玩家后未调用 `PlayerEntity.AfterAddedToWorld`，正常创建链路不会触发角色组件的入世界通知。
- 返工要求：在发布 `LocalRoleChanged` 前补齐一次防重复的入世界通知，并重新进行构建、静态规则和对象池复用生命周期复验。
- 测试记录：详细证据见 `Docs/AI/TEST_REPORT.md`；构建及扫描产物位于 `_temp/ro-state-final-test-20260914/`。
- 未解决问题：未执行 Unity Play Mode；状态机对象重载的转移校验、异步模型失效回收和实体世界添加失败回收仍需审核与补测。

## 2026-09-14 首次返工复验结果

- 本地角色入世界通知已修复，调用顺序和对象池复用重置逻辑复验通过。
- 客户端独立构建为 0 个错误、31 个既有警告；规则扫描、95 个实体源码工程登记和差异空白检查通过。
- 整体仍不通过：主城异步模型回调缺少原始实体身份校验和失效对象销毁；实体世界创建接口忽略 Add 失败且未回收 Builder 产物。
- 普通组件创建的双 `RebindOwner` 可能破坏事件自动解绑记录，等待审核最终分级；状态机对象重载未重复校验与 RO 参考一致，当前按非阻塞加固处理。
- 复验产物：`_temp/ro-state-retest-20260914/GM_Game-retest.log` 与 `_temp/ro-state-retest-20260914/build/`。
