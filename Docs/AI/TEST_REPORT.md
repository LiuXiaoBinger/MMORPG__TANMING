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
