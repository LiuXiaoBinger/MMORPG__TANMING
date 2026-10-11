# RO 风格技能系统落地设计

> 历史设计说明：本文第 2、4、5、6 节中的 `client_cast_seq`、`server_cast_id`、双 ID 去重及严格递增请求设计已由 2026-09-27 的 RO 单一施法 UUID 整改取代。当前代码与 RO 源码对照以 `项目分析/RO_SKILL_UUID_ALIGNMENT.md` 为准；本文保留原阶段范围与数据表分析，不作为恢复旧协议的依据。

## 1. 证据与边界

参考 `F:/UnityPro/ro/Ro_cs422nb/技能系统解析.md`，并核对 RO 的 `clientcode/CSProject/MoonClient/Skill/`、`roserver/gameserver/skill/`、`config/Table/CSV/SkillTable.csv`、`SkillEffectTable.csv` 和 `EffectTable.csv`。参考项目以 SkillId → 等级 → EffectId 分离技能定义与数值效果；`MSkillCore` 是实体长期数据，`MSkill` 是单次释放表现载体；客户端可预播，服务器决定真正施法和结果。参考解析内的代码示例及旧版二维角色寻路、Lua 规则不能直接复制到本项目。

本项目现状如下：

| 环节 | 已有资产与代码 | 缺口 |
| --- | --- | --- |
| 技能静态数据 | `Luaban/MiniTemplate/Datas/#SkillInfo.xlsx`、`cfg.SkillInfo`、`tbskillinfo.bytes`；已有 7 条 JobId=1 数据 | 一个技能只有一行，缺等级对应 EffectId、目标类型、服务器毫秒时间参数与表现映射；旧 `Hurt/HurtCount/HurtIterval` 语义不足以做权威结算 |
| 学习状态 | `CenterServer/DB/Table/RoleSkillTable.cs` 保存角色技能等级与绑定键；`RoleSkillInfoRet` 在 11080 返回 | GameServer 的 `RoleSkillInfoHandler` 未装载到在线角色；客户端收到列表后只用来绑定快捷键 |
| 客户端施法 | `SkillCtrl` 以 `Time.time` 维护本地 CD，派发无载荷 `SkillStarted`；`MSkillComponent` 只维护 `HashSet<int>` 并进入技能动作状态 | 未发施法请求、未绑定 SkillId/EffectId 到当前动作、没有服务器确认/拒绝、无完整一次施法生命周期 |
| 服务端场景 | `GameServer/SceneEntity/SkillComponent.cs` 是占位组件；`SceneManager.Update()` 为空；`OnlineRole.SceneUid` 尚未进入正式场景 Tick | 没有可用于目标命中、AOI、伤害的权威场景实体链；本期不得声称命中或伤害生效 |
| 动画资源 | `MAnimationMgr` 的 `animInfo.txt` 已登记 MJS 的 Atk01~03、Skill01~04 七条独立剪辑；`MAnimator` 处理异步加载/覆盖 | `SkillInfo.Action` 31/34/35/36/37/41/51 到剪辑没有映射；JX 独立技能剪辑未登记；职业 ID 与 MJS/JX 的正式对应需按 `ProfessionInfo.PrefabPath/PresentTableID` 校对 |

### 本轮最低可交付范围

1. 保留原有 `#SkillInfo.xlsx`、生成代码、协议 11080、角色技能数据库行和旧客户端资源，不覆盖、不迁移删除。
2. 新增两张 Luban 表及最小真实数据；`SkillId + 已学等级 → EffectId → 动画短名/Location`。已核对 `#ProfessionInfo.xlsx` 中职业 1 的 `PrefabPath=Role/Role_MJS, PresentTableID=1`，`#PresentTable.xlsx` 中 1 的 `Prefab=Role_MJS`，故当前 JobId=1 的五个攻击技能可配置真实 MJS 剪辑。JX 资源不凭文件名推测。
3. GameServer 在逻辑线程给在线角色加载已学技能，处理客户端施法请求，校验角色/会话/等级/职业、HP、技能状态、单技能与组 CD、MP 和 `TargetMode`；成功原子扣 MP、设置 CD，产生服务器施法 ID，并向施法者返回结果，向同地图现有附近角色及施法者发送开始/结束事件。
4. 客户端收到角色技能列表后建立等级配置，按效果 ID 选表现；本地允许仅对动画做即时预测。服务端拒绝时结束预测、恢复默认动作，并以回包 MP/CD 覆盖本地 UI；服务端开始事件确认本地预测或驱动他人表现；结束事件及断线/销毁回收资源。
5. **本轮不实现命中、伤害、Buff、子弹、引导、多段、位移、技能升级或持续 MP 持久化。** `TargetMode=0` 表示不选择目标、只播放方向/自身表现，本期唯一允许释放的模式。`TargetMode=1/2/3` 分别预留实体、坐标、自身范围，服务器本期统一拒绝 `SkillTargetUnsupported`。成功施法不等于命中。项目中滑行/轻功 `SkillInfo.Type=3/4` 不走本轮普通战斗施法处理器。

