# RO 角色技能位移、根运动与接口实现

## 2026-10-21 最新决定：只保留RO配置曲线位移（源码、配置与发布检查通过）

用户本轮明确确认“改成RO方案，移除采集器依赖”。该决定替代下文历史方案中“保留动画根运动并另接Program”的范围；历史源码事实和过去验证仍保留其当时含义，不代表当前继续保留采集器。

本次沿用现有`ChargeList→FireEvents/ECharge→charge→实体MDisplacementComponent.onStart/Update/onStop`，由`MMoveComponent`合并重力并统一碰撞。动画只播放姿态，模型明确`applyRootMotion=false`；删除运行采集器、根窗口类型、两处delta消费及制作导出支路，不重设计已有实体组件归属。

六份旧动画位移窗口需要先用真实Avatar采样得到累计水平运动，再迁移作者Program及Runtime Charge配置；旧程序字段未执行，不能当真实位移。保原窗口含末帧的时长和滑步原倍率，不能只删字段导致动作丢位移。两份原空位移表现保持空；动画、FX、Audio、CD和其它制作轨道不变。有意义横移若不能由现有前向子集表达，必须记录真实证据后做必要最小适配，不能静默截断。

Codex负责设计和最后独立验收，WorkBuddy本项目桌面任务执行；末稿三组编译0错、资产48项/轨迹174项/真实组件14断言及专用Unity原生发布58断言全部通过，正式Play仍待验。记录与逐项文件见`项目修改日志/2026-10-21-RO-CHARGE-ONLY-CHANGELOG.md`，证据`_temp/ro-charge-only-20261021`。

本轮最小适配沿原作者`MoveSegment`增加可选`curveX/distanceX`，前向继续原`curve/maxDistance`；运行复用RO已有`CurveX/DistanceX/CurveZ/DistanceZ`，不增加第二个位移驱动。固定朝向的右向为`(direction.z,0,-direction.x)`，两轴累计差量相加后仍交原移动组件一次碰撞。六份实际采样根旋转均为零；3004001有约0.896米横移，3006001前向先到约4.400617米再回到3.791251米，因此不能只保最终Z距离。

迁移的`DistanceZ`选正向峰值，曲线值为累计Z除以该尺度；使用原Continue允许回拉，不取消原前向硬上限。`DistanceX`选横向绝对包络，曲线可为负。滑步原倍率只乘进两轴距离一次，Program作者`rootMotionScale`恢复1；作者末帧按该稿自身FPS换算，E稿30FPS与其余120FPS不能混用。曲线须含真实零起点，旧根窗口含末帧的时间仍保持。第二轮曾在零起点、作者帧与发布参数检查失败，已退原开发任务修正；末稿完整采样点最大误差小于0.001毫米，作者与Runtime一致。3001002每轴实际238键，含真实零起点，不应直接以采样步数加一推断。

移除`ReadRootMotion`时，原`Validate/ValidateConfig`中的调用必须替换`ReadCharge`，因为旧接口同时执行了程序段校验；直接删调用会让错误延迟到发布写入阶段。原生Unity专用工程已复现。六份旧Animation段原不使用的Program参数必须在各自迁移段内按现有默认值归一，不放松现有校验，也不改变其它制作内容。

使用方式：六份现有作者稿的moveSegmentList已为Program，前向仍配置curve/maxDistance，可选横向配置curveX/distanceX；曲线值表示累计距离比例。沿现有技能导出入口生成Runtime.ChargeList，正常施法即通过ECharge派发，无需挂采集器。旧Animation模式会明确报错提示迁移；普通走跑跳仍由原移动组件负责。两处制作预检分别保旧表动作秒数和SourceClip.length上限，音效仍使用制作时间轴长度。

本轮显式时间轴预览、Gizmo及通用MoveSolver不在范围，旧`SkillEditorRootMotionPreview`跳过Program段；不能将本次运行配置支持两轴说成时间轴移动预览也已支持。正式Play、真实碰撞和双端同步均须与隔离源码/发布结果分别说明。

