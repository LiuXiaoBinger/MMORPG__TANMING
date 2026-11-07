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

## [QUERY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R7] 2026-10-31 缺的是 RO 运行时 LOD 选择，不是渲染器绑定

- 用户澄清打包模式渲染仍全黑。既有 937/937 绑定匹配只作为证据保留，不等同于渲染正确。bundle 与 AssetDatabase 版本、以及 LOD600/599/400/300 的对照，把主要渲染故障定位到被选中的 LOD600 路径。shader 注释把 600 标为编辑/系统光照；RO 运行时画质取值为 100/200/300/400/500，不含 600，桌面默认 500。本项目缺少 Shader.globalMaximumLOD 赋值，一直停留在 int.MaxValue。
- 参考路径：clientcode/CSProject/MoonClient/Utility/QualityGrade/MQualityGrade.cs:78-80；clientcode/CSProject/MoonSerializable/Utility/QualityGrade/MQualityGradeConditionChecker.cs:114-121；clientproj/Assets/artres/Resources/QualityConfig/MQualityParamsToAndroid.asset。MGame 初始化与 MScene 场景加载入口会应用该画质设置。本项目的定向适配落在既有 SceneMgr 加载成功入口，不引入 RO 的完整画质体系。
- 已纠正的说法：QualitySettings.maximumLODLevel 影响的是 Mesh LODGroup，不是 shader 的 SubShader LOD；shader 属性名与 Pass 元数据一致不能证明源码版本完全相同；材质关键字缺少 LIGHTMAP_ON 与现有对照都不能直接枚举被剥离的 bundle 变体，剥离仍属推断。剩余暗区同样未证明是未烘焙几何。不要基于上述说法重启 AlwaysIncludedShaders/SVC 重建、重新烘焙、按路径查渲染器的回退或方向光绕行。
- 源码修复 876b4c8 已推送；当前内存内 globalLOD 500 可恢复采样地面。包内 DLL 刷新与重新进场、Player、其他场景与剩余暗区仍未验证。完整证据：_temp/dungeon-player-regression-20261031/lightmap-binding/shading/investigation-supplement.md；_temp/dungeon-lighting-resume-20261031/root-runtime-verification.txt。完整日志：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。更早的待定/暂缓条目仍属历史；本次窄范围跟进不代表组队/副本整体功能已验收。


## [QUERY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R8-COLLECT] 2026-10-31 收集未覆盖烘焙运行条件，现有Shader包未变化

