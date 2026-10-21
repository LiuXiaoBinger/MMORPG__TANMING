# F 翻滚滑步与原技能换键

## 最新职责：F恢复特殊技能（2026-10-12，覆盖本地移动方案）

用户最新纠正：滑步仍算技能，只需特殊判断。前次TrySlide、SlideAction和七源本地接入已定向撤销，恢复SkillCtrl原SkillCastReq及预测、MSkillComponent.CurrentSkill与服务器开始/结束同步；SceneMgr已获授权的字段拼写修复保留。

直接使用现主表字段：Type3表示滑行，CanSlideCancel表示当前技能是否可被滑步打断，1允许、0不允许。客户端CanSkill与服务器TryCast在新技能Type3时检查当前旧技能的CanSlideCancel；普通新技能沿原InteruptSkill名单。现10010/10040/10050允许取消，10060/10070/10080不允许，不擅自修改表或强断所有攻击。

服务器准入以Type3替代10080硬编码；学习等级、职业、存活、MP/CD、目标及原广播顺序保持。所有校验通过才结束旧技能并开始新技能。滑步仍沿原667ms完整动作、500ms CD、约5米根运动、MTimerMgr白残影、F最右与旧F攻击2，跳跃期间禁止技能保持。新业务逻辑只改客户端CanSkill与服务器TryCast，没有新的移动状态、组件或框架。

当前两端编译零错误、独立审查PASS；客户端63真实源24专项与真实后端63专项通过，主输入SHA一致。客户端Unity/网络/状态机为薄边界，后端无网关，旧End→结果→新Start只核实际调用顺序，未正式联机/包验收。日志`项目修改日志/2026-10-12-ROLL-SPECIAL-SKILL-CHANGELOG.md`，证据`_temp/roll-special-skill-20261012/`。下文为此前需求与验收的历史记录，本地移动实现已撤销，不代表当前运行职责。

## 历史职责：F作为本地移动动作（2026-10-12，已撤销）

用户明确选择“作为移动动作，不走施法请求”，覆盖下文历史权威技能路径。按主表Type3识别，SkillCtrl调用MMoveComponent.TrySlide；移动组件持有SlideAction，借用原MArtSkill动画、MTimerMgr残影和根窗口，不写入CurrentSkill或施法预测列表，不发送SkillCastReq，不等待服务器确认、不扣MP。

本地滑步沿用原SkillCD记录与界面查询，主表0.5秒；完整约667ms动作结束后可再次按F。使用已有一级表现配置，不依赖学习等级；普通技能仍按真实职业装配和原请求/CD流程。滑步不抢占正在播放的攻击，跳跃与滑步沿现有动作互斥。

MSkill.OwnsAnimation仅适配现有两处持有者：普通CurrentSkill与移动SlideAction。MArtSkill原动画根运动与残影据此识别本地动作，Collector本体保持；MMoveComponent消费根增量并合并重力、碰撞，滑步时普通输入水平速度为零，方向输入保留给结束后的走跑。MDefaultActionComponent在滑步期间保持动画槽和倍率，结束后沿原默认动作恢复，不新增角色状态或动作框架。

F最右、原F攻击在2、原根倍率1.458485和白残影参数不变。本轮没有修改表、动画、资产、服务器或协议；约5米依据既有完整原生动画采样，实际过渡/碰撞影响仍需正式运行验收。日志`项目修改日志/2026-10-12-ROLL-LOCAL-MOVE-CHANGELOG.md`，证据`_temp/roll-local-move-20261012/`。

本轮需求是把旧项目的 `Anim_JX_Slider_F` 接回当前角色表现，F 使用翻滚／滑步，原 F 攻击改到 2；结构沿现有 RO 技能、动画根运动链，不复活旧 RoleFSM 或增加移动管理层。

## 最新残影识别：使用Excel技能类别

2026-10-12用户要求去掉`SkillId == 10080`的残影判断。使用主表`cfg.SkillInfo.Type`，3表示滑行，1/2/4分别普攻/技能攻击/轻功；`MSkill.SkillType`是Art等运行逻辑类型，不作为主表类别。

