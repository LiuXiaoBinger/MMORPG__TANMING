# 测试主角禁止主动重连变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/Global.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`

## 实现内容

- 删除测试脚本在连接代次为零时调用 `NetSocketMgr.ConnectServer` 的兜底逻辑。
- 测试场景不主动连接或断开网络，SceneWorld 就绪后直接提交模拟开始游戏角色包。
- `NetSocketMgr.IsConnected` 仅在底层连接成功回调后为真；连接尝试阶段的代次不再被当成已连接。
- Global 仅在 `IsConnected` 为真时绑定网络连接代次；离线/测试角色固定使用零代次，避免连接失败回调清理测试角色。
- 测试场景检测到已有持久化 Global 时不再启用第二个 Global；已有任意本地主角时不提交测试包，避免 `LoadLocalRole.Clear` 清掉真实角色。
- 本地主角测试包继续使用 `RoleJobtype.MJS`，由 `MainCityMgr` 选择 `Role/Role_MJS`。
- `Role/Role_MJS_Other` 仍只用于其他玩家同步，不参与本地主角生成。

## 设计原因

`ConnectServer` 会先断开当前连接，断开回调会清理 Global 的本地角色会话并回收场景模型。连接代次在连接成功前就会分配，不能作为连接成功标识；测试脚本不应接管网络生命周期，离线测试角色使用零连接代次，真实网络角色仍按连接代次隔离断线会话。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-local-role-no-reconnect-build`
- 结果：0 个错误，39 个既有警告。
- Unity MCP Play Mode 已验证：`New Scene` 成功加载 YooAsset 模拟清单和动画索引，测试角色提交状态为 `Loaded`，主角模型绑定路径为 `Role/Role_MJS`，本轮无错误和警告。

## 2026-09-18 离线测试主角实际运行验收补充

- 无服务器监听端口时运行 `New Scene`，仍成功提交模拟主角包并绑定 `Role/Role_MJS`。
- Console 出现：`动画索引加载完成`、`模拟主角角色包已提交，Global 状态：Loaded`、`角色表现绑定成功，角色 UID：2，资源路径：Role/Role_MJS`。
- Game View 截图已移入 `D:/unitypro/LYFMMORGP/_temp/main-role-offline-playmode/screenshot-20260918-233329.png`，可看到主角模型。
- Play Mode 已停止。