参考根：`F:/UnityPro/ro/Ro_cs422nb`。以下源码事实由只读参考 Agent 核对，所有引用行号是分析时真实源行号；后文记录本项目实际适配与验收，参考 RO 保持只读。

## 结论

RO 角色技能位置推进主要来自配置 ChargeList 的程序曲线，不来自动画 Animator.deltaPosition。技能只在时间点注册、派发位移事件，持续插值及碰撞由独立 MDisplacementComponent 完成。动画播放、程序位移是并行职责；本项目已经使用动画根运动，必须保留该真实能力并另接 RO 的程序位移链，不能将两个来源改名混为一谈。

## RO 可追溯主链

| 层 | 真实文件及行 | 函数接口 | 语义 |
| --- | --- | --- | --- |
| 启动 | clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:1019 | protected override void Start() | base.Start 后 FireEvents；不在 Update 重复注册 |
| 时间线 | 同文件:264 | protected virtual void FireEvents() | ChargeList 各 At 秒乘 _timeScale，SetTimer 登记 ECharge；AddedTimerToken 保存票据 |
| 管理器 | clientcode/CSProject/MoonClient/Utility/MTimer.cs:217 | public uint SetTimer(float interval, TimerEventHandler handler, params object[] args) | RO 原参数是秒、弱类型数组；本项目不得照搬弱类型数组 |
| 回调 | clientcode/CSProject/MoonClient/Skill/MSkill.cs:225 | protected void OnProcessTimer(uint token, object[] args) | 移除 token、核施法实体，再 InnerProcessTimer；Firer.IsLoaded 早退被明确注释掉 |
| 派发 | MArtSkill.cs:80 | protected override bool InnerProcessTimer(int id, object param) | ECharge 分支调用 charge(param)，与 FX/Audio 并列 |
| 指令 | MArtSkill.cs:591 | private void charge(object o) | 普通角色从 MSkillChargeData 填 MDisplacementEventArgs 后发 MEvent_Displacement |
| 生命周期 | MArtSkill.cs:469 | Stop 中发送 MEvent_DisplacementStop | 参数 skillUuid=当前技能，stop=cleanup；旧 UUID 不得停新位移 |
| 开始 | clientcode/CSProject/MoonClient/Components/Common/MDisplacementComponent.cs:115 | private void onStart(IEventArg e) | 除 Solo/未加载外，加载曲线资源；记录距离、起点、持续秒数、角度和 UUID |
| 更新 | 同文件:259 | public override void Update(float fDeltaT) | p=min(duration/totalTime,1)，计算曲线累计坐标，旋转到世界水平面，经踢墙/地面高度/空气墙后提交实体 Position |
| 停止 | 同文件:180 | private void onStop(IEventArg e) | 核 UUID；stop=false 强制终点收敛，true 留当前位置；StopDisplacement 仅关闭运行标记 |
| 清理 | 同文件:62 | public override void OnDetachFromHost() | 释放 X/Z 曲线，清活动标记、时间和位置；曲线通常在下一开始/卸载释放 |
| 安装 | clientcode/CSProject/MoonClient/Entity/MEntity.cs:964 | InitComponents 内创建 MDisplacementComponent | 与 MMoveComponent 和 MSkillComponent 并列实体组件 |

普通配置位移的数据：`PbLocal.MSkillChargeData`，生成源 `clientcode/CSProject/MoonClient/PbLocal/PblSkill.cs:6805`，字段 At、End、AimTarget、CurveX、DistanceX、CurveZ、DistanceZ、Index。At/End 为技能时间点秒；DistanceX/Z 为沿技能朝向的局部水平米；CurveX/Z 为曲线资源路径。curve Evaluate 的时间是段内归一化进度，值为距离比例，允许非单调累计曲线。

