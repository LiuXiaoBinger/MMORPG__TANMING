# Collaboration record directory

At the start of every task, search this directory by topic/ID, then read only the listed source line range.
Ranges are inclusive and include the section heading. Do not load all four source files.
At the end of every task, after approved record writes, refresh this directory once (even without new logs):
`python .agents/skills/task-log-policy/scripts/build_record_index.py`.
Use `--check` to verify freshness; generation reads local files without sending their bodies into agent context.
If a heading/range is stale, regenerate before reading. Historical status is not current status.

## Docs/AI/CURRENT_TASK.en.md

Source normalized SHA256: `06309f6bfe467146f46c45715a7736829847cd45853cb4a0cb45e9406bab160c`

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
| [CODE][FILES][HANDOFF][技能][组合][SKILL-COMBINATION-20261103] RO-style combination authoring and E-to-2 skill | 196-202 |
| [CODE][FILES][HANDOFF][技能][组合][打断][SKILL-CANCEL-FEEL-20261103] Faster E-to-2 handoff and slide/jump cancellation | 203-209 |
| [CODE][FILES][HANDOFF][副本][实体][技能][RO-DUNGEON-RUNTIME-20261104] Moshenta runtime actors and hit reactions | 210-217 |
| [CODE][FILES][HANDOFF][副本][技能][索敌][RO-DUNGEON-FORWARD-TARGET-20261104] Forward attack targeting in Moshenta | 218-224 |
| [CODE][FILES][HANDOFF][副本][战斗][RO-DUNGEON-MONSTER-HP-20261104] Shared Moshenta basic-attack damage and death | 225-229 |
| [FILES][HANDOFF][战斗表现][跳字][RO-HUD-WORD-ART-20261104] RO jump-word art and offline illustrated map | 230-234 |
| [CODE][FILES][HANDOFF][魔神塔][跳字][MOSHENTA-NORMAL-HIT-WORD-20261104] Normal attack damage words above dungeon monsters | 235-240 |
| [CODE][FILES][HANDOFF][副本][动画][MOSHENTA-SHARED-ANIMATOR-20261104] Use the public RO monster Animator in Moshenta | 241-245 |
| [CODE][FILES][HANDOFF][副本][怪物AI][MOSHENTA-ORC-W18-AI-20261104] Wave 18 orc patrol, retaliation and melee | 246-251 |
| [CODE][FILES][HANDOFF][组队][MASTER-TEAM-20261104] Party ownership moved into MasterServer | 252-256 |
| [CODE][FILES][HANDOFF][聊天][RO-CHAT-DEV-20261104] World, current scene and party chat | 257-262 |
| [CODE][FILES][HANDOFF] [构建][Shader][资源] UNITY-FULL-BUILD-20261105 — Full resource build repair | 263-269 |
| [CODE][FILES][HANDOFF][组队][TEAM-ENTRY-CONFIRM-20261105] Teammate HUD bindings and RO entry confirmation | 270-276 |
| [CODE][FILES][HANDOFF][副本][场景编辑器][MOSHENTA-EVENT-FLOW-20261106] Event-driven Moshenta waves, Boss walls and portal interaction | 277-286 |
| [CODE][FILES][HANDOFF][副本][Luban][DUNGEON-TARGET-LUBAN-20261106] RO DungeonTargetTable and Moshenta current objectives | 287-295 |
| [CODE][FILES][HANDOFF][副本][Luban][DUNGEONS-CONFIG-PROGRESS-20261106] Config-driven dungeon objectives and original RO sections | 296-307 |
| [CODE][FILES][HANDOFF][场景编辑器][UI][SCENE-EDITOR-LOCATE-20261107] Readable creature waves and explicit trigger/map locations | 308-315 |
| [CODE][FILES][HANDOFF][场景编辑器][副本][SCENE-EDITOR-STAGE-FLOW-20261107] Player-oriented dungeon stage flow | 316-333 |
| [CODE][基因][GENE-RUNTIME-20261107] Gene table and shared runtime | 334-352 |
| [CODE][FILES][HANDOFF][场景编辑器][副本][STAGE-INLINE-EDIT-20261107] Direct wave and mechanism editing in the stage view | 353-364 |
| [CODE][FILES][装备][基因][EQUIP-GENE-20261107] Equipment genes and special skills | 365-373 |
| [CODE][FILES][HANDOFF][场景编辑器][副本][STAGE-BINDINGS-20261107] Explicit stage membership in scene configuration | 374-380 |
| [CODE][FILES][基因][GENE-MINDMAP-20261108] Gene browser mind map | 381-389 |
| [CODE][FILES][HANDOFF][基因][GENE-NODE-FACTORY-20261108] Concrete nodes call the actual role | 390-396 |
| [CODE][FILES][装备][EQUIP-MANUAL-BAG-20261109] Empty equipment slots and manual weapon grant (2026-11-09) | 397-403 |
| [CODE][FILES][装备][基因][EQUIP-UI-LAYOUT-20261109] Opaque equipment details and real-runtime fixes (2026-11-09) | 404-461 |
| [CODE][FILES][HANDOFF][副本][怪物AI][DUNGEON-BATTLE-CONFIG-20261109] Per-scene monster battle resources | 462-470 |
| [CODE][FILES][HANDOFF][装备][特技][基因][EQUIP-SPECIAL-SKILL-CATALOG-20261111] Equipment special-skill catalog, sockets and gene activation | 471-477 |
| [CODE][FILES][HANDOFF][装备][特技][基因][EQUIP-SPECIAL-SKILL-COMBAT-20261111] Equipment special-skill audit and combat integration | 478-484 |
| [CODE][FILES][HANDOFF][基因][GENE-COMPONENT-UNIFY-20261111] One server GeneComponent for roles and monsters | 485-490 |
| [CODE][副本][DUNGEON-DEATH-FEEDBACK-20261112] Player damage numbers, explicit revive and two-role outfits | 491-499 |
| [CODE][副本][怪物AI][DUNGEON-AI-HIT-STATE-20261112] Distinct runtime IDs and movable non-control hit feedback | 500-506 |
| [CODE][技能][BASIC-ATTACK-TARGET-FACING-20261112] Preserve basic-attack targets through attack movement | 507-512 |
| [CODE][技能][BASIC-ATTACK-MANUAL-CHASE-MOTION-20261112] Manual-lock pursuit and three-hit basic-attack movement | 513-519 |
| [CODE][技能][SKILL-TARGET-ACQUIRE-CHASE-20261112] Front-priority automatic acquisition, target retention and skill-range pursuit | 520-525 |
| [CODE][技能][TARGET-IDLE-FACING-REMOVE-20261112] Keep selected targets without continuous idle rotation | 526-532 |
| [CODE][副本][DUNGEON-DIFFICULTY-GENE-AI-20261112] Four spreadsheet-driven difficulties and per-monster Gene/AI | 533-539 |
| [CODE][副本][DUNGEON-SIX-TIERS-LAYERS-20261112] Six difficulty choices, table layers and reentry restriction | 540-546 |
| [CODE][FILES][角色][基因][背包][ROLE-ADVANCED-UI-20261112] Role attribute tabs and bag currencies (2026-11-12) | 547-552 |
| [CODE][FILES][角色][背包][ROLE-ATTRIBUTE-RENDER-FIX-20261112] Role attribute clipping and blue-and-white styling (2026-11-12) | 553-558 |
| [CODE][FILES][角色][界面][ROLE-SUMMARY-SPACING-20261112] Larger role controls and aligned close button (2026-11-12) | 559-564 |
| [CODE][FILES][角色][界面][ROLE-PANEL-FULL-ROWS-20261112] Larger role controls, lower glow and whole attribute rows (2026-11-12) | 565-570 |
| [CODE][FILES][HANDOFF][技能][魔剑士][MJS-BLUE-SKILLSET-20261113] Magic Swordsman Blade Storm and Blue Barrier | 571-576 |
| [CODE][FILES][界面][FUNCTION-MENU-LEFT-ALIGN-20261116] Top function menu moved left, fold arrow aligned with the quick-panel hide button (2026-11-16) | 577-583 |
| [CODE][FILES][界面][HP-MP-BAR-THICKEN-20261116] Bottom HP and MP bars widened and thickened (2026-11-16) | 584-591 |
| [CODE][副本][怪物AI][MOSHENTA-RO-CAST-FX-20261116] Original RO monster spells and result effects | 592-597 |
| [CODE][副本][怪物AI][MOSHENTA-ELING-HIT-FX-20261117] Caster effects for normal attacks and Bite | 598-603 |
| [CODE][FILES][HANDOFF][技能][魔剑士][MJS-BARRIER-OPENER-20261117] Blue Barrier uses the key 1 opening animation | 604-609 |
| [CODE][FILES][HANDOFF][移动][输入][MOVE-STOP-NO-DELAY-20261117] Stop movement immediately on direction release | 610-614 |
| [CODE][技能][魔剑士][MJS-E3-LUNGE-FACING-20261117] Qingfeng lunge stop and Blade Storm facing | 615-620 |
| [CODE][FILES][HANDOFF][手感][移动][技能][ROLE-FEEL-MOVECAST-20261117] Immediate steering and moving basic attack | 621-625 |
| [CODE][技能][Luban][SKILL-STOP-AT-ENEMY-20261117] Per-skill stop-at-enemy switch | 626-631 |
| [CODE][FILES][武学][背包][货币][WEAPON-UI-JIANGHU-20261117] Martial arts layout and currency icons | 632-637 |
| [CODE][FILES][技能][客户端配置][CLIENT-CONFIG-RELOCATION-20261117] Normal attack movement disabled and client config relocated | 638-643 |
| [CODE][FILES][界面][设置][SETTINGS-PANEL-WYCLX-20261117] Rebuilt the settings window from the reference video (2026-11-17) | 644-648 |
| [CODE][FILES][HANDOFF][资源][构建][UNITY-ASSET-ORGANIZE-20261117] Archived unused Unity resources and reorganized active art | 649-654 |
| [CODE][FILES][HANDOFF][资源][构建][GM-GAME-RESOURCE-LAYOUT-20261117] Consolidated active resources under GM_Game | 655-660 |
| [CODE][FILES][HANDOFF][资源][构建][UNITY-UNUSED-ARCHIVE-20261117] Consolidated unused Unity game assets | 661-666 |
| [CODE][FILES][HANDOFF][资源][构建][GM-GAME-UNUSED-FOLLOWUP-20261117] Reviewed unused resources and exposed the archive in Unity | 667-672 |
| [CODE][FILES][HANDOFF][资源][构建][LYF-GAME-CLIENT-RENAME-20261117] Renamed the Unity game root and assembly to LYF_Game | 673-678 |
| [CODE][FILES][HANDOFF][资源][选角][CREATE-ROLE-SCENE-REPAIR-20261117] Restored missing character selection scene assets | 679-683 |
| [CODE][FILES][HANDOFF][登录][设置][副本][LOGIN-SETTINGS-DUNGEON-REPAIR-20261117] Reimported UI assets and corrected skill animation locations | 684-688 |
| [CODE][FILES][HANDOFF][副本][怪物实体][MOSHENTA-MONSTER-ENTITY-20261118] Moshenta monsters use their entity components | 689-693 |
| [CODE][FILES][HANDOFF][副本][NPC实体][RO-NPC-ENTITY-CLEANUP-20261118] Unified NPC models, dialogue and dungeon actions | 694-698 |
| [CODE][FILES][HANDOFF][副本][怪物预制体][MOSHENTA-SOLDIER-PREFAB-20261118] Moshenta soldier 5514 uses its original prefab | 699-703 |
| [CODE][FILES][HANDOFF][聊天][YIMENG-CHAT-20261118] Restore the channel management panel | 704-719 |
| [CODE][FILES][HANDOFF][副本][怪物预制体][MOSHENTA-ORIGINAL-PREFABS-20261118] Moshenta monsters, bosses and NPCs reuse their source prefabs | 720-724 |
| [FILES][HANDOFF][技能][音效][跳跃][COURSE-SFX-HIT-20261118] Course audio for MJS skills and jumps | 725-732 |
| [FILES][HANDOFF][界面][技能][COMBAT-HUD-20261118] Bottom combat HUD layout and styling | 733-739 |
| [CODE][FILES][HANDOFF][魔剑士][跳跃][MJS-JUMP-STAGES-20261118] Magic Swordsman stage 2 and 3 jump animations | 740-747 |
| [CODE][FILES][战斗][角色][怪物][COMBAT-RESOURCES-20261118] Four base combat resources for roles and monsters (2026-11-18) | 748-753 |
| [CODE][FILES][HANDOFF][HUD][竞技][ROLE-FOOTER-ARENA-20261119] Role status footer and Arena Settings | 754-762 |
| [CODE][FILES][HANDOFF][技能][魔剑士][龙卷斩][MJS-TORNADO-SLASH-20261119] Moving Tornado Slash and local game rollout | 763-772 |
| [CODE][FILES][HANDOFF][设置][音频][SETTINGS-AUDIO-20261119] Settings music and game sound volume (2026-11-19) | 773-778 |
| [CODE][FILES][HANDOFF][设置][音频][SFX-DEFAULT-VOLUME-20261119] Restore the SFX default and improve low volume (2026-11-19) | 779-783 |
| [CODE][FILES][HANDOFF][编辑器][菜单][UNITY-EDITOR-MENUS-20261119] Consolidate editor tools and retire one-time commands (2026-11-19) | 784-789 |
| [CODE][FILES][HANDOFF][技能][魔剑士][护盾][MJS-BARRIER-HUD-20261119] Blue Barrier overhead shield meter | 790-795 |
| [CODE][资源][RO-CLIENT-GC-20261119] Client GC at transition and low-memory boundaries | 796-801 |
| [CODE][FILES][HANDOFF][界面][MAIN-UI-LAYERS-20261119] Fixed main-city UI layers and sibling order | 802-807 |
| [CODE][FILES][HANDOFF][网络][HUD][RO-GATE-RTT-20261119] Gameplay gateway TCP RTT in the existing footer | 808-813 |
| [CODE][FILES][网络][移动][MOVE-POSITION-CORRECTION-20261120] Position report correction after 1.2 seconds | 814-817 |
| [CODE][FILES][HANDOFF][技能][基因][MJS-SHIELD-TORNADO-GROWTH-20261120] Blue Barrier and Tornado Slash growth through level 170 | 818-824 |
| [CODE][FILES][技能][WEAPON-SKILL-GROWTH-20261120] Eight active weapon skills share the level-170 upgrade flow | 825-830 |
| [CODE][FILES][技能][基因][ELEMENTAL-DAMAGE-20261120] Five-element damage and Tornado Slash wind growth | 831-837 |
| [CODE][FILES][武学][秘笈][MARTIAL-MANUAL-20261123] Martial manuals in the martial arts window | 838-841 |
| [CODE][FILES][技能][音效][振屏][MJS-SKILL-FEEL-20261123] Magic Swordsman cast audio and camera accents | 842-845 |
| [CODE][FILES][HANDOFF][装备][EQUIPMENT-WORKSHOP-20261123] Equipment detail opens the five-page workshop | 846-852 |
| [CODE][FILES][网络][移动][导航][AOI-NAV-CORRECTION-20261123] Reciprocal player visibility and navigation after owner correction | 853-858 |
| [CODE][FILES][武学][秘笈][基因][MANUAL-GENES-20261123] Catalogue, costs and passive genes | 859-864 |
| [CODE][FILES][战斗][基因][ATTACK-RANGE-ROLL-20261123] Sample each attack from the active internal or external range | 865-868 |
| [CODE][FILES][HANDOFF][技能][护盾][MJS-BARRIER-EXPIRY-20261123] Azure Barrier expiry burst and damage break | 869-872 |
| [CODE][角色][MJS-IDLE-BACKSWORD-20261123] Noncombat idle and back-mounted main sword | 873-880 |
| [CODE][FILES][HANDOFF][镜头][设置][PC-WORLD-CAMERA-20261123] Desktop world camera settings (2026-11-23) | 881-887 |
| [CODE][FILES][HANDOFF][技能][护盾][HUD][MJS-BARRIER-REFERENCE-20261123] Reference-based Azure Barrier light and four-line nameplate | 888-891 |
| [CODE][FILES][HANDOFF][装备][EQUIPMENT-GEM-SOCKET-20261123] Equipment socketing, inventory and persistence | 892-897 |
| [CODE][FILES][HANDOFF][武学][秘笈][MANUAL-DETAIL-ONLY-20261123] Show only manuals with recorded detail | 898-902 |
| [CODE][FILES][HANDOFF][装备][GEM-PICKER-COMPOSE-20261123] Socket-specific gem picker, synthesis and merchant | 903-908 |
| [CODE][FILES][HANDOFF][武学][秘笈][MANUAL-ITEM-RANK-20261123] Manual items, fragment synthesis and rank prices | 909-912 |
| [CODE][FILES][NPC][商店][货币][NPC-SHOP-UI-20261124] NPC merchant shelf and category views | 913-919 |
| [CODE][FILES][NPC][镜头][对话][NPC-DIALOGUE-FOCUS-20261124] Click-to-focus NPC dialogue and configurable services | 920-928 |
| [CODE][UI][组队][RO-TEAM-APPLY-REDSIGN-20261124] Show pending applications on the three RO team controls | 929-933 |
| [FILES][场景][SKYCITY-ELYSIUM-20261125] Sky City main-city environment | 934-958 |
| [CODE][网络][分线][REALM-GS-LINES-20261125] Multiple GameServers per logical realm | 959-962 |
| [CODE][区服][分线][REALM-DEPLOY-TWO-MAPS-20261125] File-based realm deployment and live login directory | 963-969 |
| [CODE][场景][分线][SKY-CITY-LINES-20261125] Sky City and Main City map selection | 970-975 |
| [CODE][UI][地图][RO-CITY-MAPS-20261125] RO minimap, region maps and world map | 976-983 |
| [CODE][交易][STALL-SELLER-20261125] Seller quote and withdrawal | 984-988 |
| [CODE][场景][ARK-EUROPEAN-STREETS-20261125] Monument courts and European streets in eight Lost Ark scenes | 989-993 |
| [CODE][商城][PAVILION-STOCK-20261125] Treasure Pavilion shelves | 994-999 |
| [CODE][UI][交易][TRADE-CATALOG-RELOAD-20261125] Restore the player market catalog after Unity reload | 1000-1003 |
| [CODE][网络][分线][传送点][REALM-RO-ENTRY-PORTALS-20261126] RO entry routing for the two public cities | 1004-1008 |
| [CODE][场景][传送点][烘焙][SKY-PUBLIC-INTERIORS-20261126] Sky City chapel and restaurant interiors | 1009-1013 |
| [CODE][交易][商会][装备][GEM-CHAMBER-MARKET-20261126] Chamber gems, player listings and Magic Swordsman upgrade patterns | 1014-1022 |
| [CODE][场景][NPC][网络][SKY-TOWNSFOLK-PATROL-20261126] Sky City professions and public-line guard patrols | 1023-1027 |
| [FILES][场景][主城][MAIN-CITY-EAST-WARDS-20261126] Public map 0 eastward city expansion | 1028-1032 |
| [CODE][FILES][场景][NPC][传送点][TWIN-CITY-BUILDING-NPCS-COACH-20261126] Residents for both cities and the working carriage launcher | 1033-1037 |
| [FILES][场景][碰撞][SKY-CITY-SURFACE-PHYSICS-20261126] Map 1 bridge seams and street collision | 1038-1042 |
| [CODE][FILES][HANDOFF][技能][界面][MJS-SKILL-DARK-ICONS-20261126] Dark fantasy art for every configured skill | 1043-1046 |
| [CODE][FILES][HANDOFF][副本][怪物AI][DUNGEON-ATTACK-IMPACT-20261126] Interrupting an ordinary monster attack before release | 1047-1050 |
| [CODE][FILES][装备][特技][EQUIP-ACTIVE-SKILL-RANK-20261126] Rank by worn items with the same active family | 1051-1054 |
| [CODE][FILES][邮箱][MAIL-RO-20261126] RO role inbox and Unity integration | 1055-1060 |
| [CODE][FILES][HANDOFF][任务][导航][WORLD-QUEST-NAV-20261126] Playable role quests and dungeon objective navigation | 1061-1064 |
| [CODE][FILES][NPC][对话][UI][NPC-NEARBY-TALK-LIST-20261126] Choose each nearby speaker by name | 1065-1069 |
| [CODE][FILES][HANDOFF][GM][RO-GM-20261126] In-game GM commands from the RO chat button | 1070-1074 |
| [CODE][FILES][邮箱][MAIL-GIFT-20261126] RO friend gift mail from the bag | 1075-1079 |
| [CODE][FILES][NPC][场景][网络][NPC-RESIDENT-MOTION-20261127] Resident idle, contact reactions and local walking routes | 1080-1086 |
| [CODE][FILES][装备][交易][GOLD-EQUIPMENT-ITEMS-20261127] Magic Swordsman silver knight gold equipment and materials | 1087-1091 |
| [CODE][FILES][HANDOFF][好友聊天][SOCIAL-CHAT-20261127] RO friend conversations and persistent friend groups | 1092-1097 |
| [CODE][FILES][HANDOFF][副本][导航][MOSHENTA-CLIENT-FULL-20261127] Playable Moshenta stairs and boss conversation | 1098-1101 |
| [CODE][FILES][HANDOFF][武学][秘笈][MJS-PROFESSION-MANUALS-20261127] Magic Swordsman profession manuals | 1102-1105 |
| [CODE][FILES][装备][交易][MJS-UPGRADE-MATERIAL-NAMES-20261127] RO names for workshop materials and Magic Swordsman upgrade patterns | 1106-1109 |
| [CODE][FILES][HANDOFF][界面][设置][HOTKEY-SETTINGS-20261127] Hotkey settings and navigation icons | 1110-1113 |
| [CODE][FILES][HANDOFF][界面][FUNCTION-MENU-RO-ANIMATION-20261127] Main HUD function menu icons and fold animation | 1114-1117 |
| [CODE][FILES][好友][邮箱][SOCIAL-ADD-MAIL-UI-20261127] Visible-player friend discovery and two-column mailbox | 1118-1122 |
| [CODE][副本][分线][DUNGEON-GS-CREATE-FIRST-20261127] Create a party dungeon before admitting its members | 1123-1126 |

