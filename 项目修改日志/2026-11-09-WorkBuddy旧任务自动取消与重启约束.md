# WorkBuddy旧任务自动取消与重启约束（2026-11-09）

用户要求：旧任务卡住网关时直接取消，取消无效就结束对应进程并重启，不再重复询问。用户随后明确批准本日志，不新增其他协作摘要。

## 修改文件

- `C:/Users/lyf/.codex/skills/workbuddy-executor/SKILL.md`：新增旧任务阻塞恢复流程，覆盖取消、定向结束进程与重启、Hub和原生队列两层占用核对、真实操作者取消、保留当前模型和任务身份。个人技能在本机生效，不在项目Git中。
- `.agents/skills/project-subagent-coordination/SKILL.md`：明确由主Codex自主恢复失效执行器；仅处理实际阻塞进程，保留旧成果，不把正常长任务误判为遗留占用，不重复执行当前任务。
- `AGENTS.md`：写入同一长期授权及规则优先级，取消与重启无需再次确认。
- 本文件：记录用户要求、实际范围、校验和Git回执。没有修改网关源码或游戏业务。

## 验证与边界

两个技能均通过 `python -X utf8 .../skill-creator/scripts/quick_validate.py` 校验。首次按系统默认GBK读取中文失败，改为Python UTF-8模式后通过，没有改校验器。三个文件均证明移除本次插入段后与修改前字节一致；暂存从HEAD投影本轮插入段，未混入既有工作区改动。定位目录生成已执行，未新增协作记录。

本轮仅改规则，未重新演练取消或进程重启。上一装备任务的真实旧占用恢复证据见 `_temp/equipment-ui-layout-20261109/revision5/operator-cancellation.json` 与 `operator-cancellation-domestic.json`，不将旧验证算作本轮测试。

## 执行与技能

主Codex直接编辑技能及规则；没有将此规则任务交给WorkBuddy执行。`skill-creator`用于条款修改和格式校验；`task-log-policy`用于先完成规则、再经弹窗授权写本记录；`token-monitor`完成collect/report和当前对话压缩计数。

原始证据在 `_temp/workbuddy-stale-policy-20261109/`。上下文与项目规则、用户需求、工具输入输出、子Agent/WorkBuddy协作、重复读取/返工/压缩/验证五类token分摊均未记录/不可测；项目最近一天实验性报告不是本任务用量，账户余额不可测。主对话原始日志只覆盖至北京时间2026-11-08 22:08:44，累计已观测5次compacted，之后累计不可测。

## Git回执

分支 `codex/feature-development`。项目内两个规则文件提交 `b8bc37ff4e43f5aa38f30cb11419504c0b37e92c`，7行新增，推送成功，远端从c54edb4推进至b8bc37f。个人技能不在本项目仓库。此次获准的日志单独提交推送；不合入main，不纳入其他任务改动。
