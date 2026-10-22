# 2026-10-22 豆包优先查找与 Bug 排查约束

## 需求目标

用户要求：代码查找、参考项目查找或 Bug 定位先交豆包，Codex沿豆包路线核验；路线错误或证据不足时再由Codex独立查找，以减少重复搜索和Codex额度消耗。

## 修改文件

1. `AGENTS.md`：新增查找与Bug排查顺序，规定豆包只读查找、证据格式、Codex定向核验、错误时独立查找、超时恢复和用户例外。
2. `AIconfig/config.toml`：在`[agent_policy]`增加`search_and_bug_lookup_rule`字符串配置，固定上述分工和额度目标。
3. `AIconfig/workflow.md`：新增先豆包后Codex核验的四步流程，并衔接现有开发与验收流程。
4. `AIconfig/agent/doubao_executor.md`：新增豆包承担查找与Bug定位的只读职责、交付证据格式和超时处理。
5. `Docs/AI/PROJECT_CONTEXT.md`：前置当前生效规则，注明本轮日志入口。
6. `Docs/AI/DECISIONS.md`：记录用户要求对应的查找分工决策及例外边界。
7. `Docs/AI/CURRENT_TASK.md`：记录本轮完成状态、基线、文件范围和后续执行顺序。
8. `Docs/AI/TEST_REPORT.md`：记录规则文本一致性、TOML解析和静态验证边界。
9. `项目修改日志/2026-10-22-DOUBAO-FIRST-SEARCH-RULES-CHANGELOG.md`：新增本次修改的逐项清单、设计、验证和Git回执。

本轮没有新增、修改、移动或删除客户端/服务端业务代码、Unity资源、场景或`.meta`文件；工作区内其他改动均为用户已有改动，未纳入提交。

## 设计与边界

Codex先限定问题、范围和证据要求，豆包通过本项目官方桌面工作任务进行只读查找；Codex随后按路径、行号和调用链读取真实文件并验证。只有路线错误、证据不足或结论不成立时才扩大为Codex独立查找。查找授权不等于修复授权，业务实现仍按现有开发子Agent与Codex最终验收职责执行；协作入口维护不递归派查找任务。

## 验证

- 已按项目要求读取`Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`和`TEST_REPORT.md`后再修改。
- 已核对四个约束文件与三份协作记录的规则文本；Python `tomli`成功解析`AIconfig/config.toml`，并确认配置键存在。
- 已执行`git diff --check`；本轮仅文档和TOML配置，未启动Unity、服务器或豆包业务查找任务，因此不宣称运行功能验收。

## Git

- 分支：`codex/feature-development`。
- 基线：`ec04ecb25a3532610edcfbf0cbc1eb791b014666`。
- 提交范围：上述9个文件，精确暂存；不合并`main`，不覆盖无关工作区改动。
- 提交：`1afa2a1`（`docs: 增加豆包优先查找核验约束`）。
- 推送：已成功推送到`origin/codex/feature-development`，远端回执为`ec04ecb..1afa2a1`；未合并`main`。

本段回执在首个提交后作为文档收尾提交补写；收尾提交为`6005063`，已成功推送，远端回执为`1afa2a1..6005063`。

## 未解决与交接

没有本轮业务未解决项。下一次代码查找或Bug排查默认先创建豆包只读工作任务，保存任务标识和证据，再由Codex沿路线核验；若入口阻断或路线被纠正，须记录实际原因和纠正事实。