## 2. 分层和状态契约

RO 同名可沿用，但不能再创建第二个 `MSkillComponent`。既有客户端 `MSkillComponent` 继续作为场景实体的动作状态和技能快捷组件；扩展其技能核心与当前表现引用。建议新增 `MSkillCore`（单个已学效果及预测/权威 CD）、`MSkillMgr`（按 SkillId/EffectId 索引）、`MSkill`（一次施法的表现生命周期）、`MArtSkill`（基础单段动画）、`MSkillFactory`（构建/回收）。对象池仅在生命周期和取消均正确后引入，不让池化掩盖过期回调。`SkillCtrl` 只负责输入、ViewModel 与网络请求，不独立成为冷却权威。正式业务事件必须携带具名强类型 `SkillCastEventArg`（SkillId、EffectId、客户端序号、服务器施法 ID、时间、预测标记），不能再对技能派发 `null`。

服务端在 `OnlineRole` 角色组件容器注册独立的 `RoleSkillComponent`，保存从 Center 装载的已学等级、在线 MP、CD/组 CD、当前施法与最近请求去重记录。GameServer `SceneEntity.SkillComponent` 仍只是未来场景战斗组件，本轮不得把角色技能状态同时放进该占位组件。`RoleSkillInfoHandler` 对 11080 回包先完成既有原包转发，再按复合 Gate/Unity 会话找到在线角色，装载技能列表；未装载完成的请求拒绝。不能依赖客户端回包填服务器角色学习状态。网络回调只入 `GameCommand`，施法修改只在 GameServer 主逻辑线程，固定帧按服务器单调 Tick 结束施法。

施法状态为 `Idle → Requested/Predicted(客户端) → Casting(服务器确认) → Finished/Interrupted/Rejected → Idle`。客户端预测仅控制动画与 UI 临时显示，不扣权威 MP、不产生伤害。服务器在接受请求的同一逻辑线程检查并提交 MP/CD；技能释放后即开始 CD，施法中断时默认不退 MP/CD，除非以后某个数值字段显式规定。死亡、断线、替换在线角色、切图必须结束当前施法并递增生命周期代次，旧广播和异步资源回调不得作用于新角色。客户端用 `(角色 ID, server_cast_id)` 去重广播；自己的 `(连接代次, client_cast_seq)` 将预测匹配到服务器结果。网络超时只清理预测并请求权威状态，不能自行确认成功。

### 权威规则与数据语义

