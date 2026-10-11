# GateServer 与 GameServer 购买后断线修复记录

## 根因

商城购买成功后，GameServer 会发送 `CMD_CountUpdateSyncInfoCode`（11150）同步限购次数。GateServer 没有注册这个来自 GameServer 的协议，`NetClient.HandleCommand` 使用字典下标取处理器时抛出未注册命令异常，`OnReceiveCB` 捕获异常后关闭连接，日志中的 10054 是连接被重置的结果。

## 修改内容

- `LYF_Server/GateServer/GateServerApp.cs` 注册 11150 回包到 `GateRoleCtrl`。
- `LYF_Server/GateServer/Ctrl/GateClientResponseForwarder.cs` 增加 11150 的 `CountUpdateSyncInfo` 协议解析校验并透传给 Unity。
- `LYF_Server/GameServer/Components/RoleCountComponent.cs` 明确填写同步结果 `CmdCode=Succeed`。

## 验证结果

- GateServer 构建：0 个警告、0 个错误。
- GameServer 构建：0 个错误，仅 7 个既有警告。
- `git diff --check` 通过。
- 尚未进行真实商城购买四端联调。

## 部署注意

必须使用最新构建产物启动 GateServer 和 GameServer；购买时应确认 11150 次数同步与 11160 物品同步均能转发，且 Gate/Game 连接保持正常。
