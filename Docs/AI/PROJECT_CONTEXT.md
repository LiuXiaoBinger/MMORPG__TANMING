# 项目协作上下文

## 技能 CD 与表现实例回收独立（2026-10-11，最新修正）

- 本条覆盖下文历史Cease调用Core.Reload清CD方案。独立已复现十秒CD/半秒表现：无回包正常收尾后0.61秒CD被清0、第二次输入发包。两源最小删除Cease→Reload联动及唯一无其他调用Reload入口；表现结束/归池不清角色CD，施放/倒计时/权威同步和明确清场维护原CD。
- 不等服务器确认不等于允许跳过CD；保原简单RO动作OnUpdate/列表倒序Update、MTimerMgr及自然声尾/位移职责，不增加字段、确认门禁、快照或管理层。
- 当前正式效果xlsx与真实生成读取器解析.bytes同值：10010/10040/10050/10060/10070分别0.5/8/10/9/13秒，组全0；当前表无零CD行，零CD仅接口合法条件。日志 `项目修改日志/2026-10-11-SKILL-CD-EARLY-RESET-CHANGELOG.md`，分析 `项目分析/RO_SKILL_CD_LIFETIME.md`，本轮终验由TEST_REPORT记录。
- 独立298运行/272源零错37旧、两报告审计PASS，主608输入SHA一致；2799保护仅两源码修改/2797保持。正常回池0.61秒仍9.39秒CD，20次输入被阻断、到期释放通过，现场Unity/网络仍未验。

## 参考移植约束与 RO 更新精简（2026-10-11，最新）

- AGENTS.md 与 AIconfig/workflow.md 已加入用户要求：参考指定项目优先沿原代码结构/接口/职责/时序实现，仅作工程必要接入；不自加需求外功能、管理层、快照/代次包装等防御机制，扩展由用户提出后再做，审核不为人造场景扩生产流程。
- 此条覆盖历史技能常驻统一更新/池化快照方案：按 RO 当前动作 OnUpdate Update/Cease/IsEnd，常驻 Update 只倒序遍历存活列表 Update 为 false 就 Cease(true)；删除 MSkillUpdateEntry 源/meta/工程索引与快照专用属性，异步计时的原施法代次保持。
- 列表不能统称“脱手”：参考 RO 还用于独立运行技能；本地原动作退出移交时序保持，没有另造全技能统一管理层。日志 `项目修改日志/2026-10-11-RO-SKILL-UPDATE-SIMPLE-CHANGELOG.md`，分析 `项目分析/RO_SKILL_CD_LIFETIME.md`，本轮结果由 TEST_REPORT 记录。
- 终验独立274运行、272源零错/37旧，主600输入SHA与两删除before一致；2801保护仅三修改/两授权删除。工程修复BOM后精确只删包装Compile的89字节。旧快照人造重入测试移除，正常列表自删除/动作一次推进和既有真实业务回归通过，现场Unity/网络边界仍未验。

## RO 技能去除额外确认等待（2026-10-11，最新）

- 本条覆盖历史逐笔预测确认超时和必须服务器 End 才回收的方案。删除组件 `_pendingSkillIds`、Core `_predictions`/Deadline/Expire/Cancel 与 HasPending 门禁；本地立即表现/CD，动作与 CD 允许后可续放，无回包不阻断。
- Core 只保最近施法及其已应用结果 UUID，供迟到权威 CD/MP 校正和重复结果去重；没有按帧确认事务。RO 同名 Reload 由 Cease 查当前安装核心/原 UUID 调用，旧等级旧实例不能清新或已权威 CD；已回池本地 Start 不重播。
- Update 按累计技能时间和 RO 预表现 2 秒/正式 10 秒上限；动作退槽且表现完成可自行回收，不需确认/End；制作/完整音尾范围与此自然回收是本地适配，未来 Timer/冷 GO/完整声尾保持，FireEvents/MTimerMgr/OnProcessTimer职责不变。
- 四既有源，独立 275 运行、273 完整客户端源零错/37旧、主605项SHA全一致；2801保护2797保持/四授权改动，真实工程before完整字节保持。分析 `项目分析/RO_SKILL_CD_LIFETIME.md`，日志 `项目修改日志/2026-10-11-RO-SKILL-NO-CONFIRM-WAIT-CHANGELOG.md`。真实网络/Unity画面与听感仍未验，不自动发布资产。

## RO 技能冷却与脱手管理（2026-10-11，已完成）