- 按用户要求由Codex直接检查，未使用子Agent或WorkBuddy。七个魔神塔收集球的lightmapIndex均为-1、探针为BlendProbes，收集场景光图数量为0。globalMaximumLOD=2147483647选中SubShader0/LOD600（AllInOne为3个Pass，Terrain为4个）；运行时500则选中LOD400。YooAsset 1.5.7的ShaderVariantCollector.cs只创建默认临时场景与普通材质球，没有设置运行时LOD或绑定光图，因此这套收集未覆盖实际烘焙运行条件。
- 已保存SVC含84个Shader/186个变体。材质实际引用的First副本AllInOne GUID515a03f53d5fd384ea44e8267b4c8920只有两条ForwardBase（passType4）：DIRECTIONAL LIGHTPROBE_SH SHADOWS_SCREEN，以及追加_ALPHAMODE_CUTOUT的组合；均无LIGHTMAP_ON。Terrain GUID227df6dc39a2e0c468c35ff2e6259714只有一条类似ForwardBase和两条ShadowCaster（passType8）。现场SVC.Contains(ForwardBase LIGHTMAP_ON)对AllInOne返回false。这证明收集缺口，不能单凭此断定最终包必然裁掉该变体。
- 纠正worker初步报告中“SVC未参与构建”的判断：Assets/AssetBundleCollectorSetting.asset收集Assets/GM_Game/Shaders；2026-10-31-1221的BuildReport明确将Assets/GM_Game/Shaders/MyShaderVariants.shadervariants（GUID7643cfdd27040854d99247a7f9a71777）列入unityshaders.bundle，输出bundle manifest也列出了它。没有C#运行时引用不能排除它是构建输入。
- 版本2026-10-31-997和2026-10-31-1221记录着相同Shader包：FileHash e947160e072ada0ef09d4d259ca3ddd0、1986839字节。当前OutputCache的SHA256为fd6da1d88e21592d9bb2f93731fcc477f8292151088c4a7696dfa5ab0f6e8df4，与此前样本相同；本地mtime为14:33:06，而当前SVC保存于20:43:39。最新收集结果尚未进入这个现有包；这不等于已经确定用户早先Player加载了哪个版本。
- 实际球体材质的Shader和必要主纹理/地形纹理均可解析，Shader支持为true、ShaderHasError为false。Terrain的_MainTex为空是源码声明的隐藏Never Used属性，Control和四张Splat贴图齐全。sc_moshenta_001_a02使用RO/Creature/Monster，其GUID与RO参考原材质相同，不判为意外替换。
- 临时合成光图实验未获得源Shader正确响应光图的阳性对照：源/包内Shader的lightmap-on结果均为0，Unlit可见性对照meanLum为0.131919，不能据此证明包内变体被裁。新增叠加场景被未保存的Untitled场景阻止，未保存该场景。临时对象和bundle均已移除，LOD、光图与测试过的着色全局量已还原；场景数1到1，七个收集球保留。未改正式代码/资源/配置，未构建、重烘焙或实施运行时修复。黑墙根因/修复、新包渲染、重新进场和Player仍未验证。证据：_temp/dungeon-lighting-resume-20261031/collection-inspection/inspection-evidence.json及其渲染子目录。完整日志：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。

## [QUERY][副本编辑器][RO-DUNGEON-EDITOR-20261102] RO 副本编辑器入口与数据链路，2026-11-02

- 授权：开发者已明确批准本次分析及双语查询入口。仅做源码/XML 静态核对；未启动 RO 编辑器、未编译、未保存、未导出、未运行游戏。
- 来源根目录：`F:/UnityPro/ro/Ro_cs422nb`。Unity 菜单：`clientproj/Assets/artres/Editor/TreeEditor/TreeEditor.cs:42` 中的 `TreeEditor/OpenSceneEditor`；`clientproj/Assets/artres/Editor/SceneEditor/SceneEditor.cs:587` 中的 `ROTools/OpenSceneEditor`。`TreeEditor.cs:51` 的 `TreeEditor/OpenSkillEditor` 是独立的技能页面。
- 编辑模块：`clientproj/Assets/artres/Editor/SceneEditor/` 包含波次、怪物/随机/继承生成、NPC/采集物/墙体节点、触发区域/分组、场景 AI 事件及巡逻路径。`NodeEditor/SceneWave/MSceneWaveNode.cs:503` 起提供事件前置条件与延迟创建波次的配置。已检查菜单和源码，未在 Unity 中实际操作这些功能。
- 魔神塔数据：`config/Assets/Resources/SceneDatas/RO_DaNaTuoSiZhiZhan_EZ.txt`、`RO_DaNaTuoSiZhiZhan_H.txt`、`RO_DaNaTuoSiZhiZhan_HH.txt` 是 XML `MSceneEditorConfig` 文件，包含 `data`、`dataEx`、`canvas`。每份均有 63 个 `MSceneWaveData` 元素及 35 个 `MSceneTriggerObjectData` 元素；这些是配置记录数，不是怪物总数，也不代表必然有 63 波纯战斗。
- 保存/导出：源码中 `clientproj/Assets/artres/Editor/SceneEditor/SceneMgr.cs:845` 先检查数据，在满足 `SceneDatas/` 路径条件时，将 `serverData` 导出为 `PbLocal.MSSceneData`，将 `clientData` 导出为 `PbLocal.MCSceneData`，之后保存编辑器 XML。客户端字节数据使用 `SceneDatasForClient`。
- 依赖与边界：需要 Odin/Sirenix、`NodeEditorFramework`、`MoonClient`、`MoonCommonLib`、`PbLocal.Xml`、表格及配置资源。该编辑器尚未移植到 `ROCharacterLightingDemo`；此前的场景/模型/动作/特效迁移未包含它。本次不声称已通过运行时验收。
- 完整分析：`项目分析/RO副本编辑器入口与数据链路-2026-11-02.md`。仅同步/检查本编号的中文段，保留全部既有记录。


