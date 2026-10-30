# Collaboration record directory

At the start of every task, search this directory by topic/ID, then read only the listed source line range.
Ranges are inclusive and include the section heading. Do not load all four source files.
At the end of every task, after approved record writes, refresh this directory once (even without new logs):
`python .agents/skills/task-log-policy/scripts/build_record_index.py`.
Use `--check` to verify freshness; generation reads local files without sending their bodies into agent context.
If a heading/range is stale, regenerate before reading. Historical status is not current status.

## Docs/AI/CURRENT_TASK.en.md

Source normalized SHA256: `5a8ad4751aa9b8eb970d28123dfe7c483db4d00d58b8ed28541ed9de1c2098bb`

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
| [CODE][FILES][HANDOFF][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Server-authoritative multiplayer party, not accepted | 97-102 |

## Docs/AI/TEST_REPORT.en.md

Source normalized SHA256: `a678628bc223491b9527c5b2edb49afa22e4aa061728aab20271227b8791828c`

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
| [VERIFY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Build/smoke passed, live create rejected after server restart | 103-108 |

## Docs/AI/DECISIONS.en.md

Source normalized SHA256: `9dc9b1322a13e41b1e73836a1dbce9429c435228660a07c4fcb333948e835262`

| Topic / record ID / title | Lines |
|---|---|
| [DECISION][任务][DS-012] Adopt a bounded first-phase task system, 2026-10-23 | 3-8 |
| [DECISION][协作][QWEN-MEM-20261023] Local light tasks and paired language editions | 9-17 |
| [DECISION][协作][QWEN-LIGHT-20261023] Exact model tag for lightweight dispatch | 18-22 |

## Docs/AI/QUERY_TASKS.en.md

Source normalized SHA256: `d40bbcd6dee35369aa01da09c180146cc424bdec538594b25f56a2d468ec754d`

| Topic / record ID / title | Lines |
|---|---|
| [INDEX][协作][QUERY-INDEX] Archive routing | 3-7 |
| [QUERY][场景][RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027] Why the copied pillar has no visible baked lightmap in edit mode | 8-20 |
| [QUERY][副本组队][DUNGEON-TEAM-20261029] RO map binding and real team UI sources | 21-28 |
| [QUERY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 RO task/team quick panel ownership | 29-35 |
| [QUERY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 RO party interfaces, limits and actual network route | 36-41 |

## Older records

Only if the topic/ID is absent above, search `项目修改日志/协作记录归档/2026-10-26/INDEX.md`.
Read only the matching snapshot line range; do not load the full archive.
