# RO 技能冷却和存活实例管理参考

日期：2026-10-11。本稿包含真实源码核对与历史方案。最新要求为参考项目优先照原结构实现，不增加需求外机制；末节“参考移植简洁约束与更新分工”覆盖原常驻统一更新/池化快照方案，恢复 RO 当前 OnUpdate 与列表 Update 分工。此前“取消额外确认等待”仍覆盖旧逐笔确认期限、pending 管理及等待门禁。历史验收只对应当时源码。

当前修正还包括“CD与表现实例回收独立”：末节同名说明覆盖历史预表现Cease→Reload清CD选择；正常无回包表现收尾不能取消角色冷却，否则会提前重放。

## 源码范围

参考项目只读：`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/MSkillComponent.cs`、`MSkill.cs`、`MSkillCore.cs`、`MSkillFactory.cs`、`SkillLogic/MArtSkill.cs`，以及 `Network/protocol/process/login/process_ptcg2c_reconnectsyncnotify.cs`。

本项目：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`、`SceneEntity/Skill/MSkill.cs`、`MArtSkill.cs`、`MSkillCore.cs`、`MSkillFactory.cs`，以及 `controller/UIController/SkillCtrl.cs`、组件基类、管理器和实体更新入口；服务器 `LYF_Server/GameServer/Components/RoleSkillComponent.cs` 仅核既有权威 CD 写入语义，不修改。

## RO 的两个冷却表

RO 的类型准确名称是 `SkillCDGroupInfo`，不是 `MSkillCdGroupInfo`。它在参考 `MSkillComponent.cs` 顶部定义，含 `int SkillGroupType`、`float CD`、`float CurrentCD`。后两者单位都是秒，分别是此笔冷却总量和当前剩余量。

| 容器 | 字典键及 SkillGroupType 语义 | 列表用途 |
| --- | --- | --- |
| `SkillCDDic` / `SkillCDList` | 单技能主表 `SkillID`；不是效果 ID、施法 UUID 或公共组号 | 每帧倒序递减单技能剩余 CD |
| `SkillCDGroupDic` / `SkillCDGroupList` | 公共冷却组类型；参考读取效果表 `PVEGroupCoolTimeType` | 每帧倒序递减公共组剩余 CD |

两表复用同一个数据类型，因此普通表也使用 `SkillGroupType` 字段。不能因字段名有 Group，就把普通 CD 改成按组删除。更新到零时先从列表和对应字典删除，再由工厂归池。工厂借还接口名称为 `GetSkillCdGroupInfo()`、`ReturnSkillCdGroupInfo(SkillCDGroupInfo info)`。

RO 组件实际接口：

```csharp
public float GetSkillCD(int skill_id)
public float GetSkillTotalCD(int skill_id)
public void SetSkillCD(int skill_id, float cd)
public void SetGroupCd(MSkillCore core, float cd)
public void SetGroupCdByType(int type, float cd)
public void ClearGroupCd()
public void StopAllSkill(bool clearCD = false)
public void OnSkillEnd(MSkill skill)
public override void Update(float fDeltaT)
```

`ClearGroupCd()` 的实际行为是清理并归还两类冷却，不是仅清公共组。`StopAllSkill(true)` 在重连同步前停止技能并清 CD，再分别调用 `SetGroupCdByType` 和 `SetSkillCD` 恢复服务器快照。`MSkillCore.CooledDown` 要求单技能和公共组都结束；`GetCDLeftValue` 显示时先读单技能，单技能为零才读组 CD。

RO `MSkillCore.Performance()` 将本地预测时长写入 `SetSkillCD`；`Reload()` 写零撤销预测。`SetTimer(float cool_down, float group_cool_down)` 接权威同步并调用两类 Set。参考项目使用自己的 `DelaySeconds * 2` 网络估算，本项目没有同样的延迟契约，不能直接移植该补偿。

参考源码有两个应避免照搬的细节：`SetSkillCD` 写零删除记录时没有归池；更新已有记录比较的是 `cd` 与总量 `info.CD`，且未同步修改总量，重复同总量校正可能不刷新剩余量。本轮保名称和用途，零/负剩余必须实际删除并安全归池；权威刷新必须更新当前剩余值。

## RO 脱手技能到底是什么

RO 当前动作 `_skill` 与存活列表 `_skills` 有不同责任。`AfterFinish` 调用 `OnSkillAutoEnd`，将仍 Casting 的当前技能转入 `_skills`，清空动作槽；角色可以恢复移动、待机或开始另一技能，旧技能仍持有自己的 UUID、计时器和其他运行资源，等待服务器结束或运行超时。这就是此处“脱手还在生命周期”，不是单指投射物，也不能因角色动画结束就回收实例。

RO 当前槽在 `OnUpdate(float)` 调用技能 Update；脱手列表在常驻 `Update(float)` 倒序调用。`MSkill.Update(float)` 返回 bool：正常存活返回 true；参考普通技能在执行期间超过 `_data.Time + 预表现2秒/确认10秒` 返回 false。`MArtSkill.Update(float)` 最后返回基类结果，`MArtSkill.IsEnd()` 用 `SkillStateTime` 判动作槽退出。**IsEnd 是动作状态到期，Update false 是生命周期应终止，二者不可混为一谈。**

`Cease(bool cleanUp = false, SkillEndReason reason = ...)` 则实际终止生命周期：停止 Casting、注销计时器、调用停止钩子、通知所属组件 `OnSkillEnd(this)` 从槽/列表删除、最后归还工厂。单纯把 Update 改成 bool 而没有 Cease→所属组件→归池的闭环，不构成同名功能。

## 本项目已有内容与缺失

本项目已经有 `_currentSkill`、`_skills`、`OnSkillAutoEnd`，以及自然 End 转脱手、旧 UUID 确认、未来 timeline/冷模型 GO 请求/声音完整尾段保留、真正 Interrupted 即时清理。现 `Update(int)` 分两轮先推进旧列表再按自然结束收尾或十秒宽限回收；`MSkill.Update(float)` 和 `MArtSkill.Update(float)` 返回 void，缺 RO 的 bool 契约及公开 Cease/组件 OnSkillEnd 终止入口。

冷却已有 `MSkillCore._cooldownEndRealtime` 维护单技能截止时间和按 UUID 管理的预测回滚；组件 `_groupCooldownEnds` 保存权威公共组截止时间。窗口通过 `GetRemainingCooldown` 查询，`CanStart` 同时校验 Core 单技能和组截止。缺上述 RO 双列表、双字典及工厂冷却记录接口。新容器必须进入正式输入、权威回包、预测超时、清场和 UI 查询链，不能仅额外声明一套无人使用的结构。

已有 11170 回包使用 `CooldownEndUnixMs - ServerTimeUnixMs`、`GroupCooldownEndUnixMs - ServerTimeUnixMs` 换算非负剩余秒数，不依赖客户端墙上时钟。后端在成功施法时分别写单技能 tick 截止和公共组 tick 截止；本轮不新加后端协议。当前没有 RO 对等的重连两类 CD 快照数据，不假造接入。

## 接入和兼容要求

1. 独立新增同名 `SkillCDGroupInfo.cs`，工厂提供同名借还，原工厂静态调用风格保持；不为了 `.singleton` 改写全部原调用。列表和字典同名，普通键映射 `MSkillCore.SkillId`，组键映射 `CooldownGroupId`（零不属于组）。
2. 组件覆盖 `Update(float fDeltaT)` 承接 RO 常驻职责；本项目主循环真实传 int 毫秒，`Update(int)` 保留作为毫秒转秒的入口。基类已具有 virtual Update(float)，无需修改世界循环。最终采用常驻入口统一推进当前与脱手各一次，动作状态 `OnUpdate` 只判断 `IsEnd` 并完成动作；这是本地状态机适配，避免离槽当帧或不同调用顺序造成双推进。`MComponentMgr` 的专用顺序仍 Move→Buff→Skill→Machine，实体位移末帧提交顺序保持。
3. 双列表每帧倒序递减、到零从表删除归池，仅玩家 CD 更新与 RO 保持。本地正式CD原来按不缩放realtime，因此记录追加内部UpdatedRealtime：主循环扣实际realtime差量，查询只计算该记录的实时剩余而不修改记录，技能逻辑仍推进fDeltaT。两者不共用缩放时钟，不把此适配说成逐字照搬RO的fDeltaT递减。输入的秒值必须有效，不让非有限数产生永久条目；写零/负剩余清对应项、重复设置复用已有记录并同时刷新CD/CurrentCD为本次cd，清场全部归池。
4. `BeginPrediction` 同步单技能 CD；匹配最新 UUID 的 `ApplyCastResult` 接两类权威 CD；Core 预测超时/撤销同步回滚当前普通 CD。旧段回包继续只确认自身，不能覆盖更新的单技能、公共组或 MP；等级升级仍转移预测记录和 CD。UI 和 CanStart 读取必须与正式 CD 一致，同名 Set/Clear 也必须改变正式可施法判定。
5. 已安装Core绑定所属组件，所有正式单技能CD判定及读写均进入该组件唯一记录；预测保留原UUID和回滚快照。Core旧截止仅兼容尚未安装的独立核心，不参与已安装技能的第二份计时。升级仍保原同技能记录，不能显示清零而Core另有旧截止阻断。保既有可传实时秒数的公开契约，不靠每帧全字典扫描重建列表。
6. `MSkill.Update(float)` 返回存活判定，子类 PruneAudio 后再决定自然 End 是否可回池；自然结束必须 `HasServerEnded && IsPresentationFinished`，不能仅动画时长或状态时长。结束丢失继续保已有制作末事件、完整长音后十秒宽限。预测两秒超时仍由 Core 按各笔 UUID 管，不替换为只看当前动画长度。
7. `Cease()` 与 `Cease(true)` 都沿RO语义实际终止：通知所属组件按原对象引用移除，再工厂Reset/Recycle；本项目当前没有cleanUp的额外清理策略，参数仅兼容同名调用，不把false解释成等待自然尾段。自然服务器End仍由原ApplyServerEnd→MarkServerEnded→OnSkillAutoEnd保尾，完成后才Cease。重复Cease不双归池。常驻循环以池化MSkillUpdateEntry值快照保存原对象和既有施法代次，避免虚Update删除前项或同步归池重Fire后重复推进/用旧false停止新施法。当前槽停止协调状态机回默认，实体位移/动画根及FireEvents→MTimerMgr→OnProcessTimer职责不改。

## 验收重点与边界

独立审核至少覆盖普通/组不同键、同组两个技能、重复设值/零移除/到零删除及池复用、玩家与非玩家、秒与毫秒入口一致、两个 CD 查询和 CanStart 一致、预测/权威/拒绝/超时回滚/旧段隔离/等级继承/清场，当前槽和多个脱手实例都各推进一次，Cease 自移除不漏前项、不二次回池，自然 End 晚于动作的事件/冷 GO/长音保留、Interrupted 即清、动画根末帧和旧代隔离回归。

本稿只读分析未运行编译或游戏；结论依当前源码，不把已有历史 189 项当本轮运行通过。用户当前 `3007001` 制作稿 `F13/end111/末声音F49`、已发布 Runtime、资源/meta/场景保持，不自动导出。

关联协作入口：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`；详细改动及最终验证由本轮 `项目修改日志/` 正式记录归档。

