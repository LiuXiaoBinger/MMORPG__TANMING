# Collaboration record directory

At the start of every task, search this directory by topic/ID, then read only the listed source line range.
Ranges are inclusive and include the section heading. Do not load all four source files.
At the end of every task, after approved record writes, refresh this directory once (even without new logs):
`python .agents/skills/task-log-policy/scripts/build_record_index.py`.
Use `--check` to verify freshness; generation reads local files without sending their bodies into agent context.
If a heading/range is stale, regenerate before reading. Historical status is not current status.

## Docs/AI/CURRENT_TASK.en.md

Source normalized SHA256: `45ce3afc867f9fa662e7095d352acdc4d793d573103e847ad323c67b8a3760b3`

| Topic / record ID / title | Lines |
|---|---|
| [HANDOFF][协作][CONTEXT-CLEANUP-20261026] Current boundaries after archival | 3-10 |
| [FILES][角色][ROLE-FACE-MATERIAL-20261025] Character face/material "washed out" fix, 2026-10-25 | 11-17 |
| [FILES][角色][ROLE-FACE-LIGHT-20261025] Character face lighting fix, 2026-10-25 | 18-22 |
| [FILES][Task][TASK-TABLE-DESIGN-20261025-R2] Quest table system expanded to 13 tables (revision 2), 2026-10-25 | 23-32 |
| [FILES][Scene][RO-FIRST-DUNGEON-SCENE-20261026] RO dungeon scene copied into the project, 2026-10-26 | 33-43 |
| [CODE][FILES][HANDOFF][场景][RO-MOSHENTA-MIN-ENV-20261027] Fixed dusk environment on Moshenta | 44-55 |
| [CODE][FILES][HANDOFF][场景][RO-MOSHENTA-EDITOR-LIGHTMAP-APPLY-20261027] Automatically consume the existing RO bake in edit mode | 56-66 |
| [CODE][FILES][HANDOFF][副本组队][DUNGEON-TEAM-20261029] RO team UI and local party entry | 67-74 |
| [CODE][FILES][HANDOFF][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 real-input repair and shared quick panel | 75-82 |
| [CODE][FILES][HANDOFF][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 minimap lifetime, permanent player and camera follow-up | 83-91 |
| [CODE][FILES][HANDOFF][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 Team HUD red-line alignment | 92-96 |
| [CODE][FILES][HANDOFF][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Server-authoritative multiplayer party, not accepted | 97-104 |
| [CODE][FILES][HANDOFF][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Final scoped delivery after authorized WorkBuddy extension | 105-112 |
| [CODE][FILES][HANDOFF][副本组队][TEAM-NEARBY-LEAVE-20261031] 2026-10-31 Nearby invitations and city leave visibility | 113-120 |
| [CODE][FILES][HANDOFF][组队][DUNGEON-PLAYER-REGRESSION-20261031] 2026-10-31 Party refresh and member entry; lighting deferred | 121-129 |
| [CODE][FILES][HANDOFF][组队][DUNGEON-PLAYER-REGRESSION-20261031-R6] 2026-10-31 Invitation visibility follow-up after failed Player verification | 130-138 |
| [CODE][FILES][HANDOFF][场景][DUNGEON-PLAYER-REGRESSION-20261031-R7] 2026-10-31 Restore the RO runtime shader LOD entry | 139-146 |
| [CODE][FILES][HANDOFF][场景][DUNGEON-PLAYER-REGRESSION-20261031-R9-SVC] 2026-10-31 Explicit baked variant and fresh package ready for developer validation | 147-154 |
| [CODE][FILES][HANDOFF][相机][MOSHENTA-CAMERA-COLLISION-20261031] Moshenta camera obstacle avoidance | 155-162 |
| [CODE][FILES][HANDOFF][技能][事件][SERVER-ENTITY-EVENT-20261101] RO entity events and OnCall entry | 163-172 |
| [CODE][FILES][HANDOFF][技能][互斥][CLIENT-STATE-EXCLUSION-20261102] RO client casting exclusion gate | 173-180 |
| [CODE][FILES][HANDOFF][互斥][规范][STATE-INTERFACE-VIRTUAL-20261102] Virtual state base class | 181-187 |
| [CODE][FILES][HANDOFF][技能][吟唱][SKILL-ZERO-SINGING-FIX-20261102] Restore zero-singing skill entry | 188-195 |
| [CODE][FILES][HANDOFF][技能][组合][SKILL-COMBINATION-20261103] RO-style combination authoring and E-to-2 skill | 196-200 |

## Docs/AI/TEST_REPORT.en.md

Source normalized SHA256: `3f2806eed5477b0aba35bee3e5c8576055fba0a4502fce060980f23405010df3`

| Topic / record ID / title | Lines |
|---|---|
| [VERIFY][协作][CONTEXT-CLEANUP-20261026] Documentation cleanup acceptance | 3-9 |
| [VERIFY][角色][ROLE-FACE-MATERIAL-20261025] Character material washout fix verification | 10-16 |
| [VERIFY][角色][ROLE-FACE-LIGHT-20261025] Character face lighting verification | 17-23 |
| [VERIFY][Task][VERIFY-TASK-TABLE-20261025-R2] Generation and cross-reference checks for the 13 quest tables, 2026-10-25 | 24-33 |
| [VERIFY][Scene][RO-FIRST-DUNGEON-SCENE-20261026] RO dungeon scene copy verification | 34-45 |
| [VERIFY][场景][RO-MOSHENTA-MIN-ENV-20261027] Incremental environment acceptance | 46-57 |
| [VERIFY][场景][RO-MOSHENTA-EDITOR-LIGHTMAP-APPLY-20261027] Existing bake visible in the current Mesh Renderer | 58-69 |
| [VERIFY][副本组队][DUNGEON-TEAM-20261029] Team shells, local controller and map references | 70-77 |
| [VERIFY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 real logged-in pointer input | 78-86 |
| [VERIFY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 real scene entry and permanent object identity | 87-97 |
| [VERIFY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 Team HUD red-line alignment | 98-102 |
| [VERIFY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Build/smoke passed, live create rejected after server restart | 103-110 |
| [VERIFY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Final real scene flow and decoded multiplayer protocol checks | 111-120 |
| [VERIFY][副本组队][TEAM-NEARBY-LEAVE-20261031] 2026-10-31 Protocol regressions and actual candidate collector | 121-128 |
| [VERIFY][组队][DUNGEON-PLAYER-REGRESSION-20261031] 2026-10-31 Callback lifecycle regression and server-notification entry path | 129-136 |
| [VERIFY][组队][DUNGEON-PLAYER-REGRESSION-20261031-R6] 2026-10-31 Real prefab invitation replay and two-recipient wire checks | 137-146 |
| [VERIFY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R7] 2026-10-31 Source compile and live runtime LOD comparison | 147-154 |
| [VERIFY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R8-CPU] 2026-10-31 Unverified lightmap stripping candidate and compiler load mitigation | 155-162 |
| [VERIFY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R9-SVC] 2026-10-31 Fresh shader package and serialized SVC verified; visual acceptance pending | 163-170 |
| [VERIFY][相机][MOSHENTA-CAMERA-COLLISION-20261031] Scoped scene and live acceptance | 171-177 |
| [VERIFY][技能][事件][SERVER-ENTITY-EVENT-20261101] Server event and protocol acceptance | 178-188 |
| [VERIFY][技能][互斥][CLIENT-STATE-EXCLUSION-20261102] Client exclusion validation | 189-197 |
| [VERIFY][互斥][规范][STATE-INTERFACE-VIRTUAL-20261102] Virtual state contract validation | 198-204 |
| [VERIFY][技能][吟唱][SKILL-ZERO-SINGING-FIX-20261102] Zero-singing regression and loopback acceptance | 205-212 |
| [VERIFY][技能][组合][SKILL-COMBINATION-20261103] Combination editor and runtime acceptance | 213-218 |

## Docs/AI/DECISIONS.en.md

Source normalized SHA256: `9dc9b1322a13e41b1e73836a1dbce9429c435228660a07c4fcb333948e835262`

| Topic / record ID / title | Lines |
|---|---|
| [DECISION][任务][DS-012] Adopt a bounded first-phase task system, 2026-10-23 | 3-8 |
| [DECISION][协作][QWEN-MEM-20261023] Local light tasks and paired language editions | 9-17 |
| [DECISION][协作][QWEN-LIGHT-20261023] Exact model tag for lightweight dispatch | 18-22 |

## Docs/AI/QUERY_TASKS.en.md

Source normalized SHA256: `ec1b227257be1f3a13bbaf901968475cfcb5347b007c737c2be1a3c43c1f2378`

| Topic / record ID / title | Lines |
|---|---|
| [INDEX][协作][QUERY-INDEX] Archive routing | 3-7 |
| [QUERY][场景][RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027] Why the copied pillar has no visible baked lightmap in edit mode | 8-20 |
| [QUERY][副本组队][DUNGEON-TEAM-20261029] RO map binding and real team UI sources | 21-28 |
| [QUERY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 RO task/team quick panel ownership | 29-35 |
| [QUERY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 RO party interfaces, limits and actual network route | 36-43 |
| [QUERY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Service link versus player KCP and gate notification contract | 44-50 |
| [QUERY][场景][MOSHENTA-BUNDLE-LIGHTMAP-BINDING-20261031] 2026-10-31 Live packaged-scene lightmap bindings are present | 51-60 |
| [QUERY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R7] 2026-10-31 Missing RO runtime LOD selection, not missing renderer bindings | 61-68 |
| [QUERY][场景][DUNGEON-PLAYER-REGRESSION-20261031-R8-COLLECT] 2026-10-31 Collector misses the baked runtime condition; existing shader bundle unchanged | 69-78 |
| [QUERY][副本编辑器][RO-DUNGEON-EDITOR-20261102] RO dungeon editor entry points and data route, 2026-11-02 | 79-89 |
| [QUERY][技能][互斥][RO-CLIENT-CAST-CHECK-20261102] RO client/server casting checks | 90-99 |
| [QUERY][技能][吟唱][SKILL-FALSE-SINGING-20261102] Non-singing skills delayed by an extra server singing phase | 100-107 |

## Older records

Only if the topic/ID is absent above, search `项目修改日志/协作记录归档/2026-10-26/INDEX.md`.
Read only the matching snapshot line range; do not load the full archive.
