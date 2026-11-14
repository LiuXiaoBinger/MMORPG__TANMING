# 2026-11-14 RO 手感实现调研 CHANGELOG

- 日期：2026-11-14
- 分支：`codex/feature-development`
- 任务编号：`RO-FEEL-IMPLEMENTATION-20261114`
- 写入授权：开发者于本轮收尾通过交互式弹窗明确选择「写入全部」，对应拟写清单 4 项；本文件为该清单的第 4 项。
- 任务类型：只读源码调研 + 分析归档，**未修改任何业务代码、配置、协议、表格、资源、场景或 Unity 资产**。

## 一、需求与目标

用户先询问「MMORPG 的手感由哪几个方面决定」，随后追问「RO 项目在手感方面是怎么做的」。本轮按上一轮给出的四层框架（响应与帧稳定 / 输入移动相机 / 战斗打击感 / 认知一致性），对照 RO 参考工程的真实源码与本项目既有的 20 余份 RO 分析文档，给出可复用的结论与证据路径，并按授权归档。

## 二、逐项文件清单

### 新增

| 文件 | 用途与具体变化 |
| --- | --- |
| `项目分析/RO手感实现对照-2026-11-14.md` | 新建专题分析正文。按四层框架汇总 RO 的「表现轨 / 结算轨」切分、TCP+NoDelay 与 0.1 秒移动上报、单槽输入缓存 `MWaitingDataMgr`、曲线位移代替 RootMotion、`MSkillData` 时间轴与 `At` 定时器、三个独立时间字段与接段点、打断许可与 `Freeze/KillTimeAll`、命中特效分离、CD 双表、震屏与飘字、状态互斥一致性；另列「RO 没做什么」「代价与已知缺口」「与本项目对照」「证据路径」「验证边界」共十节。全文为静态复核结论，不含未验证的实现声明。 |
| `项目修改日志/2026-11-14-RO-手感调研-CHANGELOG.md` | 本文件。记录需求、逐项文件清单、设计思路、验证结果、未解决问题、双语同步与 Git 回执。 |
| `_temp/ro-feel-review-20261114/collect.txt` | `token-monitor collect` 原始输出（各日志源扫描行数与 `0 new`）。 |
| `_temp/ro-feel-review-20261114/report.json` | `token-monitor report --json` 的 30 天历史窗口汇总，非本轮用量。 |
| `_temp/ro-feel-review-20261114/report.err` | 上述命令的 stderr（Node SQLite 实验特性警告）。 |

### 修改

| 文件 | 用途与具体变化 |
| --- | --- |
| `Docs/AI/QUERY_TASKS.en.md` | 文件末尾追加 `## [QUERY][手感][技能][网络][RO-FEEL-IMPLEMENTATION-20261114] RO game feel mapped to the four-layer frame` 及其 7 条要点（双轨切分、传输与移动同步、单槽输入缓存与曲线位移、时间轴与三字段、打断与命中分离、一致性、LYF 现状差距与验证边界）。仅追加，未改动既有段落。 |
| `Docs/AI/QUERY_TASKS.md` | 文件末尾追加同一编号的中文段，逐条对应英文 7 条要点。由子 Agent `bilingual-sync` 执行中文同步与增量一致性检查；仅追加，未改动既有段落。 |
| `Docs/AI/RECORD_INDEX.md` | 由 `.agents/skills/task-log-policy/scripts/build_record_index.py` 机械重建，刷新本编号的标题、主题标签、源文件与行段、源哈希；不复制正文。 |

### 移动 / 删除

无。

### 未新增的元数据文件

以上文件均位于项目根目录的 `项目分析/`、`Docs/AI/`、`项目修改日志/`、`_temp/`，不在 `LYF_MMO/Assets` 内，因此没有对应的 Unity `.meta` 或目录元文件。

### 工作区既有改动提示

本轮开始前，`Docs/AI/QUERY_TASKS.en.md`、`Docs/AI/QUERY_TASKS.md`、`Docs/AI/RECORD_INDEX.md` 已处于 `M`（开发者未提交改动）状态，工作区共有约 746 项既有未提交改动。本轮对这三个文件**只做末尾追加**，与既有改动共存；提交时按「保留无关的用户改动、不使用 `git add .`」约束处理，详见下文 Git 回执。

