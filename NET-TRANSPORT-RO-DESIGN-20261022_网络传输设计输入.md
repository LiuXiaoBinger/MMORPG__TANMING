# NET-TRANSPORT-RO-DESIGN-20261022 网络传输设计输入（只读源码分析）

> 任务：评估「玩家技能释放、技能移动等实时内容走 UDP，其他走 TCP」的可行性，参考 RO 工程。
> 范围：只读 `D:\unitypro\LYFMMORGP`（LYF_Server / LYF_MMO\Assets）与 `F:\UnityPro\ro\Ro_cs422nb`；未修改任何业务代码/配置/文档/资源，未读取 git 历史。
> 标注：【源码证据】= 直接读到代码（路径:行号）；【推断】= 由相邻代码/配置推出；【未确认】= 源码缺失需运行期验证。

---

## 0. 一页结论（TL;DR）

1. **RO 的玩家实时流量（登录、移动、技能、聊天）底层全部是 TCP（Stream + NoDelay），不是 UDP，也不是启用的 KCP。** 工程里确有 KCP-over-UDP 代码（`gateserver\udpnet`），但**全工程零调用点，是编译进二进制却运行时不启用的死代码/实验代码**【源码证据】。因此「参考 RO 用 UDP 传技能/移动」在 RO 源码里找不到先例——RO 的高频做法是 **TCP + NoDelay + PTC 单向高频上报（约 100ms 节流）**，而非换传输层。
2. **本项目现状是纯 TCP 单连接**：客户端所有消息（登录/选角/进世界/技能 11170/移动 11180/商城）走同一个 `NetSocketMgr.Client.SendData`、同一条 TCP；服务端 GameServer/GateServer 全部 `SocketType.Stream`，全仓 0 处 UDP/KCP【源码证据】。
3. **技能释放（11170）在语义上是「请求-响应 RPC」**：服务端权威结算 MP/CD 后必须回 `SkillCastRet`（含错误码、当前 MP、CD 结束时间），并广播 11171/11173；**移动上报（11180）是单向 PTC**，服务端不回包。二者可靠性需求不同，不能一刀切裸 UDP。
4. **不能直接把技能请求改成裸 UDP**：现有身份完全绑定 TCP 会话（包内不带角色 ID，服务端靠会话反查），裸 UDP 无握手/无可靠保证/无顺序/无去重/无鉴权上下文，且穿越 NAT/防火墙不可控（详见 §4.4）。
5. 若坚持引入 UDP，最小可行形态是：**TCP 保持登录/鉴权/全部可靠业务；另开一条「可靠 UDP（KCP 类）或 KCP 封装」专用通道只承载实时流量，技能请求走该通道的可靠段、移动上报走不可靠段**，并先完成会话票据绑定、序号/去重、保序与权威校验补齐（§4）。更稳妥的 RO 式路线是先留在 TCP 上做 NoDelay/节流优化（§4.1 方案 A）。

---

## 1. RO 实际传输层判定（交付项 1）

### 1.1 总结论【源码证据】

| 通道 | 判定 | 承载 | 证据 |
|---|---|---|---|
| 游戏主通道（登录服 25001 / 网关 21010） | **TCP（Stream，NoDelay）** | 登录/移动/技能/聊天/RPC 全部实时协议 | 客户端 `MSocket.cs:84`；配置 `gate_conf.xml:6`、`login_conf.xml:13` |
| 服务器列表拥挤度 ping | UDP Dgram（服务端线程已注释禁用） | 仅 serverID→状态位，非游戏流量 | `MQueryServerState.cs:68`；`serverstateinfo.cpp:28-118` 整块注释 |
| controlserver MapleSDK 上报 | UDP（出站广播/单播） | 服务器负载/账号流水上报外部采集器，与玩家无关 | `controlserver\sdk\udp_socket.cpp:26`；`report_sdk_mgr.cpp:53-63,223`；`ctrl_conf.xml:42-43` |
| `gateserver\udpnet` KCP-over-UDP | **已编译链接但零调用，运行时不启用（死代码）** | 无 | `udpnetwork.cpp:47,71`；`kcppeer.cpp:20,46,57,62`；全工程排除 thirdparty/udpnet 后搜 `CUdpNetWork/CKcpPeer/ikcp_*` 零外部引用 |
| 服务器内部 link（gs/ms/ctrl/audio…） | TCP（同预编译框架）【推断】 | 集群内部转发 | `gate_conf.xml:7,10-12` 均 127.0.0.1；`gslink.h:11`、`linelink.h:9` |
| HTTP | 仅 127.0.0.1 回环 GM 接口 + CDN 资源下载 | 非实时通信 | `MHttpServer.cs:28-29`；`DownloadHelper.cs:43` |