- `SkillInfo.Id` 是稳定技能 ID；`RoleSkillTable.SkillLevel` 为学习等级，`0` 不可施法。`SkillEffectInfo.Id` 是效果 ID，按 `(SkillId, Level)` 唯一，找不到或重复时拒绝。服务器数值只读新表；旧 `SkillInfo.CDTime/SpendMP/Hurt*` 保留旧 UI/兼容用途，不参与新施法的权威扣费与 CD。
- 服务器从 `RoleManager.GetOnlineRoleBySession(gateSessionId, unitySessionId)` 解析身份，忽略客户端任何角色 ID。要求 `RoleSkillInfoRet` 已成功装载，职业与表一致，角色 HP > 0 且未处于禁止施法状态。当前项目没有服务端完整状态机时，以可证明的在线/存活/当前施法互斥为最小状态校验；眩晕、沉默等待权威状态落地后再开放配置。客户端本地 `MStateMachine.CanSkill()` 只用于避免无效预测。
- `CooldownMs`、`GroupCooldownMs`、`CastMs`、`RecoveryMs` 为非负整数毫秒；CD 按服务器单调时间计算，回包另给 Unix 毫秒供 UI 对时。`MpCost` 为非负整数；检查与扣费在同一逻辑帧原子执行，不能变成客户端上报消耗。0 级、资源不足、重复序号/不同载荷、同帧双请求均要确定返回。
- 同地图附近广播只能沿用当前 `RoleManager` 的在线角色快照和 `IsNearbyRole` 现有语义；这个 100m 角色可见范围只是表现通知，不是权威命中范围或正式 AOI。服务端场景实体与移动同步就绪后换成场景 AOI。
- 在线 MP 和 CD 变化本轮仅为权威内存状态。现有 `SaveRoleDataReq` 只保存物品/次数，没有 MP/CD 持久化；`OnlineRole` 新实例重连会重置 CD、可能恢复旧 MP，存在重连规避成本的已知限制，不得标记完整战斗资源验收。下一阶段须设计 CenterServer 的版本化属性和冷却保存/确认并测断线与重连；在此之前不应把本轮技能当作完整对外战斗玩法发布。

## 3. 数据表设计与兼容

新增工作簿均放在 `Luaban/MiniTemplate/Datas/`，沿用本项目 `#表名.xlsx` 与 `##var/##type/##` 三行结构。当前 `__tables__.xlsx` 没有手工表注册行，现有 `#SkillInfo.xlsx` 是 Luban 按命名约定自动发现；新增表也应使用同一发现规则，并以实际生成检查确认。**禁止修改既有 `#SkillInfo.xlsx` 的表头、数据或删除旧 `cfg.SkillInfo`；新增表可并行生成独立 `cfg.SkillEffectInfo`、`cfg.SkillPresentationInfo`。** `SkillInfo.Id` 是唯一外键，不直接以旧 `Action` 充当 EffectId。旧角色 `role_skill` 按 `(RoleID, SkillID)` 原样查询；新增表未覆盖的老技能仍可展示，但新施法明确拒绝 `SkillConfigMissing`，不悄悄套用旧 Hurt 值。

### `#SkillEffectInfo.xlsx`

每个可释放等级一行。字段：`Id:int` 唯一 EffectId；`SkillId:int` 引用旧 SkillInfo.Id；`Level:int` 从 1 起；`TargetMode:int`（0 本期无目标、1 实体、2 坐标、3 自身范围，后三种暂拒）；`CooldownMs:int`、`CooldownGroupId:int`（0 不共享）、`GroupCooldownMs:int`、`CastMs:int`、`RecoveryMs:int`、`MpCost:int`；`MinRangeCm:int`、`MaxRangeCm:int`、`RadiusCm:int`、`MaxTargets:int` 为未来命中参数，当前 TargetMode=0 必须填 0；`HitCount:int`、`HitIntervalMs:int`、`DamagePermyriad:int`、`FlatDamage:int` 为未来结算参数，本期全部填 0，不能把既有文本描述当战斗数值。`Id` 不随表格排序重编；`(SkillId,Level)` 唯一。技能 CD 由表中毫秒值决定，旧 `SkillInfo.CDTime` 秒值需要人工核对但不能自动迁移。

### `#SkillPresentationInfo.xlsx`

每个实际动画变体一行。字段：`Id:int` 唯一；`EffectId:int` 引用效果；`ProfessionId:int` 对齐职业表；`ModelKey:string` 对齐项目现有模型/动画资源前缀（如经校对后的 MJS）；`WeaponType:int`（0 默认）；`VariantIndex:int`（0 默认）；`ClipName:string` 为 `animInfo.txt` 已登记短名；`ClipLocation:string` 为 YooAsset Location；`DurationMs:int` 非负、必须与剪辑长度人工核对；`FxId:int`（0 表示本期无特效）；`AudioLocation:string`（空表示无音效）。按 `(EffectId,ProfessionId,ModelKey,WeaponType,VariantIndex)` 唯一。未在 `animInfo.txt` 登记或资源不存在时不能造出路径，表现配置缺失可返回技能成功但应使用既有 Skill 状态回退动画并记录资源告警；测试时应将该职业的资源映射视为未完成。

