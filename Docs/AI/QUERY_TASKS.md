# 查询索引：仅追加新的持久证据

## [INDEX][协作][QUERY-INDEX] 归档路由

- `项目修改日志/协作记录归档/2026-10-26/INDEX.md` 保留全部原查询标题/编号及准确快照行段。只按主题/编号搜索，不在启动时加载查询历史。历史结论保留原日期，并未针对当前条件重新验证。
- 追加前应用 `.agents/skills/task-log-policy/SKILL.md`。本次未仅为记录整理过程而新建查询记录，参见 CURRENT_TASK 和本任务修改日志。

## [QUERY][场景][RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027] 复制过来的柱子在编辑态为什么看不到烘焙光图

- 这是 `RO-MOSHENTA-MIN-ENV-20261027` 的只读后续查询，不是开发轮次，也没有业务改动。选中的柱子 `sc_dungeon_moshenta_building_002 (1)`，本地坐标 `(72.987, 79.1624, 589.11035)`，预制体实例 `150537529`／源预制体 GUID `e12c4a666ffc9f54dbb6f2ebc6fba78d`，与截图一致。
- 源绑定渲染器 `117451335` 与本项目副本渲染器 `150537531` 都是 index `1`，scaleOffset 约为 `(0.04050754, 0.04050754, 0.31588426, 0.7874021)`；两侧差异只是 float 序列化舍入。
- 主 Agent 独立对比了上述 YAML 记录与实际编辑器：`scaleInLightmap=0.1`、`Receive GI=Lightmaps`、`LightmapSettings.lightmaps=0`、`renderer.lightmapIndex=-1`、组件 `_applied=False`、`Cast Shadows=Off`、`Receive Shadows=False`、场景 `dirty=True`。所以旧烘焙绑定确实在组件数据里，只是当前没有注入编辑态的 `LightmapSettings`；**可编辑的烘焙设置**与**Baked Lightmap 结果显示**是两件不同的事。
- 源美术烘焙场景是同实例 `Cast Shadows=1`、`Receive Shadows=1` 且带 `LightingDataAsset`；源发布场景与本项目副本是同实例 `0/0` 且无 `LightingDataAsset`。截图那套设置与美术烘焙场景一致，但仅凭图片不能证明它当初打开的是哪条路径。
- 现组件只消费 RO 已烘好的数据；重新烘焙并把新的映射采集回来是另一条编辑器工作流，本项目未实现。组件只有显式右键菜单「应用环境配置」这一条入口；**不要**把「停用这个非 `ExecuteAlways` 组件」当作编辑态还原回调来推荐。
- 本次后续查询**没有**执行 Apply／保存／Play／切场景，因为场景里已有用户未保存改动。
- 主 Agent 实测为 **1 盏灯**，否掉了「当前场景 0 盏灯」的说法；也没有断言「持久预览必须靠落盘序列化」，没有断言「未被引用的 EXR 是历史残留」。
- 实际 `requestModelId` 为 `deepseek-v4.1-flash`，在原生会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0` 中独立观察到。原生传输在产物文件写完后断开，核心结论由实际文件加独立核验接受，**没有**成功的最终 RPC 回执。
- 未修改任何业务文件。证据：`_temp/ro-lightmap-inspector-query-codex-accept.json` 与 `_temp/ro-lightmap-inspector-query-review.md`；原报告中被否决的断言以验收记录为准。完整后续说明见本次环境任务修改日志。


## [QUERY][副本组队][DUNGEON-TEAM-20261029] RO 地图绑定与真实队伍 UI 来源

- RO 只读证据位于 F:/UnityPro/ro/Ro_cs422nb/config/Table/CSV/：ThemeDungeonTable.csv 第 9 行，theme 7 -> 剧情 4060 / 普通 4061 / 困难 4062 / TeamTargetID 2007；DungeonsTable.csv 第 404 行将 4060 映射至 SceneID 4060；SceneTable.csv 第 560 行将 4060 映射至 sc_dungeon_moshenta_001/sc_dungeon_moshenta_001，StartPos=132.97=58.89=60.95。本项目加载的场景为 LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/sc_dungeon_moshenta_001.unity。
- UI 来源位于 clientproj/Assets/artres/Resources/UI/Prefabs/：QuickTeamPanel 是 HUD，Team1 是主窗外框，TeamMember 是其内容。ThemeDungeonTeam 是进本确认窗口，不是建队 UI。复制的 16 个 ROTeam 资源、依赖和精确源/目标清单见完整修改日志中的引用。
- Unity 实际检查纠正了此前推断：内置 UGUI 程序集 GUID f70555f144d8491a825f0804e09c671c 即使 GUIDToAssetPath 为空，仍可解析 Image/Text。有效性以实际 Missing Script 数量为准，不能仅依据 Assets 范围内的 GUID 扫描。MoonClient 组件已剥离；一个旧 TeamInvite 标题组件确实缺失，现已替换。源 Team1 的内容初始为 inactive，移植的组合窗口需显式激活它。
- 调查与失败证据保留在 _temp/dungeon-team-20261029/research.md 和 research-addendum.md；最终事实与边界：项目修改日志/2026-10-29-副本组队与RO界面迁移_CHANGELOG.md。


## [QUERY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 RO 任务/队伍快捷面板归属返修查证

- 只读参考源 F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/Scripts/Lua/UI/Ctrl/QuickPanelCtrl.lua 第 32-34 行，将同一个 QuickPanel 内容父节点下的 QuickTaskPanel/QuickTeamPanel 绑定到 ToggleTask/ToggleTeam；第 44 行默认选中任务。它是共享页签区域，不是上下堆叠的两块面板。
- UI/Handler/QuickTeamPanelHandler.lua 第 35-37 行由 TeamCreate 直接调用 TeamMgr.RequestCreateTeam。迁移后的本地控制器现已沿用该入口顺序；独立的组队按钮仍打开正式组队窗口。
- 源 UI 预制体：clientproj/Assets/artres/Resources/UI/Prefabs/QuickPanel.prefab；源哈希及剥离 26 个 MoonClient 组件的计数见 _temp/dungeon-team-20261030/quick-panel-copy.json。IsOn/IsOff 上的内置 UGUI 子 Button 在自定义脚本剥离后仍会保留，不能让它们拦截新父级页签处理器。交互成立的依据是真实射线检查，单靠 GUID 扫描或直接 Invoke 不足以证明。


## [QUERY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 RO 组队接口、人数限制与实际网络路由

- 本次实现前，服务端没有队伍协议/管理器及副本实例分配；此前的单人队伍与 Moshenta 加载均为客户端本地流程。当前可见性/移动/技能广播使用 RoleManager 在线角色和 IsNearbyRole（地图加 100m），不使用 EntityWorld.BuildSnapshots。初始世界分区方案已撤回。
- RO ModuleMgr/TeamMgr.lua 中的接口为 RequestCreateTeam:43、InviteJoinTeam:81、AcceptTeamInvatation:165、LeaveTeam:315、BegInTeamByTeamId:413 和 SetCaptain:456。保留源码实际拼写 Invatation。ModuleData/TeamData.lua:63 设置 maxTeamNumber=5；此前按 UI 列数推断为 4 的结论错误。
- CSV DungeonsTable 中 4060 行：NumbersLimit=3=5、SceneID=4060、LevelLimit=75=200、EnterType=3、TimeLimit=2=7200。ThemeDungeonTable 中 theme7 -> StoryDungeonID=4060 / TeamTargetID=2007。本项目保留已明确授权的单人模式，人数上限为 5，不重新按 RO 最低 3 人加锁。Moshenta 出生点为 132.97_58.89_60.95；当前主城默认出生点为 62.1493416_19.4139996_80.6689758。
- 网络集成需要 proto 生成、NetDefine、Game 分发、Gate 上下行注册以及 GateClientResponseForwarder 通知解析，并确保服务端 bin 与客户端插件中的 Network.dll 匹配。仅增加 GateTeamCtrl 注册仍不能通过白名单。参考根目录 F:/UnityPro/ro/Ro_cs422nb 只读。完整过程：项目修改日志/2026-10-30-服务器权威多人组队副本_CHANGELOG.md。


## [QUERY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 服务链路、玩家 KCP 与网关通知约定

- Gate 注册与 GateClientResponseForwarder 解析是两个独立的集成点。新增的 team11210/invitation11211/entry11212/leave11213 现分别解析具体 protobuf 并返回 Succeed；默认拒绝与畸形载荷拒绝规则保持不变。主 Agent 正常登录验证已证明修复后的 11200->11210 可到达 UI。
- Network/Socket/com/Session.cs 中 TrySendRealtime 在玩家 KCP 之前明确检查 IsGameServerLink。ClientType.GameServer 的非空上游 NetClient 会为 Game->Gate 服务链路保持 TCP；上游为空且没有绑定 peer 的玩家 Session 则丢弃实时包。将前者错误建模为 Session(dict,null)，导致了误导性的测试失败和第 10 轮错误断言。修正后的真实抓包验证了实际 RoleManager 观察者过滤及目标 UnitySessionId。
- 服务器存活证据必须区分监听端口、已建立的 Gate->Game 连接，以及后来退出的旧子进程。Codex 最终在真实 bin/Debug 目录启动 Game85224 和 Gate102264，保留此前由主 Agent 启动的 Center54056/Login100464，确认上游连接后，正常登录/建队通过。未增加自动重连功能。完整证据与边界：项目修改日志/2026-10-30-服务器权威多人组队副本_CHANGELOG.md。


## [QUERY][场景][MOSHENTA-BUNDLE-LIGHTMAP-BINDING-20261031] 2026-10-31 已确认当前打包场景的光照贴图绑定存在

- 延续 DUNGEON-PLAYER-REGRESSION-20261031：用户报告原生 Editor 加载时显示 Baked Lightmap，而 bundle 加载时未显示。国内 WorkBuddy 使用 deepseek-v4.1-flash 进行只读调查，session 为 c8cc03df-f534-45e5-8b00-b9a10e6819d7，attempt 为 run-1de7d949-d94f-4db3-a99d-c1944ae94b13；未新增实现轮次，原开发轮数仍为 6。其最初采样的是 Scene_Login，不能证明 Moshenta 的状态。随后用户进入 Moshenta，由 Codex 执行现场验收探针。
- 实际运行的 DefaultPackage 为 HostPlayMode/HostPlayModeImpl，版本为 2026-10-31-997，存在一个匹配的 streamed scene bundle，活动场景为 sc_dungeon_moshenta_001。LightmapSettings 有 2 张非空的 1024x1024 光照贴图和 1919 个探针；唯一活动的 MEnvorimentSetting 报告 _applied=true，其默认光照贴图 SO 存在。预期的 937 个 Renderer 引用全部存活，937 个实际索引及 UV scaleOffset 均与配置值匹配，937 个引用均能解析到非空颜色贴图。1 个 Renderer 报告静态合批；仅此不能证明最终渲染画面正确。
- 截图中准确对应的网格 sc_dungeon_moshenta_ta_002_b 当前 index 为 0，scaleOffset 为 (0.2514144,0.2514144,0.2719581,0.6459816)，与截图一致。该对象是运行时场景对象，其材质没有 AssetDatabase 路径。采样当时，Editor 选中的是 DontDestroyOnLoad 中的 Role_MJS(Clone)，并非该网格；这仅是当时观察，不能证明用户此前选错了对象。
- 更正：OnEnable/ApplyLightmapData 日志不能证明 937 次赋值成功，本次新的现场逐项比对才提供此证据。场景 YAML 中的 stripped prefab 引用属于正常序列化，不能据此认定 bundle 引用损坏。已有光照贴图可以来自 Unity 烘焙，即使该导入的 RO 场景通过环境组件而非自身的 LightingDataAsset 应用贴图。若修改序列化场景数据，仍须更新对应 bundle；不能声称仅刷新 DLL 即可更新场景数据。材质 shaderKeywords 中缺少 LIGHTMAP_ON，不能据此认定 shader 变体被剥离。
- 当前 997 版本未复现绑定丢失。本次探针未解决最初报告的 Inspector 区段缺失、旧包状态、最终全黑或偏暗渲染，以及独立 Player 的表现；仍待用户澄清当前可见症状。本次未修改代码、资源或配置，未重新烘焙、编译、切换场景或构建。没有已证实的故障前，不应添加按路径查找 Renderer 的回退逻辑或修改 shader 设置。
- 证据：_temp/dungeon-player-regression-20261031/lightmap-binding/evidence/root-live-package-probe.txt、root-live-binding-comparison.txt、root-live-package-mode.txt；worker 的 investigation.md 为初步调查，其中与 root-review.md 及本条目冲突的内容以后两者更正为准。完整获批跟进记录：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。
