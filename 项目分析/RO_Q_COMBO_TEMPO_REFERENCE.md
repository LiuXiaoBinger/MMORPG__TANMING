# RO 普攻接段时间对照

日期：2026-09-27。用于本项目 Q/10010 三段普攻节奏调整；变更和验收另见 `项目修改日志/2026-09-27-RO-NORMAL-ATTACK-COMBO-CHANGELOG.md` 与 `Docs/AI/TEST_REPORT.md`。

## 源码和原始数据

- RO `clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:249-252`：`IsEnd()` 按 `SkillStateTime` 判定人物退出技能状态，不以完整动画长度判定。`MSkillComponent.cs:768-775` 在 `IsEnd()` 后 `Finish()`，原注释明确自动战斗也不再等待完整动画。
- RO `rogamelibs/entity/skill/art_skill.cpp:180-195`：`SkillStateTime` 到期触发 `StateTimeEnd`，`Time` 到期另行触发 `SkillTimeEnd`；`rogamelibs/entity/component/skill_component.cpp:557-567` 在状态到期后 `AutoEnd`。
- RO `config/Assets/Resources/SkillData/Player_Swordman/Player_Swordman_GongJi_1_0_1.txt`、`_2.txt`、`_3.txt` 的第 18-20 行：三段均为 `Time=0.9` 秒、`AnimationLength=0.6` 秒、`SkillStateTime=0.7` 秒。`Player_Knight/Player_Knight_GongJi_0_1.txt`、`_2_1.txt`、`_3_1.txt` 也配置 0.7 秒技能状态。

## 本项目最小对应

本项目 MJS 的 `Anim_MJS_Atk01/02/03` 长度分别约 2.167/1.967/2.5 秒，并非 RO 剑士原剪辑。沿用完整 `DurationMs` 记录资源长度；Q 的三段另配 `SkillStateTimeMs=700`，作为参照 RO 普攻的**本项目节奏参数**，不能声称该数值来自 MJS 动画。Q 原 `CastMs=2167` 和 `CooldownMs=500` 不应冒充状态时间，也不改动旧技能主表、MP、CD 或其他技能数值。

客户端在状态时间到期后允许新 Q 段进入 Skill，旧实例继续按施法 ID 等待服务器结束；新段接管 Skill 动画槽时不能由旧段的结束通知释放新剪辑。服务端的 Q 动作占用时间应与同段状态时间一致，避免客户端已可接段而服务器仍因完整动画长度拒绝。超过 700 毫秒的原剪辑尾部可能被下一段或 Idle 过渡截断，这是状态与动画分离的直接结果；Game View 应检查挥击关键姿态是否完整，如需调节，只调整 Q 的状态时间数据并同步两端，不修改完整剪辑长度或播放倍率。
