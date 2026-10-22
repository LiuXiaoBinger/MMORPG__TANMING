# KCP 双通道实现说明（第三版）

任务 NETWORK-KCP-IMPLEMENT-20261022，分支 codex/network-kcp-development，基线 756ca35。设计依据见 `项目分析/RO_NETWORK_TCP_KCP_DESIGN.md`；本文记录实际落地的代码结构与边界。

## 一、为什么是 TCP + KCP

现有 Unity↔服务器全走一条 TCP：移动上报（11180）、附近移动（11181）、施法请求与结果（11170）、技能开始/结束（11171/11173）与登录、背包、商城共用一条流，实时包会被大包和重传拖住。第一版把五类实时协议移到一条可靠有序的 KCP over UDP 上，其余业务保持 TCP，登录与场景快照仍走 TCP，因此不需要新的业务协议，也不改变现有调用顺序（先 UpdateMove 再施法）。

不可靠位置流、服务器权威移动、内网 Gate→Game 改造都不在本版，避免一次改动同时触及移动语义与内网链路。

## 二、代码结构

| 层 | 类型 | 职责 |
| --- | --- | --- |
| 第三方 | `KcpProject.KCP`、`KcpProject.ByteBuffer` | 上游 limpo1989/kcp-csharp 固定提交 6ed5b68（MIT）逐字保留，只提供协议算法 |
| 网关传输 | `UdpNetwork` | UDP 监听、数据报分发、conv 路由、待绑定表、Peer 生命周期、KCP 更新线程与空闲清理 |
| 网关会话 | `KcpPeer` | 单会话 KCP：通道锁、输入、发送、更新、按序取包、输出回写、投递独占（唯一消费者） |
| 客户端 | `KcpClient` | UDP Socket、MAC 地址证明、有限重发与绑定期限、TCP 启用确认、保活与静默失效、按序交付 |
| 协议封装 | `KcpDatagram`、`KcpAuth`、`KcpBindPayload`、`KcpBindResult`、`KcpDefine` | 28 字节外层头、HMAC-SHA256 截断认证、绑定响应载荷、绑定结果码、常量与协议集合 |
| 分流 | `ServerBase.TrySendRealtime` | 唯一发送钩子，返回三态 `RealtimeSendResult`，Session 与 Unity NetClient 各自覆写 |

## 三、分流位置为什么在 ServerBase

现有业务大量直接调用 `NetSocketMgr.Client.SendData`（最终进 `ServerBase.SendData`），只改 `NetSocketMgr.Send` 接不到。因此在 `ServerBase.SendData` 内加可重写钩子：

- 上行：`NetClient`（`ClientType.Unity`）覆写后判断「通道就绪 + 属于实时集合」，满足才走 `KcpClient`，否则回原 TCP 帧。
- 下行：Gate 的 `Session` 覆写后按协议号选择 `KcpPeer`，`GateClientResponseForwarder` 与各 Ctrl 不用改。
- 服务间连接（LoginServer/GateServer/GameServer 类型）恒定返回 `KeepTcp`，不可能误用客户端 UDP。

钩子返回三态而不是布尔值，是为了区分两种失败：

| 结果 | 含义 | `SendData` 行为 |
| --- | --- | --- |
| `KeepTcp` | 不属于实时集合，或本连接从未启用成功 | 按原规则写 TCP 帧 |
| `Sent` | 实时通道已写入 | 直接返回，不写 TCP |
| `Dropped` | 实时通道曾启用、当前已失效 | 记录丢弃，**禁止写 TCP** |

第三态针对的正是「已启用后失效」：移动与技能无法确定对端是否执行过，改投 TCP 会重复执行或造成跨通道乱序；`NetClient`/`Session` 各自用「曾启用」标记锁定该状态，即使通道引用已被释放也不会退回 TCP。

`NetUtils.MakeData`（2 字节长度 + 压缩标志 + CRC16 + 可选 GZip）与 KCP 载荷互不干扰：KCP 内层直接承载 `BasePackage` 字节，不套流帧，因为 KCP 自带边界与顺序。

## 四、绑定与认证

绑定分两步，且**启用结论只由可靠 TCP 决定**，避免 UDP 回执丢失造成一边启用、一边等确认的长期分裂。

1. 客户端在角色进入世界后发 `CMD_KcpBindCode`（TCP，11190，空载荷）。
2. Gate 生成随机非零 conv 与 16 字节令牌，登记待绑定并回包 `Granted`（结果码 / conv / 令牌 / UDP 端口）。待绑定有效期与客户端绑定期限**取同一常量**（3 秒），避免客户端已放弃而网关仍挂着待绑定。
3. 客户端向 UDP 端口发 `TYPE=1` 绑定请求，**载荷为空**，唯一证明是「用令牌对空载荷计算的 MAC」。密钥本身绝不出现在 UDP 中。
4. Gate 校验载荷长度必须为 0 且 MAC 正确，建 Peer、加入 conv/会话两张表，然后：
   - 在既有表锁内重核Pending归属并入表、`AttachRealtimePeer`；
   - 释放表锁后经**可靠 TCP** 回 `Enabled`（含 conv，不含密钥）；
   - 最后发 `TYPE=2` 回执（只是地址证明的快速回执，丢失无副作用，客户端仍靠 TCP 确认启用）。
