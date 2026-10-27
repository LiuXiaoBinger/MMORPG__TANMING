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