- MSkillComponent.Update(float)常驻统一推进当前及脱手实例，int毫秒入口桥接一次；动作OnUpdate仅IsEnd退出。MSkill bool Update返回生命周期存活，Cease默认/true真结束→组件OnSkillEnd解除→工厂Reset/归池；自然End仍原ApplyServerEnd(false)保晚事件/冷GO/完整声尾。
- SkillCDGroupInfo承载两类同名列表/字典，普通键技能ID、组键组号，CD/CurrentCD秒。Get/Set/Clear与Factory.singleton借还接预测/权威/回滚/CanStart/旧UI；已安装Core读取组件唯一CD源，不缩放realtime递减，技能推进仍实体fDeltaT。原引用代次池快照防虚Update集合重入/池重借。
- 独立253当前运行、273真实源编译零错/37旧，主613项SHA一致；五旧业务源＋两新类型/meta＋生成工程共10路径。2796保护仅5授权旧源变化/4新增，生成工程无before字节限制见日志。F13/end111/F49及资源保持；现场Unity/听感/网络未验。分析项目分析/RO_SKILL_CD_LIFETIME.md，记录项目修改日志/2026-10-11-RO-SKILL-CD-LIFETIME-CHANGELOG.md。

## 位移组件实体挂接纠正（2026-10-11，已完成）

- 后续精简已删除MEntity.OnBeforeInitialize的AppendExtraComponents空扩展调用；当前全客户端无附加登记，位移直接CreateComponent后由基类初始化普通容器。独立本轮271源编译零错、189回归重新执行通过，318项SHA一致；记录项目修改日志/2026-10-11-ENTITY-EXPLICIT-COMPONENTS-CHANGELOG.md。
- 用户明确RO位移组件挂在实体，由MComponentMgr.singleton.CreateComponent创建；先前MMove构造持有偏离结构，已纠正为普通EntityComponentBase组件，实体_displacementComp管理快捷引用，组件容器统一生命周期/池回收，直接卸载立即停止。
- 普通容器运动优先阶段在原专用Move/Buff/Skill/Machine之前仅推进位移，后续普通阶段跳过位移，移动组件不手动推进；保同帧唯一提交及末帧Move先于Skill离槽。具体分析项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md，日志项目修改日志/2026-10-11-DISPLACEMENT-ENTITY-COMPONENT-CHANGELOG.md。
- 独立189运行断言通过（63运动、92生命周期重跑、34实体组件），271客户端与65共享源码编译零错；主734项SHA零差异。共享追加Displacement=32，旧0–31及类型身份保持，Common/Net两DLL同步同次产物。下方155/64是历史证据，本轮未新跑原生导出，未验真实Unity画面/墙体/Animator精确末帧与听感。现3007001为F13/end111/F49，不自动重导或回退资产。

## 粒子制作区间导出兼容（2026-10-11）

- SkillEditorAssetTools.ReadFx不再要求作者endKeyNumber为0；原预览每事件开始帧单次生成，结束字段为制作标记，不截断/重复，ReadTimelineDuration和ActionTiming.ContentEnd仍只读取特效/声音开始帧。原负帧/显式退出校验保留。
- 未接入的混音、倍率、自定义寿命和声音变更逐项报绑定/fxList/字段和值，不再笼统把合法区间报错。仅ReadFx及summary改，现特效范围UI、RO正式运行/运动链保持。
- 用户当前3007001落剑已F13/end111、末声音F49，以此新基线保护，历史F45/F60不得恢复。64原生与8宿主源零错/2旧、2788保护仅1授权修改；实际隔离导出FX=13/30秒、TimelineDuration=49/30秒，正式Runtime未自动发布。
- 使用项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，归档项目修改日志/2026-10-11-SKILL-FX-EXPORT-CHANGELOG.md及TEST_REPORT；证据_temp/skill-fx-export-20261011，正式GUI点击未验。

## 特效播放范围与独立预览（2026-10-11）

- 上轮时间文字现扩为四行+独立下沿范围条：白触发/灰预计延迟/淡绿预计发射及尾粒/橙估算末端。Structure按文字和参数范围并集分行并扩纯显示滚动尾；Timeline保原事件marker/拖动命中；SerializedInspector沿原Seek/Sample提供三个暂停定位。仅这三源修改，正式预览/RO运行/运动链不变。
- 常量与同值TwoConstants能推预计首发，随机/曲线/距离条件显示未知；末帧是预览保守参数估算，非首末像素。资源身份缓存沿旧projectChanged清理，倍率/FPS即时算；不改作者END、资源或Runtime。
- 真实1作者F4/F45/F60保持，F45第二实例立即独立生成，F46双计时推进；粒子计数蓄力末F49/落剑首F53解释视觉先后，不能称排队或画面重叠。43原生、36编辑器源0错/4旧、602保护仅3授权变化；正式GUI/挂点画面未验。
- 使用项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，归档项目修改日志/2026-10-11-SKILL-FX-RANGE-CHANGELOG.md；证据_temp/skill-fx-range-overlap-20261011/review，两报告与主116项SHA重核已通过。

