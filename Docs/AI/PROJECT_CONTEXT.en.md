# Project working context — English agent edition

English is the default agent reading edition. `Docs/AI/PROJECT_CONTEXT.md` is the complete Chinese developer edition. This compact edition preserves current collaboration rules and essential continuation facts; it is not a full translation of historical entries. Historical statements retain their original date and verification limits.

## [INDEX][协作][DOCS-READ] Read only the relevant section

- Follow root `AGENTS.md` first. Read this section and `[DOCS-ROUTES]`, then locate headings by topic and read the selected unique ID through the next level-two heading. For example: `rg -n '^## .*\[网络\]' Docs/AI/QUERY_TASKS.en.md`.
- Preserve existing category tags, Chinese topic tags and record IDs across language editions. Categories include `[FILES]`, `[VERIFY]`, `[DECISION]`, `[QUERY]`, `[STRUCTURE]` and `[HANDOFF]`; topics include network, navigation, NPC, skill and collaboration under their existing Chinese tags.
- Do not load all five files or both languages for a routine task. Only inspect necessary callers and verification evidence. File names, paths, protocol IDs, session/run identities, actual round counts, failures and unverified boundaries must remain exact.
- If a historical ID is absent from English, delegate a bounded translation of that Chinese section to Qwen or another subagent, append the factual English section under the same tags/ID, then read it. Chinese originals remain intact. Direct Chinese inspection is exceptional when checking translation fidelity or missing evidence; never load an entire archive.
- Historical references may point to `项目修改日志/协作记录归档/2026-10-23/` with original line numbers. A historical “running”, “pending” or “completed” statement does not establish today's execution state. Some older Chinese records contain corrupted characters; flag uncertainty rather than reconstructing missing facts.
- After a change, Codex updates English facts; another subagent updates the corresponding Chinese section from that task's delta and evidence. Qwen gets one attempt for lightweight synchronization; fall back on failure. Verify matching IDs, paths, execution identities, rounds, results and unverified limits before closing.
- Source: `Docs/AI/PROJECT_CONTEXT.md` `[DOCS-READ]`, plus the user's 2026-10-23 bilingual-memory instruction and root `AGENTS.md`.

## [STRUCTURE][协作][DOCS-ROUTES] Five responsibilities, two editions

| Purpose | Agent reading/writing edition | Chinese developer edition |
|---|---|---|
| Stable context and routing | `Docs/AI/PROJECT_CONTEXT.en.md` | `Docs/AI/PROJECT_CONTEXT.md` |
| File/code changes and handoff | `Docs/AI/CURRENT_TASK.en.md` | `Docs/AI/CURRENT_TASK.md` |
| Implementation and authorization decisions | `Docs/AI/DECISIONS.en.md` | `Docs/AI/DECISIONS.md` |
| Builds, checks, failures and verification limits | `Docs/AI/TEST_REPORT.en.md` | `Docs/AI/TEST_REPORT.md` |
| Queries, investigation and unimplemented proposals | `Docs/AI/QUERY_TASKS.en.md` | `Docs/AI/QUERY_TASKS.md` |

- File changes update task and verification records. Pure queries update only the query pair; formal document outputs leave short file/verification references in the execution pair. Do not duplicate the analysis body across records.
- Full change manifests belong in `项目修改日志/`; architecture, reference studies and topic guides belong in `项目分析/`. Temporary build/check artifacts belong in `_temp/`, and require product-manager confirmation before cleanup. Links use repository-root-relative paths.
- New records must use the same unique ID in the relevant language pair. Copy no unrelated historical prose into a new task. The Chinese files remain the full historical record; English history is migrated by needed ID.
- Source: `Docs/AI/PROJECT_CONTEXT.md` `[DOCS-ROUTES]`; precedence follows the current user's instruction and `AGENTS.md`.

## [CONTEXT][协作][QWEN-MEM-20261023] Local lightweight delegation and bilingual memory

