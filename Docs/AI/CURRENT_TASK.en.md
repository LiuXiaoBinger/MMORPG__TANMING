# File/code tasks and handoff — English agent edition

Default agent edition. `Docs/AI/CURRENT_TASK.md` retains complete Chinese developer history. This compact edition includes the current collaboration change and selected continuation records; request other historical IDs through the bounded translation route in `PROJECT_CONTEXT.en.md` `[DOCS-READ]`.

## [FILES][协作][QWEN-MEM-20261023] Qwen delegation and bilingual working memory

- Scope: `AGENTS.md`, `AIconfig/config.toml`, `AIconfig/workflow.md`, the Doubao/WorkBuddy/Qwen role files, five `.en.md` memory files, corresponding Chinese memory updates, changelog README and the task changelog. Exact per-file purposes are in `项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md`.
- Branch: `codex/qwen-bilingual-memory`; baseline: `446ff741867f5579b532b5079ecf1ef84ef99743`. No game business, resources, protocols, scenes or gateway implementation changes; existing unrelated dirty files remain outside integration.
- Qwen gets bounded lightweight lookup/comments/small edits/Chinese synchronization, one attempt then Doubao lookup or WorkBuddy editing. Complex tasks retain their existing workflow. Codex writes verified English facts; another subagent updates Chinese under the same ID and evidence.
- The five English files are compact working editions. Complete Chinese historical prose remains; unported history is translated by the requested ID when needed. Neither language nor short file size proves a fixed token/fee saving.
- Qwen's draft failed review without writing a file. WorkBuddy Hub `hub-7d83b097-b15a-476e-bcaa-fe419f5ae9da` was unclaimed and cancelled with `holdsCapacity=false`. Doubao same-project local session `38448830092170754`, Pro run `59464168910756610`, created the Qwen role file, independently read and accepted by Codex. Stalled/cancelled transport rounds are documented separately and are not counted as accepted development deliveries.
- The five Chinese files were synchronized in Doubao session `38448830092170754`, run `59469831083523330`. Codex independently checked the resulting files: 1256 original historical sections retained, 18 selected English sections present, matching migrated/new IDs and tags, no duplicate IDs, and valid TOML/model/role references. This is documentation/configuration acceptance only; final native completion, Git receipts and any later synchronization are recorded in the changelog.
- Source: user's 2026-10-23 instruction, actual files/API receipts, and the task changelog. Chinese counterpart uses `[QWEN-MEM-20261023]`.

## [FILES][协作][QWEN-LIGHT-20261023] Switch lightweight dispatch to qwen3:8b-light

- Updated active routing/configuration references to the exact Ollama model `qwen3:8b-light`; callers must pass the full tag and cannot silently use `qwen3:8b` or `qwen3:8b-cpu`.
- Updated `AGENTS.md`, `AIconfig/config.toml`, `AIconfig/workflow.md`, `AIconfig/agent/qwen_executor.md` and the paired context/decision/task records. Historical CT-455 model evidence remains unchanged.
- Local `/api/tags` check returned `qwen3:8b-light`; unavailable-tag behavior remains one attempt followed by Doubao/WorkBuddy fallback. No game code, resource or protocol files are in scope.
- The bounded Chinese-translation request to `qwen3:8b-light` timed out after 90 seconds with no output; it was not retried and caused no Qwen file write. Doubao completed the Chinese synchronization in original local session `38448830092170754`, run `59467964748331266`; the Qwen attempt does not count toward either external model's seven rounds.
- Chinese counterpart: `Docs/AI/CURRENT_TASK.md` `[QWEN-LIGHT-20261023]`.

## [FILES][NPC][协作][CT-005] Historical NPC dialog reference fix, 2026-10-22

- Recorded cause: `MainCityMgr` bound the entity to `MainRoleCtrl` using `SetROObject`, but did not store the reverse controller reference on the entity. Arrival passed null to `NpcCtrl.OpenTalk`, causing an early return. The change added `sceneEntity.SetEntityCtrl(mainRoleController)` in `BindMainRoleCompatibilityController`; navigation was not changed.
- WorkBuddy Hub `hub-1284e335-707f-4b74-8252-b84d93defbce`, native session `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`: platform round 1 was read-only diagnosis; round 2 was the first development delivery, `run-5b6704c9-656f-4068-8df2-8df1c5985ad7`, accepted with capacity released. Earlier Doubao connection obstruction was stopped and not counted as a failed delivery.
- Recorded independent checks: 289 client sources, 0 errors/38 warnings, 557 compilation-input hashes preserved; 24 pre-fix fault-reproduction assertions and 26 post-fix assertions. Thin Unity/entity/movement/UI substitutes did not verify real rendering. Preserve user `GM_Game.dll.bytes`; no hot-update deployment or Unity on-screen confirmation was established.
- Historical branch/baseline: `codex/npc-dialog-fix`, `32a7fa7`. Five recorded changed files: MainCityMgr.cs, root CURRENT_TASK/TEST_REPORT, `项目分析/项目难点.md`, and `项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md`. Evidence: `_temp/npc-dialog-20261022/arrival-followup/codex-validation`. Next behavior check requires matching hot-update code and prior UI resources, then actual NPC click/dialog text/close/reopen observation.
- Source: `Docs/AI/CURRENT_TASK.md` `[CT-005]`. This records the prior stage, not today's Unity/runtime state.

## [FILES][网络][CT-011] Historical RO network interface migration, 2026-10-22

- Recorded change: `ServerBase` to `BaseLink`, `NetSocketMgr` to `MNetClient`, preserving meta GUIDs; three Boolean Send overloads and public OnRecv carry complete messages. Client TCP/KCP share main-thread dispatch; shop connection-capture behavior remains. Five realtime protocols use KCP; other business and Gate-to-Game use TCP.
- WorkBuddy implementation/review and Codex independent checks/plugin integration were recorded. Previous timed-out native work was confirmed idle/stopped and cancelled, then scoped closure used Hub `hub-d6df4730-7ed9-4817-9d11-dc02bc773288` in native session `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`; actual problem rounds were retained.
- Recorded result: five service projects and 289 client sources compiled without errors; independent Socket/KCP 122/0, entry 8/0 and client thin boundary 54/0. The official Network.dll matched the final build. This did not deploy the user's retained `GM_Game.dll.bytes`.
- Unverified: real Unity dual-client behavior, actual animation/MP, business reconnect, early entities, end-to-end weak network and IL2CPP/HybridCLR publication. Use matching library/hot-update assemblies before runtime verification. Historical branch/baseline: `codex/network-kcp-development`, `85cda92`; manifest/Git receipts: `项目修改日志/2026-10-22-RO-NETWORK-INTERFACE-CHANGELOG.md`.
- Source: `Docs/AI/CURRENT_TASK.md` `[CT-011]`. No new network or Unity validation was run for the bilingual-memory task.