## [QUERY][技能][互斥][RO-CLIENT-CAST-CHECK-20261102] RO 客户端/服务端施法校验链路

- 授权：开发者已批准的查询记录。仅对 `F:/UnityPro/ro/Ro_cs422nb` 做只读核对；未构建 RO、未启动编辑器、未做联网测试。
- 客户端 `clientcode/CSProject/MoonClient/Skill/MNavigationComponent.cs:307` 是到达目标后的施法回调入口；它在 `Entity.Skill.CanSkill(...)`（同文件 344 行）之前先调用 `StateExclusionManager.singleton.GetIsCanChangeState(MExlusionStates.State_AJ_Singing, false)`。判定失败直接返回，不发送 `RpcC2G_SingingRequest`；发送成功（`MNetClient.singleton.Send(info)`，同文件 437 行）后立即进入本地吟唱/技能预表现，不等待服务端权威确认。
- `clientcode/CSProject/MoonClient/StateExclusion/StateExclusionManager.cs:346` 的 `GetIsCanChangeState` 每次重算玩家当前大状态列表，再读 `StateExclusionTable.RowData.Concatenate[targetState]`。第二个参数是 `showErrorTips`，所以传 `false` 只表示不弹提示，不是关闭校验。真正开关是 `StateExclusionClientCheck`，本次核对所见快照里 `config/Table/CSV/GlobalTableSystem.csv:579` 配置为 `1`（该行含义为“是否开启前端检测(0关闭1开启)”）。矩阵值 2 在正常客户端调用下放行，退旧状态与最终结果仍由服务端处理。
- 客户端不只这一处判定：`clientcode/CSProject/MoonClient/Skill/MSkillComponent.cs:272` 的 `CanSkill` 另外校验动作许可（面向吟唱、搬运/死亡、技能互断等），并继续覆盖场景、冷却、武器/QTE、目标、Buff 限制、消耗与脚本确认；导航侧 `MNavigationComponent.cs:573` 一带还有距离处理。因此“客户端从不做距离校验”的说法不准确。
- 服务端 `roserver/gameserver/protocol/action/rpcc2g_singingrequest.cpp:98` 通过 `STATE_MUTEX_MGR->TransferStateByFunc` 请求状态迁移，只有该迁移的成功回调才派发 `ROClientFireSkillArgs`；`rogamelibs/entity/action/singing_component.cpp:273` 的 `OnGetEvent` 校验进入条件（`CheckDoSkill` 在 324 行、目标范围在 345 行、`CanSkill` 在 414 行），`OnUpdate`（619 行）在 651 行再次校验目标合法性。服务端是多条独立校验、多阶段复查，而不是每侧固定一次判定。
- 既有思维导图中“客户端 `CanSkill` 是唯一准入”的说法漏掉了它前面的 `StateExclusionManager` 判定；本次查询未修改该 HTML。后续客户端移植是另行提出的需求，本文只记录源码事实，不代表该移植已实现或已通过运行时验收。

## [QUERY][技能][吟唱][SKILL-FALSE-SINGING-20261102] 非吟唱技能被服务端额外吟唱阶段延迟