RO 更细的时间轴可在下一阶段新增 `#SkillTimelineEvent.xlsx`（EffectId、变体、事件序号、AtMs、事件类型、FxId/AudioLocation/Bone/偏移），以及 `#SkillFxInfo.xlsx`（FxId、PrefabLocation、跟随/持续时间）。本轮不复制 RO 的 Lua `DamageScript/ConfirmScript`，不在客户端表中解释权威战斗脚本。

### 数据迁移与发布门禁

1. 先备份并核对 `#SkillInfo.xlsx`/既有二进制校验值；新增工作簿并注册，生成到现有 Luban 管线。生成代码属于第三方输出，可保留生成器原有命名空间，不要手改生成文件。
2. 全表验证主键、`(SkillId,Level)` 唯一、技能外键、等级范围、非负时间/费用、TargetMode=0 对应未来命中字段为零、ClipName/Location 在索引和资源中真实存在、职业 PrefabPath/PresentTableID 对应实际模型。新旧 CD/MP 差异单独列出供策划确认。
3. 原有角色学习等级与绑定键不修改；新数据仅对已学 Level>0 生效，不自动开放旧 0 级技能。不存在可释放等级效果的技能仍显示在 UI，服务器返回配置错误。
4. 本期 5 行真实数据覆盖职业 1 的 10010/10040/10050/10060/10070 等级 1。旧 Action 31/34/35/36/37 本次分别策划映射至 `Anim_MJS_Atk01`、`Anim_MJS_Skill01`、`Anim_MJS_Skill02`、`Anim_MJS_Skill03`、`Anim_MJS_Skill04`；这些是本项目新增配置关系，旧表没有 Action→资源路径。只有 10010 默认已学一级，其余四项仍为原有 0 级，不能因新增效果表自动开放。旧描述承诺命中/伤害，但本期不会结算；在下一阶段补全结算前，正式 UI 不应把这五条当作完整战斗技能发布。

## 4. 协议与网络时序

`LYF_Server/ProtoBuf/proto/RequestEntity.proto` 新增 `SkillCastReq`；`ResultEntity.proto` 新增 `SkillCastRet`、`SkillCastStartNtf`、`SkillCastEndNtf` 与具名技能错误码。使用未占用的命令号 11170（请求/结果）、11171（开始广播）、11173（结束广播）；11172 暂不定义命中消息。`NetDefine`、Gate 两向注册、GameServer 回调/Handler 和客户端 Network.dll 都必须随生成物一起更新。旧 11080 只负责技能列表，不作为施法协议。

| 消息 | 字段与单位 | 处理 |
| --- | --- | --- |
| `SkillCastReq` | `client_cast_seq:uint64` 本连接单调非零；`skill_id:int32` 旧 SkillInfo.Id | 不携角色 ID、等级、费用、CD、伤害或客户端命中；身份来自复合会话，等级来自 Center 装载 |
| `SkillCastRet` | `cmd_code:CmdCode`、`client_cast_seq:uint64`、`skill_id:int32`、`effect_id:int32`、`server_cast_id:uint64`、`server_time_unix_ms:int64`、`current_mp:int32`、`cooldown_end_unix_ms:int64`、`group_cooldown_end_unix_ms:int64`、`cast_duration_ms:int32` | 成功含权威提交结果；失败也回序号、技能、MP/CD 快照；未识别技能时效果/施法 ID 为 0 |
| `SkillCastStartNtf` | `caster_role_id:int32`、`server_cast_id:uint64`、`skill_id:int32`、`effect_id:int32`、`start_server_time_unix_ms:int64`、`cast_duration_ms:int32` | 先回施法者结果，再通知施法者及现有同地图附近角色；客户端按 ID 去重 |
| `SkillCastEndNtf` | `caster_role_id:int32`、`server_cast_id:uint64`、`skill_id:int32`、`end_server_time_unix_ms:int64`、`interrupted:bool` | 正常结束、死亡/断线/切图取消发结束；断线本人收不到不影响服务端回收 |