## RO 程序技能位移与动画根运动（2026-10-11）

- 最新运动链：MArtSkill.Start仅base.Start/FireEvents，FireEvents将Fx/Audio/ChargeList一次注册MTimerMgr，OnProcessTimer→InnerProcessTimer→charge交独立MDisplacementComponent.onStart；技能Update不扫描或消费未来事件。持续曲线差量由独立运动Update求值，MMove统一合输入、重力、动画根和程序量，一次MoveCharacter，侧撞停止。
- 原作者moveSegmentList的Program/Forward/Self/固定方向水平累计曲线导为MSkillChargeData；At/End按FPS直接秒换算、不加动画末帧+1，零时长瞬时。保Y三策略及maxDistance硬上限，Continue可回拉；目标、纵向、动态方向、忽略重力或碰撞等未接入非默认参数拒绝。Animation与程序窗口重叠拒绝，原Collector算法保留。
- Fire冻结朝向、全局时钟原点、Init会话及动作时长；后续Core被服务器确认切段不能改变当前指令校验。冷配置/模型只执行窗内剩余差量，过窗跳过，同模型同步换NativeObject清旧pending并重建基线。自然离槽/Interrupted停运动不拉终点，原音效/粒子自然保尾契约保持。
- 九既有源、四新增类型/meta，业务由ro_skill_motion_develop修改，ro_skill_motion_review独立复验；真实RO分析与接口见项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md，使用见项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，修改和实际验收边界见项目修改日志/2026-10-11-RO-SKILL-MOTION-CHANGELOG.md与TEST_REPORT。
- 现七原绑定中3007001作者声音F60/30=2秒而发布Runtime Timeline为1.8秒/54帧，旧导出源亦同样失败，须原绑定入口显式发布后同步；本轮不自动改写资产。其余原资产与资源保持。

## 编辑器特效两类时间显示（2026-10-11）

- 集中Window的Timeline/Structure/SerializedInspector/SoundTimeline四既有源提供触发帧秒、持续估算或custom截断、原资源名三行旁注及tooltip/选中摘要。纯显示lane和滚动尾按字体宽度计算，真实marker/拖动/作者END保持。
- 持续复用GetPreviewParticleDuration预览保守参数算法，注明估算，不代表随机/曲线/实际停播精确测量。源循环与自动寿命技能单轮播放明确区分，custom秒数不含预览收尾宽限。
- AssetDatabase原Prefab不能按activeInHierarchy判资源内活跃；以实际生成particleSystem.gameObject根内activeSelf祖先路径只读过滤，源根失活仍排除。资源身份缓存沿既有projectChanged/关闭→ClearSoundWaveforms清，事件speed/custom/FPS即时读。
- 使用项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，变更及边界项目修改日志/2026-10-11-SKILL-FX-TIME-DISPLAY-CHANGELOG.md和TEST_REPORT。

## 最新技能到期回调与模型 GO 边界（2026-10-11）

- 本节替代下文历史 PresentationReady/pending 方案。MArtSkill.Start 只 base.Start/FireEvents；protected virtual void FireEvents 一次登记 MTimerMgr，OnProcessTimer→InnerProcessTimer→fx/audio 处理具体到期指令。Update 只基类时钟与已结束声源回收，不登记、补播或绑定根。
- 删除启动模型订阅、OnModelReady、PresentationReady 事件与技能两套 pending。RO 同名 MModel.AddLoadGoCallback/RemoveLoadGoCallback 按需持有已到期 GO 操作；暖同步返回零，冷非零可取消，实际登记顺序不依赖字典槽位。真正 Reset 清等待，同模型 Load/SetGameObject 保尚未交付请求，交付核加载会话。
- RO FX 到期后申请 GO 回调；RO audio 直接调用 FMOD 模型音频接口。本项目 Unity AudioSource 需要 Transform，冷模型音效也等待 GO 是现有契约的适配，不声称与 RO 声音原样相同。
- 动画由统一 TriggerAnim 登记当前动作所有者，模型加载完成只恢复当前动画；离槽即解除。Collector→MMove 继续负责水平 Animator.deltaPosition、重力与碰撞，RO ECharge 是配置位移，不是动画根运动。
- 自然 End 保未来计时器、已到期 GO 请求与完整声尾，Interrupted 立即取消；锁定配置行、UUID/施法代次/池复用与粒子独立租约保持。完整已加载模型缺固定挂点报告并跳过该粒子，不再无限待重载或阻塞后续指令。
- 业务修改范围为 MArtSkill.cs、MSkill.cs、MModel.cs，当前用户基线与资源保护保持；最终验收见 TEST_REPORT，设计见 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`，归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CALLBACK-CHANGELOG.md`。