## 本项目末稿接口与验收

本轮已完成同名功能移植，以下行号已核正式末稿；前文StopAllSkill等是RO参考接口清单，本轮实际清场继续使用既有ClearSkillCores/Shutdown，不宣称移植参考项目全部技能系统。

| 实际接口 | 本项目源码位置 | 用途 |
| --- | --- | --- |
| `public override void Update(float fDeltaT)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs:692` | 常驻推进当前及脱手实例，倒序清两类CD，到期归池；int入口:685仅转秒。 |
| `GetSkillCD(int skill_id)` / `GetSkillTotalCD(int skill_id)` | 同文件:223 / :243 | 查同一单技能记录，分别返回非负剩余和本次设置总量。 |
| `SetSkillCD(int skill_id, float cd)` | 同文件:254 | 创建/覆盖记录，同值刷新；零/无效清除归池，正式预测和权威同步进入此源。 |
| `SetGroupCd(MSkillCore core, float cd)` / `SetGroupCdByType(int type, float cd)` | 同文件:295 / :304 | 按配置组或显式组号同步公共CD；与普通键独立。 |
| `ClearGroupCd()` | 同文件:342 | 同时清两类CD并归池，Core判定同步读零。 |
| `OnSkillEnd(MSkill skill)` | 同文件:362 | 解除原对象在当前槽/脱手列表的引用；当前结束回默认动作。 |
| `public virtual bool Update(float deltaSeconds)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs:227` | 返回生命周期是否继续存活，false由组件Cease，区别动作结束。 |
| `public virtual void Cease(bool cleanUp = false)` | 同文件:241 | 默认/true都真结束并通知组件和归池；本地无额外cleanUp策略。 |
| `public override bool IsEnd()` / `public override bool Update(float deltaSeconds)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:43` / :88 | 动作窗结束判定；声音回收后返回基类存活状态。 |
| `BindCooldown(MSkillComponent owner)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillCore.cs:131` | 安装Core与组件共用唯一CD记录，原预测UUID/回滚与未安装兼容保留。 |
| `singleton.GetSkillCdGroupInfo()` / `ReturnSkillCdGroupInfo(SkillCDGroupInfo info)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillFactory.cs:18` / :25 | 借还同名强类型记录，归池清状态；原静态技能工厂调用保持。 |