`MDisplacementEventArgs` 位于 `Event/MEventDefine.cs:2592`：curveX、distanceX、curveZ、distanceZ、totalTime、angle、skillUuid。**angle 实际是度**：MArtSkill.charge 调 `MCommonFunctions.AngleToFloat`，该方法在 `MoonCommonLib/Utility/MCommonFunctions.cs:385` 返回度；`HorizontalRotateVetor3:357` 明确 degree 参数。不能照搬组件旧注释“弧度”。

普通计算是：局部累计向量 `(CurveX(p)*DistanceX,0,CurveZ(p)*DistanceZ)`，空曲线退化线性 p；按冻结朝向角旋转后加初始位置。RO 普通位移的碰撞在 `MoonClient/Transfer/MNavigationMgr.cs:245 KickBlockedTest`、`:298 TryGetHeight`、`MAirWallMgr.CheckWall`。**不是经 MMoveComponent 转发，也不是无碰撞原生 Transform 根运动**。

鹰分支在 MArtSkill.charge:595 发 MDisplacementFlyEventArgs（Event/MEventDefine.cs:2559），终点施法点或目标半高，ClientShow 模式作起终点插值。这是表现飞行，不属于本次角色水平最小移植能力，不能用一个空入口冒称已接通。

## 动画链与 Animator 根运动搜索

`MoonClient/Skill/MSkill.cs:113 public virtual void TriggerAnim()` 选择动画、`Firer.Model.Ator.OverrideAnim`、设置 AtorSpeed，然后 Play/CrossFade Skill 槽。原接口 `MoonClient/AsyncObj/MAnimator.cs:777 OverrideAnim(string key,string clipPath,string blendTreeKey=null)`、`:589 Play(string stateName,float normalizedTime=0)`、`:609 CrossFade(string stateName,float fadeTime,float normalizedTime=0)`。这些接口播放姿态并不产生业务坐标事件。

全参考仓 C# 搜索 `applyRootMotion|OnAnimatorMove|deltaPosition|deltaRotation|rootMotion|RootMotion`，业务 MoonClient 只出现触摸输入 deltaPosition。其他命中是生成 Animator LuaWrap、ToLua 触摸适配、Spine 导入工具、`MoonCommonLib/Utility/Extension/GameObjectCallBack.cs:45-50` 通用 onAnimatorMove/OnAnimatorMove 桥。进一步搜索 onAnimatorMove 全仓，没有角色业务订阅。搜索 roab/clientproj/clientpackproj/clientcode 的 Lua/C#/JSON 同样只有生成 Unity API 声明和上述桥，未找到真实角色 Animator.deltaPosition 消费。

因此可证实的是“该检索范围内未发现 RO 业务动画根增量接管；普通技能位置由 Charge 配置曲线推进”。不能无限扩大为任何 RO 版本/任何资源均无根运动。未修改或运行 RO，也未检查不在这份工程里的闭源 DLL 实现。

## 本项目真实已有能力

- `SceneEntity/3DModel/SkillRootMotionCollector.cs` 接管 Animator.applyRootMotion，OnAnimatorMove 读取真实 Skill 状态，限定 RootMotionSegments，只保存水平世界 delta。
- `SceneEntity/Skill/MArtSkill.cs` 的 TriggerAnim/OwnsRootMotion/ReleaseAnimation 明确当前槽、真实剪辑和模型所有者；自然离槽不再推动角色，尾 FX/Audio 仍可存活。
- `SceneEntity/Component/MMoveComponent.cs:445-454` 合并输入速度、重力、Collector.ConsumeDelta 后只提交一次 Entity.MoveCharacter；该正式碰撞入口必须保留。
- `Runtime/Data/Global.cs:921 MoveSegment` 已有 Program/Animation 两驱动；当前宿主 `SkillEditorAudioMotionTools.ReadRootMotion:58` 只接受 Animation，Program 拒绝；作者没有 ChargeList/MSkillChargeData 独立制作入口。
- 动画窗口用末帧+1/FPS的半开区间，这个+1不能复制给 Program 的归一化累计进度。Y MoveSegment.Normalize 的 Program 末帧就是 p=1；end<=key 是瞬时段。

## 参考阶段迁移建议

