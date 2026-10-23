# Implementation and collaboration decisions — English agent edition

Default agent edition. Full historical Chinese decisions remain in `Docs/AI/DECISIONS.md`; request a missing old ID through `PROJECT_CONTEXT.en.md` `[DOCS-READ]`. This edition does not erase or silently rewrite historical policy.

## [DECISION][协作][QWEN-MEM-20261023] Local light tasks and paired language editions

- Authority: user's 2026-10-23 instruction. Qwen3 8B may perform lightweight lookup, comments, small bounded file changes and developer-facing Chinese synchronization. One attempt only; connection/incomplete-output/check failure hands lookup to Doubao or edits to WorkBuddy, with Doubao as appropriate alternative. No repeated Qwen rework; its attempt does not consume the receiving model's seven-round allowance.
- Qwen is an external model served by Ollama, not a native Codex agent or an already-installed autonomous file executor. The caller supplies bounded inputs, performs approved tool actions and verifies actual outcomes. Complex business/protocol/concurrency tasks retain the established external developer workflow.
- Keep five Chinese `.md` files for developers, create five matching `.en.md` files for default agent reads. Codex maintains English acceptance facts; another subagent synchronizes only the changed Chinese record. Preserve identical tags/IDs, paths, session/run identities, real rounds, failures and unverified limits. Do not close a task with unsynchronized or failed records.
- English is compact working memory, not an up-front full historical translation. Preserve all Chinese history. When a needed old ID is missing in English, delegate its bounded translation and record the English facts before Codex reads them. Do not silently mark old running/completed states current or guess corrupted source text.
- Root `AGENTS.md` and `AIconfig` carry these overrides. They take precedence over older default-Doubao lookup rules and the old all-Chinese-prose instruction; user replies/business comments remain Chinese. Language alone does not guarantee fixed token/fee savings.
- No gameplay, resource, protocol or gateway redesign. Branch `codex/qwen-bilingual-memory`; baseline `446ff741867f5579b532b5079ecf1ef84ef99743`; main remains untouched. Scope/evidence/actual integration: `项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md`.
- Chinese counterpart: `Docs/AI/DECISIONS.md` `[QWEN-MEM-20261023]`.

## [DECISION][协作][QWEN-LIGHT-20261023] Exact model tag for lightweight dispatch

- The user's follow-up makes `qwen3:8b-light` the only active Qwen model for lightweight tasks. The request must carry the exact tag; `qwen3:8b` and `qwen3:8b-cpu` are historical/alternative tags, not silent fallbacks.
- If the exact tag cannot be reached, apply the existing one-attempt handoff to Doubao or WorkBuddy. Do not retry Qwen or substitute a tag without recording and approval.
- Chinese counterpart: `Docs/AI/DECISIONS.md` `[QWEN-LIGHT-20261023]`.