独立终验PASS：253断言为189本轮既有回归＋48CD＋16生命周期，47完整生产输入；独立273真实客户端源码Rebuild零错/37旧警告。主613项末稿与编译/运行输入SHA零差异。2796保护2791保持、5授权旧C#修改，4授权新源码/meta无越界；生成工程无事前字节快照限制另记，不能宣称完整before保护。

当前自然End晚FX/Audio、冷GO、完整声尾及Q/池UUID代次、实体位移末帧与碰撞回归通过；默认Cease首稿等待语义已修为RO真结束，用实际取消资源专项验证。CD保既有不缩放时钟的必要适配、两个新独立类型及十路径范围见 `项目修改日志/2026-10-11-RO-SKILL-CD-LIFETIME-CHANGELOG.md`。

Unity原生与动作状态机仍薄边界，网络仅源码核对与完整编译；真实PlayMode/画面/听感、墙体、Animator精确末帧/跨窗、实际服务器联调/Bundle未验，不将隔离通过扩大为现场结论。证据 `_temp/ro-skill-cd-lifetime-20261011/`，临时保留，不自动发布用户F13/end111/F49及Runtime资产。

## 取消额外确认等待（2026-10-11，最新要求）

用户明确只移植 RO 实际具有的机制，不额外等待服务器确认。因此撤销前面接入要求第 4、5、6 条中保留 `_pendingSkillIds`、Core `_predictions` 和各笔确认超时的设计，相关旧验收只作为历史记录。新一轮证据目录为 `_temp/ro-skill-no-confirm-wait-20261011/`。

