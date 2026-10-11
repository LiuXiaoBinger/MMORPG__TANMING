# 跳跃动画组件与表现表变更说明

## 修改文件

- `Luaban/MiniTemplate/Datas/#PresentTable.xlsx`
- `LYF_Server/Network/LubanManage/Codes/PresentTable.cs`
- `LYF_Server/Network/LubanManage/Tb/tbpresenttable.bytes`
- `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimationStateNames.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Builder/PlayerEntityBuilder.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Builder/RoleEntityBuilder.cs`
- `LYF_MMO/GM_Game.csproj`

## 实现内容

PresentTable 新增 `JumpStartAnim`、`JumpLoopAnim`、`JumpEndAnim` 三个动画字段，当前配置使用 `Anim_JX_Jump_Start`、`Anim_JX_Jump_Loop`、`Anim_JX_Jump_End`。

动画状态键按实际 `AnimCtrl_Role_jx.controller` 对齐为 `Jump_Start`、`Jump_Loop`、`Jump_End`，避免原生 Animator 状态名不匹配。

新增 `MJumpComponent`，参考 `MIdleComponent` 的生命周期等待模型 Animator 就绪，并统一登记三阶段动画。一段、二段、三段跳不重复加载动画，均复用同一组状态。

`MMoveComponent` 增加 `BeginJump(int jumpCount)` 和 `EndJump()` 明确入口。`BeginJump(1/2/3)` 每次都会派发起跳事件，解决空中二段跳、三段跳时布尔状态未变化导致动画不重播的问题；组件不执行重力或坐标位移，避免引入第二套物理逻辑。

## 验证结果

- Artifact Tool 表格复读、渲染通过。
- Luban 生成通过。
- Network 构建 0 错误、0 警告。
- GM_Game 构建 0 错误，保留项目既有警告；状态键修正后的 final-build4 仍为 0 错误。
- 目标文件静态约束和 `git diff --check` 通过。

## 未解决问题

尚未在 Unity Play Mode 中验证实际资源加载。需要确认 `animInfo.txt` 已登记三条动画，并在专用测试场景调用 `MMoveComponent.BeginJump(1/2/3)` 与 `EndJump()` 验证起跳、循环和落地表现。

## 组件类型索引冲突修复

`EntityComponentContainer` 使用 `EntityComponentType` 作为唯一索引。为防止 `MJumpComponent` 使用 `View` 覆盖 `MIdleComponent`，在已有公共枚举中追加 `EntityComponentType.Jump = 31`，并将 `MJumpComponent.ComponentType` 切换为该独立类型。已有枚举值未调整，网络协议和移动物理逻辑未修改。

验证结果：MMoRpgCommon、Network、GM_Game 均构建通过；构建输出位于 `_temp/jump-component-type-*`；审核 Agent `jump_animation_review` 已通过；Unity Play Mode 尚未执行。

## 通用 Animator 跳跃槽位补齐

## 重复挂载修复

此前 Controller 实际存在结构错误：三个 Jump 状态既在 `Base Layer` 根节点，又在 `Jump` 子状态机中，导致 Animator 窗口仍显示根节点状态。本次已删除根节点的三个状态引用，仅保留 `Base Layer` 对 `Jump` 子状态机的挂载；`Jump` 内继续保留 `Jump_Start`、`Jump_Loop`、`Jump_End`。

审核 Agent 已按实际 YAML 复核通过。文本结构和 GM_Game 构建验证通过，但 Unity Play Mode 尚未执行；运行前需要重新导入 Controller 并刷新 YooAsset 模拟清单。

`MainCityMgr` 使用通用 `Assets/artres/Resources/Controller/MAnimator.controller`，原 Controller 缺少跳跃原始剪辑键，导致异步加载的角色跳跃剪辑无法写入 `AnimatorOverrideController`。本次在 `Assets/artres/Resources/Anims/Default` 新增三个同名占位剪辑 `Jump_Start`、`Jump_Loop`、`Jump_End`，并在通用 Controller 的 `Jump` 子状态机中创建三个同名状态分别引用它们。`MAnimator` 增加 `Base Layer.Jump.<状态>` 的状态路径解析，普通状态保持兼容；没有把角色统一切换到 JX 专用 Controller。

验证结果：GM_Game 构建 0 错误；Unity Editor.log 显示三个占位剪辑和通用 Controller 成功导入；审核 Agent `jump_animation_review` 已通过覆盖键匹配；Unity Play Mode 尚未执行。
