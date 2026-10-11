# 2026-09-18 YooAsset 测试入口最小化修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`
- `LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`

## 实现内容

- 测试入口先复用或创建 `DefaultPackage`，设置默认包，再执行编辑器模拟清单生成。
- 对 `SimulateBuild` 返回值同时检查空值和文件存在性；失败直接输出中文错误，不调用 `InitializeAsync`。
- 仅在默认资源包 `InitializeAsync` 成功后启用场景预置 Global，避免重复创建或重复启用。
- 测试入口不再承担 SceneWorld 超时、NpcManager 或 UIRoot 初始化；场景移除测试对象上的 NpcManager，并保留默认禁用的 Global。

## 设计思路

日志显示 `Assets/GM_Game/Resources/ResInfo` 是无效收集路径，导致模拟构建返回空清单，进而触发空路径异常。测试脚本应在清单路径验证阶段终止并说明配置原因；Global 必须是场景对象，只有资源包初始化成功后才激活。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/test-yooasset-simple-build`：0 个错误、39 个既有警告。
- 目标文件禁用 `namespace`、`foreach`、三目运算符、`object`/`dynamic` 扫描通过。
- `git diff --check` 无空白错误。

## 未解决问题

YooAsset 收集配置中的 `Assets/GM_Game/Resources/ResInfo` 仍需由资源配置流程修正；本次按限定范围未修改该配置，也未执行 Unity Play Mode 和真实 EditorSimulate 成功验证。