- 用户反馈连续按普通攻击，以及一个技能完成后接续另一个技能时出现 `当前状态无法施法`，并明确确认这些技能不是吟唱技能。本次只读排查：未操作桌面，未修改业务代码、表格或数据库，未重启服务器或与实网游戏交互。本记录是问题诊断，不代表已修复。
- `SkillCtrl.cs:426` 将服务端 `CmdCode.SkillStateBlocked` 映射为该提示；`OnSkillCastResult` 先应用结果再显示提示。新客户端 `MNavigationComponent.cs:162` 的互斥检查传入 `showErrorTips=false`，因此该处提前拒绝不会显示提示。`OnAfterAction` 发送请求后立即调用 `BeginPrediction`；客户端通过 `MSkill.IsSkillStateFinished` 与 `MSkillCore.SkillStateTimeSeconds` 判断动作完成。
- 移植时将原有动作时长误用为服务端新增的吟唱等待：`LYF_Server/GameServer/Components/SingingComponent.cs:91` 设置 `interval_ = effect.CastMs`。等待结束后，`ROSkillComponent.cs:818-829` 又取表现段的 `SkillStateTimeMs`（或 `DurationMs`）作为动作占用时间，即使只有一个表现段也如此。因此两个字段均为 3100 ms 的非吟唱技能，服务端占用约 6200 ms，而客户端立即开始的动作占用 3100 ms。该数字是源码时序推导，不是实网测量；这可以解释客户端已允许接续请求时，服务端仍认为上一个动作未结束。
- 与反馈相符的拒绝路径有两条：额外吟唱阶段收到的连续普通攻击可进入 `RoleSingingComponent` 请求队列，经过 `REQUEST_LIST_OVER_TIME=300` ms 后过期（`:16`、`:227-235`），返回 `SkillStateBlocked`；后续服务端动作阶段，`ROSkillComponent.PrepareCast` 在 `CheckDoSkill` 失败时拒绝（`:647-649`）。`CheckDoSkill` 读取 `SkillCastingRulesCatalog.CanInterrupt`（`:370`）；配置中的普通技能 `InteruptSkill` 列表为空。这些路径来自源码核对，未通过实网抓包确认。
- 使用现有生成的配置表读取代码解析实际部署的 `LYF_Server/GameServer/bin/Debug/LubanManage/Tb/tbskilleffectinfo.bytes` 与 `tbskillpresentationinfo.bytes`，未启动 GameServer。技能 10010：CastMs=2167，首段 SkillStateTimeMs=700，cooldown=500。技能 10040/10050/10060/10070 的 CastMs 与动作占用分别为 3100/3433/3167/1133 ms。源表 `Luaban/MiniTemplate/Datas/#SkillPresentationInfo.xlsx` 中 Id=3001001 的行当前为 500 ms，部署数据为 700 ms；早期 500 ms 的解释引用了 Excel，检查部署数据后已更正。未改动数据，未检查实网客户端实际加载的 bundle。
- RO 参考 `F:/UnityPro/ro/Ro_cs422nb/rogamelibs/entity/action/singing_component.cpp:794/878-894` 使用 `FixSingingTime` 加 `FloatSingingTime` 计算真正吟唱时间，而非表现时长。建议修复方向：当前非吟唱技能走零吟唱事件路径；保留动作时长、占用语义与互斥检查，并对齐客户端和服务端的完成时点及 MP/CD 时机。不要绕过互斥检查或给这些技能凭空增加吟唱。尚未修改业务代码，也未执行运行时验收。证据：`_temp/skill-false-singing-query-20261102/evidence.json`。

## [QUERY][技能][打断][组合][RO-SKILL-CANCEL-20261103] RO 技能取消与组合接段语义

