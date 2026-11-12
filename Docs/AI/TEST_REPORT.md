# 验证事实

## [VERIFY][协作][CONTEXT-CLEANUP-20261026] 协作记录整理验收

- 范围：12 份原入口快照及字节数/SHA256、标题到原行段的恢复索引、缩短后的根目录双语入口、两个客户端重定向、按需日志规则和 AGENTS 路由。本次没有修改业务代码、Unity 资源、源数据表或运行时设置。
- 验收证据将记录快照哈希一致性、恢复覆盖、必要链接、近期编号保留、技能校验、修改文件范围与双语增量检查。修改日志承载实际结果和未决边界；本段自身不等于通过证明。
- 保留边界：下文最近根目录角色/任务表证据；较晚客户端 TMP/字体/布局检查在 `项目修改日志/协作记录归档/2026-10-26/LYF_MMO/Docs/AI/TEST_REPORT.md`；更早 UI/网络/玩法未验证事实仍带原日期保存在快照。本次仅整理文档，没有重跑 Unity/服务端测试。
- Token-monitor collect/report 输出：`_temp/context-log-cleanup-20261026/token-collect.txt` 和 `token-report.txt`。Codex 适配器为实验性；项目最近一天统计窗口包含其他工作，不能单独代表本次整理消耗或账户额度。

## [VERIFY][角色][ROLE-FACE-MATERIAL-20261025] 角色材质发白修复验收

- 导入后读回：七个 `P03_*.mat` 的 `_HighColor = (0,0,0)`；`P03_skin` 的 `_1st_ShadeColor = (0.886,0.784,0.780)`、`_2nd_ShadeColor = (0.749,0.596,0.612)`；`P03_Skin_Normal.png` 读回 `textureType = NormalMap`、`sRGB = False`、运行时格式 `DXT5`（原为 `DXT1`）。
- 隔离对照渲染（同一 `Role_JX.prefab`、暖白主光 0.85、600×600、固定机位）：脸颊均值由 (221,167,161) 变为偏暖且可见明暗分段，背景/构图不变；四档方案对比见 `_temp/skin_options_4.png`（①原状 ②去高光加色 ③再加阴影加深 ④③+GI 0.3）。已应用②③，④过暗未采用。
- 场景内验证：`Scene_CreateRole` 用真实相机 + 临时实例化同一预制体渲染，角色受光正常、面部可辨；渲染后即删，场景 `dirty = False`，未保存。
- 已查证但不改：项目为 Gamma 色彩空间（UTS2 原生按 Linear 设计）；该 `LegacyToon.shader` 为裁剪版 UTS2，`_MATCAP/_RIMLIGHT/_NORMALMAP` 无 shader_feature，改材质 float 不切变体（实测关闭 matcap 像素差 ≤1）；`_MatCap_Sampler` 引用的贴图 guid 已丢失，遮罩为 0 故暂无视觉影响。
- 未验证：Play 模式真实运行观感；主城黄绿方向光（0.985,1,0.764）下的肤色未单独取色；NPC `QuQu` 使用另一套着色器未处理。未运行客户端/服务端构建。

## [VERIFY][角色][ROLE-FACE-LIGHT-20261025] 角色面部灯光优化验收

- 材质导入后读回：7 个 `P03_*.mat` 的 `_GI_Intensity = 0.7`、`_Is_BLD = 0`。
- 隔离对照渲染（同一 `Role_JX.prefab`，头部区域平均亮度/255）：捏人场景原状 120.7 → 加主光后 170.8（强度 0.85 档，过曝像素 19.2%）；背光面部 144.3 → 175.3；正面受光基准 179.8。强度 1.05 档为 178.3/过曝 31.3%，偏白故弃用。
- 场景 diff 复核：+104/−10 行，`Light:` 块数 = 1，`m_RemovedComponents: []` 仍为 18 条，无其它对象被删或被改；两处 `stripped` Transform 仅顺序变化。
- 过程事故已修复：清场时用 `FindObjectsOfType<Light>(true)` 误删场景内未激活的预制体灯并保存，已 `git checkout` 完整恢复后重做，最终 diff 已复核无残留删除。
- 未验证：真实运行时捏人界面与主城的实际截图（角色与相机均由运行时代码启用/生成，编辑器态无角色）；NPC `QuQu` 使用另一套 `Toon_DoubleShadeWithFeather_Mobile.shader`，本次未处理；主城效果由同预制体背光对照推断。未运行客户端/服务端构建。

## [VERIFY][任务][VERIFY-TASK-TABLE-20261025-R2] 任务表体系 13 张的生成与交叉引用验证，2026-10-25

- 范围：`Luaban/MiniTemplate/Datas/` 下 13 张任务表的内容正确性、Luban 可生成性、表间交叉引用与枚举取值。不含任何代码、协议、数据库、Unity 资源或运行时行为。
- 表头格式：13 张表统一为 A 列 `##var`/`##type`/`##` 标记列 + B 列起字段；用 `_temp/verify_quest_tables.py` 逐表断言 A1/A2/A3 值，全部通过。
- Luban 生成：在 `Luaban/MiniTemplate` 执行 `dotnet Luban/Luban.dll -t all -c cs-bin -d bin --conf luban.conf`，输出重定向至 `_temp/luban_check2/`；生成 13 个 `tbquest*.bytes`、13 个 `Quest*.cs`、13 个 `TbQuest*.cs`，日志无 ERROR 并以 `bye~` 结束；`QuestInfo.cs` 的 `public readonly` 字段计数为 55，与表头 55 列一致。
- 交叉引用（`_temp/verify_quest_tables.py`，结果 0 错误 0 警告）：13 表主键唯一；`QuestInfo.Type` 全部命中 `QuestTypeInfo`；`ChapterId` 命中 `QuestChapterInfo`；`RewardId` 命中 `QuestRewardInfo.RewardGroupId`；`PreQuestIds`/`TogetherPreQuestIds`/`SubQuestIds` 全部指向存在的任务；`QuestTargetInfo.QuestId` 与 `QuestTriggerInfo.QuestId` 命中任务；`QuestTriggerInfo.TargetSeq` 在对应任务内存在该 `Seq`；类型 6 目标的 `ParamId` 命中 `QuestItemUseInfo.ItemId`；类型 21 目标的 `ParamId` 命中 `QuestSceneInteractionInfo.Id`；`QuestDropInfo` 的 `OwnerId` 按 `OwnerType` 命中任务或目标；`QuestSurpriseInfo`/`QuestFlyAcceptInfo` 的 `QuestId` 命中任务；`QuestTypeInfo.AcceptConditionId` 命中 `QuestConditionInfo`。
- 枚举与区间：`LimitSex`∈{0,1,2}、`AcceptType`∈{1,2,3}、`FinishType`∈{1,2}、`ShortcutShowType`∈{1,2,3}、`FindwayType`∈{1,2,3}、`AbandonType`∈{0,1,2}、`DestroyType`∈{1,2}、`TargetRunType`∈{1,2}、`TargetType`∈1~22、`Moment`∈1~4、`EventType`∈{1..19,22,24,25,26,27}、`Theme`∈{1,2,3}、`SourceType`∈{1,2}；三组等级区间均满足 `min ≤ max`（`max=0` 视为不限）。
- 数据规模：章节 3、类型 11、任务 10、目标 12、触发器 5、奖励明细 13、条件 6、奇遇 2、目标库 4、掉落 3、场景交互 2、道具使用 2、飞鸽 2。
- 未验证：未跑 Unity/服务端编译；未更新 `LYF_Server/Network/LubanManage/` 生成物；未进播放模式；怪物 ID 1001~1003 与场景坐标是占位值（`#NpcInfo` 仅 4001，无怪物表）；`TaskGuideNpcTable`/`TaskChangeJobTable`/`TaskEdenLevelTable`/`TaskCameraTarget` 未建表。以上均不得据此推断任务玩法可用。证据见 `项目修改日志/2026-10-25-RO任务系统分析与Luban任务表设计_CHANGELOG.md` 第二轮修订段。

## [VERIFY][场景][RO-FIRST-DUNGEON-SCENE-20261026] RO 副本场景复制迁移验证

- 执行者：WorkBuddy 桌面会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`，模型 `deepseek-v4-flash`（全程未切换），在 hub `hub-0f37bdcc-e89f-4a17-8020-9f9c2024ade5` 下共 4 轮；最终被验收的 attempt 为 `run-b574c91b-5930-438a-837f-386f3e37afa7`。独立验收与 Git 分支操作由 Codex 完成。
- Unity 实例 `LYF_MMO@47ea91c229a904f3`，2023.2.20f1c1，Built-in 管线。全部检查走附加场景加载 + 临时相机与临时灯光；未覆盖用户当时打开的 `Scene_Boot`，编辑器始终非 Play、非编译中。
- 已通过：`ShaderUtil.ShaderHasError = False`、`isSupported = True`；10 个材质全部绑定 `LYF/Dungeon/ROVertexColorSafe`；场景根对象 1、Renderer 172，`nullMaterial / noShader / errorShader / nullMainTex / multiSlot` 全为 0；MonoBehaviour 0 组件 / 0 类型、Missing Script 0；新目录内无 `.cs` / `.dll` / `.asmdef`。
- 实测数据：全帧洋红像素 0，原问题像素 (271,383) 由 `255,17,197` 变为 `71,66,65`；平均 RGB `63.0/63.2/63.6`，G−R = +0.1、G−B = −0.5，对照 R3 的 `31.6/51.3/30.7`（G−R = +19.7、G−B = +20.6）——这就是"偏绿"的量化含义。与 R3 截图差异 509,758 / 786,432 像素（64.8%），最大通道差 127，属亮度与色偏的整体变化，符合预期。
- 落盘计数：251 文件 / 18,661,241 字节 / 17.80 MiB，与 `_temp/ro-first-dungeon-migration-manifest-r4.json` 一致（250 条 + 目录外同级 meta）。正式日志第 5 节 251 行表的字节合计重解析后仍为 18,661,241，完全对齐。
- Console：仅两条历史 `sc_dungeon_duobiwanfa_water_001 ... Shader 'RO/Scene/Water' doesn't have _LightFactor`，是 R2 诊断阶段临时读写该属性的副作用；本轮新增错误 0，Console 未清空。`Could not load the file 'NiceIO'.` 为无关历史条目，未处理。
- 未执行：未接 Boot / 配置表 / YooAsset 运行时加载，未做副本玩法、组队、阵营、服务端、打包或联调，未做完整客户端或服务端编译。源场景本身无碰撞、无灯光，未重建烘焙光；局部最简贴图着色不等于原 RO 完整渲染效果。
- 记录期复核与漂移核实（2026-10-26，只读）：一次 Unity 编辑器保存把 `RODungeon_First.unity` 从 904,144 重新序列化为 946,305 字节（+42,161，mtime 22:39:09）。该迁移 Hub 之后的一轮只读核验（attempt `run-77bf2883-af7b-49c1-aba9-fefa6099f8b9`，是核验轮、不是第 5 轮迁移）把增量逐字节归因：新增 `Role_MJS` 预制体实例 +2,408、`SceneRoots` +133、170 个既有预制体实例 +40,907（Unity 2023 的 `serializedVersion: 3` 与显式 `m_AddedComponents`）、YAML 折行合并 −1,627、`LightmapSettings` / `NavMeshSettings` / `RenderSettings` +340，合计 +42,161。用 R1 剥离规则在 `_temp` 重建的旧基线精确为 904,144 字节（sha256 `fa99118e…`），当前文件 sha256 `55f9981e…`；展开折行后 170 个实例的修改项 0 缺失 / 0 多出 / 0 值变化，172 个 `addedObject` 全是既有 `LODGroup`。Unity 同时生成 `RODungeon_FirstSettings.lighting`（1,772 字节）与其 `.meta`（200 字节），故目录现为 **253 文件 / 18,705,374 字节**；本段早前写的 18,703,402 字节漏了这两个文件，此处更正。保存操作者无法从文件或日志判定，**不推定**——迁移脚本与本会话都没有 `SaveScene` 调用。集成验收基线仍是 251 文件 / 18,661,241 字节。详见正式日志第 5 节与第 12.1 节。
- 证据：`_temp/ro-first-dungeon-final-review.md`、`_temp/ro-first-dungeon-migration-manifest-r4.json`、`_temp/ro-first-dungeon-final-r4.png`；R1 黑图、R2 洋红、R3 偏绿截图均保留。完整日志：`项目修改日志/2026-10-26-RO首个副本场景复制迁移_CHANGELOG.md`。

## [VERIFY][场景][RO-MOSHENTA-MIN-ENV-20261027] 环境组件增量验收

