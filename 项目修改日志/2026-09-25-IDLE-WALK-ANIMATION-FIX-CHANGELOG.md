# Idle 状态误播移动动画修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`

## 修改内容

本地主角默认动作选择 Walk/Run 时，同时检查 `HasDirectionalInput` 和 `IsMoving`。旧控制器切回 Idle 时，即使移动运行态已经为 false，`SetMovingForPresentation(false)` 仍会清除方向标记和预测位移，避免残留方向让默认动作继续请求移动动画。

## 设计思路

`HasDirectionalInput` 表示输入层是否仍有方向键，`IsMoving` 表示移动组件是否仍在执行水平移动。两者必须同时成立才允许播放移动动画；任意一个为 false 都由默认动作返回 Idle。兼容状态入口负责同步清理输入标记，状态机继续使用原有 `MoveStopped` 刷新流程。

## 验证结果

- `GM_Game.csproj` 构建成功，0 个错误，38 个项目既有警告。
- 目标文件通过命名空间、`foreach`、三目运算符和业务无类型载荷扫描。
- Unity Play Mode 未执行，快速点按和 Animator 实际槽位仍需编辑器回归。

## 未解决问题

当前无法连接 Unity 编辑器，不能在本轮确认真实模型的 Animator 当前状态；若仍出现 Idle 逻辑对应 Walk 剪辑，应继续检查 `PresentTable.IdleAnim` 配置值及 Animator Controller 的 Idle 覆盖槽位。

## 兼容 Controller 修复

MJS 的 `AnimCtrl_Role_jx` 使用 `Movement/Action` 参数和 Blend Tree，不提供独立的 Idle/Walk 覆盖槽位。`MAnimator.Play/CrossFade` 在剪辑不可用时现在调用 `MAnimatorStateCompatibility.Apply`，将 Idle 的 Movement 参数设为 0，移动状态设为 2，避免 Animator 沿用旧 Walk 状态。

最终构建 0 个错误、39 个项目既有警告；Unity Play Mode 未执行。

## 审查返工

- 兼容 Controller 的 Movement 参数现在明确使用 `2=行走`、`3=跑步`，保留 Shift 跑步表现。
- 兼容 Idle 入口清除方向后，即使 `IsMoving` 已经是 false，也会在实体处于默认状态时调用 `MarkTriggered()`；跳跃、技能等特殊状态不会被抢占。
- 返工后构建 0 个错误、39 个项目既有警告；Unity Play Mode 未执行。