- 用户已批准本次静态调查记录；本次未修改业务代码、配置、资源或数据库，也未执行构建或游戏运行。分析与证据见 `项目分析/2026-11-03-RO技能主动打断与组合接段分析.md`。目标是用户描述的操作手感，不是对《逆水寒手游》内部实现的已验证结论。
- RO 将 SkillStateTime 与完整表现时长分开，在子技能状态结束时推进组合下一段（MArtSkill.cs:297；art_skill.cpp:190/211）。新技能的 InteruptSkill 列表在客户端和服务端决定是否允许打断当前技能 ID。通用 StopState 仍检查 CanActiveInterrupt，普通技能状态不会无条件授予主动打断权限。在已检查的输入、移动、状态和协议范围内，未找到与本项目匹配的专用闪避/三段跳取消输入，不能将这些机制等同于默认允许跳跃取消；这也不能证明所有 RO 脚本或版本均不存在此类输入。
- RO 的 BuffStopState 到达 ROSkillComponent.OnStateStopBySkill（skill_component.cpp:1698），遵守 ATTR_SPECIAL_NO_STOP_SKILL，再依次执行 Stop → OnStop → EndSkill。ResultList.At 调度服务端逐次命中事件；Freeze 清除待触发计时器和子弹、结束子技能实例，并阻止后续组合段。客户端 Cease/Puppetize 取消计时器与回调；MArtSkill.Stop(cleanup=true) 销毁已记录特效并停止音效和移动。已经结算的伤害保留，但当前段尚未发生的命中也会取消，因此开始某一段不保证该段伤害一定发生。
- 当前 E/2 的持续时间与状态时间分别为 3100/3167 ms；组合技能 10100 等待 E 完整占用结束，服务端总占用时间为 6300 ms。客户端 MSkill 仅在多段表现时使用 SkillStateTime，TriggerAnim 会按动画片段长度重新赋值单段表现的状态时间。CanSlideCancel 为 E=1、2=0、组合=0；由当前主技能 ID 控制权限。TryJump 拒绝 kStateSkill，且未使用 CanJumpCancel。本地 OnStop 仍通过 OnSkillAutoEnd 衔接，允许后续表现事件继续；权威 Interrupted 则单独调用 Cease。已经生成的粒子独立存续。
- 服务端 ROSkill 当前跟踪动作截止时间，尚未调度命中/结果事件；OnlineRole 仅注册 default/singing/skill 状态。客户端眩晕状态尚未构成完整的权威控制/Buff 链。以下仅为提议，尚未实施：统一按状态时间接段；按权限允许滑铲/跳跃取消；区分正常尾段与实际中止的清理；接入真实的逐次命中权威伤害；控制效果调用同一停止路径。新的接段毫秒数、实际视觉/碰撞行为及伤害测试仍未验证。完整的本项目文件路径和 RO 源码行号见分析文档。

## [QUERY][怪物AI][副本][LYF-MONSTER-AI-READY-20261104] LYF 副本第 18 波接入服务端怪物 AI 的现状

