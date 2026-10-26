# Project working context — English agent edition

## [INDEX][协作][DOCS-READ] Bounded startup reading

- Follow `AGENTS.md`. At the start of every task, read or search `Docs/AI/RECORD_INDEX.md` first, locate related topics/IDs, and read only their listed source line ranges. Do not load all four task-record bodies or scan unrelated history. Read stable routing here only when needed.
- At the end of every task, after approved record writes and bilingual synchronization, run `python .agents/skills/task-log-policy/scripts/build_record_index.py` once to refresh the directory. Run it even when no task log is approved; unchanged sources leave identical directory content, without timestamps or empty records. Generation is local and returns a short result, not source bodies. If the directory is missing or a heading/range is stale, regenerate before reading. Only look up the archived index for a relevant older topic/ID; historical status is not current state. Directory maintenance does not authorize new task-log entries.
- After every task, apply `.agents/skills/task-log-policy/SKILL.md`: list proposed log paths, contents, reasons and unverified limits, then ask the developer whether to write them. Wait for explicit approval before updating formal task records; an existing explicit logging instruction remains valid without another question. Recommend no log for unchanged/repeated facts, but let the developer decide. Directly requested business/skill/rule edits proceed normally. Report written, awaiting approval, or declined accurately.
- Codex writes English facts; a subagent synchronizes/checks only changed Chinese sections and reports scope/evidence. Preserve unchanged historical bytes in the archive. Report actual skills and observable token coverage under AGENTS.

## [STRUCTURE][协作][DOCS-ROUTES] Responsibilities and recovery

| Need | Read only when relevant |
|---|---|
| Stable structure and routing | `Docs/AI/PROJECT_CONTEXT.en.md` selected topic |
| Current changes and handoff | `Docs/AI/CURRENT_TASK.en.md` selected task |
| Checks and unresolved limits | `Docs/AI/TEST_REPORT.en.md` same task |
| Durable implementation decision | `Docs/AI/DECISIONS.en.md` selected decision |
| New investigation/evidence | `Docs/AI/QUERY_TASKS.en.md` selected query |

- Chinese siblings serve developers. Full per-task manifests/Git receipts live once in `项目修改日志/`; analyses in `项目分析/`; raw tool outputs in `_temp/`. Do not copy bodies across these roles.
- Archive routing: `项目修改日志/协作记录归档/2026-10-26/INDEX.md` → source snapshot and line range; hashes: `manifest.json` in the same directory. Archived failures, unresolved boundaries and execution identities remain recoverable. No history was reverified by this cleanup.
- Client-local `LYF_MMO/Docs/AI` entries redirect here. Consult their archived 2026-10-24/25 UI/font evidence only for that topic; it includes later facts absent from the old root entries.

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