MArtSkill.FireEvents在一次注册时通过已有LubanMgr取该主表，非空且Type==3才登记原残影，其他时序和参数保持。直接沿现读取器，不新增Core缓存属性、初始化参数或分类框架；技能ID仍用于定位配置、绑定及权威施法，改变的是残影是否启用的类别条件。验证和记录 `项目修改日志/2026-10-12-SLIDE-CATEGORY-CHANGELOG.md`，证据 `_temp/slide-category-20261012/`。

## 最新位置纠正：翻滚在整个技能栏最右端

2026-10-12用户纠正“最左”为“最右”。F翻滚移到整个技能栏最右端现有空槽，左侧Q/E/1/2按原相对顺序紧凑排列，空槽及其同级Inset底图移至左侧靠盘空位；R中央、2攻击绑定、约5米、白残影及0.5秒CD保持。仅UIRoot七个X，其余字节保持，原生预览主目检及18项SHA验收通过；首漏Inset遮挡F图已返工保留，未正式PlayMode/发布。以下最左描述是上一轮历史结果。日志 `项目修改日志/2026-10-12-F-ROLL-RIGHT-CHANGELOG.md`，证据 `_temp/f-roll-right-20261012/`。

## 最新追加：最左栏、约5米、原残影和低CD

2026-10-12用户追加并确认每次完整滑步约5米，覆盖下文首轮倍率1/约3.43米及6秒CD、无特效的初稿参数。F仍为翻滚、2仍为原10060；UIRoot只移动F/Q/E三个快捷锚点，视觉从左至右F/Q/E/1/2，不改变输入绑定或中央R。

- 作者Animation根倍率由实际原Avatar水平增量校准约1.458485，目标完整约5米，保原80/120秒窗口、空ChargeList；不改原动画导入或用程序位移叠加。实际正式Controller过渡及墙体仍可能缩短距离，原生单剪辑测量与正式现场分开记录。
- 正式主表CDTime为float秒，可直接0.5；效果CooldownMs改500，动作667毫秒/零MP/无伤害保持。CD早于动作结束，到原动作结束可再次按F，不在动作未结束时重叠施放，不增加按住自动连发/缓冲。正常Luban只同步两必要bytes。
- 已找到之前特效真实来源：`Scripts/controller/RoleController/MainRoleCtrl.cs`的旧Slider状态每帧调用`GhostEffect.CreateGhostEffectObject(Color.white,0.2f,0.2f,0.2f,0.2f)`，为白色网格姿态残影；指定独立anim仅AnimaEnd，没有这个粒子事件。因此复用原GhostEffect和Shader，以MArtSkill.FireEvents注册30Hz MTimerMgr快照，在InnerProcessTimer调用原渲染，避免开发未要求的残影编辑轨道。
- 只为10080注册残影，本地EGhost编号100不伪称RO原编号、不改变协议；MSkillTimerArgs仅新增无载荷强类型构造。冷资源注册不补历史姿态，模型未加载/动作退槽/结束不出新影，旧影按原0.2秒淡出；不在技能Update扫描或发特效。
- 原生显式原OnDisable/OnEnable复现GhostEffect两渲染器缓存被清空却非null，失活再启顶点43749→0；最小CleanUp置两缓存null重新收集后43749→43749。保原烘焙绘制算法及现有注释，未重构组件。
- 旧Shader只有打包目录收集，没有材质/预制体引用或显式加载，Shader.Find在构建中没有可靠引用。GraphicsSettings仅追加现有Character_Ghost到Always Included，不造新的加载系统。

本轮证据 `_temp/f-roll-refine-20261012/`，日志 `项目修改日志/2026-10-12-F-ROLL-REFINE-CHANGELOG.md`。开发与独立验收完成：272源编译/真实作者七项导出、63生产源34专项及17权威连续通过，主748项SHA一致。原生单Skill累计约5.00米、白残影和F最左HUD主/独立目检通过；正式PlayMode、原生墙体和联机未验。模型复用夹具仅证明缓存重收集，EditMode Destroy提示不隐去；已生成影自然消散仍沿原实现，不冒称现场完整淡出实测。以下首轮结果保留历史含义。

