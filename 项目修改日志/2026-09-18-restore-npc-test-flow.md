# 测试场景 YooAsset 与 NPC 初始化恢复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`
- `LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`
- `LYF_MMO/Assets/AssetBundleCollectorSetting.asset`

## 实现内容

- 修复 YooAsset 资源收集配置：删除 `AnimationIndex` 对 `Resources/ResInfo` 的重复收集，保留 `Resources` 组，避免 `animInfo.txt` 重复收集导致模拟清单生成失败。
- 测试脚本在默认包 `InitializeAsync` 成功后启用预置 Global，等待 `Global.SceneWorld` 创建完成后调用一次 `NpcManager.Initialize()`。
- 测试场景保留默认禁用的 Global，并将 NpcManager 恢复挂载到测试对象。
- 测试对象的 `_npcManager` 序列化引用已绑定到场景中的 NpcManager 组件，运行时不会因引用为空而跳过初始化。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/restore-npc-test-build`：0 个错误，39 个既有警告。
- 静态检查确认 `AnimationIndex` 组为 0、`Resources` 组为 1、无 ResInfo 重复收集路径；目标测试脚本未使用 namespace、foreach、三目运算符或弱类型载荷。
- 已尝试通过 Unity MCP 进入 New Scene Play Mode；播放期间 MCP WebSocket 断开，无法完成本轮 Console 运行态验证，详情见 `Docs/AI/TEST_REPORT.md`。

## 未解决问题

- Unity MCP 恢复后需重新执行 Play Mode，确认模拟清单成功、Global.SceneWorld 就绪以及 NPC 实例化日志。
