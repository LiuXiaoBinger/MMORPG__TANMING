# Audios 英文目录、用途和触发时间

2026-10-11。资源根目录：`LYF_MMO/Assets/GM_Game/Audios`；角色声音位于`Characters/MJS`。
修改日志：项目修改日志/2026-10-11-AUDIO-ENGLISH-REFACTOR-CHANGELOG.md。

## 目录和命名

目录和音频名全部英文。Characters存角色声音，角色内Movement、Jump、Skills按用途分类；Skills再分Shared、E、R、SwordDrop、Legacy。文件名以前缀Movement_、Jump_、Skill_直接表示声音类别和动作阶段。Shared/Skills/Effects存原有技能特效音源；共享音源保留一份，各技能通过同一AudioClip引用使用。帧和时间由作者稿定义，不写进共享文件名，避免同一音源在Q和F中使用不同帧时产生误解。

```text
Audios/
├─ Characters/
│  └─ MJS/
│     ├─ Movement/        Movement_Footstep01.wav 等走跑脚步
│     ├─ Jump/            Jump_TakeoffWhoosh.wav、Jump_LandingFootstep.wav
│     └─ Skills/
│        ├─ Shared/       Skill_BasicAttack_Slash01.wav 等共享挥刀声
│        ├─ E/            Skill_E_Phase01_HeavySlash.wav 等分阶段声音
│        ├─ R/            Skill_R_Full.wav、Skill_R_Followup_HeavySlash.wav
│        ├─ SwordDrop/    Skill_SwordDrop_Charge.wav、Skill_SwordDrop_ReleaseImpact.wav
│        └─ Legacy/       Skill_E_LegacyFull.wav 等备用素材
└─ Shared/
   └─ Skills/
      └─ Effects/        Skill_04_ChargeFx.wav、Skill_04_ReleaseFx.wav、Skill_02_ChargeFx.wav
```

## 原名和新位置

下表路径相对于Audios。原RO样本名用于来源追溯；所有音频和原meta一起迁移，GUID、波形、导入参数保持。

| 原文件名 | 新相对路径 |
|---|---|
| player_male_footstep_base-001.wav | Characters/MJS/Movement/Movement_Footstep01.wav |
| player_male_footstep_base-002.wav | Characters/MJS/Movement/Movement_Footstep02.wav |
| player_male_footstep_base-003.wav | Characters/MJS/Movement/Movement_FootstepVariant03.wav |
| Whoosh_BigJump3.wav | Characters/MJS/Jump/Jump_TakeoffWhoosh.wav |
| player_male_footstep_base-004.wav | Characters/MJS/Jump/Jump_LandingFootstep.wav |
| sword_01.wav | Characters/MJS/Skills/Shared/Skill_BasicAttack_Slash01.wav |
| sword_02.wav | Characters/MJS/Skills/Shared/Skill_BasicAttackF_Slash02.wav |
| _attack_sword2.wav | Characters/MJS/Skills/Shared/Skill_Combo_FastSlash.wav |
| Swing_Big_b.wav | Characters/MJS/Skills/E/Skill_E_Phase01_HeavySlash.wav |
| Swing_Short_h.wav | Characters/MJS/Skills/E/Skill_E_Phase02_ShortSlash.wav |
| Player_Knight_ShiZiFenXun.wav | Characters/MJS/Skills/R/Skill_R_Full.wav |
| Swing_Big_c.wav | Characters/MJS/Skills/R/Skill_R_Followup_HeavySlash.wav |
| GhostChargedEnergyBlast1.wav | Characters/MJS/Skills/SwordDrop/Skill_SwordDrop_Charge.wav |
| ef_magnumbreak.wav | Characters/MJS/Skills/SwordDrop/Skill_SwordDrop_ReleaseImpact.wav |
| Player_Knight_JianDingDaJi.wav | Characters/MJS/Skills/Legacy/Skill_E_LegacyFull.wav |

## 技能声音触发时间

时间为本次技能动画从零开始的累计秒数，不是wav内部的响点或文件总时长。FPS是各作者稿的制作帧率；改时间应在技能编辑器的Audio事件中编辑，再通过绑定发布。

