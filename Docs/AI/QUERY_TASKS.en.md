# Queries and investigation — English agent edition

Default agent edition. `Docs/AI/QUERY_TASKS.md` preserves the complete Chinese query history. Query-only work updates this language pair; formal document outputs leave short file/verification references in the execution records. Missing historical IDs are translated by a subagent on demand under the route in `PROJECT_CONTEXT.en.md` `[DOCS-READ]`.

## [INDEX][协作][QUERY-INDEX] Query routing

- Search level-two headings by existing topic tags, then read the selected record ID only, through the next level-two heading. For example: `rg -n '^## .*\[网络\]' Docs/AI/QUERY_TASKS.en.md`.
- Retain original CT/TR IDs and original dates. A historical proposal is not implementation evidence; test records and query conclusions keep their distinct roles. Use the same ID in the Chinese and English section when migrating an old query. Never load the entire Chinese history to answer one question.
- Source: `Docs/AI/QUERY_TASKS.md` `[QUERY-INDEX]`, as overridden by the 2026-10-23 bilingual-memory instruction.

## [QUERY][协作][CT-454] New-task context usage inquiry, 2026-10-23

- The inquiry did not create a task or modify business code. New-task prompt size includes task instructions, product/system instructions, project AGENTS and any loaded records/skills. The available interface did not supply a verified exact pre-dispatch token estimate or a per-new-task usage measurement.
- Earlier character-based estimates were rough, not a tokenizer measurement or a guaranteed total. Official OpenAI Docs search returned HTTP 403; no model limit, fixed cost or exact token count was established from that page. Keep task briefs short, scope files precisely and avoid copying full conversation history.
- Source: `Docs/AI/QUERY_TASKS.md` `[CT-454]`. This preserves the inquiry's measurement limits rather than treating a rough estimate as a verified budget.

## [QUERY][协作][CT-455] Local Qwen3 8B feasibility smoke check, 2026-10-23

- Local Ollama `/api/tags` returned `qwen3:8b`, 8.2B, Q4_K_M, model file size 5225388164 bytes. `/api/show` advertised completion/tools/thinking and context metadata 40960. GPU query returned RTX 4070 Laptop, 8188MiB VRAM. In the 4096-window, think=false smoke test, `/api/ps` reported context_length 4096 and size_vram 5586592726 bytes. Metadata capacity does not establish full-window performance.
- Tool-selection sample produced `read_project_file` with `Assets/Scripts/Example.cs`, 198 prompt/26 output tokens, about 4.17 seconds. The synthetic file was not read; full tool execution, response return and autonomous multi-step operation were not validated.
- Isolated C# sample corrected `i <= values.Count` to `<`, changed foreach to for and retained negative-value skipping: 231 prompt/168 output tokens, about 4.72 seconds. It omitted requested method/parameter/several-local Chinese comments. No sample was written or compiled in the project; this is a constraint-following failure, not a completed project-development benchmark.
- Recommended bounded tasks and final Codex review/compilation. Complex networking/threading/cross-module reliability, expanded context, native Unity behavior and real-project task quality were not established. A local execution/tool layer is distinct from the model. No zero-Codex-usage claim or fixed speed/saving ratio follows from these samples.
- Official Ollama tool-calling page was reachable: `https://docs.ollama.com/capabilities/tool-calling`; Qwen's Hugging Face card request failed to connect and was not used as evidence. Later dispatch authorization supersedes this historical proposal under `[QWEN-MEM-20261023]` in the decision/task/context editions.
- Source: `Docs/AI/QUERY_TASKS.md` `[CT-455]`.