重新只读核对 RO 的结果如下：

| RO 真实入口 | 实際行为 | 对本项目的结论 |
| --- | --- | --- |
| `MSkillCore.Performance()` | 将 `CoolTime` 写入组件单技能 CD，设置核心 `performance_` | 本地请求可以立即启动表现和预测 CD，不为请求建立独立到期票据。 |
| `MSkillCore.Reload()` | 清 `performance_`，单技能 CD 写零，清 `fire_time` | 尚未确认的表现实例结束时可以解除预测 CD；只作用于原施法，旧段不得清新段 CD。 |
| `MSkillCore.SetTimer(float, float)` | 清核心预表现标志并写单技能、公共组权威 CD | 权威同步独立校正 CD，不以寻找 pending 记录作为应用前提。 |
| `MSkillCore.ServerFire(int using_times)` | 仅将 `UsingTimes` 设为服务器段号，注释里的 CD 修改没有执行 | 服务器段号只校正后续动作选择，不能重播已开始的当前动画。 |
| `MSkill.Fire(...)` | 选择本次数据，设 `_casting`，立即 `Execute()` | 没有“等待回包后才 Fire/Start”的门禁。 |
| `MSkill.ServerConfirm()` | 只将实例 `_performance` 改为 false；组合技转交根节点 | 确认属于实例状态切换，不是解锁后才能继续输入。 |
| `MSkill.Update(float)` | 执行时推进 `_timeElapsed`，超过 `_data.Time + (_performance ? 2 : 10)` 返回 false | 这是**每个施放实例的最大存活时间**，不是组件额外轮询每笔请求的确认期限。 |
| `MSkill.Cease(...)` | 终止实例；若仍预表现，调用核心 Reload；组件解除原引用并归池 | 生命周期在实例 Update/Cease 闭环内结束，不另建确认管理器。 |

RO `CanSkill` 读取 `CooledDown`，后者校验实际单技能和组 CD；未发现以“当前请求尚未确认”为由阻止下一次施放的额外门禁。参考 UI 中有 `IsPerformance` 判断，它用于 CD 展示判断，不构成此处单段待确认输入锁。普通连击的 `UsingTimes` 按实际动作配置轮换；新动作可在前一实例已脱手、但尚存活时继续发生。