拒绝码追加 `SkillNotLearned`、`SkillConfigMissing`、`SkillOnCooldown`、`SkillInsufficientMp`、`SkillStateBlocked`、`SkillTargetUnsupported`，保留原 `CmdCode` 数值 0~27 不变。若服务器可确定为非法会话，仍用既有 `InvalidSession`。同一会话同一 `client_cast_seq` 重发必须回同一结果，不得重复扣 MP/CD；序号相同而技能不同返回 `DuplicateRequest`。结果广播包含权威服务器施法 ID，使客户端在重传、乱序和预测匹配时不会重复播。

## 5. 客户端表现生命周期与资源

`SkillCtrl` 接收输入后查询本地学习等级并找到效果；检查本地状态和预测 CD，仅作为体验闸门，生成序号并发送 `SkillCastReq`。预表现先使用 `SkillPresentationInfo` 的职业/模型/武器变体定位 `ClipName`，经 `MAnimationMgr.GetClipInfo` 与 `MAnimator.OverrideAnim(STATE_SKILL, ClipName/Location)` 获取和释放共享动画资源；应使用现有异步加载的会话代次过滤过期回调。没有真实资源配置时不得构造 `Action` 数字路径。MP、CD UI 以服务端 `SkillCastRet` 的快照校正。

表现载体只持有一次施法的 SkillId、EffectId、角色 UID/RoleId、客户端序号、服务器施法 ID、开始时间、动画租约、特效/音频句柄。结束、拒绝、模型销毁、切图、断线和对象池回收都必须幂等释放；后一技能开始时旧表现按规则中断。自己的广播若已由预测播放，仅绑定服务器 ID 与时间，不二次播；其他角色由开始通知实例化表现。`MArtSkill` 本期只负责单段动画和可选已证实的资源，服务器结束通知驱动状态恢复；动画时长早于结束只进入等待/Idle，不宣称命中。正式特效时间轴、骨骼挂点、音频和子弹由后续表驱动。

## 6. 测试与返工门禁

1. 数据：新旧表并存、旧文件哈希不变；新增表双端 Luban 生成与加载通过，外键/唯一键/数值范围/动画索引和真实资源逐项通过。
2. 协议：`protoc` 后 Network/客户端程序集版本一致；11170/11171/11173 在 Gate 上下行完整登记，错误结果不会被通用转发吞掉，11080 仍照常到达技能 UI。
3. 权威：未学/0 级、职业不符、表缺失、无目标模式以外、MP 不足、CD/组 CD 内、死亡、未装载、伪造角色/会话、重复请求与同帧多请求均拒绝；成功只扣一次 MP、只启动一次 CD、只产生一个 cast ID。
4. 表现：MJS 已登记的真实剪辑能播且释放后回 Idle；本人预测+确认只播一次，拒绝/超时回滚预测，其他玩家只因服务器开始通知播放；关闭场景/断线/模型池复用后无残留动画/资源句柄。
5. 运行：GameServer/Center/Gate/Unity 联调记录时间线、同地图附近角色广播和跨地图不广播；不把构建通过等同实际联调。测试失败交对应开发 Agent 返工后重新编译、重新审查。
6. 禁止用本轮施法成功证明命中/伤害/MP/CD 持久化。下一阶段先接通场景实体 UID、服务器 Tick、目标与距离、AOI 和移动权威，再定义 11172 命中广播及伤害/属性同步；另补角色 MP/CD 的版本化持久化与重连回归。

## 7. 开发交接顺序

策划先交新表和协议源；服务端开发 Agent 生成协议、加载配置、把 11080 装到 `OnlineRole.RoleSkillComponent`、实现 GameCommand 主线程处理和起止广播；客户端开发 Agent 更新 Network.dll、接入 `SkillCtrl → MSkillComponent/MSkillCore/MSkill` 与资源映射；测试 Agent 构建、静态检查和四服/Unity 联调；审查 Agent 复核后有失败立即打回对应开发 Agent。新增业务类型一类一文件、全局命名空间、中文注释到字段/参数/局部变量，禁用 `foreach`、三目和弱类型业务载荷。项目修改记录存放 `项目修改日志/`，协作结果同步 `Docs/AI/CURRENT_TASK.md` 与 `Docs/AI/TEST_REPORT.md`。
