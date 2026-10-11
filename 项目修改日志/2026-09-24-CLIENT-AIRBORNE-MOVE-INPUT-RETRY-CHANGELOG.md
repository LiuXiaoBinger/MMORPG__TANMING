# 空中按前进后落地仍切 Idle 修复

- 日期：2026-09-24。
- 需求：先起跳，再在空中按住前进直到落地；落地不能丢失方向并直接停在 Idle。空中松开方向仍应立即停止。
- 本次追加修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`。`PlayerInputCtr.cs` 中已存在的 33ms 输入采样是此前工作区实现，本次没有改动该文件。
- 根因：`Movement` 是 Value 类型 Vector2 动作，started/performed 在输入值变化时派发一次。如果边沿抵达时 `MMoveComponent.onMoveEvent` 因 `Entity.IsMovable == false` 提前返回，输入采样会重试；但即便方向后来被接收，`PlayerUpdate` 仍会在 `!Entity.IsMovable` 分支调用 `stopMove()`，清除 `_targetDir` 和 `_isMoving`。因此跳起后才按前进的方向可能在落地前再次被清掉。
- 实现：复用此前已有的约 33ms 输入采样重发逻辑。`MMoveComponent.onMoveEvent` 在跳跃期间允许记录方向，`PlayerUpdate` 也不会因地面动作状态的可移动门槛清除跳跃期间已接收的方向；非跳跃状态仍保留原来的不可移动处理。按键松开仍由 Movement.canceled 即时发送停止事件。
- 交接链核对：输入被接收后 `onMoveEvent` 会设置 `_targetDir` 并调用 `SetMoving(true)`；跳跃期间 `MDefaultActionComponent.PlayAnim` 延迟默认动画，`EndJump` 不清理移动方向；落地动画结束时 `MJumpComponent` 调用 `ForceToDefault()` 和 `MarkTriggered()`，随后默认动作组件读取 `Entity.IsMoving` 决定 Idle 或 Walk/Run。因此无需修改旧 `MainRoleCtrl`，也不需改落地动画状态机。
- 验证：客户端 `GM_Game.csproj` 构建 0 错误，38 个项目既有警告；修改文件静态约束扫描通过。全工作区 `git diff --check` 受多个既有文件的空白字符问题影响，未把这些无关文件纳入修复。
- 未解决：未执行 Unity Play Mode；需在专用测试场景验证“跳跃后空中按住前进直到落地”以及“空中按前进后松开”。运行时结果不能由本次静态检查替代。
- 相关测试记录：`Docs/AI/TEST_REPORT.md`；当前任务：`Docs/AI/CURRENT_TASK.md`。
- 独立复核：测试 Agent 对事件、移动状态和落地动画链路作静态审查，结论通过；重建 GM_Game 仍为 0 错误、38 个既有警告，目标文件约束扫描和空白检查通过。
- 实机验收状态：Unity MCP 在当前会话不可用；桌面连接工具返回 `unsupported Codex auth method: apikey` 且未返回 Unity 应用。没有执行 Play Mode，因此不可将需求标记为功能测试通过；需在可连接 Unity 的会话中完成两条输入回归，失败再返工。
