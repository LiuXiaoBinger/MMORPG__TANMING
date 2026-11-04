# MasterServer 组队迁移

- **任务**　`MASTER-TEAM-20261104`。按开发者选定的首期范围迁移现有组队，Codex 直接实现、验收并维护双语记录。未派给其他模型。
- **运行方式**　在 `LYF_Server/LYF_Server.sln` 构建解决方案，启动 `MasterServer.exe`，再启动新版 `GameServer.exe`。MS 默认监听 `127.0.0.1:10150`，GS 复用内网 TCP 主动连接。协议保持原客户端兼容，Gate 保留原路由。
- **职责**　MS 决定建队、邀请、申请、通过或拒绝、队长转让、解散与进本名单，GS 根据已认证的玩家会话转发请求，处理实际场景、怪物、技能、伤害及回城，并补齐队伍界面实时血量等字段。沿用既有单 GS 分区语义，分区队伍是内存状态，MS 链路断开会解散该分区队伍并由 GS 清理组队副本。

## 逐项文件

| 变化 | 项目根相对路径 | 本次变化 |
| --- | --- | --- |
| 修改 | `LYF_Server/GameServer/GameServer.csproj` | 编入主动连接 MS 的服务适配器。 |
| 修改 | `LYF_Server/GameServer/GameServerApp.cs` | 在正常启动入口注册并连接队伍服。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/ClientMessageManager.cs` | 允许 `MasterServer` 消息来源，并调用组队协议的统一注册。 |
| 修改 | `LYF_Server/GameServer/MessageHandler/Team/TeamMessageHandler.cs` | 集中注册六个网关组队命令，按真实会话确认身份后转交 MS。 |
| 新增 | `LYF_Server/GameServer/Service/MasterTeamLink.cs` | 维护内网 TCP 链路，把 MS 结果切回游戏逻辑线程处理。 |
| 修改 | `LYF_Server/GameServer/manger/Command/GameCommandSource.cs` | 加入独立 MS 来源，和 Gate、Center 的消息分开分派。 |
| 修改 | `LYF_Server/GameServer/manger/TeamManager.cs` | 移出队伍关系与审批，只保留实际入场角色、怪物、场景可见性和回城，补齐队伍服断线清场。 |
| 修改 | `LYF_Server/LYF_Server.sln` | 将 `MasterServer` 加入服务分组及 Debug、Release 构建配置。 |
| 修改 | `LYF_Server/MMoRpgCommon/copy_common_dll.bat` | 公共库的手动分发目标加入 MasterServer。 |
| 新增 | `LYF_Server/MasterServer/MasterServer.csproj` | 新增独立 .NET Framework 4.8 控制台程序，引用共享网络与公共库。 |
| 新增 | `LYF_Server/MasterServer/MasterServerApp.cs` | 注册内网监听与断链清理，默认端口 10150，可用首个启动参数指定监听端口。 |
| 新增 | `LYF_Server/MasterServer/MasterTeamService.cs` | 为接入的 GS 分区分派队伍请求，结束连接时释放对应分区。 |
| 新增 | `LYF_Server/MasterServer/TeamManager.cs` | 唯一维护成员、邀请、申请、满员检查、队长继任、解散及实例进入授权。 |
| 修改 | `LYF_Server/Network/Network.csproj` | 编译新生成的组队服务间 Protobuf 类型。 |
| 新增 | `LYF_Server/Network/Proto/MasterTeam.cs` | 由仓库内的 protoc 生成，与新增协议定义配套。 |
| 修改 | `LYF_Server/Network/Socket/com/NetDefine.cs` | 增加队伍服 TCP 端口和服务间命令号，保留现有客户端命令号。 |
| 修改 | `LYF_Server/Network/copy_dll_to_client.bat` | 共享网络 DLL 的现有分发目标加入 MasterServer。 |
| 新增 | `LYF_Server/ProtoBuf/proto/MasterTeam.proto` | 定义认证角色的定向消息与从 MS 发往 GS 的副本成员清单。 |
| 修改 | `LYF_Server/Stop-Servers.ps1` | 将本项目路径下的 MasterServer 加入已有停止清单。 |
| 新增 | `LYF_Server/Tests/DecodedFrame.cs` | 保存测试解码的完整协议包，以断言名单、目标会话和实际实例号。 |
| 新增 | `LYF_Server/Tests/LoopbackLink.cs` | 将原临时回环测试夹具纳入仓库，复用实际 GS 网络注册表及 CRC、压缩解码。 |
| 新增 | `LYF_Server/Tests/MasterTeamProbe.cs` | 用真实 TCP 向独立 MS 提交请求，从响应与正式 GS 行为观察结果。 |
| 修改 | `LYF_Server/Tests/OrcWave18AiSmoke.cs` | 让现有兽人测试通过独立 MS 建队，再从 GS 按真实实例号检查怪物。 |
| 新增 | `LYF_Server/Tests/Run-TeamAuthoritySmoke.ps1` | 在隔离目录构建，启动临时端口的 MS，运行组队和战斗回归后关闭测试子进程。 |
| 修改 | `LYF_Server/Tests/TeamAuthoritySmoke.cs` | 原回归改由独立 MS 决策，新增双角色网关帧、进退本与 MS 断线全链路检查。 |
| 修改 | `Docs/AI/PROJECT_CONTEXT.en.md`、`Docs/AI/PROJECT_CONTEXT.md` | 在现有 PC-082 段同步服务清单、端口和 MS/GS 分工。 |
| 修改 | `Docs/AI/CURRENT_TASK.en.md`、`Docs/AI/CURRENT_TASK.md` | 同编号的实现摘要、启动与分区生命周期。 |
| 修改 | `Docs/AI/TEST_REPORT.en.md`、`Docs/AI/TEST_REPORT.md` | 同编号的干净检出失败、修正与通过证据。 |
| 修改 | `Docs/AI/RECORD_INDEX.md` | 按源记录生成英文条目的标题、行段及源哈希。 |
| 修改 | `项目分析/项目难点.md` | 记录拆分权威和运行态、定位缺失注册的过程与面试复盘。 |
| 新增 | `项目修改日志/MasterServer组队迁移-2026-11-04.md` | 汇总上述文件、设计、验证、未验证项与 Git 回执。 |

## 验收与边界

- 正式复现入口为 `LYF_Server/Tests/Run-TeamAuthoritySmoke.ps1`，需要 Windows、Visual Studio MSBuild 和 .NET Framework 4.8 开发工具。它使用独立进程、回环 TCP 和临时端口，结果保存在项目的 `_temp/`。
- 第一次干净投影在建队命令 `11200` 处发现注册依赖未提交的工作区改动。修正后六个协议由正式 `TeamMessageHandler.Register` 注册，回环会话复用同一注册表。早期测试还修正了测试程序的 Protobuf 绑定重定向、压缩帧读取与样本目录创建。均为实际失败后的定向修正。
- 最终代码树 `ffc1761ea844654c11d2b973749c3f8ced30be33` 在隔离检出中构建出 Center、Login、Gate、Game、Master 五个可执行程序。组队断言 **114 项、失败 0 项**。现有第 18 波兽人 AI 战斗断言 **39 项通过**。覆盖邀请、拒绝、审批、五人上限、队长继任、独立实例、主城可见性、单人回城、断线及场景回收。
- 隔离证据分别为 `_temp/master-team-20261104-01a1a67e/verified-tree/_temp/team-authority-69640/smoke.log` 和 `_temp/master-team-20261104-01a1a67e/verified-tree/_temp/team-authority-69640/combat.log`。正式 MS 可执行文件已生成于 `LYF_Server/MasterServer/bin/Debug/MasterServer.exe`。本轮使用已绑定的测试会话，未做 Unity 多账号真人登录、正在使用的开发服切换、跨 GS 组队和角色迁移。独立额度证据在 `_temp/master-team-20261104-01a1a67e/task-usage.json` 和 `_temp/master-team-20261104-01a1a67e/context-compactions.json`。

## Git

分支 `codex/feature-development`。任务启动基线 `c21ef96`，集成时包含同期已提交的第 18 波兽人更新，验收父提交 `07be839`。仅选取本任务的完整文件或相关改动段，保留无关工作区内容。
