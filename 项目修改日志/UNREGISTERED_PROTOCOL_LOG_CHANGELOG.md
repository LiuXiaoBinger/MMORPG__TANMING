# 未注册协议日志处理记录

## 修改内容

- `LYF_Server/Network/Socket/NetClient.cs`：收到未注册协议时使用 `TryGetValue`，输出 Warn 日志并丢弃消息。
- `LYF_Server/Network/Socket/com/Session.cs`：客户端会话收到未注册协议时使用相同处理，避免异常冒泡到接收回调。
- 日志包含 `protoCode`，便于定位哪个服务遗漏了协议注册。

## 行为

日志格式：`command not regist, protoCode=协议号`。

未知协议不会再触发字典异常，也不会因此关闭 GateServer/GameServer 连接；但该协议对应业务消息会被丢弃，仍需补充正式注册和处理器。

## 验证

GateServer 构建 0 警告、0 错误；GameServer 构建 0 错误。详细结果见 `Docs/AI/TEST_REPORT.md`。
