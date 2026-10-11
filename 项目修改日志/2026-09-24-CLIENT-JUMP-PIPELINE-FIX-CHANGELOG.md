# 客户端跳跃链路与 Special 误触发修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/controller/RoleController/MainRoleCtrl.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/controller/RoleController/RoleCtrlBase.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEntity.cs`

## 实现内容

- 跳跃只在 Input System 的 `Jump.started` 阶段触发，输入控制器直接取得 `Global.Instance.SceneWorld.LocalRole` 并调用 `MoveComp.TryJump()`。
- 删除 `PlayerInputCtr.JumpingEvent`、松键触发逻辑及 `MainRoleCtrl.JumpKeyPress()`，新跳跃不再依赖旧 `MainRoleCtrl`、`RoleFSM` 或 `RoleState.Jump`。
- `MMoveComponent` 统一管理一段、二段、三段跳计数；每次合法空中续跳重新设置向上速度并复用起跳动画事件，落地后次数归零。
- `MMoveComponent` 统一积分向上速度、上升/下落重力和最大下落速度，并把水平与垂直位移合并为每帧一次 `CharacterController.Move`。
- `MEntity.Update` 恢复专用组件驱动，使移动组件、状态机等实体组件进入正常帧更新。
- 绑定新场景实体后，`RoleCtrlBase.Update` 和 `OnAnimatorMove` 不再执行旧移动、重力和根运动，避免同一个 CharacterController 被两套系统重复移动。
- `RoleCtrlBase.RouteStateToEntity` 的未知状态、旧 Jump 和旧 Slider 不再派发 `SpecialActionStarted`，统一停止移动并回到默认 Idle 状态；Special 只保留显式业务事件入口。

## 设计思路

本地移动只保留一条权威链路：`PlayerInputCtr → PlayerEntity.MoveComp → CharacterController`。`MJumpComponent` 继续只负责 Jump_Start、Jump_Loop、Jump_End 动画表现，不修改坐标；物理状态和动画事件都由 `MMoveComponent` 的同一跳跃生命周期产生，避免动画跳跃与实际位置脱节。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-jump-component-fix-build-final`：构建成功，0 个错误，38 个项目既有警告。
- 新增代码未使用 `namespace`、`foreach`、三目运算符、`object[]` 或 `dynamic`。
- 全项目检索确认不存在 `JumpingEvent`、`JumpKeyPress` 和 `DispatchEvent(MEventType.SpecialActionStarted...)` 隐式派发。
- 目标文件 `git diff --check` 通过。

## 未解决问题

- 当前会话没有可用 Unity MCP 实例，无法自动执行 Play Mode。需要在专用测试场景实测连续三次空格、第四次拒绝、落地次数重置、移动中跳跃和无输入落地，并确认控制台不再出现 Special 状态告警。