### 1.2 客户端（C#）证据与调用链【源码证据】

文件根：`F:\UnityPro\ro\Ro_cs422nb\clientcode\CSProject\MoonClient`

- **建 socket（TCP）**：`Network\SessionLayer\MSocket.cs:84` `new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)`；`:85` `NoDelay=true`（关 Nagle，实时游戏典型调优）；`:130` `BeginConnect`；`:162` `BeginReceive`；`:268` `BeginSend`。
- **TCP 流重组（UDP 不需要这种逻辑）**：`MSocket.cs:441-456` `getPacketLen` 从前 4 字节取包长，不足则续收（粘包/拆包）。
- 全 C# 仅 3 处建 socket：`MSocket.cs:84`（TCP 游戏主通道）、`MQueryServerState.cs:68`（Dgram 状态 ping）、`MHttpServer.cs:28`（回环 HttpListener）；**无 KCP/ENet/UdpClient 游戏通道**。

**上行调用链**：
```
业务/Lua 发协议
→ MNetClient.Send (ApplicationLayer\MNetClient.cs:108 → :167 _connection.SocketObj.Send)
  / Lua SendByLua (MLuaNetClient.cs:14 → :58)
→ MConnection 持有单例 MSocket (MConnection.cs:85, :92 Connect)
→ MSocket.Send/SendByLua 入队 (MSocket.cs:189/179)
→ findSomethingToSend 发送泵 (MSocket.cs:214-269)
→ MPacker.PackProtocol/PackLuaProtocol (PresentationLayer\MPack.cs:20/42)
→ MProtocolHead 写 4 字节小端长度前缀+type+flag(+tagId)，可选 zlib (MProtocolHead.cs:122,149-159)
→ BeginSend (MSocket.cs:268) → TCP 字节流
```

**下行调用链**：
```
BeginReceive (MSocket.cs:162) → onReceiveCallback (:318) → EndReceive (:328)
→ getPacketLen 流重组 (:441) → MUnpacker.UnpackPacket (MPack.cs:140，zlib 解压 :114-119)
→ 拆 MPtc/MRpc (:154-191) → NetEventMgr.PushReceiveEvent (:373)
→ MNetClient.processMsg (MNetClient.cs:176) 分发
```

### 1.3 服务端（C++）证据【源码证据 + 推断】

