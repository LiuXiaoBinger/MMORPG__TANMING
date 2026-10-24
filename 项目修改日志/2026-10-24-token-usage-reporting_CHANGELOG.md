# 每次任务额度使用报告约束

- 日期：2026-10-24
- 任务：将用户要求的“每次任务报告额度/token 用量，并使用新安装技能”固化为项目规则。
- 分支：`codex/task-system-ui`
- 未修改任务 UI、Unity 场景或业务代码；保留工作区中其他用户改动。

## 文件清单

- 修改 `AGENTS.md`：新增每次任务额度报告强制条款，规定报告分类、token-monitor 使用、估算标记、缺口和隐私边界。
- 修改 `AIconfig/workflow.md`：在标准流程后新增额度使用记录，要求收集本地日志并在交付记录中注明窗口、来源和不可测维度。
- 修改 `Docs/AI/PROJECT_CONTEXT.en.md`：在 `[DOCS-READ]` 增加英文默认入口规则，要求按类别报告 token 使用并区分订阅额度不可测限制。
- 修改 `Docs/AI/CURRENT_TASK.en.md`：新增 `[TOKEN-REPORT-RULE-20261024]` 任务事实和证据路径。
- 修改 `Docs/AI/TEST_REPORT.en.md`：新增 `[VERIFY-TOKEN-REPORT-20261024]` 的 token-monitor 执行结果与验证边界。
- 修改 `Docs/AI/PROJECT_CONTEXT.md`：同步 `[DOCS-READ]` 中本次 token 报告规则。
- 修改 `Docs/AI/CURRENT_TASK.md`：仅同步 `[TOKEN-REPORT-RULE-20261024]` 的任务事实。
- 修改 `Docs/AI/TEST_REPORT.md`：仅同步 `[VERIFY-TOKEN-REPORT-20261024]` 的统计窗口、数字和未测边界，由子 Agent 增量检查一致性。
- 新增 `_temp/token-monitor-20261024/collect.txt`、`report-trend.txt`、`report-trend-latest.txt`、`context.txt`、`analyze.txt`、`current-session.json`：保存本地分析原始输出和当前会话聚合，不作为正式业务数据。
- 新增本日志：记录规则、文件、设计、验证、token 证据及未解决边界。

## 设计与限制

- 每次最终回复按上下文与项目规则、用户需求、工具调用输入输出、子 Agent/WorkBuddy、重复读取/返工/压缩/验证五类报告；日志没有独立维度时写“不可测”，不填 0。
- `token-monitor collect` 成功写入 `C:/Users/lyf/.token-monitor/token-monitor.sqlite`。本次当前 Codex 会话 ID 为 `01a169b4-4500-79b3-933a-721536092b38`，本地事件窗口为 2026-10-23 17:00:00Z 至 2026-10-24 09:00:42Z，共 1,342 条事件；按活动聚合为 thinking 23,688,070、coding 4,900,382、conversation 3,236,061 个输入+输出 token，缓存读取分别为 97,016,320、29,458,304、17,759,744。该会话包含前序任务，不能冒称为本次规则变更的精确独立用量。
- 本次收尾重新执行 `collect`，新增 19 条 Codex 记录；随后最新 30 天报告为 13 个会话、5,212 turns、88.5M input、2.2M output、587.4M cache-read、87% cache hit、估算费用 `~$291.69`。活动分类为 thinking 67.9M (74.9%)、coding 9.5M (10.5%)、conversation 13.3M (14.6%)。这些是包含既往会话的窗口合计，Codex 数据源仍为实验性，不代表本次规则编辑的独立消耗或账户账单。
- 最新报告原文保存在 `_temp/token-monitor-20261024/report-trend-latest.txt`。工具结果携带、账户剩余额度和重置时间没有本地可验证数据。
- 不把 token-monitor 的估算费用当成订阅账单，不把本地会话聚合当成账户配额；原始结果仅留在 `_temp/`，不写入提示词或源码。

## 验证

- 已执行 `npx -y @ryandemelo/token-monitor collect`、`report --trend`、`context`、`analyze`、`rules`；收尾再次 collect 并更新 `report --trend`。最新收集新增 19 条 Codex 记录，报告统计窗口与实验性来源限制已保留。
- 已执行当前会话 SQLite 只读聚合，结果保存为 `_temp/token-monitor-20261024/current-session.json`。
- `git diff --check` 的唯一现有提示来自无关用户改动 `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs` 第 58 行尾随空格；本次规则文件未新增该问题。
- `/root/sync_task_ui_records` 已交付中文同步与增量一致性检查：仅本次 `[DOCS-READ]` 规则、`TOKEN-REPORT-RULE-20261024`、`VERIFY-TOKEN-REPORT-20261024`。针对最新报告数字做了一次增量修正；19 条新增记录、统计窗口、覆盖缺口、工具结果不可测及零值边界已对齐。主 Agent 已核对实际文件差异范围和交付证据。
- 本次 8 个规则/协作文件的定向 `git diff --check` 通过，仅有既有 Git 行尾转换提示；没有执行 Unity 或业务构建。

## 未解决问题

- token-monitor 目前不能按本次用户任务可靠切分当前长会话，也不能读取订阅剩余额度或重置时间；未来报告必须保留该限制。
- 子 Agent 的实际模型/协作 token 只有在执行器披露并被日志记录时才能报告，否则写“未披露/不可测”。

## 技能与本次分类用量

- 主 Agent 使用 `token-monitor`：收集本机日志并读取报告、上下文与会话活动分析；保留实验性来源、4 天覆盖缺口、估算价格和不能按本任务切分的边界。
- 主 Agent 与 `/root/sync_task_ui_records` 使用 `project-subagent-coordination`：限定本次中英同步范围并核对增量交付；子 Agent 提供方/模型未披露，未调用 WorkBuddy，也未将其工作计为免费模型成果。
- 本次任务的上下文/项目规则、用户需求、工具输入输出、子 Agent 协作、重复读取/返工/压缩/验证五类均不能从现有本地输出可靠独立量化，逐类标记为“不可测”；活动占比只是上述 30 天窗口的 thinking/coding/conversation 分类，不能冒充这五类的精确占比。会话上下文底数 31.8k token 也不是本次规则文件本身的 token 数量。
- 本次仅为规则和协作记录变更，客户端/服务端编译、Unity 运行验收不适用。

## Git 集成

- 仅暂存本日志与上述 8 个正式规则/协作文件；不纳入 `_temp` 原始日志、既有 UI/场景资源、已安装技能及其他工作区差异。
- 中文提交说明：`docs: 增加每次任务Token用量报告约束`。提交范围为本次报告规则和增量中英记录；推送目标 `origin/codex/task-system-ui`，真实结果在执行后追加本节。
- 规则提交 `cadc9cb` 已成功推送至 `origin/codex/task-system-ui`，远端回执 `b43130c..cadc9cb`。本条回执单独提交，避免为 Git 回执重复改写全部协作入口。
