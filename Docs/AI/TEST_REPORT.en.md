# Verification records — English agent edition

Default agent edition. Full Chinese developer history remains in `Docs/AI/TEST_REPORT.md`. Historical pass counts are not fresh checks; unported IDs use the translation route in `PROJECT_CONTEXT.en.md` `[DOCS-READ]`.

## [VERIFY][协作][QWEN-MEM-20261023] Documentation/configuration acceptance

- Scope: lightweight Qwen routing, one-attempt fallback, five English working editions, preservation of complete Chinese history, matching synchronized task IDs and current evidence. The task does not change game code/resources or gateway implementation.
- Executed so far: `AIconfig/config.toml` parsed successfully with Python/tomli; Qwen model, one-attempt limit, fallback providers and bilingual policy were asserted. Codex independently read Doubao's actual `AIconfig/agent/qwen_executor.md` and accepted its role/ownership boundaries.
- Qwen's role draft was rejected because it confused the caller's write responsibility and omitted distinct lookup/edit fallback routing. No repeated Qwen rework and no Qwen file write were claimed. WorkBuddy's unclaimed cancelled task and stalled Doubao transports are not completed development rounds.
- Codex independently ran `verify_memory.py`: exit 0, no errors; all 1256 original Chinese historical sections retained outside the explicitly extended routing sections, 18 required English sections present, matching tags/IDs, substantive English bodies and no duplicates. Python/tomli parsing plus model/fallback/context/role-file and five English-file reference assertions passed. This verifies structure/preservation; selected NPC/network facts and stable structure were separately compared to their original sections.
- The initial English working editions totaled 31230 bytes; final file sizes are recorded in the verification report. Of the old historical sections, 1242 are not translated yet and remain complete in Chinese for bounded, on-demand migration. This is an explicit scope boundary, not a claim of full historical equivalence or measured token savings.
- Chinese synchronization used the original Doubao local session `38448830092170754`, run `59469831083523330`; actual output was checked independently. Final staged-format/scope checks and push outcome are recorded in the changelog rather than repeated across all entries.
- Preservation baseline: five source snapshots in `_temp/qwen-bilingual-memory-20261023/baseline/`, totaling 1256 level-two headings. This count proves the historical preservation scope, not that 1256 entries have been translated. Full historical translation is intentionally on demand.
- No Unity/server build or runtime validation is required for these prose/policy changes; no game acceptance is inferred. Actual final check outputs and unresolved items: `项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md` and `_temp/qwen-bilingual-memory-20261023/verification.json`.
- Chinese counterpart: `Docs/AI/TEST_REPORT.md` `[QWEN-MEM-20261023]`.

## [VERIFY][NPC][协作][TR-005] Historical reference-binding validation, 2026-10-22

- Recorded sole business change: add `sceneEntity.SetEntityCtrl(mainRoleController)` plus Chinese explanation in `MainCityMgr.BindMainRoleCompatibilityController`; preserve SetROObject, navigation, OpenTalk and UI flow.
- Recorded client rebuild against the official Network.dll: exit 0, 0 errors/38 warnings, 557 input hashes unchanged. Warning provenance was not individually attributed.
- Pre-fix 24/0 assertions reproduced the failure (near/far arrival produced no dialog call and creation remained unfinished). Post-fix 26/0 checked bidirectional identity, one TalkWindow call for near/far arrival, same NPC identity, stopping, no repeat over five later frames and the original distance warning. Do not add 24+26 as feature pass counts.
- Thin Unity/container/position/UI substitutes did not check actual window visibility/rendering or globally exclude double UI triggers. Nine user-file hashes and meta bytes were preserved; user hot-update bytes were not replaced. WorkBuddy platform round 2 was accepted; real Unity/deployment verification remained pending.
- Source: `Docs/AI/TEST_REPORT.md` `[TR-005]`; evidence: `_temp/npc-dialog-20261022/arrival-followup/codex-validation`; manifest/Git: `项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md`.

## [VERIFY][网络][TR-011] Historical RO interface validation, 2026-10-22

- Recorded Network/Gate/Game/Center/Login and 289 client sources compiled with 0 errors. Recorded service warning counts: 14/14/28/14/16; client 350. No blanket claim attributes all warnings to old or new code.
- Codex checks: real Socket/KCP 122/0, world entry 8/0, client thin boundary 54/0. Client prerequisites used stubs and a KCP callback check invoked the real method by reflection; these are not Unity rendering or real UDP-downlink evidence. Developer counts 126/52/8 are separate and must not replace Codex's counts.
- The initial 53/1 test failure was an overly strong test expectation; only the test was corrected for old-generation invalidation on Disconnect. Business code did not gain a gate. Original logs were preserved.
- Official plugin and final Codex build hashes matched; nine user files, meta identity and 33 mechanical migrations were preserved. Socket shutdown/UDP 10054 cleanup warnings remained; neither long-run stability nor error-free runtime logs were established.
- Unverified: Unity dual clients, real model animation/MP, business reconnect, early entity arrival, end-to-end weak-network behavior and IL2CPP/HybridCLR. No user Unity/service lifecycle, scene save or hot-update byte replacement was performed.
- Source: `Docs/AI/TEST_REPORT.md` `[TR-011]`; evidence: `_temp/network-ro-interface-20261022/codex-validation/final-verification.json`; manifest/Git: `项目修改日志/2026-10-22-RO-NETWORK-INTERFACE-CHANGELOG.md`.
