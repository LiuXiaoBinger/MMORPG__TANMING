# RO 技能组件功能与命名对照

## 检查范围

用户要求重新检查 RO 客户端技能释放组件及服务器技能组件，将功能接入本项目，尽量沿用类、函数和成员变量名称，完成后检查。

参考源码根目录：`F:/UnityPro/ro/Ro_cs422nb`。

- 客户端：`clientcode/CSProject/MoonClient/Skill/MSkillComponent.cs`。
- 服务器共享逻辑：`rogamelibs/entity/component/skill_component.h`、`skill_component.cpp`。
- 服务器角色扩展：`rogamelibs/entity/component/role_skill_component.h`、`role_skill_component.cpp`。
- 本项目真实客户端入口：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`、`controller/UIController/SkillCtrl.cs`。
- 本项目真实服务器入口：`LYF_Server/GameServer/MessageHandler/Skill/SkillCastHandler.cs` → `Components/RoleSkillComponent.cs`，由 `OnlineRole` 容器创建并更新。

## 已确认的结构差异

当前客户端已经具备单技能与公共组 CD、当前动作和独立运行列表、预测与权威回包、普通技能打断、Type3 特殊取消及跳跃限制。与 RO 的差异包括 `_currentSkill` 命名，以及装配、查询、停止和开始入口的结构。

RO 服务器的 `ROSkillComponent` 基类承担核心装配、技能实例、CD 和施法生命周期；`RoleSkillComponent` 是角色扩展。本项目现有 `RoleSkillComponent` 同时承担核心执行和网络适配，需要把核心职责下沉到 RO 同名基类，并让原角色组件继承它，保持真实处理器及容器接入。

`LYF_Server/GameServer/SceneEntity/SkillComponent.cs` 是历史场景实体占位。当前 `RoleManager` 没有将在线角色绑定到该场景实体运行时；它不是现有正式施法入口。本次不凭空建立第二个角色宿主或扩展世界/AOI 管理来模拟接入。

## 最小接入原则

- 客户端保留当前等级核心与引用生命周期，原 CD、计时器、动画及 F/跳跃功能继续使用；同名入口必须实际接入调用链。
- 服务器使用 `ROSkillComponent` → `RoleSkillComponent` 的参考继承关系，保持单一活动施法与冷却来源；学习、MP 及协议处理沿本项目真实接口适配。
- 不引入参考之外的等待确认、重试、额外门禁或通用管理层。
- 不新增目标坐标等协议字段，不修改技能表及表现资源。

## 外部依赖能力

RO 组件还调用 QTE、Buff 技能替换、武器限制、阵营目标、墙体检测、技能威力同步、变身与完整世界同步等系统。本项目目前尚未接通这些完整玩法及对应协议；不能用空方法、固定成功返回或孤立字段将其记为已实现。用户已明确选择“先完整接入当前技能释放链，列清外部系统缺项”，本轮以此为验收范围。命中与伤害依赖完整目标及战斗运行系统，也列为当前尚未接入能力，不把无目标表现 Start/End 当成伤害结算。

## 验收与交接

### 客户端功能及命名

| RO 方法或成员 | 当前实现与必要适配 |
| --- | --- |
| `_skill`、`_skills`、`stop` | 主实例改为 RO 同名 `_skill`；列表保持原实例；停止位由 OnStop 设置，BeenStop 一次读取后复位。 |
| `SkillCDGroupList/SkillCDList/SkillCDGroupDic/SkillCDDic` | 四个原同名集合保持，本项目不缩放时钟与权威时间差继续驱动，表现回收不清合法 CD。 |
| `IsAlreadyCasting/IsCasting/GetLastSkill/GetSkill(ulong)` | 分别查同 UUID 的当前或列表实例、活动主槽、主槽或首项；正式动作开始使用同 UUID 去重。 |
| `BeenStop/IsPreperformance` | 一次消费停止通知；当前主实例未确认时返回预表现状态。 |
| `StopAllSkill/EndSkill/OnSkillEnd(MSkill)` | 结束主技能与列表，默认保 CD，显式清场才清 CD；实例回调解除组件原引用。 |
| `OnAttachSkill/CheckAndAttackSkillCore/AttachSkillInfo` | 11080 正式装配链使用这些入口，当前职业与已学等级经过原工厂创建核心，同等级不重建；移除失效技能。 |
| `HasSkillCore(uint)/HasSkillCore(int,int)/GetSkill(uint)/GetSkill(int,int)` | 区分效果主键与主表 ID/等级，只读当前已装配等级；查询未知效果不会自动赋予学习状态。 |
| `OnGetEvent→OnAttack→OnStart→FireSkill` | 强类型开始事件检查核心和原动作准入，OnStart 取核心，FireSkill 移交旧实例、借出载体并启动；统一 PlayAnim 触发动画。 |
| `OnSkillStart/OnSkillEnd/OnSkillCoreSync` | SkillCtrl 的 StartNtf、EndNtf、Ret 正式回调分别接入同名强类型入口；沿用预测去重、自然尾段、打断和 MP/CD 校正。 |
| `OnFinished/OnStop/OnUpdate/AfterFinish/OnSkillAutoEnd` | 按原动作时序更新和移交；补 RO 已有后摇/战斗待机标志与停止通知，当前默认动作消费这些标志。 |
| `OnDetachFromHost/Shutdown/ClearSkillCores` | 全部施法、核心与 CD 清场后走原解绑生命周期，停止通知清零。 |
| `_skillMgr/SkillMgr` | 本项目仍用 `_cores` 保持当前学习等级，旧施法独立持有原等级核心；不新增只为名称服务的管理层。 |
| `CanSkill` | 当前状态矩阵、InteruptSkill、Type3→CanSlideCancel 保持；CanStart 接原核心 CD/MP 与跳跃限制。 |
| `OnAttachToHost/ID/WithBattleVehicle` | 沿本项目既有宿主绑定、ComponentType 和动作基类属性适配，不复制另一套组件身份。 |
| `OnSkillWithOutAnim/FireSkillNoAnim` | 未接入；现 StartNtf 没有 Anim 等字段，Art 载体要求实际动画配置，无动画载荷和配置消费链未建立。 |
| `OnSkillResultTarget/SyncData/SkillPower` | 未接入；当前缺多目标结果、对应多目标表现和元素威力同步字段。 |
| `_qteSkillMgr/_endHandler/onSkillEndDelay` | 未接入；没有 QTE 管理，以及 RO 自动寻路恢复和等待普攻消费方，不设置孤立计时器或空管理器。 |

RO `GetSkill(uint)` 会按效果自动装配，本项目装配受真实中心服学习快照控制。RO 管理器可长期保留多个等级，本项目只保当前等级并让旧实例持有自己的旧核心。本项目没有组合子技能类型，因此 UUID 查询不额外添加无消费方的 `skill_num` 参数。

### 服务器功能及命名

| RO 类型、方法或成员 | 当前实现与必要适配 |
| --- | --- |
| `ROSkillComponent→RoleSkillComponent` | 新基类承接核心执行，角色子类保中心服学习快照；继承本项目 RoleComponentBase，原 OnlineRole 容器和 SkillCastHandler 实际调用同一组件。 |
| `ROSkillCore` | 新具名强类型保存主表与等级效果原引用；SkillID/SkillEffectID/Level 区分主表、效果与等级。 |
| `ROSkill` | 新具名强类型保存一次 UUID、所用核心、动作截止 Tick、表现段和结束原因，动作结束不退 MP/CD。 |
| `skill_`、`last_used_skill_effect_id_`、`skill_cd_group_cd_` | 尽量沿原 RO 字段名，当前实例、最后成功效果及组 CD 各有唯一来源。 |
| `CheckAndAttachSkillCore/GetSkillCore` | 按主表及学习等级装载，按效果或主表/等级查询；重复返回原核心，TryCast 实际调用。 |
| `CurrentSkill/GetSkill/IsCasting/GetLastSkillEffectId` | 查询当前原实例、同 UUID、活动状态及最后成功效果；不另外维护重复活动字段。 |
| `CheckDoSkill/CanSkill` | 接原活动取消、无目标模式与 CD 校验；学习、职业、死亡、MP 等真实请求准入沿 TryCast 保持。 |
| `FireSkill` | 全部准入通过后提交 MP/CD，再建立一次 ROSkill，段号只随成功请求推进；保旧 End→结果→新 Start 顺序。 |
| `Update/OnUpdate/SkillCease/EndSkill/StopAll` | 由既有在线角色固定帧更新，动作到期自然结束，死亡/离线中断；结束先清当前引用再广播，保合法 CD。 |
| `SetSkillCoolDown/GetSkillCoolDown/SetSkillGroupCoolDown/GetSkillGroupCoolDown` | 公开接口统一毫秒，内部保存 GameGlobal 单调截止 Tick，按 50ms 向上取整，与客户端秒单位明确区分。 |
| `LoadLearnedSkills/SkillsLoaded/GetLearnedLevel` | 角色子类只承担真实中心服学习装载，核心基类读取权威等级；不接受客户端指定学习等级。 |
| `InterruptActiveCast/Dispose` | 保留当前生产调用方兼容接口，结束委托到 EndSkill；角色与基类按同一生命周期释放。 |
| RO 状态机、ROSkillManager、HandlerManager | 本项目已有在线组件/固定 Tick/具名实例直接适配，无另造动作状态机、句柄管理器或场景角色宿主。 |
| RO 并发/脱手服务器技能列表 | 当前服务器仅已接入单一无目标活动动作；没有 RO 持续伤害、多目标、组合子技能生态，不添加无业务消费的列表。 |
| RO QTE/Buff/被动/变身/武器/目标/墙体/伤害/重连外观同步 | 当前尚未接入完整依赖系统，本轮遵用户确认范围保留缺项，不实现固定成功空接口。 |

原 `SceneEntity/SkillComponent.cs` 与 EntityFactory 保持，它们属于尚未绑定世界的历史场景运行时，与当前正式在线技能链明确区分。本次新增类型位于 `Components/ROSkillComponent.cs`、`SceneEntity/ROSkillCore.cs`、`SceneEntity/ROSkill.cs`，均不声明命名空间；GameServer 工程显式增加三个编译条目。

服务器 `EndSkill/Freeze(bool)` 使用当前协议 Interrupted 标记适配 RO SkillEndReason；CanSkill 的目标等参数不虚设，当前无目标/CD 与请求整体学习/MP/职业准入分工保持。ROSkillCore 装载/查询缓存真实配置引用，当前活动只保存一次 ROSkill，不扩展本项目尚无玩法消费的服务端持续技能列表。

### 最终验证

- 客户端277完整生产源编译0错/37既有警告；服务器及两引用隔离Rebuild0错/11既有警告。
- 独立165项：客户端25同名入口＋25真实发送＋24滑行；服务器28组件＋63权威滑行，审查通过。服务器开发另111项包含真实计时7项，不与独立断言合并。
- 主2145去重文件SHA一致，1502关键资源/表/协议/场景保护保持，`root-manifest-check.json` PASS；没有已复现未解决业务故障。
- 首发送最终日志实际为滑行24，因Program替换时间导致dotnet增量缓存。主/独立核证据阻断后强制重建，最终 `review/client-send-flow-final2.log` 为真实发送25；错误证据保留，未改业务或复制旧结果。
- 测试覆盖实际实体/技能/控制器/协议和服务端真实组件/配置；客户端Unity/状态机/UI/网络为薄边界，服务器无网关。未验正式Unity现场、Animator/碰撞/声音、TCP/多服务联机/安装包/Bundle，未部署重启或发布。
- 下一步原入口重新编译运行两端，现场验证普通组合、F、CD和自然结束。现有用户修改、并发输入与临时证据保持，不清理。

正式修改说明：`项目修改日志/2026-10-12-RO-SKILL-COMPONENTS-CHANGELOG.md`；协作状态：`Docs/AI/CURRENT_TASK.md`；证据目录：`_temp/ro-skill-components-20261012/`。不重启部署正式服务，临时文件保留。
