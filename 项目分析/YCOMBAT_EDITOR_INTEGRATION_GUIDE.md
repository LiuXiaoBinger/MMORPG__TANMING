# YCombatEditor 接入实现与使用

## 文件与运行边界

原 Y 仓库 `D:/unitypro/JNBJ/YCombatEditor` 保留。游戏使用固定嵌入包 `LYF_MMO/Packages/com.ethan.act-action-editor`，保留 MIT、`SkillEditorWindow`、`SkillConfigSO` 等原名称；Unity 声明适配为 2023.2。包内既有类型通过独立程序集隔离，宿主编辑桥接用 `SerializedObject` 读取第三方嵌套类型，避免两个 `Global` 混淆。

正式角色继续 `MSkillComponent.PlayAnim → MSkill.TriggerAnim`。不在角色预制体添加 Y 的 ActionPlayer、ActionClipAnimator 或 RootMotionCollector。原技能表、动画、RoleAtkConfig、施法 UUID、对象池、当前与旧技能实例列表继续保留。

所有技能配置位于 `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill`：

| 位置 | 用途 |
| --- | --- |
| `Editor/Authoring/SkillConfigSO_表现ID.asset` | 七条 MJS 表现的 Y 编辑配置 |
| `Editor/Authoring/SkillEditorBinding_表现ID.asset` | 完整旧表身份、原剪辑、预览模型和旧事件来源绑定 |
| `Editor/Authoring/JX_Attack01_Reference.asset` | 原 JX 攻击动画和 RoleAtkConfig 的独立编辑参考 |
| `Editor/Authoring/JX_Attack01_ReferenceBinding.asset` | JX 参考的模型、动画和原事件绑定，PresentationId=0 |
| `Runtime/SkillRuntimePresentation_表现ID.asset` | 七份正式运行表现，只保留宿主可执行数据 |
| `Editor/Authoring/SkillConfigSO_MJS_动作.asset` | 走路、跑步、战斗走跑、起跳与落地六份声音作者稿，skillID=0 |
| `Runtime/SkillRuntimeLocomotionConfig_MJS.asset` | 六种普通动作的动画路径、完整时长与单次声音事件 |
| `SkillCastingRulesConfig.asset` | 五技能的施法权限唯一编辑源 |
| `Resources/SkillCastingRules.json` | 客户端启动前同步读取的规则 |
| `LYF_Server/GameServer/Config/SkillCastingRules.json` | 与客户端同源导出的游戏服规则，Build复制到部署目录 |

JX 参考不替换 MJS 的正式 Q 动画。MJS Q1 原剪辑没有 PlayEffect；2026-10-08按用户要求为其编辑/运行轨道补入已有JX参考刀光，使用该参考F48/120FPS（0.4秒）的粒子、EffectPos挂点和变换。来源字段不冒充MJS原动画事件。JX参考从原FBX实际导入事件（0.40227696秒）建立轨道，不把FBX.meta归一化时间当秒。根据用户后续纠正，Q首两段各一条，第三段为两阶段普攻刀光：F23与F54（120FPS约0.191667/0.45秒）均使用Effect_Attack01和EffectPos局部挂点。第三段第二刀光按用户指定局部位置(0.92,0.36,0.07)米、欧拉角(298.69,254.45,98.72)度配置，作者与运行同步。

技能源码与配置分开保存，源码目录如下（均从项目根目录开始）：

