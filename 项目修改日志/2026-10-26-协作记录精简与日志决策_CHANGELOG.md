# 协作记录精简与任务日志按需写入

- 日期：2026-10-26（Asia/Shanghai）。任务编号：`CONTEXT-CLEANUP-20261026`。
- 需求：梳理旧任务、移除启动入口中无必要的历史过程，在技能中加入每次任务收尾判断是否写日志及列明实际日志内容的规则。
- 分支：`codex/context-log-cleanup`；基线 `aaead8e6ec29eef1a4bddf0eef1827e714ee1c34`。不合并 main。

## 修改思路与边界

- 先逐字节保存 12 份原入口，再精简活动区。历史失败、未验证事项、原模型/任务/轮次身份和未译历史仍可按标题/编号与行号追溯；不将归档推断为完成。
- 中文保留的最近任务与英文沿用各自既有事实；旧记录未做全量重新验收。客户端后续字体/TMP/任务界面事实通过独立交接入口指向原文，不能以较早根目录结果覆盖。
- 新技能只增加收尾日志判断，不要求每次询问用户；实际文件修改有正式日志，新调查/验证事实更新对应入口，无新事实的问答/状态不创建流水。
- 整理前工作区有 279 个变动路径，包含本次要整理的旧文档。本次 12 份文档的既有事实完整收入快照；业务代码、Unity资源、表格、既有派工技能等其他用户改动不纳入提交、不覆盖。
- 不清理 `_temp`，不改模型/极速设置。本任务已由用户后续要求转为项目记录整理。

## 逐项文件清单

| 文件 | 操作与具体内容 |
|---|---|
| `AGENTS.md` | 修改：按需日志决策、归档按编号定位、纠正英文读取入口，不再为单条历史查证强制翻译；保留全局业务规则。 |
| `.agents/skills/task-log-policy/SKILL.md` | 新增：文件改动/新证据/无新事实三类日志判断、每种日志职责、最终交付实际日志清单。 |
| `项目修改日志/README.md` | 修改：说明按需记录与本次历史归档位置、检索方式。 |
| `Docs/AI/PROJECT_CONTEXT.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/PROJECT_CONTEXT.md` | 新增：`Docs/AI/PROJECT_CONTEXT.md` 整理前逐字节快照（124995 字节，SHA256 见 manifest）。 |
| `Docs/AI/PROJECT_CONTEXT.en.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/PROJECT_CONTEXT.en.md` | 新增：`Docs/AI/PROJECT_CONTEXT.en.md` 整理前逐字节快照（16657 字节，SHA256 见 manifest）。 |
| `Docs/AI/CURRENT_TASK.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/CURRENT_TASK.md` | 新增：`Docs/AI/CURRENT_TASK.md` 整理前逐字节快照（810963 字节，SHA256 见 manifest）。 |
| `Docs/AI/CURRENT_TASK.en.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/CURRENT_TASK.en.md` | 新增：`Docs/AI/CURRENT_TASK.en.md` 整理前逐字节快照（57502 字节，SHA256 见 manifest）。 |
| `Docs/AI/TEST_REPORT.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/TEST_REPORT.md` | 新增：`Docs/AI/TEST_REPORT.md` 整理前逐字节快照（925011 字节，SHA256 见 manifest）。 |
| `Docs/AI/TEST_REPORT.en.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/TEST_REPORT.en.md` | 新增：`Docs/AI/TEST_REPORT.en.md` 整理前逐字节快照（50359 字节，SHA256 见 manifest）。 |
| `Docs/AI/DECISIONS.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/DECISIONS.md` | 新增：`Docs/AI/DECISIONS.md` 整理前逐字节快照（114533 字节，SHA256 见 manifest）。 |
| `Docs/AI/DECISIONS.en.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/DECISIONS.en.md` | 新增：`Docs/AI/DECISIONS.en.md` 整理前逐字节快照（4205 字节，SHA256 见 manifest）。 |
| `Docs/AI/QUERY_TASKS.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/QUERY_TASKS.md` | 新增：`Docs/AI/QUERY_TASKS.md` 整理前逐字节快照（112086 字节，SHA256 见 manifest）。 |
| `Docs/AI/QUERY_TASKS.en.md` | 修改：根目录活动入口精简；保留当前结构/最近事实，历史按归档索引取段。 |
| `项目修改日志/协作记录归档/2026-10-26/Docs/AI/QUERY_TASKS.en.md` | 新增：`Docs/AI/QUERY_TASKS.en.md` 整理前逐字节快照（8994 字节，SHA256 见 manifest）。 |
| `LYF_MMO/Docs/AI/CURRENT_TASK.md` | 修改：删除第二套活动状态，改为根入口与原文证据指引。 |
| `项目修改日志/协作记录归档/2026-10-26/LYF_MMO/Docs/AI/CURRENT_TASK.md` | 新增：`LYF_MMO/Docs/AI/CURRENT_TASK.md` 整理前逐字节快照（12018 字节，SHA256 见 manifest）。 |
| `LYF_MMO/Docs/AI/TEST_REPORT.md` | 修改：删除第二套活动状态，改为根入口与原文证据指引。 |
| `项目修改日志/协作记录归档/2026-10-26/LYF_MMO/Docs/AI/TEST_REPORT.md` | 新增：`LYF_MMO/Docs/AI/TEST_REPORT.md` 整理前逐字节快照（10849 字节，SHA256 见 manifest）。 |
| `项目修改日志/协作记录归档/2026-10-26/.gitattributes` | 新增：禁用快照文本换行转换，确保提交和克隆后 SHA256 仍对应原始字节。 |
| `项目修改日志/协作记录归档/2026-10-26/INDEX.md` | 新增：所有历史二级标题、编号和原行段的恢复索引。 |
| `项目修改日志/协作记录归档/2026-10-26/manifest.json` | 新增：12 份快照原路径、字节数与 SHA256。 |
| `项目修改日志/协作记录归档/2026-10-26/README.md` | 新增：快照来源、历史状态边界和检索规则。 |
| `项目修改日志/2026-10-26-协作记录精简与日志决策_CHANGELOG.md` | 新增：本任务唯一完整修改清单、验收、未决项、额度摘要与 Git 回执。 |

