# 开源 MMORPG 项目源码解析（7 个项目 · 图文详解）

> 本目录对 7 个有代表性的开源 MMORPG / 游戏服务器项目做**超级详细、图文并茂**的源码级解析，作为 LYFMMORGP（Unity 客户端 + .NET 四服）的架构参考、技术选型对比与面试复盘资料。
>
> 解析日期：2026-10-15　｜　源码位置：`F:\MMORPG_OpenSource\`（只读，未做任何修改）
> 解析方式：**静态源码走读 + 官方文档核对**，未实际编译/运行，每篇文档均在显著位置标注。
> 质量流程：每篇文档都经过 **写作 → 未参与写作的独立复查员按 7 个维度复查 → 原作者按遗漏清单补漏 → 最终全量校验**。

---

## 一、7 个项目导航

| 项目 | 形态/一句话定位 | 技术栈 | 许可证 | 主解析文档 |
|---|---|---|---|---|
| **ET** | 国内最流行的 Unity+C# 双端分布式框架（本版为 ET10「昭君」，AI Native） | Unity + C# / .NET 8 / Fiber Actor / KCP / MemoryPack / MongoDB | ET License（商用上线 4999 元，禁止改后开源） | [ET-源码解析.md](ET/ET-源码解析.md) |
| **FishMMO** | Unity 一体化 MMORPG（客户端/游戏服/Web 账号/更新器/数据库多模块） | Unity 6 + FishNet / .NET 8 / PostgreSQL+EF Core（Redis 预留） | MIT | [FishMMO-源码解析.md](FishMMO/FishMMO-源码解析.md) |
| **UnityMMO** | 个人从零做的 3D MMO，Lua 热更 + skynet 后端的经典样本 | Unity + XLua（前端）/ skynet（C+Lua 后端）/ sproto / MySQL | MIT | [UnityMMO-源码解析.md](UnityMMO/UnityMMO-源码解析.md) |
| **NoahGameFrame** | 老牌 C++ 插件化、数据驱动游戏服务器框架 | C++ / libevent / protobuf / Redis+MySQL / 单宿主多进程 | Apache-2.0 | [NoahGameFrame-源码解析.md](NoahGameFrame/NoahGameFrame-源码解析.md) |
| **OpenCoreMMO** | 现代 .NET 复刻 Tibia 8.60 的开源 MMORPG 服务端 | C# .NET 10 / 双 TCP 端口 / EF Core(InMemory/SQLite/PostgreSQL) / Lua | GPL-3.0 | [OpenCoreMMO-源码解析.md](OpenCoreMMO/OpenCoreMMO-源码解析.md) |
| **ServUO** | Ultima Online 大型多人在线 RPG 服务端模拟器（RunUO 精神延续） | C# / .NET Framework 4.8 / 固定二进制 Packet / 世界文件序列化 | GPL-2.0 | [ServUO-源码解析.md](ServUO/ServUO-源码解析.md) |
| **Intersect-Engine** | 免编程的 2D ORPG 完整开发套件（客户端+服务端+编辑器+单机） | C# / .NET 8 / MonoGame / LiteNetLib / EFCore SQLite/MySQL / 多协议 | 客户端 MIT + 服务端/编辑器 GPL-3.0 | [Intersect-Engine-源码解析.md](Intersect-Engine/Intersect-Engine-源码解析.md) |

> 每个项目文件夹下另有 `assets/` 存放架构图/拓扑图/流程图 PNG；文档内还包含多张 Mermaid 图（在支持 Mermaid 的 Markdown 查看器中直接渲染）。

---

## 二、按技术形态分组（按你的关注点选择阅读）

- **A. Unity 双端、C# 全栈（与 LYFMMORGP 最接近）**
  - [ET](ET/ET-源码解析.md)：一套 C# 双端共享、Fiber 单线程 Actor、分布式多进程、状态同步（含帧同步 + 预测回滚 demo）。**最优先精读**。
  - [FishMMO](FishMMO/FishMMO-源码解析.md)：同工程 `UNITY_SERVER` 宏切服务端，FishNet 网络、数据库心跳做服务发现、RSA+SRP 登录、ECA 数据驱动技能。
- **B. Lua 热更 + 轻量 actor 后端（想看热更与 AOI/移动）**
  - [UnityMMO](UnityMMO/UnityMMO-源码解析.md)：XLua 业务 + skynet 服务，三维十字链表 AOI（50m 进/60m 出滞后带）、状态同步。
- **C. 纯 C++ 服务器框架（想看插件化、数据驱动、进程拓扑）**
  - [NoahGameFrame](NoahGameFrame/NoahGameFrame-源码解析.md)：单宿主 + 插件组合成多进程、Property/Record 数据驱动、属性广播开关。
- **D. 复古/经典服务端（想看固定包、序列化、脚本化、网格同步）**
  - [OpenCoreMMO](OpenCoreMMO/OpenCoreMMO-源码解析.md)：现代 .NET 单线程事件队列、Tibia 走格子同步、Sector 视野。
  - [ServUO](ServUO/ServUO-源码解析.md)：世界对象序列化、Timer 队列、固定二进制 Packet、Gump 服务端 UI、GM 命令、海量脚本。
- **E. 完整 2D 引擎（想看分层、配置驱动、可视化事件、编辑器）**
  - [Intersect-Engine](Intersect-Engine/Intersect-Engine-源码解析.md)：Core/Network/Client/Server/Editor 分层、EF Core 存储、可视化事件系统、单机/联机同构。

---

## 三、横向对比总表（关键工程维度）

> 下表数字/结论均来自各项目实际源码，细节与证据见对应文档；均为静态走读，未编译运行。

| 维度 | ET | FishMMO | UnityMMO | NoahGameFrame | OpenCoreMMO | ServUO | Intersect-Engine |
|---|---|---|---|---|---|---|---|
| 主要语言 | C# 双端 | C# | C# + Lua（前端业务 Lua） | C++（少量 Lua） | C#（少量 Lua） | C# | C# |
| 客户端 | Unity 2022.3 | Unity 6000.2 | Unity（XLua） | 独立 NFUnitySDK（Unity，另仓库） | 外部 OTClient（Tibia 8.60） | 经典 UO 客户端 | 自研（MonoGame） |
| 服务端形态 | .NET 8 多进程/多纤程 | 同 Unity 工程 Dedicated Server | skynet 多 actor | 单宿主+插件组合多进程 | 单进程 Standalone | 单进程（登录/游戏一体） | Server，可被 SinglePlayer 内嵌 |
| 传输层 | 纯 C# KCP（可切 TCP/WS/UDP） | FishNet：Tugboat TCP / Multipass UDP / Bayou WS | TCP（登录 8001/游戏 8888） | libevent（TCP/WS/UDP，TCP 15001 等） | 双 TCP：7171 登录 / 7172 游戏 | TCP 2593 固定二进制 Packet（0x100 注册表） | LiteNetLib UDP（可靠/不可靠） |
| 序列化 | MemoryPack | FishNet 自研 CodeGen（非 protobuf） | sproto（非 protobuf） | protobuf（proto3）+ 包一层 MsgBase | Tibia 兼容二进制包 | 自研二进制 Packet | MessagePack（包）+ JSON（配置，Newtonsoft） |
| 数据存储 | MongoDB（BSON 直存） | PostgreSQL（EF Core）；Redis 预留无业务调用 | MySQL 双库（无 Redis），密码明文 | Redis（在线）+ MySQL（持久化，DBServer） | EF Core：InMemory/SQLite/PostgreSQL | 无数据库；内存全量 + 每 15 分钟三段式 Saves(.bin/.idx/.tdb) | EF Core 8：SQLite/MySQL 双 Provider（schema 迁移约 394 个） |
| 同步模型 | 状态同步（另含帧同步 demo） | 状态同步 + FishNet 预测/回滚 | 状态同步，服务端半权威（不校验坐标） | 状态同步，20Hz 按 group 广播（无真实 AOI） | 走格子权威，Sector 16×16 视野 | 走格子 + 客户端预测 + 失败回滚 | 2D 网格权威（状态/坐标/冷却/碰撞四重校验） |
| 视野/AOI | AOI 组件 + 状态同步 | FishNet NetworkObserver（无自研 AOI） | 三维十字链表，50m 进/60m 出 | 整个 group（副本/分线）互见 | 16×16 Sector 链表 + 11×11 视野 | Sector 分块差量，视野 18 格 | 2D 地图分块可见性 |
| 并发模型 | Fiber 单线程 Actor | Unity 主循环 + 服务端自定 tick | skynet actor，10/50/100ms 定时环路 | 单线程 1ms 循环 + 线程池处理慢任务 | 单消费者 Channel（Dispatcher） | Core 主线程 + TimerThread 8 级时间桶 + IO 后台线程 | 单线程调度 + SmartThreadPool 并发更新地图 |
| 配置表 | Luban（Excel→C#） | Addressables/SO/数据库 | sproto + 配置文件 | XML（Struct 类模板 / Ini 数据） | JSON + Lua（怪物/NPC/物品） | C# 脚本（编译为 Scripts.dll）+ Spawns XML | JSON 内容工程 + 可视化编辑器 |
| 热更/资源 | HybridCLR + YooAsset | Addressables；Patcher 增量补丁（无代码热更） | XLua + AssetBundle | —（服务端框架） | — | 无（改脚本需重启） | 编辑器产出内容 |
| 代码规模（本地） | 约 4261 .cs | 约 1417 .cs | 227 .cs + 396 Lua + 29 C/C++ | 约 571 C/C++ | 约 1593 .cs | 约 6323 .cs（多为 Scripts） | 约 2337 .cs |
| 许可证 | 商业 ET License | MIT | MIT | Apache-2.0 | GPL-3.0 | GPL-2.0 | MIT + GPL-3.0（分项目） |

---

## 四、对 LYFMMORGP 的借鉴价值矩阵（按模块）

| 本项目模块/问题 | 最值得参考的项目 | 借鉴点（详见各文档第 23 节） |
|---|---|---|
| 整体架构 / 四服分布式 | **ET**、NoahGameFrame | Fiber Actor 与多进程职责；单宿主插件化多进程与 Master/Proxy 拓扑 |
| 网关 Gate / 转发 | **NoahGameFrame**、FishMMO | Proxy 只转发不懂业务（兜底 OnOtherMessage，改 player_id 转发 Game）；World 兼路由 |
| 账号登录 / 鉴权 | **FishMMO**、OpenCoreMMO | RSA 握手 + SRP-6a（零密码泄露）；登录短连 7171 / 游戏长连 7172 分离 |
| 服务发现 / 心跳 | **FishMMO**、ET | 数据库表心跳做极简服务发现（无 Consul）；ET 进程/Zone 配置 |
| 移动同步 / AOI | **UnityMMO**、ET、OpenCoreMMO | 进/出滞后带防抖的十字链表 AOI；状态同步 + 预测回滚；Sector 视野批量广播 |
| 网络包 / 序列化 | **ServUO**、OpenCoreMMO、NF | 固定包/Handler 注册与 SendQueue；Packet/Handler 注册；6 字节包头 |
| 主线程逻辑 / 帧 | **OpenCoreMMO**、NF、UnityMMO | 单消费者队列无锁串行；对照本项目 20Hz 固定帧 |
| 组件 / 实体模型 | **ET**、NF、ServUO | ECS 组件/事件系统；Property/Record 数据驱动 + 广播开关；Item/Mobile 继承体系 |
| 技能 / 战斗 | **FishMMO**、OpenCoreMMO、Intersect | ECA（事件/条件/动作）数据驱动技能；Spell 结算；Intersect 战斗结算 |
| 怪物 AI | **FishMMO**、OpenCoreMMO、UnityMMO | NavMeshAgent+FSM；JSON 数据驱动 + C# 状态机 + A*；Blueprint FSM |
| 背包 / 装备 / 掉落 | **ServUO**、OpenCoreMMO、Intersect | Container/Layer/Loot 表；物品容器；装备/资源系统 |
| 数据持久化 | **ServUO**、NF、Intersect | 世界序列化/自动存档 + 双构造；字段 Save 开关；EF Core 多 Provider 与迁移 |
| 配置表 / 数据驱动 | **Intersect**、NF、ET | JSON 内容工程；XML 类模板 + 字段开关；Luban（ET 与本项目一致） |
| 编辑器 / 可视化内容 | **Intersect** | 地图/事件/任务/技能可视化编辑器 + 事件命令 |
| 服务端 UI / GM 工具 | **ServUO** | Gump 服务端下发 UI、GM 命令、Spawner 刷怪 |
| 客户端热更 / 资源 | **UnityMMO**、ET | XLua/AB 与 HybridCLR/YooAsset（ET 与本项目方案一致） |

---

## 五、许可证与商用合规（务必先看）

| 许可证 | 项目 | 能否直接抄代码商用 | 关键约束 |
|---|---|---|---|
| MIT | FishMMO、UnityMMO | 可（保留版权声明即可） | 最宽松 |
| Apache-2.0 | NoahGameFrame | 可 | 需保留 NOTICE/许可证，含专利授权条款 |
| 混合 MIT | Intersect：Core/Network/Client/Client.Core/Client.Framework/Server.Framework | 可（这些程序集） | 客户端侧宽松 |
| GPL-3.0 | OpenCoreMMO；Intersect 的 Server/Editor/Building | 衍生作品需开源；Intersect 服务端不对外分发时风险较低，改服务端卖给客户需向客户披露 | 强 Copyleft |
| GPL-2.0 | ServUO | 衍生作品需开源 | 强 Copyleft；UO 客户端资源版权在 EA |
| 商业 ET License | ET | **不能直接抄代码上线**；需向广州一踢互联网科技付费授权（4999 元） | 禁止改后开源；Odin 等第三方商业插件需另购 |

> 建议：**设计思路、架构模式、协议时序、数据结构可自由借鉴**；直接复制源码则受上述许可证约束。本项目优先采用 MIT/Apache 与“只借鉴思路”的方式；ET 与 GPL 项目若要直接搬代码，需先走授权/法务评估。

---

## 六、建议学习/面试路线

1. **先建立整体观**：读本 README → ET 第 1/7/12/13 章（架构、启动、登录）。
2. **再打通网络与同步主线**：ET/UnityMMO 的登录时序与移动同步 → NF Proxy 转发 → OpenCoreMMO/ServUO 固定包对比。
3. **按业务模块横向对比**：战斗技能（FishMMO/OpenCoreMMO/Intersect）、AI（FishMMO/UnityMMO/OpenCoreMMO）、背包（ServUO/OpenCoreMMO）、存储（ServUO/NF/Intersect）。
4. **沉淀到本项目**：每篇文档第 23 节“可借鉴到 LYFMMORGP”逐条对照本项目 Center/Login/Gate/Game、20Hz 固定帧、AOI、技能/移动同步、技能编辑器。
5. **面试复盘**：每篇第 25 节给出 30 秒/2 分钟话术与高频追问；结合本项目 `项目分析/项目难点与亮点-面试精简版-2026-10-15.md`。

---

## 七、目录结构

```
开源MMORPG项目解析/
├── README.md                  # 本总索引（横向对比 + 选型 + 合规 + 学习路线 + 统计）
├── ET/
│   ├── ET-源码解析.md
│   └── assets/                # et-architecture-01.png / et-fiber-02.png
├── FishMMO/
│   ├── FishMMO-源码解析.md
│   └── assets/
├── UnityMMO/
│   ├── UnityMMO-源码解析.md
│   └── assets/
├── NoahGameFrame/
│   ├── NoahGameFrame-源码解析.md
│   └── assets/
├── OpenCoreMMO/
│   ├── OpenCoreMMO-源码解析.md
│   └── assets/
├── ServUO/
│   ├── ServUO-源码解析.md
│   └── assets/
└── Intersect-Engine/
    ├── Intersect-Engine-源码解析.md
    └── assets/
