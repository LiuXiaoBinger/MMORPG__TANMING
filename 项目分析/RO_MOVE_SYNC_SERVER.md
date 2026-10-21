# RO 在线角色移动同步服务端对照

## 本轮范围与原入口

用户要求移动同步尽量沿用 RO 的接口、变量和正常流程，后续问题再扩展。本轮服务端只接位置上报和现有附近角色的移动表现通知，不改技能/F/CD、数据库、导航、反外挂或 AOI 系统。

参考入口：

- `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Entity/MPlayer.cs`：`UpdateMove()` 上报终点 `Dx/Dy/Dz`、朝向及 `Time`，清理本段耗时。
- `F:/UnityPro/ro/Ro_cs422nb/roserver/gameserver/protocol/action/ptcc2g_moveoperationreq.cpp`：`Process()` 从会话查 `Role`，把 `destination/time/moving_/face` 交给移动组件。
- `F:/UnityPro/ro/Ro_cs422nb/rogamelibs/entity/action/move_component.cpp`：`MoveComponent::OnMoveStart()` 更新朝向并进入位移处理，其他角色按场景同步接收表现。

本项目现正式世界入口维护的是 `OnlineRole` 与 `RoleManager` 的在线/会话双索引；`GameServer/SceneEntity/MoveComponent.cs` 属于未实际绑定世界的历史占位。本轮不把占位当作实现、不创建第二套实体世界。沿上次 `ROSkillComponent` 接入方式，新增全局 `ROMoveComponent : RoleComponentBase`，由 `OnlineRole.InitSerializes()` 实际装配。

## 当前实际调用链

`PlayerEntity.UpdateMove()` → `MoveOperationReq/11180` → `GateMoveCtrl.OnServerCommand()` → 现网络总队列/`ClientMessageManager` → `MoveOperationHandler.Handle()` → 原会话索引取得 `pRole` → `ROMoveComponent.OnMoveStart(action)` → 更新 `mainRoleInfo.BaseInfo.Pos` → `RoleManager.SyncMove()` → `SyncMoveNtf/11181` → `GateMoveCtrl.OnClientCommand()`/原网关转发器 → 客户端现角色索引/`onSyncMoveEvent`。

服务端保留 RO `OnMoveStart`、`IsMoving()`、`is_moving_` 和入参 `action`。当前角色基础位置采用既有 `x_y_z` 文本格式，使进入世界、技能附近广播和移动通知读取同一个位置。广播复用已有同图 100 米范围和在线索引，仅向其他在线角色发送，不让本地移动等待确认。

## 协议最小适配

| RO 数据语义 | 本项目请求/通知 | 单位和来源 |
|---|---|---|
| destination | Dx/Dy/Dz | 世界坐标，米，本地碰撞完成后的终点 |
| face | Face | Y 轴朝向，度 |
| time | Time | 本段累计秒耗时；客户端 CostTime 每帧按 deltaMilliseconds × 0.001 以秒累积，发送时直接赋给 Time |
| moving_ | IsMoving | 本段末帧主动移动状态 |
| 客户端 IsMoved | IsMoved | 本段位置变化标志，远端动作接入 |
| 服务器场景对象身份 | RoleId（仅通知） | 服务器填持久角色 ID，客户端用现有远端角色索引 |

本项目没有 RO 原 `Common` 位打包的场景/朝向契约，使用强类型独立字段；不移植车辆/传送/场景高度/导航等缺失系统，也不添加等待、代次、重试或管理层。`IsMoved` 在 RO 客户端原代码为移动组件字段，原上行位打包注释未启用；本项目为当前远端动画最小接入采用显式 bool，不代表新增玩法。

两 `.proto` 先追加，再用现 `LYF_Server/ProtoBuf/bin/protoc.exe` 生成原 `Network/Proto` 两文件。生成代码保持工具输出，协议语义注释在 `.proto`。原 51 消息类整段代码与本轮备份相等；反射描述符仅按新增消息自然变化。

## 验证与边界

证据统一 `_temp/ro-move-city-20261012/server/`。GameServer及引用 Rebuild 0 错误/11 既有警告，GateServer及引用 0 错误/4 既有警告，Network及引用 0 错误/4 既有警告。全部中间和输出使用 `_temp`，未部署、重启正式服务。

`MoveSyncSmoke.cs` 在独立进程使用真实正式程序集、角色容器、协议注册、消息队列和本机临时 TCP 连接，通过 22 项断言：上行会话和字段无损、消费前不写位置、逻辑队列调用真实组件、现位置快照更新、附近通知身份/世界终点/朝向/秒耗时、本人/远处/异图/离线排除、网关真实下行透传、停止和跳跃高度、不存在会话保持 RO 正常返回。反射只在测试边界绑定临时已连接 Socket，不替换业务处理器或网络封包。

未验证正式三服务与 Unity 两客户端联机、画面插值、实际墙体/安装包/Bundle；回环帧实测不称正式联机验收。现服务端没有 RO 地面高度/导航位移结算，本次终点来自客户端现碰撞链，不宣称反外挂或服务器物理权威实现。按用户范围保持现有附近广播，不扩展动态 AOI 加入/离开。

DLL 说明：客户端 `Plugins/Net/Network.dll` 与独立 `network-bin/Network.dll` 字节一致；Game/Gate 的各输出目录编译使用相同源文件，调试路径影响程序集哈希，不称三份产物字节相等。源码/工程输入清单 `compiled-source-hashes.json` 共 246 个去重项；末稿清单 `final-manifest.json` 共 16 个正式路径。正式既有修改完整备份于 `server/before/`。

修改日志：`项目修改日志/2026-10-12-RO-MOVE-SYNC-SERVER-CHANGELOG.md`。本轮四协作入口由主 Agent 统一更新。