- LYF 副本 4060 的每个实例已由 `SceneWaveSpawner` 生成第 18 波的 1 只邪骸兽人，配置 ID 为 5515，出生血量为 240，`SpawnId` 为 77309411329。场景 XML 选行为树槽位 1 和巡逻路径 1，路径由 3 个点组成，按 PingPong 往返。`MonsterWaveManager.Update` 方法为空，生成器也未逐帧更新自己的 `EntityWorld`。AI、仇恨和导航组件目前仅作标记，在本次检查的游戏代码中未找到可执行的 RO 行为树和黑板流程。项目已有共享的 `MNavigation` 实现和地图数据。隔离寻路查询覆盖出生点到首个巡逻点，以及相邻巡逻点之间的两个方向，5/5 均找到路径。
- 要让怪物移动后仍能正确受击，需要调整现有命中检测。`SceneWaveSpawner.ChangeHP` 以出生模板坐标 `point.Position` 判断命中，未读取怪物的当前坐标。在独立进程中调用已编译的 `GameServer` 类，将兽人沿 X 轴移开 30 米后，测试攻击在新位置未命中，在原出生位置命中。程序集内两份场景资源均与当前项目文件一致。未启动游戏服务器或数据库。
- 玩家普攻 10010 已能由服务端结算对怪物的伤害。当前服务端技能表有 10010，缺少 RO 兽人普攻 1000118401。现有移动和施法消息用角色 ID 标识玩家，怪物属性更新携带怪物血量和伤害。要让怪物攻击玩家，还需由服务端选择目标，按副本实例和 `SpawnId` 向客户端同步怪物移动与施法，并同步玩家受到伤害的结果。5515 的跑步与普攻动画片段均已存在，预制体实际引用的控制器仅接有 `Idle`、`FIdle`、`BeHit`、`Dead` 四个状态。
- 建议先实现第 18 波巡逻，由服务端维护怪物当前位置，客户端回放移动。之后接入受击反击、追击和一次普攻，验证同一实例内其他玩家所见表现和怪物死亡流程，再扩展到 BOSS。这是开发建议。源码和隔离验证的证据见 `_temp/lyf-monster-ai-readiness-20261104/source-check.json`、`_temp/lyf-monster-ai-readiness-20261104/runtime-probe.json` 和 `_temp/lyf-monster-ai-readiness-20261104/skill-table.json`。本次未修改游戏业务文件，也未在 Unity 实机或多人联机中验证追击和攻击。
- 5515 的实体编号、原始预制体 `Monster_XieHaiShouRen.prefab` 和动画片段均来自 RO。原始预制体未绑定控制器。非过场动画时，RO 为它设置普通怪物动画类型，并在运行时加载公共控制器 `Controller/MMonsterAnimator`。LYF 中的这份公共控制器与 RO 原件逐字节一致，兽人的普攻和跑步片段也分别与 RO 对应片段逐字节一致。Codex 在提交 `0a7c986` 中为 LYF 魔神塔运行时角色新增了 `Monster_5515.prefab` 和 `Monster_5515.controller`。LYF 的 `RuntimeCreatures` 中有 22 个怪物预制体和 11 个 NPC 预制体，均直接绑定各自的控制器。`MSceneDungeon` 传入空控制器路径时，`MAnimator` 会沿用该预制体的绑定。证据见 `_temp/lyf-monster-ai-readiness-20261104/animator-origin.json`。

## [QUERY][怪物AI][副本][MOSHENTA-WAVES-FLOW-20261106] 全波次 AI、生成条件与副本目标

- 本次对 RO 副本 4060 / EZ 进行只读调查。其 63 个配置组包括 47 个怪物组、14 个 NPC 组、一个采集组和一个触发组事件；条件包括 54 个 None、八个 WaitEvent 和一个 And。波次 ID 是标识符，不代表必须按数字顺序击杀推进。波次 18 和多数普通怪物满足启动时生成条件。若要求每场普通怪物遭遇都在进入区域时生成，需要新增激活配置；RO 已提供相应事件机制。
- 准确流程为采集波次 41（500033）-> DestroyDemonMatrix -> SceneAI / 触发组 2 -> SummonDemon -> 魔法师波次 42 -> 其销毁完成目标 141 并解锁波次 62 -> 触发组 12 创建传送对象 5。区域对象 18/19/20/8/21 分别完成目标 137/138/139/140/142。目标 137-143 属于特殊事件目标；DungeonTargetMgr 管理副本进度并同步当前阶段。Key01-Key04/NoKey 选择的是钥匙 NPC 候选，并非普通怪物路线波次。
- 每个实体应选用其配置的树。StandBy 对应波次 15,16,18,19,21,25-28,30-32,42,43；RandMove 对应 17,20,23,44,45；NanGuaHun 对应 22,24,29,33。波次 34/35/46 使用三棵不同的性格树，49 使用 BossController。对于 5510，配置索引 1 经 GetAIByIndex 限制到最后一棵可用树，实际得到 StandBy，并非未使用的 MoFaShi 文件名。其技能列表包括一个普攻和四个组 1 法术。没有树的尸体、伤员和机关实体应保留原有职责；普通怪物还需要原有主动仇恨及各实体自身的技能、属性，不能复制兽人常量。复用公共 MMonsterAnimator。
- NPC 波次 47（20186 -> EntityID 5456）使用 DaNaTuoSi 机关树。波次 34/35/46 死亡后完成目标 143 并设置 DaNaTuoSiState=5；波次 47 施放觉醒 1000545602，其来源事件在 3.33333349 秒发送 FinishDungeon；SceneAI 激活波次 48（5357），后者的出生 buff 925801 通过自杀满足 VictoryCondion1=2=5357=1。完成目标本身并非完整胜利链。波次 60/61 是事件驱动的震屏实体。
- LYF 本次检查的源码从 26 个启动组展开 68 个战斗实体，且只为波次 18 绑定 AI。MSceneDungeon 仍决定本地启动生成；在已检查脚本中未找到其 SendEvent 入口的调用方，低于 100% 的前驱阈值仍被阻断。缺失接入包括服务端负责的动态波次、必要的区域/采集/SceneAI 事件、目标同步、其他怪物技能和树，以及完整胜利链。现有角色任务展示不提供这些副本目标。建议实施顺序和各组详情见 `项目分析/RO魔神塔全波次AI与副本流程调查-2026-11-06.html`。
- 证据：`_temp/ro-moshenta-waves-query-20261106/evidence.json` 覆盖 63 个组、35 个触发对象、21 个服务端触发组及图连接；runtime-entity-tree-check.json 将两份随包提供的 EntityTable.bytes 中的 32 个 ID 与 CSV 对照，未发现不一致。相关波次字段与 LYF 副本一致。HTML 包含 63 个配置组行、七个目标行和 13 个源文件链接；仅执行静态内容检查。未验证 RO 实际运行触发时序、双客户端完整流程、运行中的服务端版本或打包构建。本次调查未修改玩法源码、资源配置、协议或服务端进程。
- 开发者于 2026-11-06 批准正式归档。完整文档清单、执行身份、双语证据和 Git 回执见 `项目修改日志/2026-11-06-魔神塔全波次AI与副本流程调查.md`。调查和归档由 Codex 执行；遵循用户仅用 Codex 的指示，本轮新增段的中文同步与增量一致性检查由 Codex 原生子 Agent 完成。
## [QUERY][场景编辑器][UI][SCENE-EDITOR-USABILITY-20261107] 缺少流程导航及编辑器与运行时差异

