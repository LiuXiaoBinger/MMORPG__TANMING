# 底栏网关 TCP 延迟

任务编号 `RO-GATE-RTT-20261119`。分支 `codex/feature-development`，起始基线 `0c673ac`。

## 文件清单

- 修改 `LYF_MMO/Assets/LYF_Game/Scripts/Global.cs`。将非缩放时间交给网关探测。
- 修改 `LYF_MMO/Assets/LYF_Game/Scripts/Manger/MNetClient.cs`。在游戏会话通过现有 TCP 连接发送探测，在 TCP 回调取回包时间，沿已有连接代次和发送标记确认结果，超时与断线时清空读数。
- 修改 `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/Window/Widget/RoleStatusFooterWidget.cs`。沿用原延迟文案、图标和本机时钟控件，按 RTT 显示数字与颜色。
- 修改 `LYF_MMO/Assets/LYF_Game/Plugins/Net/Network.dll`。以更新后的共享 Network Debug 产物同步共享协议号。
- 修改 `LYF_Server/Network/Socket/com/NetDefine.cs`。新增 `RpcC2T_SyncTime = 11191`。
- 修改 `LYF_Server/GateServer/GateServerApp.cs`。在已有 TCP 监听注册流程中登记探测协议。
- 修改 `LYF_Server/GateServer/GateServer.csproj`，纳入新处理器源码。
- 新增 `LYF_Server/GateServer/Ctrl/GateSyncTimeCtrl.cs`。现有 `RegistCommand` 要求实现 `IContainer`，处理器使用接入会话校验并回显 8 字节标记。
- 追加 `项目分析/项目难点.md` 的网关 RTT 跨模块条目。四份中英文协作记录分别增补本任务开发或验证事实，并生成 `Docs/AI/RECORD_INDEX.md` 的定位。

## 设计与协作

- RO 的 `MServerTimeMgr.Update` 调用 `RpcC2T_SyncTime`，`OnSyncTime` 用发包到回复的时间更新延迟。网关的 `rpcc2t_synctime.cpp` 负责回复，`SystemInfoCtrl.UpdateNetState` 把档位映射到 HUD。默认探测 5 秒一次、超时 2 秒，档位边界为 100、250、500 ms。
- LYF 沿用定期网关回包测 RTT，采用同进程的 `Stopwatch`。游戏会话发起已有实时绑定后启用经 TCP 的探测。主界面底栏用灰色 `-- ms` 表示无结果，真实不足 1 ms 显示 `<1 ms`，其余向上取整并按原始 RTT 分绿、黄、橙、红四档。
- Codex 原生子 Agent `/root/gate_rtt_server` 执行 `RO-GATE-RTT-20261119-SERVER-1`，`/root/gate_rtt_client` 执行 `RO-GATE-RTT-20261119-CLIENT-1`，各一轮。客户端子 Agent 在同一会话完成一轮 `RO-GATE-RTT-20261119-DOCS-1` 同步并核对新增中英文段落。本地两个子会话的模型均为 `gpt-6.2-sol`。主 Agent 完成共享插件同步、隔离 TCP 联调、Unity 底栏查证和 Git 集成。

## 验证

- GateServer Release 和 Network Debug/Release 构建通过。Unity 2023.2.20f1c1 Roslyn 编译 `LYF_Game` 成功，0 个错误、50 条警告。Unity 插件 SHA-256 为 `2AB5422E7D987D55A45EE1713963540243B8D3BB57C39D7F871148B7F41712D1`。
- 两个隔离 TCP 客户端分别验证了实际网关处理器原样回显 8 字节标记，并验证 0、7、9 字节请求没有回包。使用编译后的 Unity `MNetClient` 和网关处理器进行独立 .NET TCP 联调，在服务端加入 120 ms 测试等待的回合观测到 321.09 ms，验证测量排除了另外加入的主线程等待。错标记、5 秒发送间隔、2 秒超时及旧代次回包检查通过。
- Unity 查到正式底栏上的 `LatencyText` 和 `LatencyIcon` 引用，最长测试文案 `2000 ms` 的首选宽度为 72.67，文本可用宽度为 125。当前主城 Play 仍加载原程序集，真实主城新版显示与发布包待验证。原始证据在 `_temp/ro-gate-rtt-20261119-01a1f424/`。

## 用量记录

本轮主会话用量来自本地 Codex 原始 JSONL。按北京时间 2026-11-19 23:33:49 至 2026-11-20 00:00:07 的暂存前采样，主会话输入 13,167,724 token，其中缓存 13,060,864，输出 51,875。子会话、最终窗口和压缩计数见 `_temp/ro-gate-rtt-20261119-01a1f424/current-task-usage.json` 和同目录 `context-compactions.json`。本地记录覆盖模型输入输出，账户剩余额度不可测。

## Git

- 实现提交为 `d123137fb19e1a274a15ef240b6838e2f518c3bf`，包含 8 个业务文件和本次获准写入的记录。两个原有 UTF-8 BOM 改动保留在工作区。
- `git push origin HEAD:refs/heads/codex/feature-development` 成功，推送范围为 `0c673ac..d123137`。`git ls-remote` 核对了远端分支上的实现提交。命令回执保存在 `_temp/ro-gate-rtt-20261119-01a1f424/git-feature-push.txt`。