| 源码位置 | 用途 |
| --- | --- |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill` | 原技能链、运行表现类型、挂点解析和共享施法规则 |
| `LYF_MMO/Assets/GM_Game/Editor/Skill` | 宿主绑定类型、Inspector、技能资产生成和导出工具；保留 LYF.SkillAuthoring.Editor 程序集 |
| `LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor` | 集中的编辑器源码，按Window/Timeline/Infrastructure职责分文件，保留Ethan.ActionEditor.Editor程序集及原meta；全部原轨道制作功能保留 |
| `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/SkillVerification` | 只读资源诊断和构建验证；保留独立 LYF.SkillAuthoring.Verification.Editor 程序集 |

`Data/Skill` 中的配置路径、脚本 GUID 引用及数据保持原样。游戏服通过原共享源码编译引用使用规则类型，路径随源码迁移同步更新。

## 编辑现有技能

在 Project 选择对应 `SkillEditorBinding_*.asset`，点击 Inspector 的“用 SkillEditorWindow 编辑”，会载入对应真实配置、动画和预览角色。JX 使用 `JX_Attack01_ReferenceBinding.asset`。保留 Y 窗口时间轴及保存接口，编辑后保存 Y 资产，再点击绑定 Inspector 的“校验并导出当前表现”，或菜单 `Tools/技能/YCombat/校验并导出全部表现`。单纯保存编辑配置不会更新正式运行数据。

菜单 `Tools/技能/YCombat/生成缺失技能资产并导出` 创建缺失配置，保留已存在的编辑内容和资产 GUID，并导出运行表现与规则。`Tools/技能/YCombat/验证已保存资产与依赖` 用于检查已保存数据。正式资产由 Unity AssetDatabase 创建，不手写 YAML 或 GUID。

Q 的 3001001/3001002/3001003 三份资产仍是 UsingTimes 的三段普通攻击变体，每段动作状态窗700毫秒；E/R/F/1 对应3004001/3005001/3006001/3007001。状态窗与 MP/CD 不由 Y 的 exitFrame、skillCD 或能量字段覆盖。

## 时间与粒子定位

生成器使用 `AnimationUtility.GetAnimationEvents` 读取原事件秒数，以各剪辑实际 `frameRate` 设置 Y 时间轴 FPS，四舍五入成整数帧。运行时使用 `帧/FPS` 秒。绑定的 FxSources 保留原秒数与来源序号，量化误差不超过半个该时间轴帧；不能把时间轴帧率和角色渲染帧率混为一谈。

| 定位模式 | 实际行为 |
| --- | --- |
| ActorRoot | 跟随当前角色模型根节点 |
| NamedSocket | 跟随完整 Transform 挂点路径 |
| TargetTop | 目标位置加模型高度；无目标时采用配置前方距离 |
| CasterForwardWorld | 角色前方固定距离的世界落点，保留原 skill03_2 的6米与角色朝向 |
| WorldAbsolute | 固定世界坐标 |

原 Jx_Skill02 右手路径属于 JX 骨架。`SkillFxSocketResolver.Resolve` 先查精确路径，只有这条已确认旧右手路径可用当前 Avatar 的 RightHand 映射。MJS 的真实节点为 `root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_r/upperarm_r/lowerarm_r/hand_r`，不是 IK 手节点。未知、缺失或有歧义的路径拒绝，不猜测首项；旧配置文件保留。

2026-10-08删除运行时旧事件链后，10070 与其他表现统一导出 `FxEvents`。当前30FPS轨道在第4帧蓄力（约0.133333秒）、第9帧落剑（0.3秒），挂点与目标头顶定位保留；无目标落剑为角色前方10单位。编辑FX按统一校验发布，不再要求保持旧事件或写入兼容标志。原动画和来源参考保留，没有引入敌人锁定、伤害或飞行弹体。

## 施法特殊规则

选择 `SkillCastingRulesConfig.asset` 编辑规则，点击 Inspector 导出，或菜单 `Tools/技能/YCombat/创建并导出施法规则`。修改在重新进入游戏和重启服务器后生效；规则同步加载，不随表现资源晚到而切换。

- `InteruptSkill` 保留 RO 拼写和方向：填写在“新技能”的规则行，列表中是该新技能可以打断的“当前技能”ID。例如在10040行填写10010，表示 E 可打断 Q。反向不会自动成立。
- `MoveCasting` 表示当前技能施法时允许普通水平移动。默认false，禁止移动技能开始时清除旧速度，逐帧输入和积分同样拦截。
- 技能中主动起跳固定禁止，即便 MoveCasting=true；已有空中重力和落地继续。旧 CanJumpCancel/CanSlideCancel 字段没有自动变成权限；本次未新增跳取消协议。

首次配置五行都为 InteruptSkill空、MoveCasting=false，保持此前已生效行为。客户端 CanSkill 与技能事件自转移读取同一名单。游戏服 `RoleSkillComponent.TryCast` 在学习、职业、目标、CD、MP 全部通过后才结束旧 UUID，再提交新技能；非法新请求不会取消旧技能。服务端目前没有水平移动权威实现，MoveCasting 的实际输入/坐标门控位于客户端，不能称为服务器移动结算验证。

## 可发布数据与拒绝边界

宿主当前接入单段完整原剪辑、上述定位的单次粒子、单帧单次固定音量声音及 Animation 模式水平根位移窗口。导出器核对旧表现完整身份、真实剪辑、时长、状态窗、字段默认值、粒子变换、声音依赖及位移窗口；隐藏轨道也受检查，未知包字段拒绝。

动画裁剪、混合、多段动画、区间或随机声音、攻击判定、Y动作跳转/Flow、取消轨道、Program 位移曲线、投射物、摄像机、霸体和其他未接入轨道只要有非默认数据就阻止导出。Animation 模式沿原 Y 忽略程序方向、曲线、目标与 maxDistance 等字段，并保留作者原值；主动忽略重力/角色碰撞不支持且拒绝。Y的 jumpList 是动作跳转，不能当成角色物理起跳。新增九类 RO 轨道继续为制作保存范围，非默认内容仍阻止正式发布。

## RO 声音与动画位移

真实声音来自 RO `clientproj/Assets/artres/_FMod/Mobile/Action.bank` 内 FSB5 样本，使用 RO 原 FMOD 库解码为24kHz/16bit WAV；未使用 Unity Timeline 示例声音。十二份声音保存于 `LYF_MMO/Assets/GM_Game/Audios/Characters/MJS`，新音频 GUID 由 Unity 导入生成。当前选用样本是本游戏动作的初始适配，不能将同一 bank 中的样本名冒称为 RO 原技能全部事件组合。

`SkillEditorAudioMotionSetup.PrepareAndExport` 通过 Unity API 追加声音并发布，保留原粒子和已有位移段。Q1/F48、Q2/F23、Q3/F23与F54、F/F37与F84对应原挥刀时点；E/R按首粒子时点播放原骑士技能样本，1技能按其30FPS的F4蓄力、F9落剑播放。音效通过 `SkillEditorAudioMotionTools.ReadAudio` 转为 `AudioEvents`，正式 `MArtSkill.Update` 按统一施法秒时钟触发，实例由技能生命周期停止回收。

Q1原F0–15 Animation 窗口保留。Y窗口含末帧，导出为半开秒区间，120FPS时结束为16/120≈0.133333秒。Q2/Q3/E/F无位移段时首次制作全动画倍率一；R与1没有新添根位移。正式根位移取原 Animator 的水平增量，仍经过实体移动碰撞且不接管Y轴重力。编辑器则从固定初始根姿态按真实RootT/人体缩放重建水平累计位置，不能把这种曲线预览当作原生Animator混合、碰撞或完整Game View验收。

六份普通动作稿沿用原MJS动画、没有技能表ID。走/跑及战斗走/跑左右脚初始步点取原 `LeftFootT.y/RightFootT.y` 高度最低帧；起跳与落地声音在F0。`ExportLocomotion` 发布到同一运行配置，由已有移动/跳跃状态读取；作者编辑后须独立执行普通动作发布菜单。制作入口先检查目标脏状态，存在未保存修改时停止。

## 加载与生命周期实现

`MSkillFactory` 按旧表现行构造 `MSkillData`，后者持有 `SkillRuntimePresentationCache` 的七表现预取引用。缓存加载完成只写自己的记录，不写会回池的核心/技能。`MSkill.Fire` 锁定本段表现ID、动画与原表时长；`MArtSkill`取得并校验该ID的完整轨道。冷加载期间累计施法时钟，就绪后补发已到点事件，不读取动画事件，也不随核心Q换段改变行。无效配置只记录一次并停止本次特效。

`MArtSkill.Update` 按每次施法时钟消费事件，同帧按数组顺序，越帧补发所有已越过事件；模型未就绪不会提前消费。退出动作槽的旧实例可以继续事件，但不能重新占动画槽。

粒子由 `SkillRuntimeFxLifetime` 独立持有运行资产，无源ClipInfo兼容路径。无引导技能克隆体根/子粒子单轮发射，以 IsAlive(true) 等真实尾迹结束后销毁，OnDestroy恰好释放租约。回池、拒绝、角色移除、场景退出与资源晚到不得写入下一实例。

## 验收证据

2026-10-07 实现与配置门禁通过：正式 Editor 编译、生成后强制同步重载、78文件幂等检查、17项编辑合同、服务端完整隔离构建和26项规则回归、46个实际技能相关Bundle及七运行资产加载。子Agent交叉独立终审通过。JX参考已用真实SkillEditorWindow打开，配置/模型/Animator载入且正式场景脏状态不变。

资源验收采用正式YooAsset设置生成完整模拟清单，再按正式映射构建真实原生Bundle依赖子集；未进行完整YooAsset发布、Player构建或运行句柄冷加载。实际Game View、键盘、目标头顶及GPU预览画面仍待用户在已有模拟角色手测；窗口API加载成功不能替代画面验证。服务端没有水平移动权威链。

逐轮失败、返工及真实成功证据记录在 `Docs/AI/TEST_REPORT.md` 和 `项目修改日志/2026-10-07-YCOMBAT-EDITOR-INTEGRATION-CHANGELOG.md`。临时编译、诊断与构建产物统一位于 `_temp/ycombat-integration`、`_temp/ycombat-runtime-build`、`_temp/ycombat-casting-rules`，未经产品经理确认不清理。操作速查见 `项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`；历史初审方案以本文的实际实现为准。

## RO 时间轴数据扩展

新增制作数据沿 `pbl_skill.proto` 的 MSkillRotateData、MCameraShakeData、MSkillCameraFxData、MCameraPostEffectData、MSkillChangeColorData、MSkillBarData、MSkillMobUnitData、MSkillAIEventData、MSkillBuffData，各类型及原枚举独立文件位于包 Runtime/Data。SkillConfigSO 追加对应原字段，轨道枚举只追加值，不重排旧值。

MSkillTrackTiming 统一处理秒与帧的投影、制作末端和新增数据校验。SkillEditorWindow.RoTracks 接入原 TrackDef、事务、属性页及剪贴板，读取当前 configFile 的真实引用；数据保存仍由既有 SaveConfig 生命周期管理。镜头后处理保持 RO 单对象，Buff 仅保存原 At/Index；没有新增自定义技能协议。

宿主 DefaultOnlyFields 加入新字段：空数据兼容旧发布，非默认数据明确报未接入，不能静默导出。RequireEventHandlers 仍要求可执行支持；此次扩展制作能力不代表正式 MArtSkill 或 Y ActionPlayer 已执行新内容。碰撞及飞行物已有制作用途，但不宣称完整复制 ResultList/LongAttackData。

操作字段见 `项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`，设计见 `项目分析/RO_SKILL_TRACK_EXTENSION_DESIGN.md`，本轮验收以 `Docs/AI/TEST_REPORT.md` 和 `项目修改日志/2026-10-10-RO-SKILL-TRACK-EXTENSION-CHANGELOG.md` 为准。

## 2026-10-11 音频用途分类

角色声音位于MJX内的移动、跳跃和技能子目录；技能分通用挥刀、E、R、1、备用。资源使用中文用途名，例如Skill_E_Phase01_HeavySlash、Skill_E_Phase02_ShortSlash、Jump_TakeoffWhoosh、Movement_Footstep01。共享素材只保留一个文件；每个动作的具体声音帧、秒和音量查项目分析/AUDIO_ASSET_USAGE_GUIDE.md。原meta/GUID随文件迁移，编辑器引用继续使用原身份。
## 全英文音频目录（2026-10-11最新布局）

整个Audios内目录和文件名均采用英文：角色声音在Characters/MJS/Movement、Jump和Skills；Skills分Shared、E、R、SwordDrop、Legacy。文件名前缀Movement_、Jump_、Skill_直接区分类别。原三份技能特效AudioSource声音在Shared/Skills/Effects，依实际Skill04蓄力/释放、Skill02蓄力引用命名。此前MJX中文分类章节属于上一轮迁移历史，当前目录及完整18声音对照以项目分析/AUDIO_ASSET_USAGE_GUIDE.md为准。

所有原wave/meta/GUID和目录身份保留，制作脚本同步英文路径，作者/Runtime与特效预制体的强引用延续；时点、音量、特效和根位移不因改名调整。正式归档项目修改日志/2026-10-11-AUDIO-ENGLISH-REFACTOR-CHANGELOG.md。