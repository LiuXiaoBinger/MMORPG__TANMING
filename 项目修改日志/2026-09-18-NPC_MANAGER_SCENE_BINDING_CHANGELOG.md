# 2026-09-18 测试场景 NpcManager 序列化绑定核验

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`：核验测试对象与 NpcManager、Global 的序列化引用；当前工作区已包含目标 `_npcManager: {fileID: 138351009}` 字段，因此不重复改写用户已有场景内容。
- `Docs/AI/CURRENT_TASK.md`：登记本次场景绑定核验状态。
- `Docs/AI/TEST_REPORT.md`：登记静态断言与构建结果。

## 实现与设计

测试对象 GameObject `138351006` 保留 test 组件 `138351007` 和 NpcManager 组件 `138351009`。test 的 `_sceneGlobal` 指向 `700000003`，`_npcManager` 指向同一 GameObject 上的 `138351009`；Global GameObject `700000001` 默认禁用，运行时由测试流程按既有逻辑启用。未修改任何业务脚本，也未删除 Global 或 NpcManager 组件。

## 验证结果

- 场景静态断言全部通过：测试对象组件列表、Global 引用、NpcManager 引用、组件归属、Global 默认禁用和 Global 脚本 GUID 均匹配。
- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/bind-npc-manager-build`：0 个错误，39 个既有警告。

## 未解决问题

Unity MCP 当前未完成 Play Mode 运行态复核；后续需确认 YooAsset 模拟清单、Global.SceneWorld 就绪及 NPC 初始化日志。