- 执行者：WorkBuddy 原生桌面会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`，同一会话原地续作（开发 2 轮；本次记录同步不是第 3 轮实现）。模型 `deepseek-v4.1-flash`，由 Codex 在两个原生轮次的实际请求里独立观察到 `requestModelId`，不以界面配置的模型作为唯一证据。验收与 Git 由 Codex 负责。
- Codex 独立在 Unity 现场核对：Shader `supported=True`、`hasError=False`、`GetShaderMessages=0`；组件存在，937 条绑定 0 空引用；之后 Console 0 error。
- WorkBuddy 证据：第 1 轮验过 Play 进入/退出与场景重载；2 张光照图与 LightProbes 能恢复，光照图开/关使建筑区 **25.25%** 像素差超过阈值 12；云色扰动确实改变云层画面（均值 `129.81/67.38/80.93` vs `129.81/129.62/129.42`，扰动后已恢复源默认白）。
- 第 2 轮对 27 个通道与"源资产值 + 源公式"的独立复算对照（最大误差 5.4e-7）；改配置后再应用能更新云色 `(1,0.2,0.2,1)`；Restore 把 27 个通道**逐项精确**还原成应用前的哨兵值，收尾写回测试前状态后 0 差异。第 2 轮刻意不重跑未变的映射 / 像素 / Play 大测试。
- 代码审查与 41 个文件的哈希全部通过（新增 39 + 修改 2 + 删除 0）；场景验收 sha256 `92c2ce0db331ac918614e69a01c113efbf929c67aea9a42a94432e9f511422a9`（10,017,238 B，与验收快照逐字节一致）。
- 源里处于非激活状态的两个对象各有 1 个空材质槽（`sc_fuben_002_002`、`sc_moshenta_shan_001`，`activeInHierarchy=False`），源本如此，不是本次丢失。
- 未验证：未做正式游戏场景进出流程、未做角色光照接入、未做客户端完整编译与打包；`RO/Scene/AllInOne` 的源关键字配置决定静态建筑不消费动态场景色调/雾通道，本任务只证明烘焙光与云层响应，**不声称**已还原 RO 完整渲染效果。
- 场景序列化与 HEAD 的差异已核实并记录，不掩饰：新增 679 条 stripped `MeshRenderer` 文档 + 1 个 `MonoBehaviour`，189 个预制体实例新增 359 条 `m_Materials.Array.size` 与 1 条 `m_StitchLightmapSeams` 覆盖，`m_FogColor` 改为黄昏 `(0.31461376,0.40945742,0.6603774)`，字节 +338,829。场景 YAML 的 `LightmapSettings` 文档未变——光图由组件应用时注入，不是落盘在场景里。属正常 Unity 序列化，未回改已验收场景。完整日志：`项目修改日志/2026-10-27-魔神塔最小环境组件与Shader依赖修复_CHANGELOG.md`。

## [VERIFY][场景][RO-MOSHENTA-EDITOR-LIGHTMAP-APPLY-20261027] 当前 Mesh Renderer 已可见既有烘焙光

- WorkBuddy 在连接中断之前实测通过：真实编译、编辑态自动应用、Disable 还原、Enable 自动应用，且应用前快照保持完整。
- Codex 之后用普通 Connect 按钮恢复了原 Unity MCP 会话，独立观察到 maps 2、probes 符合预期、`_applied` True、`ExecuteAlways` True、`dirty` False、console errors 0。
- 选中的 `sc_dungeon_moshenta_ta_002_b` 实际 `index 0`、scaleOffset `(0.2514144, 0.2514144, 0.2719581, 0.6459816)`、图为 `lm_sc_dungeon_moshenta_001_00_00.exr`；真实原生窗口截图中已展开其 Baked Lightmap 缩略图 / 索引 / UV。
- Worker 当时的 partial 状态与纯黑 DockArea 截图作为历史失败保留；Codex 的实际截图补齐了那块缺失证据。
- 场景磁盘 SHA 仍为 `92c2ce0db331ac918614e69a01c113efbf929c67aea9a42a94432e9f511422a9`，脚本 SHA 为 `54b4ece2b3fdbe6f08b7c9bc0de5f5a1fea2053e557fb7ad2705036654d44e37`。
- 未做新的 Play 测试、未做正式切场景测试、未重烘。
- 此前那次 Unity 重启的原因、以及更早未保存改动的存留情况均无法判定；本记录不声称早前内存中的改动全部存活，也不把重启归因于已证明的原因。
- 当前 `dirty` False 只是点位观察，不构成"自动应用永远不标脏场景"的保证。


## [VERIFY][副本组队][DUNGEON-TEAM-20261029] 队伍界面壳、本地控制器与地图引用

- Codex 核验了实际预制体按钮回调：寻找、邀请、设置、目标、成员详情、申请、招募和成员菜单均正确打开；目标页可返回设置，子窗关闭返回正确。16 个 ROTeam 预制体及组合窗口的 Missing Script 均为 0；UIRoot 窗口引用未赋值字段为 0。仅临时实例为 EditMode 测试使用 EditorAndRuntime 回调；未将测试数据或实例保存到正式资源、场景。
- 使用真实 PlayerEntity 类型的隔离控制器测试通过：未建队挑战会打开窗口，创建后绑定玩家昵称，关闭重开保留队伍，离队恢复空队伍状态，无关主题不会加载 Moshenta。这不是实际账号或联网运行。证据：_temp/dungeon-team-20261029/codex-ui-verification.txt 和 codex-controller-verification.txt；codex-team-members.png、codex-team-search.png、codex-quick-team.png 为隔离测试预览。
- 附加场景检查：出生点已绑定至 (132.97, 58.89, 60.95)，MainCamera 数量为 1，碰撞器数量为 510，出生点导航采样=True。检查结束时编辑器不处于 Play 或编译状态。未发现 C# 编译错误。曾阻止预制体保存的 TeamInvite/Group/TextMesh 缺失组件已替换，保存结果=True。
- 未运行：真实登录 -> 挑战 -> 地图 -> 键盘/点击移动、联网组队与实例隔离、Player 构建。完整过程与此前失败版本：项目修改日志/2026-10-29-副本组队与RO界面迁移_CHANGELOG.md。


## [VERIFY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 真实登录指针输入返修验收

- 先前仅用 Invoke 的测试不能证明鼠标交互：迁移的内部 Canvas 缺少 GraphicRaycaster；RO 页签图形还保留空子 Button，拦截了新挂接的父级页签处理器。最终预制体已修复两项问题；QuickPanel 与 ThemeDungeonTeamCreate 的 Missing Script 为 0，缺少射线检测器的 Canvas 为 0。UIRoot 的四个快捷/队伍引用均已赋值；未改动其无关 Canvas。
- 使用真实已登录的 PlayerEntity，未使用 Mock 角色数据：通过 EventSystem.RaycastAll -> pointerClickHandler，验过任务/队伍切换、快捷一键创建、昵称与实际玩家一致、打开/关闭、重开保留成员、离队及窗口内创建。操作分隔在不同 Editor 帧执行，测试点位于实际控件内部。未向正式代码增加等待/重试或调试入口。
- 证据：_temp/dungeon-team-20261030/final-real-ui-verification.json、real-team-verification.json、hud-task.png、hud-team.png、team-window.png。图片由实际运行中的主/UI 摄像机离屏渲染，排除头像 RenderTexture 摄像机。主脚本编译无错误。临时验证器曾引用不存在的 SceneWorld 类型，已通过反射修正；正式代码未受影响。
- 直接 ScreenCapture 最初产生两条截图工具错误；清理该段 Console 历史前，已导出至 _temp/dungeon-team-20261030/capture-tool-errors.json。替代离屏截图成功，随后最终关闭窗口时 Console 错误为 0。未绕过截图工具禁止输出到项目外的限制；图片由临时摄像机渲染器写入项目根目录下的 _temp。
- 未验证：真实魔神塔进塔/移动、多人联网/副本隔离、Player 构建。编译后因密码未保存，由用户重新进入主城；未收集或记录凭据。完整过程：项目修改日志/2026-10-29-副本组队与RO界面迁移_CHANGELOG.md。


## [VERIFY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 真实场景往返与永久对象身份

- 原始错误见 _temp/dungeon-entry-20261030/original-error.json：Single 场景加载后，MinMapWindow.Update 第 45 行访问已销毁的主城 MinMapHelp。Moshenta 没有 MinMapHelp。隔离的专用 EditMode 场景检查通过首次绑定、缓存辅助对象已销毁时更新、显式销毁回调清理、缺少辅助对象时隐藏、绑定新辅助对象并恢复显示。EditMode 不会自动派发普通 MonoBehaviour 生命周期回调，验证器显式调用这些回调；此处不声称进行了真实场景切换。
- 真实账号通过现有登录/角色 UI，再通过正式挑战事件及已加载的主题 id7 / SceneMgr，执行 主城 -> Moshenta -> 主城 -> Moshenta。最终模型 ID -6380022、Animator ID -6380026、MainRoleCtrl ID -6380952 在四次快照中均保持相同，所有 modelScene 均为 DontDestroyOnLoad。用户澄清后，临时保留再 MoveGameObjectToScene 的方案已被替代，不是最终实现。
- Moshenta 出生点=(132.97,58.89,60.95)，2m 内导航采样=True；相机跟随原模型，世界相机 depth=-1，UI 相机 depth=0。返回主城后小地图恢复；副本清除旧辅助对象及静态引用并隐藏小地图。受测主城出生点的既有导航采样在本轮修改前后均为 False；未修改主城导航，也未声称已修复。
- 镜头诊断：运行时 Y 曾降至 0，镜头距离约 1.84m，而保存的场景原本就默认 Y=0.5。Moshenta 与主城不同，保留了遗留 Mouse X/Mouse Y 输入轴名，与右键镜头处理争用。清空轴名并对齐轨道默认值后，Y=0.5、距离约 10.32m；真实指针打开/关闭队伍窗口验证通过，Y 仍为 0.5。未覆盖用户的 CameraMgr 代码。
- 紧凑 UI 的真实指针检查通过任务/队伍切换及折叠/恢复；折叠后列表右缘=-1.5501 屏幕像素，完全位于屏幕外。源分页/折叠按钮尺寸为 40x64 / 34x34，任务视口高度=216。未将测试组件或 Mock 主角保存到正式场景/预制体。
- 证据：_temp/dungeon-entry-20261030/permanent-actor-verification.json、map-lifecycle-verification.json、city-layout.png、permanent-dungeon.png；截图由实际运行中的主/UI 相机离屏渲染。脚本编译通过，限定范围 diff 检查通过，最终 console errors=0。临时布局生成器曾使用错误的箭头子物体组件路径，验证器曾有相机变量遮蔽问题；均已纠正，未向正式业务添加兜底逻辑。被中断/停止的 Play 会话通过正常 UI 重新启动；未将凭据写入项目文件或记录。
- 未执行：Player 构建、多人队伍/副本隔离、完整战斗、受控键盘/点击移动及镜头碰墙回归。保留现有右键/滚轮镜头逻辑。完整修改日志：项目修改日志/2026-10-29-副本组队与RO界面迁移_CHANGELOG.md。


## [VERIFY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 队伍 HUD 红线对齐

- 保存的预制体与运行实例均使用 anchoredPosition=(148, -292)，此前为 (168, -320)。已将实际相机图像与用户红线参考对照检查；真实 EventSystem 射线点击成功打开并关闭组队窗口。证据：_temp/dungeon-entry-20261030/compact-team-redline.png。本轮仅检查位置调整；未执行 Player 构建或额外的场景/战斗回归。


## [VERIFY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 构建与冒烟通过，重启服务器后真实建队仍被拒绝

- WorkBuddy 构建 Network/Game/Gate/Center/Login，错误数为 0。强制 Unity 导入后，TeamData/TeamCtrl 已加载，控制台错误数为 0；此前空控制台不能证明新脚本已导入。离线检查验证了 5 名成员显示、本人不在首位时的正确绑定、邀请/申请回调参数、序列化引用及 MissingScript=0。这些均为隔离检查，不是真实多人验证。
- WorkBuddy 冒烟报告为 64 项检查/0 失败，Codex 独立重跑结果一致（root-smoke-review.txt）。已注册测试角色实际执行了 BaseInfo 地图/实例/出生点写入及离队清理。真实回环 RoleLeave 测试中，同实例观察者收到 20 字节，其他实例收到 0 字节；该结果未验证解码后的会话路由或所有广播类型。
- 纠正报告中的过度结论：源码并未调用 HandleClientEnterWorld；其中重复挑战测试不是真实的重复 EnterWorld 测试。移动/技能/可见性消息的接收者覆盖，以及双客户端可见性/移动仍未验证。
- 2026-10-30 重试：确认四个进程及所需端口均在运行；全新正常登录到达主城，真实 EventSystem TeamCreate 点击发出 11200。Game 发出 11210，网关发出 10001（本地时间 19:55:21，UnitySessionId=2）；客户端仍为 TeamId=0/members=0。不声称真实进入/返回成功或多人验收通过。证据：_temp/dungeon-multiplayer-20261030/server-retry/、root-live-review.json 和 root-business-inventory.json。完整过程：项目修改日志/2026-10-30-服务器权威多人组队副本_CHANGELOG.md。


## [VERIFY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 最终真实场景流程与解码后的多人协议检查

- 网关白名单回归：4 个有效通知成功，未知消息码及 4 个畸形载荷被拒绝；13 项检查/0 失败。真实 UI 建队不再收到网关 error10001。真实单客户端建队/挑战/离队/返回通过，原生 model/Animator/controller 的 ID 保持相同；证据为 _temp/dungeon-multiplayer-20261030/ 下的 root-live-single-pass.json。
- 37 项解码后的协议检查通过，并由 Codex 独立重跑。实际调用 HandleClientEnterWorld，加上主城阳性对照，现已证明该方法确实执行；副本实例/地图/出生点仍由服务器权威决定。双成员队伍共享非零实例，另一队伍被隔离；邀请/接受邀请/申请均一致拒绝已进入实例的队伍。
- 服务链路阳性抓包：move11181->Unity32；skill-start11171/end11173->Unity31（本人）、32（观察者）；presence11100/leave11213->Unity32；其他实例 33/其他地图 34 均收到 0 帧。GateTeamCtrl 将 team11210 和 leave11213 路由至不同的真实回环 Unity 会话，没有交叉投递。这些是进程内使用真实 Socket/Session 的隔离测试，不是两个已登录的 Unity 客户端。
- 保留失败轮次：最初的接收断言失败；随后第 10 轮错误断言移动/技能的 TCP=0。第 11 轮修正了测试数据：服务链路具有非空 NetClient(ClientType.GameServer)，因此 Session.IsGameServerLink 保持 TCP；只有未绑定的玩家链路丢弃实时包。玩家阴性用例不能替代服务链路阳性投递。
- 最终注释检查：13 个文件 token 等价，diff0/syntaxError0；最新 Game/Gate 编译至隔离的 _temp 输出，错误数为 0；Unity compiling=false/errors0。仅补注释的平台第 14 轮原生执行结束但未返回报告；Codex 独立核验快照/Roslyn/构建产物，并通过官方接口释放已停止且过期的租约（taskCompleted=false），未伪造执行者完成回执。注释 token 相同，无需重复行为测试。
- Codex 重启已退出的 Game/Gate 后，最终正常登录通过主城/建队（TeamId1/members1）。当前端口/进程独立检查，未按旧 PID 推断。证据：root-mp-verify-r11.log、root-comment-token-check.txt、final-game-build.log、final-gate-build.log、gate-whitelist-retry/ 和 mpverify/service-link-verification-r11.md。未运行：两个真实 Unity 客户端、已绑定末端客户端的 KCP 多人投递、Player 构建/完整战斗。完整记录：项目修改日志/2026-10-30-服务器权威多人组队副本_CHANGELOG.md。


## [VERIFY][副本组队][TEAM-NEARBY-LEAVE-20261031] 2026-10-31 协议回归与实际候选人收集器

- WorkBuddy 构建服务端/客户端均为 0 错误。同一组 95 项经帧解码的回环断言：基线失败 6 项，修复后失败 0 项；Codex 独立重跑修复版为 95/95。覆盖主城队长/成员/单人退队、地图/副本/位置保持不变、观察者在场可见性/移动、副本退队/返回及离线移除。服务链路使用非空 GameServer NetClient。证据：`_temp/team-nearby-leave-20261031/smoke/smoke-baseline-utf8.log`、`smoke/smoke-fixed-utf8.log`、`root-smoke.log`。
- WorkBuddy 隔离 UI 验过 0/1/3 行，EventSystem 指针点击分发返回对应行的正确 RoleId。Codex 使用隔离的真实 SceneWorldSystem/PlayerEntity/RoleEntity 实例调用实际 `CollectNearbyCandidates`：排除自身、同队、不同地图/副本、缺少信息、距离 >100m 及已移除实体；保留附近与恰好 100m 的实体。测试夹具修正了对 Global 字段/属性的假设及 Int32 InstanceId 类型，正式代码未改动。证据为同一证据根目录下的 `root-filter-test.cs`、`root-filter-result.json`。临时对象/队伍状态已恢复，未保存场景/预制体测试组件。
- round2 仅补注释；四个文件去除注释并归一化后的源码均保持一致，工作者提供的词法 token 证明在两个编辑文件中均匹配。验收后 Unity Console 错误为 0；未因仅注释改动重复执行行为测试。
- 构建夹具最初选到了旧服务端 EXE；最终断言前，修复测试 EXE 的哈希已与新构建的服务端一致。随后 Codex 构建常规 GameServer 输出为 0 错误，启动四个服务并确认 Gate-to-Game Established。未完成两个已登录 Unity 客户端联调、截图或 Player 构建验证。完整记录：项目修改日志/2026-10-31-附近邀请与主城退队可见性修复_CHANGELOG.md。


## [VERIFY][组队][DUNGEON-PLAYER-REGRESSION-20261031] 2026-10-31 回调生命周期回归与服务端通知入场路径

- Codex 在 Editor 内存中使用实际 SocketDispatcher/TeamCtrl 实例和真实序列化 TeamInfoNtf 执行检查：三个回调均转移到新控制器；释放旧控制器保留新回调；依次收到单人、双人快照时，当前接收者触发两次且 IsInTeam 保持 true；释放当前控制器移除全部三个回调。四项全部通过，原有 dispatcher/TeamData 状态在 finally 中恢复。证据：`_temp/dungeon-player-regression-20261031/root-team-regression.cs` 和 `root-team-regression-result.json`。这是隔离代码路径检查，不是真实 A/B Player 测试。
- 最终清理保持所有权行为不变；删除未使用的 LoadMoshentaScene 包装，从原 EnterMoshenta 移除本地 `_selectedEntry` 门禁。源码检查确认，无须事先选择 UI，scene4060 通知即可到达原有场景加载路径。最终 C# 编译错误0；Codex Console 错误0。热更新字节数667136/SHA256 063c97e09e6f17f0cc44c87f06c47d5d01c8165eae741b4af762ebc64d0e7f63；元数据中存在当前所有者，未使用包装及 TeamFlow 诊断均已移除。
- 较早的光照调查证实了 EXR/探针的交付与应用，但 Shader 剥离仍是假设。广泛保留 Custom 触发长时间编译；r2 收窄/构建包并未证明 Player 画面恢复。最初声称 DLL 中没有 TeamFlow，是 UTF-16 对齐导致的假阴性，已在 r2 纠正。随后用户暂缓光照；恢复本任务改动的设置，保留此前输出，未声称黑屏已恢复。
- 未执行：实际同包双客户端登录/邀请/UI/入场；Player 画面/副本一致性；最终新增 Bundle 或 Player 构建。用户选择自行验收。旧95/37测试未重跑，也未作为本任务多人联机证明。完整记录：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。


## [VERIFY][组队][DUNGEON-PLAYER-REGRESSION-20261031-R6] 2026-10-31 正式预制体邀请回放与双接收方链路校验

- WorkBuddy 第5轮编译 GameServer（0错误/10警告）、GateServer（0/0）、GM_Game 离线程序集（0/40），随后 TeamAuthoritySmoke 的108项断言全部通过（既有95项+新增13项）。这些属于隔离测试，不等同于 Player 验证。第6轮复用未变化的编译输入，补充定向证据。
- 在 Unity 临时附加场景中，使用真实预制体/Window/Widget/TeamData/SocketDispatcher 和真实按钮回放：38项断言，37项通过、1项失败。隐藏窗口收到邀请后可见并显示真实邀请者姓名；加入和 X 均隐藏邀请；无待处理邀请时不能打开假邀请；1至2成员快照展示两个带姓名且 RoleId 正确的卡片/行。旧行为对照复现窗口不可见和邀请残留。唯一失败为快捷行完整矩形因既有 VerticalLayoutGroup.spacing=-4.18 交叠4.18px；昵称矩形间距为34.78px。这不是几何全通过测试，未修改布局。其他状态文字边界仍可能交叠，不能把昵称结果推广至所有文字。证据：_temp/dungeon-player-regression-20261031/evidence/r6/ui-behavior-r6-v2.txt。
- 临时对象/场景已移除；dispatcher/cache、场景数量1至1、原始干净 Scene_Boot 和两个预制体哈希均已恢复/保留；UI 测试未发送数据包。加入提交在无连接时 transport 返回 false，因此仅证明 UI 关闭与事件接线，不能证明真实网络加入成功。
- TeamWireCheck 19/19通过：真实 TeamManager/OnlineRole 回环分别为 UnitySessionId7001/7002 捕获完整双成员 TeamInfoNtf，其非零 TeamId/CaptainId 相同，RoleId 为4001/4002。真实 GameCommand/TeamMessageHandler 与 GateClientResponseForwarder 将 CMD_ErrCode 路由至7003并保留 ReqParamError；畸形对照负载产生 ServerError。证据：evidence/r6/teamwire/teamwire-r6.log。客户端/服务端 Network.dll 的64个消息及8个枚举在字段编号/类型/枚举值上完全一致；未修改协议 DLL。
- Build/BuildDll 仅刷新变化的 GM_Game bytes；其余三个复制的 AOT bytes 哈希保持不变。新 AnswerInvitation 与 _hasPendingInvitation 元数据均存在；前缀804ca786095b1bd8，667136字节。最终 Editor console 错误0，停止播放并就绪，Scene_Boot 干净。Codex 核对9个清单哈希、实际范围内差异、现有路由与关键测试证据；限定范围的 git diff --check 通过。输入未变化且先前已通过，未重复完整构建/复测。
- 仍未验证：真实同包双 Player 邀请/UI 及共同进入副本。邀请清理可解释面板残留，不能解释有效 TeamInfo 后成员缺失；隔离成员链路当前显示正确。既有队伍可以拒绝邀请，但无现场 trace 证明该分支造成用户截图现象。错误文案仍为通用提示；光照搁置。完整记录：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。

## [VERIFY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R7] 2026-10-31 源码编译与现场运行时 LOD 对照

- WorkBuddy 第7轮编译了真实的 LYF_MMO/GM_Game.csproj，输出与中间目录全部重定向到 _temp/dungeon-lighting-resume-20261031/build：0 个错误；增量编译 2 条程序集冲突警告。这是 dotnet 源码编译，不是 HybridCLR CompileDll，也不是包体部署。正式 GM_Game.dll.bytes 未被覆盖。Codex 已核对实际三行源码差异、git diff --check 与已保存的编译结果；输入未变且先前已通过的项未重复构建。
- Codex 在当前 HostPlayMode 魔神塔的对照使用同一主相机、640x360 RenderTexture 与未改动场景：globalLOD 2147483647 时 meanLum 0.00502、暗像素 96.95%；globalLOD 500 时 meanLum 0.10378、暗像素 48.49%。地面变为可见。Camera.targetTexture 已恢复，globalLOD 500 有意保留在 Play 中生效。证据：_temp/dungeon-lighting-resume-20261031/root-runtime-verification.txt、root-before.png、root-after500.png。
- WorkBuddy 的窄范围对照只改了两个真实 bundle shader 实例：逐 shader 500 时 meanLum 0.10407，基线 0.00527；其余 15 个 shader 的 maximumLOD 无漂移；两个 shader 的上限均已复原。600/599/400 对照隔离出有问题的 LOD600 路径；500 实际选中 LOD400。证据：_temp/dungeon-player-regression-20261031/lightmap-binding/shading/evidence-shader-lod-supplement.txt 与 investigation-supplement.md。
- 这些结果支持“修复漏迁的运行时 LOD 入口，并恢复当前采样视角的地面”，但不能证明变体被剥离、整场景渲染正确、包内 DLL 刷新成功、真实重新进场与独立 Player、同包双端流程，也不能证明对其他高档 shader 无影响。本次授权的文档跟进未重复任何编译/渲染/UI/协议套件。完整日志：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。


## [VERIFY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R8-CPU] 2026-10-31 光照剔除候选未验收与编译负载缓解

- WorkBuddy 第8轮将 LYF_MMO/ProjectSettings/GraphicsSettings.asset 的 m_LightmapStripping: 0（Automatic）改为1（Custom），原有 m_LightmapKeep* 值仍为1。这是工作区内未验收的候选，不纳入本次文档提交。SVC 中缺少 LIGHTMAP_ON 条目不能证明最终 bundle 丢失该变体；Custom 包渲染尚未验证此诊断或修复。临时报告中更强的根因断言不作为已接受的证据。
- Editor.log 显示 YooAsset BuildRunner/TaskBuilding/BuildAssetBundles，以及一批 Shader 的完整变体数577536、内建剔除后72192、脚本剔除后15792，随后 starting compilation。截图显示多个 Unity Shader Compiler 进程繁忙。这能证明当时正在编译，但不能证明构建由谁发起、是否已经结束，或负载是否仅由 Custom 导致。MCP ping 未获响应；Codex 在此次恢复中没有另行发起构建。
- 用户报告严重卡顿后，Codex 将观察到的全部32个 UnityShaderCompiler 进程设为 Idle、ProcessorAffinity=3；读回验证32个进程共用同样的2个逻辑核心，机器共有32个逻辑核心。这是临时进程调度，不是项目设置。一项 Unity 编辑器优先级调整返回 Access denied。未结束 Unity；编译进程限速不等于现有构建已取消。
- 国内版 WorkBuddy 会话769bac42-7f9c-4a2b-86a3-1c835f892974（deepseek-v4.1-flash），任务 DUNGEON-PLAYER-REGRESSION-20261031-SHADER-IDENTITY-FRESH，恢复 attempt run-ef355d9b-300b-4869-ad82-0573bade6274，收到 session:cancel。首次响应仍为 working/isProcessing=true，后续原生读回为 completed/isProcessing=false，并有一项 cancelled 工具调用。这确认原生执行已停止，不能据此确认队列已验收或 Unity 编译已停止。恢复和取消不增加开发轮，累计第8轮仍未验收。
- 仍未验证：卡顿是否完全恢复、新构建 bundle 的渲染、实际重新进入副本与独立 Player。本次获准跟进仅写记录，不恢复 WorkBuddy、不重新构建、不重新烘焙、不更改运行时 Shader。证据：_temp/dungeon-lighting-resume-20261031/black-fix-r8/report-r8.md、r8-connection-resume.txt、pause-native-for-cpu.txt、approved-cpu-records/；完整日志：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。

## [VERIFY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R9-SVC] 2026-10-31 新Shader包与序列化SVC核验通过，画面待验收

- 两次原生增量构建均返回Success=true，最终版本2026-10-31-1266-lm-final。Shader SHA256从fd6da1d88e21592d9bb2f93731fcc477f8292151088c4a7696dfa5ab0f6e8df4变为5c1e9712fdb5038ac898154187d5f8c9066b6905534c9aa6f48bca1da9980b38；大小从1986839增至20943010字节，FileHash为3eef2ed242d6b0a354f3cedb4d8e8604，FileCRC为ec9ed307。MD5、CRC、大小、二进制清单MD5与hash文件、JSON和version文件的一致性均通过。CRC按YooAsset小端字节输出核验；最初按普通十六进制整数比较是校验脚本错误，已修正，未改包。
- 直接加载最终哈希包：1份SVC、183个变体；SVC与Shader的AssetDatabase路径均为空，RO/Scene/AllInOne supported=true，Contains(ForwardBase LIGHTMAP_ON)=true；序列化记录为passType4/keywords LIGHTMAP_ON。首次ShaderVariant构造函数因编译包缺编辑器snippet而拒绝，后通过结构体字段赋值及序列化条目完成核验；两种探测都不能证明最终墙面成像。
- 首次构建仍出现既有ShaderGraph PBRDeferredFragment错误，但原生构建最终成功；未修复这些无关Shader。此前GraphicsSettings Custom候选与本任务基线逐字节相同，仍未验收；本次构建不能单独区分SVC和GraphicsSettings的影响。原匿名delayCall一直排队，Codex仅移除已识别的本轮回调后调用一次，避免重复执行；后续最终增量构建仅包含JSON计数修正，日志没有新增Shader编译。
- 交付时服务器版本仍为2026-10-31-1221；未发布、自动进副本或测试Player。黑墙、重新进场、Player及后续LOD400+光图收集流程待验证。按用户不开子Agent要求，由Codex完成本次定向核验和中英同步。证据：_temp/dungeon-lighting-resume-20261031/lightmap-variant-build/final-build-result.json、package-verification.json、bundled-svc-probe.txt；完整日志：项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md。


## [VERIFY][相机][MOSHENTA-CAMERA-COLLISION-20261031] 场景与现场定向验收

- 通过：现场12方向、同帧切换扩展开关对照；遮挡3/3从6.0476拉近至4.5694或3.9962，无遮挡9/9不变，axesRestored=True。最终Geometry掩码排除Default主角。早期Default候选距离缩至约0.3329，判定失败并弃用。
- 通过：场景AssetDatabase导入、控制台error查询无条目、YAML fileID唯一、源文件/暂存版本在扩展及层覆盖之外分别与任务基线/HEAD一致、meta不变、暂存差异空白检查；用户原有修改未混入。
- 未验证：新场景包、重新进场、独立Player、主城移动回归。未构建、未中断Play。证据：_temp/dungeon-camera-collision-20261031/acceptance.json、live-geometry-obstacle-probe.txt、scene-patch-verification.json。完整日志：项目修改日志/2026-10-31-魔神塔相机避障配置修复_CHANGELOG.md。


## [VERIFY][技能][事件][SERVER-ENTITY-EVENT-20261101] 服务端事件与协议验收

- 通过：旧式 .NET Framework 4.8 GameServer.csproj 的显式编译清单已包含全部 3 个新增生产源文件。dotnet build GameServer.csproj --no-incremental -v:m 产生 0 个错误 / 14 个既有警告；直接调用 MSBuild.exe 与 dotnet msbuild 被 WorkBuddy 的命令策略拦截，成功构建使用的是 dotnet build。证据：_temp/server-entity-event-20261101/build-gameserver-r3.log。生产 bin/obj 仍在旧式默认目录且未纳入 Git；临时测试工程、输出与证据都在 _temp 下。
- 通过：EventDispatcherSmoke 的 9 个用例覆盖同事件送达、被不同回调替换、派发中解除、同类型嵌套派发、不同角色派发器隔离、UnInit/销毁与真实施法。环回 TCP 用例以已登记的复合会话进入 SkillCastHandler.OnCall，先收到成功结果再收到开始（技能 10010 / UUID 7 / MP 500 → 480 / CD 已填充），随后 UUID 8 收到拒绝且 MP 仍为 480、300 毫秒内无开始。实际捕获的拒绝码是 SkillStateBlocked(32)，不是已证实的冷却拒绝。证据：任务目录下 tests/smoke1-r3-utf8.log。
- 通过：SkillRoleSnapshotSmoke 拒绝角色 A 的迟到列表并装载角色 B 的有效列表。Codex 独立各运行一次两个测试可执行文件，均 exit 0：任务目录下 codex-runtime-acceptance.json、EventDispatcherSmoke-codex.raw.log、SkillRoleSnapshotSmoke-codex.raw.log。测试在保留开发者既有改动的本地工作区进行，不是干净克隆或部署测试。
- 通过：四个运行中的服务接受真实 TCP 非法会话施法探针并返回 InvalidSession。WorkBuddy 按可执行文件路径停止 Center/Login/Game/Gate 的 PID 58592/59492/65580/64468。Codex 于 2026-11-01 18:38:02 +08:00 终检为 0 个项目服务进程、10110/10120/10130/10140 上 0 个监听。证据：tests/probe-r3-utf8.log、codex-servers-final.json。
- 通过：最终 HTML 的 JavaScript/结构检查与 Codex 的 Edge headless 渲染均无页面错误；本项目边界面板已渲染。证据：mindmap-render.json、mindmap-overview.png、mindmap-project-boundary.png。
- 未验证：完整 Login/Gate 认证后的实网成功施法、Unity 表现、真实多角色广播可见性与性能。环回 TCP 送达不等于完整登录验收。状态互斥等待与真实吟唱时长/打断行为本次移植未实现。
- 执行者：国际版 WorkBuddy deepseek-v4.1-flash，Hub hub-de717361-5b83-4381-872e-03f678bafd41，会话 df323eca-beff-41fc-a54a-840e40ff96d5，6 个真实轮次；Codex 完成最终验收。完整清单、失败历史、Git 回执与精确路径：项目修改日志/2026-11-01-服务器实体事件与OnCall移植_CHANGELOG.md。


## [VERIFY][技能][互斥][CLIENT-STATE-EXCLUSION-20261102] 客户端互斥判定验收

- 通过：真实 GM_Game 与 LYF.SkillAuthoring.ContractTests.Editor 重建退出码 0；报告告警数 626/1734 含既有依赖告警，因此不声称存在干净的告警基线。程序集内可查到新增生产/测试类型；Unity 生成的工程文件不纳入提交。
- 通过：WorkBuddy 隔离 harness 直接编译 3 个生产源文件与真实 StateDefine，只替换 Unity/全局角色环境。它报告 50 条断言，并可选择输出矩阵 dump；Codex 独立运行同一 harness 但不带该可选参数：49 通过/0 失败，退出码 0（codex-smoke.log）。该差异是一条输出路径断言，不是缺少领域测试。发包/预测门口是源码顺序断言，不是观测到的网络流量。
- 通过：原 CSV 字节一致，38 行×38 列，1444/1444 个 Concatenate 值相同。两个原始 StateId 拼写与枚举名不同，已按要求保留；原表自带的逐状态列在 (25,36)/(36,25) 与 Concatenate 不同，故保留但不消费。代码 0/-1 拒绝，1/3 放行，2 放行除非 ingnoreOneLeaveOneEnter=true。已接来源：采集/攀爬/吟唱/钓鱼/跳舞；无当前状态时放行。复用列表避免每次调用重复收集分配；全链路零 GC 未测量。
- 失败/修正：第一轮 Unity Resources 装载找到了资源，但新增的行名校验拒绝了原表第 24 行。第 2 轮移除该检查并按未改动的源格式解析；编译与隔离验证通过。修复后未做 Unity 玩法验证与抓包，也未调用 Editor 的 Run 入口。未启动任何四服。
- 范围、证据与真实执行者身份见 项目修改日志/2026-11-02-客户端RO状态互斥移植_CHANGELOG.md；_temp/client-state-exclusion-20261102/csv-identity.json、matrix-dump.tsv、codex-smoke.log、development-report.md、query-sync-check.md。已批准的 RO 查询双语检查通过，其后输入未再变化。

## [VERIFY][互斥][规范][STATE-INTERFACE-VIRTUAL-20261102] 状态虚函数契约验收

- Codex 将当前 `LYF_Server/GameServer/GameServer.csproj` 重建至 `_temp/state-interface-virtual-20261102/gameserver-out/`：退出码 0，14 警告 / 0 错误。专用 net48 StateMutexSmoke 工程编译为 0 警告 / 0 错误，随后测试可执行文件返回 0，并输出 `StateMutexSmoke: all cases passed`。测试编译仓库中的实际测试源码，并引用本次新构建的生产程序集；测试专用子类未注册到生产环境。
- 覆盖构造固定状态编号、Check/Leave 默认 false、虚函数重写派发、原始表格/空生产注册、禁止/直接/先退出旧状态再进入的转换、退出失败、取消、销毁及回调异常后的事件释放。最终命令和结果见 `_temp/state-interface-virtual-20261102/codex-build-GameServer.log`、`codex-build-StateMutexSmoke.log`、`codex-StateMutexSmoke.log`。输出出现既有 SkillCastingRules 默认策略警告；本次不能证明完整技能配置集成已通过。
- WorkBuddy 早期向既有生产输出目录构建时遇到文件锁定，产生 4 错误 / 38 警告；改用临时输出目录解决了该构建限制。其早期冒烟通过发生在后续临时 GetType 错误之前，不作为最终验收。已保留原生 429、确认 idle、官方取消回执（`remoteStopped=false`）、本地 Qwen 的 1 次修正及 Codex 最终验证；停止证据为原生 429 与 idle，不能把该取消回执作为停止证明。本任务未启动任何服务器，未触动既有服务器进程。未做实网、Unity 运行时或干净检出测试。完整记录见 `项目修改日志/2026-11-02-状态互斥基类与虚函数约束_CHANGELOG.md`。

## [VERIFY][技能][吟唱][SKILL-ZERO-SINGING-FIX-20261102] 零吟唱回归与环回验收

- Codex 将 `LYF_Server/GameServer/GameServer.csproj` 重新构建到 `_temp/skill-zero-singing-fix-20261102/before-out/` 和 `after-out/`，中间路径也位于该任务目录内。GameServer 最终构建为 0 错误、14 个既有警告。`SingingStateSmoke`、`EventDispatcherSmoke`、`EventPoolSmoke`、`StateMutexSmoke`、`SkillAuthoritySmoke` 各自均为 0 错误/0 警告，并使用修复后的程序集运行通过。证据为该目录下的 `verification.json`、`build-after.log`、`build-*.log` 和 `run-*.log`。
- 新增连续施法回归对修复前程序集执行时以 `非吟唱技能不得等待 CastMs，技能 10010` 失败，修复后通过。覆盖三段普通攻击 700/700/700 毫秒，随后技能 10040/10050/10060/10070/10080 分别为 3100/3433/3167/1133/667 毫秒；每次均立即开始，动作占用保持至最后一次 50 毫秒逻辑帧推进，随后允许下一个技能，并且只扣除一次 MP。独立的无表现配置测试保留 `CastMs + RecoveryMs` 动作占用。
- 真实环回 TCP 验证 `OnCall`：无须推进逻辑帧，即先收到成功结果、再收到 Start；每段 700 毫秒普通攻击动作结束后，下一个请求立即成功，UUID、段数、时长和 MP 均符合预期。既有拒绝、合成吟唱的打断/排队终态回包、事件池和状态互斥检查均通过。非网络状态测试有意不提供网关，因此会输出发送失败诊断；这不代表在线玩家传输故障。
- 初始临时测试工程存在一个未使用且缺失的 `Grpc.Core` 引用，并遗漏规则副本；随后移除该引用，补入部署表/规则副本。测试配置补全后，基线回归仍失败，修复后的构建则通过。新增环回接续和无表现配置覆盖后，仅重跑了修改过的测试。本次未操作 Unity，未进行四服登录测试、生产部署或干净检出验收。正吟唱使用 `SingingStateFixture`，不代表真实生产配置路径。源 Excel/部署表的 500/700 毫秒差异仍未解决。完整记录与 Git 回执见 `项目修改日志/2026-11-02-非吟唱技能接续施法修复_CHANGELOG.md`。

## [VERIFY][技能][组合][SKILL-COMBINATION-20261103] 组合技能编辑器与运行验收

- GameServer/CenterServer 构建成功，保留既有警告。当前 Unity Editor 编译并通过 CombinationSkillContractTests，覆盖真实已保存资源、工厂选择、E→2、UUID/确认传播、根技能持有槽位时的子技能移动权限、自然完成和中断清理。界面截图和测试证据位于 `_temp/combo-skill-20261103/`（client-contract.txt、editor-combination.png）。
- CombinationSkillSmoke 通过学习、授予、已有快捷键保留、6300 ms 总占用时间、仅扣除一次 200 MP、仅根技能冷却、共享 UUID、完成和中断验证。SingingStateSmoke 与 SkillAuthoritySmoke 回归构建及运行也通过。无网关测试会按预期输出发送失败诊断，这些诊断不构成真实玩家传输验证证据。
- 真实失败与修正：无法访问内部 schema 常量，已改用当前版本 1；首个客户端测试夹具保留了尚未加载的 MModel，并等待 GO 回调，导致完成断言失败。夹具现已归还并移除空模型，未弱化业务等待行为。源码空白检查通过；保留 Unity 生成 YAML 中空数组的空白。两张 XLSX 表各新增一行数据；客户端与服务端规则 JSON 一致。
- 未完成四服完整登录/视觉验收、碰撞/特效/音效观察、YooAsset 包加载测试、Player 构建、生产部署或干净检出构建。编辑器仅实现逐个子技能预览。所有文件与回执见 `项目修改日志/2026-11-03-组合技能编辑器与E接2.md`。

## [VERIFY][技能][组合][打断][SKILL-CANCEL-FEEL-20261103] 接段与取消回归验收

- GameServer 构建通过，保留既有警告。Unity 编译通过，CombinationSkillContractTests 菜单测试通过：覆盖 E 在 1.5 s 接段、根 UUID/确认/移动归属、自然尾部表现、实际 Stop 清理已登记的未来特效/音效计时器票据、组合→滑步的预测技能状态自转换、起跳取消 E/2/滑步/组合，以及无效或被禁用的起跳保留原动作。
- CombinationSkillSmoke 通过 1500 ms 接段 / 4700 ms 总占用、仅扣除一次 200 MP、根技能 CD，以及全部七个既有技能（10010/10040/10050/10060/10070/10080/10100）的滑步/跳跃取消。还验证了打断后不进入后续组合段、保留 MP/CD、被拒绝的滑步保留当前施法、重复空中状态上报保留当前施法，以及跳跃计数增加时取消当前施法。SingingStateSmoke 与 SkillAuthoritySmoke 同样通过。网关发送失败诊断来自没有真实在线会话的隔离角色。
- 真实修正：首个正式服务端事件测试暴露滑步请求被排队至旧动作结束；直接派发攻击事件并接入经校验的技能状态自转换后修复。一个客户端断言将池中复用的子技能误判为旧施法仍存活，现改为检查旧 UUID。另一个服务端测试夹具仅推进 10 s，而实际冷却为 13 s，现改为推进 15 s。Artifact Tool 将共享空字符串导出为 `32`；最终仅将该工具写入的数值单元格合并回原始 XLSX 包，保留其他内容与样式。部署 bytes 对比确认仅一个字段变化：3004001.SkillStateTimeMs 3100→1500。
- 最终源码与暂存区空白检查通过。证据位于 `_temp/skill-cancel-feel-20261103/`（`server-build.log`、`client-contract.txt`、`unity-console-final.json`、`CombinationSkillSmoke.log`、`SingingStateSmoke.log`、`SkillAuthoritySmoke.log`、`compare-tables.ps1`、`staged-business.json`）。未完成四服完整登录/视觉/音效/碰撞验收、YooAsset 包测试、Player/干净检出构建、权威伤害测试或运行进程重新部署/重启。完整文件清单与回执见 `项目修改日志/2026-11-03-组合接段提速与滑步跳跃打断.md`。

## [VERIFY][副本][实体][技能][RO-DUNGEON-RUNTIME-20261104] 运行时生成、受击与清理

- 用户完成真实登录与组队进入：63 波、54 条初始记录、151 个怪物 + 10 个 NPC + 本地玩家；所有实体对象均属于 sc_dungeon_moshenta_001。真实 CastSkill(10010) 入口使一个处于重叠范围的怪物受击并返回默认状态，同位置的 NPC 不受影响（live-skill-hit.json）。独立的几何检查覆盖窗口外/范围外不命中、每窗口目标去重及下次施法重置（collision-contracts.json）。最初命名为 live-skill-miss.json 的观察实际击中了另一个场景实体，不能作为打空证据。
- Key01 请求第 50 波并新增其 NPC，未启动其他等待中的波次。真实离队回城移除了包含该 NPC 在内的全部 162 个副本实体，并保留玩家（exit-cleanup-final.json）。
- 真实失败：仅按骨骼名称计算哈希的方式失败；原始网格使用完整 Transform 路径哈希，骨架去优化后按完整路径处理。18 个人物预制体继承了禁用的 Animator（92 个实时实例）；最终资源中已启用。进出场景时出现 EntityAttributesComponent 空引用；随后捕获 NPC 属性宿主错配，并修复池化快捷引用/容器绑定。
- 修复后的对象池回归：3 轮 / 36 个实体，容器引用与宿主引用正确。2 轮无需账号的运行时场景循环，每轮创建 151 个怪物 + 10 个 NPC，badAttributes=0；退出时移除全部副本模型，控制台无新增错误。这些循环属于场景/运行时回归，不是额外的完整组队登录测试。33 份预制体、33 份控制器与 90 个蒙皮渲染器的资源检查通过。Unity 编译通过；生成的 YAML 中空值后的行尾空格属于 Unity 标准序列化。
- 未验收 2 个客户端同时在线、独立 Player/正式资源包、怪物血量/死亡/奖励的权威处理、外部 AI/Lua、完整通关或 NPC 交互。Born 状态调度及旧编辑器预览对已迁移人物/NPC 网格的全覆盖仍不在本次范围内。完整记录/文件清单/回执：项目修改日志/2026-11-04-魔神塔运行时实体与技能受击.md；原始证据：_temp/dungeon-runtime-entities-20261104/。
- 俯卧角色/命中特效后续核查：当前初始怪物数量为 75 个死亡姿态 + 8 个受伤姿态 + 68 个正常待机/施法姿态 = 151；NPC 为 5 个受伤姿态 + 1 个虚弱姿态 + 4 个正常姿态 = 10。可见角色资源目录中的全部 33 份待机剪辑均与原始 RO CSV 表及预制体默认 Idle 动作一致；Animator 均已启用。这支持已配置的剧情姿态，不代表新增的实时会话视觉验收。MSkillCollision 仅分发 DamageReceived；编辑数据中的 hitFxList 为空且 hitVfx 未赋值，导出器仍不支持 hitFxList，运行时表现数据也没有命中特效数据。受击动画已交付，命中粒子特效未交付；此处的命中粒子特效与施法特效、受击动画相区分。剧情尸体/伤员中被归类为怪物者仍可命中；排除这些目标及接入命中特效只是建议，本轮未实施这些业务改动。证据：_temp/dungeon-pose-hitfx-20261104/pose-counts.json、original-idle-comparison.json、controller-defaults.txt。完整后续记录：项目修改日志/2026-11-04-魔神塔运行时实体与技能受击.md。

## [VERIFY][副本][技能][索敌][RO-DUNGEON-FORWARD-TARGET-20261104] 前方索敌、手动目标与剧情角色命中排除

- 修改前，在真实组队副本中未指定手动目标时，实际选敌方法 SkillCtrl.GetTargetFilterSkill 对 10010 和 10070 都选中了玩家身后 0.69 m 的尸体，水平朝向点积为 -0.48。观察结束后已恢复先前目标。
- 主 Unity 编辑器已加载编译后的选敌逻辑和 8 个带标记的 ID。独立 Unity 编辑器中的正式 GM_Game 程序集通过 11 项受控检查，覆盖身后/侧面目标及距离更近的剧情目标、转身后重新索敌、手动选择/Tab、前方没有可用目标、非攻击技能、真实物理重叠和池化实体复用。最终检查使用 Unity 导入的资源目录记录，并沿用正常的世界实体移除/复用流程。
- 修复后再次登录组队施法、双客户端同时运行和 Player 构建均未验证。原 Unity 会话在工作期间结束。主编辑器最终处于编辑状态，仅打开魔神塔场景。证据见 _temp/dungeon-forward-target-20261104/。详细记录见 项目修改日志/2026-11-04-魔神塔普攻与技能前方索敌.md。
- 主 Unity 编辑器在 Play Mode 下使用隔离场景和真实怪物/NPC 预制体，将模拟键盘按下和松开的事件加入 Input System 队列，由 PlayerInputCtr.Update 处理。选敌顺序为首个敌人、第二个敌人、再回到首个敌人，长按 Tab 不重复切换。渲染截图显示脚圈随目标移动，超过 2 秒后仍稳定可见。目标可见性、范围、移除和玩家清理检查均通过。场景入口原有的 18 m 半径内，符合条件的初始怪物出生点为 0 个，最近的配置士兵距入口 85.865 m。附近最多 4 个敌人参与切换。编辑器已恢复到仅打开魔神塔场景的状态。本轮未再次登录组队，也未进行 Player 构建验收。证据见 _temp/ro-tab-target-ring-20261104/input-visual.json、tab-first.png、tab-held.png、tab-second.png、entry-range.json。

## [VERIFY][副本][战斗][RO-DUNGEON-MONSTER-HP-20261104] 副本实例隔离、致死命中与 Unity 动作回放

- 17 项回环战斗检查和 108 项队伍回归检查全部通过。同一队伍的两名成员攻击同一只南瓜怪，双方收到的 HP 依次为 240 → 160 → 80 → 0，另一副本实例中的南瓜怪仍为满血。针对已死亡目标、背向未命中、超距未命中、重新进入副本和 Gate 转发的检查均已通过。旧队伍测试读包器无法读取已压缩的入场快照，隔离测试夹具改用生产代码的 NetUtils 解码器。
- Unity 在正式预制体上回放 GameServer 的实录消息，目标先进入 BeHit 再进入 Dead，死亡后不能再被选为目标。14 个导入的动画控制器各自绑定对应的 RO 死亡动作。隔离 RO 场景编辑器程序集后，Windows Player 脚本编译通过。两台 Unity 客户端同时登录、完整 Player 打包和事件驱动的怪物波次均未验证。原始证据见 _temp/dungeon-monster-combat-20261104/。
- 当前工作树编译出的 Windows 热更新 DLL 为 689152 字节。仅使用分支已跟踪文件复现时，已跟踪的 MainView.cs 引用了未跟踪的 DailyTaskWindow.cs，导致干净分支构建受阻。重新生成的 GM_Game.dll.bytes 仅保留在本地，本次功能提交未纳入该无关任务的依赖。

## [VERIFY][战斗表现][跳字][RO-HUD-WORD-ART-20261104] 图集引用、Unity 导入与离线浏览器验证

- Unity 中全部 97 个 Sprite 的名称及本地文件 ID 均与源图集一致。FontAssembly 映射 85 个字符，SpriteAssembly 映射 8 个图标，这两张表均未使用其余 4 个图集切片。可下载的 ZIP 压缩包中，97 张原始 PNG 均与源文件逐字节一致。
- 在 Edge 中验证了六个分支、25 种样式和 38 条变体，并通过时间轴回放、搜索、单个切片下载和完整 ZIP 下载检查。离线页面没有发起 HTTP 请求，在 390 px 宽度下没有横向溢出。游戏内跳字播放和 Player 构建超出本次美术资源任务范围，未予验证。证据见 _temp/ro-damage-hud-20261104/。

## [VERIFY][魔神塔][跳字][MOSHENTA-NORMAL-HIT-WORD-20261104] 服务端伤害分发与普通跳字渲染

- GameServer 和 GateServer 均构建通过。两条独立的队友连接收到相同的 80 点伤害和最后扣除 20 HP 的致死一击，另一副本实例内的第三条连接未收到命中通知。首次入场和重新入场的快照以及未命中的攻击均未产生跳字。服务端 44 项断言通过。
- 两名接收者的消息均经 Unity 正式客户端处理逻辑处理，并在正式 5504 模型上使用正式材质和数字切片渲染。50 项断言通过。重复更新只显示一次跳字，本地 HP 过期时致死一击仍显示实际扣除的 20 点伤害。同一帧原本重叠的多条跳字可以区分，跳字到期或卸载后网格均已移除。在两张可对照的 960×540 首帧渲染图中，跳字与可见模型顶部的间距分别为 17 px 和 15 px。
- 本地 Windows 热更新程序集编译通过，大小为 692736 字节。两台 Unity 游戏客户端同时登录和打包后的 Player 均未实测。证据见 _temp/moshenta-normal-hit-word-20261104/ 和 项目修改日志/2026-11-04-魔神塔普通伤害跳字.md。

## [VERIFY][副本][动画][MOSHENTA-SHARED-ANIMATOR-20261104] 验证魔神塔公共怪物动画控制器的实际播放

- Unity 2023.2.20f1c1 在隔离预览场景中检查了全部 33 个实际运行时预制体，经 `MModel.SetGameObject` 和公共 `MAnimator` 逐一播放 156 项序列化绑定，每个槽位实际播放的片段均与配置一致。兽人 5515 收到正式的 `DamageReceived` 事件后播放 `BeHit`，结束后回到 `Idle`。触发 `Death` 事件后，经 `Update` 和 `LateUpdate` 播放死亡片段。5514 和 5515 同时使用同一基础状态图，各实例的片段互相独立。项目资源中已无对已删除控制器 GUID 的引用。
- Unity 编辑器编译未报告 C# 错误。当前工作树的 Windows Player 脚本在隔离输出目录中编译出 28 个程序集，包括 `GM_Game.dll`。未部署独立构建，也未运行双客户端游戏。验证证据见 `_temp/lyf-monster-public-animator-20261104/unity-clip-playback.json`、`_temp/lyf-monster-public-animator-20261104/unity-entity-events.json` 和 `_temp/lyf-monster-public-animator-20261104/player-compile.json`。

## [VERIFY][副本][怪物AI][MOSHENTA-ORC-W18-AI-20261104] 第 18 波兽人的服务端、伤害与 Unity 回放验证

- 候选 `GameServer` 使用 3 名玩家、2 个副本实例和真实 TCP 数据包，推进 42.9 秒模拟服务端时间，39 项专用检查全部通过。把怪物移开 30 米后，玩家普攻在旧位置打空，在怪物权威当前位置命中。检查覆盖巡逻与等待、受击反击、追击、攻击时刻与冷却、血量、切换仇恨目标、死亡取消攻击、入场快照和实例清理。已有的 17 项战斗及 Gate 检查、108 项队伍检查也全部通过。9 组固定随机取值的样例使用 LYF 目标数据，与 RO 原版 Lua 分支结算一致，覆盖防御、暴击、暴击抵抗、暴伤减免、未命中和伤害浮动。
- Unity 2023.2.20f1c1 在隔离预览场景中，经正常客户端通知与模型流程回放 10 个实抓数据包，17 项检查全部通过。覆盖公共控制器的实际动画片段、本地与远端视角的血量、跨实例消息过滤、死亡、攻击播放速度，以及迟到和已过期的动作快照。未同时运行两个独立发布客户端，也未进行线上部署。证据见 `_temp/orc-wave18-ai-20261104/test-report.json`、`_temp/orc-wave18-ai-20261104/damage-parity.json` 和 `_temp/orc-wave18-ai-20261104/unity-wire-replay.json`。

## [VERIFY][组队][MASTER-TEAM-20261104] 独立队伍服与游戏服回归

- 从本任务独立检出的代码构建 .NET Framework 4.8 解决方案成功。真实 TCP 队伍服回归 114 项、失败 0 项，GS 第 18 波兽人战斗回归 39 项通过。覆盖两个已绑定身份的测试角色、邀请、队长转让、同实例进本、单人退队回城、MS 断线清场、伤害和实例隔离。
- 首次干净检出发现六个 Gate 到 GS 的组队命令缺少注册。修正后干净版本通过。未做 Unity 多账号真人登录、现网切换和跨 GS 迁移。本轮由 Codex 直接验收，证据及复现入口见 `项目修改日志/MasterServer组队迁移-2026-11-04.md`。

## [VERIFY][聊天][RO-CHAT-DEV-20261104] 文字投递与语音播放
- 隔离进程通过 129 项聊天、114 项组队和 39 项副本战斗检查。四个已绑定角色会话验证三个频道、回包与广播分离、真实发言人、网关正式转发和 96,000 字节下载一致。Unity 验证两种气泡、三秒单声道 AudioClip 和空转写。经批准恢复 DailyTaskWindow.cs 和 .meta 后，以提交范围的源码成功编译 Windows Player。
- 未做两台设备真人录音互发、移动端权限实测、实际等待 24 小时的缓存过期测试或生产部署。详见 `项目修改日志/RO三频道文字与语音-2026-11-04.md`。
- 2026-11-05 返修通过提交范围 353 份及本机 354 份源码的 Windows Player 编译、八个启用且可交互页签的引用、五个显隐设置及程序集八项绑定检查。三个恢复方法与 `8513729` 一致，预制体只改九个显隐值。本轮未进行 Unity Play 模式验证。
- 2026-11-05 Unity 2023.2.20f1c1 / YooAsset 1.5.7 对 StandaloneWindows64 的 DefaultPackage 2026-11-05-1016 增量构建通过：2,824 项主资源及全部 1,183 个包文件存在。聊天界面资源仍在，错误的脚本资源包已排除。本轮增量构建无新增 Shader error，仍有一条 HumanTemplate 编辑器资源警告。本次未做全量 Shader 重编、可执行程序打包或运行时加载验证。证据：`_temp/yoo-chat-build-20261105-01a1a67e/verification.json`。

## [VERIFY] [构建][Shader][资源] UNITY-FULL-BUILD-20261105 — ForceRebuild 验收

- Unity 2023.2.20f1c1 / Windows64 / BuiltinBuildPipeline / ForceRebuild / LZ4 / EncryptionNone：`2026-11-05-1144-icons` 用时 741 秒成功；2820 个主资源及 1181 个包与报告一致。最终日志无 Shader/C# 错误、归档写入失败、原生资产索引断言或编辑器对象警告；Console 错误/断言 0。
- 已核对 12 个原报错 Shader 名称完成序列化；UIRoot/Chat 保留；.asmref/.ht/.shadersubgraph 与编辑器收集配置被排除。干净脚本编译及依赖包修改核对通过：Shader Graph 1 个文件、YooAsset 2 个文件。四张 TMP 编辑器图标在排除前单独构建可复现断言。
- 中间构建不作为通过证据：磁盘耗尽、初次遗漏 UV1 声明、旧 PackageCache 路径、回执成功但仍有原生断言、只过滤主资源仍从依赖带入子图、过滤子图后仍有独立的 UGUI 图标断言。真实失败证据保留于 `_temp/unity-force-build-20261105-01a1a67e`。
- 边界：仅当前工作区资源包；未在干净检出重建，未进行 EXE/移动端打包、双设备真人运行或完整场景视觉检查。详情和 Git 回执见 `项目修改日志/Unity全量打包与Shader兼容修复-2026-11-05.md`。

## [VERIFY][组队][TEAM-ENTRY-CONFIRM-20261105] 确认协议、真实队伍与预制体验证

- 工作区及 HEAD 加本次叠加文件的隔离构建均通过：组队/网关 143 项、聊天/语音 129 项、战斗 41 项。隔离输入包含 HEAD 中跨客户端引用的共享代码、导航网格和本机包缓存。MS 为真实独立 TCP 进程，角色身份及客户端连接为测试夹具，不是真人登录。
- 现场证据为两名缓存成员、零个生成行、三个空引用；补上真实绑定后显示两名实际成员并截图。Unity 编译通过，Console 错误为 0。预制体/协议/按钮 15 项与实际控制器方法 3 项通过，包含重复询问保持打开、退队关闭。已查看临时场景渲染结果，测试实例已移除，未保存测试场景。
- 首轮旧正式路由用例仍预期立即入场，已改为明确答复。隔离夹具曾漏跨目录依赖，补齐 HEAD 输入后通过。UI 检查首次执行到旧编译的状态标签映射，刷新后通过。本机切换修正 PowerShell 文件名数组并等待旧进程释放句柄后，已核对最终二进制哈希及连接。
- 未重建 Player/移动端包，未做新版双设备真人确认。完整清单、失败、部署及 Git 回执：`项目修改日志/队友显示与RO进本确认-2026-11-05.md`；证据：`_temp/team-entry-confirm-20261105/`。

## [VERIFY][副本][场景编辑器][MOSHENTA-EVENT-FLOW-20261106] 权威事件流程与 Unity 运行验收

- 开发者自检：GameServer/GateServer 构建 PASS（存在既有警告）；隔离进程中的真实 TCP + protobuf/CRC 封帧检查，34 项断言 PASS。主 Agent 重跑 `_temp/moshenta-event-flow-20261106/run-server-smoke.ps1 -SkipBuild`：PASS34。覆盖初入战斗数为 0、事件只激活一次、wave18 巡逻/当前位置命中、门 0/1 战斗中拒绝与脱战后目标位置、采集 41→42→62→门户 5、Boss 墙线段拒绝与位置纠正、晚加入、胜利出口 35 的实际请求、团灭清理、另一实例隔离及释放。
- 主 Agent 使用 Unity2023.2.20f1c1、实例 `LYF_MMO@47ea91c229a904f3`：最终 Network.dll 刷新/编译 error0；真实 TCP 快照经 TeamData，再由实际 PlayMode 的 MSceneDungeon.Start/YooAsset 异步加载：初入 0 个战斗实体→前线 52→Boss 69，预期 SpawnIds/HP 和公共动画控制器均符合；三名 Boss 的 HP0 后，空气墙 23 移除/出口 35 可见；重置后战斗实体与墙为 0、出口禁用→重试 52，实体身份不重复，即本轮 Play 序列 0→52→69→0→52。实际 TeamCtrl 失败回执把本地实体纠正到权威位置。证据为 `root-runtime-check.txt`、`server-packets/types.txt`；合成的本地实体只用于回执测试，不是已登录的多人客户端。
- 客户端隔离检查：靠近显隐、正式 MainView 按钮引用/事件、门户 4 粒子系统、墙 BoxCollider 和 carving。CharacterController.Move 在 z=-1.09 被阻挡，禁用墙后到达 z=4.91。XML 编辑器往返读取为 336 个节点/254 条连线/缺失 0/目标 7；阶段预览通过，使用加速计时器。NavMesh 采样成功不等同完整路线通行。主 Agent 已停止 Play、返回干净的 Scene_Boot 并移除仅运行期的测试对象；没有保存正式测试组件。
- 真实自检修正：服务端 RunGroup 最初匹配 AIEvent 而非原 WaveEvent，已在最终 34 项 PASS 前修正；门 1 预期目标改为原 XML 数据；客户端 out 类型编译错误已修正。主 Agent 的反射探针最初命中重复的 Global/MonsterEntity 类型，改为指定已知程序集/基类，无须修改业务。完整人工联机游玩、独立构建/部署、原版完整 AI/时序仍未验证。日志为 `项目修改日志/2026-11-06-魔神塔事件刷怪与Boss封路传送.md`；子 Agent 交付与原始证据位于 `_temp/moshenta-event-flow-20261106/`。
- UI 修正验收：新的 GM_Game 源码经外部编译，C# 错误为 0；实际已加载的新 TaskCtrl/TaskMgr 在隔离状态中执行检查，之后恢复状态。进入副本及普通 SetTasks 后，普通任务对象/选中项保留；阶段进度、下一未完成目标、完成、重置和退出均保留普通任务。正式既有 TaskWindow 和 QuickTaskPanel 能渲染副本/普通任务行；两份正式预制体均不再包含独立 Objectives 子树；门户点击/隐藏仍有效。主 Agent 已检查 8 文件任务差异、截图与任务选中调用路径。证据：`_temp/moshenta-task-ui-fix-20261106/verification.txt`、`task-ui-preview.png`、`delivery.md`、`file-manifest.json`。截图在预览场景中使用显式测试进度 3/18，不是实时玩家进度。
- 真实失败、恢复与边界：首次外部编译响应文件指向错误程序集（缺少 AotStubs）；改用实际 GM_Game 响应文件后编译通过。第一次暂存暴露出尚未提交的任务 UI 枚举/布局修改；原开发者在第 2 轮提供共享页值兼容修正及纯 HEAD 投影，未覆盖用户修改。工作区与 HEAD 投影两套源码均编译通过；两者的实际 TaskCtrl/TaskMgr 页映射和普通任务保留检查均通过。自动 ForceDomainReload 曾使原实时会话 Play 暂停，实体/重力绑定为 null；已恢复偏好值 2 及隔离测试状态，准确的重载发起源仍未确认。用户明确批准重启后，主 Agent 执行干净登录、选角、组建单人队伍，并通过真实挑战入口进入 sc_dungeon_moshenta_001。收到的目标“前往前线与部队会合 0/1”与普通任务一同显示于既有任务窗口/详情和快捷追踪栏；主 Agent 使用该实际收到的条目调用既有任务选中处理。Play=true、paused=false，独立 Objectives 子树不存在，console errors=0。`root-live-acceptance.json` / `live-task-ui-final.png` 记录的是实际运行入场，不是模拟阶段进度。本次修正没有重跑服务端 34 项，也不宣称已完成完整副本通关、双客户端游玩、发布构建或完整任务玩法验证。


## [VERIFY][副本][Luban][DUNGEON-TARGET-LUBAN-20261106] 原表一致性与当前Section验收

- 配置子Agent逐项比对原CSV的554行×5字段=2770个值；实际生成读取器解码全部行，cfg.Tables加载17张表，查询API通过，原有48份生成文件未变。重新生成后，两份新增C#文件与bytes的哈希一致。Network/GameServer/GateServer构建通过，保留既有警告；客户端与编辑器的工作区和仅含本任务的HEAD投影均编译通过。未重新生成无关任务表。
- 服务端子Agent与主Agent各运行一次真实隔离TCP烟测，均为PASS50。覆盖保留原34项检查，并增加原始137–143顺序、提前/错误/重复事件、采集41与波次42完成区别、Boss部分/全部死亡、晚加入、实例隔离与重置。主Agent审查实际代码和原recv-event语义；未新增协议或覆盖全部目标类型的框架。
- Unity中实际Network/LubanMgr加载554行和七个XML引用，旧目标定义已不存在。真实TCP报文在临时隔离状态下经过实际TaskCtrl/TaskMgr：入场137、前线138、Boss143、胜利、重置137、退出清理及普通任务保留均通过。正式任务UI已渲染，主Agent检查截图。编辑器加载真实表与引用，dirty=false。临时状态和预览已清除，用户已托管的编辑器窗口保留。
- 主Agent更新本地GameServer的五项构建/表产物并重启原GateServer恢复连接，已记录源/目标SHA。真实登录、角色进入、单人队伍与挑战入场收到137，表文案与任务文案一致，Play=true/paused=false/errors0。证据为root-live-verification.txt/root-live-ui.png及deployment.json。未记录凭证。
- 真实失败：未托管测试EditorWindow.Close抛异常，测试清理改用DestroyImmediate，未因此修改业务。Windows PowerShell 5无法解析UTF-8无BOM脚本；正式生成器补BOM后解析错误由77降为0。本地部署在停止进程后首次遇到临时可执行文件占用；确认旧进程/监听均不存在后复制已校验产物并成功重启。完整手动通关、双客户端实机游玩、发行/生产环境及其他目标类型仍按完整日志记载，未验证或未实现。

## [VERIFY][副本][Luban][DUNGEONS-CONFIG-PROGRESS-20261106] 北方异变之森目标验收与魔神塔回归

- 配置子 Agent 将 462×56 + 50×3 工作簿数据单元格及真实二进制读取值逐字段对照 RO 原表；19 张表和两组查询 API 加载通过，52 个旧文件哈希不变。Windows PowerShell 5 实际生成、6 个生成物确定性和 Network 编译通过。首次遗漏新增 bytes 的 Content 项导致 `Init` 失败，补齐后完整复验通过；组目标注释由“候选”修正为同组全部目标同时激活并全部完成，仅改注释、bytes 不变，Network 重编译一次。
- 服务端工作区通过新增 54 项和既有魔神塔 50 项。主 Agent 独立构造 HEAD 加本任务改动的组合，补齐 13 份基线链接源码/导航输入后两服编译通过；主 Agent 自行执行 TCP 验收，魔神塔 50 项通过，修正损坏投影后通用 54 项通过。覆盖真实 MS 的 4060→4050→4051 入场、缺资源失败保留旧状态、空旧实例清理、同实例重入/晚加入、真实碰撞死亡/采集、237 部分计数、246=247、重置及两张原分支图。
- 主 Agent 退回 SceneAI 展平实现：旧代码真实违反原 Selector 优先级；保留布尔条件与首成功语义后，8 项专项通过并并入通用 54 项。41 条保留映射逐项对照原顺序、条件、结果，9 份正常路径 wire 哈希未变。另发现 3 份无末尾换行的北森资源投影串入下一份 diff 头；子 Agent 修正标准无换行补丁标记并检查全部 22 份投影文件。失败与修复证据均保留在 `server/` 和 `root-server/`。
- Unity 实际加载 462 副本/50 组，回放真实 TCP 包 234、237 部分进度、246+247、其中一个目标完成、250、reset、4060；既有两个任务界面同时展示双目标和普通任务，保留选择与页签，退出只清除副本目标。预览状态恢复，正式场景未污染；实际编辑器 4050/4060 原 XML 关联和 `dirty=false` 通过。源方法隔离检查与实际游戏 UI 操作确认：4050 缺资产不发入场请求、不改变当前实例。
- 确认无游戏客户端后部署本机 MS/GS 构建产物及必要配置/依赖；Gate 二进制保持原样并重启。停止后短暂的 `Network.dll` 占用，在确认旧进程及模块持有者消失后完成复制，17 项产物源/目标 SHA 一致。通过新服务真实执行 Boot 登录→角色→一人组队→4060，收到原目标 137，控制台错误 0；北森缺资产提示后保持主城实例 0，随后魔神塔目标 137=0/1、原表标题“达纳托斯之战 · 和前线部队回合”，`Play=true`、`paused=false`，Unity 留在魔神塔。证据：`root-live-verification.txt`、`root-live-moshenta.png`、`client/ui-replay-verification.txt`、`client/ui-both246247.png`、`deployment-final.json`、`root-server/`。
- 测试工具实际修正：初始 HEAD 快照遗漏 13 份基线链接输入，补齐时未改业务；首次操作误选未激活 UI 模板，改为实际活动窗口后完成验证，未为此修改业务。证据未写入账号或密码。完整 51 文件清单、失败和 Git 回执见 `项目修改日志/2026-11-06-DungeonsTable与通用副本目标推进.md`。未验证/未实现：北森完整客户端资产、NPC 剧情时间轴、完整 Boss 玩法、游戏内对话/分支执行器、完整人工多人通关、发布包和线上环境；夹具只在原事件边界补发缺失剧情输入，其来源见 `server/wire-provenance.json`，不称自然通关。

## [VERIFY][场景编辑器][UI][SCENE-EDITOR-LOCATE-20261107] 离线展示检查与隔离提交验证

- 最终11个编辑器源码使用267个实际Unity/项目引用编译，0错误、0警告。临时控制台加载该真实程序集，366条断言通过，其中336条为节点标题检查；覆盖Wave 18/44、NPC及逻辑节点、真实触发连线及条件、来源与目的地、事件组来源和XML只读行为。主Agent另行编译排除已有菜单改动的提交投影；差异空白检查和新增meta GUID检查通过。
- Wave 44的实体编号为5506，共5只怪物、5个刷点。楼梯触发器为区域1玩家交互 → 角色不在战斗中（`valid_state=12`、`opposite=false`）→ 场景4060目的地 `(75.3,65.9,106)`。WaveEvent来源为Wave 62 → 组12、Wave 63 → 组34；AI时机数据不含事件名称字段。
- 首次测试预期错误地遗漏了第四个战斗条件节点；保留两次失败/诊断执行，并按真实XML修正测试夹具。定向审查修正了多行高度、触发组及条件解释、NPC预览能力表述、已知战斗状态表述、事件来源说明以及Play模式同地图定位。最终XML SHA256保持为 `97152c11ec44d856c5b6defca3db6156ab961a6f5fe9ac3f3c1604faf0405b3f`。
- 证据位于 `_temp/scene-editor-locate-20261107/`：`delivery.md`、`offline-editor/receipt.json`、`helper-test/results.json`、`directed-review.json`、`head-projection/receipt.json`。尚未验证Unity实机导入、UI、场景导航、预览及发布行为。完整范围和Git回执见 `项目修改日志/2026-11-07-场景编辑器怪物识别与触发定位.md`。


## [VERIFY][场景编辑器][副本][SCENE-EDITOR-STAGE-FLOW-20261107] 阶段关联、可读条件与独立提交

- 最终 13 个编辑器源文件使用 267 个真实 Unity/项目程序集引用编译通过，错误和警告均为零。实际编译后的解析器测试通过 27 项关键行为/可读性检查，以及 924 项结构检查（逐一核验 462 条副本行的唯一键和有效后继）；这是离线数据检查，不是端到端业务通关。主 Agent 独立编译了排除既有菜单改动的提交投影；新增 meta GUID 无重复，差异空白检查通过。
- 证据覆盖 Moshenta 七个阶段、137->138 生成时序、altar41/42/62 与 portal5、Boss34/35/46/63 与 wall23/portal35、Northern Forest 实际 XML 事件结果顺序与同时存在的 targets246/247、真实分支 row2809、XML 不匹配处理及源文件只读行为。两份 XML 输入均未改动。面向用户的文案返修保留了每个阶段的目标/后继/波次/机关关联及顺序；技术事件、源路径和黑板证据保留在可展开的 Notes 中。
- 临时测试程序的一处括号错误在检查通过前已修正；正式解析器编译始终通过。第一版 UI 在每次布局时重建；隔离运行环境中测得全图 Build 平均约 12ms，因此最终 UI 仅在相关内容变化后刷新。此结果不是 Unity 内帧耗时测量。主 Agent 驳回了默认 `dungeonfinishtarget` 文案；数据实现仅经过一次正式返修，目前展示实际进入区域或所需波次操作。
- 证据位于 `_temp/scene-editor-stage-flow-20261107/`：`data-test/results.json`、`snapshot.json`、`data-manifest.json`、`ui-source-check.json`、`data-compile/receipt.json`、`head-projection/receipt.json`。1100x650 布局几何已按源码核验；Unity 实机视觉/布局/点击/导入/场景定位、完整手动通关及发布尚未验证。完整文件与 Git 记录见 `项目修改日志/2026-11-07-副本阶段流程视图.md`。

- 后续返修（2026-11-07）：Unity Editor.log 和 Bee 输入证实了“无效的 33 字符 meta GUID -> Flow 脚本被忽略 -> CS0103 -> 仍加载旧窗口”的链路；此前只检查 GUID 唯一性，未验证长度。修复后的 GUID 格式有效、唯一，且 Assets 中没有旧 GUID 引用。随后 Cecil 确认新的磁盘编辑器 DLL 中包含 `ROSceneStageFlow`、`ROSceneStage`、`editorView`、`RefreshStageFlow` 和 `DrawStageFlow`。
- 中文端口标签改动使用 267 个真实引用编译 13 个编辑器源文件通过，错误和警告均为零；主 Agent 核验了限定范围的差异及空白检查。Unity 整体编译仍因 Navigation 导出/预览中的 CS0433 失败：GM_Game 和更新后的 MMoRpgCommon 均包含被链接的导航类型；最新记录的刷新中域重载次数为零。标签、布局、点击和阶段 UI 尚未在 Unity 中验证。先前两次桌面操作尝试被用户按 Escape 中止；用户限制仅使用虚拟桌面 1 后，只读取了窗口清单，没有发送 UI 输入。完整失败/返修细节仍记录在同一份已授权任务日志中。

- 阶段卡片返修验证（2026-11-07）：13 个 UI 编辑器源文件使用 267 个真实引用编译通过，错误和警告均为零；12 项限定范围的源码检查通过，主 Agent 已核验实现差异。源码尺寸检查覆盖 1100x650、244 像素阶段目录、正常字号，以及真实 Boss 完成条件（88 字符/源码两行，未截断）。这些是源码/布局检查，不是 Unity 渲染截图。解析器、配置表和运行时未改动，也未重新遍历测试全部 462 行。
- 默认服务端 Common 与 BuildForUnity Common 均构建通过（各有 4 条既有警告）。客户端变体恰好移除 8 个重复 Navigation 类型；其余 1,356 项 public/protected API 条目和 Gene=4 保持一致；两个客户端 DLL 哈希一致，75 个受保护的 meta/插件/运行中服务端文件未变。LYF.Navigation.Editor、Assembly-CSharp-Editor 和 RO.SceneEditor.Editor 的真实 Unity rsp 编译均通过（仅主编辑器有一条既有 TMP 警告）。首轮尝试因缺少既有导航引用而报 CS0006；将依赖编译到 _temp，并仅替换该临时验证引用后通过，未写入 Library。新 bat 定位片段执行成功；未运行完整服务端分发。
- 发布时间为本地 20:19:08，而最后观察到的 Unity 日志时间为 20:11:47；实机刷新/重载、1100x650 渲染布局、阶段/地图点击、完整游玩及发布验收仍未验证。证据：`_temp/scene-editor-stage-flow-20261107/stage-cards/`，重点包括 ui-source-check.json、offline-editor/receipt.json 和 compile-audit/delivery-receipt.json。桌面 1 输入确认仍待用户答复。

- MCP 实机验证（2026-11-07）：读取现有实例、窗口和状态，随后检查实时窗口并捕获其自身 GUIView 缓冲区，未激活 OS 窗口。初始状态为 editorView=0、selectedStageKey=entry、dungeon4060，共 10 个条目（含高级未归属条目），具有实际 target137–143 后继链，GUID 为有效的 c12b495622374f6a951cdb097a1ffa54。通过既有方法成功选择并重绘 stage2 和 stage7；真实截图展示完整目录、默认无连线图、波次/机关卡片及固定完成条件/下一步页脚。stage7 完成条件完整列出了全部所需 waves34/35/46。PNG 编码时已修正捕获内容的 Direct3D 行顺序；这些是真实窗口截图，并非生成的示意图。
- 未清空 Console。最终仅查询错误时，仍包含验收前已有的同一条历史无效 GUID 错误；未观察到新增选择/渲染错误。本次证实了实机重载以及 1480x708.33 尺寸下的渲染，不代表物理鼠标命中测试、场景/地图定位、1100x650 渲染或完整玩法已通过。此前在用户确认 desktop1 后，CUA 尝试却激活了 desktop2，被用户按 Escape 中止；随后的 MCP 验收未发送 OS 键盘/鼠标输入，也未使用 Focus/Show。证据为 _temp/scene-editor-stage-flow-20261107/ 下的 mcp-live-acceptance/window-state-initial.json，以及同目录的 stage-entry.png、stage-2.png、stage-7.png、console-initial.json 和 console-errors-final.json。

## [VERIFY][基因][GENE-RUNTIME-20261107] 基因运行模块验收

### 最新补充验收

- 真实 Luban 将六个 Sheet 合并到 TbGene；独立 XLSX 校验确认 80 个唯一 ID、同一字段结构、跨页引用有效、初版 69 条数据值不变。证据：`_temp/gene-tabs-20261107/luban.log`、`table-checks.json`；已核对表格预览。
- 对真实生成表和服务端通过 351 项断言，覆盖全类型组合、嵌套事件／次数状态保留、最后一次先执行再移除、刷新补次、祖先场景过滤，以及循环引用失败且不插入半成品。证据：`_temp/gene-tabs-20261107/runtime-tests.log`。
- 服务端编译和客户端独立编译通过，客户端 367 份源码、164 个警告、0 错误。证据：`_temp/gene-runtime-tests/build.log`、`_temp/gene-tabs-20261107/client-build/build.log`。现有项目警告保留；未做 Unity 场景战斗、技能接线、网络或数值平衡验证。
- 表格工具没有公开的改名 API：数据及格式由工具编辑／渲染，再仅修改 workbook.xml 中三个原页签名称；XML 回读及真实 Luban 生成已验证最终文件。Codex 只检查本次双语增量。

### 初次验收事实（历史）


- 真实 Luban 生成通过；独立读取 XLSX 确认共 69 行、原 1～23 条所有数据单元格保留、三个页签名称及其他页签数据保留；客户端组件改名后 Unity GUID 不变。证据：`_temp/gene-20261107/delivery-checks.json`、`diff-check.log`。
- `Tests/Gene/run.ps1` 对真实表与已编译服务端通过 194 项断言，覆盖来源、叠层上限、刷新、周期、大帧到期、组合、状态、场景切换及真实服务端实体初始化／更新／销毁。证据：`_temp/gene-20261107/runtime-tests.log`、`_temp/gene-runtime-tests/build.log`。
- 服务端与 366 份源码的客户端独立编译通过。客户端最初因只同步 Net/MMoRpgCommon.dll、Common/MMoRpgCommon.dll 仍为旧版而失败；两处既有副本同步后通过，164 个警告、0 错误。原有警告保留；日志：`_temp/gene-20261107/server-build.log`、`_temp/gene-20261107/client-build/build.log`。
- 未执行 Unity Play 模式战斗、技能施加、网络同步或数值平衡验证。周期结果需要战斗模块消费，控制／属性查询不自动替换现有战斗逻辑。Codex 只检查本次双语增量，未由外部 Agent 执行。
- 完整证据、用量边界及 Git 回执：`项目修改日志/2026-11-07-Gene基因表与运行模块.md`。

## [VERIFY][场景编辑器][副本][STAGE-INLINE-EDIT-20261107] 阶段内编辑、序列化与依赖检查

- 最终 14 个编辑器源文件使用 267 个真实引用编译通过，错误和警告均为零。真实辅助实现测试通过 46 项核心断言、8 项定向补充断言和 9 项直接自身依赖断言（共 63 项）；10 项源码检查及差异空白检查通过。未重跑未改动的既有测试集。新增 meta 的 f29c04c639e448919e7d48ce1bc70d61 是有效、唯一的 32 字符 GUID。最终哈希与 delivery.json 及编译回执一致。
- 测试使用真实编译的编辑器 DLL 和实际临时 Moshenta XML/Luban 表，覆盖 section2/Boss 出生前置条件、空阶段 sections3/6 中先目标后波次的顺序、保存/重载及未知字段保留、最后使用者清理、机制复制/删除时保留组/区域/共享节点、联盟清理、实体目标影响和改变出生来源后的归属。正式 XML/场景哈希未变。运行时代码确认先处理 target，再处理 wave/Event 和 ActivateWave；该源码追踪不是实际游玩验证。
- 主 Agent 曾退回首次追加后不可见的编辑面板和含糊的模板菜单；目前面板在顶部打开，模板具备名称且可唯一识别。最初认为 Wave41 是唯一生产者的测试假设，已根据实际另有 SummonDemon 生产者的事实修正。随后审查复现真实 Wave34 -> Wave63 自身依赖问题：保存会移除自连并改变前置条件；round2 在修改 XML/dirty 状态前拒绝该操作，并通过 9 项限定范围断言。证据：test/results.json、extra-results.json、self-dependency-results.json、offline-editor/receipt.json、delivery.json、audit/review-receipt.json。
- MCP 就绪/未保存状态检查确认现有窗口无未保存改动。仅请求脚本编译未导入新代码；强制刷新随后暴露 GM_Game 当前 6 条 GeneRuntime.cs CS1061 错误，涉及 Target/TriggerChance/Cooldown。生成的 Gene.cs 已具备这些字段，但实际客户端 Network.dll 仍为旧版本；Common DLL 与此无关。该次检查时，实机窗口仍缺少 ROSceneStageEditing，因此未宣称新 UI 或实机保存成功。用户要求等待，而不是向并行任务发送消息。证据：mcp/console-after-import.json、type-reflect-result.json、audit/gene-import-blocker.json。已准备的 MCP 测试夹具脚本尚未执行。
- 待验收项目：实机新控件/编辑/保存/重新打开、物理按钮命中、地图定位、完整手动副本通关及发布。本任务未发送 OS 鼠标/键盘输入，未使用 Focus/Show，也未切换虚拟桌面。完整文件/Git 记录：项目修改日志/2026-11-07-阶段页波次与机制直接编辑.md。

- 最终 Unity MCP 验收：新加载的模块为 b10b7dce-88c6-4989-b26b-22d9df12203e（helper=True）。真实 DuplicateStageWave 创建了临时 Wave64，带有 HuiHeBuDui 且 count3；SaveChanges/重载后两者均保留。真实 CreateStageRuleWave 在 phase3 创建 Wave65，使用 StageEditorWave_65，缺失连线为零。真实 DuplicateStageMechanism 复制了 phase3 的入口到二层机制；保存/重新打开后，目的地 X=77.25 和 phase3 归属均保留。通过正式辅助实现删除三个临时根后，恢复 336 个节点/254 条连线，原始节点数据深比较一致，无私有信号且缺失连线为零。最后通过 LoadConfig 恢复无未保存改动的正式 Entry 视图；受保护的 XML/场景哈希一致。证据：mcp/type-live-result.json、fixture-wave-result.json、fixture-empty-stage-result.json、fixture-mechanism-result.json、fixture-delete-restore-result.json 和 protected-final.json。
- 真实窗口缓冲区图像 live-wave.png 和 live-mechanism.png 确认了固定新增按钮及顶部行内表单。最终 console-final.json 中有四条位于 MCP ExecuteCode.cs:275 的 RenderTexture 释放错误；该结果中没有 Gene 编译错误。不得报告 Console 无错误，也不得宣称已做物理鼠标/按钮测试。未保存正式场景/配置文件；完整玩法、地图操作、完整构建及线上使用仍未验证。


## [VERIFY][装备][基因][EQUIP-GENE-20261107] 装备与基因验收

- 通过 618 项不重复断言：基因 452 项、装备/战斗 47 项、服务端事务 49 项、实际回环网关转发 12 项、客户端模型 58 项。Codex 已按最终源码重跑 49 项事务断言，这次重复执行不计入 618 项。Game/Center/Gate 构建通过；独立 371 份源码的客户端构建为 0 错误、164 条既有警告。
- 已核对真实 Luban/protoc 生成结果。XLSX 校验保留原 80 条基因和 12 条装备记录，验证总计 107 条基因、15 条特技配置，并保留无关预制体块。源文件与哈希见 `_temp/equipment-gene-20261107/task-files.json` 及子 Agent 交付文件。
- Codex 同步客户端 Common/Network DLL 后，现有 Unity 2023.2.20f1c1 编辑器已完成编译和域重载，编译错误为 0。真实隔离预制体预览核验了 11 个详情引用、MainView 关联、槽位/卸装回调，以及角色面板 22 个引用和 16 行显示。截图与结果见本任务临时目录下的 `unity-acceptance/`；未保存场景，未向正式资源添加测试组件。
- 预览清理最初产生 4 条 RenderTexture 目标相关错误；测试辅助工具改为先解除相机目标再释放临时纹理，随后一次预览没有新增错误。未清空 Console。首次截取错误场景及 Windows PowerShell 调用失败的证据仍保留；最终使用 pwsh 运行事务测试通过。
- 未进行真实数据库迁移/保存后重登、已部署服务端到 Unity 的玩法联调或数值平衡验证。隔离角色测试中的无会话发送失败属于预期边界；回环网关覆盖不等于部署环境覆盖。完整证据和限制见 `项目修改日志/2026-11-07-装备穿脱基因与特技.md`。

## [VERIFY][场景编辑器][副本][STAGE-BINDINGS-20261107] 关联持久化与共享节点验收

- 最终离线编译：15个源文件/267个引用，0错误/0警告。真实正式辅助实现检查通过40项绑定用例、18项保留原字节的迁移用例和9项定向波次 ID 修复用例（共67项）。首轮10项源码检查和最终7项限定范围的提示/范围检查通过。未重跑未改动的测试集；测试引用真实编译的编辑器 DLL。
- 保留失败经过：通过任一高级字段编辑器将波次 ID 从64改为65，会丢失阶段归属，删除后还会留下失效的 wave64 引用。审计使用真实辅助实现复现了该问题。目前两个编辑器回调均先捕获旧 ID，再仅更新阶段绑定引用；9项补充用例验证了保存后的双阶段引用及清理。证据：audit/wave-id-repro/result.json、audit/review-final.json、test/rename-results.json。
- 主 Agent 的实机 MCP 在临时 XML 上调用真实 SetStageNodeLinked 和新增/复制 API：将 Wave18 的展示归属从阶段2移到阶段1，而 HuiHeBuDui 保持不变；将入口机制关联到阶段1，同时保留入口关联；保存/重新打开；清空阶段1并确认仍为空；复制后删除共享临时机制，并确认全部引用已清理。原始336个节点/254条连线及全部运行时节点 Data 保持一致，缺失连线为零。正式 XML 哈希仍与交付一致，正式窗口已恢复为 dirty=false。未执行物理点击。
- 主 Agent 截图发现切页后“this stage”状态文案存在歧义。Round3 仅将两处字符串改为使用实际 stage.Title；真实跨页回调补验通过，最终渲染的正式 Entry 视图显示新增关联/解绑控件，最终 MCP 错误 Console 查询返回0条。证据：mcp/acceptance-edit-result.json、acceptance-clear-restore-result.json、acceptance-message-result.json、live-bindings.png、formal-protected.json 和 console-final.json，以及 round3-message-check.json。此前审计源码哈希覆盖 round2；最终 Window 增量由主 Agent 结合最终编译回执核验。
- 迁移回执证明在偏移779867处精确插入3334字节，包含9个绑定阶段/61个波次引用/23个机制引用；原始字节及用户 y=18.9807758 均保留。完整日志：项目修改日志/2026-11-07-副本阶段关联显式配置.md。未完成完整玩法、物理按钮/地图操作、打包或线上验收；保留了原始出生/触发行为，未用阶段关联列表替代这些行为。
- 2026-11-08 XML 注释续作（同一任务）：在 LYF_MMO/Assets/GM_Game/Resources/Dungeons/Moshenta/RO_DaNaTuoSiZhiZhan_EZ.txt 的 editor_stage_bindings 周围新增19条独立中文注释（交付时行19450-19569），说明目标名称、稳定阶段键、波次/节点 ID、共享关联及未改变的运行时触发行为。开发自检8项通过；仅移除新增注释即可逐字节恢复原文件，UTF-8 BOM、CRLF、用户节点 y=18.9807758，以及9个阶段/61个波次引用/23个机制引用均保留。主 Agent 核验实际差异仅为插入，并确认 XML 数据一致；暂存时排除了既有用户位置改动。目标137文案已按当前正式二进制表核对。证据：_temp/scene-stage-bindings-comments-20261108/verification.json，以及同目录 target137-current-table.json、root-review.json 和 git-receipt.json。本次仅注释续作未执行 Unity 编译、MCP/桌面操作或副本玩法验证。完整日志仍为 项目修改日志/2026-11-07-副本阶段关联显式配置.md。

## [VERIFY][基因][GENE-MINDMAP-20261108] Gene 思维导图验收

- 生成器验证了 33 个唯一节点、38 个实际存在的源码/配置入口、可定位的代码标记及有效的节点引用。`node --check _temp/gene-mindmap-20261108/script.js` 通过。本次仅改动文档/HTML，未重跑游戏构建及历史 618 项断言。
- 实际 Edge 检查通过：属性公式/详情及 GeneRuntime.cs:167 代码摘录；折叠为 9 个节点并展开至 33 个；AddGene 搜索命中 4 个节点；破甲示例从第 5 步推进到 8 秒到期步骤；缩放使 viewBox 改变，Fit 恢复视图；捕获的控制台错误列表为空。最终完整导图已保留为浏览器交付结果。
- 初始 1112px 布局的导图文字偏小；刷新后已检查 <=1300px 时上下排列的布局。一次 PowerShell/Python 引号 SyntaxError 和一次 summary/button 定位器不匹配已解决，未改动游戏逻辑。拖动、滚轮缩放及剪贴板复制未分别进行浏览器验证。
- 证据：`_temp/gene-mindmap-20261108/verification.txt`、`browser-preview.jpg`、`atlas-data.json`，以及同目录下的 token 报告和原始会话压缩计数。预览为临时本机回环服务。真实数据库/重登、部署后的游戏联调与数值平衡仍不在已验证范围内。
- 完整记录及 Git 回执：`项目修改日志/2026-11-08-Gene基因思维导图.md`。中英检查仅覆盖本次新增的两条 GENE-MINDMAP-20261108 记录；未改动历史段落。

## [VERIFY][基因][GENE-NODE-FACTORY-20261108] 直接修改真实角色与清理验收

- 已通过：使用 107 条真实配置执行的 1843 项 Gene 断言；50 项装备/战斗/角色持有断言；49 项装备流程断言；58 项客户端装备断言；6 项实际客户端 Gene/属性组件断言。服务端、Unity Common/Network 与 381 个客户端源码文件编译通过，保留既有警告。证据位于 `_temp/gene-oncall-20261108/`：gene-tests.log、GeneEquipmentTests.log、FlowTests.log、client-tests-run.log、client-gene-after.log，以及三个构建目录。
- 自定义多属性节点在一次 OnCall 中修改 HP，并按两倍数值修改外攻，覆盖叠加和移除。真实服务端场景属性保留共享 HP 接管、伤害倍率及 firer77 归属，未重复扣血。直接资源结算可能移除节点时，运行时使用保留原对象引用的快照。
- 已复现客户端刷新失败：临时移除时 HP 从 1200 降到 1000；client-gene-before.log 保留失败证据。最终 6 项组件测试覆盖刷新、真实移除、Destroying 先于属性清理以及复用。测试通过 Unity Mono 运行实际客户端程序集，仅以反射搭建宿主，没有启动 Unity 场景。开发期间的编译/测试宿主失败保留在完整日志中。
- 已验证导图锚点及全部 47 个路径；node --check 通过；三个已发布 DLL 与 dll-publish.json 中的构建哈希一致。没有新增浏览器交互验证。隔离装备测试不含数据库/玩家会话，gateway-session 错误属于预期隔离边界。真实 Unity 战斗、数据库迁移/重新登录、多服务器部署、完整 Gene 同步及平衡性未验证。
- 完整文件、Git 与用量记录：`项目修改日志/2026-11-08-Gene节点工厂与公共运行模块.md`。中文增量检查仅限 CURRENT_TASK 和 TEST_REPORT 中本任务编号的新段，证据位于 `_temp/gene-oncall-20261108/bilingual-check.json`；旧双语内容不在本次检查范围内。

## [VERIFY][装备][EQUIP-MANUAL-BAG-20261109] 空装备栏界面与武器持久化（2026-11-09）

- 主 Agent 核验了 `UIRoot.prefab` 中精确 12 字节的变化，均为选定外层 Image 的启用标记，并查看了 `empty-equipment.png`。Unity 隔离 PreviewScene 确认 12 个空装备槽边框、Gene 的三个引用，以及真实 `RefreshUI → LoadSpriteAsync` 使用正式 Sprite 的缓存回调、正确的点击 UID 和清空行为。Console 错误保持 0，保留 Scene_Boot dirty=False。该验证未覆盖包异步加载或真实 PlayMode。
- 真实服务端组件隔离测试：两把适用武器经 AddItem/WearItem/TakeOffItem 共通过 34 项断言。每把武器安装七个 Gene 根节点，改变最终属性且不改变裸属性，移除后恢复。向隔离角色 95009 发送时报告缺少网关会话，不能据此声称真实网络已通过。
- 真实 localhost:3308/game_db 事务：角色 `1 / 789` 与 `2 / 112` 各获得 `21001,21002`，位于 BagType 1、从零开始的格位 0 和 1；共四个独立 UID，item/equip/equip_xl_gene 每张表各新增四行。保存版本为 `21→22` 和 `0→1`，两个角色的穿戴列表均为空。既有物品、裸属性、容量和其他角色字段保持不变。正式 `CentRoleModel.RoleKanpaskInfo(EnterWroldReq)` 读回匹配 152 个字段，读回本身未修改存储；主 Agent 另行将实时记录与事务后快照比对。回滚仅作预检，从未执行。
- 初次只读查询因表名拼写错误产生 MySQL 1146，已改为 `equip_xl_gene`；一次临时 PowerShell 引号错误通过改用脚本解决。未发生失败写入。原生开发者：`/root/equipment_manual`、`gpt-6-astra`，会话 `01a1be4e-c60a-7e42-9530-6ba0b70e009b`，第 1 轮分两个阶段执行；主 Agent 已完成验收。
- 证据：`_temp/equipment-bag-manual-20261109/`（`changed-files.json`、`unity-ui-verification.txt`、`weapon-check-results.log`、`database/verification.json`、`parent-acceptance.json`）。完整且已批准的日志：`项目修改日志/2026-11-09-角色空装备栏与武器入包.md`。仍未验证：实际客户端登录/Socket 路径、Unity PlayMode 与手动穿戴、YooAsset 包异步加载及战斗平衡。本次仅追加文档后未重跑业务测试。

## [VERIFY][装备][基因][EQUIP-UI-LAYOUT-20261109] 布局与主城实机界面验收（2026-11-09）

**补充验收——六孔与防具套装部位（2026-11-11）。** Unity 编译和资源导入通过，Console 错误为零。64 项配置表、预制体与隔离装备容器检查通过，覆盖全部 12 条六孔记录、四个按钮样式、套装件数 0–6、移除一件装备，以及武器和饰品的排除规则。主城正常装备详情入口在 1920×1080 与 1366×768 下各通过 17 项运行检查，覆盖显示六个孔位、当前角色的 `0/6`、六个部位状态、四个菜单项射线命中目标和两层点击外部关闭。运行检查调用 Unity 事件处理器和射线检测。独立 Player 构建、真实服务器穿戴与卸下、宝石镶嵌仍待验证。证据见 `_temp/equipment-detail-six-slots-20261111/import-checks.txt`、`_temp/equipment-detail-six-slots-20261111/live-1920.txt`、`_temp/equipment-detail-six-slots-20261111/live-1366.txt`、`_temp/equipment-detail-six-slots-20261111/final-layout.png`。

**最新接续验收——浮层逐层关闭（2026-11-10）。** 真实 Unity 编译通过，最终 Console 错误查询为零。既有运行中武器 21001 在 1920x1080 通过 35/35 项检查，在 1366x768 通过 35/35 项检查，覆盖三个特技卡、属性卡、菜单开关、子层共存与依次关闭、紧凑行高、图标射线命中及原背包 UID。这 70 项检查在真实运行实例上调用既有处理函数，并通过反射调用私有外部点击方法，不是 70 次 OS 物理鼠标测试。另在 1366x768 通过真实 OS 鼠标输入确认：第一次空白点击 main=True/card=False，第二次 main=False/card=False，更多菜单开关 False→True→False。两个原 UID 61353029711958016 和 61353029713006592 均仍在背包中，未提交穿戴或特技使用动作。

初版实现暴露了真实的更多按钮时序冲突（mouse-down 关闭菜单，随后 onClick 又将其打开）；最终代码将该开关按钮排除在自动关闭范围之外。证据均位于 `_temp/equipment-ui-stack-20261110/`：`more-toggle-before.txt`、`assertions-1920.txt`、`assertions-1366.txt`、`physical-clicks.txt`、`prefab-preservation.json`、`after-layout-1920.png` 和 `after-layout-1366.png`。相对本任务基线，预制体恰好修改四个既有块，其余工作区块、引用与 stripped 头均保留。已暂存预制体为 HEAD 加四处数字字段修改，排除了无关脏内容。Player 构建与战斗未测试；装备／特技协议、持久化及平衡不在本次修正范围。

**最新修正验收——主 Codex（2026-11-10）。** 已验证的预制体共有 6,377 个块，其中详情区 724 个块：相对本轮快照修改 120 个块、新增 1 个 LayoutElement、删除 26 个旧控件块；全部 69 个 stripped 块及无关块均保留。安装前已通过父子类型、反向链接、所有者、环、删除引用和变更 YAML 检查。三个源文件已在真实 Editor 中编译，未新增禁用语法；已加载程序集不再包含 _activateButtons。同一 Unity 进程 56992 始终存活。最终主城 Console 查询为零错误；此前曾单独观测到一次 Package Manager Unity-ID 认证错误，本 UI 工作未修复该独立问题。

真实 1920x1080 与 1366x768 截图确认图标、边框、光效均在头部内，养成文字采用 Regular 15／16 磅层级，主手／副手名称与身份信息完整。8 张未经修改的实机帧以 0.4 秒间隔采集，图标区域绿色分量为 0.575–0.994、蓝色分量为 0.763–0.987，最大 RGB 像素差为 16／44／51；weapon-glow-live.gif 是这些帧的裁剪。两个尺寸下打开的特技卡均通过整数鼠标像素中心 22/22 可见按钮命中检查。保留此前一次分数中心坐标失败：精确数学点未通过原生矩形包含检查，但其周围 9 个整数像素全部命中；未因此添加业务规避逻辑。证据位于 _temp/equipment-ui-icon-20261110/ 下的 final-*.png/txt、raycast-final*-pixels.txt、glow-pixels.json 和 remove-button-investigation.txt。

三个槽均打开各自的标题；未选择的 slot1 显示“使用”，已选择的 slot0 显示“已选·未穿”，空槽不提供使用动作。隔离替换监听器后，从侧卡“使用”按钮捕获了 slot1，并在 finally 中恢复原处理器，未向服务端发送激活请求。属性卡同时隐藏使用／状态控件。打开 21002 会重置全部六个浮层／展开项；原 UID 61353029711958016／61353029713006592 仍在背包中。最终状态为 1920x1080、Scene_MainCity、主武器详情与第二槽侧卡打开。物理 OS 输入、真实穿戴／当前生效状态、激活协议往返、持久化、打包及其他尺寸仍未验证；此前战斗／平衡验收边界继续保留。已批准日志和下文此前失败记录保持不变。

**最新验收——无临时绑定、进程稳定与界面状态（2026-11-10）。** 全新正常登录进入 Scene_MainCity，MainCtrl 与遮幕引用有效；此前 MainView:219 的阻断已在工作副本中修复。其修正后的引用在 HEAD 中已存在；无关的本地预制体改动保留。预制体崩溃修复后，同一 Unity 进程 56992（16:49 UTC+8 启动）导入资源、进入 Play 并完成正常登录与界面检查，此后未再观测到 APPCRASH；运行时 Console 错误查询返回零。这证明的是本次已观测到的修复，并不等于永久不再崩溃。

16:31/16:40 的两次原生崩溃源于 r3 的三个 SectionDivider 子引用使用 GameObject ID（class 1）而非 RectTransform ID（class 224）。两份崩溃栈都在 UIRoot.prefab 导入期间重复出现 Transform::InitializeTransformHierarchyRecursive。WorkBuddy 精确修正三处子节点列表条目；Codex 在该次修复时核验其余字节未变、69 个 stripped 块头保留、无非法子节点类型。VisualScripting 的 node options 警告属独立问题。证据：_temp/equipment-ui-polish-20261110/crash/parent-verified.json 及两份提取的崩溃栈；final-static.json 校验最终资源。

已核对真实 1366x768 与 1920x1080 截图。按钮中心射线通过：带特技卡 22/22、1366 下带更多菜单 22/22、1920 下 17/17。镶嵌/套装/淬锋各自 false→true→false，内容高度回到 544.75；孔数为二且恰好两格激活。短卡片会收紧；已读取当前层/下一层特技文本与空的第三槽，外观在底部可见，六个新增未开放动作的提示与消息一致，关闭把图标 alpha 复位为 0.1，重开武器 21002 复位全部六个展开/覆盖状态。未测试物理系统鼠标、穿戴/激活、打包构建、超出当前长度的文案或其它分辨率。证据：_temp/equipment-ui-polish-20261110/ 下的 final-*.png/txt 与 final-static.json。


**最终两条验收（2026-11-10）。** 主 Agent 核验两个新按钮中心各自命中自己的处理函数，且实机武器 21001 上出现两条完全一致的未开放提示；截图与证据：`_temp/equipment-ui-layout-20261109/revision16/r18-final.png`、`r18-parent-buttons-final.txt`、`r18-stripped-fix.json`。本次检查需要对既有 MoshentaBlackCurtainWindow 做临时运行时绑定：无关的工作区 MainView 引用在 InitView 第 219 行为 null，导致 Play 暂停。未保存任何生产绑定；正常重新登录验收仍被该引用阻断。在带此仅测试用绑定的新一次运行中，使用原相机渲染与正常登录，未执行穿脱/特技操作。已停止 Play 以丢弃全部临时状态。此前的完整布局检查仍为历史；本补充不主张未经修改的重新登录已通过。


**最新第 6 轮验收——录屏对照与实机运行（2026-11-10）。**

- WorkBuddy 最终构建零错误通过；此前整包重建记录为 62 条警告。Unity 导入最终预制体/脚本；最终 Console 错误查询为零。主 Agent 目视核对真实 1920x1080 与 1366x768 截图（非 Mock 预览）；GameView 已恢复 1920x1080。
- 主 Agent 驱动真实的既有按钮/ExecuteEvents。两把背包武器通过悬停=false、右键=false、左键=true、穿戴=true、卸下=false。三个特技槽显示各自完整说明，含“血债”10% 回复描述与明确的空槽。八个养成/编辑动作给出各自对应的未开放提示。1366x768 下底栏/更多状态下全部 20 个可见按钮中心均命中各自处理函数；此前 17 按钮测试亦通过。未点击任何穿戴或激活按钮。
- 真实失败均保留并修复：页头溢出、属性基线不齐、宝石槽过大、特技名被隐藏、详情只有标题、控制器数据被开发者误重置、预览时程序集过期、折叠的淬锋状态跨重开残留、底栏跑出窗口、底栏对比度过低。主 Agent 观察到淬锋 alpha 1→0→1、重开=1、切至 21002=1 且底栏关闭。底栏现为不透明浅蓝，主面板内为可读的深色 14 号字。开发者后来的登录阻塞是把角色名当账号的误判；主 Agent 通过只读归属查询恢复了既有正确账号，未改服务端或数据库代码。
- 最终证据目录：_temp/equipment-ui-layout-20261109/revision16/；跨输入证据保留在 revision13/parent-details.txt 与 revision15/final-forge.txt。更早的编辑器内快照不构成实机证明。本地对照 HTML 未经浏览器验证，因浏览器拒绝 file URL，且未做任何绕过。物理系统鼠标输入、新穿戴状态、战斗、持久化、养成/平衡与其它分辨率在本阶段仍未验证。文档同步与 Git 回执另行记录于已批准的完整日志。

**最新第 5 轮验收——仅点击详情链路（2026-11-09）。**

- 实际源码差异仅限 KnapsackSlotWidget.OnPointerEnter：删除一处旧详情调用，并更新 summary 与参数注释。Unity GM_Game.dll 于 2026-11-09T12:29:44.6842054Z 编译；编译完成，Play 前及运行时检查后 Console 错误数均为零。
- 正常 UI 登录回到角色 789 / Scene_MainCity。ExecuteEvents 对两件真实武器控件派发 enter/down/up/exit：悬停与右键路径保持详情关闭；左键路径在 EquipmentDetailWindow 打开对应标题且显示“装备”；离开槽位后已点击的详情仍保持打开。关闭后不残留旧 EquipItemTips 对象；普通物品悬停同样不打开详情。原 UID 61353029711958016 与 61353029713006592 仍留在装备背包，穿戴槽数为零。截图 left-detail.png 已由主 Codex 目视核对。
- 证据：_temp/equipment-ui-layout-20261109/ 下的 revision5/hover-check.json、equipment-hover-right.json、equipment-left.json、final-runtime.json、implementation.diff 与 parent-acceptance.json。国内版 WorkBuddy hy4free，Hub hub-d0273747-ecdc-4772-9164-c64aa5d5eaa2，第 2 轮 / run-3f8ae84f-19aa-4b50-b3e7-db9ad0341b88；主 Codex 已验收实际代码、编译、事件与截图证据。未做物理系统鼠标自动化、穿戴槽运行时测试、穿脱或特技请求、数据库写入或战斗回归。临时检查片段遇到 Global 类型重复、可选按钮未绑定与相机工具输出目录限制，仅调整检查脚本，未对业务代码做规避。

**此前验收历史（原文保留）：**



**最新验收——UI 第 4 轮返修（2026-11-09）。**

- 主/副武器详情在 1920x1080 和 1366x768 下均通过（四种组合）：固定头部/右侧操作、“卸载”与“装备”对应正确、顶部/底部文字完整，滚动范围约 78.33 参考单位。复用 RO 背景的内部 alpha 为 255，截图呈现不透明淡蓝色。实际 Prefab 差异：修改 89 个既有块、新增 41 个详情块、删除 0 个块；详情之外 5653 个块逐字节保留。UIRoot SHA256 为 ebd159a613fdde26f2514cbf14998f52d32708deb07e9f8ccb21e75ff04f8de5。主 Agent 查看了 main-1920-top、offhand-1920-bottom、main-1366-bottom 截图，以及录屏 0/19.80s 帧。
- 真实 UI 滚轮使 verticalNormalizedPosition 从 1→0.540405，三个镜头 orbit 均未改变（顶部高度 12）。在场景空白处，滚轮使顶部高度由 12→22，而详情正文保持不动；之后恢复为 12。CameraMgr 的最小 UI 命中条件已在实际 Editor GM_Game.dll 中编译通过（error CS=0）；正常重登加载正式资源，最终 Console error=0，详情关闭无残留，Gene 保持隐藏。源码哈希 5467aa0403cc2ee67130ca4f41dbd72e7afb1e293109ef23a4bdef021843dee5 包含此前无关 dirty 改动，这些改动不得随本任务提交。
- 物理拖拽自动化在 8 秒追踪中没有产生可观察的按下/保持/释放帧，因此不声称物理拖拽通过。既有 ScrollRect initialize/begin/drag/end 事件到达两端边界，真实滚轮输入已验证。未执行穿脱、特技激活或数据库写入。保留当前主武器已穿戴、副武器在包及选中槽 0 的状态；本轮不属于新的战斗/持久化测试。RO 参考为 CommonItemTipsBaseComponent.prefab 与 Common1.png；用户视频提供了 5 个关键帧，打造/弹窗及不可用属性明确不在本次布局范围内。证据：_temp/equipment-ui-layout-20261109/ 下的 revision4/runtime-acceptance.json、compile-check.json、prefab-preservation.json、CameraMgr.task-only.diff 和 video-reference/observations.json。沿用同一原生开发者/会话，第 4 轮；完整已批准日志及此前 EditorPrefs 边界仍如下方所记。

**此前验收历史（原文未变）：**


**最新验收——第 2/3 轮返修（2026-11-09；第 1 轮证据保留于下方）。**

- 第 2 轮：真实 TakeOff 请求 11161 成功，观察到回复 11160→11162→11161。原主/副武器 UID 现位于 bag 1 的 slot 0/1，各一件、零穿戴。服务端与客户端属性一致：外攻 1286–2519→50–65、命中 2041→20、暴击 1980→15、诛心 4151→0、韧性 3088→0；已移除七个主武器 Gene 根节点。1920x1080 下首次打开 3.464 秒、重新打开 3.417 秒，ActiveGenesPanel 均保持隐藏，12 个槽位全部为空，角色宽度为 640。主 Agent 已查看截图和差异。临时协议委托已恢复，未新增正式测试组件。
- 持久化回归问题真实存在：主 Agent 于 16:02:11 +08 读取数据库时，仍为版本 23、主武器 bag 4/slot 7；随后同时读取服务端/客户端状态，排除了用户重新穿戴。定时器在 _flags=null 时触发 87 次，两条真实 NullReferenceException 堆栈均停在设置 needSave 前的 SetFlag。仅修改 OnlineRole.cs 的字段初始化/注释。GameServer 构建通过（0 错误、19 条既有警告）；针对已编译程序集的 10 项隔离检查覆盖构造、独立 flags、60000ms 触发、消费、下个周期及 Stop；这些检查未使用网络/数据库。初次 OutDir 依赖失败已保留，并通过使用既有 MMoRpgCommon/Network 二进制解决。
- Gate 断开后的正常离线保存使数据库版本由 23→24；两把原武器都在 bag 1、slot 0/1，各一件。旧角色的在线/待处理索引通过正常的 ACK 后释放路径变空；ACK 成功由该生命周期与数据库提交推断，并非单独抓取了 ACK 包。主 Agent 于 16:53:20 +08 完成六项只读数据库验收。GameServer 可执行文件哈希及实际加载元数据均匹配修复构建；Game/Gate 已重启，Center 持续运行。文件释放期间曾发生一次替换失败/旧版本启动，已保留证据并在 Gate 重启前修正。
- 经用户授权，正常 UI 重登在随后一次 Rider 服务重启后于 17:11:46 +08 通过（当前 Game 66484 / Gate 66108 / Center 66468）；当前加载元数据与其二进制一致。新 _flags 非空。17:11:44→17:15:44 期间，真实定时器触发次数由 0→4，保存标志 4 变为 true；周期后的 needSave=false 与经 UpdatePerSecond/ConsumeNeedSave 消费的路径一致，并非采样到了瞬时 true。六个背包的 dirty 标志均为 false、在途保存为空，版本仍为 24：干净数据走既有的不生成快照路径。当前进程快照未发现匹配的 SetFlag 空引用堆栈，这不等于完整历史日志审计。主 Agent 已查看新截图：12 槽为空、Gene 隐藏、两把原武器位于背包 slot 0/1；外攻 50–65、命中 20、暴击 15。17:17:14 的只读数据库检查确认版本 24 及两件物品。最终主城装备背包已打开，未为测试而重新穿戴、选择特技或改动物品。角色 112、新脏数据的周期自动写库、重新穿戴/切换特技及战斗/平衡仍未在本阶段验证；此前正常离线持久化已证实。新增证据：_temp/equipment-ui-layout-20261109/revision2/persistence/repair3/relogin/delivery.json 及其截图。沿用同一原生开发者/会话，第 3 轮；此前 EditorPrefs 限制保留。

**第 1 轮历史（原文保留；其中未改动物品、未验证协议往返的限制仅适用于第 1 轮）：**

- 初始布局阶段：Unity C# 编译通过；隔离的 1920x1080/1102x759 预览确认布局、滚动，并通过红/蓝背景下像素相同验证面板不透明。初次 git 空白警告来自既有的 38 行相同 Prefab 块；新增/修改块未引入行尾空白。预览克隆在异步图标回调前被销毁，产生一条已记录的 MissingReferenceException；已修正测试时序并保留原始 Console 证据。
- 用户登录后，真实角色 789 暴露出正式 Prefab 的 Mock 组件将全部 12 个装备槽覆盖为 UID=0。已移除其三个块及父引用，通过既有 RoleInfoCtrl 刷新；首次打开等待 2.364 秒，重新打开等待 2.375 秒后，仅保留真实主武器，其余 11 槽为空。Prefab 延迟为 2 秒，不是脚本默认的 0.5 秒。源 Prefab 和当前运行实例中此类 Mock 组件现均为零，测试脚本仍保留。
- 实机槽位 PointerClick 与背包 PointerUp 打开了正确的主/副武器详情，分别显示 TakeOff/Wear 按钮，特技状态与实例一致。滚动 1→0 时头尾位置不变；关闭角色窗口也隐藏 Gene，背包/详情可继续显示，随后四个窗口全部隐藏。已检查真实 GameView 1920x1080 与 1366x768；最终会话保持在 Scene_MainCity，窗口关闭，恢复原来的 1920x1080，dirty=False。
- 最后一张 1366 截图暴露出 renderQueue 3001 的 img_enter 覆盖层，高于普通 UI 的 3000。仅在 UIRoot 内禁用 12 个未使用装饰，同场景对比确认白色覆盖消失，未修改共享材质。主 Agent 核验了角色、修复覆盖后的详情及全部关闭截图、最终哈希与精确实机差异：删除 3 个块、修改 13 个既有块，其余全部保留，git diff --check 通过。最终 UIRoot SHA256 为 `e22ef3df2b81fb77ec34cb9b0da1a0970567910f8e163eba1eda321d122080d9`。
- 主武器 UID `61353029711958016`（21001、bag 4、slot 7、selected 0、families 2/4/0）与副武器 UID `61353029713006592`（21002、bag 1、slot 1、selected 0、families 3/5/0）前后完全一致。未写数据库，也未点击实际穿脱/特技变更操作。未新增运行时业务异常，早期预览异常仍留在 Console。工具字段/源码/ID 解析失败均发生在写入前，已保留于证据。
- 开发者：Codex 原生 `/root/equipment_manual`、`gpt-6-astra`，会话 `01a1be4e-c60a-7e42-9530-6ba0b70e009b`，第 1 轮先布局、后实机阶段。证据为 `_temp/equipment-ui-layout-20261109/`（`runtime-acceptance.json`、`parent-acceptance.json`、`runtime-overlay-comparison.json`）。已批准的完整日志：`项目修改日志/2026-11-09-装备详情不透明布局与基因面板归位.md`。边界：未在移除后重新登录，未切换角色 112，未进行实际穿脱/特技协议往返或战斗/平衡复测。初始阶段将 EditorPrefs ScriptCompilationDuringPlay 设为 1，原值未记录，不声称已恢复。

## [VERIFY][副本][怪物AI][DUNGEON-BATTLE-CONFIG-20261109] 配置化战斗加载与魔神塔回归

- 服务端以 HEAD 50e63e1 加本任务文件/共享场景登记隔离编译，未带入工作区无关 Gene/Attributes 改动；最终测试 GS SHA256 为 84680C658AD920950C1D46B90D2C84901CFB171EC3091693BDCF020831A0FCB6。客户端编译覆盖当前完整 GM_Game 工作区（384 个源文件、268 个引用），不是 HEAD 隔离投影。主 Agent 已核对交付 35 文件哈希、精确暂存范围及 git diff --check。
- 全部 299 项断言通过：MoshentaAiSmoke 50（含原 Lua/Code 的 65 事件等价）、MoshentaSkillSmoke 103、自然推进/TCP MoshentaFullAiIntegration 50、真实北森 DungeonConfigProgressSmoke 52、DungeonBattleProfileSmoke 33、DungeonSceneRegistrationSmoke 11。显式 FixtureA/B 目录的同号技能/实体/Buff/曲线、同名行为树、NPC/时间轴绑定与采样导航均隔离。无时间轴配置可以省略 Lua/轴计划路径。
- 合成场景 99001 通过正常 Spawner/Update 入口执行，在测试进程临时登记表映射并恢复。测试投影仅追加三个嵌入资源，353 份业务源码哈希与已测服务端一致；证明的是配置化非 4060 入口，不是真实北森战斗。证据见 verify/fixture-source-hashes.json 和 verification-summary.json。
- 主 Agent 通过 Unity MCP 实际加载 4060 的 40 技能、32 Buff、7 通知、四项效果预制体及公共 MMonsterAnimator；核验按根目录稳定缓存，4050/4051 的空目录不增加缓存，两处真实 32 实体目录和 XML、NoFadeEntityIds 5357/5501/5502/5503 及不同采集标题。只读检查保留场景身份和 dirty 状态，Console 返回 0 条错误。没有桌面输入或 Prefab/场景修改。
- 保留真实失败：客户端临时编译初次引用过期 Network.dll，缺少 RoleGeneData，改用当前项目引用后通过；旧测试在过期位置/类型查找共享生命与 Gene API，且临时测试适配曾编译失败。最终测试使用实际 AttributesComponent 和 Gene 返回类型，没有将无关 Gene 工作区改动拷入服务端。主 Agent 在最终验收前退回硬编码尸体 ID 与未使用时间轴路径必填问题修正。
- 代码提交 6a094fb 已推送；未切换本机运行服务器、构建独立 Player 或线上部署。未完整移植北森战斗/地图，未手动多人完整通关。完整文件清单、实际角色、回执和边界见 项目修改日志/2026-11-09-副本AI技能导航配置化.md；原始证据位于 _temp/dungeon-generic-battle-20261109/。

## [VERIFY][装备][特技][基因][EQUIP-SPECIAL-SKILL-CATALOG-20261111] 目录与装备实例验收

- Network、CenterServer、GameServer 均构建成功。当前 Unity `GM_Game.csproj` 编译零错误。最终 Luban 数据经 Network 解析通过（98 族、294 行、无断链基因引用）。最终 12 处描述修改后，全部已绑基因行与两个基因文件均未变动。
- GameServer 验收通过 76 项检查，覆盖铜币与开孔顺序、装备部位限制、同族 Lv1–3 叠级且 Lv3 封顶、破甲·外以敌人为目标。隔离 CenterServer 数据库通过旧空槽同版本重放、改孔冲突、已开孔与铜币保存/重载、第三孔迁移、删除时清理。
- 既有服务未随本次构建部署。独立 Player 打包、完整游戏重登，以及安装特技的物品玩法均未做端到端验收。待接入的 88 族不是运行时效果。
- 证据：`_temp/ymjh-special-skill-catalog-20261111/main-catalog-final.json`、`_temp/ymjh-special-skill-catalog-20261111/accept-game/runtime.log`、`_temp/ymjh-special-skill-catalog-20261111/accept-db-r3/db-acceptance.txt`、`_temp/ymjh-special-skill-catalog-20261111/build-servers-r3.log`、`_temp/ymjh-special-skill-catalog-20261111/accept-current-client-build.log`。完整结果与边界见 `项目修改日志/2026-11-11-装备特技目录开孔与基因生效.md`。

## [VERIFY][装备][特技][基因][EQUIP-SPECIAL-SKILL-COMBAT-20261111] 正式战斗入口验收

- Network、CenterServer、GameServer 及当前 Unity `GM_Game.csproj` 均构建成功。最终 Network 解析器读取 294 行与 179 条基因行，无断链基因引用。最后一次修改只改了两处来源标注，基因数据一致。
- 隔离夹具经真实副本扣血路径执行 36 项检查，另经普通技能写入与共享生命执行 9 项，全部通过。覆盖振奋 Lv1–3、法力封顶、未击杀与重复击杀保护、生命阈值、首领与非首领、背击与正面、沉默及击倒/易伤独立到期、单独冷却刷新与响应快照，以及可归因的共享生命击杀。正向概率分支在夹具中已改为确定。
- 完整真人多人、运行中服务部署、PvP 与独立 Player 打包仍未验证。80 个未绑定族等待所列接点或证据。破竹破气分支与跨角色外发治疗仍为部分或待接入。
- 证据：`_temp/ymjh-special-skill-combat-20261111/production-hit-runtime-r5.log`、`_temp/ymjh-special-skill-combat-20261111/production-hit-runtime-r5-remaining.log`、`_temp/ymjh-special-skill-combat-20261111/catalog-runtime-final.log`、`_temp/ymjh-special-skill-combat-20261111/build-servers-r5.log`、`_temp/ymjh-special-skill-combat-20261111/client-build.log`。完整文件清单与 Git 回执见 `项目修改日志/2026-11-11-装备特技实战接入与原作校对.md`。

## [VERIFY][基因][GENE-COMPONENT-UNIFY-20261111] 统一组件与既有副本战斗验收

- GameServer 全量重建通过，25 个警告均为既有警告。隔离程序对真实角色初始化与怪物工厂、世界生命周期跑了 30 项检查；两个独立挂载的角色验证了状态只施加目标、逐帧正常到期与关闭清理；11162 的首次全量/空列表与增量、同身份替换与删除均带序列化核对。
- 既有正式副本验收程序通过 36 项伤害与触发检查、9 项技能提交与共享生命检查。WorkBuddy 侧临时程序编译时未能解析 GameServer 类型，改由 Codex 用本地构建与直接编译补齐引用后完成验收。证据在 `_temp/unified-gene-component-20261111`：`codex-build.log`、`unified-runtime.log`、`production-hit-runtime.log`、`production-hit-remaining-runtime.log`。
- 验收未使用网关会话；联机多人、PvP 玩法与独立 Player 构建仍未验证。业务提交 `8b18017` 与完整范围见 `项目修改日志/2026-11-11-角色怪物基因组件统一.md`。

## [VERIFY][副本][DUNGEON-DEATH-FEEDBACK-20261112] 真实服务端死亡复活与配装验收

- 协议、服务端、网关及客户端编译通过，保留既有警告。隔离复活 16 项、真实 TCP 网关转发 4 项、配装数据库比对 28 项、首次装载资源处理 20 项检查通过。首轮复活测试夹具重复释放测试角色，修正夹具清理后重跑；未为掩盖测试问题增加业务保护分支。
- HUD 资源及旧白色样式检查 26 项、独立 Mono 显示探针 11 项通过，不能作为 IL2CPP 发布包证据。真实 Unity 与本机服务中，角色 112 进入地图 4060/实例 1，波次 16 造成真实致死命中（伤害 1000，按剩余生命裁切），出现红色 HUD、打开正式死亡窗口，尸体留在 (148.54,62.03,123.87)。
- 正式序列化复活按钮回调发送 11243，实际 11244 返回 HP47496/47496、MP10500/10500、出生点 (132.97,58.89,60.95)。消费真实已排队响应后，窗口关闭并恢复正常状态。滞留原因是模型释放后的 RestoreSpeed 空引用引起 Editor Error Pause，并非没有复活回包。最小修复已编译，并在临时 PreviewScene 执行实际释放方法；不宣称修复后再次完整跑过真实全灭。
- 正式入口模拟另验出生点下方 101m 的活人恢复、排除尸体，以及普通/暴击/致死回调。临时观察组件与状态已移除或还原。UIRoot 基线保留核验仅移除本次 26 个对象与 2 处引用，结果与此前脏基线完全一致。
- 5515 原公式采样 10000 次，穿装后普通伤害为 1521～1616；满血可承受 29 次采样最高伤害，不涵盖 Boss 与群怪机制。两人装备均从真实存档回读；初始当前生命仍为 1000，促成随后首次装载修复。最终已部署服务的正常 Editor 首登通过：112/RoleID2，HP47496/47496、MP10500/10500、Scene_MainCity/kStateDefault、playing=true、paused=false、Console 错误 0。证据为 damage-numbers/final-editor-login.txt/.png。仅已启动的一次 GenerateAll 完成；Boot 原字节已恢复，遵照用户要求未继续打包。
- 证据与失败记录见 `项目修改日志/2026-11-12-副本受伤数字与主动复活及双角色配装.md` 和 `_temp/dungeon-death-feedback-20261112/{death-ui,damage-numbers,equipment,deploy-backup}/`。没有独立 Player 验收结果、真人双客户端完整通关、线上部署或自然碰撞穿地根因复现结论。

## [VERIFY][副本][怪物AI][DUNGEON-AI-HIT-STATE-20261112] 错误目标复现与真实 Editor 战斗验收

- 旧身份探针复现 NPC UID2 遮蔽玩家 2。定向服务端编译及真实 4060 配置的 96 项隔离检查通过，包括两个玩家 ID、84 个实体自定位、SpawnId 保持、Buff 归属、第 15～18 波追向真实玩家位置并权威扣血。主 Agent 已审查实际差异与动作事件生命周期。
- 本地不修改 HP 的表现探针分别验证：静止受击自然结束、受击后移动（0.133s 为 Default，0.192s 实际播放 ZdMoveWalk）、移动/跳跃受伤保持状态、普通 10010 技能不被强制切为 Default。随后真实服务端事件验证：第 18 波在实际玩家 (149.38,63.94,162.88) 附近造成 1524 伤害；第 17 波追到 (133,122)；真实移动受伤保持 ZdMoveRun，空中受到 706 伤害保持 Jump_Start。真实死亡后再 Move 仍为 Dead/HP0/原位置；真实全灭清理未出现空引用异常。
- 第二轮第 18 波样本是在全灭/复活后重新生成、没有目标的 path1 PingPong 巡逻，不能作为持续追击或距离超限失仇的证据。StandBy 没有追击距离上限；Sight6 用于发现目标，HatredAway0|0。原先错误追向 NPC 聚集点的根因已有独立实证。
- 最终观察组件与自动路线均已移除，playing=true/paused=false/sceneDirty=false，Console 错误 0。未进行桌面输入、Player 打包或线上部署。证据位于 `_temp/dungeon-ai-hit-state-20261112/`；完整范围与边界见 `项目修改日志/2026-11-12-怪物目标ID冲突与受击移动衔接修复.md`。新增普攻朝向反馈另行处理，在本次记录中仍未解决。

## [VERIFY][技能][BASIC-ATTACK-TARGET-FACING-20261112] 八次普攻验证越过目标后的逐击转向

- 最初 15 秒只读采样时附近没有敌人，未捕捉到用户攻击，不能记作现场复现。单文件修复后，正常 Unity 重载、登录、组队及进入副本通过，Console 错误 0，未打包。
- 对实际已加载 SkillCtrl 的方法检查验证：表中 10010.Type1/10040.Type2、无敌人返回 null、自动选择的背后目标保持、无目标时选择最近背后敌人、手选目标保持，以及 Type2 朝向/手选规则不变。临时朝向与目标已恢复。这些是方法检查，与游戏集成验收分开记载。
- 以 0.85s 间隔调用正式 OnSkillKey("Q") 入口 8 次，使用真实怪物、协议和原攻击位移，属于正式快捷键入口调用，不是物理键盘测试。未修改坐标、HP、朝向或曲线制造此过程。正常前冲越过 UID104 后，第 3 击将朝向 dot 从 -0.9953933 转为 1，仍保留 UID104；第 4/6/7 击同样转向该目标，正常 0/1/2 攻击段保留。主 Agent 已核对实际 INPUT_BEFORE/AFTER 证据。
- 证据：`_temp/basic-attack-target-facing-20261112/selection-runtime-checks.log`、`continuous-basic-fixed.log/.png`、`development-handoff.md`。全部临时回调已移除，sceneDirty=false、Play 未暂停、Console 错误 0，没有自动治疗或复活。全部职业武器、物理键盘连续输入、真人多人完整通关、发布包及线上环境仍未验证。完整记录见 `项目修改日志/2026-11-12-普攻目标保持与逐击转向修复.md`。

## [VERIFY][技能][BASIC-ATTACK-MANUAL-CHASE-MOTION-20261112] 真实普攻追近与位移检查边界

- 正常 Unity 重载通过，最终 Console 错误 0。三个表现资源 Runtime 校验均为 true，ChargeList 数量为 0/0/1。首次错误读取 MEntity.Target 导致 CS1061，改用 PlayerEntity.Target；源资源零距离段导出空 CurveX 键并导致实际表现校验失败，随后移除这些位移段并重新导出，未削弱校验。
- 附近有 7 个敌人、无手动锁定时调用正式 Q 入口，保持朝向（dot≈1），selected/manual/nav=false。实际手选 UID158/spawn73014444036 时距离 17.20311m，先开始导航、不释放技能；约 2.046s 后首次 SkillStarted 距离为 2.947199m，随后服务端权威伤害 1678、目标 HP53453。目标在开刀后才移动，首刀时 targetMoved=0；文件名不能作为追击移动目标的验收依据。以上为 Editor 入口调用，不是物理键盘测试。
- 修复前取消锁定后仍移动约 3.36m。修复后从 12.23103m 处取消，仅剩一帧 0.0900019m 移动，之后导航、选择、afterAction 和技能均清空；9.51565m 处的方向事件也清空导航/afterAction。目标生命周期仍为 Active 时，本地同步 HP0 可取消追近，并在同次调用内恢复 HP55131；这不是服务端真实击杀。较早一次无候选尝试在玩家已死亡时失败，尚未修改属性。
- 前两段实测位移为 0m。首次第三段样本 0.749m 仅覆盖约 0.9s；延长样本等待第三刀后至少 2.5s，净位移为 0.748075m，技能已结束、状态为 BeHit、无侧碰撞。真实受击干扰意味着不能宣称无干扰自然完整位移，也不能将该净位移等同配置前向 1.0815m。
- 主 Agent 已核对实际代码/资源差异、前两段资源除位移列表外不变、XLSX 单格隔离暂存，以及 bytes 仅偏移 148 由 8 改为 3；差异检查通过。证据位于 `_temp/basic-attack-manual-chase-motion-20261112/`，重点为 client-final-handoff.md、manual-moving-target-real.log、export-runtime-validation.log、root-asset-scope-check.json 和 staged-workbook-check.json。最终观察组件已移除，正式复活返回 HP47496、位置 (132.97,58.89,60.95)，Default/无导航，Play=true/Paused=false/sceneDirty=false。未伪造交还时的血量或位置，没有打包、重启或线上部署。完整范围与边界见 `项目修改日志/2026-11-12-手动锁定追近与普攻三段位移修正.md`。

## [VERIFY][技能][SKILL-TARGET-ACQUIRE-CHASE-20261112] 二十项几何检查与真实移动怪物自动追近

- Unity 重载/编译通过，最终 Console 错误 0。主 Agent 已核对实际四文件差异、正式调用点、任务前基线与 HEAD 一致、仅四文件暂存及差异检查。20 项实际方法断言通过（包括恢复）：前 8m/后 5m、后方回退、包含 10m/排除 10.01m/仅 20m 无目标、身后 25m 自动旧目标保持及超过 18m 不取消、自动来源标志不变、手动换目标、死亡失效后重寻、静止面向和移动方向保持。
- 同一次调用的临时几何测试沿正式 E 入口使用已学习的 10040 Type2/AtkDistance10：导航按其自身 10m 射程，目标位置移动时更新位置而不换身份，取消清除到达动作，方向输入中断。最终恢复位置/HP/朝向/选择。死亡测试是临时 HP0，不是服务端击杀；Type2 检查不是实战伤害验收。测试专用 lambda 重名与 AttrComponent 拼写错误已修正，不属于业务回归。
- 一次真实副本 Q 入口样本从未选中开始，自动取得 9.61579m 外的 UID104/spawn73014444033（manual=false）。追击中怪物实际移动 2.783357m；约 0.827s 后在 2.867724m 处首次出刀，服务端权威伤害 1678、目标 HP53453。5s 内持续保持同一 UID，静止 Default 朝向点积为 0.9994631。与上一任务不同，本轮已经实际覆盖移动目标追近。该样本未人为修改 HP/位置/伤害；Q/E 是 Editor 入口调用，不是物理按键。
- 证据：`_temp/skill-target-acquire-chase-20261112/actual-method-geometry.cs.txt/.log`、`auto-acquire-real.log/.png`、`client-final-handoff.md`、`root-review-and-git.json`。最终观察组件/订阅已移除，正式复活返回 HP47496/MP10500、位置 (132.97,58.89,60.95)，Default/无导航，Play=true/Paused=false/sceneDirty=false。没有桌面控制、打包、服务重启或部署。其他职业/主动技能实战伤害、真人多人完整通关、Player/线上环境仍未验证。完整归档见 `项目修改日志/2026-11-12-攻击前方优先选敌与目标保持追近.md`。

## [VERIFY][技能][TARGET-IDLE-FACING-REMOVE-20261112] 闲站持续面向删除的限定范围检查

- 保存的 MSkillTargetMgr.cs 基线与 HEAD1a5d58b 一致；主 Agent 核对精确单文件差异和暂存范围，差异检查通过。UpdateToCheckTarget 无 Rotation/LookRotation 写入，保留失效目标 ResetTarget(null) 及有效选择。施法/追近入口未修改；这些是静态证据，不是新的游戏运行结果。
- 原生 Codex 开发者正常停止 Play 并刷新，脚本编译完成，Console 错误 0。没有重新登录、活体方法探针、战斗重跑、Player/Yoo/GenerateAll、桌面操作、服务重启或部署。最终 Play=false/Paused=false/Compiling=false，位于 Scene_Boot，dirty=false，无新增测试回调/组件。
- 证据为 `_temp/target-idle-facing-remove-20261112/client-final-handoff.md`、`verification.txt`、`editor-state.txt` 及保存的基线。业务提交 dfaa0ce 已推送；本次仅进行已批准归档。此修正覆盖此前持续闲站面向策略，保留原始测试历史。完整文件范围以及物理输入/登录后战斗/多人/发布环境未验边界见 `项目修改日志/2026-11-12-攻击前方优先选敌与目标保持追近.md` 的本编号条目。
