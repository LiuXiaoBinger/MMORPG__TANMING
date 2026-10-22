# 2026-10-22 豆包、WorkBuddy额度节省与Codex失败回退约束

## 需求目标

固定后续工作方式：豆包负责查找定位，Codex沿路线核验，WorkBuddy按核验结论开发和自检，Codex最终复核、测试、记录与Git集成；如果豆包和WorkBuddy仍无法解决，停止继续依赖两者，由Codex独立接手。

## 设计规则

- Codex先拆解问题、限定范围和验收，不提前全量扫描。
- 豆包只读返回真实路径/行号、必要调用链、证据、复现信息和未确认项。
- Codex只核验豆包报告列出的文件、直接调用者和必要测试；路线成立后不重复全量搜索。
- WorkBuddy只接收已核验结论和逐项文件范围，不重新扫描整个项目。
- 有明确修正方向时沿原任务补充或返工；确认两者仍无法解决时，立即停止循环委派，不凑固定轮数，Codex独立查找、设计、编写或修复并验证，记录失败证据和接手原因。
- 单次任务等待超时先读取原任务状态，不把超时直接判定为失败；Codex接手修改前确认原执行者已结束或停止，避免并发写同一文件。
- 任务单和交付报告使用精简格式，完整源码、日志和原始证据留在`项目分析/`、`项目修改日志/`或`_temp/`，后续引用路径和摘要。

## 修改文件

1. `AGENTS.md`：新增额度节省分工、失败回退、任务单和精简交接规则；明确回退时Codex可独立处理当前任务。
2. `AIconfig/config.toml`：新增额度流程和精简交接配置，调整默认业务委派、主Agent边界、失败处理和禁止项，使回退规则与主流程一致。
3. `AIconfig/workflow.md`：新增分工、失败回退和精简任务单/交付格式，修正旧的绝对委派描述。
4. `AIconfig/agent/doubao_executor.md`：明确豆包查找失败后停止重复委派及精简报告格式。
5. `AIconfig/agent/workbuddy_executor.md`：明确WorkBuddy接收核验结论、针对性修正及Codex回退条件。
6. `Docs/AI/PROJECT_CONTEXT.md`：记录当前生效的额度分工和回退授权。
7. `Docs/AI/DECISIONS.md`：记录短链路分工、失败证据和回退决策。
8. `Docs/AI/CURRENT_TASK.md`：记录本轮约束同步及后续执行方式。
9. `Docs/AI/TEST_REPORT.md`：记录规则覆盖范围和静态验证边界。
10. `项目修改日志/2026-10-22-AGENT-QUOTA-FALLBACK-RULES-CHANGELOG.md`：记录本轮逐项文件、设计、验证和Git结果。

本轮没有修改客户端/服务端业务代码、Unity资源、场景或`.meta`文件；工作区其他改动均保留。

## 验证

- 已按顺序读取`Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`和`TEST_REPORT.md`。
- `AIconfig/config.toml`经Python tomli解析通过，额度流程和精简交接键均存在，`git diff --check`通过；已核对规则关键词及失败回退例外。首次断言误用“Codex独立接手”而正文为“Codex独立查找”，修正验证词后通过，不属于配置故障。
- 本轮不启动豆包、WorkBuddy、Unity或服务器任务；只验证配置与记录，不把规则同步报告成运行验收。

## Git

- 分支：`codex/feature-development`。
- 基线：`93e556ece8e12f996303f50db5f0f6fa7f262171`。
- 提交范围：本日志列出的10个文件，精确暂存，保留无关用户改动。
- 提交：`88fd637`（`docs: 固定额度节省分工与失败回退规则`）。
- 推送：已成功推送到`origin/codex/feature-development`，远端回执为`93e556e..88fd637`；未合并`main`。

## 未解决与交接

没有本轮业务未解决项。后续任务按豆包查找、Codex核验、WorkBuddy实现、Codex验收执行；确认两者无法解决时由Codex独立接手，不再循环依赖。
