# 当前任务事实

## [HANDOFF][协作][CONTEXT-CLEANUP-20261026] 归档后的当前续作边界

- 本次仅维护文档/技能。根目录与客户端历史均逐字节保存在 `项目修改日志/协作记录归档/2026-10-26`。从启动入口移除的记录不表示已完成；按主题/编号从 INDEX 恢复旧未决任务，不恢复旧外部会话或推断它们当前正在执行。
- 下文保留最近根目录证据：角色面部/材质/灯光改动与第二轮任务表设计。任务数据生成不代表客户端/服务端任务玩法已经可用，详细未验证边界仍在 TEST_REPORT。
- 较晚的客户端记录（2026-10-24/25）：任务 UI 的 TMP 迁移、Common SDF 统一和对齐修正有静态/导入证据，运行时渲染/布局和部分编译检查仍未验证。仅在继续这些主题时读取 `项目修改日志/协作记录归档/2026-10-26/LYF_MMO/Docs/AI/CURRENT_TASK.md` 及其 TEST_REPORT，不用较早根目录任务 UI 验收覆盖这些后续事实。
- 较早的 UI 交互边界（聊天覆盖、标题拖动、多分辨率）、外部 Agent 身份/轮次及协作规则落地证据保留在原任务/验收快照。归档中的待办没有被本次逐项解决或重新测试。
- 本任务文件清单、验收和 Git 回执：`项目修改日志/2026-10-26-协作记录精简与日志决策_CHANGELOG.md`。新任务使用实际 Git 分支/状态，旧上下文分支只代表历史。

## [FILES][角色][ROLE-FACE-MATERIAL-20261025] 角色面部/材质"发白、没层次"修复，2026-10-25

- 用户反馈角色面部在场景中很怪、与自己游戏里的观感不一致且"很丑"。先排除材质不同源：Unity 实测 `Role_JX.prefab` 的 4 个 UTS 渲染器挂的就是 `materials/P03_*.mat`，工程内无运行时替换角色材质的代码，角色也不走 AssetBundle，故编辑器所见即运行所见。
- 根因一（确定的 bug）：`P03_Skin_Normal.png` 按 Default 导入（`textureType: 0`、sRGB 开）→ 运行时 DXT1；桌面端 `UnpackNormal` 按 DXT5nm 读 A/G 通道，法线数据错误。同目录 `P03_cloth_A_normal` 本就是 NormalMap/DXT5。已改为 NormalMap、关闭 sRGB。
- 根因二（发白主因）：`UCTS_ShadingGradeMap.cginc:190/192` 把 `_HighColor_Tex × _HighColor` 直接加到 base，而七个材质的 `_Set_HighColorMask` 全为 null（默认白），等于每个材质被叠加"自身颜色 × 系数"（皮肤 0.15 / 头发 0.27 / 衣服 0.35 / 眼 0.69 / 眉 0.53 / 腮红 0.28 / 金属 0.20）。已将七个材质的 `_HighColor` 全部归零。
- 根因三（平板感）：`P03_skin` 的 `_1st/_2nd_ShadeColor` 近乎白色（0.943/0.906），皮肤没有明暗分段。已改为 (0.886,0.784,0.780) / (0.749,0.596,0.612)。
- 仅上述 8 个资产文件与修改日志；未改代码、着色器、场景与预制体。完整清单、四档对比与回滚方式见 `项目修改日志/2026-10-25-角色面部材质发白修复_CHANGELOG.md`。

## [FILES][角色][ROLE-FACE-LIGHT-20261025] 角色面部灯光优化，2026-10-25

- 用户要求捏人场景与主城场景的角色面部都有光，且场景只有方向光时角色背光面部不变黑。取证结论：`Scene_CreateRole` 场景内无任何 Light（唯一带灯的 `CreateRole_Models.prefab` 在该场景被根节点 `m_IsActive: 0` 关闭）；角色 `P03_*.mat` 使用 UTS2 `LegacyToon.shader`（`_SHADINGGRADEMAP` 分支）且 `_GI_Intensity: 0`，完全不吃环境光。
- 改动：7 个 `P03_*.mat`（skin/hair/eye/Brows/Cheek/Cloth/Metal）的 `_GI_Intensity` 0 → 0.7；`Scene_CreateRole.unity` 新增根节点方向光 `CreateRole_KeyLight`（暖白 (1,0.96,0.90)，强度 0.85，无阴影，朝向 = 捏人相机朝向叠加 (18°,25°)）。未开启 `_Is_BLD` 固定受光方向（实测收益有限且会让角色脱离场景光照）。
- 仅上述 8 个资产文件与修改日志；未改代码、协议、其它场景与预制体。完整清单、量化验证与回滚方式见 `项目修改日志/2026-10-25-角色面部灯光优化_CHANGELOG.md`。工作区另有多项前序任务未提交改动，本次仅暂存上述文件。

