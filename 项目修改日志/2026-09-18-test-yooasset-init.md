# 测试脚本 YooAsset 初始化时序修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`

## 实现内容

- 测试脚本启动协程后依次执行 YooAsset 初始化、创建并设置 `DefaultPackage`、编辑器模拟清单初始化。
- 等待 `InitializationOperation` 成功后才添加 `Global`，避免 `Global.Awake` 在资源包未初始化时调用 `YooAssets.GetPackage`。
- 增加包为空、初始化操作为空、初始化失败以及测试场景缺少 `NpcManager`/`UIRoot` 时的中文日志。
- 未修改 `Global.cs`、其他正式业务代码或场景资源。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-yooasset-init-build`：0 个错误，39 个既有警告。
- 目标脚本静态检查：无 `namespace`、`foreach`、三目运算符和弱类型业务载荷。

## 未解决问题

- 尚未在 Unity Play Mode 中实测 EditorSimulate 清单和测试场景运行。
- 测试脚本仅允许挂载专用测试场景或手动临时对象，不应挂载到正式场景或正式预制体。
