# 主角模型被连接失败回调回收修复说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/Global.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`

## 修复内容

- 连接建立前只保存待定连接代次，`CurrentConnectionGeneration` 保持为零；底层连接成功后才发布有效代次和 `IsConnected` 状态。
- `Global.SubmitLocalRoleLoad` 仅为真实已连接会话记录代次，离线测试主角使用零代次，避免 10061 失败回调清理测试角色。
- 测试场景检测到持久化 `Global` 时不再启用第二个实例；场景中已有任意 `LocalRole` 时不重复提交测试角色包，避免 `LoadLocalRole.Clear` 清理真实主角。
- P1 修复：连接代次为零的初次连接失败仍通过 Unity 主线程执行 `connFail`，但不执行商城或角色会话清理；正代次失败路径保持原有清理逻辑。

## 设计原因

之前连接尝试阶段就写入有效代次，服务器未启动时 10061 失败回调会进入 `SceneWorld.Clear`，再经过实体池和 `MModel.Reset` 销毁已绑定的 `Role_MJS`。现在只有真实连接成功才建立会话归属，离线测试模型不会被初次连接失败清理。

## 验证结果

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-connection-fail-callback-build`
- 构建结果：0 个错误，39 个既有警告。
- Unity Play Mode 未复验：当前仅完成静态链路修复和 .NET 构建，需在 Unity 编辑器空闲后确认无服务器运行时模型绑定后不再被 10061 回收。

## 竞态补充修复

- 修改：零代次连接失败回调进入 Unity 主线程后，除客户端引用校验外增加 `CurrentConnectionGeneration == 0` 校验。
- 行为：如果回调排队期间自动重连已经成功，旧的 `connFail` 回调直接忽略，不再误报连接失败或影响新会话。
- 最终构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/final-model-recycle-fix-build2`，0 个错误、39 个既有警告。
- 未执行：Unity Play Mode 断线与自动重连交错场景的运行态回归。
