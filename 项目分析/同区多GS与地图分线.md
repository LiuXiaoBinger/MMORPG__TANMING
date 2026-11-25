# 同一区服的 GameServer 和地图分线

玩家在选服页选的是逻辑区服。在公共地图中选择的是该地图的一条线。每条线在开放期间对应一份场景实例，由一台 GameServer 承载。同一台 GameServer 可以运行同一地图的多条线。切线不会改变角色 ID、所属区服或网关连接。

## 本机运行

1. 使用 Visual Studio 或 MSBuild 构建 `LYF_Server/LYF_Server.sln` 的 Debug 配置。
2. 启动现有的 CenterServer 和 LoginServer。本机示例使用中心服默认端口 10110。多个逻辑区可以共用账号登录和存档中心。
3. 确认数据库 `game_server` 中所选区的 `Id` 等于 XML 根节点的 `id`，`IpHost`、`Port` 指向玩家能访问的 **GateServer**。每个逻辑区一条选服记录。默认开发示例使用 127.0.0.1:10140。
4. 在项目根目录运行 `./LYF_Server/Start-Realm.ps1`。脚本读取 `LYF_Server/Config/RealmServers.xml`，依次启动 ControlServer、MasterServer、配置列出的各个 GameServer 和一个 GateServer。从另一台机器访问时，按实际网卡与客户端可达的网关地址修改配置和选服记录。
5. 登录后通过小地图下方的线路选择器查看线路人数、选择目标。独立副本期间隐藏选线。启动其他区服时指定对应 XML，同机部署需错开监听端口，共用物品 UID 空间的物理 GS 使用不同编号。

启动脚本适用于区服各进程在同一台开发机运行。分机器启动时，在各程序的输出目录中执行 `ControlServer.exe <区服XML>`、`MasterServer.exe <区服XML>`、`GameServer.exe <区服XML> <game编号>`、`GateServer.exe <区服XML>`。示例中 Gate、Master、Control 和 Center 共用根节点的 `host`，GS 可以有独立 `host`。旧开发启动方式仍可使用无参数 Gate、Master 和 GS 运行单节点。

## 配置与运行数据

| 配置 | 作用 |
|---|---|
| `realm.id` | 对应选服列表和角色表的 `ServerId` |
| `game.id / host / port` | 物理 GS 编号及其内网监听地址 |
| `scene.map / line / game / capacity` | 地图、玩家看到的线号、固定宿主、单线人数上限 |
| `expansion.warning / maxLines` | 本地图所有在线线路均达到警戒值时，向在线 GS 中承载人数较少的一台增加一条线，常驻线和弹性线总数受上限约束 |

ControlServer 在内存中维护已登记的 GS、每条线的场景实例编号、已占和预留名额。弹性线空闲且其他线有余量时回收。实例编号在当前调度进程内有效。MySQL 保留账号、逻辑区服、角色、背包和技能等既有记录；下次登录按存档地图重新选线。

## 服务配合

| 进程 | 本项目职责 |
|---|---|
| LoginServer | 账号入口、返回可选区服 |
| GateServer | 持有玩家 TCP/KCP 连接，根据当前角色归属将消息投递到对应 GS |
| ControlServer | 为进入或切换公共场景申请名额，返回目标 GS 和场景实例，接管确认后释放旧名额 |
| GameServer | 运行公共场景与副本、视野和战斗，切线时保存并交接角色 |
| MasterServer | 区内共享队伍与聊天，进副本前将分散在不同 GS 的队员集结到同一宿主 |
| CenterServer | 根据账号和逻辑区服选取角色，读写角色数据，确认持久化结果 |

跨 GS 切线时，网关先向控制服申请目标实例。原 GS 等待已有脏数据保存确认后交出角色、位置和冷却。目标 GS 接管并恢复视野，网关再提交新归属。队伍副本按同一实例号和宿主运行。RO 参考工程的 ControlServer 还初始化了登录控制、排队、踢号、角色管理和白名单；本次接入聚焦公共场景分线与区内多 GS 路由。