## 已确认资源与真实参考

- 本项目已有 `LYF_MMO/Assets/GM_Game/Animas/Role/MJS/Common/ALL/Anim_JX_Slider_F.anim`，对应原 FBX 位于上级 `Common/Anim_JX_Slider_F.FBX`。剪辑120Hz、80帧、0.6666667秒、不循环。`LYF_MMO/Assets/GM_Game/Resources/ResInfo/animInfo.txt` 同样登记0.6666667秒。
- FBX原take为 `GhostSamurai_APose_Slide_F_Root`，XZ根位移未烘焙进姿态。剪辑有 `RootT.x/y/z`；z起点0.14491099、终点2.8037145，x起点0.013246205、终点-0.0070285215。此为Humanoid原曲线坐标，不能直接宣称世界滑行2.6588米。实际距离须以角色Avatar的 `Animator.deltaPosition` 检查。
- 现成图标 `LYF_MMO/Assets/GM_Game/UISprites/Icon/Skill/Skill_330600.png` 已目检：紫色技能背景、展开双腿的人形剪影与左右移动箭头，属于位移动作，直接复用。无需从RO复制新美术。当前主表10080图标路径也指向 `Icon/Skill/Skill_330600`。
- 正式 `Luaban/MiniTemplate/Datas/#SkillInfo.xlsx` 已由后端开发读取：10080“滑行”、Type3、Action41、CD6秒、旧MP60。当前正式Effect与Presentation原来没有对应10080条目，不能只靠主表或旧文本文档认为技能已完整接入。
- `LYF_MMO/Assets/GM_Game/Scripts/Role/FSM/SliderState.cs` 是旧兼容状态：只设置Action41并在0.3秒退Idle，没有真实程序移动。`MainRoleCtrl.SkillKeyEvent` 曾有F→Slider，但该旧输入订阅已注释，正式移动由SceneEntity负责。因此不能复活该状态替代当前技能生命周期。
- RO参考 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs` 的 `FireEvents` 把Charge交给 `MTimerMgr.singleton.SetTimer`；`InnerProcessTimer/OnProcessTimer` 分发到 `charge`，后者派实体位移事件。`.../Components/Common/MDisplacementComponent.cs` 在实体上接 `onStart/onStop/Update` 并处理曲线位移及碰撞。此次没有查到可直接照搬的独立翻滚状态实现；已有通用技能链已能承载该动作。

## 最小接入与产品规则

- 保留技能ID10080，不造新技能身份；补Effect2008001、Presentation3008001及作者／绑定／Runtime三资产。以真实现成动画完整时长为动作窗，F绑定10080，原F攻击10060改2。
- 当前 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs` 专用模拟11080将10060绑定F，2原为空；只读数据库也没有其它2技能占用。后端需同时处理默认自动学习Type3及存量F非Type3改2，不能只改本机模拟表现。
- 本轮沿10080既有6秒CD，MP设0、伤害为0。用户只要求位移动作，不新增无敌、气力消耗、回复／积攒、额外冲刺次数等规则；真气和气力接口仍只是既有界面。
- 默认沿角色当前面向前滑，不新增方向缓冲或动态选目标；原技能释放门禁与跳跃不能施法继续适用。
- 只配置一条Animation根运动窗口，倍率1；ChargeList为空，避免动画根运动和程序冲锋重复推动。

## 当前运行职责

`SkillCtrl` 读取11080绑定→`MSkillComponent.CanStart` 检查动作／跳跃／CD→沿原协议和本地预测→`MArtSkill` 播放滑步动画并绑定根窗口→`SkillRootMotionCollector.OnAnimatorMove` 读取真实水平delta→`MMoveComponent` 合并重力后走 `Entity.MoveCharacter` 碰撞→按原技能结束和CD生命周期收尾。