本节记录参考阶段的迁移建议；实际采用的数据字段、曲线策略、冷模型和接口签名以后一节及末稿验收为准。

保留作者 moveSegmentList，新增运行 `ChargeList<MSkillChargeData>` 强类型数据。从现 Program/Forward/Self/固定方向的水平累计曲线子集导出，不另加无执行器的独立 RO 制作轨道。字段采用 At/End 秒、DistanceZ 米、CurveZ 强引用 AnimationCurve（替代 RO 资源字符串边界）；DistanceX=0/AimTarget=false。

映射 At=keyNumber/FPS；End=max(keyNumber,endKeyNumber)/FPS；DistanceZ=maxDistance；CurveZ=curve 的冻结复制。maxDistance 有限且非负；曲线键时间、值、权重有限，切线拒绝 NaN，允许 Unity 阶梯段的合法无穷切线；评价结果及乘距离结果仍必须有限。累计值允许回拉，不能误作速度。非零起值与瞬时段需要独立验证。不能未经确认把累积曲线武断限制为单调/末值必须1，RO 原实现没有这项限制。

只放开这份可执行子集；目标/输入/定制方向、动态朝向、纵向、碰撞重力忽略、alignFacing、目标偏移/到达限制等未接入字段必须逐项拒绝。Program 的默认 target=LockedTarget 在 Forward/Self 中不参与求值可保默认；不得因此假称当前支持目标移动。曲线 overshoot 最终按本项目确认的 Clamp/Stop/Continue 三策略适配，不沿参考初稿一律拒绝。

`FireEvents` 登记 ECharge；`OnProcessTimer` 仍使用已有 MSkillTimerArgs 代次/UUID强类型保护；`InnerProcessTimer` 直接调用 `charge(MSkillChargeData)`；位移指令保存技能 UUID、代次、当前冻结方向与已累计时间，派入独立 `MDisplacementComponent` 的 onStart/onStop 对应职责。

独立组件只求水平累计差，并向 MMoveComponent 提供当前帧一次增量。帧推进不得放入 MArtSkill.Update；不能独立写实体 Position 或第二次 CharacterController.Move。组件必须阻止旧 UUID/代次在 Q 新动作或池复用后继续推动；离动作槽也停位移，不能随保留的音尾/FX继续冲锋。

Animation 根窗口和 Program 窗口严格拒绝重叠（含瞬时边界语义需验证），避免水平两个来源相加。老 Runtime 的 ChargeList 缺字段或 null 必须按空兼容；所有现正式资产不自动导出修改。

RO Stop(false) 强拉终点不直接复制；本项目自然离槽立即解除动作运动，保原单次碰撞与重力职责；区间自己到期须提交最后累计增量一次。跨帧 timer 迟到与冷配置补发按当前技能累计时钟处理，不能重启整段持续时间导致双倍位移。

需验收：真实导出前向Program、瞬时/区间/末点、30/120FPS；非默认能力门禁；动画窗原行为；只登记一次且Update不消费时间线；同帧/跨帧/冷配置到期、自然离槽/Interrupted/Q/同UUID池复用；墙体碰撞不穿透、重力只合并一次；编译、中文注释与禁止语法、资产/服务器/原动画保护。

## 原实现不应盲抄的细节

1. RO _timeScale 是延迟倍率，对外 TimeScale 是倒数；FireEvents 延迟乘 _timeScale，charge.totalTime 却原样 End-At。本项目没有同一时间语义时必须清楚适配，不能把原注释当单位依据。
2. RO onStart 未加载实体直接跳过，本项目冷配置到期补偿有现有契约；在技能时间窗已经结束时不能补移动。
3. RO Stop(false) 普通终点不使用曲线末值；瞬时/零时长除法与飞行停止插值存在原代码边缘问题。本项目要明确边界实现，不照搬 NaN/硬拉终点。
4. RO namespace、object[]、foreach、三目和资源字符串都不符合当前项目约束；沿接口名与职责移植，业务载荷强类型。


