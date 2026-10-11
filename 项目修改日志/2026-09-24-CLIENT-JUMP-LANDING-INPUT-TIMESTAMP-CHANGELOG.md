# 跳跃落地输入时序修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`

## 修改内容

- 移动周期对账改为先检查 `Movement.IsPressed()`，动作已取消时强制使用零向量，避免 Input System 的残留值在落地或松开后重新驱动角色。
- 在 `Movement.canceled` 中保存 Input System 事件时间戳；`started/performed` 回调如果早于或等于最近一次取消时间，则视为排队旧事件并丢弃。
- 输入控制器重新启用时清空取消时间戳，确保新一轮按键可以正常开始移动。

## 设计思路

跳跃组件只根据移动组件保留的方向决定落地后进入 Walk/Run 或 Idle。输入层必须保证这个方向状态与真实按键一致：按住方向时，33ms 对账持续补发方向；释放方向后，取消回调和实时按压检查共同发送停止；旧回调不能越过取消事件恢复方向。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-jump-held-input-build/output --verbosity quiet`：0 个错误，保留项目既有 38 个警告。
- 目标文件约束扫描通过：未新增 `namespace`、`foreach`、三目运算符、`object[]` 或 `dynamic`。
- `git diff --check` 未发现目标代码空白错误。
- Unity Editor Play Mode 未执行；当前 Unity MCP 和桌面连接不可用，不能宣称真实输入验收通过。

## 未解决问题与交接

- 需要在可运行 Unity 的专用测试场景验证：空中按住方向落地继续移动、空中松开后落地停止、地面快速点按方向后立即停止。