- Current work branch: `codex/qwen-bilingual-memory`, starting at `446ff741867f5579b532b5079ecf1ef84ef99743`. This is a documentation/configuration task; no game source, Unity resource, protocol or gateway implementation is in scope. Preserve all pre-existing user changes and do not merge main.
- Per the user's subsequent QWEN-LIGHT-20261023 instruction, Qwen uses the exact local Ollama model `qwen3:8b-light`, not a registered native Codex agent. Pass this full model name explicitly; do not silently substitute `qwen3:8b` or `qwen3:8b-cpu`. If unavailable, use the existing one-attempt Doubao/WorkBuddy fallback. Prefer it for bounded lookup, comments, small file edits and Chinese memory synchronization expected to finish in one attempt. Complex diagnosis and implementation retain the Doubao/WorkBuddy workflow.
- Use `http://127.0.0.1:11434/api/chat` with bounded input; the lightweight starting context is 4096 and `think=false`. The caller performs approved read/write actions and checks the actual files. Model text is not proof of tool execution. Do not execute arbitrary commands generated by the model.
- On connection failure, incomplete output or failed review, hand lookup to Doubao and edits/synchronization to WorkBuddy, or Doubao when appropriate. Do not repeatedly rework Qwen. Its attempt does not consume either external model's seven-round allowance. Confirm the prior execution stopped before another writer takes over.
- This migration keeps the complete Chinese historical files and creates compact English working editions. It does not claim that every historical heading has already been translated. Codex maintains English; a subagent synchronizes the Chinese task delta, followed by actual-file review.
- Qwen's role-description draft failed review: it confused model output with the caller's file-write responsibility. No Qwen file write occurred. Doubao's same-project local session `38448830092170754`, run `59464168910756610`, using the desktop-listed `豆包 2.1 Pro`, created `AIconfig/agent/qwen_executor.md`; Codex read and accepted that small deliverable.
- WorkBuddy Hub `hub-7d83b097-b15a-476e-bcaa-fe419f5ae9da` never claimed the task because its native client lacked the gateway port; cancellation returned `cancelled`, `holdsCapacity=false`. Connection stalls are not completed development rounds. Further native execution identity and final acceptance are recorded in the task changelog.
- Source/change manifest: `项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md`. Detailed temporary evidence: `_temp/qwen-bilingual-memory-20261023/`. No fixed token/fee saving is asserted.

## [CONTEXT][协作][QWEN-LIGHT-20261023] Exact lightweight model constraint

- The active lightweight model is exactly `qwen3:8b-light` from the local Ollama registry. Callers must pass this full model name and must not silently substitute `qwen3:8b` or `qwen3:8b-cpu`.
- A local tag check on 2026-10-23 returned `qwen3:8b-light`. Unavailability still triggers the existing single-attempt fallback to Doubao or WorkBuddy; it does not authorize repeated Qwen retries or an unrecorded model substitution.
- This is a current configuration constraint. Historical query evidence that tested `qwen3:8b` remains unchanged and is not proof of the active model.
- Chinese counterpart: `Docs/AI/PROJECT_CONTEXT.md` `[QWEN-LIGHT-20261023]`.

## [STRUCTURE][综合][PC-080] Repository locations

- Root: `D:/unitypro/LYFMMORGP`; Unity client: `LYF_MMO`; backend: `LYF_Server`. Recorded Unity version: `2023.2.20f1c1`; recorded MCP instance: `LYF_MMO@47ea91c229a904f3`. These recorded identifiers do not prove a currently connected editor.
- Source: `Docs/AI/PROJECT_CONTEXT.md` `[PC-080]`.

## [STRUCTURE][综合][PC-081] Client layout

- Main assembly: `LYF_MMO/Assets/GM_Game/GM_Game.asmdef`; startup/hot update: `LYF_MMO/Assets/Boot`; business scripts: `LYF_MMO/Assets/GM_Game/Scripts`; UI: its `UI` directory; infrastructure/network: `Common`; managers: `Manger`.
- Recorded dependencies: YooAsset, UniRx, DOTween, HybridCLR, Google.Protobuf and Network.dll. The recorded active scene was `Assets/GM_Game/Scenes/New Scene.unity`; confirm actual editor state when needed.
- Source: `Docs/AI/PROJECT_CONTEXT.md` `[PC-081]`.

## [STRUCTURE][综合][PC-082] Backend layout

- Solution: `LYF_Server/LYF_Server.sln`; services: CenterServer, LoginServer, GateServer and GameServer; shared libraries: Network and MMoRpgCommon. Protocol sources: `LYF_Server/ProtoBuf/proto`; generated protocol code belongs in Network.
- Recorded defaults: Center 10110, Login 10120, Game 10130, Gate 10140. Stack: .NET Framework 4.8, Google.Protobuf, TCP Socket, SqlSugar/MySQL, Redis and Luban. These recorded defaults are not current connection observations.
- Source: `Docs/AI/PROJECT_CONTEXT.md` `[PC-082]`.

## [STRUCTURE][综合][PC-083] Responsibility boundaries and execution flow