## 验证与中英文同步

- 静态验收通过：12 份原始快照字节数与 SHA256 相符，1,404 个原始二级标题均有准确恢复行段，30 个保留段与原语言快照一致；活动入口合计 2,248,172 → 65,499 字节（约减少 97.1%）。这是文件体积，不是实际 token/费用节省率。
- 新技能 quick_validate 通过；修改范围 git diff --check 通过；初始工作区中本任务范围以外的用户文件哈希无变化。快照 .gitattributes 禁止自动换行转换，暂存后另验 Git blob 字节。验证证据 `_temp/context-log-cleanup-20261026/verification.json`。文档/技能改动不运行 Unity/服务器编译。
- 首次验证真实失败：中文保留段被加倍空行，原子 Agent 第 2 轮定向恢复原语言快照后通过；原第 1 轮正文哈希结论失效。技能验证器首次因 Windows 默认 GBK 解码失败，设置 PYTHONUTF8=1 后原验证器通过；未修改验证器。
- Qwen 可用性检查：`http://127.0.0.1:11434/api/tags` 返回 HTTP 502；未发起模型生成、未切换其他 Qwen 标签。中文同步与一致性检查由 Codex 原生子 Agent `/root/bilingual_cleanup_delta` 执行（实际模型未披露），真实 2 轮。第 2 轮交付检查通过：5 个新增段标签/编号/引用一致，15 个中文保留段按原文恢复；只对新增/修改段做双语核对，不重译历史。证据 `_temp/context-log-cleanup-20261026/bilingual-review.md`。

## 可观测消耗

- 已运行 token-monitor collect 和 report（Codex 来源、LYFMMORGP、最近 1 天）。原始输出：`_temp/context-log-cleanup-20261026/token-collect.txt`、`token-report.txt`。
- 该采样：2 会话、13 turns、65.7k 输入、3.7k 输出、432.6k 缓存读取、87% 缓存命中，估算 `~$0.25`；覆盖 Codex 1/1d。Codex 适配器为 experimental；窗口含其他工作、未覆盖完整当前任务，不能作为本任务精确消耗或订阅余额。
- 上下文与项目规则、用户需求、工具输入输出、子 Agent 协作、重复读取/返工/压缩/验证：当前工具不按这五项提供可信独立统计，均为未记录/不可测；不会用 0 代替。账户剩余额度本地日志不可测。

## 未解决问题与下一步

- 历史记录中未验证的运行行为保留原边界，本次不逐条证明已解决；继续对应功能时只取相关编号。
- 物理证据未删除。删除的是活动入口重复/过时内容；历史原文完整归档。档案会占磁盘，但不再默认注入上下文。
- 原始双语快照保留旧版本自身差异；本次子 Agent 只对新修改段同步检查，不把历史差异冒称已修正。

## Git 回执

- 本次主变更已提交：`ef1e4b9`（`docs: 精简协作入口并增加任务日志按需记录规则`），32 个文件，与本日志清单完全一致，无范围外暂存项。
- 12 份归档的暂存 Git blob SHA256 已与 manifest 核对一致，确认 Git 未转换原始字节；验证通过后未再改相应输入。
- `git push -u origin codex/context-log-cleanup` 成功，新分支已在 origin 建立并设置跟踪。未合并 main、未强制推送。
- 本条真实回执在主变更推送成功后补记；补记提交由 Git 历史及最终交付标识，不为获得自身提交号反复改写日志。

## 实际技能成果

- token-budget：先检索再取段，12 份原文先归档再从活动入口移除旧过程，按编号/行段恢复证据。
- repo-context-ledger：按其先判断新事实和有界路由原则复用项目现有五类账本，不为文档整理初始化第二套账本或复制私有任务；维护新事实、验证边界和可恢复历史。
- skill-creator：创建 task-log-policy 项目技能并通过官方 quick_validate；保持技能简短、按具体结果决策。
- task-log-policy：本任务有文档/技能改动，故写一份完整修改日志；同任务返修复用原记录，不因整理另造查询流水。
- token-monitor：执行本地 collect/report，保留实验性来源与不可独立归因边界，不推算订阅余额。
- project-subagent-coordination：限定中文同步范围，Qwen 可用性检查失败后使用其他子 Agent；沿原子任务修复换行问题并核对交付范围与证据。
- 子 Agent 实际应用 task-log-policy，记录到同一主任务；其底层模型未披露，不冒称外部模型调用。