## Docs/AI/TEST_REPORT.en.md

Source normalized SHA256: `edc63298ff04d22410d61597b86f29889a6473be5e15be10292aeb4c67ea1c7f`

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
| [VERIFY][技能][组合][SKILL-COMBINATION-20261103] Combination editor and runtime acceptance | 213-220 |
| [VERIFY][技能][组合][打断][SKILL-CANCEL-FEEL-20261103] Handoff and cancellation regression acceptance | 221-227 |
| [VERIFY][副本][实体][技能][RO-DUNGEON-RUNTIME-20261104] Runtime spawning, reactions and cleanup | 228-237 |
| [VERIFY][副本][技能][索敌][RO-DUNGEON-FORWARD-TARGET-20261104] Front selection, manual targets and story hit exclusion | 238-244 |
| [VERIFY][副本][战斗][RO-DUNGEON-MONSTER-HP-20261104] Instance isolation, lethal hits and Unity playback | 245-250 |
| [VERIFY][战斗表现][跳字][RO-HUD-WORD-ART-20261104] Atlas references, Unity import and offline browser checks | 251-255 |
| [VERIFY][魔神塔][跳字][MOSHENTA-NORMAL-HIT-WORD-20261104] Server damage fan-out and rendered normal words | 256-261 |
| [VERIFY][副本][动画][MOSHENTA-SHARED-ANIMATOR-20261104] Verify shared monster animation playback | 262-266 |
| [VERIFY][副本][怪物AI][MOSHENTA-ORC-W18-AI-20261104] Wave 18 server, damage and Unity playback checks | 267-271 |
| [VERIFY][组队][MASTER-TEAM-20261104] Independent MS and GS regression | 272-276 |
| [VERIFY][聊天][RO-CHAT-DEV-20261104] TCP routing and voice playback | 277-282 |
| [VERIFY] [构建][Shader][资源] UNITY-FULL-BUILD-20261105 — ForceRebuild acceptance | 283-289 |
| [VERIFY][组队][TEAM-ENTRY-CONFIRM-20261105] Confirmation protocol, real roster and prefab checks | 290-296 |
| [VERIFY][副本][场景编辑器][MOSHENTA-EVENT-FLOW-20261106] Authoritative event flow and Unity runtime acceptance | 297-306 |
| [VERIFY][副本][Luban][DUNGEON-TARGET-LUBAN-20261106] Original table fidelity and current Section acceptance | 307-315 |
| [VERIFY][副本][Luban][DUNGEONS-CONFIG-PROGRESS-20261106] Northern forest objectives and Moshenta regression | 316-326 |
| [VERIFY][场景编辑器][UI][SCENE-EDITOR-LOCATE-20261107] Offline presentation and isolated commit validation | 327-334 |
| [VERIFY][场景编辑器][副本][SCENE-EDITOR-STAGE-FLOW-20261107] Stage associations, readable conditions and isolated commit | 335-352 |
| [VERIFY][基因][GENE-RUNTIME-20261107] Gene runtime acceptance | 353-370 |
| [VERIFY][场景编辑器][副本][STAGE-INLINE-EDIT-20261107] Inline authoring, serialization and dependency checks | 371-382 |
| [VERIFY][装备][基因][EQUIP-GENE-20261107] Equipment and Gene acceptance | 383-390 |
| [VERIFY][场景编辑器][副本][STAGE-BINDINGS-20261107] Persistent membership and shared-node acceptance | 391-400 |
| [VERIFY][基因][GENE-MINDMAP-20261108] Gene mind map acceptance | 401-408 |
| [VERIFY][基因][GENE-NODE-FACTORY-20261108] Direct role mutation and cleanup acceptance | 409-416 |
| [VERIFY][装备][EQUIP-MANUAL-BAG-20261109] Empty equipment UI and persisted weapons (2026-11-09) | 417-424 |
| [VERIFY][装备][基因][EQUIP-UI-LAYOUT-20261109] Layout and live main-city UI acceptance (2026-11-09) | 425-489 |
| [VERIFY][副本][怪物AI][DUNGEON-BATTLE-CONFIG-20261109] Configured battle loading and Moshenta regression | 490-498 |
| [VERIFY][装备][特技][基因][EQUIP-SPECIAL-SKILL-CATALOG-20261111] Catalog and equipment instance acceptance | 499-505 |
| [VERIFY][装备][特技][基因][EQUIP-SPECIAL-SKILL-COMBAT-20261111] Production combat entry acceptance | 506-512 |
| [VERIFY][基因][GENE-COMPONENT-UNIFY-20261111] Unified component and existing dungeon combat acceptance | 513-518 |
| [VERIFY][副本][DUNGEON-DEATH-FEEDBACK-20261112] Real server death/revive and equipment verification | 519-527 |
| [VERIFY][副本][怪物AI][DUNGEON-AI-HIT-STATE-20261112] Wrong-target reproduction and real Editor combat | 528-534 |
| [VERIFY][技能][BASIC-ATTACK-TARGET-FACING-20261112] Eight attacks reacquire facing after passing the target | 535-541 |
| [VERIFY][技能][BASIC-ATTACK-MANUAL-CHASE-MOTION-20261112] Real approach before basic attack and bounded movement checks | 542-549 |
| [VERIFY][技能][SKILL-TARGET-ACQUIRE-CHASE-20261112] Twenty geometry checks and real automatic pursuit of a moving monster | 550-556 |
| [VERIFY][技能][TARGET-IDLE-FACING-REMOVE-20261112] Bounded idle-facing removal checks | 557-563 |
| [VERIFY][副本][DUNGEON-DIFFICULTY-GENE-AI-20261112] Four difficulties, effective Gene attributes and Editor entry | 564-571 |
| [VERIFY][副本][DUNGEON-SIX-TIERS-LAYERS-20261112] Six tiers, layers and actual-role entry checks | 572-578 |
| [VERIFY][角色][基因][背包][ROLE-ADVANCED-UI-20261112] Advanced attributes, currency bindings and prefab checks (2026-11-12) | 579-585 |
| [VERIFY][角色][背包][ROLE-ATTRIBUTE-RENDER-FIX-20261112] Two-resolution Unity render checks (2026-11-12) | 586-591 |
| [VERIFY][角色][界面][ROLE-SUMMARY-SPACING-20261112] Enlarged role panel render and interaction checks (2026-11-12) | 592-596 |
| [VERIFY][角色][界面][ROLE-PANEL-FULL-ROWS-20261112] Whole attribute rows and role panel layout (2026-11-12) | 597-601 |
| [VERIFY][技能][魔剑士][MJS-BLUE-SKILLSET-20261113] Eight authoritative hits, ward absorption and Unity asset checks | 602-607 |
| [VERIFY][界面][FUNCTION-MENU-LEFT-ALIGN-20261116] Function menu and fold-arrow coordinate check (2026-11-16) | 608-615 |
| [VERIFY][界面][HP-MP-BAR-THICKEN-20261116] HP/MP bar resize checked in Play (2026-11-16) | 616-622 |
| [VERIFY][副本][怪物AI][MOSHENTA-RO-CAST-FX-20261116] Original cast, replicated target warning and Unity effects | 623-628 |
| [VERIFY][副本][怪物AI][MOSHENTA-ELING-HIT-FX-20261117] Caster anchoring checked in live first-floor combat | 629-634 |
| [VERIFY][技能][魔剑士][MJS-BARRIER-OPENER-20261117] Blue Barrier animation binding and pose verified | 635-640 |
| [VERIFY][移动][输入][MOVE-STOP-NO-DELAY-20261117] Movement release regression checks | 641-645 |
| [VERIFY][技能][魔剑士][MJS-E3-LUNGE-FACING-20261117] Real Magic Swordsman E and 3 combat checks | 646-651 |
| [VERIFY][手感][移动][技能][ROLE-FEEL-MOVECAST-20261117] Unity input probe and skill navigation checks | 652-656 |
| [VERIFY][技能][Luban][SKILL-STOP-AT-ENEMY-20261117] Binary table and Unity motion checks with both switch values | 657-662 |
| [VERIFY][武学][背包][货币][WEAPON-UI-JIANGHU-20261117] Render, raycast, and currency entry verification | 663-668 |
| [VERIFY][技能][客户端配置][CLIENT-CONFIG-RELOCATION-20261117] Editor import, runtime config reads and embedded resource paths | 669-673 |
| [VERIFY][界面][设置][SETTINGS-PANEL-WYCLX-20261117] Unity rendering and settings window interaction checks (2026-11-17) | 674-678 |
| [VERIFY][资源][构建][UNITY-ASSET-ORGANIZE-20261117] Dependency comparison and Windows64 bundle verification | 679-684 |
| [VERIFY][资源][构建][GM-GAME-RESOURCE-LAYOUT-20261117] GM_Game relocation and package checks | 685-690 |
| [VERIFY][资源][构建][UNITY-UNUSED-ARCHIVE-20261117] Reference, runtime address, and Windows64 package verification | 691-697 |
| [VERIFY][资源][构建][GM-GAME-UNUSED-FOLLOWUP-20261117] Verified reference reachability, archive visibility, and Windows64 bundles | 698-703 |
| [VERIFY][资源][构建][LYF-GAME-CLIENT-RENAME-20261117] Verified LYF_Game editor compile and Windows64 package | 704-709 |
| [VERIFY][资源][选角][构建][CREATE-ROLE-SCENE-REPAIR-20261117] Verified the restored character selection scene | 710-714 |
| [VERIFY][登录][设置][副本][LOGIN-SETTINGS-DUNGEON-REPAIR-20261117] Tested login, settings and the slide skill in the local game | 715-719 |
| [VERIFY][副本][怪物实体][MOSHENTA-MONSTER-ENTITY-20261118] Verified wave 18 orc attack and effects | 720-724 |
| [VERIFY][副本][NPC实体][RO-NPC-ENTITY-CLEANUP-20261118] Verified city NPC dialogue and Moshenta story effects | 725-730 |
| [VERIFY][副本][怪物预制体][MOSHENTA-SOLDIER-PREFAB-20261118] Verified original prefab and both soldier waves | 731-735 |
| [VERIFY][聊天][YIMENG-CHAT-20261118] Channel manager layout, apply and settings recovery | 736-751 |
| [VERIFY][副本][怪物预制体][MOSHENTA-ORIGINAL-PREFABS-20261118] Verified reused prefabs in live Moshenta and recorded combat | 752-756 |
| [VERIFY][技能][音效][跳跃][COURSE-SFX-HIT-20261118] Verified course clips and saved MJS audio bindings | 757-763 |
| [VERIFY][界面][技能][COMBAT-HUD-20261118] Unity rendering and existing combat HUD bindings | 764-770 |
| [VERIFY][魔剑士][跳跃][MJS-JUMP-STAGES-20261118] Compiled and replayed the three jump stages | 771-779 |
| [VERIFY][战斗][角色][怪物][COMBAT-RESOURCES-20261118] Combat resource compilation and isolated checks (2026-11-18) | 780-785 |
| [VERIFY][HUD][竞技][ROLE-FOOTER-ARENA-20261119] Role footer and Arena Settings verification | 786-794 |
| [VERIFY][技能][魔剑士][龙卷斩][MJS-TORNADO-SLASH-20261119] Tornado Slash login, cast, rendering and hit timing | 795-804 |
| [VERIFY][设置][音频][SETTINGS-AUDIO-20261119] Settings audio verification (2026-11-19) | 805-810 |
| [VERIFY][设置][音频][SFX-DEFAULT-VOLUME-20261119] SFX low-volume verification (2026-11-19) | 811-815 |
| [VERIFY][编辑器][菜单][UNITY-EDITOR-MENUS-20261119] Unity editor menu validation after cleanup (2026-11-19) | 816-821 |
| [VERIFY][技能][魔剑士][护盾][MJS-BARRIER-HUD-20261119] Authoritative shield snapshots and Unity overhead display | 822-827 |
| [VERIFY][资源][RO-CLIENT-GC-20261119] Client GC compilation, static and Editor verification | 828-833 |
| [VERIFY][界面][MAIN-UI-LAYERS-20261119] Main-city UI prefab, window toggle and Play input order | 834-839 |
| [VERIFY][网络][HUD][RO-GATE-RTT-20261119] Gateway echo, compiled client RTT and footer binding checks | 840-845 |
| [VERIFY][网络][移动][MOVE-POSITION-CORRECTION-20261120] Position report timeout and renewal | 846-849 |
| [VERIFY][技能][基因][MJS-SHIELD-TORNADO-GROWTH-20261120] Skill growth, dungeon effects and persisted copper | 850-855 |
| [VERIFY][技能][WEAPON-SKILL-GROWTH-20261120] Other weapon skills use higher-level numbers in real cast methods | 856-860 |
| [VERIFY][技能][基因][ELEMENTAL-DAMAGE-20261120] Wind genes change actual dungeon health loss | 861-866 |
| [VERIFY][武学][秘笈][MARTIAL-MANUAL-20261123] Formation, progression and persistence | 867-870 |
| [VERIFY][技能][音效][振屏][MJS-SKILL-FEEL-20261123] Measured audio and camera timing for three casts | 871-874 |
| [VERIFY][装备][EQUIPMENT-WORKSHOP-20261123] Workshop entry, layout and tab navigation | 875-880 |
| [VERIFY][网络][移动][导航][AOI-NAV-CORRECTION-20261123] Visibility boundary and corrected pursuit | 881-886 |
| [VERIFY][武学][秘笈][基因][MANUAL-GENES-20261123] Catalogue and equipped passive verification | 887-894 |
| [VERIFY][战斗][基因][ATTACK-RANGE-ROLL-20261123] Damage-entry range and forced endpoints | 895-898 |
| [VERIFY][技能][护盾][MJS-BARRIER-EXPIRY-20261123] Zero-damage launch and both shield endings | 899-902 |
| [VERIFY][角色][MJS-IDLE-BACKSWORD-20261123] Idle and weapon mount acceptance boundaries | 903-910 |
| [VERIFY][镜头][设置][PC-WORLD-CAMERA-20261123] Desktop camera UI, solved orbit and persistence (2026-11-23) | 911-917 |
| [VERIFY][技能][护盾][HUD][MJS-BARRIER-REFERENCE-20261123] Reference video, real avatar rendering and nameplate checks | 918-921 |
| [VERIFY][装备][EQUIPMENT-GEM-SOCKET-20261123] Gem filtering, pointer clicks and MySQL login restoration | 922-927 |
| [VERIFY][武学][秘笈][MANUAL-DETAIL-ONLY-20261123] Eighteen visible manuals and original ID alignment | 928-932 |
| [VERIFY][装备][GEM-PICKER-COMPOSE-20261123] Socket picker clicks, composition settlement and relogin | 933-938 |
| [VERIFY][武学][秘笈][MANUAL-ITEM-RANK-20261123] Manual items, fragment synthesis and rank prices | 939-942 |
| [VERIFY][NPC][商店][货币][NPC-SHOP-UI-20261124] Merchant layout and checkout verification | 943-949 |
| [VERIFY][NPC][镜头][对话][NPC-DIALOGUE-FOCUS-20261124] Scene click, dialogue portrait and real stock | 950-959 |
| [VERIFY][UI][组队][RO-TEAM-APPLY-REDSIGN-20261124] Team application badge verification | 960-964 |
| [VERIFY][场景][SKYCITY-ELYSIUM-20261125] Sky City scene, navigation and bundle | 965-989 |
| [VERIFY][网络][分线][REALM-GS-LINES-20261125] Realm deployment and public line transfer | 990-994 |
| [VERIFY][区服][分线][REALM-DEPLOY-TWO-MAPS-20261125] Single-realm two-map line acceptance | 995-1002 |
| [VERIFY][场景][分线][SKY-CITY-LINES-20261125] Two-city line presentation and single-realm routing | 1003-1008 |
| [VERIFY][UI][地图][RO-CITY-MAPS-20261125] Two-city RO map Play Mode and navigation | 1009-1016 |
| [VERIFY][交易][STALL-SELLER-20261125] Seller quote and withdrawal checks | 1017-1021 |
| [VERIFY][场景][ARK-EUROPEAN-STREETS-20261125] Eight scenes, statue approaches and front doors | 1022-1026 |
| [VERIFY][商城][PAVILION-STOCK-20261125] Empty shops and hidden chamber shelf | 1027-1031 |
| [VERIFY][UI][交易][TRADE-CATALOG-RELOAD-20261125] Player market catalog after Unity reload | 1032-1035 |
| [VERIFY][交易][STALL-REALM1-LIVE-20261126] One authenticated, partially filled stall order on running realm 1 | 1036-1042 |
| [VERIFY][网络][分线][传送点][REALM-RO-ENTRY-PORTALS-20261126] One-realm expansion and live portal travel | 1043-1047 |
| [VERIFY][场景][传送点][烘焙][SKY-PUBLIC-INTERIORS-20261126] One-realm interior travel, character scale and baked lighting | 1048-1052 |
| [VERIFY][交易][商会][装备][GEM-CHAMBER-MARKET-20261126] Gem settlement and stock-aware item introductions | 1053-1061 |
| [VERIFY][场景][NPC][网络][SKY-TOWNSFOLK-PATROL-20261126] Two Unity players and three real socket roles | 1062-1066 |
| [VERIFY][场景][主城][导航][MAIN-CITY-EAST-WARDS-20261126] East wards scale, access and navigation | 1067-1071 |
| [VERIFY][场景][NPC][传送点][TWIN-CITY-BUILDING-NPCS-COACH-20261126] Two-city residency, dialogue and carriage arrivals | 1072-1076 |
| [VERIFY][场景][碰撞][导航][SKY-CITY-SURFACE-PHYSICS-20261126] Role movement and moving-camera seam checks | 1077-1081 |
| [VERIFY][技能][界面][MJS-SKILL-DARK-ICONS-20261126] Skill atlas loading, crop and small-size rendering | 1082-1085 |
| [VERIFY][副本][怪物AI][DUNGEON-ATTACK-IMPACT-20261126] Hit frames, interruption and released projectiles | 1086-1089 |
| [VERIFY][装备][特技][EQUIP-ACTIVE-SKILL-RANK-20261126] Same-family activation on two items | 1090-1093 |
| [VERIFY][邮箱][MAIL-RO-20261126] Inbox, item claims and client build | 1094-1099 |
| [VERIFY][装备][特技][EQUIP-DIVINE-LIVE-20261126] Divine Power (external) on two weapons in Unity Play | 1100-1103 |
| [VERIFY][任务][导航][WORLD-QUEST-NAV-20261126] Role quest progression and route verification | 1104-1109 |
| [VERIFY][NPC][对话][UI][NPC-NEARBY-TALK-LIST-20261126] Simultaneous named choices and actual NPC destinations | 1110-1114 |
| [VERIFY][GM][RO-GM-20261126] GM authentication, real commands and persistence | 1115-1119 |
| [VERIFY][邮箱][MAIL-GIFT-20261126] Two-character gift delivery and claim | 1120-1124 |
| [VERIFY][NPC][场景][网络][NPC-RESIDENT-MOTION-20261127] Actual bones, physical contact and line-authoritative residents | 1125-1130 |
| [VERIFY][装备][交易][GOLD-EQUIPMENT-ITEMS-20261127] Gold item data, sprite references and eight-slot wear | 1131-1135 |
| [VERIFY][好友聊天][SOCIAL-CHAT-20261127] Private delivery, group persistence and Unity input | 1136-1141 |
| [VERIFY][副本][导航][MOSHENTA-CLIENT-FULL-20261127] Full graphical client clear of Moshenta 4060 | 1142-1145 |
| [VERIFY][武学][秘笈][MJS-PROFESSION-MANUALS-20261127] Magic Swordsman profession manuals | 1146-1149 |
| [VERIFY][装备][交易][MJS-UPGRADE-MATERIAL-NAMES-20261127] Renamed materials in workshop, inventory and market search | 1150-1153 |
| [VERIFY][数据库][LOCAL-DB-CLEANUP-20261127] Local mailbox and chat test database cleanup | 1154-1160 |
| [VERIFY][界面][设置][HOTKEY-SETTINGS-20261127] Hotkey settings and navigation icons | 1161-1164 |
| [VERIFY][界面][FUNCTION-MENU-RO-ANIMATION-20261127] Main HUD menu pointer and animation checks | 1165-1168 |
| [VERIFY][好友][邮箱][SOCIAL-ADD-MAIL-UI-20261127] Friend discovery and mailbox layout checks | 1169-1173 |
| [VERIFY][副本][分线][DUNGEON-GS-CREATE-FIRST-20261127] Verify creation, party transfer and return across GameServers | 1174-1177 |