- Client paths below are relative to `LYF_MMO/`. `Assets/Boot` owns startup/resources/hot updates. `Assets/GM_Game/Scripts/Common` owns infrastructure, network and shared data; `Manger` owns client managers; `controller` owns UI controllers and protocol-response registration.
- `Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs` and `RoleEntity.cs` directly inherit `ROObject`. Local business components and scene-entity components use different containers; remote synchronization data is held on the scene entity. Do not recreate a same-named business root model.
- `Assets/GM_Game/Scripts/FunlModule/Role/SceneWorldSystem.cs` is the unique scene-entity manager. Local/remote/other entities share one `SceneEntityWorld`, queried and removed by entity ID. `Global.SceneWorld` provides process-level access; business modules use `SceneWorld.LocalRole`. Do not restore `ClientRole`, `RoleWorldSystem` or parallel entity worlds.
- Official windows/pages: `Assets/GM_Game/Scripts/UI/Main`; popup dialogs: `UI/Tips`; test-only entries: `UI/test`. Recorded resource areas include `Prefabs`, `Scenes`, `UISprites` and `artres`. Tests must not be attached to production scenes/prefabs. `Assets/GM_Game/Plugins/Net/Network.dll` must match generated backend protocols.
- Backend paths below are relative to `LYF_Server/`. `GameServer/manger/RoleManager.cs` owns online roles, role/session indexes, role protocols, world entry and nearby synchronization. `GameServer/Ctrl/Game_LoginCtrl.cs` handles login/role creation/start and Center results, not role runtime logic.
- `GameServer/Service` owns authoritative domain validation/mutation; `GameServer/Entity` holds runtime `OnlineRole`; `GameServer/Entity/Components` holds its component interfaces, containers and domains. Keep this distinct from `GameServer/SceneEntity`, organized into `Base`, `Entity`, `Component`, `Skill` and `World`. The 2026-10-21 reorganization recorded 5/15/24 base/entity/component files; existing namespaces were retained during that directory-only change, which is not permission to add new namespaces.
- Bag capacity is persisted separately for `KnapsackType.RolePackPlain`, `RolePackEquip`, `RolePackConsume` and `RolePackMaterial`. `RoleCurrtEquipPack` is the equipped slot container, excluded from expansion. Plain is an independent bag, not an aggregate of equipment/consumables/materials; client tabs read separate containers.
- `GameServer/manger/Command` owns cross-thread envelopes, the shared queue, module routing and fixed-tick registration. `GameServer/manger/ClientMessageManager.cs` receives Gate/Center callbacks and dispatches by source/protocol on the logic thread. `GameServer/MessageHandler` uses one business-protocol handler per file; shared-source protocols branch on `GameMessageHandlerContext.Source`. `GameMessageProtocolRegister` registers callbacks/handlers.
- Flow: network callback copies data into immutable `GameCommand` and enqueues it in `GameCommandDispatcher`; the GameServer main loop feeds the client-message queue; handlers run on the logic thread. A `CenterMessageRoute.ForwardToGate` reply is forwarded unchanged through the original Gate session before its handler; `Internal` remains local. Every accumulated 50ms, `GameGlobal` updates business managers in the recorded RO order. Managers do not inherit the message-module base or consume network queues. Responses use the command's transport link on the logic thread; network callbacks do not mutate gameplay state.
- CenterServer owns persistence/center protocols; GateServer owns client sessions/forwarding; LoginServer owns account login; Network owns transport/packets/connections, not game business; MMoRpgCommon owns shared foundational types/tools. Use the five bilingual memory roles in `[DOCS-ROUTES]`; keep full changes in `项目修改日志/`, analysis in `项目分析/`, intermediates in `_temp/` and role templates in `AIconfig/`.
- Source: `Docs/AI/PROJECT_CONTEXT.md` `[PC-083]`. These are recorded architectural facts, not a fresh whole-repository audit.

## [STRUCTURE][协作][PC-084] Global collaboration constraints

- User-facing replies and business-code comments remain Chinese; English memory prose is the explicit language exception. Modified business types/interfaces, methods/constructors, fields, properties, constants, events, parameters and locals need concrete Chinese comments covering semantics, units/range or lifetime.
- No new namespace declarations, `foreach`, ternary operators, untyped business payloads (`object`, `object[]`, `dynamic`, unconstrained generic payloads), or more than three Boolean terms in one combined expression. Existing multi-type files may remain; newly designed top-level types use one same-named file each. Apply constraints within the change scope, without unrelated rewrites.
- Codex owns requirements, scope, design, delegation, final verification, records and Git integration. Business implementation normally goes to the appropriate external developer. Qwen's lightweight exception and one-attempt fallback override older default lookup routing; after fallback, Doubao/WorkBuddy retain their existing per-problem seven-round policy. Waiting/status/connection failures are not delivery rounds; preserve actual task identity and counts across sessions.
- Preserve user edits. Use a `codex/` feature branch, never develop/push main or merge without explicit authorization. Stage only task files, use Chinese commits with verification and changelog references, and report actual push results. Do not force push.
- Implement only the requested scope. Reuse specified reference structure/interfaces/call order with minimal project adaptation; do not add speculative gates, retries, queues, generations or generic frameworks. UI follows View/WindowBase/WindowType/existing Ctrl; views/widgets display and interact, controllers own business/protocols.
- Keep test entry points/Mock logic in dedicated test scripts/components; never attach them to production scenes/prefabs. Verify changed behavior proportionately. Keep intermediate output in `_temp/`; cleanup needs product-manager confirmation. Record difficult real issues in `项目分析/项目难点.md` with evidence, tradeoffs and verification limits.
- Source: `Docs/AI/PROJECT_CONTEXT.md` `[PC-084]`, superseded where necessary by current `AGENTS.md` and the 2026-10-23 user instruction.