| 技能 / 表现行 | FPS | 事件帧 | 时间（秒） | 声音文件名 | 音量 |
|---|---:|---:|---:|---|---:|
| Q第一段 / 3001001 | 120 | F48 | 0.4 | Skill_BasicAttack_Slash01 | 0.8 |
| Q第二段 / 3001002 | 120 | F23 | 0.191667 | Skill_BasicAttackF_Slash02 | 0.7 |
| Q第三段首刀 / 3001003 | 120 | F23 | 0.191667 | Skill_BasicAttack_Slash01 | 0.8 |
| Q第三段后刀 / 3001003 | 120 | F54 | 0.45 | Skill_Combo_FastSlash | 0.2 |
| E第一段 / 3004001 | 30 | F9 | 0.3 | Skill_E_Phase01_HeavySlash | 0.65 |
| E第二段 / 3004001 | 30 | F36 | 1.2 | Skill_E_Phase02_ShortSlash | 0.3 |
| R整段 / 3005001 | 120 | F63 | 0.525 | Skill_R_Full | 0.35 |
| R后续挥刀 / 3005001 | 120 | F136 | 1.133333 | Skill_R_Followup_HeavySlash | 0.45 |
| F首刀 / 3006001 | 120 | F37 | 0.308333 | Skill_BasicAttackF_Slash02 | 0.65 |
| F第二刀 / 3006001 | 120 | F142 | 1.183333 | Skill_Combo_FastSlash | 0.25 |
| 1蓄力 / 3007001 | 30 | F4 | 0.133333 | Skill_SwordDrop_Charge | 0.35 |
| 1释放 / 3007001 | 30 | F9 | 0.3 | Skill_SwordDrop_ReleaseImpact | 0.65 |

F的第二声音事件与粒子事件独立，声音为F142，粒子仍为F84。文件名字描述素材用途，不表示必须在某特效帧播放。

## 移动和跳跃声音

六份普通动作稿都是120FPS，声音时间相对于各动作剪辑的本轮起点；走跑循环每轮按同样步点执行。普通动作没有可施放技能ID。

| 普通动作稿 | 声音帧与秒 | 依次使用的文件 | 音量 |
|---|---|---|---|
| Walk | F4 / 0.033333；F42 / 0.35 | Movement_Footstep02；Movement_Footstep01 | 0.65；0.65 |
| Run | F4 / 0.033333；F32 / 0.266667 | Movement_Footstep02；Movement_Footstep01 | 0.65；0.65 |
| BattleWalk | F14 / 0.116667；F62 / 0.516667 | Movement_Footstep02；Movement_Footstep01 | 0.65；0.65 |
| BattleRun | F9 / 0.075；F48 / 0.4 | Movement_Footstep02；Movement_Footstep01 | 0.65；0.65 |
| JumpStart | F0 / 0 | Jump_TakeoffWhoosh | 0.35 |
| JumpEnd | F0 / 0 | Jump_LandingFootstep | 0.8 |

Movement_FootstepVariant03和Skill_E_LegacyFull目前不在正式声音事件中播放，作为保留素材。旧E还供Setup识别上一版默认声音并迁移。

## 编辑与引用

在原技能编辑器Effect轨道的Audio事件选择相应用途的英文音频，再设置Start frame和Base Volume。作者与运行配置使用GUID强引用；迁移原meta后不需要重新绑定所有事件。制作脚本已同步新相对路径，不增加运行目录扫描或新的播放逻辑。

普通动作编辑后执行原`导出MJS走跑跳声音`入口；技能通过原绑定发布。整理只改变名字和目录；验收和未决情况见Docs/AI/TEST_REPORT.md，不将资源引用检查当作实际游戏听感测试。
## 三份已有特效音源

这些素材由原技能特效预制体AudioSource引用，以实际引用用途命名，不依据旧素材名猜测角色。预制体及原GUID保持。

| 原文件名 | 新Audios相对路径 | 实际引用 |
|---|---|---|
| DragonAttack.wav | Shared/Skills/Effects/Skill_04_ChargeFx.wav | Effect_Skill04_Juqi.prefab |
| Clip_Skill5.wav | Shared/Skills/Effects/Skill_04_ReleaseFx.wav | Effect_Skill04.prefab |
| Shield_buff.wav | Shared/Skills/Effects/Skill_02_ChargeFx.wav | Effect_Skill02_Juqi.prefab |

原文件名仅作历史来源追溯。原角色十五声音及三特效音源共十八份，所有原meta/GUID和波形保持，不改变作者/Runtime的事件时点、音量、粒子或根运动。旧中文目录与旧Skill/MJX路径已被本次英文结构替代。