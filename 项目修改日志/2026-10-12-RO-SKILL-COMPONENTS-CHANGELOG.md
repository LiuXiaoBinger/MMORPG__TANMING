# RO 客户端与服务器技能组件对照接入

- 日期：2026-10-12。
- 需求：重新核对 RO 客户端技能释放组件及服务器技能组件，接入功能，尽量保持原类、函数和成员命名，完成后检查。
- 用户澄清：先完整接入当前技能释放链，列清外部系统缺项。
- 状态：开发与独立审查验收完成，无已复现未解决业务故障。

## 实际修改文件

| 文件 | 修改内容 |
| --- | --- |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs` | `_currentSkill` 改 RO `_skill`，补停止/查询/装配接口与 OnAttack→OnStart→FireSkill、OnFinished/停止通知；同名 OnSkillStart/OnSkillEnd/OnSkillCoreSync 接真实回包，保核心引用、CD、预测和表现尾段。 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs` | 11080 装配及 Start/End/Ret 调用组件同名入口，控制器仍处理协议、界面和权威 MP。 |
| `LYF_Server/GameServer/Components/ROSkillComponent.cs`（新增） | RO 核心释放基类，承接装配/查询、当前施法、CanSkill/CheckDoSkill、Fire/End/SkillCease/StopAll、单/组 CD 和原 TryCast 权威提交。 |
| `LYF_Server/GameServer/Components/RoleSkillComponent.cs` | 继承 RO 基类，只保中心服学习快照与等级查询，原真实处理器/在线容器/更新离线链实际进入同一基类。 |
| `LYF_Server/GameServer/SceneEntity/ROSkillCore.cs`（新增） | 具名强类型主动核心，保正式技能主表和等级效果原引用。 |
| `LYF_Server/GameServer/SceneEntity/ROSkill.cs`（新增） | 具名强类型一次施法，保 UUID、核心、表现段、固定 Tick 期限及中断标记。 |
| `LYF_Server/GameServer/GameServer.csproj` | 只新增上述三个类型的 Compile 索引，其余原字节保持。 |
| `Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md` | 更新当前结构、范围确认、验收与后续交接，保历史记录。 |
| `项目分析/RO_SKILL_COMPONENT_PARITY.md`（新增） | 客户端/服务端函数、成员、职责与差异对照，列清外部缺项和验证边界。 |
| 本修改日志（新增） | 归档需求、文件、思路、验证与未接入能力。 |

## 设计思路

沿 RO 原职责与调用顺序进行当前项目必要适配。客户端保原当前等级核心字典及旧施法引用，不为了成员名增加管理层；服务器采用 `ROSkillComponent→RoleSkillComponent` 参考继承分工，沿既有 `OnlineRole` 持久角色组件生命周期驱动，不凭空建立第二个场景宿主或扩展 World/AOI。

技能开始去重、停止及生命周期查询实际使用原实例。停止表现默认保合法 CD，清场才清 CD；原学习、MP、职业、普通互断、Type3/CanSlideCancel、跳跃、组合段、Timer/根运动/残影和服务器时钟修复保持。服务器 CD 接口用剩余毫秒，内部50ms Tick向上取整；EndSkill/Freeze(bool) 对应当前 Interrupted 协议，不新增结束原因协议。

原 `SceneEntity/SkillComponent.cs` 是未绑世界的历史占位，本次保持；当前正式施法由在线组件中的 RO 基类执行。所有新类型具名强类型、全局命名空间、同名独立文件，不增加等待确认、重试或通用框架。

## 验证结果

- 客户端277完整生产源编译0错误、37既有警告；服务器及两引用隔离Rebuild0错误、11既有警告。
- 独立165项通过：客户端组件25＋真实发送25＋滑行24；服务器组件28＋权威滑行63。覆盖真实装配、实例查询、预测/确认/拒绝、CD保留/清理、Q组合段、停止/死亡/卸载与F/跳跃原限制，审查PASS。
- 服务器开发另111项通过：滑行63＋核心41＋真实时钟7；实际1200ms忙循环Tick25/活动0，保持原计时修复，不与独立数合并。
- 主复核2145去重文件SHA一致，1502关键资源/表/协议/场景保护保持，见 `_temp/ro-skill-components-20261012/root-manifest-check.json`。两端7正式路径、277/236编译输入、72客户端独立源及运行边界清单均与当前字节一致。
- 首客户端Q测试假定动作0.6秒结束失败，修夹具真实时长后通过；首发送最终log误记滑行24，原因是替换Program保留旧mtime导致dotnet增量缓存。证据检查阻断后显式Rebuild并实际重跑，正确 `review/client-send-flow-final2.log` 为发送25通过，错误日志及首次主检查保留；未改生产代码迁就夹具或复制旧PASS。
- 六业务源中文注释与禁namespace/foreach/三目/弱业务载荷、一类一文件审查通过；服务器工程三条索引之外字节保持。无资源/协议/表变化，无正式测试挂载或发布。
- 四协作入口、正式分析和本日志收口检查通过（verify_records.py）；首两检查误要求决策直接含临时目录和专题引用自身，修正检查假设后通过，未改业务或正文迁就检查。

## 未接入能力与下一步

按用户确认范围，当前无目标释放链完成。RO 无动画载荷/载体、目标筛选/多目标结果/墙体、QTE、Buff/被动/技能替换、元素威力、命中伤害、吟唱/服务器脱手生态、自动寻路恢复/等待普攻、变身与完整技能重连序列化等外部系统仍未接入；详细差异见 `项目分析/RO_SKILL_COMPONENT_PARITY.md`。不将同名接口或无目标Start/End表现当成完整RO战斗结算。

客户端Unity/状态机/UI/网络仍为薄测试边界，服务端真实组件/表但无网关；未验正式场景画面、Animator/碰撞/声音、TCP/多服务联机、安装包/Bundle。下一步按原流程重新编译并运行两端，现场回归普通组合、F、CD与自然结束。本次不重启部署正式服务、不写数据库、不发布；全部临时证据保留于 `_temp/ro-skill-components-20261012/`，待产品经理确认后清理。
