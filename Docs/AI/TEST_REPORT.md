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