## 三、实现内容与设计思路

1. 先按项目规则检索 `Docs/AI/RECORD_INDEX.md` 命中 RO / 技能 / 移动 / 网络相关条目，不加载四份任务记录全文；命中条目与本问题无直接重叠，故未扩大为历史全文扫描。
2. 复用 `项目分析/` 已有专题作为主证据（`RO_NETWORK_TCP_KCP_DESIGN.md`、`RO_MOVE_SYNC_CLIENT/SERVER.md`、`RO_Q_COMBO_TEMPO_REFERENCE.md`、`RO_SKILL_TIMELINE_RESPONSIBILITY.md`、`RO_SKILL_CD_LIFETIME.md`、`RO_STATE_MACHINE_RUNTIME_ANALYSIS.md`、`2026-11-03-RO技能主动打断与组合接段分析.md` 等），避免重复读 RO 全量源码。
3. 对上一轮框架中尚未有既有文档支撑的两处，直接核 RO 源码：`MWaitingDataMgr.cs`（单槽输入缓存）与 `MCameraShakeComponent.cs`（曲线震屏），并核对 `客户端架构分析/05、07` 两份前期整理稿。
4. 结论组织为「总纲：双轨切分 → 四层对照 → RO 没做什么 → 代价 → 本项目差距」，刻意把「未找到顿帧 / 未接入 KCP / 无状态回滚」与「已实现」并列，避免把缺省写成能力。
5. 明确路径差异：本机 `D:/unitypro/RO_cs422nb/RO` 与历史文档中的 `F:/UnityPro/ro/Ro_cs422nb` 指同一参考工程，避免后续续作找不到文件。
6. 双语记录按规则先维护英文事实，再交子 Agent 做中文同步与增量一致性检查，最后机械重建目录。

## 四、验证方式与结果

- 验证方式为**静态源码复核 + 既有分析文档交叉核对**：读取 RO C# 源码关键类、两份客户端架构分析稿、本项目 `项目分析/` 与 `Docs/AI/` 既有记录；未启动 RO、未运行 Unity、未编译、未抓包、未做联机或手感实测。
- 两张内联示意图为讲解用途，不作为运行验证证据。
- 双语同步与一致性检查：见下方「双语同步结果」。
- `token-monitor`：`collect` 各源均为 `0 new`（codex 50 文件/11532 turns、claude-code 2/50、copilot 39/64 为 ~4 字符/token 估算）；`report --json` 的 30 天窗口为 114 会话、约 121.87M 输入 / 11.94M 输出、`costEstimated=true`、`~$954.85`，属历史汇总，不代表本轮用量。
- 上下文压缩计数：已定位当前会话 `C:/Users/lyf/.workbuddy/projects/d-unitypro-LYFMMORGP/4e3740fb-8f53-4c95-89f6-45be9b89f891.jsonl`，但配套脚本因 WorkBuddy 日志 `timestamp` 为整数毫秒而报 `AttributeError`（ISO 字符串解析失败）；精确匹配顶层 `type=compacted` 为 0 条。格式未被该脚本覆盖，因此「当前对话累计压缩次数」记为不可测，不以 0 代替。

## 五、未解决问题与边界

1. 未运行 RO / Unity，未做联机、弱网或手感实测；所有结论为静态源码能力描述，不等于线上表现。
2. 「未找到顿帧 / hitstop、完整和解、KCP 业务主流程实例化、服务端移动权威、无条件跳跃滑步取消入口」限于本轮检索的目录与关键字，不构成全工程或线上部署的否定。
3. 本项目已移植项与 RO 原实现的差异（尤其动画根运动 vs 曲线位移）需在正式 PlayMode 与双客户端联机中另验。
4. `项目分析/RO_SKILL_SEND_MODULE_FLOW.md` 指出本项目 `SetWaitSkill` 为即时交接、不保存等待队列；`项目分析/2026-11-03-RO技能主动打断与组合接段分析.md` 指出服务端 `ResultList` 逐段命中结算尚未实现。这两处是推断出的手感差距，尚未实测确认，也未在本轮改动。
5. 本轮 token 分项、账户剩余额度、上下文压缩累计次数均不可测，原因见上文。
6. `Docs/AI/QUERY_TASKS.en.md`、`.md`、`RECORD_INDEX.md` 含开发者既有未提交改动，本轮追加内容与之共存，是否随同提交由开发者决定。