现有Collector只采 `Skill` 状态中当前动作的真实根增量；不读取RootT来直接改transform，不接管重力，不重复使用程序Charge。不需要新增翻滚组件、计时管理器或按帧资产扫描。

## 原生动画采样证据

2026-10-12以同版本Unity 2023.2.20f1c1在全新最小隔离工程只复制正式 `SK_CALISTA_01.fbx` 及meta、正式 `Anim_JX_Slider_F.anim` 及meta。保持模型原Avatar和1.2导入比例，用 `AnimationClipPlayable` 驱动其真实Animator，按80步、每步1/120秒读取deltaPosition；以与生产Collector相同的规则丢弃Y后累计。

- 初始面向0度：水平增量 `(-0.026141, 0, 3.428114)` 米，前向为主，极小侧漂来自原动画。
- 初始面向90度：水平增量 `(3.428114, 0, 0.026141)` 米，随角色世界面向旋转，水平距离一致。
- 原生日志含 `SLIDE_NATIVE_PASS` 与明确退出码0，没有脚本编译失败或采样异常。Unity启动许可校验和退出网络中止提示保留在原日志，不当作业务错误或隐去。
- 上述约3.43米仅证明现模型Avatar的独立单剪辑全窗原生评估，不代表已实测正式施法链、实际墙体或联机移动。正式Controller过渡与窗口末帧调度可能影响实际距离，不把该数写成新的业务距离常量。
- 证据：`_temp/f-roll-slide-20261012/reference/native-slide-result.txt`、`native-slide.log`、`reference-manifest.json`；采样脚本和最小工程同目录保留。正式资源没有因采样被修改。

## 验收与边界

需核正式表与两端bytes一致、10080零耗蓝无伤害、F与2唯一绑定、10080异步表现引用真实动画和现成图标、仅一个根窗口、跳跃门禁保持、CD不会随动画结束清零。原生采样证据放 `_temp/f-roll-slide-20261012/reference/`，正式玩法／联机操作另与专项回归区分。

对应修改日志：`项目修改日志/2026-10-12-F-ROLL-SLIDE-CHANGELOG.md`，其中列明16个正式代码/配置/资产路径和协作记录。

## 实际实现与验证

- 作者/绑定/Runtime3008001已接入原完整剪辑，真实Unity Validate→Export→ValidatePublished七项通过，正式Runtime与导出字节一致。唯一宿主编辑器适配为FindMjsClip允许这条精确动画路径，其他ALL目录动画没有开放。
- 三源表与真实Luban读取器逐字段一致，正常Luban生成三共享bytes同步；不新增客户端独立数据副本、schema或DLL。中心服仅规范已有10080为F/至少1级、10060旧F为2而保等级；建角原Type3一级/F链保持。游戏服只新增已接入10080/Type3准入，其他权限沿旧检查。活数据库前后只读查询相同。
- 客户端272真实源Rebuild零错/37旧警告，两服务Rebuild零错。权威施放19项、真实ORM11080只读回包7项通过；既有学习/职业/死亡拒绝和6秒CD保持。
- 独立链接59份实际生产源及真实Luban数据，24项专项通过：11080与SkillCtrl输入/视图同绑定、F请求10080/2请求10060、冷Runtime及根窗口恢复、Collector去Y后MMove唯一提交、根量仅消费一次、退槽停止移动、667ms表现结束保CD、起跳及下降拒绝/落地恢复、6秒CD到期续放。
- 专项Animator增量/CharacterController墙体/网络使用明确测试边界，证明生产调用和状态处理，不代替原生物理现场。原生单动画评估、真实作者导出和HUD闲/CD四图各自提供对应证据，未验正式PlayMode联机或发布Bundle。
- HUD预制体/场景与旧技能配置保持；F现成位移图标、2旧攻击图标，真气气力双满/怒气暗。重新运行或登录后从新11080和配置重建技能；安装包运行仍按已有热更/资源发布流程同步，当前没有自动发布。
