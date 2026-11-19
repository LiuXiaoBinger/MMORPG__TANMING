# 参考 Ro 接入客户端 GC

任务编号 `RO-CLIENT-GC-20261119`。在启动、成功切换场景和系统低内存通知三个时机接入主动托管回收。

## Ro 源码依据

- `clientcode/CSProject/MoonClient/Stage/MLoginStage.cs` 的 `OnEnterStage` 在进入登录阶段时调用 `System.GC.Collect()`。
- `clientcode/CSProject/MoonClient/Effect/Scene/MEnvorimentSetting.cs` 的 `BatchObject` 在静态合批结束后调用 `MResLoader.UnloadUnusedAssetsAndSystemGCCollect`。该方法发起 Unity 未使用资源卸载，随后调用 `GC.Collect()`。
- `clientproj/Assets/Scripts/MScript.cs` 订阅 `Application.lowMemory`，收到通知后使用 `Time.time` 以 600 秒间隔限频回收。`clientproj/Assets/Scripts/Lua/Main.lua` 为 Lua 堆设置 `setpause=100`、`setstepmul=5000`，并在 `OnLevelWasLoaded` 中调用 `collectgarbage("collect")`。

## 文件清单

| 路径 | 修改内容 |
| --- | --- |
| `LYF_MMO/Assets/Boot/Load.cs` | 资源更新和代码装载完成后，在加载首个登录场景前做一次 `GC.Collect()`。 |
| `LYF_MMO/Assets/LYF_Game/Scripts/Manger/SceneMgr.cs` | `Single` 场景加载成功并完成导航替换后，由本次资源包卸载引用计数为零的资源，再做一次 `GC.Collect()`，随后启用业务根节点并执行回调。加载界面的关闭和失败分支沿用原有顺序。 |
| `LYF_MMO/Assets/LYF_Game/Scripts/Global.cs` | 配对订阅和取消订阅 `Application.lowMemory`。限频沿用 600 秒，使用 `Time.unscaledTime`，游戏暂停时仍按未缩放时间计算。 |
| `Docs/AI/CURRENT_TASK.en.md`、`Docs/AI/CURRENT_TASK.md` | 增加本任务的三个回收入口和续作定位。 |
| `Docs/AI/TEST_REPORT.en.md`、`Docs/AI/TEST_REPORT.md` | 增加本任务的编译、静态验证和未验证项。 |
| `Docs/AI/RECORD_INDEX.md` | 由现有脚本刷新本次新记录的索引范围和源摘要。 |

本文件为新增的完整修改记录。资源回收接入 YooAsset 的引用计数流程，Unity 增量 GC 已启用。

## 代码复查

初版在 `MNavigationMgr.OnSceneLoaded` 前执行回收。此时导航管理器仍持有旧场景的 `MNavigation` 查询器，导致这次回收无法处理旧导航对象。调整后，导航先解除旧引用，随后回收零引用资源与托管垃圾。回收仍在完成场景切换的同一帧，早于业务根节点启用和完成回调。按开发者指示，代码复查与双语记录同步由 Codex 完成。

## 验证与未解决项

- 使用当前 Unity `2023.2.20f1c1` 的 Bee 编译参数与 Roslyn 分别编译 `LYF_Game` 和 `Assembly-CSharp`，均通过且为零错误。产物与日志位于 `_temp/20261119-ro-scene-gc/`。`LYF_Game` 报告 50 条警告，包含既有 `Singleton<T>` 重名。`Assembly-CSharp` 无警告。
- `static-check.txt` 记录启动回收和低内存生命周期检查。复查后的 `review-LYF_Game.compile.txt` 和 `review-static-check.txt` 分别记录 `LYF_Game` 复编通过，以及导航替换、YooAsset 清理与托管 GC 的顺序和加载失败返回验证通过。
- Unity MCP 反射确认了 `Application.lowMemory` 与 `ResourcePackage.UnloadUnusedAssets()` 的签名。`unity-lowmemory.json` 记录运行中的 Global 已订阅事件，隔离调用首次回收、间隔内抑制及模拟到期后的回收均通过。首次检查因严格比较浮点秒数误判，按 0.01 秒容差复测通过。
- 待验证真实设备的低内存通知，以及进出场景时的内存变化和 GC 帧耗时。

## Git

- 业务提交 `0059634c72fd4d8419d60f2af0ad638ddb422fc1` 包含上表三份 C# 文件。已推送到 `origin/codex/feature-development`，并通过 `git ls-remote` 核对。
- 复查提交 `168ae28068db15173202459894de29c22a5a8e86` 仅调整 `SceneMgr.cs` 的回收时机。已推送到同一分支，并通过 `git ls-remote` 核对。