- 用户反馈整个编辑器难用，尤其看不到从开始到结束的清晰路线。对当前工作区的静态审查确认：波次/机制页签只过滤左侧列表，中央画布仍保留默认 XML 的全部 336 个节点；在最小 1100x650 窗口中，按原坐标适配后计算缩放约 ~0.0636，节点宽约 ~14 px。连线没有箭头或事件标签，也没有开始/结束、实时执行/阻塞标志。目标面板已有，但只是静态表格视图，不是预览进度。依据为 `LYF_MMO/Assets/GM_Game/Editor/ROSceneEditor/ROSceneEditorWindow.cs` 的 DrawList、DrawGraph、DrawNode、DrawDungeonTargets、FitGraph。以上尺寸由源码/XML 计算，并非从 Unity 截图测量。
- `MSceneEditorConfig.cs` 存在两个静态确定的编辑风险：Connect 允许机制 triggerKnob 连接波次 prevInKnob，随后 SynchronizePreWaves 写入 pre_wave_id=-1；RemoveNode 删除触发器组 34 时，不会清理波次 63 的 trigger_group_event_info/group_id=34。未在 Unity 实机执行修改或复现。编辑器源码已有的未提交改动保持原样。
- 预览与正式运行差异：ROScenePreview 支持 Or/TriggerEvent 前置条件，以及 And 百分比阈值；服务端 MoshentaSceneFlow.Advance 支持 None/WaitEvent/And，并要求前置波次全部完成。预览处理 Timer 和 result_index 执行顺序，服务端 RunResults 均未处理。预览检查触发器 enable/次数限制，而服务端按钮路径未应用相同字段。这些属于源码确认的差异，不是已经测试的正式运行事故。
- UI 提案尚未实现：持续显示明确的 Start/End 和有序阶段导航；只展示所选阶段的进入条件、动作、完成条件与后继；相关参数放在旁边，并显示当前预览等待/阻塞原因。本地交互布局草图通过宽窄两种宽度的 12 项选择检查，无脚本错误或溢出，证据为 `_temp/scene-editor-audit-20261107/layout-check.json`；这不是 Unity 行为验证。
- Codex 子 Agent audit_scene_editor_graph（会话 01a1b536-c15c-71e2-8c5d-371d611e529e）和 audit_scene_editor_preview（会话 01a1b536-ec2b-7882-bbf2-c0f9839eb1f0）各完成 1 轮只读审查，无返修轮次；精确模型未披露。主 Codex 核对了关键源码证据。原生 UI 截取未返回 Unity 编辑器，因此实机布局/交互仍未核验；未修改 Unity、服务端、场景、玩法或配置。用户于 2026-11-07 仅批准本项查询记录归档。用量证据位于 `_temp/scene-editor-audit-20261107/child-usage.json`；主会话日志覆盖截止 2026-11-06 15:54:12 CST，已观测到 2 次压缩，但当前累计总数覆盖不完整。