## Docs/AI/DECISIONS.en.md

Source normalized SHA256: `9dc9b1322a13e41b1e73836a1dbce9429c435228660a07c4fcb333948e835262`

| Topic / record ID / title | Lines |
|---|---|
| [DECISION][任务][DS-012] Adopt a bounded first-phase task system, 2026-10-23 | 3-8 |
| [DECISION][协作][QWEN-MEM-20261023] Local light tasks and paired language editions | 9-17 |
| [DECISION][协作][QWEN-LIGHT-20261023] Exact model tag for lightweight dispatch | 18-22 |

## Docs/AI/QUERY_TASKS.en.md

Source normalized SHA256: `202d9f7423720d803f64536e8d4194911739627a8fb440508f1bdbbd868ad0d6`

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
| [QUERY][技能][吟唱][SKILL-FALSE-SINGING-20261102] Non-singing skills delayed by an extra server singing phase | 100-109 |
| [QUERY][技能][打断][组合][RO-SKILL-CANCEL-20261103] RO cancellation and combination handoff semantics | 110-117 |
| [QUERY][怪物AI][副本][LYF-MONSTER-AI-READY-20261104] Readiness of LYF wave 18 for server-driven monster AI | 118-125 |
| [QUERY][怪物AI][副本][MOSHENTA-WAVES-FLOW-20261106] All-wave AI, spawn conditions and dungeon objectives | 126-135 |
| [QUERY][场景编辑器][UI][SCENE-EDITOR-USABILITY-20261107] Missing flow navigation and editor/runtime mismatches | 136-143 |
| [QUERY][场景编辑器][副本][STAGE-CONFIG-EXPLAIN-20261107] Stage order, spawning and mechanism timing | 144-151 |
| [QUERY][受击][动画][NONCONTROL-HIT-FEEDBACK-20261108] Non-control damage feedback during locomotion | 152-160 |
| [QUERY][装备][基因][EQUIP-GENE-FLOW-20261108] Equipment attachment and removal flow | 161-168 |
| [QUERY][基因][属性][RO-CLIENT-BUFF-UPDATE-20261108] RO client effect ticks and attribute UI refresh | 169-177 |
| [QUERY][RO][区服][分线][交易][RO-REALM-SCENE-LINES-20261125] Login realms, static-map lines and stall scope | 178-187 |
| [QUERY][区服][分线][副本][DUNGEON-INSTANCE-HOST-20261127] Which GameServer hosts a party dungeon | 188-194 |

## Older records

Only if the topic/ID is absent above, search `项目修改日志/协作记录归档/2026-10-26/INDEX.md`.
Read only the matching snapshot line range; do not load the full archive.
