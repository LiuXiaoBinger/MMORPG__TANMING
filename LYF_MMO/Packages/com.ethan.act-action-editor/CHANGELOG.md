# Changelog

## Unreleased

- 本游戏接入原 Audio 单帧单次事件与 Animation 位移窗口发布；强引用声音随正式表现收集，根位移保留角色重力和碰撞。提供显式 RO 声音制作入口及走跑/战斗走跑/起跳/落地六份普通动作稿，原技能表、粒子与已有位移段保留。编辑根运动预览从固定零帧重建水平位置，重复拖帧不会累积跑远；声音预览使用窗口专用隐藏声源及 Base Volume，结束时清理。

- 本项目按 RO 原数据补充模型变色、技能条、召唤、AI事件、Buff、震屏、镜头后处理、镜头特效及角速度旋转九类制作轨道，保留原 Y 全部轨道。新增类别支持原字段编辑、按秒投影到帧轴、拖动、复制粘贴、删除和原撤销/保存链；后处理保持单项，兼容 Unity 内联序列化的全零空占位。新增事件计入时间轴长度及制作校验；正式游戏执行与 Scene 预览仍需宿主接入，未支持内容阻止发布。字段与枚举选项均支持中文 / English。

- 本项目将编辑器源码集中到`Assets/GM_Game/Editor/Skill/ActionEditor`，保留原程序集与meta身份，并补充窗口和支持代码的中文说明。位移、碰撞、飞行物、音效、镜头、流程等现有功能保留；Runtime、配置及运行支持范围保持既有实现。帮助文档及版本报告从Runtime程序集定位嵌入包。

- 本项目嵌入版修复预制体预览继承历史场景坐标：副本放到原点；显式动画采样固定角色及 Animator 根变换，保留骨骼姿态、原朝向和缩放。原地预览不驱动 Motion 根位移；场景源和旧时间模式保留既有兼容行为。


- Recover timeline rows for event data missing from the saved layout without modifying assets. Populated tracks remain discoverable when collapsed; unassigned projectiles display Missing Prefab and their frame.

- Grouped Telegraph under Effect with shared overlap lanes, creation menu and focused visual/audio inspector pages.

- Added Action Flow nodes for Allow Exit, Branch and Complete, unified legacy flow display, shared request policy and playback completion boundaries.

- Added Hierarchy bone-path copying and direct binding from the selected bone, with actor-relative path validation.

- Reorganized collision authoring into Shape, Response and Feedback pages with shape-specific creation and focused feedback controls.
- Added persistent Scene volume display, position/bounds/rotation editing and Undo-safe bone sample edits.
- Aligned box height/rotation across Scene authoring and Demo collision queries; added contact signal integration.

- Refined the project overview and user guide structure.

- Fixed legacy read-only and empty action views overlapping the three-row toolbar. Asset selection now ends the current IMGUI pass and resets per-asset selection/scroll state before repainting.
- Added asset-mode repaint regression; verification keeps graphics enabled in batch EditMode tests so the window can actually initialize.

## [0.2.0-preview.2] - 2026-09-13

- Prepared the independent YCombatEditor source repository and versioned TGZ/source ZIP distribution.
- Added a Chinese roadmap, Demo-to-package maintenance contract and standalone build/verification tools.

- Added a maintained Chinese track/button user guide and a Help button in the action editor.
- Effect Add now offers Visual Effect or Audio. The selected subtype survives copy/paste, Undo and serialization, and only matching properties are shown, including in advanced views.
- Preserved legacy mixed effects with separate property tabs; added ACT163 validation for missing or mismatched typed effect references.
- Effect previews now play only the selected subtype, and Stop effect preview also stops preview audio.

- Added opt-in legacy empty FX placeholder compatibility for playback requests; strict authoring validation and explicit-timing validation remain unchanged.
- Added root-motion ownership control so character controllers can suppress unclaimed builtin animation movement without disabling authored root-motion capture.

## [0.2.0-preview.1] - Local candidate

- Added explicit timeline FPS, source-frame trimming, shared preview animation output, moving outgoing blends and backed-up opt-in legacy timing migration.
- Separated runtime content duration from the minimum editor viewport length; preserved legacy mode.
- Added All/Any/Not condition assets, cycle validation, opt-in per-condition diagnostics and an interaction playground sample.
- Added schema-2 action read/clone/edit operations with stable GUID/local-ID references, stale-digest rejection, dry-run diffs and atomic Undo commits.
- Fixed explicit-FPS clip trimming, preview isolation/cleanup and animation segment drag offsets.
- Added designer/Agent workflows, static CI and reproducible sample Player-build entrypoint. Publication remains gated by provenance and final acceptance.

## [0.1.0-preview.2] - Unreleased

- Added optional device-independent Jump.triggerCommandId with editor shortcuts and clipboard/Undo regression. Consumer adapters resolve IDs; legacy triggerKey remains serialized.

- Added an interaction-window timeline with Evade/Block/Parry presets, reusable condition assets, custom signals, priority/activation resolution and ACT140 validation.
- Added on-demand interaction queries and per-playback state cleanup; damage and committed effects remain consumer-owned.
- Added a per-key latest-intent input buffer utility and editor/runtime regression coverage.
- Restricted animation displacement/flags to their frame intervals and made horizontal override independent of segment list order.

- Replaced the long user tutorial with an offline HTML guide separating built-in capabilities from consumer responsibilities.
- Added package-root AGENTS.md, a machine-readable capability manifest and project integration/extension guidance.
- Added Editor automation for inventory, read-only config validation, non-overwriting action creation and actor preflight, with a JSON batch entrypoint.
- Added always-available help menus independent of importing Basic Playback.
- Kept controllers, input, damage rules and AI on the consumer side; no behavior-tree editor or full controller is bundled.
- Unity 2022 remains unverified; the declared Unity minimum is unchanged.

## [0.1.0-preview.1] - Unreleased

- Added Package Manager tarball export and isolated installation validation in maintainer tools.
- Fixed playback ending before an explicit exit frame beyond the animation extent.
- Guarded sample Play/Stop outside Play Mode, enabled strict handlers by default and exposed validation details.
- Connected a generated frame-15 attack event to the sample receiver for a complete timing example.
- Added a full Chinese installation, authoring, integration and import-acceptance tutorial.

- Withdrew earlier local candidates after owner confirmed plugin-derived presentation code.
- Isolated legacy implementations outside the package, preserving game script GUIDs.
- Added ActionClipAnimator, local ActionHitStop, dedicated-pivot ActionCameraShake and a root-motion boundary using Unity APIs.
- Public package no longer includes the legacy mixer, global time effects or URP chromatic effect implementation.

- MIT license selected by owner; provenance verification remains a release gate.
- Added safe, generated Basic Playback scene/animation sample without game art.
- Added Chinese quickstart, runtime guide, limitations and publication checklist.
- Included imported sample compilation in isolated regression validation.
- Earlier editor work includes serialized inspectors, native Undo gestures and backed-up restoration.

## [0.1.0] - 2026-09-02

- Added a Unity 6 Embedded/Git-ready package layout.
- Added ActionPlayer, portable runtime contracts, and shared validation.
- Added Unity-native editor transactions, settings, migration support, and validation UI.
- Preserved existing action asset and component script identities.
