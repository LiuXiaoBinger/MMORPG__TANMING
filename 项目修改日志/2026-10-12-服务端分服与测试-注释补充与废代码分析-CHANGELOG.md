# 修改日志：服务端分服与测试 — 注释补充与废代码分析

- 日期：2026-10-12
- 执行分片：CenterServer(19) + GateServer(5) + LoginServer(5) + Tests(3) = 32 个业务 cs
- 范围：只加中文注释、不改任何代码逻辑；废代码只分析不删除。
- 静态检查：`_temp/static_check_server_shard.ps1`，18 个改动文件大括号与 #if/#endif 全部配平通过。

## 一、按项目分组的修改文件清单

### CenterServer（改 13 个）

| 文件 | 改了什么 |
|---|---|
| `CenterApp.cs` | 新增类/方法 XML 注释，标注启动顺序、主循环节拍与各控制器职责 |
| `DB/DBMgr.cs` | 新增类/字段/initDB 注释，写明连接串为本地开发库、写任务队列单消费者语义 |
| `Ctrl/CenterRoleCtrl.cs` | 新增类注释；修正构造参数文档（原误写 loginModle 实为 centRoleModel）；空 OnInit/OnClientCommand 注明为 IContainer 接口实现 |
| `DB/Modle/CentRoleModel.cs` | 新增类/字段/构造注释；补全 RoleKanpaskInfo 空 param/returns 文档 |
| `DB/Modle/LoginModle.cs` | 完善类/字段/构造注释；补全 Login/RegistAccont/GetServerList/LoginGameServer/CreateRole/StartGame 的空 param/returns 文档；在末尾死方法集群上方加“当前无调用者”说明注释 |
| `Redis/RedisMgr.cs` | 英文类注释中文化；为 IsInitialized/Init/Get/GetValue/GetValueAsync/Set/LPush/LPop/RPush/RPop/HSet/HGet/HDel/Del/ExistsKey/Close/GetDatabase/ValidateKey 补中文摘要 |
| `DB/Table/AccoutTable.cs` | 修正 Email 注释（原误写“手机号”）；补全各字段语义（State=1 正常、密码明文等） |
| `DB/Table/GameServerTable.cs` | 修正类注释（原误写“用户表”）；补 IpHost/Port/State/IsNew 等字段注释，修正“服务器名称名”笔误 |
| `DB/Table/RoleKnapsackTable.cs` | 修正 SugarTable TableDescription（原误写“服务器列表”）；补 State/curitemid 遗留字段注释 |
| `DB/Table/RoleBaseArrtTable.cs` | 新增类注释；补 roleID 冗余字段注释 |
| `DB/Table/RoleTable.cs` | 新增类注释；补 State/itemUid/ServerId/Pos/CameraOffset/MapId 等字段注释（原 ServerId 仅空“//”） |
| `DB/Table/RoleCountTable.cs` | 补 RoleId/Action/key/CountKey/Count/LastRefreshTime/Version 字段注释，标注 key 为遗留 |
| `DB/Table/RoleShopPurchaseTable.cs` | 修正复合主键注释（原误提不存在的 NpcID，实为 RoleID+ShopID） |

未改但已核对注释充分：`DB/Table/EquipTable.cs`、`EquipXLGeneTable.cs`、`ItemTable.cs`、`RoleSkillTable.cs`、`DB/Task/CRoleWriteTask.cs`、`Ctrl/Center_LoginCtrl.cs`。

### GateServer（改 3 个）

| 文件 | 改了什么 |
|---|---|
| `GateServerApp.cs` | 新增类/Main 注释；为无注释的技能与物品回包注册行补中文说明 |
| `Ctrl/GateLoginCtrl.cs` | 新增类注释；空 OnInit 注明接口实现；补 OnServerCommand/OnLoginGameServerHandle/OnClientCommand 注释 |
| `Ctrl/GateRoleCtrl.cs` | 新增类注释；空 OnInit 注明接口实现；补 OnServerCommand 文档 |

未改但已核对：`Ctrl/GateClientResponseForwarder.cs`、`Ctrl/GateSkillCtrl.cs`（注释已规范）。

### LoginServer（改 2 个）

| 文件 | 改了什么 |
|---|---|
| `LoginServer.cs`（LoginApp） | 新增类/Main 注释，写明登录服连接中心服 10110、监听 10120、gRPC 验证码职责 |
| `Ctrl/LoginCtrl.cs` | 新增类注释；空 OnInit 注明接口实现；补 OnClientCommand/OnServerCommand 文档 |

未改但已核对：`Grpc/GRPCMgr.cs`（中文注释已规范）。`Grpc/Message.cs`、`MessageGrpc.cs` 为 protoc 自动生成（DO NOT EDIT），按规则不改动。

### Tests（0 个改动）

`SkillAuthoritySmoke.cs`、`SkillConfigSmoke.cs`、`SkillRoleSnapshotSmoke.cs` 三个文件已具备充分的中文测试意图与覆盖场景注释，符合要求，未改动。

## 二、注释改动合计

- 有实质注释新增/完善的文件：**18 个**（CenterServer 13 + GateServer 3 + LoginServer 2）。
- 已核对、注释本就规范无需改动：**12 个**（含 3 个 Tests）。
- 按规则不动的自动生成代码：**2 个**（Message.cs、MessageGrpc.cs）。
- 覆盖业务文件总数：32/32。

## 三、废代码分类统计（详见《项目分析/废代码分析报告-服务端-分服与测试.md》）

| 分类 | 数量 |
|---|---|
| 确认死代码（私有方法集群，可删除） | 4 个（LoginModle） |
| 未调用 public 方法（需确认/预留） | 14 个（CentRoleModel 1 + RedisMgr 12 + GRPCMgr 1） |
| 被注释代码块 | 6 处文件 |
| 空方法（接口实现，保留） | 8 处 |
| ORM 未读取字段（DB 列，保留） | 10 个 |
| 未使用 using（本次不删） | 约 15 条 |
| TODO/FIXME（有意保留） | 3 处 |

## 四、静态检查结果

- 脚本：`D:\unitypro\LYFMMORGP\_temp\static_check_server_shard.ps1`
- 结果：18 个改动文件大括号配对 OK、#if/#endif 配对 OK，`STATIC CHECK ALL PASS`。
- 未执行 msbuild/dotnet build（多子 Agent 并行，编译由主 Agent 统一执行）。
- 过程中曾误删 LoginModle.StartGame 返回类型 `StartGameRet`，已当场回读并修复，复诵确认 `public StartGameRet StartGame(StartGameReq req)` 签名完整。

## 五、未解决问题 / 下一步交接

1. 类别 A1（LoginModle 4 个死私有方法）与类别 C 注释代码块是否删除，待产品/主 Agent 决策；本次未删。
2. 类别 B 的 public 预留方法（RedisMgr 多数封装、GetRoleShopPurchaseInfo、ShutdownAsync）是否精简，待业务确认。
3. 未使用 using 未删除，建议后续单独小任务清理。
4. 连接串（DBMgr localhost:3308）、Redis 默认端口 6380/密码 123456、LoginServer 硬编码中心服端口 10110 等均为开发期写死，部署前需外置配置（仅记录，未改）。
