# 2026-09-25 快速松开方向键输入时序修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`

## 修改内容

- 移动周期采样改为只接受 `Movement.IsPressed()` 仍为真的方向输入。
- 记录 `Movement.canceled` 的 Input System 时间戳，取消之后到达的旧 started/performed 回调不再重新启动移动。
- 零向量、取消、组件禁用和销毁都会清理输入缓存并派发停止事件。

## 设计思路

输入回调负责低延迟响应，33ms 周期采样负责对账。对账读取动作的实时按压状态，而不是直接相信缓存向量；取消事件建立时间边界，避免 Input System 队列中的旧方向覆盖停止结果。移动组件仍负责清理方向、预测位置和移动动画，输入层不保存角色生命周期引用。

## 验证结果

- `GM_Game.csproj` 构建成功：0 个错误，38 个项目既有警告。
- 目标文件未新增 `namespace`、`foreach`、三目运算符、业务 `object/dynamic`；`git diff --check` 通过。
- Unity Play Mode 未执行：当前 Unity MCP 和桌面连接不可用，不能把静态构建当作实机验收。

## 未解决问题与交接

在可用 Unity 会话中验证：地面快速点按后停止、快速反向后停止、空中按住方向落地继续移动、空中松开后落地停止。