本项目应删除 `_pendingSkillIds`、各笔 `_predictions`、`MSkillPrediction` 的创建/归还/DeadlineRealtime 驱动、`ExpirePrediction` 常驻轮询，以及 `CanStart` 中单段 `HasPendingPrediction` 限制。保留实际冷却、MP、已学等级和动作打断规则校验；CD 结束且动作规则允许时，可以再次施放，不等服务器回包。若保留 `BeginPrediction` 作为现有输入 API，只表示“本次立即启动本地预表现与 CD”，不暗含确认锁。

RO 本身存在预表现 2 秒、正式实例 10 秒的存活宽限，不能将这两个数说成自创机制。它们应放在 `MSkill.Update` 的实例生命周期判定，依据实际累计执行时间推进；前次组件每笔 deadline 管理应移除。主 Agent 已确定本项目采用此实例 2/10 秒兜底，并将合法制作末触发和完整声音尾段纳入内容边界，保护既有晚事件和声音契约。RO 原代码的阈值只有 `_data.Time`，没有本地独立制作范围和完整音尾计算；这部分必须称为本地表现契约适配，不能声称逐字相同。

本项目自然回收也不再要求 `HasServerEnded`：动作已经退槽、表现已经完成时，即使没有服务器回包也允许 `Cease` 归池。尚有未来计时器、冷模型 GO 请求、合法完整声音尾段则继续存活，最后由累计时钟 2/10 秒兜底清理异常。**本地退槽并完整表现后自行回收**是用户新要求下的本地适配；参考 RO 本身由服务器 End 或 Update 的最大存活阈值终止，不能描述为 RO 原代码已有相同自然回收条件。服务器自然 End 可释放动作槽，但无需成为实例归池前置条件。动作窗、完整动画窗和制作窗仍保持各自语义。

UUID 仍有实际用途：匹配本地预表现与服务器广播，避免重复播放；按原 UUID 找到并取消被拒绝的旧实例；限制旧结果影响新动作。删除 pending 不等于删除 UUID，也不需要新建一套永久回包队列。

最新单技能结果应可在表现实例已结束后应用权威 CD；全局最新结果才能更新当前 MP 和公共组快照。旧段结果仍可确认或结束自己的存活对象，不得覆盖当前新段的 CD、MP 或 Core 动作段号。重复最新结果应只处理一次，不能再次刷新倒计时。对本地主角的开始广播，有同 UUID 的存活实例时只确认；已终止的旧 UUID 不能走远端新表现路径重新 Fire，截断新动作。上述都是现有 UUID 顺序及去重用途，不是额外“等待确认”。

本轮只读分析没有编译或运行测试；开发与独立审核须重新验证不等待回包可再次释放、普通 CD 结束后续放、Combo 旧段/新段错序、最新迟到结果、重复广播/回包、实例 2/10 生命周期归池、真正打断、自然结束晚事件/冷 GO/完整声尾、池复用与实体位移末帧。历史 253、189 项不计为本轮结果。

### 去除确认等待：最终落地

实际四源为 MSkillComponent、MSkillCore、MSkill、MArtSkill，无新增或删除业务类型。既有 BeginPrediction/ApplyCastResult 保留调用接口，前者只立即启动本地 CD/表现；Core.Reload 为 RO 同名实际清理动作，不增加薄包装或每笔确认票据。Cease 以实体当前安装核心和原 UUID 校验，保护等级升级及新施放；权威结果已应用则 Reload 不清权威 CD。新施放重置去重标记，UUID 回绕后的新首次结果仍可接纳。

MSkill.Update 不要求 HasServerEnded；动作退槽且制作表现结束后回收，累计技能时钟负责 RO 2/10 秒异常上限，暂停不积分。MArtSkill.Update 先回收当帧已结束声源再判断存活，FireEvents/MTimerMgr/OnProcessTimer 职责保持。MSkillPrediction 旧定义源码/meta/索引仍保持并参与编译，但运行链没有引用或创建。

