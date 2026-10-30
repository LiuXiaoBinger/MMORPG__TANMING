---
name: token-monitor
description: >
  Measure and explain AI coding-agent token spend from local session logs — activity
  breakdown, cache/rework/context-surface signals, per-task duplicate work, and priced
  recommendations. Use when the user asks where their tokens or agent costs are going,
  why a project is expensive, whether their agent usage is efficient, what an MCP server
  or a big tool result is costing them, asks to install / run / interpret token-monitor,
  or needs the required end-of-task context compaction count in LYFMMORGP.
---

# token-monitor

A local, zero-dependency CLI that parses the session logs Claude Code, Gemini CLI, Codex, Cursor, Antigravity and Copilot Chat already write, and answers *what the tokens bought*. No API keys, no server, no telemetry. Requires Node ≥ 24.

Run it with `npx -y @ryandemelo/token-monitor <command>`, or `token-monitor <command>` when it is installed globally.

## The workflow

| Step | Command | What it answers |
|---|---|---|
| 1 | `collect` | Scan local logs into `~/.token-monitor/token-monitor.sqlite`. Idempotent — safe to re-run any time, and required before anything else. |
| 2 | `report [--trend]` | Where the tokens go, what they cost, the persona, the priced recommendations, and follow-through on earlier advice. |
| 3 | `context` | What the standing surface costs: session floor, tool-result carry, per-MCP-server spend, connected-but-unused servers. |
| 4 | `analyze` | Which *sessions and habits* burn the tokens: expensive sessions, fix loops, context bloat, cold restarts, subagent fan-out, tool error rates. |
| 5 | `categorize` | Cluster sessions by task intent; flag the same task re-derived across projects. |
| 6 | `relay` | Prompts that repeat an earlier session's output — text hand-carried between sessions instead of handed over. |
| 7 | `rules [<key>]` | The catalogue of waste rules and which fire on this window. |
| — | `html --out report.html` | Self-contained dashboard, no server or external assets. |
| — | `merge <exports>` | Team rollups from signed member exports. |

## Reading the numbers

- **Cache hit ratio** — cache reads cost ~10% of fresh input. The single biggest cost lever.
- **Rework ratio** — spend on coding/testing turns after the first failure in a session. High rework usually means skipped planning. Declined permission prompts are not failures.
- **Think:code** — planning + exploration tokens per coding token. Low values correlate with high rework.
- **Context bloat / cold restarts / session floor / tool-result carry** — four different context problems with four different remedies: compact at task boundaries; batch prompts inside the cache window; trim what is loaded on every session; bound what tools return.
- **Subagent share** — spend from Task/Agent fan-out. Descriptive, never a verdict: delegating heavily is often exactly right. Read it against what the fan-out produced.
- **Personas** — architect, surgeon, explorer, sprinter, firefighter, balanced — with recommendations tailored to each.

Recommendations carry evidence (the worst sessions, by id and date) and a priced estimate from the user's own model mix. Once a recommendation fires, follow-through tracks whether its metric actually moved.

## Rules for using this tool

1. **Report what it printed.** Never re-estimate, extrapolate, or smooth a number into a better story. If the coverage line reports gaps, say so before quoting anything.
2. **Keep the `~`.** It marks placeholder prices and estimated token counts. Quoting an estimate as exact is the fastest way to make the whole report untrustworthy.
3. **Names stay local.** Tool names, MCP server names and subagent type names are shown on the machine and never leave it. Do not paste them into files, commits, issues, or messages unless the user explicitly asks for exactly that. Project names are basenames and are safe to discuss with the user, but exports carry aggregates only.
4. **`--llm` sends data off the machine.** `analyze --llm` hands aggregates (counts, ratios, redacted tool names, project basenames — never prompts or code) to whichever agent CLI is installed. Say so before running it; skip it if project names are sensitive.
5. **Absent is not zero.** Sources that cannot report something (Cursor has no cache split, Copilot has no token counts, only Claude Code persists tool results and subagent transcripts) report *nothing*. Never read that as "zero waste".
6. **One finding at a time.** The user came with a question, not a dashboard. Lead with the largest priced lever and its evidence.

## 每次任务的上下文压缩次数

每次任务最终回复在用量报告中增加：`上下文压缩：当前对话累计 N 次（统计截止时间）。` 只报告这个对话从开始至今的累计次数，不报告“本任务次数”。

- 从当前 Codex 原始会话 JSONL 的顶层 `type=compacted` 记录计数，一条记录计一次。不得按摘要文本、`replacement_history`、token 下降、窗口数或其他模型自报推算；此计数不区分自动与手动触发。
- 同一对话中的新任务、澄清、返修、自动续作与上下文恢复均不重置计数。只统计当前对话，不合并其他对话或子 Agent 对话的压缩次数。
- 使用配套脚本，只读取日志并输出计数、时间范围和覆盖边界，不输出会话正文：

```powershell
python -X utf8 .agents/skills/token-monitor/scripts/count_context_compactions.py --session-file "当前对话JSONL绝对路径" --output "D:/unitypro/LYFMMORGP/_temp/本任务目录/context-compactions.json"
```

- 当前对话日志缺失、损坏或格式未覆盖时，报告已观测次数及缺口，不把不完整计数写成累计总次数。同一对话存在多个日志文件时仅汇总属于该对话的记录，并确认没有重复；无法完整覆盖时保留不可测边界。
- 日志完整且确实没有压缩事件时，报告 `0 次`；没有证据时不得以 0 代替不可测。原始计数证据留在 `_temp/`，正式记录仍按项目日志授权规则处理。

## When the user wants more

- Team rollups: each member runs `report --json > me.json`, the lead runs `merge exports/*.json --team team.yaml`. Exports are Ed25519-signed and carry aggregates only.
- A waste pattern the tool does not measure: a rule is one file in `src/rules/`, one fixture and one test — see CONTRIBUTING.md in https://github.com/ryandemelo/token-monitor.