## 本项目确认的实现范围

作者配置仍使用既有 moveSegmentList，窗口和包的作者 Schema 不改。Program 中 Forward/Self/冻结方向的水平曲线子集导出为 ChargeList；Animation 继续原 RootMotionSegments，两类窗口交叉重叠在导出及运行校验拒绝。Program 采用作者原 Clamp/Stop/Continue 三种曲线越界语义：上限保 maxDistance，Continue 可回拉，Clamp 不回拉，Stop 反向或触及上限结束；这是 Y 制作数据投影到 RO 命名的适配，原 RO 本身没有这个硬上限策略。目标、纵向、动态方向、跳过重力或碰撞等尚未实现的非默认参数继续拒绝，不静默忽略。

新增 MSkillChargeData、MDisplacementComponent、MDisplacementEventArgs、MDisplacementStopEventArgs 分别一类型一文件，使用全局命名空间与具名强类型。先前将位移驱动由 MMoveComponent 持有偏离 RO 的实体组件结构，用户已明确纠正：MDisplacementComponent 必须继承 EntityComponentBase，在实体通过 MComponentMgr.singleton.CreateComponent 挂普通组件容器；组件统一初始化、更新及池回收。沿 RO onStart/onStop/Update/StopDisplacement 的名字和职责，业务载荷仍强类型。

MSkill 冻结施法时全局 MTimerMgr 时钟原点。Program 运动按标称事件开始时刻评估累计曲线，不把 timer 已推进的整帧时间再积分一次。暖模型大步跨多段累加所有到期段再一次提交碰撞；冷配置或模型只执行窗口内剩余差量，过窗不补历史移动。自然离槽和真正打断都停止运动、丢尚未提交的位移、不强拉终点；FX/Audio 自然结束保尾继续原行为。停止必须匹配 owner、UUID 及原施法代次，不能误停新 Q。

移动组件合并输入、重力、原动画根增量和配置位移后只走原 MoveCharacter 一次；碰撞侧面阻塞停止当前程序位移。非本地角色只新增配置位移提交，不制造远端输入和新重力逻辑。SkillRootMotionCollector 原动画采集算法本轮不改，其精确跨窗口采样不能仅用薄夹具宣称已修复。

既有业务文件为 MArtSkill、MSkill、MSkillTimerArgs、SkillRuntimePresentation、MMoveComponent、MModel、MTimerMgr，以及宿主 SkillEditorAudioMotionTools、SkillEditorAssetTools。MTimerMgr 只增加 Init 会话标识，不改调度；施法冻结该身份，避免 Init 后时钟再次到旧值时续旧位移。驱动核真实 NativeObject 身份，处理同模型同步替换 GO 时的冷基线。实际函数签名和验收见下文，完整13源码及4meta清单见正式修改日志。全部用户资产与原资源保留，不自动重新导出。

正式修改归档：项目修改日志/2026-10-11-RO-SKILL-MOTION-CHANGELOG.md；当前任务与验收：Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md；临时证据：_temp/ro-skill-motion-20261011/。

## 初次移植的接口对应

以下路径均从项目根开始；这是初次移植终验时的行号。实体挂接纠正后的实际入口以末节为准。

