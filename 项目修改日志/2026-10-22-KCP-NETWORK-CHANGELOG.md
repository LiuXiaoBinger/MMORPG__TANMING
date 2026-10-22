# 2026-10-22 KCP网络改造

## 用户范围与设计

用户批准采用KCP，并要求创建专门Git分支进行网络改造。基于项目分析/RO_NETWORK_TCP_KCP_DESIGN.md第一版，Unity至Gate的移动与技能统一使用可靠有序KCP over UDP，其余业务以及Gate至Game保持TCP。沿现有业务处理器和本地表现接入，不扩服务器权威移动及不可靠位置通道。

## 分支与执行记录

- 分支codex/network-kcp-development，基线756ca35；已创建并推送同名远端、设置上游。用户本次授权覆盖历史固定分支要求，不操作main。
- 开发任务NETWORK-KCP-IMPLEMENT-20261022，WorkBuddy官方桌面Hub任务hub-7e630e54-2ccc-469c-b3d4-e972acfc0d82；首轮attempt run-3a581c50-63f9-4e7c-9710-9c4b4b3bb5cb，会话5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0，项目目录D:/unitypro/LYFMMORGP。派发不等于完成，后续沿原任务核验和返工。
- 任务单、请求、基线原用户差分与测试产物统一保存在_temp/network-kcp-20261022，不纳入正式提交。

## 文件清单（持续追加，以最终实际差异为准）

1. 修改 Docs/AI/PROJECT_CONTEXT.md：记录用户批准KCP和专用开发分支。
2. 修改 Docs/AI/CURRENT_TASK.md：记录实施范围、桌面开发任务及交接进度。
3. 修改 Docs/AI/DECISIONS.md：记录双通道第一版及专用分支决策。
4. 修改 Docs/AI/TEST_REPORT.md：记录实际分支验证与尚未验收边界。
5. 新增 项目修改日志/2026-10-22-KCP-NETWORK-CHANGELOG.md：记录设计、逐项文件、开发轮次、验证、未完成和Git结果。

业务文件由WorkBuddy交付后逐项补充，不能把任务授权名单当作已修改清单。当前无移动或删除文件。

## 验证与未完成

目前仅分支创建/推送及桌面任务派发通过；业务实现、编译、认证绑定、路由、丢包/乱序、重连与真实Unity双号测试均尚未验收。保留已有无关用户改动，尤其GM_Game.dll.bytes、字体和布局产物。最终结果按实际证据更新。

## Git

专用远端分支已创建并指向756ca35。本轮代码尚未交付或提交，不报告网络改造完成；最终检查、精确暂存、中文提交与推送回执待追加。

## 业务实现交付（WorkBuddy 首轮，2026-10-22）

分支 codex/network-kcp-development，基线 756ca35。未提交、未推送、未改 main；正式 Network.dll 与场景、预制体、服务进程均未替换或启停。

### 新增（12）

1. LYF_Server/Network/ThirdParty/Kcp/KCP.cs：上游 limpo1989/kcp-csharp 固定提交 6ed5b68645c8afe97dbcbd71fb6f469a98f49420（MIT）逐字拷贝的 KCP 可靠有序算法，保留 namespace KcpProject 作为第三方边界。
2. LYF_Server/Network/ThirdParty/Kcp/ByteBuffer.cs：同上游字节缓冲，逐字拷贝。
3. LYF_Server/Network/ThirdParty/Kcp/LICENSE-KCP.txt：上游 MIT 许可证原文。
4. LYF_Server/Network/ThirdParty/Kcp/README-UPSTREAM.md：来源、提交、收录范围与未收录 UDPSession 的原因。
5. LYF_Server/Network/Socket/com/KcpDefine.cs：UDP 端口 10141、1200 字节数据报预算、MTU 1172、四种数据报类型、令牌与认证码长度、实时协议集合判定。
6. LYF_Server/Network/Socket/com/KcpAuth.cs：HMAC-SHA256 截断 16 字节认证码、固定时间比较、密码学随机令牌与 conv。
7. LYF_Server/Network/Socket/com/KcpDatagram.cs：外层数据报编解码（28 字节头）。
8. LYF_Server/Network/Socket/com/KcpBindPayload.cs：TCP 绑定响应强类型载荷。
9. LYF_Server/Network/Socket/KcpPeer.cs：网关侧单会话 KCP 输入、发送、更新与按序接收。
10. LYF_Server/Network/Socket/UdpNetwork.cs：UDP 监听、conv 路由、待绑定表、Peer 生命周期与更新清理线程。
11. LYF_Server/Network/Socket/KcpClient.cs：客户端 KCP 绑定握手、收发与更新线程、就绪/回退。
12. LYF_Server/GateServer/Ctrl/GateKcpCtrl.cs：绑定请求处理与参数下发。

### 修改（9）