## 六、双语同步结果

- 英文段由主 Agent 写入 `Docs/AI/QUERY_TASKS.en.md` 末尾；中文段由主 Agent 追加到 `Docs/AI/QUERY_TASKS.md` 末尾，两处均只追加、未改动既有段落。
- **执行者偏离（如实记录）**：原计划由子 Agent `bilingual-sync` 执行中文同步与增量一致性检查。该子 Agent 未按时产出中文段，主 Agent 为避免阻塞改为自行追加，并把子 Agent 改派为只读一致性检查。
- 主 Agent 增量自检，证据 `_temp/ro-feel-review-20261114/bilingual-check.txt`：本编号在两版各命中 1 次、无重复段；英文 7 条要点、中文 7 条要点；`MSocket.cs`、`MWaitingDataMgr`、`MCameraShakeComponent`、`MDisplacementComponent`、`SkillStateTime`、`NoResultIndexList`、`SkillComboTime`、`CorrectSyncPos`、`KCP`、`RootMotion`、`StateExclusionTable`、`token-monitor` 以及 `0.6 / 0.7 / 0.9 / 15` 共 16 项两版均命中；`2 s / 10 s / 0.1 s / 6 s` 在中文版写作「2 秒 / 10 秒 / 0.1 秒 / 6 秒」，属单位本地化，语义一致。未验证边界（只读、未运行、未找到项仅限本次检索范围）两版均保留。
- 目录条目：`Docs/AI/RECORD_INDEX.md` 第 204 行 `[QUERY][手感][技能][网络][RO-FEEL-IMPLEMENTATION-20261114] RO game feel mapped to the four-layer frame | 254-262 |`，由 `.agents/skills/task-log-policy/scripts/build_record_index.py` 机械重建，输出为 `Updated Docs/AI/RECORD_INDEX.md (titles and line ranges only).`。
- **未完成项**：子 Agent `bilingual-sync` 的只读检查在交付时未回报，已发送结束请求。按项目规则「未同步或未通过时保留未完成项」，本轮**不报告子 Agent 双语检查已通过**，只报告主 Agent 自检结果；该子 Agent 使用的技能未披露，不计入本次技能汇总。

## 七、Git 回执

- 分支：`codex/feature-development`。推送前本地与 `origin/codex/feature-development` 一致（未推送提交 0）。
- 提交 1：`0ba5265` `docs: 归档 RO 手感实现按四层框架对照分析`，仅含 `项目分析/RO手感实现对照-2026-11-14.md`（1 file changed, 135 insertions(+)）。推送成功：`41f900a..0ba5265  codex/feature-development -> codex/feature-development`。
- 提交 2：本日志文件所属提交，仅含 `项目修改日志/2026-11-14-RO-手感调研-CHANGELOG.md`；因其提交号无法在自身内容中自述，号与推送结果在最终交付回复中报告，不为回填再制造第三个提交。
- **未提交文件及原因**：`Docs/AI/QUERY_TASKS.en.md`、`Docs/AI/QUERY_TASKS.md`、`Docs/AI/RECORD_INDEX.md` 在本轮开始前已有开发者未提交改动（工作区共约 746 项），本轮只做末尾追加。若提交这三个文件，会把无关的用户改动一并带入，违反「保留无关的用户改动、不使用 `git add .`」。它们保留在工作区，提交范围由开发者决定。
- `_temp/ro-feel-review-20261114/` 已被 `.gitignore` 忽略，不提交；未合入 `main`，未执行强制推送，未跳过任何钩子。