| 接口 | 本项目位置 | 实际职责 |
| --- | --- | --- |
| `protected override void Start()` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:56` | 仅调用 base.Start、FireEvents。 |
| `protected virtual void FireEvents()` | 同文件:90 | 一次登记 FX、Audio、ChargeList 的 MTimerMgr 回调；冷配置只在加载终态补登记。 |
| `protected void OnProcessTimer(uint token, MSkillTimerArgs args)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs:366` | 注销到期票据，核 UUID/施法代次后派发。 |
| `protected override bool InnerProcessTimer(int id, MSkillTimerArgs param)` | `MArtSkill.cs:176` | EFx、EAudio、ECharge 并列处理这一条已到期指令。 |
| `private void charge(MSkillChargeData data, float initialElapsedSeconds)` | 同文件:200 | 冻结角度、标称开始毫秒、冷基线和施法身份，交 onStart。 |
| `public Vector3 SkillTowardsTo` | `MSkill.cs:69` | Fire 冻结的水平世界单位方向。 |
| `public bool onStart(MDisplacementEventArgs args)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MDisplacementComponent.cs:41` | 接管已到期段，同施法跨多段先完成前段差量。 |
| `public void onStop(MDisplacementStopEventArgs args)` | 同文件:95 | 原对象、UUID、开始代次匹配才取消，不拉终点。 |
| `public void Update(float fDeltaT)` | 同文件:106 | 保 RO 秒接口，以全局绝对时钟评价累计曲线，求本帧水平差量。 |
| `public Vector3 ConsumeDelta()` | 同文件:210 | 再核当前动作及实际 GO 身份，向唯一移动入口交付一次差量。 |
| `public void StopDisplacement()` | 同文件:230 | 清活动段及未提交量，碰撞、离槽、失效和组件归还共用。 |
| `public static List<MSkillChargeData> ReadCharge(SkillConfigSO config, float durationSeconds)` | `LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAudioMotionTools.cs:106` | 校作者参数、复制曲线、输出强类型运行指令；不改用户资产。 |

MArtSkill.Update 仍只基类时钟和已结束声音回收。运动组件 Update 是持续插值职责，不重新登记或扫描时间线未来事件；声音、粒子仍由到期回调处理。

MSkill 的内部只读 `DisplacementDurationSeconds` 使用本次 Fire 已冻结的 `_runtimePresentationDurationSeconds`。服务器确认可以为后续段调整共享 Core，当前指令的二次校验不得读取新 Core 时长；独立通过真实 BeginPrediction→ApplyCastResult 入口复现并驱动返工。

## 初次移植验收与适用边界

独立终审通过：155项运行断言（63运动＋92当前生命周期）、64项隔离Unity原生导出断言；271份GM_Game源和8份宿主Editor源编译零错误，37＋2既有警告。主读取两报告和保护审计，重算793项SHA零差异。失败已按开发/独立职责返工，覆盖Init重合clock、默认Unity曲线wrap、同步GO及pending、冷基线与共享Core时长校正。证据为 `_temp/ro-skill-motion-20261011/review/independent-review.md`、`independent-test-report.md`、`final-audit.json` 与主 `root-manifest-check.json`。

原生导出验证使用真实SkillConfigSO、SerializedObject、AnimationCurve及宿主完整发布/回验链；运动运行链链接31完整生产源，但Animator/CharacterController等原生交付使用薄边界。正式角色画面、真实Animator根增量及Collector末帧/跨窗采样、设备听感、原生墙体物理、服务器与Bundle未验，不将隔离逻辑通过扩大为这些现场结论。

七原作者门禁、Animation投影和空ChargeList兼容已核，六已发布稿完全一致；3007001用户作者声音F60/30=2秒而旧Runtime时间轴1.8秒，冻结前真实导出源亦同样拒绝，是既有未显式发布差异。保留作者/Runtime原值；需要同步时使用原绑定入口显式发布。

## 实体组件挂接纠正（2026-10-11，已完成）

用户指出 RO 的 `MEntity.InitComponents:964` 通过 `_displacementComp = MComponentMgr.singleton.CreateComponent<MDisplacementComponent>(this)` 挂接位移组件。本项目按这个所有权结构纠正，删除移动组件内构造和持有；实体保 `_displacementComp` 与只读组件访问，普通组件容器执行 Initialize/Shutdown/ResetForPool，组件继承 EntityComponentBase，新增共享 `EntityComponentType.Displacement=32`，旧值保持。

本项目专用 Move/Buff/Skill/Machine 当前在普通组件前更新，不能直接将位移挂普通容器后任由 Move 先消费上一帧差量，也不能移动组件手动再调位移 Update。采用普通容器 `UpdateBeforeMovement` 只推进位移组件，再原专用组件，最后其余普通组件；后者跳过位移优先阶段，保证唯一自动更新及末帧 Move 先于 Skill 结束。位移组件以实体作为宿主，不依赖持有旧移动组件的引用；MMove 只读取实体组件的一次差量并合并碰撞。

本轮六份源码及共享两 DLL 修改范围见 `项目修改日志/2026-10-11-DISPLACEMENT-ENTITY-COMPONENT-CHANGELOG.md`；开发与独立验证证据 `_temp/displacement-entity-20261011/`。前节155/64只是初次移植证据，本轮已重新验证受影响运行链及真实实体组件创建/更新/释放/池复用。

本轮实体结构的实际入口（已按终审末稿核行号）：

| 接口 | 本项目源码 | 用途 |
| --- | --- | --- |
| `_displacementComp = MComponentMgr.singleton.CreateComponent<MDisplacementComponent>(this)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEntity.cs:268` | OnBeforeInitialize 在 Created 阶段挂普通容器，随后统一初始化。 |
| `public MDisplacementComponent DisplacementComp` | 同文件:184 | 实体直属组件的只读快捷引用，计时器与移动组件使用同一实例。 |
| `public MDisplacementComponent(ROObject owner)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MDisplacementComponent.cs:32` | 接受管理器反射构造的实体宿主，调用组件基类。 |
| `public override EntityComponentType ComponentType` | 同文件:37 | 独立 Displacement=32，不复用 Move 或 View 的索引。 |
| `public override void Initialize()` | 同文件:40 | 每次装配清旧运动。 |
| `public override void Update(int deltaMilliseconds)` | 同文件:47 | 容器的毫秒生命周期桥到 RO 秒 Update；进度仍取绝对全局时钟。 |
| `public override void Shutdown()` | 同文件:53 | 普通组件池回收前清指令和待提交量。 |
| `public override void OnDetachFromHost()` | 同文件:59 | 直接卸载也立即停止，随后基类解除挂接。 |
| `public bool onStart(MDisplacementEventArgs args)` | 同文件:68 | 核实体与启用/挂接身份，接已到期指令。 |
| `public void onStop(MDisplacementStopEventArgs args)` | 同文件:122 | 原施法停止，不能解除新宿主技能。 |
| `public override void Update(float fDeltaT)` | 同文件:133 | 独立实体组件持续求水平差量。 |
| `public Vector3 ConsumeDelta()` | 同文件:238 | 仅交付已有差量，不代替容器推进。 |
| `public void StopDisplacement()` | 同文件:258 | 清运动引用与缓存。 |
| `public void UpdateBeforeMovement(int deltaMilliseconds)` | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/base/EntityComponentContainer.cs:119` | 查已挂普通组件，移动前唯一调度；其余普通阶段跳过此类型。 |

用户已在之后修改3007001为落剑F13/end111、末声音F49，这才是本轮保护基线；前节F60是初次移植历史事实，不能回退或套用旧未发布描述冒称当前字段。

本轮独立审查及测试均PASS：189运行断言（63运动＋92生命周期重新执行＋34真实实体组件）；运行链接45完整生产文件，已验证同实例挂接、每帧唯一求值、唯一碰撞提交、自然末帧先提交后退出、直接卸载立即停止及真实组件池重借新宿主。271客户端源及65共享源编译零错误，37及4既有警告；主734项末稿及验证输入SHA零差异。

共享旧0–31枚举及导出类型以可靠旧DLL和原工具文本核实保持，新仅Displacement=32；共享源码缺事前字节快照的限制单列。两正式DLL等于开发同次产物，实际客户端引用Common。2796前置路径只7授权变化，另共享枚举单列，无越界；当前用户资产保持。

本轮未新跑原生导出，旧170输入有8处后来授权差异，不继承旧64为当前原生结论。Unity原生与状态机仍为薄边界，真实画面、听感、PlayMode、墙体及Animator精确末帧/跨窗、服务器/Bundle尚未现场验证。完整证据与交接见本轮修改日志及Docs/AI/TEST_REPORT.md，临时资料保留。
