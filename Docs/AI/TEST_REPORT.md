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
