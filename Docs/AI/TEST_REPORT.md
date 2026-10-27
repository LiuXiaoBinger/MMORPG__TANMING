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
