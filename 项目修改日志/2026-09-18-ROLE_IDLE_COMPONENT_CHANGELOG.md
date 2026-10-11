# 2026-09-18 普通角色 RO 待机动画组件变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MIdleComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Builder/RoleEntityBuilder.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Builder/PlayerEntityBuilder.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`
- `LYF_MMO/GM_Game.csproj`

## 实现内容

- 新增 `MIdleComponent`，仅为普通角色装配 RO 风格待机剪辑。
- 模型 Animator 就绪后，读取 `PresentDataView.IdleAnim` 与 `IdleFAnim`，分别绑定到 `Idle` 与 `FIdle` Animator 状态槽。
- 角色骑乘普通载具或公共载具时跳过装配，避免覆盖载具动画表现。
- 角色构建器在创建本地玩家和其他角色时统一注册组件；组件进入普通组件容器，因此初始化、更新和对象池回收均走现有生命周期。
- `MDefaultActionComponent` 接收进入/退出战斗事件后唯一更新 `Entity.BattleIdle`；当前确为默认动作且非射击状态时，静止分支才选择 `FIdle`。
- 射击状态优先返回 `ShootIdle` 或原移动状态，不受 `BattleIdle` 抢占；特殊状态仅由当前状态机组件控制。
- 移除了默认动作组件中的三目运算符和 `FootSteps` 的 `object[]` 参数，脚步音效入口改为无参私有方法。

## 设计说明

待机剪辑属于角色长期存在的表现资源，不应由默认动作状态在每次播放时临时解析。`MIdleComponent` 负责一次性登记普通和战斗待机状态，默认动作组件只依据角色状态选择 `Idle` 或 `FIdle`。模型异步创建前，组件在每帧更新中等待；模型就绪后完成装配并请求状态机刷新。

## 验证结果

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/role-idle-component-build`。
- 首次构建：因自动生成的 `GM_Game.csproj` 未包含新增脚本，产生 2 个 `MIdleComponent` 未找到错误；已在工程清单加入该脚本后重新构建。
- 重构前构建结果：通过，0 个错误、39 个项目既有警告。
- 静态规则：新增组件和构建器修改文件未命中 `namespace`、`foreach`、三目运算符、`object`/`dynamic` 业务载荷。
- 差异检查：本功能目标源码当前均为未跟踪文件，使用 `git diff --no-index --check NUL <文件>` 逐个校验，命令未报告空白错误。该命令因 NUL 与现有文件存在内容差异返回退出码 1，此退出码不表示空白检查失败；判定依据为输出中不存在 trailing whitespace/new blank line 诊断。全局 `git diff --check` 受用户既有无关改动影响，不作为本功能通过依据。
- 工程清单：`GM_Game.csproj` 现已包含 `MIdleComponent.cs` 的 Compile 条目，命令行构建会编译该组件。Unity 下次刷新工程时仍可能重建该生成文件，实际 Unity 脚本编译以 `Assets` 下脚本和 `.meta` 为准。

## 未解决事项

- 需要 Unity 编辑器会话可用后，在主城运行态验证普通静止播放 `Idle`、战斗静止播放 `FIdle`，并确认骑乘载具时不覆写载具 Animator。