## 技能导出与制作尾段（2026-10-11）

- 完整源动画的自然末帧与零完整哨兵均可导出；按源采样率核对，不使用作者时间轴FPS判断裁剪。
- SkillRuntimePresentation.DurationSeconds继续旧表原动画时长；TimelineDurationSeconds独立保存制作末触发范围，零兼容旧资产。FX/Audio可晚于动画；RootMotion和动作解锁仍原窗口，纯预览波形/粒子尾迹不延长制作范围。
- ReadAudio与现有编辑预览一致：开始帧固定音量单次完整音频，结束帧是制作标记；不新增截断或循环。此条替代旧声音非单帧门禁记录，其他未接入声音参数继续校验。
- SkillCtrl透传已有Interrupted；false自然End退动作到原_skills，等待已注册/未来票据/pending/声源完成后回池；true打断/拒绝/清场立即注销。RO计时器、UUID和池代次原链保持，原宽限含完整声尾，已自然结束的实例不因旧预测到期误清。
- 正式资产通过编辑态原绑定按钮显式导出；Inspector运行态禁用按钮并说明，其他校验错误就地显示。修改归档项目修改日志/2026-10-11-SKILL-EXPORT-FIX-CHANGELOG.md，使用说明项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，验证边界见TEST_REPORT。

## 正式技能时间线入口（2026-10-11）

