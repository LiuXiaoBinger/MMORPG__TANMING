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


## [QUERY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 RO party interfaces, limits and actual network route

- Before this implementation, server party protocols/manager and dungeon instance allocation did not exist; the former single-member party and Moshenta load were client-local. Active presence/move/skill broadcasts use RoleManager online roles and IsNearbyRole (map plus 100m), not EntityWorld.BuildSnapshots. The initial world-partition proposal was withdrawn.
- RO ModuleMgr/TeamMgr.lua contains RequestCreateTeam:43, InviteJoinTeam:81, AcceptTeamInvatation:165, LeaveTeam:315, BegInTeamByTeamId:413 and SetCaptain:456. Keep the actual Invatation spelling. ModuleData/TeamData.lua:63 sets maxTeamNumber=5; the initial inference of 4 from UI columns was wrong.
- CSV DungeonsTable row 4060: NumbersLimit=3=5, SceneID=4060, LevelLimit=75=200, EnterType=3, TimeLimit=2=7200. ThemeDungeonTable theme7 -> StoryDungeonID=4060 / TeamTargetID=2007. This project retains the explicitly authorized single-member mode with max 5; do not re-lock it to RO min 3. Moshenta spawn=132.97_58.89_60.95; current city default=62.1493416_19.4139996_80.6689758.
- Network integration requires proto generation, NetDefine, Game dispatch, Gate upstream/downstream registration AND GateClientResponseForwarder notification parsing, plus matching Network.dll in server bins/client plugin. Adding GateTeamCtrl registration alone did not pass the whitelist. Reference root F:/UnityPro/ro/Ro_cs422nb is read-only. Full process: 项目修改日志/2026-10-30-服务器权威多人组队副本_CHANGELOG.md.


## [QUERY][副本组队][DUNGEON-MULTIPLAYER-20261030] 2026-10-30 Service link versus player KCP and gate notification contract

- Gate registration and GateClientResponseForwarder parsing are separate integration points. New team11210/invitation11211/entry11212/leave11213 now each parse their concrete protobuf and return Succeed; the default and malformed-payload rejection remain unchanged. Root normal-login verification proved11200->11210 reaches the UI after this repair.
- Network/Socket/com/Session.cs TrySendRealtime explicitly checks IsGameServerLink before player KCP. A nonnull upstream NetClient of ClientType.GameServer keeps TCP for the Game->Gate service link; a null-upstream player Session without a bound peer drops realtime packets. Modeling the former as Session(dict,null) produced the misleading test failure and wrong round10 assertion. Corrected real captures verify the actual RoleManager observer filter and target UnitySessionId.
- Server liveness evidence must distinguish listening ports from established Gate->Game connectivity and from older child processes that later exited. Codex finally started Game85224 and Gate102264 in their real bin/Debug directories, kept Root-started Center54056/Login100464, confirmed the upstream connection, then normal-login/create passed. No automatic reconnect feature was added. Full evidence and limitations: 项目修改日志/2026-10-30-服务器权威多人组队副本_CHANGELOG.md.


## [QUERY][场景][MOSHENTA-BUNDLE-LIGHTMAP-BINDING-20261031] 2026-10-31 Live packaged-scene lightmap bindings are present

- Continues DUNGEON-PLAYER-REGRESSION-20261031 after the user reported that native Editor loading displays Baked Lightmap while bundle loading does not. Read-only investigation by domestic WorkBuddy deepseek-v4.1-flash, session c8cc03df-f534-45e5-8b00-b9a10e6819d7, attempt run-1de7d949-d94f-4db3-a99d-c1944ae94b13; no implementation round added, prior development count remains6. Its initial sample was Scene_Login, so it did not prove the Moshenta state. The user then entered Moshenta; Codex performed the live acceptance probes.
- Actual runtime DefaultPackage is HostPlayMode/HostPlayModeImpl, version2026-10-31-997, with one matching streamed scene bundle and active sc_dungeon_moshenta_001. LightmapSettings has2 nonnull1024x1024 maps and1919 probes; the only active MEnvorimentSetting reports _applied=true and its default lightmap SO is present. All937 expected renderer references are live, all937 actual indices and UV scaleOffsets match their configured values, and all937 resolve a nonnull color texture. One renderer reports static batching; this alone does not prove correct rendered output.
- The exact screenshot mesh sc_dungeon_moshenta_ta_002_b currently has index0 and scaleOffset(0.2514144,0.2514144,0.2719581,0.6459816), matching the screenshot. It is a runtime scene object; its material has no AssetDatabase path. At sampling time, Editor selection was Role_MJS(Clone) in DontDestroyOnLoad, not this mesh; this is a point-in-time observation, not proof of the user's earlier selection.
- Corrections: OnEnable/ApplyLightmapData logs do not prove937 assignments succeeded; the new live comparison does. Scene YAML stripped prefab references are normal serialization and do not establish broken bundle references. Existing lightmaps can originate from Unity baking even when this imported RO scene applies them through an environment component rather than its LightingDataAsset. A serialized scene-data fix would still require updating its bundle; do not claim a DLL refresh alone updates scene data. Missing LIGHTMAP_ON in material.shaderKeywords does not establish stripped shader variants.
- Current997 binding loss is not reproduced. The originally reported absent Inspector section, previous package state, fully black/dark rendering and standalone Player behavior are not resolved by these probes. User clarification on the current visible symptom is pending. No code/resource/config change, rebake, compilation, scene switch or build was performed. Do not add path-based renderer fallback or shader settings without a demonstrated failure.
- Evidence: _temp/dungeon-player-regression-20261031/lightmap-binding/evidence/root-live-package-probe.txt, root-live-binding-comparison.txt, root-live-package-mode.txt; worker investigation.md is provisional and superseded where corrected by root-review.md and this entry. Full approved follow-up: 项目修改日志/2026-10-31-组队界面刷新与成员入场修复_CHANGELOG.md.
