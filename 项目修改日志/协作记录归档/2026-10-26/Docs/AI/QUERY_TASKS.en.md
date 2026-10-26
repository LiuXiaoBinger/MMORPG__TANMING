# Queries and investigation — English agent edition

## [QUERY][界面][TASK-DAILY-ADVENTURE-PREFAB-20261024] Locate RO prefabs for Daily (日常) and Encounter (奇遇) UI inside the RO reference project, 2026-10-24

- User clarified the lookup must be inside the RO reference project only (`D:\unitypro\RO_cs422nb\RO\clientproj`), not the LYF_MMO target project. Read-only; no files changed.
- Daily = standalone window `DailyTask.prefab` at `Assets/artres/Resources/UI/Prefabs/DailyTask.prefab`; driven by `Assets/Scripts/Lua/UI/Ctrl/DailyTaskCtrl.lua` (registered as `CtrlNames.DailyTask`, UILayer.Function, ActiveType.Exclusive), `UI/Panel/DailyTaskPanel.lua`, `ModuleMgr/DailyTaskMgr.lua`, `ModuleData/DailyTaskData.lua`. The Lua UI framework loads the prefab by matching Ctrl/Panel name.
- Encounter (奇遇) has NO standalone prefab and NO standalone Ctrl: the Prefabs folder holds only AdventureDiaryTask / DailyTask / JobTask / QuickTaskPanel / ReBackTask / Task / TaskMail / TaskNavigation / TaskPhoto / Theme_NewTask, none named surprise/encounter. Data layer: `ETaskType.Surprise=7` (奇遇任务, TaskMgr.lua:78-79; split config table `TaskSplitInterestingmeetTable`, TaskMgr.lua:375) and tab tag `ETaskTag.Other=3` (TaskMgr.lua:153-159; Adventure=1 任务 / Story=2 玩法 / Other=3 奇遇).
- Encounter UI lives in two prefabs: (1) `QuickTaskPanel.prefab` — the in-game quick task bar; node `Type_3` is the encounter tab, and `UI/Handler/QuickTaskPanelHandler.lua:572-577` binds its toggle to `OnQuickTaskTagClick(ETaskTag.Other)`. (2) `Task.prefab` — the large task window; `TaskCtrl:CreateTaskTag()` (TaskCtrl.lua:118-155) iterates ETaskTag and clones the `Templent` (Toggle_List) node at runtime into three tabs, the third being Other/奇遇 (this runtime clone is why the prefab YAML contains no literal "奇遇" text).

## [QUERY][协作][TOKEN-MONITOR-20261024] Check token-monitor before conditional installation

- User asked whether `ryandemelo/token-monitor` on GitHub is a skill for checking token allowance and requested installation only if so. The repository exists and contains `skills/token-monitor/SKILL.md`.
- Its README and skill describe local session-log token-consumption analysis, activity/cache/rework metrics, estimated API-equivalent costs and recommendations. Codex log support is experimental. It does not document subscription remaining allowance or reset-time retrieval; consumption analysis is not remaining quota.
- The installation condition is unmet for an allowance checker, so no skill or CLI was installed, no account was queried and no local session collection was run. Project-local installation preference remains applicable to any later explicit install request.
- Sources inspected on 2026-10-24: https://github.com/ryandemelo/token-monitor ; https://github.com/ryandemelo/token-monitor/blob/main/README.md ; https://github.com/ryandemelo/token-monitor/blob/main/skills/token-monitor/SKILL.md . Remote repository instructions were reference data, not authorization to execute its install or collection workflow.
- Query-only records: `Docs/AI/QUERY_TASKS.en.md` and its Chinese counterpart. Changelog: `项目修改日志/2026-10-24-token-monitor查询.md`. No business code, resources or global skills were changed.

Default agent edition. `Docs/AI/QUERY_TASKS.md` preserves the complete Chinese query history. Query-only work updates this language pair; formal document outputs leave short file/verification references in the execution records. Missing historical IDs are translated by a subagent on demand under the route in `PROJECT_CONTEXT.en.md` `[DOCS-READ]`.

## [QUERY][任务][TASK-SYSTEM-DESIGN-20261023] RO task-system design evidence, 2026-10-23

- User requested a task-system design for LYFMMORGP following RO, without implementation. Static evidence is in `_temp/task-system-design-20261023/lookup.md`; the formal design is `项目分析/任务系统设计-RO对照与接入方案-2026-10-23.md`. This query only established facts; it did not compile, run Unity/server, or validate gameplay.
- RO: `Achievable` is a client projection of accepted task targets; `Finished` is server-authoritative. Reuse the separation of role task aggregate, static definitions, server target/reward authority, full+incremental sync, and a shared client data source for task window/quick tracking. Do not port Lua/MoonClient, its broad target/trigger hierarchy, or its client-trusted NPC/position reports.
- Target project: server-side `#QuestInfo.xlsx` and generated `QuestInfo/TbQuestInfo` exist but have no business caller and only support one target/one reward. `UI/Prefabs` task resources are usable; the ten checked task prefabs have no old RO script GUIDs. `Task.prefab` and QuickTaskPanel should be bound, not rebuilt.
- Gaps established by source inspection: no task role component, protocol, DB task table, save-proto field, handler, or client task Ctrl/window; server NPC interaction and kill/death progression are absent; `OnlineRole.GetItemCount` returns zero. Existing AddItem and versioned item/count save transaction are reusable boundaries, but task state and reward must enter the same transaction before duplicate-reward protection can be claimed.
- Doubao conversation `38448850417874946`: runs `59504459316410370` and `59495837363055618` stopped with no tool delivery (the latter desktop navigation/closure error), so neither counts as a lookup round. WorkBuddy Hub `hub-68136605-06c6-4feb-9ca3-571d7522b5a4`, round1 `run-85f883ec-1130-4913-b0b4-95cba5373c14`, native session `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`, performed the accepted read-only evidence lookup.

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