## [QUERY][场景编辑器][副本][STAGE-CONFIG-EXPLAIN-20261107] 阶段顺序、刷怪与机关时机

- 阶段页根据 DungeonsTable.DungeonTargetType/DungeonTarget 构建顺序。Type1 使用 `|` 分隔阶段，`=` 表示列出的全部目标 ID 属于同一阶段；目标文案/条件来自 DungeonTargetTable。Type2 引用 DungeonTargetGroupTable 的 ID。入口/完成出口是编辑器生命周期条目，不是额外目标行。来源：ROSceneStageFlow.BuildSections 和 DungeonTargetProgress 构造函数。既有已验证快照 `_temp/dungeon-config-progress-20261106/config/DungeonsTable-rows.json` 记录 dungeon4060/type1/137|138|139|140|141|142|143；本次说明未重新读取当前 XLSX。
- 当前 Moshenta XML 的顺序是：进入 area18 -> SceneAIEvent HuiHeBuDui 完成137 -> 当前目标变为138 -> WaveEvent HuiHeBuDui 激活 Wave18/entity5515。目标138在 area19 通过 TongWangErLou 完成；目标139在 area20 通过 DaoDaErLou 完成。阶段2不要求击杀 Wave18。证据：LYF_MMO/Assets/GM_Game/Resources/Dungeons/Moshenta/RO_DaNaTuoSiZhiZhan_EZ.txt 的行1724-1731、7841、8204、8300、8427、8378及原始连线；LYF_Server/GameServer/SceneEntity/MoshentaSceneFlow.cs:207。
- 入口到二层的交互34属于阶段3完成后的效果：其初始 ignore_init=true；进入 area20 时先完成139，再创建对象34。随后点击34会传送到 (41.13,72.06,212.74)。这是返回捷径，并非进入阶段3时就自动开放的传送门。XML 证据为行1606、10949、10972-11005、11037及连线19201/19322-19323。
- 波次/机关卡片依据真实目标/事件/前置条件/区域/端口推导关联；选择阶段不是运行门禁，也不是任意指定阶段编号。DungeonTargetProgress 只处理当前阶段，并在切换时重置目标计数；MoshentaSceneFlow 独立跟踪波次生命周期，因此活着的怪物可以在阶段切换后继续存在。修改顺序需要改副本表，修改目标需要改目标表，修改刷怪/交互需要改实际场景触发条件/结果。行内编辑器没有用独立的阶段编号字段替代这些机制。
- Codex 原生子 Agent verify_stage_example 完成限定范围的源码/配置复核；主 Agent 核验阶段解析及既有证据。本次说明未修改代码、配置表或场景，也未运行 Unity/玩法测试。用户仅批准本条双语查询记录及索引；此前实现证据仍归属 STAGE-INLINE-EDIT-20261107。技能：project-subagent-coordination 用于限定范围的 Codex 复核，task-log-policy 用于已批准的记录写入，token-monitor 用于 collect/report 和压缩计数。本地日志覆盖截至2026-11-06 15:54:12 CST，已观测到2次压缩；本任务 token 归属及账户余额不可测。本地原始输出：`_temp/stage-configuration-explanation-20261107/`。