- 玩家入口配置：`roserver\exe\conf\to_tx\world\gate_conf.xml:6` `<peer ip="0.0.0.0" port="21010" handler="clientlink" .../>`，**无任何 udp/kcp/protocol 属性**；登录服 `exe\conf\login_conf.xml:13` `0.0.0.0:25001 clientlink`。
- 连接类：`gateserver\network\clientlink.h:9-22` `ClientProtocolHead/RpcHead{dwLen,dwType,dwFlag[,dwTagID]}`（与客户端头一一对应）；`:25` `class CClientLink : public ListenLink`（被动接受）。
- 发送：`clientlink.cpp:68` Send(Ptc) / `:106` Send(Rpc) → `:103/:142 CNetProcessor::Instance()->Send(...)`；解码 `:168 DecodeHead`、`:236 MinSize`。
- 流重组：`common\include\netpacketcoder.h:69-85 BreakPacket` 前 4 字节取长度、不足续收。
- **KCP 死代码证据**：`gateserver\udpnet\kcppeer.cpp:20 ikcp_create / :46 ikcp_send / :57 ikcp_input / :62 ikcp_update`；`udpnetwork.cpp:47,71 socket(AF_INET,SOCK_DGRAM)`；构建链 `gateserver\CMakeLists.txt:4,31-33` 链 kcp、`winproject\gateserver\gateserver.vcxproj:101,120,190-192` 编 udpnet/*.cpp，但**无任何 `new CUdpNetWork/Init` 调用点**。
- battleserver 仅 controllink/gslink/mslink，零 SOCK_DGRAM/ikcp/clientlink，无面向客户端端口【源码证据】。

【未确认】TCP listen 的 `socket(SOCK_STREAM)/accept` 实现在预编译网络库（Win `iocp.lib/network.lib`，Linux `libepoll.a/libnetwork.so`），源码未随工程提供；旁证可定案：客户端 TCP `BeginConnect` 到 21010/25001 必须由 TCP 监听端完成三次握手，且 iocp.lib 含 `AcceptEx/WSARecv/WSASend` 符号。

---

## 2. RO 各类消息走什么通道（交付项 2）

> 前提：RO 客户端同一时刻只有一个连接对象 `MNetClient._connection`（`MNetClient.cs:9,33`），登录服→gate 是关闭旧连接后在同一 socket 对象上重连（`MConnection.cs:134 TryReConnectSilently`）。RPC（填 tagID 进等待表等 OnReply/OnTimeout，`MRpc.cs:52-58,80-87`）用于要结果的操作；PTC（单向无等待，`MPtc.cs:32-35`）用于高频/广播。协议号高 16 位编码路由：C2G=0x4347、C2T=0x4354、C2I=0x4349、C2N=0x434E（`gateserver\network\clientlink.cpp:162 IsProtocol2Gs`）。

| 消息类别 | 通道/语义 | 客户端发送点（路径:行号） | 服务端处理入口（路径:行号） | 标注 |
|---|---|---|---|---|
| **技能释放/施法** | RPC C2G（1128747757），gate 透传 gameserver，**等回包** | `Skill\MNavigationComponent.cs:350` 构造 `RpcC2G_SingingRequest`，`:437 MNetClient.singleton.Send(info)`；回包 `Network\protocol\process\action\process_rpcc2g_singingrequest.cs:12 OnReply` | `gameserver\protocol\action\rpcc2g_singingrequest.cpp:19` IMPLEMENTION，`:21 OnCall`→状态机 `:73`→`DispatcherEvent :82`；结果经状态流 `ro_games\event\action_event_handler.cpp:133 HandleSkillStartSync` 等广播 | 【源码证据】；普攻/自动战斗复用此通道【推断】 |
| **玩家移动/位移上报** | **PTC C2G 单向**（1128751369），约 100ms 节流、非每帧 | `Entity\MPlayer.cs:411 UpdateMove()`，`:426` 构造 `PtcC2G_MoveOperationReq`，`:457 Send(ptc)`；节流 `:361-363`（CostTime>0 且姿态变化且 >0.1s） | `gameserver\protocol\action\ptcc2g_moveoperationreq.cpp:20 Process`→`:83-118` 派发 `ROMoveStartActionArgs`；他人位置 `action_event_handler.cpp:636 HandlePositionSync`→`role.cpp:1448 DoStepSync` 批量推 `PtcG2C_SyncPositionNotify`(1195589771) | 【源码证据】；视野裁剪/插值在 ROGameLibs 原生层【推断】 |
| **状态同步（HP/属性/buff）** | **服务端主动 PTC G2C 推送，无轮询** | 无发送；接收 `process\attribute\process_ptcg2c_attrupdatenotify.cs:14 Process` | 增量推送 `ro_games\event_processor\action\attribute_change_processor.cpp:94-105`；进场景全量 `InitAttrPush :139-153`；buff `action_event_handler.cpp:423/459/494/739` | 【源码证据】 |
| **使用物品** | RPC C2G（1128773760） | Lua `clientproj\Assets\Scripts\Lua\ModuleMgr\PropMgr.lua:396`，`:406 SendRpc` | `gameserver\protocol\item\rpcc2g_useitem.cpp:23`，`:39 OnCall` | 【源码证据】 |
| **拾取掉落** | RPC C2G（1128748650） | `Entity\MDrop.cs:300` 构造，`:302 Send(rpc)` | `gameserver\protocol\item\rpcc2g_pickupitem.cpp:7`，`:9 OnCall` | 【源码证据】 |
| **装备/脱装/移动/整理背包** | RPC C2G（1128749880/1128744877/1128746186/1128785581） | 【未确认】发送点未在 Lua/C# 命中 | `protocol\item\rpcc2g_equipitem.cpp:11`、`rpcc2g_moveitem.cpp:14`、`rpcc2g_sortbag.cpp:11` OnCall | 服务端【源码证据】，客户端【未确认】 |
| **背包数据下发** | 服务端主动 PTC `ItemChangeNtf`(1195630403)，无「打开背包」请求 | 无（本地 UI） | `gameserver\item\baglistener.cpp:38` 构造，`:169 Send` | 【源码证据】+ 无开包请求【推断】 |
| **取登录服地址** | **先 HTTP GET**（非 TCP） | Lua `ModuleMgr\BaseAuthMgr.lua:136-138` 拼 `GETLOGIN_API`，`:157 HttpTask GetResponseAynsc` | Web/门户，不在本工程 | 【源码证据】 |
| **认证 CertifyRequest** | RPC C2I（1128911720），TCP 连登录/信息服 | `BaseAuthMgr.lua:250`，`:267 SendRpc` | info/登录门户服（未展开） | 客户端【源码证据】，服务端【未确认】 |
| **查 gate 列表** | RPC C2I（1128921835），同登录服 TCP | `CommonServerInfoMgr.lua:9`，`:17 SendRpc`；`BaseAuthMgr.lua:324/345` | info 服（未展开） | 客户端【源码证据】，服务端【未确认】 |
| **gate 登录 ClientLoginRequest** | RPC C2T（1129625954），**gate 本地终结**；先 `BaseAuthMgr.lua:358 CloseConnection`，`:423` 重连 gate | `BaseAuthMgr.lua:429`，`:456 SendRpc` | `gateserver\protocol\login\rpcc2t_clientloginrequest.cpp:14`，`:16 OnCall`→`:74` 转 `RpcT2N_LoginRequestNew`，`:88 OnDelayReplyRpc` | 【源码证据】 |
| **选角/建角** | RPC C2N（1129200538/1129232997），gate 转发 name 服 | `SelectRoleMgr.lua:160/:164`；建角 `:204/:213` | name 服（gate 无本地实现） | 客户端【源码证据】，路由【推断】 |
| **进入场景** | RPC C2G（1128751841），gate 透传 gameserver | `Scene\MSceneLoader.cs:158 SendDoEnterScene()`，`:162` 构造，`:164 Send` | `gameserver\protocol\scene\rpcc2g_doenterscene.cpp:30`，`:180 OnCall`，`:216/:247 InitAttrPush` | 【源码证据】 |
| **校时/保活** | C2T，gate 本地处理 | 校时 `Utility\MServerTimeMgr.cs:106/:108 Send`；保活应答 `process\login\process_ptct2c_keepalivepingreq.cs:14-15` | `gateserver\protocol\common\rpcc2t_synctime.cpp:7/:9 OnCall` | 【源码证据】 |

**gate 分流**：clientlink 是唯一客户端入口，本地终结 C2T（登录/校时/保活/登出，`gateserver\ptcregister.cpp:61-85`）；C2G 按会话 `m_dwGsLine` 由 `gslink.cpp:63-101 GetForwardConnID` 透传 gameserver，跨服走 RSLink；C2I/C2N 转信息服/name 服【源码证据+推断】。

---

## 3. 本项目现状（交付项 3）

### 3.1 TCP 连接建立【源码证据】

- 监听/Accept：`LYF_Server\Network\Socket\NetServer.cs:33-54 StartServer`；`:37 new Socket(InterNetwork, Stream, Tcp)`（原生 Socket，非 TcpListener）；`:48 Listen(100)`；`:59-62 BeginAccept`；`:69-90 ClientConnectCB`（`:74 EndAccept`→`:79 new Session`→`:80 ReceiveData`→`:83` 递归 Accept）。
- 主动连接：`Network\Socket\NetClient.cs:45-64 StartConnect`（`:53` Stream/Tcp，`:57 BeginConnect`）；断线重连 `:148-156 SetReconnectTimer`（3s 首连/10s 周期）。
- 会话：`Network\Socket\com\Session.cs:25-31` 构造即 `SessionMgr.AddSession`；`HandleCommand :46-70`（`:61` 盖 UnitySessionId、`:66` 盖 GateSessionId，`:69 OnServerCommand`）；会话表 `SessionMgr.cs:18-55`。
- 收包循环：`Network\Socket\com\ServerBase.cs:44-90 BeginReceive/OnReceiveCB`→`ProcessReceiveBuffer :93-128`。
- 进程拓扑：GameServer 监听 10130、GateServer 监听 10140（面向 Unity）并连 Game 10130、LoginServer 10120、Center 10110（`GameServer\GameServerApp.cs:14-20`；`GateServer\GateServerApp.cs:19-23`）。

### 3.2 消息封装（线格式）【源码证据】

- 帧：`[2B 小端长度][1B 压缩标志][2B 小端 CRC16][体]`，体 >200B GZip；封包 `Network\Socket\com\NetUtils.cs:22-54 MakeData`；解包 `:61-80 ParseData`；CRC16 `:128-141`；GZip `:87-121`。**无魔数、无版本号**；小端【推断，全用 BitConverter 无大端转换】。
- 粘包分包：`ServerBase.cs:93-128`（2B 长度定界，半包前移留存 `:119-127`）。
- 信封 Protobuf：`ProtoBuf\proto\RequestEntity.proto:5-10 BasePackage{proto_code=1, unity_session_id=2, gate_session_id=3, bytes data=10}`；发送 `ServerBase.cs:158-177`（`:174 _socket.Send(MakeData(ToByteArray()))`，同步 Send）。

### 3.3 Protobuf 协议生成与分发【源码证据】

- proto 仅 `ProtoBuf\proto\RequestEntity.proto`、`ResultEntity.proto`（另有 gRPC 旁路 `VarifyCodeServer\message.proto`，与游戏通道无关）。
- 生成：手动跑 `ProtoBuf\aaa.bat:11-13`（`bin\protoc --csharp_out=out`）→ `move_files.bat:11,44` 搬到 `Network\Proto\`；预生成 `Network\Proto\RequestEntity.cs/ResultEntity.cs`，`Network.csproj:105-106` 直接 Compile，**无 MSBuild Protobuf 项/prebuild**。
- 两级分发：网络层 `协议号→IContainer`（`NetServer.RegistCommand`，GameServer 登记 `GameServer\MessageHandler\ClientMessageManager.cs:371-389`）；逻辑层 `(Source,协议号)→IGameMessageHandler`（注册表 `:18-19`，注册 `:357-426`，分发 `ProcessCommand :147-189`）。协议号常量 `Network\Socket\com\NetDefine.cs:19-48`：`CMD_SkillCastCode=11170 :40`、`11171 :42`、`11173 :44`、`CMD_MoveOperationCode=11180 :46`、`CMD_SyncMoveCode=11181 :48`。

### 3.4 MoveOperationHandler（11180）【源码证据】

- `GameServer\MessageHandler\Role\MoveOperationHandler.cs:2-27`：`CanHandle :7-10`（仅 GateServer+11180）；`Handle :14-26`（`:17-18` 会话反查角色，`:24 ParseFrom MoveOperationReq`，`:25 ROMoveComponent.OnMoveStart`）。
- 处理/广播：`GameServer\Components\ROMoveComponent.cs:21-41 OnMoveStart`——**无速度/位移合法性校验**，`:25-27` 直接采用客户端终点写 `Pos`，注释 `:27` 明确「本项目没有 RO 场景高度/导航结算，直接沿用客户端碰撞完成后的终点」；`:28` 调 `RoleManager.SyncMove`。
- 广播：`GameServer\manger\RoleManager.cs:551-569`（跳过本人，同图 100m 内发 11181，邻近判定 `:639-661`）。
- 链：Unity→Gate `GateServer\Ctrl\GateMoveCtrl.cs:10-13` 透传→Game `Session.HandleCommand`→入队（`GameGlobal.cs:48-51`）→逻辑帧 `ProcessCommand`→Handler→`OnMoveStart`→`SyncMove`→附近 `SendToClient`（`OnlineRole.cs:96-126`）→Gate `GateMoveCtrl.cs:18-23`→Unity。

### 3.5 SkillCastHandler（11170）【源码证据】

- `GameServer\MessageHandler\Skill\SkillCastHandler.cs:5-115`：`CanHandle :11-15`；`Handle :19-95`（解析 `:36`、会话/在线校验 `:50-62`、组件校验 `:65-84`、`:89 TryCast`、`:90 SendResult`、`:91-94 BroadcastSkillStart`）；`SendResult :100-114` 原路回 11170。
- 权威结算：`GameServer\Components\ROSkillComponent.cs:251-401 TryCast`（参数→快照→在线/HP→互断→已学等级 `:310`→Luban 核心 `:317`→职业 `:327`→类型 `:332`→`CanSkill :158-178` 目标模式/CD→MP `:346`→扣 MP/CD `:376-382`→`FireSkill :384`）；**本期无命中/伤害**（`:180` 注释）。
- 广播：开始 `RoleManager.cs:495-502`（本人+100m，11171）；结束由帧推进 `Update :405`→`EndSkill :443-463`→`BroadcastSkillEnd :507-514`（11173）。

### 3.6 客户端发送入口【源码证据（调用）/DLL 参考（实现）】

- 连接管理：`LYF_MMO\Assets\GM_Game\Scripts\Manger\NetSocketMgr.cs:9` 单例；`ConnectServer :64`（`:71 new NetClient`，`:82 StartConnect`）；选服重连 `controller\UIController\LoginCtrl.cs:197`，先断旧连，**同一时刻仅一条 TCP**。
- 统一发送入口：`NetSocketMgr.Client.SendData(protoCode, ByteString)`；实现 `ServerBase.SendData`（共享源码 `ServerBase.cs:180→158`）。
- **技能链**：`PlayerInputCtr.DispatchSkillShortcut :410→:428 SkillKeyEvent` → `SkillCtrl.OnSkillKey :272` → `MSkillClassicsHandler.CastSkill :303` → `MWaitingDataMgr.SetWaitSkill :16→DoCastSkill :25/:32 DispatchEvent MEvent_Navigation` → `MNavigationComponent.OnGetEvent :82` → `closeTargetAfter :373/:377 先 UpdateMove` → `OnAfterAction :383`（CanStart 校验 :389，组装 SkillCastReq :402）→ **`SendData(CMD_SkillCastCode=11170)` MNavigationComponent.cs:403**。事件驱动按次发送，非每帧。
- **移动链/频率**：`PlayerInputCtr.DispatchMovement :470→:502` 本地事件；帧驱动 `Global.Update :127→140`→`SceneEntityWorld.Update :35`→`PlayerEntity.Update :125` 门控（≥0.1s 节拍 `:153` + 姿态变化：位移≥1cm/朝向>1°/状态变/跳跃段变 `:155-161`）→`UpdateMove() :40`→**`SendData(CMD_MoveOperationCode=11180)` PlayerEntity.cs:69**。约 10Hz 节流，非每帧。
- **全部消息同一入口**：登录 `LoginCtrl.cs:132`、注册 `:168`、服务器列表 `:184`、验证码 `:242`、登录游戏服 `:220`、开始游戏 `CreateRoleCtrl.cs:280`、创角 `:304`、进世界 `MainCityMgr.cs:502`、技能 `MNavigationComponent.cs:403`、移动 `PlayerEntity.cs:69`、商城 `ShopMgr.cs:135`，均走同一 `SendData`/同一条连接；背包无主动发包（服务器推 11110）。
- **UDP/KCP 痕迹：无**。全仓 `.cs`/Assets 文本与 Network.dll 二进制扫描 `UdpClient/SocketType.Dgram/ProtocolType.Udp/kcp/Dgram/quic` 无真实命中，全部 Stream/TCP【源码证据】。

---

## 4. 最小可行方案设计输入（交付项 4，不含实现代码）

### 4.1 路线选择（先认清 RO 给的参照）

- **方案 A（RO 式，零新传输，推荐先评估）**：保持单 TCP，开启/确认 NoDelay（对照 RO `MSocket.cs:85`），移动维持现状 ~10Hz PTC 节流，技能维持 RPC 等回包。RO 证明「实时流量跑在 TCP 上 + NoDelay + 高频单向 PTC」对这类 MMO 是成立的；KCP 在 RO 里是未启用的预留，不构成「RO 用 UDP」的证据。
- **方案 B（双链路：TCP 可靠 + 实时专用通道）**：TCP 继续承载登录/鉴权/背包/商城/选角/进世界等全部可靠业务；另开一条实时通道（优先可靠 UDP 封装如 KCP，而非裸 UDP），只承载 11180 移动上报（不可靠段）与 11170 技能请求（可靠段，需回包）。

### 4.2 通道划分建议（按语义，不按「实时」笼统切）

| 消息 | 现状语义 | 建议通道 | 理由 |
|---|---|---|---|
| 技能释放 11170 / 回包 SkillCastRet / 11171 / 11173 | RPC 请求-响应，权威结算 MP/CD，需错误码 | 可靠段（可靠UDP/KCP 或暂留 TCP） | 丢包=客户端不知技能是否被接受，本地预测与权威脱节 |
| 移动上报 11180 | 单向 PTC，最新位置覆盖旧段 | 不可靠段（允许丢包/乱序，旧段丢弃） | 时延敏感，丢一个旧位置段无危害 |
| 状态推送 11171/11173/属性/背包 | 服务端主动推 | TCP（或可靠段保序） | 技能开始/结束、buff 需保序，背包需可靠 |
| 登录/鉴权/选角/进世界/商城 | 强可靠、需结果 | TCP | 现有会话与权限全部建立于此 |

### 4.3 可靠性 / 顺序 / 丢包 / 重复 / 切换 / 鉴权 / 服务器权威注意事项

- **可靠性分级**：技能请求必须有确认与重传（等价现有 RPC 等待表，对照 RO `MRpc.cs:52-58`）；移动上报不重传，只发最新段。
- **顺序**：技能流 11170→11171→11173 必须保序（开始先于结束）；移动段按 `Time`/序号单调推进，迟到旧段直接丢弃（现状 `MoveOperationReq.Time` 段式语义，`PlayerEntity.cs:64`）。
- **丢包**：移动丢包靠下一段自愈插值；技能请求丢包由可靠段重传，超时按现有 `SkillCastRet.CmdCode` 失败语义处理。
- **重复包**：实时通道每包带递增序号；技能复用现有 `SkillUuid`（1-9999 循环，`MNavigationComponent.cs:396`）做服务端去重，防连按/重传重复扣 MP/CD。
- **连接切换/双链路**：TCP 登录成功后由服务端下发「UDP 会话票据（token + 允许的 remote 信息 + 过期时间）」；UDP 首包携带票据，服务端建 `remote endpoint → session` 映射；TCP 断开/登出时同步吊销 UDP 映射；重连必须先 TCP 重鉴权再重建 UDP 映射，不允许裸 UDP 自行恢复。
- **鉴权**：现有身份完全来自 TCP 会话（`Session.cs:61/66` 盖戳，Handler 靠 `GateSessionId/UnitySessionId` 反查角色，`SkillCastHandler.cs:50-52`、`MoveOperationHandler.cs:17-18`，包内不带角色 ID）；UDP 包必须自带票据并由服务端校验绑定，不能信任包内任何身份字段。
- **服务器权威（server authority）**：现状移动**无任何速度/距离校验**（`ROMoveComponent.cs:25-27` 直接采用客户端终点），切 UDP 不仅不解决反而放大伪造风险；上线实时通道前应先补移动限速/最大步长/场景合法性校验。技能 MP/CD 已在服务端 `TryCast`（`ROSkillComponent.cs:251-401`）权威结算，保持不下放客户端。
- **帧格式适配**：现有 `[2B长度][1B压缩标志][2B CRC16]`（`NetUtils.cs:22-54`）是为 TCP 流设计的；UDP 需独立 datagram 封装，注意单包不超 MTU、去掉流重组假设、CRC 保留。
- **NAT/防火墙**：家用/运营商 NAT 可能阻挡入站 UDP，需保持出站打洞保活；服务器需单独开放 UDP 端口，不能复用 10140 TCP。

### 4.4 为什么不能直接把技能请求改成「裸 UDP」（专门论证）

结合本项目源码现状，裸 UDP（无 KCP/无可靠层）至少有 7 个硬障碍：

1. **无连接、无会话可反查**：现有角色身份不随包携带，`SkillCastHandler.cs:50-52` / `MoveOperationHandler.cs:17-18` 靠 TCP 会话（GateSessionId+UnitySessionId）反查 OnlineRole；裸 UDP 到达时服务端没有连接对象，无法定位角色，必须另建 endpoint→session 映射，等于重写一层会话管理。
2. **无可靠保证，技能是 RPC 语义**：11170 必须回 `SkillCastRet`（错误码/CurrentMp/CD 结束时间，`SkillCastHandler.cs:90,100-114`）；裸 UDP 丢包后客户端永远不知道技能是否被接受，本地预表现（`MNavigationComponent.cs:428 BeginPrediction`）与服务端已扣 MP/CD 脱节。
3. **无顺序**：UDP 乱序会让 11173（结束）先于 11171（开始）到达，或移动段倒退，表现错乱。
4. **无去重**：网络重传/连按会产生重复 11170，服务端重复扣 MP/CD（现状靠 TCP 流天然不重复）。
5. **无鉴权上下文**：登录 token/会话在 TCP 上建立（11020 登录、11040 登录游戏服），裸 UDP 包与该上下文无关联，伪造源地址即可冒发技能/移动（UDP 源地址可伪造，TCP 三次握手做不到）。
6. **NAT/防火墙不可控**：裸 UDP 无握手，许多网络环境丢弃入站 UDP；客户端在 NAT 后端口会变化，服务端无法主动回包。
7. **现有帧/流假设失效**：2B 长度+CRC+GZip+TCP 流重组（`ServerBase.cs:93-128`）在 UDP datagram 模型下不成立，裸改会出现半包/粘包误判。

结论：**技能请求至少需要可靠 UDP（KCP 类带 ack/重传/保序）或保留 TCP；只有移动这类单向可丢包流量才适合不可靠 UDP，且仍需票据鉴权与序号去重。**

---

## 5. 未确认项与建议下一步验证（交付项 5）

### 5.1 证据缺口（源码层）

1. RO 服务端 TCP listen 的 `socket()/accept()` 在预编译库（iocp.lib/libepoll.a），源码缺失；内部 link（gslink/mslink）传输类型为【推断】TCP。
2. RO 装备/脱装/移动/整理背包（1128749880 等）的**客户端发包调用点**未命中（疑在通用 UI 封装、`clientpackproj` 镜像包或原生层）。
3. RO C2I（CertifyRequest/QueryGateIPNew）、C2N（选角/建角）服务端在 info/name 服，未展开；ROGameLibs 原生层视野裁剪/插值/状态流合并未见源码。
4. 本项目客户端 `NetClient/ServerBase/NetUtils/NetDefine` 源码不在 Assets（仅 Network.dll），DLL 与 `LYF_Server\Network` 共享源码是否逐字同版未验证。
5. `ServerBase.SendData` 为同步 `_socket.Send`（`:174`），多线程并发同连接是否有竞争/锁未深查；自动重连成功后是否需重发登录/进世界包，Assets 内无显式重登流程。
6. protoc 具体版本（目录有 protoc-28.3-win64.rar 与 bin\protoc.exe）未执行 `--version`（只读约束）。

### 5.2 建议下一步验证（运行期/抓包/配置，不写实现代码）

1. **抓包定现状**：本地起服 + 客户端，Wireshark 过滤 10130/10140 端口，确认技能/移动是否全走 TCP、RST/重传情况、实际包率与时延（验证 §3 的静态结论）。
2. **量化 TCP 是否真瓶颈**：统计 11180 实际发包频率、RTT、TCP 重传率、NoDelay 是否生效；若重传率低，优先方案 A（TCP+NoDelay），不必引入 UDP。
3. **核实 KCP 死代码**：在 RO 工程搜索 `CUdpNetWork` 实例化与 `Init` 调用点（已静态判零），可在运行期确认 udpnet 未 bind 任何 UDP 端口。
4. **确认部署网络**：目标环境是否有公网 IP、防火墙/NAT 策略、是否允许 UDP 入站（决定方案 B 可行性）。
5. **补齐权威校验清单**：在引入实时通道前，先确认服务端移动限速/步长校验、技能冷却/MP 结算的覆盖范围（现状无移动校验，`ROMoveComponent.cs:25-27`）。
6. **DLL 对齐核对**：比对 Assets 的 Network.dll 与 LYF_Server\Network 编译产物版本，确保线格式（小端/CRC/GZip）两端一致。
7. **低风险验证**：若选方案 B，先以「移动 11180 走不可靠 UDP、技能 11170 暂留 TCP」做最小切片验证票据绑定与序号去重，再决定技能是否上可靠 UDP。

---

### 附：证据来源分片（只读分析，未改动任何文件）
- RO 传输层：`clientcode\CSProject\MoonClient\Network\SessionLayer\MSocket.cs`、`ApplicationLayer\MNetClient.cs/MConnection.cs/MLuaNetClient.cs`、`PresentationLayer\MPack.cs/MProtocolHead.cs`；`roserver\gateserver\network\clientlink.cpp/.h`、`udpnet\*`、`common\include\netpacketcoder.h`、`exe\conf\to_tx\world\gate_conf.xml`。
- RO 消息通道：`MoonClient\Skill\MNavigationComponent.cs`、`Entity\MPlayer.cs/MDrop.cs`、`Scene\MSceneLoader.cs`、`Network\protocol\**`；`roserver\gameserver\protocol\**`、`ro_games\**`；Lua `clientproj\Assets\Scripts\Lua\ModuleMgr\*.lua`。
- 本项目服务端：`LYF_Server\Network\Socket\**`、`ProtoBuf\proto\*.proto`、`GameServer\MessageHandler\**`、`GameServer\Components\ROSkillComponent.cs/ROMoveComponent.cs`、`GameServer\manger\RoleManager.cs`。
- 本项目客户端：`LYF_MMO\Assets\GM_Game\Scripts\Manger\NetSocketMgr.cs`、`SceneEntity\Component\MNavigationComponent.cs`、`SceneEntity\Entity\PlayerEntity.cs`、`controller\UIController\LoginCtrl.cs`。