- MArtSkill 的 `protected virtual void FireEvents()` 一次注册本次已锁定运行资产的 FxEvents/AudioEvents，通过 `MTimerMgr.singleton.SetTimer` → `MSkill.OnProcessTimer` → `InnerProcessTimer` 实际执行，替代每帧扫描未来配置事件。
- `Scripts/Common/MTimerMgr.cs` 使用 RO 同名管理入口与整数毫秒时钟；Global 从原实体 deltaMilliseconds 唯一驱动。`SceneEntity/Skill/MSkillTimerArgs.cs` 是具名强类型载荷，携带 UUID、施法代次及具体声音/粒子事件。
- AddedTimerToken/KillTimerAll 管理本次票据，Reset 先注销且更新施法代次；冷配置按本次累计时钟补发，模型迟到只重试已经到期的 pending。Q 旧施法仍存活推进，真正取消停止，粒子独立租约收尾保持。
- 编译/回归及未验实机边界见 TEST_REPORT；完整修改归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CHANGELOG.md`。制作范围与原未接入轨道门禁保持原能力。

### 最新时间线职责纠正（2026-10-11）

- 上轮防重标记没有完成职责迁移；最新 `MArtSkill.Update` 仅推进基类时钟与回收已结束声源，注册在Start/CacheReady一次执行，MTimerMgr到期回调直接消费，模型迟到由真实MModel.FinishLoad的PresentationReady补已到期pending。Reset解除可取消订阅与票据，保留UUID/施法代次及资源身份保护，不用Update等待或重试表现。
- Global取得YooPackage后调用SkillRuntimePresentationCache.NotifyResourcePackageReady，一次启动包迟到前已持有记录；缓存暖终态同步、冷终态一次、失败空值通知，取消后旧闭包不得写复用施法。
- TriggerAnim/模型就绪登记当前动作根归属；实际Animator水平delta仍由SkillRootMotionCollector采集，经MMoveComponent合并重力走原碰撞入口，两层本轮未改。RO ECharge是配置曲线/距离事件，不能与动画根运动混同；本轮未新增完整Charge执行器。
- 最新TimelineDurationSeconds独立制作范围、自然End保未来票据/pending/声尾和Interrupted立即清理契约保持；当前94独立断言、267源零错及实机边界见TEST_REPORT。职责事实与最终实现见 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`，修改归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RESPONSIBILITY-CHANGELOG.md`。

## 项目位置

- 项目根目录：`D:/unitypro/LYFMMORGP`
- Unity 客户端：`D:/unitypro/LYFMMORGP/LYF_MMO`
- .NET 服务端：`D:/unitypro/LYFMMORGP/LYF_Server`
- Unity 版本：2023.2.20f1c1
- Unity MCP 实例：`LYF_MMO@47ea91c229a904f3`

## 客户端

- 主程序集：`LYF_MMO/Assets/GM_Game/GM_Game.asmdef`
- 启动和热更新：`LYF_MMO/Assets/Boot`
- 业务脚本：`LYF_MMO/Assets/GM_Game/Scripts`
- UI：`LYF_MMO/Assets/GM_Game/Scripts/UI`
- 网络和管理器：`LYF_MMO/Assets/GM_Game/Scripts/Common`、`Manger`
- 依赖：YooAsset、UniRx、DOTween、HybridCLR、Google.Protobuf、Network.dll
- 当前已知活动场景：`Assets/GM_Game/Scenes/New Scene.unity`

## 服务端

- 解决方案：`LYF_Server/LYF_Server.sln`
- 服务：CenterServer、LoginServer、GateServer、GameServer
- 公共库：Network、MMoRpgCommon
- 协议：`LYF_Server/ProtoBuf/proto`，生成代码位于 Network 协议目录
- 默认端口：中心 10110、登录 10120、游戏 10130、网关 10140
- 技术栈：.NET Framework 4.8、Google.Protobuf、TCP Socket、SqlSugar/MySQL、Redis、Luban

## 项目结构总览

### Unity 客户端分层

- `Assets/Boot`：启动、资源初始化和热更新入口。
- `Assets/GM_Game/Scripts/Common`：客户端通用基础设施、网络和公共数据。
- `Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`：本地玩家场景实体，直接继承 `ROObject`；本地角色业务组件与场景实体组件分属不同容器。
- `Assets/GM_Game/Scripts/SceneEntity/Entity/RoleEntity.cs`：其他角色场景实体，直接继承 `ROObject`；角色同步数据作为实体运行时字段，不另建同名业务根对象。
- `Assets/GM_Game/Scripts/FunlModule/Role/SceneWorldSystem.cs`：客户端场景实体唯一管理器；本地 `PlayerEntity`、其他 `RoleEntity` 及其他实体统一进入同一个 `SceneEntityWorld`，通过实体 ID 查询和移除。
- `Global.SceneWorld`：跨场景访问场景实体世界的进程级入口；业务模块从 `SceneWorld.LocalRole` 获取本地 `PlayerEntity`，禁止恢复 `ClientRole`、`RoleWorldSystem` 或独立业务同名实体模型。
- `Assets/GM_Game/Scripts/Manger`：客户端管理器，例如资源、提示和全局状态管理。
- `Assets/GM_Game/Scripts/controller`：界面控制器和服务器协议回包注册，例如 `MainCtrl`。
- `Assets/GM_Game/Scripts/UI/Main`：正式窗口和页面，例如背包、NPC 商城。
- `Assets/GM_Game/Scripts/UI/Tips`：提示、购买确认等弹窗。
- `Assets/GM_Game/Scripts/UI/test`：测试脚本和 Mock 入口，只能用于专用测试场景或手动临时挂载。
- `Assets/GM_Game/Prefabs`、`Scenes`、`UISprites`、`artres`：正式预制体、场景、UI 资源和美术资源。
- `Assets/GM_Game/Plugins/Net/Network.dll`：客户端使用的网络与协议程序集，必须与服务端协议生成版本匹配。

### .NET 服务端分层

- `GameServer`：游戏逻辑服；网络回调只生产 `GameCommand`，主循环消费队列并驱动模块 Tick。
- `GameServer/manger/RoleManager.cs`：在线角色、角色/会话双索引、角色协议、进入世界和附近同步。
- `GameServer/Ctrl/Game_LoginCtrl.cs`：登录、创建角色、开始游戏及 CenterServer 结果转发；不负责角色运行时逻辑。
- `GameServer/Service`：领域服务，例如 `KnapsackService`，负责背包容量等权威业务校验和状态修改。
- `GameServer/Entity`：游戏服运行时实体，例如 `OnlineRole`。
- `GameServer/Entity/Components`：角色组件接口、组件容器和各领域角色组件；新增角色组件优先放入此目录，避免与角色根实体混放。
- 背包容量按 `KnapsackType` 的 `RolePackPlain`、`RolePackEquip`、`RolePackConsume`、`RolePackMaterial` 分类型持久化；四类背包相互独立，`RoleCurrtEquipPack` 为穿戴栏，不参与扩容。
- `RolePackPlain` 是独立普通背包，只包含自身物品，不汇总装备、消耗品和材料；客户端页签按类型读取独立容器。
- `GameServer/manger/Command`：跨线程消息信封、总队列、模块路由和固定帧注册表。
- `GameServer/manger/ClientMessageManager.cs`：统一接收 Gate/Center 网络回调，在逻辑线程按来源和协议分发客户端及中心服消息。
- `GameServer/MessageHandler`：按登录、角色、背包等领域存放一个协议一个处理器；处理器文件名只使用业务协议名，不包含 Gate/Center 来源；双来源协议由同一处理器根据 `GameMessageHandlerContext.Source` 分支处理。`GameMessageProtocolRegister` 统一登记网络回调与处理器，`ClientMessageManager` 只维护队列和处理器注册表。
- `CenterServer`：角色、背包等持久化数据和中心服协议处理。
- `GateServer`：客户端连接接入、会话管理和协议转发。
- `LoginServer`：账号登录和登录前置流程。
- `Network`：TCP、协议包、连接和公共网络基础设施；本项目当前不在其中实现游戏业务。
- `MMoRpgCommon`：各服务共享的基础类型和工具。
- `ProtoBuf/proto`：协议源文件；生成代码位于 `Network` 协议目录。

### 服务端消息与逻辑流程

1. GateServer 或 CenterServer 网络线程收到包，统一交给 `ClientMessageManager` 回调，只复制为不可变 `GameCommand` 并放入 `GameCommandDispatcher` 总队列。
2. GameServer 主循环每轮消费总队列，直接投递到 `ClientMessageManager` 的命令队列；客户端消息管理器在逻辑线程按协议和来源调用登录、角色及中心服处理入口。注册为 `CenterMessageRoute.ForwardToGate` 的 Center 回包必须在调用 Handler 前按原 Gate 会话原样转发；`Internal` 消息只由 GameServer 消费。
3. 累计达到 50ms 后，`GameGlobal` 按 roserver 固定顺序直接调用各业务管理器 `Update(delta)`；业务管理器不继承消息模块基类，也不承担网络队列消费。
4. 需要回包时，在逻辑线程通过命令携带的传输链路发送，禁止在网络回调线程直接修改游戏状态。

### 协作记录位置

- `PROJECT_CONTEXT.md`：长期稳定的项目结构、职责和技术约束。
- `CURRENT_TASK.md`：当前任务、验收标准、修改范围、状态和下一步交接。
- `DECISIONS.md`：已经确认的架构和设计决策，后续实现必须遵守。
- `TEST_REPORT.md`：每次构建、静态检查、运行验证和失败复现记录。
- `D:/unitypro/LYFMMORGP/项目修改日志/`：逐项项目修改的变更说明，覆盖代码、配置、协议、表格、资源、场景、工具及文档；禁止在其他目录散落项目修改日志。
- `D:/unitypro/LYFMMORGP/项目分析/`：参考项目解析、架构设计、方案说明及专题使用文档；`Docs/AI` 不再存放这些专题设计文件。
- `_temp`：构建、生成和验证产生的临时文件，未经产品经理确认不清理。
- `AIconfig`：项目 Agent 模板；`config.toml`、`workflow.md` 和 `agent/*.md` 必须按本项目实际环境维护。

### 文档分类约束

- 每次项目修改后的变更说明必须创建或追加到 `项目修改日志/`；历史根目录变更日志已统一迁入该目录。
- 项目分析、参考项目解析、架构设计和功能方案文档必须新建或维护在 `项目分析/`；历史专题设计文档已统一迁入该目录。
- `Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md` 是协作入口记录，继续固定保留在 `Docs/AI/`。
- 文档互相引用时使用从项目根目录开始的路径，例如 `项目修改日志/xxx_CHANGELOG.md`、`项目分析/xxx_DESIGN.md`。

## 协作约束

- 所有回复和代码注释使用中文。
- 新增或修改的业务代码必须为每个类型/接口、函数（含构造函数）、字段、属性、常量、事件、参数及局部变量补充具体中文注释；注释必须说明用途、数据语义、单位/范围或生命周期等实际信息，禁止只重复标识符名称。修改既有代码时，须在修改范围内补齐缺失注释。
- 新增或修改代码禁止声明命名空间（禁止使用 `namespace ...` 代码块）；类型直接使用全局命名空间，引用外部程序集时仅使用必要的 `using`。
- 每个顶级类、接口、枚举和委托必须独立放置在一个同名源文件中，禁止在同一源文件定义多个顶级类型。
- 主 Agent 只负责需求拆解、任务委派、测试审查协调、验收和协作记录；客户端、服务端业务代码必须由对应开发子 Agent 修改。
- 修改前先读取本文件、`CURRENT_TASK.md` 和相关决策。
- 保留用户现有修改，不使用回退或覆盖命令恢复工作区。
- 客户端和后端新增或修改代码时，为协议字段、状态处理、复杂计算和容易误用的逻辑添加简洁中文注释。
- 修改后必须进行对应编译或脚本校验，并把结果写入 `TEST_REPORT.md`。
- 开发、构建、验证产生的中间文件和临时文件统一放入 `D:/unitypro/LYFMMORGP/_temp`，禁止散落在根目录或客户端/服务端目录。
- 临时文件使用完成后先记录用途，再由产品经理确认清理。
- 项目代码禁止使用 `foreach`；新增或修改代码统一使用 `for` 遍历。字典等不可直接按整数索引的集合，先创建快照列表再按下标遍历；列表元素为引用类型时，修改元素对象会作用于原对象，但替换列表元素不会回写原容器。
- 测试按钮、Mock 数据、调试入口和测试逻辑只能放在测试脚本或专用测试组件中，禁止写入正式业务脚本。
- 测试组件不得挂载到正式场景或正式预制体；测试需要使用专用测试场景、测试预制体或手动临时挂载。

## 技能编辑与运行配置（2026-10-07）

- 编辑器为固定 MIT 嵌入包 `LYF_MMO/Packages/com.ethan.act-action-editor`，适配 Unity 2023.2，保留 YCombatEditor 原 `SkillEditorWindow`、`SkillConfigSO`；原参考仓库不修改。
- 编辑器源码集中于`LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor`，按Window/Timeline/Infrastructure职责分文件，保持原`Ethan.ActionEditor.Editor`程序集、meta及GUID；嵌入包保留Runtime、文档和原测试。帮助与自动化版本报告通过Runtime类型定位包。位移、碰撞、飞行物、音效、镜头、流程等制作功能全部保留，本游戏运行支持范围继续按导出链校验。源码阅读索引为`项目分析/SKILL_EDITOR_CODE_READING_GUIDE.md`。
- 技能配置根目录 `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill`：Editor/Authoring 保存七 MJS 配置与绑定、JX_Attack01 参考与绑定；Runtime 保存七正式表现；SkillCastingRulesConfig.asset 是施法权限编辑源。原 Luban 技能表与原动画/特效资源继续保留。
- 2026-10-08目录分离：Data/Skill只存配置与JSON；运行/配置类型源码位于 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill`，宿主编辑工具与 LYF.SkillAuthoring.Editor 程序集位于 `LYF_MMO/Assets/GM_Game/Editor/Skill`，独立验证代码与 LYF.SkillAuthoring.Verification.Editor 程序集位于 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/SkillVerification`。游戏服通过更新后的Compile路径共享规则源码；资产路径、GUID与程序集名称保持。
- 客户端 Resources/SkillCastingRules.json 与游戏服 Config/SkillCastingRules.json 同源导出，服务端 Build 复制部署目录。InteruptSkill 使用新技能可打断旧技能的名单方向；MoveCasting 控制客户端水平移动，技能主动起跳仍固定禁止。
- 正式动画仍由 MSkillComponent.PlayAnim → MSkill.TriggerAnim 统一驱动，不在正式角色挂 Y 播放器。2026-10-08按用户要求移除运行时 AnimationEvent 特效兼容链，所有表现（含10070蓄力/落剑）统一使用编辑导出的 FxEvents。单次施法锁定表现行ID、动画和时长；冷配置就绪后按本次已累计时钟补发，不随Q换段改读其他行，粒子独立持有运行资产到尾迹结束。
- 编辑操作与已支持字段详见 `项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md`、`项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`，验证边界见 TEST_REPORT。服务端目前没有水平移动权威结算，不能将客户端权限字段当作服务器坐标校验。

## RO 轨道数据与源码入口（2026-10-10）

- 编辑器九类新增制作轨道在集中源码 Window/SkillEditorWindow.RoTracks.cs 注册并处理操作；SkillEditorRoTrackLabels.cs 显示对应字段及原枚举的中英文字。
- 包 Runtime/Data 新增九个原 RO 数据类及九个原值枚举，Runtime/Core/MSkillTrackTiming.cs 处理原秒字段的时间投影、末尾和校验；SkillConfigSO 追加九个对应字段，原 TrackType 身份保持。
- 这些入口用于编辑保存；正式运行仍按已有单动画/单次粒子导出范围，宿主拒绝未接入的非默认内容。原七技能、Q1/1/2与3-2局部参数、原表与预览修复保护。
- 阅读入口项目分析/SKILL_EDITOR_CODE_READING_GUIDE.md，方案项目分析/RO_SKILL_TRACK_EXTENSION_DESIGN.md。


## 技能音效和普通动作声音入口（2026-10-10）

- Assets/GM_Game/Audios/Skill/MJX保存12真实RO解码波形；Data/Skill下七原技能稿/Runtime新增AudioEvents和Animation根窗口，Editor/Authoring另有六SkillConfigSO_MJS_*普通稿，Runtime/SkillRuntimeLocomotionConfig_MJS.asset由SoundComponent加载。
- Editor/Skill/SkillEditorAudioMotionSetup显式制作及ExportLocomotion；SkillEditorAudioMotionTools负责音/根导出和一致校验；集中ActionEditor的Infrastructure/SkillEditorRootMotionPreview和Window.AudioPreview负责绝对根预览及专用隐藏声音源。
- 运行MArtSkill派技能声，MModel驱动SoundComponent普通动作并绑定SkillRootMotionCollector；MMove合并水平动画增量和重力走原碰撞入口。操作和边界见项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md及YCOMBAT_EDITOR_INTEGRATION_USAGE.md。

## 多段技能声音制作定稿（2026-10-11）

- MJX原12声音保留，新增RO原样Swing_Big_b、Swing_Short_h、Swing_Big_c。现有AudioEvents多事件执行器不变；E/R独立两阶段、F后刀声音F142独立于粒子F84，Q3/F第二声音量平衡。
- Setup显式制作仅精确识别旧资源/旧帧/默认音量迁移；自定义量保留，旧资源/帧改变则保留原项并补缺默认。作者/Runtime已同步；最新声音表项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md，方案项目分析/SKILL_AUDIO_POLISH_NOTES.md，实际听感边界见TEST_REPORT。
## 角色音频分类入口（2026-10-11）

- MJX下十五音频按移动、跳跃、技能分类；技能内通用挥刀、E、R、1、备用。文件名中文用途化，原meta/GUID与波形保留，Setup采用新相对路径，所有作者/Runtime强引用继续指同音频。
- 目录、原RO名对照及每声音的技能/动作帧秒/音量查项目分析/AUDIO_ASSET_USAGE_GUIDE.md；技能共享音源只保留一份，时间以事件配置为准。旧记录平铺英文路径仅历史来源含义。
## Audios最新英文结构（2026-10-11，替代上一轮中文分类）

- 全音频根Assets/GM_Game/Audios，角色声Characters/MJS/{Movement,Jump,Skills}；Skills为Shared/E/R/SwordDrop/Legacy。三原技能特效声Shared/Skills/Effects。十八声音名用Movement_、Jump_、Skill_英文前缀，GUID/波形/导入参数及原目录身份保持，Setup同步英文路径。
- 最新目录和原来源/用途/22事件帧秒量见项目分析/AUDIO_ASSET_USAGE_GUIDE.md。旧MJX中文路径是历史迁移记录，当前英文要求优先；声音强引用不需要重制配置或改预制体。
## 技能编辑器声音独立行与拖动语义（2026-10-11）

- Sound保持独立表现轨道，原fxList仍保存粒子/声音。旧缺Sound布局有声音时仅恢复显示，不自动写布局；显式contentKind区分空Audio/Visual，Legacy推断兼容。Fx/Warning仍统一视觉行；音效不参与其高度和lane排序。
- Interaction的MovedEventEnd统一十类事件末帧平移：正数点跟随起点，0/负旧单次哨兵保持，区间保持跨度。音效仅在Sound复制粘贴，不跨入Fx菜单。
- 声音正式单次门禁仍保留；ReadAudio诊断区间资源/起末帧与修正入口。参考项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md和项目修改日志/2026-10-11-SKILL-TRACK-DRAG-FIX-CHANGELOG.md；回归和现场边界见TEST_REPORT。

## 技能编辑器暂停与声音对齐入口

- 集中ActionEditor窗口的SetPreviewPlaying切换播放／暂停，SeekPreviewFrame统一逐帧与播放头定位，SampleAnim使用时间轴固定步推进粒子／飞行物；停止清理与暂停保留分开。AudioPreview为每个声音事件维护独立隐藏AudioSource，支持定位、暂停和继续。
- Infrastructure/SkillEditorAudioWaveform读取真实PCM包络与精确主峰，Window/SkillEditorWindow.SoundTimeline绘制完整声音、分配重叠行并支持Alt选点；Inspector的AlignSoundTrigger沿原Undo链平移事件。完整波形只影响显示范围，不改事件末帧或正式技能END。
- 原用户配置和正式播放链不在本轮修改范围。使用步骤见项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，验收及现场界面／设备声音边界见TEST_REPORT；归档项目修改日志/2026-10-11-SKILL-PREVIEW-PAUSE-ALIGN-CHANGELOG.md。

## 编辑器纯预览尾段

- GetPreviewContentEndFrame保留正式事件结束门限，GetPreviewEndFrame只缓存粒子／声音尾帧并扩预览导航；动画与根运动在正式END保持终帧，END后不补发新的FX／声音，已触发实例满足收尾条件后循环。
- 手工Simulate＋Pause下IsAlive可能一直为true。编辑器有限粒子以包含延迟／发射／尾粒／各系统及事件倍率／子发射器的保守时间轴上界，加实际particleCount归零判断结束；仍有粒子时继续保留。自定义寿命与手动预览原清理语义保持，正式运行未改。
- 1技能真实后段与其它轨道核查结果见Docs/AI/TEST_REPORT.md，操作见项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md；归档项目修改日志/2026-10-11-SKILL-PREVIEW-MULTISTAGE-CHANGELOG.md。