5. 客户端收到 `Enabled` 且 conv 匹配时才 `ConfirmEnabled` 进入就绪；此前实时协议继续走 TCP，KCP 下行段仍被接收但不交付业务。
6. 请求或回执可能丢失：客户端每 300ms 重发同一请求，网关对已绑定会话幂等重发 `Enabled` 与回执，不新建第二条通道；3 秒期限到仍未启用则关闭 UDP 通道，本会话确定保持 TCP。
7. 3 秒期限到仍未启用：客户端关闭 UDP 通道，并经可靠 TCP 回传 `Abandoned`(conv)；`GateKcpCtrl` 收到后释放待绑定与可能已挂接的 Peer，会话回到 TCP。TCP 也可能同时断开，此时由 `Session.Disconnect` 兜底清理。
8. 之后所有 `TYPE=4` 数据报以令牌计算 HMAC-SHA256 前 16 字节，校验失败直接丢弃，不回复、不建状态。

日志脱敏：`ServerBase.SendData` 对 `CMD_KcpBindCode` 只记录 `result` 与 `conv`，并标注「令牌已省略」；其他业务日志保持原格式。

conv 只用于查表，不承担身份；客户端自填的 `UnitySessionId/GateSessionId` 不参与路由，KCP 上行清空后由原 `Session.HandleCommand` 按绑定会话补入。

**威胁边界（如实披露）**：TCP 未做 TLS，令牌在明文 TCP 上传输可被同链路窃听；UDP 数据报不加密，也不做防重放（重放绑定请求只得到幂等回执，重放 KCP 段由序号与窗口去重）。本方案只保证非会话方无法伪造或劫持已认证会话，不宣称是安全传输。

## 五、生命周期与失效

- TCP 断开 → `Session.Disconnect` 释放 Peer 与待绑定条目；客户端 `Disconnect`/连接失败 → 关闭 `KcpClient`。
- 重登或同会话重绑：重新分配 conv 与令牌，旧 conv 立即移出路由表，旧通道数据不再投递。
- 初次绑定失败或服务端未启用 UDP：客户端 3 秒内放弃并保持 TCP，网关也不会挂接 Peer，两端结论一致。
- 运行期失效：客户端每 5 秒发 `TYPE=5` 保活（网关原样回执），静止站立玩家不会被 30 秒空闲阈值误清理；一旦 15 秒收不到任何已认证入向报文，判定通道失效并触发现有 TCP 断线恢复。已发出的实时协议不重投、不静默退回 TCP 继续发，避免未知是否执行的施法被重复结算。
- 并发安全：KCP 通道本身不是线程安全的，`KcpPeer`/`KcpClient` 用实例锁把 `Send/Input/Update/PeekSize/Recv` 串行，业务回调在锁外执行；UDP 发送与会话 TCP 帧写入各有独立发送锁，锁序固定为「通道锁 → 发送锁」。
- 投递顺序：网关侧有接收线程与更新线程两处触发投递。`KcpPeer` 用 `_delivering` 投递独占保证同一 Peer 只有一个线程能取包回调，未取得者立即返回、由持权线程按序交付到通道暂空为止，因此业务回调严格按 KCP 序号串行、不会并发也不会倒序；投递权在 `finally` 释放，异常不会卡死通道。
- 通道归属代次：`KcpClient.OwnerGeneration` 记录通道建立时的 TCP 连接代次，`ShouldReleaseOnFailure` 只对**同一代次**的失败通知返回 true。自动重连会分配新代次与新通道，旧连接的迟到失败回调只记日志，不会关闭正在使用的新通道。

## 六、验证结果

- Network、GateServer、GameServer、CenterServer 编译 0 错误；Unity 289 源编译 0 错误（引用本轮新 Network.dll）。
- 真实 Network.dll 回环 **78/78** 通过（日志 82 个 PASS、0 个 FAIL），含真实 UDP 代理注入：丢包 1/7 + 复制 1/5 + 相邻交换乱序下双向只交付一次且严格按序、并发 4×40 上行与 3×20 下行不重不漏、初次上行阻断 3 秒放弃且网关无 Peer、绑定回执丢失仍靠 TCP 确认启用、下行整向阻断 15 秒静默检测只通知一次且已发消息不重投、静止站立 6.5 秒保活刷新、同会话重绑旧 conv 失效。
- 本轮新增：投递串行化 4 条（并发回调为 0、完成顺序与发送顺序一致、慢回调期间下行仍可写出）；运行期失效不写 TCP 4 条（经真实 `NetClient` 发送路径）；绑定超时清理 7 条（含真实 `GateKcpCtrl` 放弃清理、过期待绑定不产生 Peer）；日志脱敏 5 条；代次归属 6 条。
- 前后对照：`probe_delivery_window.py` 临时关闭投递独占后运行**同一份**断言，复现并发回调 `overlaps=4`；还原后为 0。
- 未覆盖：真实 Unity 双客户端联机、弱网端到端测量、MP 扣除链路、跨通道实体早到时序的运行时实测（现状为「丢弃不缓存」，见 `_temp/network-kcp-20261022/developer-report.md` 第七节）；`MonoBehaviour` 侧的自动重连时序未在 Unity 内运行，代次判定只在函数级验证。不能把回环测试当作正式联机验收。

