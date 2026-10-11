# 跳跃落地继续移动与 Shift 走跑切换

## 需求

玩家长按方向移动时跳跃，空中与落地后应继续移动，不应要求重新按方向键；按住 Shift 使用跑步，松开 Shift 恢复普通移动。该功能必须使用 `SceneWorld.LocalRole` 与新实体 `MMoveComponent`，不依赖旧 `MainRoleCtrl/RoleCtrlBase`。

## 根因

- `MMoveComponent` 的 `_targetDir` 保存当前方向输入。起跳和 `EndJump()` 均没有清理它；只有停止移动事件或角色不可移动时才清除方向，因此水平输入本身没有在跳跃落地时丢失。
- `MJumpComponent` 播放落地动画时，落地动画计时结束后无条件直接播放 `Idle/FIdle`，绕过默认状态机，导致视觉表现像是停止移动。
- 新移动链每帧固定使用跑步速度；`MDefaultActionComponent.StateName` 也固定选择跑步动画。Shift 输入仅与旧控制器保留事件关系，没有更新新移动组件，因此新链没有走/跑状态。

## 修改文件与实现

- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`
  - Shift 按下和松开时读取按钮状态，并直接交给场景本地玩家 `MoveComp.SetRunning()`。
  - 不再通过旧控制器的 Shift 事件驱动移动。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`
  - 保存 Shift 跑步状态；跳跃期间不重置此状态。
  - 根据模式选择 `PlayerStats.topSpeed` 或 `runningTopSpeed`；走跑模式切换时将现有速度限制到新速度上限。
  - 起跳、落地和走跑切换均不修改 `_targetDir`；只有停止输入或明确不可移动才清方向。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`
  - 根据当前战斗状态、是否移动及 Shift 跑步状态选用对应 Walk/Run 动画。
  - 落地时默认状态机读取此时仍有效的移动方向和 Shift 状态，恢复正确动画。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`
  - 落地动画结束后不再直接播放 Idle；将表现权交回默认状态机并请求重新评估当前动作。

## 验证

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-movement-jump-run-build`：成功，0 个错误、38 个项目现存警告。
- 目标文件静态约束扫描：未发现 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`。
- `git diff --check`：通过。
- 未修改 `MainRoleCtrl.cs`、`RoleCtrlBase.cs`。

## 未解决与交接

- 当前 Agent 环境没有 Unity MCP 工具，尚未在 Play Mode 验证具体角色控制器和动画资源。
- 后续应在 Unity 中按住方向键起跳、保持方向至落地，确认角色不中断；分别在地面与空中按下/松开 Shift，确认速度和 Walk/Run 动画按当前输入切换。
- 协作验证记录见 `Docs/AI/TEST_REPORT.md`，当前任务记录见 `Docs/AI/CURRENT_TASK.md`。

## 注释门禁补充（2026-09-24）

- 在 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs` 为 `PlayerUpdate(int deltaTime)` 补充参数语义：帧间隔单位为毫秒，位移积分将负值按零处理；同时把方法摘要改为准确描述移动、跳跃速度和落地检测。
- 复核本轮触及的接口注释：`PlayerInputCtr.ApplyRunningState` 与 `MMoveComponent.SetRunning` 均有用途及参数含义；`MMoveComponent.IsRunning` 有状态语义；`MDefaultActionComponent.StateName` 已说明战斗、移动和 Shift 走跑状态决定动画槽。未发现其他缺失参数或返回值说明。
- 本次只改注释，不改运行逻辑；客户端构建成功，0 个错误、38 个现存警告。