独立本轮275断言通过（92生命周期+63运动+34实体+49新语义CD+16Cease/重入+21无确认），273真实客户端源零错/37旧；51生产运行输入含真实SkillCtrl/protobuf输入/结果链。主605输入SHA一致，2801保护仅四授权旧源修改，生成工程真实before全字节保持。完整验收、证据和现场边界见 `项目修改日志/2026-10-11-RO-SKILL-NO-CONFIRM-WAIT-CHANGELOG.md`。真实网络、Unity画面/听感、墙体和Animator精确末帧仍未验。
# 参考移植简洁约束与更新分工（2026-10-11，最新）

用户要求主要照 RO 原代码实现，删除原引用/代次快照包装。本节覆盖下文历史“常驻统一更新当前与列表”和 MSkillUpdateEntry 快照设计；历史验收保留，不作为本轮结果。

真实 RO 的 MSkillComponent.Update(float) 仅倒序推进 `_skills`，直接取原技能实例，Update 返回 false 则 Cease(true)。当前 `_skill` 由动作 OnUpdate 调用 Update/Cease/IsEnd；OnSkillAutoEnd 把仍 Casting 的当前技能移交列表。RO 的独立技能施放分支也直接加入 `_skills`，因此不能将其笼统称为脱手列表。

本地遵守相同分工：常驻 Update 直接倒序列表，当前 OnUpdate 推进主动作；保持原退出移交及 Entity 先 Skill 后 Machine 的调度，避免同一帧在两处推进。删除 MSkillUpdateEntry 源/meta、生成工程 Compile 与仅供它使用的 MSkill.FireGeneration 属性；真实计时器和异步资源的 `_fireGeneration` 不属该循环包装，本次不扩大修改。

AGENTS.md 与 AIconfig/workflow.md 已明确优先参考结构/接口/职责/时序，仅作工程必要适配；用户提出后才扩功能，不以人造极端场景增加生产包装。未把本地列表改造成全技能统一管理层，也不改冷却时钟或新增参考项目未要求的功能。实际范围及验收见 `项目修改日志/2026-10-11-RO-SKILL-UPDATE-SIMPLE-CHANGELOG.md`。

本轮独立274断言/272真实客户端源零错、37旧，主600输入SHA与两删除before核验通过；两报告/final-audit PASS。275−3项包装人造测试＋2项普通列表自删除回归＝274；当前动作与列表调度、Q/尾段/计时/声音/实体运动和无确认回包链重新验证。工程BOM审计返工后仅删包装Compile，其余字节保持；本轮证据 `_temp/ro-skill-update-simple-20261011/`，真实Unity/网络现场仍未验。

## CD 与表现实例回收独立（2026-10-11）

用户报告全部技能可无限放。独立修改前完整SkillCtrl/protobuf请求链已复现：十秒CD、半秒表现，无回包，0.61秒正常回池后CD=0，第二次输入成功。原Cease的IsPredicted分支调用Core.Reload清唯一单CD，把RO预表现失败清理与本地正常提前收尾混用；原测试验证回池和迟到结果，却缺少“回池后合法CD仍保留”的断言。

必要最小兼容修正仅两源：MSkill.Cease删除查当前Core并Reload的代码，MSkillCore删除唯一无其他调用Reload方法；角色CD仍由施放、倒计时、权威结果和明确清场维护，正常收尾/归池/中断资源清理不改CD。没有新增字段/确认等待/快照或管理层，当前动作/列表及计时事件结构保持。

当前实际配置由xlsx和生成TbSkillEffectInfo/ByteBuf只读.bytes核对：10010/10040/10050/10060/10070为0.5/8/10/9/13秒，组全0；不是全表零CD，当前无零CD行。0.5秒短CD在长动作结束前自然到期属于合法。工厂CooldownMs×0.001f转秒和Core/组件查询正常，不改任何作者或运行配置。

同一保CD验收对冻结before实际失败，末稿0.61秒剩9.39秒，连续20次按键不发包/创建新实例，十秒到期可再次输入。当前298运行＝274受影响回归重跑+9真实输入早回池CD专项+15五技能配置专项，272真实客户端源零错/37旧。最终保护/输入一致性及现场边界见 `项目修改日志/2026-10-11-SKILL-CD-EARLY-RESET-CHANGELOG.md`，证据 `_temp/skill-cd-early-reset-20261011/`；真实Unity/网络现场仍未验。

最终独立两报告/审计PASS，主608输入SHA一致（含10实际配置读取），2799保护仅两源码修改/2797保持、无增删越界/工程完整保持；当前无已复现剩余阻断。原表与发布资源未变，不新增确认层，以上现场边界继续有效。