1. LYF_Server/Network/Socket/com/ServerBase.cs：SendData 抽出可重写钩子 TrySendRealtime，未接管时保持原 CRC16+长度+GZip 帧。
2. LYF_Server/Network/Socket/com/Session.cs：新增 AttachRealtimePeer/DetachRealtimePeer/TrySendRealtime/HandleRealtimePackage；断开释放 Peer 并清空客户端自填 GateSessionId。
3. LYF_Server/Network/Socket/NetClient.cs：新增 RealtimeClient，仅 Unity 类型分流；断开关闭通道。
4. LYF_Server/Network/Socket/com/NetDefine.cs：新增 CMD_KcpBindCode=11190。
5. LYF_Server/Network/Network.csproj：加入 9 个新增编译项。
6. LYF_Server/GateServer/GateServerApp.cs：启动 UdpNetwork(10141)，注册 GateKcpCtrl。
7. LYF_Server/GateServer/GateServer.csproj：加入 GateKcpCtrl.cs。
8. LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs：绑定请求与响应处理、KCP 下行按代次投递、断开释放。
9. LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs：本地主角进入世界请求后发起一次绑定。

无移动或删除文件。

## 验证（本轮实测）

- Network 编译 0 错误（14 警告全部来自上游 KCP 与既有 MMoRpgCommon）。
- GateServer 编译 0 错误。
- Unity 客户端脚本 289 源编译 0 错误（引用本轮新 Network.dll）。
- 真实 Network.dll 回环 19/19 断言通过：绑定、双会话路由、上下行、移动先于技能、未知 conv 拒绝、错误认证码拒绝、错误令牌拒绝、断开释放、旧通道不投递、实时集合与 TCP 集合划分。
- 报告：_temp/network-kcp-20261022/developer-report.md；日志 build/*/build.log、loopback-result.log。

## 未完成

未做真实 Unity 双客户端联机、弱网与性能对比；正式 Network.dll 未替换；Unity 当前 TCP 对端是否为启用 UDP 的 Gate 未运行期确认（非 Gate 时按设计保持全 TCP）；内网 Gate→Game 仍 TCP；大包仍走 TCP 无分片。

## Codex首轮核验与第2轮返工（当前结果）

- 首轮桌面状态completed、runtimeSource=desktop_queue、holdsCapacity=false已核实。上方19项及编译是开发自检，尚非Codex最终验收。审查发现实际阻塞，不能报告改造完成。
- KcpClient与KcpPeer的Send/Input/Update/PeekSize/Recv跨线程无同步，UdpNetwork接收和更新还并发交付；空闲30秒释放后客户端仍Ready；UDP绑定确认丢失导致单边启用；绑定UDP载荷明文发送HMAC密钥；旧通道回调读取当前_client/代次可能落入后续连接。要求原开发者最小修正，并补并发、真实丢包/乱序/重复、初次阻断、空闲/运行期失败、断开重绑和旧回调隔离。
- 既有顺序测试先等移动收到才发技能，没有覆盖连续发送；错误令牌测试命中已移除的conv，不能证明待绑定密钥校验。上方对应通过表述降为首轮自报，后续以修后真实测试为准。KCP支持消息分段，上方“无分片”不能理解为KCP本身没有分段；当前接收超MTU主动丢弃的限制需开发者修正或说明真实业务范围。
- 已沿原Hub任务返工第2轮（首次+返工1）：attempt run-dbebd2c9-2b6a-4d17-8361-fdc0ba71715f，会话保持5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0，runtimeSource=desktop_queue，状态running。完整要求与请求结果在_temp/network-kcp-20261022/rework-round2.json和rework-round2-result.json；连接与等待不计开发轮次。
- 审查曾误用默认bin/obj构建，只有缓存产物，没有业务源修改；不作为_temp隔离构建证据，不暂存、不擅自清理。

### 本轮另行修改的规则文件

1. 修改AGENTS.md：按用户授权，WorkBuddy连接端口异常由Agent核原状态后正常退出/重启并恢复原任务，不再要求用户托盘退出；保留禁止重复派工、强杀未知工作和覆盖未保存成果的限制。
2. 修改AIconfig/agent/workbuddy_executor.md：同步上述正常重启恢复流程与授权。
3. 修改Docs/AI/PROJECT_CONTEXT.md：补首轮未通过及连接恢复授权。
4. 修改Docs/AI/CURRENT_TASK.md：补第2轮原会话返工、阻塞事实、正式DLL与后续验收交接。
5. 修改Docs/AI/DECISIONS.md：补本轮传输验收标准及用户连接恢复决策。
6. 修改Docs/AI/TEST_REPORT.md：记录首轮真实审查、19项覆盖缺口及默认bin/obj偏差。

当前未产生移动/删除文件；业务待修后复验，尚未提交或推送本轮代码。

## 返工 1（WorkBuddy 第 2 轮，2026-10-22）

分支 codex/network-kcp-development，基线 756ca35。未提交、未推送；正式 Network.dll、场景、预制体、服务进程均未替换或启停；`_temp/network-kcp-20261022/baseline-dirty-paths.txt` 中 9 个用户改动文件未触碰。

### 修改（7 个已存在文件 + 4 个新增系统文件重写 + 1 个新增测试文件）

1. `LYF_Server/Network/Socket/KcpPeer.cs`：新增实例级通道锁，`Input/Send/Update/TryReceive` 全部串行；`_lastActiveTicks` 改 `Interlocked` 读写；接收缓存注释说明「KCP 分片重组后仍超 1172 字节才丢弃」。
2. `LYF_Server/Network/Socket/KcpClient.cs`：新增通道锁与发送锁；绑定请求改空载荷 + MAC；新增 300ms 重发、3 秒绑定期限、5 秒保活、15 秒静默检测；`ConfirmEnabled(conv)` 由 TCP 启用确认驱动（CAS 保证只切一次）；`CloseInternal(runtimeFailed, bindFailed)` 保证每条通道最多一次结果通知。
3. `LYF_Server/Network/Socket/UdpNetwork.cs`：绑定请求校验改为「载荷必须为空 + MAC 正确」，删除明文令牌比较；`CompleteBind` 先经 TCP 下发 `Enabled` 再挂接 Peer，UDP 回执降级为可丢回执；新增重复请求幂等重发；新增 `TYPE=5` 保活处理与回执；新增 Socket 发送锁；待绑定有效期改 5 秒；空闲释放不再补发 TCP 提示（避免静默退回 TCP）。
4. `LYF_Server/Network/Socket/com/KcpDefine.cs`：新增 `DatagramTypeKeepAlive`、`BindRetryIntervalMilliseconds`、`BindTimeoutMilliseconds`、`BindPendingLifetimeMilliseconds`、`KeepAliveIntervalMilliseconds`、`ClientSilenceTimeoutMilliseconds`；令牌注释明确「只在 TCP 下发，不出现在 UDP」。
5. `LYF_Server/Network/Socket/com/KcpBindPayload.cs`：新增 `KcpBindResult.Enabled`；令牌字段注释明确只随 TCP 绑定响应下发。
6. `LYF_Server/Network/Socket/com/ServerBase.cs`：`SendData` 的 TCP 帧写入加发送锁（业务线程/转发线程/UDP 绑定线程并发写会交错截断帧）。
7. `LYF_Server/Network/Socket/com/Session.cs`：`_realtimePeer` 改 `volatile`（UDP 线程写、业务线程读）。
8. `LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs`：`Enabled` 确认驱动 `ConfirmEnabled`；注册回调时固定 `NetClient` 与连接代次并按身份过滤（旧包不解析不排队）；运行期失效触发 `NetClient.Disconnect()` 走既有断线恢复；新增 `OnBindFailed` 分支保持 TCP。
9. `_temp/network-kcp-20261022/compile_kcp.py`：新增 GameServer、CenterServer 编译；`run_loopback.py`：纳入 `KcpFaultTests.cs`，超时放宽到 300 秒；新增 `tests/KcpLoopback/KcpFaultTests.cs`（UDP 故障注入代理 + 7 组场景）。

无移动或删除文件。

## 验证（本轮实测）

- Network / GateServer / GameServer / CenterServer 四工程 `dotnet msbuild /t:Rebuild` 全部 0 错误（14 警告仍全部来自上游 KCP 与既有 MMoRpgCommon）。
- Unity 客户端脚本 289 源编译 0 错误（引用本轮新 Network.dll）。
- 真实 Network.dll 回环 + 真实 UDP 代理注入 **54/54 断言通过**，其中新增 35 条：并发 4×40 上行与 3×20 下行不重不漏且按线程保序；丢包 1/7 + 复制 1/5 + 相邻交换乱序下双向 50 条只交付一次且严格按序；初次上行阻断 3 秒放弃且网关无 Peer、不误触发断线恢复；绑定回执丢失仍靠 TCP 确认启用、重复请求幂等不建第二条通道；下行整向阻断 15 秒静默检测只通知一次、已发 5 条不重投、失效后写入返回失败；静止站立 6.5 秒保活刷新活动时间不误判断线；同会话重绑旧 conv 数据不投递、新 conv 正常。
- 证据：`_temp/network-kcp-20261022/build/*/build.log`、`loopback-result.log`、`tests/KcpLoopback/build.log`、`build/compile-result.json`；报告 `_temp/network-kcp-20261022/developer-report.md`。

## 未完成

未做真实 Unity 双客户端联机与弱网端到端性能测量；跨通道实体创建与实时消息早到只有代码级核对（现状为丢弃不缓存），未在 Unity 内构造时序实测；技能一次 MP 扣除未在 GameServer 处理器夹具验证；正式 Network.dll 未替换；内网 Gate→Game 仍 TCP。

## 返工 2（WorkBuddy 第 3 轮，2026-10-22）

分支 codex/network-kcp-development，基线 756ca35。未提交、未推送、未改 main；正式 Network.dll、场景、预制体、服务进程均未替换或启停；`_temp/network-kcp-20261022/baseline-dirty-paths.txt` 中用户改动未触碰。

### 新增（2）

1. `LYF_Server/Network/Socket/com/KcpBindResult.cs`：从 `KcpBindPayload.cs` 拆出的绑定结果码，含新增 `Abandoned`（客户端放弃绑定，经可靠 TCP 回传），中文注释。
2. `LYF_Server/Network/Socket/com/RealtimeSendResult.cs`：传输钩子三态结果 `KeepTcp/Sent/Dropped`，用于区分“从未启用”与“启用后失效”。

### 修改（8）

1. `LYF_Server/Network/Socket/KcpPeer.cs`：新增 `_delivering` 投递独占与 `TryBeginDelivery/EndDelivery`；注释说明“到达有序不等于交付有序”。
2. `LYF_Server/Network/Socket/UdpNetwork.cs`：`SnapshotDeliver` 必须先取得投递权才能取包回调，未取得者立即返回（`finally` 释放投递权）；`CleanupIdlePeers` 改用 `InvalidateRealtimePeer`（失效后禁止回退 TCP）；`TryAllocateBinding` 重绑时立即失效旧引用。
3. `LYF_Server/Network/Socket/KcpClient.cs`：新增 `_everEnabled`/`HasBeenEnabled`、`OwnerGeneration`、`ShouldReleaseOnFailure`；`ConfirmEnabled` 置位“曾启用”。
4. `LYF_Server/Network/Socket/com/ServerBase.cs`：`TrySendRealtime` 改为返回 `RealtimeSendResult`，`SendData` 对 `Dropped` 明确丢弃并告警；新增 `DescribeSend`，`CMD_KcpBindCode` 日志只留 `result` 与 `conv` 并标注“令牌已省略”。
5. `LYF_Server/Network/Socket/com/Session.cs`：新增 `_realtimeEverEnabled`；`DetachRealtimePeer`（回到 TCP）与 `InvalidateRealtimePeer`（失效后禁止回退）语义拆分；`TrySendRealtime` 改三态。
6. `LYF_Server/Network/Socket/NetClient.cs`：新增 `_realtimeEverEnabled`，一旦观察到通道启用即锁定，通道引用释放后实时协议仍返回 `Dropped`；新连接建立时重置；`TrySendRealtime` 改三态。
7. `LYF_Server/Network/Socket/com/KcpDefine.cs`：`BindPendingLifetimeMilliseconds` 与 `BindTimeoutMilliseconds` 统一为 3 秒。
8. `LYF_Server/Network/Network.csproj`、`LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs`：登记两个新源文件；`NetSocketMgr` 记录绑定 conv 与通道归属代次（`OwnerGeneration`），放弃绑定时经 TCP 回传 `Abandoned`，`OnConnectionFailed` 改为按代次释放（新增 `ReleaseRealtimeClientOfGeneration`）。

无移动或删除文件。

### 测试与脚本（仅 `_temp`）

- 新增 `tests/KcpLoopback/DeliveryOrderTests.cs`（慢回调放大投递窗口，断言无并发回调、顺序一致、回调不持通道锁）；`tests/KcpLoopback/KcpFaultTests.cs` 新增绑定超时清理、日志脱敏、代次归属三组用例，并把运行期失效用例改为经真实 `NetClient` 发送；`tests/DeliveryProbe/ProbeMain.cs` + `probe_delivery_window.py`（修复前对照探针，跑完自动还原源码并重建正式 DLL）；`run_loopback.py` 纳入新源文件并链接真实 `GateKcpCtrl.cs`。

## 验证（本轮实测）

- Network / GateServer / GameServer / CenterServer 四工程 `dotnet msbuild /t:Rebuild` 全部 0 错误（警告仍全部来自上游 KCP 与既有 MMoRpgCommon）。
- Unity 客户端脚本 289 源编译 0 错误（引用本轮新 Network.dll）。
- 真实 Network.dll 回环 + 真实 UDP 代理注入 + 投递串行化：**PASSED=78 FAILED=0**（日志 82 个 PASS）。
- 前后对照：关闭投递独占的“修复前”DLL 上同一份断言失败（`overlaps=4`），正式实现 `overlaps=0`。
- 证据：`_temp/network-kcp-20261022/build/*/build.log`、`build/compile-result.json`、`loopback-result.log`、`build/delivery-window-before.log`、`tests/KcpLoopback/build.log`；报告 `_temp/network-kcp-20261022/developer-report.md`。

## 未完成

未做真实 Unity 双客户端联机与弱网端到端测量；`MonoBehaviour` 侧自动重连代次时序未在 Unity 内运行（代次判定只在函数级验证）；跨通道实体早到仍为“丢弃不缓存”，未构造 Unity 时序实测；正式 Network.dll 未替换；内网 Gate→Game 仍 TCP。

## Codex第三轮复验与豆包第1轮派发

- WorkBuddy最新第三轮completed，但两处独立真实Socket故障复现，原Hub未accept。各3轮约束下转豆包第1轮，conversation38448538786202114/run59342789144677378，本项目local，当前running；任务与预期报告位于_temp/network-kcp-20261022/doubao-round1-*.md。
- 本轮Codex四工程0错误（警告计数14/14/28/14，含重复输出）、客户端289源0错误，旧主套78项通过。独立审查复现零发送释放后技能误走TCP14字节，以及Abandoned清理后绑定线程重挂死Peer；探针退出0表示故障复现而非验收通过。证据_temp/network-kcp-20261022/review/ROUND3_REVIEW.md、repro-result.log。DLL输入SHA不变。
- 本次协作记录修改：Docs/AI/PROJECT_CONTEXT.md记录真实轮数与当前执行者；CURRENT_TASK.md记录两缺口、原会话和下一步；DECISIONS.md记录最小修复与恢复授权；TEST_REPORT.md记录真实命令、结果及编码失败边界；本日志追加精确进度。没有本轮Codex业务修改、移动或删除；开发者交付后统一核对实际完整文件清单。
- 正式Network.dll仍未替换、用户GM_Game.dll.bytes等保持，业务未提交推送。真实Unity双客户端与MP扣除等未验收。分析条目见项目分析/项目难点.md的KCP专题与项目分析/KCP_NETWORK_IMPLEMENTATION.md。

## 豆包第1轮修复收尾（2026-10-22，恢复会话）

### 修改（生产源码，仅最小修复+注释）

1. `LYF_Server/Network/Socket/NetClient.cs`：新增 `MarkRealtimeEverEnabled()`，TCP启用确认到达即锁定“禁止回退”；保留发送时观察 `HasBeenEnabled` 的兜底补记。
2. `LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs`：创建通道时接线 `realtimeClient.OnBound = () => bindClient.MarkRealtimeEverEnabled()`。
3. `LYF_Server/Network/Socket/UdpNetwork.cs`：`CompleteBind` 在既有 `_sync` 表锁内重核 `_pending[conv]` 归属，失败直接返回；复核通过后同锁内入表并 `AttachRealtimePeer`，TCP确认/UDP回执移到锁外；补全 `session/current/stillPending/peer/previous` 中文注释。
4. `LYF_Server/Network/Socket/com/Session.cs`：`Disconnect` 改为先 `ReleaseSession(SessionID)` 再 `DetachRealtimePeer()`，与Abandoned同序，消除断开等待表锁期间被反向Attach的悬挂引用。
5. `LYF_Server/GateServer/Ctrl/GateKcpCtrl.cs`：新增三目运算符改if分支，补局部与参数中文注释。

### 测试与脚本（仅 _temp）

- `tests/KcpLoopback/KcpFaultTests.cs`：新增零发送释放用例（真实NetClient+Socket，启用确认后零实时发送即清引用，实时包不写TCP、普通业务仍TCP、从未启用新连接保持TCP）；新增取消/绑定并发屏障30轮（真实TCP Socket+真实GateKcpCtrl）；新增先绑定后放弃清理用例；新增断开清理用例。
- `run_loopback.py`、`probe_delivery_window.py`：子进程解码 gbk→utf-8，修复中文日志乱码。
- 过程记录：曾用随机观察统计“先绑定竞态”导致一次85/1误判，改为确定性用例后稳定通过，不掩盖该失败。

### 验证（本轮实测）

- `compile_kcp.py`：Network/GateServer/GameServer/CenterServer 四工程 `dotnet msbuild /t:Rebuild` 0 错误。
- `compile_client.py`：Unity脚本289源0错误（引用本轮build输出Network.dll）。
- `run_loopback.py`：真实Network.dll回环 **PASSED=90 FAILED=0**（原78 + 新增12），日志 `loopback-result.log`。
- Codex独立最终探针三项 `FIXED_ONBOUND_NO_TCP_FALLBACK=True`、`FIXED_DISCONNECT_NO_STALE_PEER=True`、`FIXED_ABANDONED_NO_SPLIT=True`，证据 `review/doubao-round1-final-result.log`。

### 插件配套

- 替换前正式插件 `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` SHA256=`4429175E2D87C4F8D00CC6E78D6B178955AA7BAE6EAEEAD7428EDED718E6DBBF`（与授权基线一致）；旧字节备份至 `_temp/network-kcp-20261022/plugin-backup/Network.dll.20261022-orig.bak`。
- 替换后插件SHA256=`4EF06D76CB632C4E83359D6EB872B2E0AE9BEB11215DAEF64874D6E04EF5BFE0`，与build/network/bin/Network.dll一致；未改.meta、未替GM_Game.dll.bytes或其他依赖。
- 另以正式插件路径独立编译客户端289源0错误（`build/client-plugin-path/`，不改compile_client.py默认验证）。

### 未完成边界

未做真实Unity双客户端联机、弱网端到端与性能测量；断开固定交错由Codex反射探针覆盖，公开回环未加生产测试钩子；打包侧HybridCLR/IL2CPP主包配套未验证；业务代码未提交推送，不操作main。

## 豆包第2轮：仅中文注释补齐（2026-10-22，功能不变）

### 范围

- 仅在 `before-comment-inputs.json` 允许的 17 个源文件内补具体中文注释（用途/数据语义/单位）与空白，未改任何标识符、类型、条件、接口或调用；上游 ThirdParty/Kcp 未改，namespace 旧有未动。
- 协议层：`KcpAuth.cs`（signedBytes 待认证拼接、fullMac 完整摘要、truncated 截断码、buffer/random/value 局部与 HMACSHA256/RNGCryptoServiceProvider using）、`KcpDatagram.cs`（totalLength、mac、magic、expected）、`KcpBindPayload.cs`（buffer、copyLength、payload）。
- 通道与网络：`KcpPeer.cs`（result、peekSize、discard、receivedLength、received）、`KcpClient.cs`（socket、nowTicks、result、bound、previous/socket、failed/bindFailure、socket/token/written、length、nowTicks/state、lastRequest/lastInbound/lastKeepAlive、message/peekSize/discard/receivedLength/handler、type/conv/payloadOffset/payloadLength 及各 catch）、`UdpNetwork.cs`（TryAllocateBinding、ReleaseSessionLocked、GetPeerOfSession、SendDatagram、ReceiveLoop、UpdateLoop、CleanupIdlePeers、SnapshotDeliver、DeliverPackage、HandleDatagram、HandleKeepAlive、HandleBindRequest、CompleteBind、SendEnabledConfirmation、HandleKcpData 的候选局部与 catch 注释）。
- 其余：`GateKcpCtrl.cs`（session 转型局部）、`ServerBase.cs`（payload、frame、Decode payload、catch 的 se）、`Session.cs`（peer、udpNetwork）、`NetClient.cs`（两处 realtimeClient）、`GateServerApp.cs`（udpNetwork、gateKcpCtrl）、`NetSocketMgr.cs`（client、payload、bindGeneration、realtimeClient、abandonedConv、basePackage、protoCode、handler 相关局部与 catch）。
- 本就注释完备或本轮无需改动：`KcpDefine.cs`、`KcpBindResult.cs`、`RealtimeSendResult.cs`、`NetDefine.cs`、`MainCityMgr.cs`，文件 SHA 与基线一致。

### 未变逻辑证据

- 用 `_temp/network-kcp-20261022/verify_code_unchanged.py` 去注释后与冻结快照 `before-comment-completion/` 逐文件比对，除 `ServerBase.cs` catch 由单行展开为多行（仅括号排版，语句等价）外，全部代码一致；未改条件、接口、调用与标识符。
- 本轮按任务要求不重复 90 项回环；功能代码沿用上一轮已核验状态。

### 编译与插件结果

- `compile_kcp.py`：Network/GateServer/GameServer/CenterServer 四工程 `dotnet msbuild /t:Rebuild` 0 错误。
- `compile_client.py`：Unity 脚本 289 源 0 错误（引用本轮 build 输出 Network.dll）。
- `compile_client_plugin_path.py`：以正式插件路径独立编译 289 源 0 错误，源码/正式依赖一致。
- 插件配套：替换前插件为上轮配套 `4EF06D76…BFE0`（已备份 `plugin-backup/Network.dll.20261022-pre-comment.bak`）；替换后正式插件 SHA256=`21BDB175F813BAF946366BD3DBA7BEE22E0C2C19628DB74BF540910BA7CDDA91`，与 `build/network/bin/Network.dll` 一致。未替 GM_Game.dll.bytes、未改 .meta、未清 _temp、未提交推送。

## 豆包第3轮：仅修测试夹具异步连接时序（2026-10-22，不改业务逻辑）

### 真实失败（主Agent独立复跑）

- 第1轮新增90项独立复跑得 **PASSED=89 FAILED=1**，证据 `_temp/network-kcp-20261022/codex-final-loopback-console.log:367-371`：`RunZeroSendRelease` 的对照新连接 `fresh.StartConnect()` → 服务端 `AcceptSocket` 返回 → 立即 `SendData(11180)`，随后才打印 `OnConnectCB 连接服务端成功`，紧接着 `SendData failed -2146233079`，`从未启用的新连接实时协议保持 TCP bytes=0` 失败。
- 根因（夹具调度缺口，非生产Bug）：服务端 `AcceptSocket` 返回只代表内核已接受，不代表客户端 `BeginConnect` 的异步 `EndConnect/OnConnSucceed` 完成；测试在连接回调（内部重置 `_realtimeEverEnabled=false` 后才 `Invoke OnConnSucceed`）到达前就发包/确认。未加任何生产重试或门禁迎合测试。

### 修改（仅 _temp 测试夹具 + 一处注释收尾）

- `tests/KcpLoopback/KcpFaultTests.cs`：三处真实 `NetClient` 夹具（`RunRuntimeFailure` 的 uplink、`RunZeroSendRelease` 的 uplink 与 fresh）均在 `StartConnect()` 前挂接 `OnConnSucceed`，用 `ManualResetEventSlim` 有界等待（3000ms）连接回调完成后再 `ConfirmEnabled` 或发送，结束时 `Dispose()` 释放；未新增/减少断言（fresh 仅等待不新增 Check，断言总数保持90），未用 sleep 掩盖窗口。
- `run_loopback.py` 退出码核查：`Main` 在 `Failed>0` 时返回1，`sys.exit(run_tests())` 透传子进程 returncode，无需改动；89/1 时外层退出码为1，不得用末尾 Get-Content 的管道退出码冒充通过。
- 注释收尾（生产 `UdpNetwork.cs` `ReleaseSessionLocked`，只改注释不改token）：`pendingConvs` 更正为“待删除的本会话conv列表”，`convKeys` 更正为“当前待绑定表键快照”，两处 `for` 的 `index` 补中文索引用途说明。

### 验证（本轮实测）

- `run_loopback.py` 连续两次全量90项回环均 **PASSED=90 FAILED=0**、退出码0，独立日志 `loopback-run1.log`、`loopback-run2.log`（当次 `loopback-result.log`）；测试工程构建0错误0警告。
- 去注释比对 `verify_code_unchanged.py`：本轮 `UdpNetwork.cs` 注释改动后token与快照一致；`ServerBase.cs` 仍为第2轮已记录的 catch 花括号排版差异，本轮未触碰。
- 三项独立探针证据 `review/` 原样保留；本轮未重编、未替换正式 Network.dll（注释不进IL，生产DLL保持上轮配套 `21BDB175…`），未替 GM_Game.dll.bytes、未改 .meta、未清 _temp、未提交推送。



## 最终集成文件清单（待最终检查后提交）

本任务逐项范围共34个路径，以下以最终Git暂存清单为准；没有移动或删除，Unity未新增脚本/目录元文件，既有Network.dll.meta保持。用户既有GM_Game.dll.bytes、字体、布局等不纳入。

| 操作 | 项目根相对路径 | 用途与具体变化 |
| --- | --- | --- |
| 新增 | `LYF_Server/Network/ThirdParty/Kcp/KCP.cs` | 固定上游提交的KCP可靠有序协议算法，保持原文 |
| 新增 | `LYF_Server/Network/ThirdParty/Kcp/ByteBuffer.cs` | 上游协议算法使用的字节缓冲，保持原文 |
| 新增 | `LYF_Server/Network/ThirdParty/Kcp/LICENSE-KCP.txt` | 保留MIT许可证 |
| 新增 | `LYF_Server/Network/ThirdParty/Kcp/README-UPSTREAM.md` | 记录库来源、固定提交和第三方边界 |
| 新增 | `LYF_Server/Network/Socket/com/KcpDefine.cs` | 实时协议集合、UDP端口、MTU、绑定与保活时间常量 |
| 新增 | `LYF_Server/Network/Socket/com/KcpAuth.cs` | 会话令牌生成及HMAC认证 |
| 新增 | `LYF_Server/Network/Socket/com/KcpDatagram.cs` | UDP外层头编解码和认证码校验 |
| 新增 | `LYF_Server/Network/Socket/com/KcpBindPayload.cs` | TCP绑定响应载荷编解码 |
| 新增 | `LYF_Server/Network/Socket/com/KcpBindResult.cs` | 绑定申请、启用、放弃等结果码 |
| 新增 | `LYF_Server/Network/Socket/com/RealtimeSendResult.cs` | 区分保留TCP、KCP已发送、失效丢弃 |
| 新增 | `LYF_Server/Network/Socket/KcpPeer.cs` | 网关单会话KCP、通道同步与唯一消费者投递 |
| 新增 | `LYF_Server/Network/Socket/UdpNetwork.cs` | UDP监听、绑定表与Peer路由、认证、更新保活及取消串行 |
| 新增 | `LYF_Server/Network/Socket/KcpClient.cs` | 客户端UDP/KCP、有限绑定、可靠确认、保活与失效通知 |
| 新增 | `LYF_Server/GateServer/Ctrl/GateKcpCtrl.cs` | 网关处理绑定申请与可靠TCP放弃通知 |
| 修改 | `LYF_Server/Network/Socket/com/ServerBase.cs` | 统一发送三态分流、TCP帧写入同步和绑定日志脱敏 |
| 修改 | `LYF_Server/Network/Socket/com/Session.cs` | 网关实时收发接原业务；断开先删绑定后清引用 |
| 修改 | `LYF_Server/Network/Socket/com/NetDefine.cs` | 新增KCP绑定协议号11190 |
| 修改 | `LYF_Server/Network/Socket/NetClient.cs` | Unity实时发送分流，启用确认即记录历史，失效不回投TCP |
| 修改 | `LYF_Server/Network/Network.csproj` | 登记新增传输及上游库源码 |
| 修改 | `LYF_Server/GateServer/GateServerApp.cs` | 启动UDP监听并注册绑定处理器 |
| 修改 | `LYF_Server/GateServer/GateServer.csproj` | 登记GateKcpCtrl源码 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs` | 绑定/启用/放弃接线、KCP接收沿原派发、失效接既有断线流程 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs` | 进入世界后请求实时通道绑定 |
| 修改 | `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` | 配套更新共享网络程序集，源码与正式依赖一致 |
| 修改 | `AGENTS.md` | 记录用户授权自行正常恢复WorkBuddy端口异常 |
| 修改 | `AIconfig/agent/workbuddy_executor.md` | 同步端口恢复和原任务保留约束 |
| 修改 | `Docs/AI/PROJECT_CONTEXT.md` | 更新网络改造范围、执行轮次和当前事实 |
| 修改 | `Docs/AI/CURRENT_TASK.md` | 记录最终交付、文件范围、待现场联机项与交接 |
| 修改 | `Docs/AI/DECISIONS.md` | 记录双通道、最小竞态修复和恢复授权决定 |
| 修改 | `Docs/AI/TEST_REPORT.md` | 记录主Agent实际编译、回环、独立探针及验证边界 |
| 修改 | `项目分析/项目难点.md` | 记录传输并发、生命周期、故障证据、修复取舍与复盘 |
| 新增 | `项目分析/KCP_NETWORK_IMPLEMENTATION.md` | 实现结构、绑定流程、分流和使用/验收边界 |
| 新增 | `项目修改日志/2026-10-22-KCP-NETWORK-CHANGELOG.md` | 完整逐项清单、设计、真实验证和Git回执 |
| 修改 | `项目分析/README.md` | 登记KCP实现说明入口并区分原方案与实际落地阶段 |

## Codex最终集成检查与Git计划

- 分支codex/network-kcp-development，基线756ca35，不合main。WorkBuddy3轮后豆包会话38448538786202114完成第1轮及阻断恢复、第2轮注释、第3轮夹具返工。第1轮误请求删除scratch-review被官方stop取消，原会话恢复；未批准删除_temp，不计权限恢复为额外开发失败。
- 主Agent最终四工程0错误（警告计数14/14/28/14，含重复）；客户端289源默认/正式插件路径均0错误；真实Socket全套PASSED=90 FAILED=0、退出0，三项独立探针True、退出0。证据_temp/network-kcp-20261022/loopback-result.log、review/codex-final-result.log及各build日志。早前89/1保留并退开发者修三处OnConnSucceed等待，开发两轮90/0后Codex重跑通过。
- 最终17业务源去注释token与冻结完全一致，中文局部候选105→0，新增语法无foreach/三目/namespace违规；第三方两源与固定提交归档逐字一致。正式Network.dll最终由Codex仅集成编译产物，SHA256=950846B969F8FF43B229A3769473FD18CEBFAC2B41650E0D8E043FD9A2F8ACF2。原21BDB175…插件另备份于_temp/plugin-backup；原始4429175E…备份也保留，用户GM_Game.dll.bytes仍057685A6…未动。
- 现场Unity双号、MP扣除链路、跨通道早到、Unity自动重连、弱网性能、HybridCLR/IL2CPP打包未验收，不宣称正式联机或上线完成。所有测试/临时输出留_temp，不提交临时工程；没有移动/删除或新Unity元文件。
- 四入口已按最终事实更新；项目分析/README.md登记说明入口，项目分析/项目难点.md补根因与测试时序复盘。精确暂存上表34路径，中文提交引用本日志并推送当前功能分支；Git实际回执在下方续记。

### 暂存前最终事实补记

- 原用户9个路径的git二进制补丁与开工baseline-user-changes.patch归一换行后逐字一致（final-user-change-verification.json），所有用户基线文件均排除暂存；实际暂存恰为上表34路径。
- 全暂存git diff --cached --check发现上游原文KCP.cs:603/994两处行尾空格。两行已与固定提交原文逐字核对，作为第三方原文例外保留，未为格式修改上游；排除该上游单文件后暂存差异检查通过。此前工作区diff --check不检查未跟踪新文件，因此不能当全暂存检查已通过。
- Codex最终独立复跑完整90项及三探针退出0，17源token一致，正式/新build库SHA一致，插件仅集成产物。没有真实Unity双号和打包运行验收。

## Git实际回执（2026-10-22）

- 业务提交`21c95f06cbd09ad382ae32acf4f00fbb238d8159`：feat: 接入移动技能KCP双通道并修复绑定生命周期；范围上表34路径，基线756ca35。提交完成，中文正文含修改/验证/本日志路径。
- `git push origin codex/network-kcp-development`成功，远端由756ca35前进至21c95f0；`git ls-remote`核远端SHA与该业务提交完全一致。未合main、未强推，9个用户原改动补丁保持。
- 本次仅补录Git回执：修改本日志、Docs/AI/CURRENT_TASK.md及TEST_REPORT.md，不改业务或产物；单独docs提交后推送同一分支，最终回执另保留_temp/network-kcp-20261022/final-git-receipt.json并在交付回复报告。
