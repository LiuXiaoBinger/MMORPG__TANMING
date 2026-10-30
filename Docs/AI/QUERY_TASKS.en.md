# Query index: append new durable evidence only

## [INDEX][协作][QUERY-INDEX] Archive routing

- `项目修改日志/协作记录归档/2026-10-26/INDEX.md` retains every original query heading/ID and exact snapshot line range. Search by topic/ID only; do not load query history at startup. Historical findings remain dated and unverified for current conditions.
- Apply `.agents/skills/task-log-policy/SKILL.md` before adding an entry. No new query record was created merely to document this cleanup; see CURRENT_TASK and the changelog.

## [QUERY][场景][RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027] Why the copied pillar has no visible baked lightmap in edit mode

- Read-only follow-up to RO-MOSHENTA-MIN-ENV-20261027. The selected pillar sc_dungeon_moshenta_building_002 (1), local position(72.987,79.1624,589.11035), prefab instance150537529/source prefab GUID e12c4a666ffc9f54dbb6f2ebc6fba78d, matches the screenshot.
- Source binding renderer117451335 and copied renderer150537531 both use index1 and scaleOffset approximately(0.04050754,0.04050754,0.31588426,0.7874021).
- Codex independently compared those YAML records and the live editor: scaleInLightmap0.1, receiveGI=Lightmaps, maps0, rendererIndex-1, environment _applied=False, cast=Off,receiveShadows=False,dirty=True. Thus the old bake binding is present in the component; it is not currently injected into edit-mode LightmapSettings. The editable baking settings and the Baked Lightmap result display are different.
- RO source art scene has cast/receiveShadows1/1 and a LightingDataAsset; source published scene and our copy have0/0 and no LightingDataAsset. The screenshot settings are consistent with the art scene, but the image alone does not prove its source path.
- The current component only consumes existing RO bake data; rebaking and capturing new mappings is a separate editor workflow, not implemented here. Explicit component context menus apply/restore existing data; do not recommend disabling a non-ExecuteAlways component as a verified edit-mode restore callback.
- No Apply/Save/Play/scene switch was performed in this follow-up because the scene already had unsaved user changes.
- Codex observed1Light, rejecting the worker's0Light claim; no claim that a persistent preview necessarily requires disk serialization, and no claim that unreferenced EXRs are historical leftovers.
- Actual requestModelId deepseek-v4.1-flash independently observed in native session5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0. Native transport disconnected after artifacts were written; core findings were accepted from real files plus independent checks, not a successful final RPC receipt.
- No business files changed. Evidence:_temp/ro-lightmap-inspector-query-codex-accept.json and -review.md; source report's rejected claims are superseded by acceptance notes. Full follow-up in the existing changelog.


## [QUERY][副本组队][DUNGEON-TEAM-20261029] RO map binding and real team UI sources

- Read-only RO evidence under F:/UnityPro/ro/Ro_cs422nb/config/Table/CSV/: ThemeDungeonTable.csv line 9, theme 7 -> story 4060 / normal 4061 / hard 4062 / TeamTargetID 2007; DungeonsTable.csv line 404 maps 4060 -> SceneID 4060; SceneTable.csv line 560 maps 4060 -> sc_dungeon_moshenta_001/sc_dungeon_moshenta_001, StartPos=132.97=58.89=60.95. Our loaded scene is LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/sc_dungeon_moshenta_001.unity.
- UI sources in clientproj/Assets/artres/Resources/UI/Prefabs/: QuickTeamPanel is the HUD, Team1 is the main frame, TeamMember is its content. ThemeDungeonTeam is an entry-confirmation window, not the team-creation UI. Sixteen copied ROTeam assets, their dependencies and exact source/target manifests are referenced by the full changelog.
- Actual Unity inspection corrected an earlier inference: builtin UGUI assembly GUID f70555f144d8491a825f0804e09c671c can resolve Image/Text even when GUIDToAssetPath is empty. Missing-script counts, not an Assets-only GUID scan, determine validity. MoonClient components were stripped; one old TeamInvite title component was actually missing and replaced. Source Team1 content is initially inactive, requiring explicit activation in the migrated wrapper.
- Research/failed evidence remains in _temp/dungeon-team-20261029/research.md and research-addendum.md; final facts and limits: 项目修改日志/2026-10-29-副本组队与RO界面迁移_CHANGELOG.md.


## [QUERY][副本组队][DUNGEON-TEAM-20261029] 2026-10-30 RO task/team quick panel ownership

- Read-only source F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/Scripts/Lua/UI/Ctrl/QuickPanelCtrl.lua lines 32-34 binds QuickTaskPanel/QuickTeamPanel to ToggleTask/ToggleTeam under the same QuickPanel content parent; line 44 selects task by default. It is a shared tabbed area, not two stacked panels.
- UI/Handler/QuickTeamPanelHandler.lua lines 35-37 calls TeamMgr.RequestCreateTeam directly from TeamCreate. The migrated local controller now follows that entry order; the separate group button still opens the formal team window.
- Source UI prefab: clientproj/Assets/artres/Resources/UI/Prefabs/QuickPanel.prefab; source hash and 26 removed MoonClient component count: _temp/dungeon-team-20261030/quick-panel-copy.json. Built-in UGUI child Buttons on IsOn/IsOff survive custom-script stripping and must not intercept the new parent tab handler. Real raycast checks, not GUID-only scans or direct Invoke, establish interaction.