## [FILES][任务][TASK-TABLE-DESIGN-20261025-R2] 任务表体系扩到 13 张（第二轮修订），2026-10-25

- 起因：用户指出第一版 3 张表"作为商业游戏太少了"，要求重读 RO `TaskTable.xlsm`（F 盘路径）。重读后修正三处事实错误：主表 **58 列**（第一版记"约 45 列"）；`Comments` 说明 sheet **只写 43 字段、漏 15 个**（Chapter/ChapterName/PushLevel/AcceptAutoPath/FindwayType/AbandonType/DestroyType/LimitTask/CycleNumber/TargetDes1~5/FinishTips/Target4~5）；周边 **36 张**（第一版记 35 张），其中 **20 张 `TaskSplit*` 是与主表同构的 58~59 列表**，机制是"一套 schema、多文件"便于多策划并发编辑与按玩法热更，不是 20 套玩法逻辑。
- 判断修正：第一版"首期只需主表+类型表+目标表 3 张"是错的。类型表、条件表、奖励、目标库都是常规任务链必需；只有伊甸园/爱神/佣兵/转职属后期玩法。本版建 **13 张**：`#QuestInfo`(55 列)、`#QuestTargetInfo`(17)、`#QuestTriggerInfo`(14)、`#QuestRewardInfo`(8)、`#QuestChapterInfo`(8)、`#QuestTypeInfo`(9)、`#QuestConditionInfo`(6)、`#QuestSurpriseInfo`(9)、`#QuestTargetLibrary`(14)、`#QuestDropInfo`(9)、`#QuestSceneInteractionInfo`(11)、`#QuestItemUseInfo`(16)、`#QuestFlyAcceptInfo`(6)。前 3 张为重建，后 10 张为新建；旧版备份 `_temp/quest_table_backup_v2_20261025/`。
- 结构改造五个论点：①`Target1~5` 定宽 15 列 → 主从表，条数不限；②`2|1001=10|2001=1=2=3` 复合字符串 → 拆 `TargetType/ParamId/ParamCount/SceneId/PosX/Y/Z` 具名列；③`TaskTrigger` 的 `vector<vector<float>>` → 主从表 + `ParamA~F` 具名 + `EventType` 限定已实现列表；④等级 `15=0`、道具 `ID=数量`、NPC `NPCid=场景` 三种不同 `=` 语义 → 全部拆 `int` 列；⑤章节内嵌每行重复 + `RewardID` 跨外部表 → 章节独立成表、奖励纳入本体系。
- 枚举口径：目标类型 **22 种**（RO 自标 6 种未实现：3采集/8完成操作/10活动/11成就/12公会/13社交）；触发事件 Comments 列 18 种 + "23取消buff"，客户端 `TaskTriggerMgr.lua:33` 实现 **17 种**（文档与实现不同步，有编号空洞），我方 `QuestTriggerInfo.EventType` 按 24 个取值集合校验；任务类型 **21 种**归 3 个 Theme；条件类型 **13 种**。
- 新增脚本：`_temp/xlsx_head.py`（批量导表结构）、`_temp/xlsx_filter.py`（按列筛选抽取真实样本）、`_temp/create_quest_tables_v2.py`（13 表建表，数据行用「列名→值」字典构造并按列序对齐）、`_temp/verify_quest_tables.py`（交叉引用与枚举校验）、`_temp/ro_tasktable_all_heads.txt`（RO 36 表结构导出 1553 行）。
- 验证：Luban 实际生成通过（13 个 `tbquest*.bytes` + 13 个 `Quest*.cs` + 13 个 `TbQuest*.cs`，产物 `_temp/luban_check2/`，**未覆盖**项目 Codes/Tb）；`verify_quest_tables.py` **0 错误 0 警告**；`QuestInfo.cs` 字段数 55 与表头一致。踩坑：首轮 13 张全报"没有找到有效的表定义"，真因是**A 列必须是 `##var`/`##type`/`##` 标记列、字段从 B 列起**（第一版脚本对，第二版重写时漏了），修正后一次通过。
- 未验证：未跑 Unity/服务端编译；`LYF_Server/Network/LubanManage/` 生成物未更新；未进播放模式；怪物 ID 1001~1003 与场景坐标为占位；`TaskGuideNpcTable/TaskChangeJobTable/TaskEdenLevelTable/TaskCameraTarget` 四类功能暂缓建表；未提交 Git。详见 `项目分析/RO任务系统源码分析与Luban任务表设计-2026-10-25.md`（本版重写）与 `项目修改日志/2026-10-25-RO任务系统分析与Luban任务表设计_CHANGELOG.md`（含第二轮修订段）。本轮未派子 Agent，中英记录由主 Agent 直写。