```

> 文档内的源码路径均相对各项目根目录（即 `F:\MMORPG_OpenSource\<项目名>\`）。中间扫描数据、绘图脚本、复查与补漏记录在 `D:\unitypro\LYFMMORGP\_temp\opensource-mmo-analysis-20261015\`（不随正式解析交付）。

---

## 八、文档统计、初版遗漏与补漏说明

每篇文档在初稿完成后，都由**未参与写作的独立复查员**对照源码二次核查（7 个维度：目录覆盖、关键系统、构建步骤、图表、代码路径真实性、结论证据、借鉴点与面试问答），再由原作者按遗漏清单补漏，最后复跑链接/图片/代码围栏/Mermaid/源码路径校验。最终统计如下：

| 项目 | 最终行数 | Mermaid 图 | PNG | 不重复源码引用 | 初版发现的主要遗漏 → 补充内容 |
|---|---|---|---|---|---|
| **ET** | 1758 | 9 | 2 | 78 | 尾部“假结尾”拼接事故（删除假 §24/25/26 与 8 个凑数空小节）；并回断裂的 §23 借鉴表；包数“61 个 cn.etetet”纠正为“Packages 共 61 目录、其中自研 cn.etetet.* 56 个”；Entity 序列化注解纠正为 `[MemoryPackable(NoGenerate)]+[BsonIgnore]`；补 truesync 定点数学库；补 YooAsset/MemoryPack/HybridCLR 版本 |
| **FishMMO** | 991 | 8 | 2 | 172 | 怪物 AI 由“未确认”补为 NavMeshAgent + 9 状态 FSM（仇恨扫描/脱战 Warp 回 Home）；Redis 核实为“工厂已接线但全仓库无业务调用”；伤害补为 ScriptableObject 固定 DamageAmount + 减算；补任务三层模型与对话树；修正 appsettings.json 拼写、Patcher 增量补丁 |
| **UnityMMO** | 1187 | 9 | 3 | 82 | msgagent 预创建池确认为 10（gated.lua:99-102）；tag 由“300~399/400~499”改为 msgagent 层 300~499 合并路由；AOI 5000/6000 半径澄清为 RoleMgr.lua:107 硬编码而非配置表 |
| **NoahGameFrame** | 1039 | 10 | 2 | 77 | 删除两处全仓库不存在的 NFIPluginModule.h 引用；json.lua 由 Dependencies 改归 _Out 子模块路径；补一张 Mermaid 目录结构图；重排图编号；script 文件名改为带进程前缀；Redis 6379 补硬编码兜底证据 |
| **OpenCoreMMO** | 833 | 8 | 2 | 52 | DI 容器由 Autofac 纠正为 Microsoft ServiceCollection（props 声明 Autofac 包但未启用）；模块数 16→15；Dispatcher 的 30s CTS 核实为“创建了但未传 token 给 Action，真实防卡靠 try/catch”；补 Condition 系统与 EF Core 三 Provider |
| **ServUO** | 1471 | 10 | 2 | 66 | 包表错误：0x0A 由 EquipReq 改为 Edit、EquipReq 改到 0x13；0x73 PingReq 的 ingame 改 false；Mailbox 路径改正；规模更新为 Server/ 143 个 .cs、Item.cs 6477/Mobile.cs 12821 行；补 Party 组队、Targeting 目标选择系统；Scripts 子目录补全到 17 个；借鉴表去重 |
| **Intersect-Engine** | 1220 | 9 | 2 | 63 | 聊天频道表重写（Local/Party/Global/PM/Admin/Guild）；PacketHandler 确认为服务端 3202 行/67 方法；RSA 改为网络 RSA-2048+AesGcm（4096 仅 Web JWT）；迁移分 schema 约 394 个（Sqlite 196 + MySql 198，按 Game/Player/Logging 三 DbContext）与数据迁移 5 个两类；删约 100 行注水克隆段；补目录结构图、战斗结算、地图并发更新 |

> **引用缺失口径**：最终校验仍报“缺失”的仅 8 个引用，全部是文档已说明的非仓库文件——
> 运行时生成文件：FishMMO `logging.json`、`appsetting.json`（文档已注明真实名为 appsettings.json、并引用 README 自身的错误拼写）、Intersect `client/update.json`、`version.client.json`（IntersectGame.cs 里的运行时字符串）；
> 未拉取子模块/vcpkg 依赖：UnityMMO `Server/skynet/lualib/skynet.lua`、`config_scene_1001/2001.lua`，NF `json.lua`、`event2/bufferevent.h`。
> 无任何凭空捏造的源码路径。

---

## 九、未验证声明（诚实标注）

- **7 个项目全部为静态源码走读 + 官方文档核对，未实际编译或运行**：
  - ET：本机缺 Unity 2022.3.62 与 .NET 8、MongoDB；
  - FishMMO：缺 Unity 6、PostgreSQL、FishNet 运行环境；
  - UnityMMO：`Server/skynet`、`lsocket` 子模块未拉取，缺 Unity/skynet/MySQL；
  - NoahGameFrame：Dependencies 子模块未拉全、vcpkg 三方库未装，缺 C++ 编译环境；
  - OpenCoreMMO：缺 .NET 10 与 OTClient；
  - ServUO：缺 .NET Framework 4.8 运行环境与 UO 客户端/资源；
  - Intersect-Engine：缺 .NET 8 编译运行环境。
- 所有运行时行为（端口连通、登录走通、移动表现、存档落盘等）均为基于源码的静态推演，不得当作已验证运行结果。
