# 客户端 RO 状态互斥移植

任务：CLIENT-STATE-EXCLUSION-20261102；分支：codex/feature-development；基线：d4b065c。

## 变化与逐项文件清单

- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Data/StateExclusion.meta`：状态互斥目录元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Data/StateExclusion/Resources.meta`：Resources目录元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Data/StateExclusion/Resources/StateExclusionTable.csv`：RO原始互斥表完整副本，38×38矩阵不改值。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Data/StateExclusion/Resources/StateExclusionTable.csv.meta`：CSV资源元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/StateExclusion.meta`：状态互斥目录元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/StateExclusion/StateExclusionEnum.cs`：RO 状态及取值枚举，保持原顺序。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/StateExclusion/StateExclusionEnum.cs.meta`：枚举脚本元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/StateExclusion/StateExclusionManager.cs`：当前五个状态收集、查表准入与可选提示，复用状态列表。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/StateExclusion/StateExclusionManager.cs.meta`：管理器脚本元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Table.meta`：表类型目录元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Table/StateExclusionTable.cs`：按原始 CSV 解析 Concatenate 的强类型表及行数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Table/StateExclusionTable.cs.meta`：表脚本元数据。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/StateExclusionContractTests.cs`：专用Editor契约验证，未挂正式场景/预制体。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/StateExclusionContractTests.cs.meta`：验证脚本元数据。
- 修改 `LYF_MMO/Assets/GM_Game/Scripts/Global.cs`：启动时装载互斥表。
- 修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MNavigationComponent.cs`：发包及现有 CanStart 前增加 RO 同名互斥判定，拒绝直接返回。
- 记录 `Docs/AI/QUERY_TASKS.en.md`、`QUERY_TASKS.md`：获批 RO 源码查询事实及中文增量同步。
- 记录 `Docs/AI/CURRENT_TASK.en.md`、`CURRENT_TASK.md`：本次行为、接入边界与续作引用。
- 记录 `Docs/AI/TEST_REPORT.en.md`、`TEST_REPORT.md`：真实构建/隔离验证与未验证边界。
- 修改 `项目分析/项目难点.md`：原表消费者/名称/状态来源不一致的失败与取舍。
- 更新 `Docs/AI/RECORD_INDEX.md`：机械生成标题与源哈希。
- 新增本日志 `项目修改日志/2026-11-02-客户端RO状态互斥移植_CHANGELOG.md`：集中本次事实与Git回执。

没有移动或删除文件；保留任务开始前用户的代码、资源和记录改动。

## 设计、失败与修正

按RO客户端 StateExclusionManager.GetIsCanChangeState(State_AJ_Singing,false) → 原CanStart → Send → BeginPrediction 接入。五个来源为采集、攀爬、吟唱、钓鱼、跳舞；其余玩法无真实来源，不虚构接入。资源读取与热路径状态收集分离，列表重复使用。服务器、协议、原技能准入不变。
首轮把原表两处名称改成枚举名以通过自加行名校验，已被Codex退回。现已恢复原 CSV（SHA256 57b7fda1147f065a52ddabd6bb00d7bc04db4a696fd4e9fe24f9613649dd27bf），按索引读取第三列 Concatenate，1444格完全相同。移除泛化 kStateSpecial→SingleAction 和额外 target-default 放行。真实证据见 _temp/client-state-exclusion-20261102/development-report.md、csv-identity.json；难点见 项目分析/项目难点.md 的同编号条目。

## 实际执行与验证

国内版 WorkBuddy / deepseek-v4.1-flash；任务 client-state-exclusion-domestic-20261102；会话 46168831-5453-4dcb-aacc-3d540babbcb5。代码第1轮 run-b9304afd-d68f-48f3-9994-7d6f4e9a09dc，修正第2轮 run-dbe04d81-3a56-4fb2-9a2f-029e900adc54；随后第3轮仅执行本次获批中英记录同步。Codex负责源码RO对照、退回具体问题、独立验收和Git集成。WorkBuddy自报未使用技能，未委派其他执行者。
GM_Game 与 LYF.SkillAuthoring.ContractTests.Editor 实际重编0错误，626/1734告警含既有依赖告警，不声称有干净告警基线。直接编译正式实现的隔离harness使用Unity/角色环境替身：WorkBuddy带dump参数50项；Codex不带dump参数49项，failed=0，exit=0，区别为输出路径断言。门口不发包是源码顺序验证，未实际点击与抓包。
已批准RO查询记录中英仅同编号增量同步检查通过，证据query-sync-check.md；开发记录中文同步及检查由同一子Agent执行并另交证据。
未验证：修正后Unity实机资源装载、Editor契约Run、实际网络和游戏表现；未启动任何四服，没有新增需停止的服务器进程。缺失玩法来源未实现。

## 用量与 Git 回执

最终可观测用量、压缩统计、记录同步回执和提交/推送结果在收尾后补入本节；原始证据留 _temp/client-state-exclusion-20261102/。

- 记录第3轮 run-af01ff5d-6e78-470c-9b1d-3ca5053c2889 完成：CURRENT_TASK/TEST_REPORT 同编号中文同步，12个维度0差异，未重查未变QUERY。子Agent发现目录尚待重建，Codex已运行目录生成与--check通过，证据 development-record-sync-check.md。
