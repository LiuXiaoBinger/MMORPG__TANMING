# RO 移动同步服务端与网关修改日志

- 需求：按 RO 实际移动接口、变量和职责实现正常移动同步，仅作本项目必要接入，不加多余判断或完整导航/AOI系统。
- 修改日期：2026-10-12（本轮统一证据目录沿用任务日期）。
- 分工：服务端开发子 Agent 执行业务代码、协议生成及共享 DLL；主 Agent 负责整体验收和四协作入口；独立测试审查随后验收。

## 修改文件

1. `LYF_Server/ProtoBuf/proto/RequestEntity.proto`：追加 `MoveOperationReq`。
2. `LYF_Server/ProtoBuf/proto/ResultEntity.proto`：追加 `SyncMoveNtf`。
3. `LYF_Server/Network/Proto/RequestEntity.cs`：由项目现 protoc 生成请求类和描述符。
4. `LYF_Server/Network/Proto/ResultEntity.cs`：由项目现 protoc 生成通知类和描述符。
5. `LYF_Server/Network/Socket/com/NetDefine.cs`：追加 11180/11181 移动协议常量。
6. `LYF_Server/GameServer/Components/ROMoveComponent.cs`（新增）：RO `OnMoveStart` 接现正式角色位置与移动状态。
7. `LYF_Server/GameServer/MessageHandler/Role/MoveOperationHandler.cs`（新增）：在逻辑线程按现复合会话取得角色并调用真实移动组件。
8. `LYF_Server/GameServer/Entity/Role/OnlineRole.cs`：正式容器装配移动组件。
9. `LYF_Server/GameServer/manger/RoleManager.cs`：复用现在线索引、同图 100 米范围通知其他角色。
10. `LYF_Server/GameServer/MessageHandler/ClientMessageManager.cs`：移动网络回调和正式处理器注册。
11. `LYF_Server/GameServer/GameServer.csproj`：登记两新源文件。
12. `LYF_Server/GateServer/Ctrl/GateMoveCtrl.cs`（新增）：上行和下行移动透传。
13. `LYF_Server/GateServer/GateServerApp.cs`：登记移动网关请求/通知。
14. `LYF_Server/GateServer/Ctrl/GateClientResponseForwarder.cs`：按现流程解析无错误码移动通知并转发。
15. `LYF_Server/GateServer/GateServer.csproj`：登记网关新文件。
16. `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`：与生成协议同源编译共享网络 DLL；既有 meta 不变。
17. `项目分析/RO_MOVE_SYNC_SERVER.md`：记录 RO 对照、正常调用链和未接外部依赖。
18. 本日志。

## 设计与保护

先改 proto，再生成，再接正式 `OnlineRole` 容器和现逻辑队列。沿 RO 会话反查→`OnMoveStart` 顺序；不改历史场景移动占位、不虚建第二套世界、不扩导航或 AOI、不改技能/F/CD/数据库。现有用户原稿完整备份 `_temp/ro-move-city-20261012/server/before/`，没有回退工作区。

## 开发验证

- 正式 GameServer及引用 Rebuild：0错/11既有警告；GateServer及引用：0错/4既有警告；Network及引用：0错/4既有警告。
- 真实生产程序集+现队列/在线组件+临时本机TCP 22项通过，`move-runtime.log`：`MOVE_SYNC_PASS checks=22`。
- 现protoc输出与正式生成.cs一致；原51消息类整段代码与本轮备份相等。
- 客户端Network DLL与 `server/network-bin/Network.dll` 字节一致：SHA256 `443534892fbc5999bff4b39e58722ba4af360674b76f899544838cc8e43d64e8`。
- 新业务三个类型均独立同名文件，全局类型/中文用途注释，无 foreach、三目、弱业务载荷；修改必要注册/容器/广播处附中文注释。
- 246去重编译源码/工程输入哈希和16正式路径末稿哈希留在证据目录。
- 首测试夹具编译漏引用 System.Memory/netstandard 且protobuf版本未按GameServer输出选取，仅修 `_temp` 测试编译依赖，不改正式业务/配置；随后真实执行22通过。

## 未解决与交接

未部署重启正式服务器，未验Unity两客户端真实联机、原生画面/插值/墙体和安装包/Bundle。服务端终点采用现客户端碰撞结果，不宣称服务器导航/反外挂。现同图100米广播保持既有业务，不扩动态AOI。独立测试审查结果由主 Agent 集成；四协作入口由主 Agent 统一更新。全部临时证据保留，待产品经理确认后清理。

- 文档复核修正：项目分析/RO_MOVE_SYNC_SERVER.md 的 Time 单位说明改为客户端 CostTime 每帧按 deltaMilliseconds × 0.001 以秒累积、发送直接赋给 Time；仅文档一行修正，未改业务或重测。
