# RO 技能子类与本项目映射

## RO 子类职责

RO 客户端 `MoonClient/Skill/MSkill.cs` 将 UUID、施法状态、子弹列表、定时器和回收放在抽象父类；`MSkillFactory.cs` 按表现数据的 `TypeToken` 从对应具体类型的池借还。

| RO 类型 | 实际职责 | 本项目判定 |
| --- | --- | --- |
| `MArtSkill` | 按时间轴播放动画、特效、音效等演出；`AddBullet` 接收服务器弹体同步并交 `MBulletMgr` 创建弹体。飞行不是另一个 `MSkill` 子类。 | 现有五个技能均是普通表现；10070“无限剑决”的原动画已有巨剑粒子时间点和资源，归在 Art 表现。 |
| `MCombineSkill` | 按 `CombineSkillList` 的子技能 ID 顺序启动、更新、结束和回收子技能。 | 现有表无子技能序列；Q 的 Atk01/02/03 是同一技能 `UsingTimes` 按键轮换，不是组合技。 |
| `MChannellingSkill` | `MArtSkill` 的持续引导特化：在配置时间切循环动画，结束/打断时关闭引导条。 | 现有表无循环引导动画或引导条配置，无技能归此类型。 |

参考 RO 文件从项目根外的 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/` 读取：`MSkill.cs`、`MSkillFactory.cs`、`SkillLogic/MArtSkill.cs`、`MCombineSkill.cs`、`MChannellingSkill.cs`。本项目只融合有配置及资源支持的 Art 行为，不给现有技能臆造组合序列或引导状态。

## 10070 既有资源

`Luaban/MiniTemplate/Datas/SkillInfo数据.txt` 对 10070 描述为“无限剑决”，巨剑从天落下并伤害目标周围敌人；`LYF_MMO/Assets/GM_Game/Animas/Role/MJS/Common/ALL/Skill/Anim_MJS_Skill04.anim` 在约 0.1265、0.2969 秒有两次 `PlayEffect`，均引用 `LYF_MMO/Assets/GM_Game/Scripts/Data/Config/Jx_Skill04.asset`。该配置引用 `Effect_Skill04_Juqi` 和 `Effect_Skill04` 两个粒子 prefab；第二个 prefab 的节点包含 Sword、冲击等表现，但没有弹体运动、碰撞组件。

用户确认 1 号是特效，飞行物技能暂不开发。现阶段不新增 RO `MBulletMgr` 对应弹体链，也不把 `Effect_Skill04` 当投射物实体。

旧 `RoleAnimaBehaviour` 在第二次事件将特效放角色前方 10 单位，没有真正读取目标；当前四个正式角色预制体已按用户要求将该组件失活，组件仍保留。客户端 `MArtSkill` 读取剪辑事件和现成配置进行本地演出，保持共享配置只读。已有目标 UID 时可查场景目标；当前按键目标 UID 为零，沿用旧落点。这个接法恢复视觉，不等于 RO 服务器弹体系统。

10070 的事件时间轴独立持有 `MAnimationMgr.ClipInfo`，其路径与本次施法绑定，避免后续技能复用动画槽导致旧实例读错事件。存活实例移入旧技能列表后仍可消费晚到剪辑；结束、拒绝或回池时解除资源引用，不注册会补发事件的完成回调。已产生的粒子按根及子粒子的配置寿命销毁，正常技能结束不截断尾迹；拒绝后已有粒子也会继续有限尾迹。上述时间轴是接入本项目已有动画事件资源的适配，不是完整移植 RO 编辑器表现数据格式。

## 技能启动与统一动画接口

用户要求方法职责和调用顺序尽量与 RO 一致。本节修正此前“启动时尽早安装动画”的实现假设。RO 的源码顺序如下，路径基于 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/`：

1. `Components/Action/MActionComponent.cs` 的 `OnGetPermission` 标记状态机需要播放动画，并调用状态启动钩子。
2. `Skill/MSkillComponent.cs` 的 `OnStart` 进入施法；`Skill/MSkill.cs` 的 `Fire` 设置本次上下文，调用 `Execute`，由其保证 `Start` 每次施法执行一次。普通动画不从这条启动链调用。
3. `Skill/SkillLogic/MArtSkill.cs` 的 `Start` 注册表现事件。
4. `Components/State/MStateMachine.cs` 的 `LateUpdate` 调当前状态的统一 `PlayAnim` 接口。
5. `Skill/MSkillComponent.cs` 的 `PlayAnim` 只调用当前技能的 `TriggerAnim` 并返回状态名。
6. `Skill/MSkill.cs` 的 `TriggerAnim` 才覆盖 Skill 动画槽；经过时间超过 0.1 秒时按归一化进度 `Play`，否则 `CrossFade`，过渡参数为 0.1。

本项目对应整改限定在 `MSkill`、`MArtSkill` 与 `MSkillComponent`：移除 `Begin` 和 `TryPrepareAnimation`，沿用 RO 的上述接口职责，保留本项目已有的强类型施法参数与异步资源管理。`MArtSkill.Start` 只负责已有 Skill04 特效事件资源，普通动画触发回到父类。现有状态机在模型 Animator 尚未就绪时保留触发标记，模型就绪后由同一 `PlayAnim` 入口播放；技能逐帧更新不再自行重试安装动画。技能组件必须覆写 `PlayAnim`，否则通用动作基类会因没有通用 Skill 动画字段而回退 Idle。

本轮不引入未接入的战斗坐骑、性别动作替换或 `AnimList` 配置。具体修改和验收见 `项目修改日志/2026-09-27-RO-SKILL-ANIMATION-ENTRY-CHANGELOG.md`。

## 数据与权威边界

`SkillEffectInfo` 的五个现有技能均为 `TargetMode=0`，10070 的距离、范围、命中和伤害字段也为零；`SkillPresentationInfo` 无技能 `TypeToken`、组合列表和弹体数据，`FxId=0`。当前 `SkillCastReq` 只有 UUID/技能 ID，开始/结束广播没有目标与弹体消息；服务端拒绝非零目标模式。故当前无法把 10070 描述中的“敌人头上”和八次伤害视作已实现。实现 RO 式目标弹体与组合技，需要具体技能数据、目标选择、权威协议和服务端结算一起落地，不能只增加空子类或把当前投射物空壳当作已运行功能。

验收记录见 `Docs/AI/TEST_REPORT.md`，修改明细见 `项目修改日志/2026-09-27-RO-SKILL-SUBTYPE-CHANGELOG.md`。