## 七、豆包第1轮修复：零发送生命周期、绑定取消串行与断开顺序

第三轮复审在真实 Socket 上复现两条阻塞，本轮做最小修复，不新增代次、队列或通用框架。

1. **零发送释放后禁止回退 TCP（问题1）**：原 `NetClient._realtimeEverEnabled` 只在 `TrySendRealtime` 实际发送时从 `KcpClient.HasBeenEnabled` 复制；通道启用后若零实时发送就被释放引用，标记仍为 false，下一条实时协议被误判 `KeepTcp` 写回 TCP。修复：`KcpClient.ConfirmEnabled` 成功后经既有 `OnBound` 回调（`NetSocketMgr` 在创建通道时接线）调用新增的 `NetClient.MarkRealtimeEverEnabled()`，在 TCP 启用确认到达的瞬间即锁定“禁止回退”，不等待首条业务发送；发送时观察到 `HasBeenEnabled` 的补记保留为兜底。新 TCP 连接在 `OnConnectCB` 重置标记，初次从未启用仍保持 TCP。
2. **绑定完成与取消/释放串行（问题2）**：原 `CompleteBind` 取 Pending 快照后放锁，入表后在锁外发 Enabled 并 `AttachRealtimePeer`，与 TCP Abandoned/Disconnect 交错时会出现字典已空而 Session 重挂死 Peer。修复：在既有 `UdpNetwork._sync` 表锁内重新核对 `_pending[conv]` 仍存在且归属同一会话，失败直接返回；复核通过后在同一把锁内完成入表与 `AttachRealtimePeer`，TCP 确认与 UDP 回执移到锁外。锁内只做字段赋值，不回调业务，避免持锁死锁。
3. **Disconnect 顺序对齐 Abandoned**：`Session.Disconnect` 原先 `DetachRealtimePeer` 再 `ReleaseSession`，断开线程等待表锁期间若 CompleteBind 已 Attach，Release 只删表不清会话引用，留下悬挂 Peer。修复为先 `udpNetwork.ReleaseSession(SessionID)` 再 `DetachRealtimePeer()`，与 Abandoned 路径一致：释放先拿锁则 Pending 缺失、复核返回；绑定先拿锁则 Attach 完成后由 Release 清表、Detach 清引用。
4. **样式与脚本**：`GateKcpCtrl` 新增三目运算符改为 if 分支并补局部注释；`run_loopback.py`、`probe_delivery_window.py` 的子进程解码由 gbk 改为 utf-8（实际输出 UTF-8，原 gbk 解码导致中文日志乱码）。

验证：四工程与客户端 289 源编译 0 错误；真实 Network.dll 回环 **90/90** 通过（原 78 + 零发送释放 5 + 取消并发 2 + 先绑定后放弃 3 + 断开清理 2）；Codex 三项独立探针 `FIXED_ONBOUND_NO_TCP_FALLBACK`、`FIXED_DISCONNECT_NO_STALE_PEER`、`FIXED_ABANDONED_NO_SPLIT` 均为 True。正式插件 `Plugins/Net/Network.dll` 已替换为本轮新 DLL（旧字节备份于 `_temp/network-kcp-20261022/plugin-backup/`）。

## 八、下一步

1. 本轮正式 `Plugins/Net/Network.dll` 已替换；仍需在打包侧确认 HybridCLR/IL2CPP 主包配套，本轮回环与脚本编译不替代打包验证。
2. 在真实服务器进程与两个 Unity 客户端上验证移动、跳跃、六技能位移与打断，确认五类协议确实走 UDP。
3. 再做弱网与性能测量，决定是否需要不可靠位置通道；不可靠位置通道必须先完成序号、收尾可靠化与跨通道因果处理。

## 最终集成与验证边界（2026-10-22）

Codex最终四服务工程0错误，客户端289源默认新库/正式插件两种引用均0错误；本机真实Socket全90项与三项独立同步探针通过，最后退出码均0。夹具异步连接等待修正、曾失败证据和各执行轮次见根修改日志；本轮业务源17份去注释token与冻结一致。正式Network.dll最终SHA256=950846B969F8FF43B229A3769473FD18CEBFAC2B41650E0D8E043FD9A2F8ACF2，与最终编译产物一致。

正式网关TCP10140、UDP10141。运行需更新服务端与客户端同版网络程序集；打包端Network属于AOT配套，旧主包只更新热更GM_Game不足以带入新网络类型，正式HybridCLR/IL2CPP构建仍需验证。没有启停用户服务或Unity、发布或真实双号验收。下一步按既有七/八节运行清单验证移动/技能、MP扣除、自动重连及跨通道早到，当前未知实体消息沿原处理丢弃、不新增缓存。此边界不等于批准扩功能。
