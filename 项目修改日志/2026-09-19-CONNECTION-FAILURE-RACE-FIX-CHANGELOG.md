# 连接失败回调竞态修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs`

## 实现内容

- 在初次连接失败的主线程回调中增加 `CurrentConnectionGeneration != 0` 检查。
- 如果失败回调排队期间已经建立新连接，则忽略旧回调，避免误触发连接失败处理。

## 设计思路

连接失败回调可能晚于自动重连成功回调进入主线程队列。零代次失败回调只允许作用于仍未建立连接的状态，因此必须同时校验客户端引用和当前连接代次，保证过期回调不会影响新连接。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-connection-fail-race-build`：通过，0 个错误，39 个既有警告。

## 未解决问题与下一步

- 尚未在 Unity Play Mode 中重复验证断线与自动重连交错时的运行态日志；需要编辑器空闲后继续验证。
