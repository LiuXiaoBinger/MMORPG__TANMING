# 跳跃落地移动动画直达修复

- 日期：2026-09-24。
- 需求：跳跃期间按住方向直到落地时，角色应直接进入 Walk/Run，不能先进入 Idle；落地前松开方向时应正常停止。
- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`。
- 设计思路：输入状态和移动运行态分离。`HasDirectionalInput` 表示方向键是否仍保持，`IsMoving` 继续表示移动逻辑是否运行，落地动画选择不得仅依赖后者。
- 修改内容：移动事件方向非零时设置方向输入标记，停止事件和回收时清除；本地主角默认动作按该标记选择 Idle 或 Walk/Run；落地时仍保持方向输入则直接交回默认状态并刷新 Walk/Run，跳过会产生 Idle 闪帧的落地中间态，无方向输入仍保留原落地动画。
- 约束：未修改旧 `MainRoleCtrl`、`RoleCtrlBase`，未新增命名空间、`foreach`、三目运算符或业务 `object/dynamic`。
- 验证：`GM_Game.csproj` 构建成功，0 错误、38 个项目既有警告；目标文件静态规则和空白检查通过。
- 第一次审查：不通过。无方向落地分支存在同帧 Idle 覆盖 `Jump_End`，原因是停止移动遗留的状态机刷新标记在 `IsJump=false` 后仍可触发默认动画。
- 返工：跳跃组件增加只读落地动画保护状态；默认动作在空中和落地动画期间都不切换动画，`Jump_End` 完成并解除保护后才允许进入 Idle。移动落地仍直接进入 Walk/Run。
- 第二次审查：独立测试静态逻辑及编译审查通过；返工构建 0 错误、38 个既有警告。
- 未解决：未执行 Unity Play Mode，仍需在专用测试场景用真实输入验证持续按住和落地前松开两条路径。
- 相关记录：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。

## 2026-09-24 输入状态收敛返工

- 返工原因：用户真实运行确认“起跳后在空中按住前进，落地仍切换 Idle”，证明此前仅修改落地状态判断并未解决输入状态来源问题。
- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`。
- 根因：`Movement.started` 和 `Movement.performed` 的即时移动派发被注释，角色只能等待 33ms 周期采样；周期采样只补发非零移动，不会在零输入时校正残留方向，输入边沿、角色生命周期和移动组件状态可能失去一致性。
- 实现：输入控制器新增明确的移动输入保持状态；开始和方向变化时立即派发最新方向，取消时立即清除缓存并停止；33ms 周期同时校正非零持续输入和零输入残留；组件禁用或销毁时主动停止。移动组件在方向标记由 true 变 false、但移动运行态已经为 false 时补发默认动画刷新，避免表现状态无法回到 Idle。
- 设计边界：落地仍由 `MJumpComponent` 根据 `HasDirectionalInput` 决定直接进入 Walk/Run 或播放 `Jump_End`；未恢复旧 `MainRoleCtrl` 移动链，也未在动画层强制覆盖真实移动状态。
- 构建验证：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/air-jump-input-convergence-build/output --verbosity quiet` 成功，0 个错误、38 个项目既有警告。
- 静态验证：目标文件未新增 `namespace`、`foreach`、三目运算符、`object[]` 或 `dynamic`；目标文件 `git diff --check` 无空白错误，仅报告既有行尾转换提示。
- 运行验证：Unity MCP 未暴露可调用实例；桌面控制连接返回 `apps=[]` 和 `unsupported Codex auth method: apikey`，本 Agent 无法执行 Play Mode。运行验收仍需独立测试 Agent 或可连接 Unity 的会话完成。
