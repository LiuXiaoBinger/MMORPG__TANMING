# RO 技能动画、特效时间轴与位移实现

## 范围与读法

本文只分析 `F:/UnityPro/ro/Ro_cs422nb` 的现有源码和配置，不改变当前游戏的技能实现。下面的源码路径均相对于 RO 项目根目录；行号为本次检查时的行号。`config/Assets/Resources/SkillData/**/*.txt` 是可读的技能编辑器数据样本，客户端实际由同名 `.bytes` 解析 `PbLocal.MSkillData`。特效时间采用**施法起点后的秒数**；“第几帧”只有结合具体剪辑采样率和播放倍率才能换算，运行时由计时器驱动，不能把 `At` 直接当 Unity `AnimationEvent` 帧号。

## 一、数据从哪里来

| 层 | 类与函数 | 职责 |
| --- | --- | --- |
| 技能主表与效果表 | `SkillTable`、`SkillEffectTable`；`clientcode/CSProject/MoonClient/Skill/MSkillCore.cs:688` 的 `InitWeaponDatas` | `SkillDataPath`、`SkillScriptPrefix`、可选 `SkillScriptPostfix` 组合出技能脚本名。后缀可区分武器与连击段；`GetDataByWeaponType` 选择当次表现。 |
| 技能脚本加载 | `clientcode/CSProject/MoonClient/Skill/MSkillFactory.cs:67` 的 `BuildSkillData` | 读取 `SkillData/{path}/{scriptName}.bytes`，`PbLocal.MSkillData.Parser.ParseFrom`，按路径缓存；`CreateSkill` 按 `TypeToken` 借对应技能子类。 |
| 脚本结构 | `config/AutoGen/protobuf/local/common/pbl_skill.proto:8` 的 `MSkillData` | `ClipName`/`SkillClips` 是普通动画；`AnimList` 是按时间切动画的列表；`FxList`、`ChargeList`、`AudioList`、`ResultList` 等是独立轨道。`Time`、`AnimationLength`、`SkillStateTime` 是不同长度。 |
| 单条动画/特效/位移 | 同一 proto 的 `MSkillAnimationData:392`、`MSkillFxData:206`、`MSkillChargeData:236` | 动画有 `anim_path`、`begin_time`；特效有 `At`、`End`、`Fx`、`LoopCount`、`LoopCycle`、`Bone`、`Follow`、偏移；位移有 `At`、`End`、X/Z 距离和曲线。 |

因此“一招有两段挥刀动画、两次刀光和蓄力光”通常是一份 `MSkillData`，其中 `AnimList` 有两条切换记录，`FxList` 有三条独立记录。动画条目与特效条目没有一对一外键；策划通过同一施法时间轴上的 `BeginTime` 和 `At` 对齐。若有更细的命中表现，`ResultList`/`ResultFxList` 属于另一条由服务器结果触发的链。

## 二、施法到动画播放

1. `clientcode/CSProject/MoonClient/Components/Action/MActionComponent.cs:67` 的 `OnGetPermission` 标记状态机动画刷新，调用状态 `OnStart`。
2. `clientcode/CSProject/MoonClient/Skill/MSkillComponent.cs:931` 的 `FireSkill` 创建/选取 `MSkill`，调用 `MSkill.Fire`。`MSkill.cs:457` 的 `Fire` 绑定技能核心、目标、施法点、UUID、`UsingTimes` 和 `TimeElapsed`，从核心选出本次 `_dataOnFire`，调用 `Execute`。`MSkill.cs:608` 的 `Execute` 保证 `Start` 只执行一次。
3. `clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:1008` 的 `Start` 调 `FireEvents`，将各条表现轨道注册为计时器。技能开始链本身不直接执行普通动画 `TriggerAnim`。
4. `clientcode/CSProject/MoonClient/Components/State/MStateMachine.cs:147` 的 `LateUpdate` 调当前状态的 `PlayAnim`；`Skill/MSkillComponent.cs:763` 的覆写调用当前技能 `TriggerAnim`。
5. `Skill/MSkill.cs:113` 的 `TriggerAnim`：若 `AnimList` 非空就返回，由分段定时器负责动画；否则选择 `SkillClips` 或 `ClipName`，覆盖 `MAnimator.STATE_SKILL`。已经运行超过 0.1 秒时按 `TimeElapsed / TimeScale / clip.Length` 的归一化时间 `Play`，否则 `CrossFade(..., 0.1f)`。这里 `TimeScale` 属性为 `_timeScale` 的倒数；实际定时器延迟使用 `_timeScale`。
6. `Skill/SkillLogic/MArtSkill.cs:363` 的 `FireEvents` 对每条 `AnimList` 以 `BeginTime * _timeScale` 注册 `OverrideAnim`；`:483` 的回调覆盖 Skill 槽，并按 `(TimeElapsed - BeginTime) / TimeScale / clip.Length` 续播当前段。两次挥刀可以在不同 `BeginTime` 切换两段剪辑。

`MAnimator`（`clientcode/CSProject/MoonClient/AsyncObj/MAnimator.cs`）负责 `OverrideAnim`、异步剪辑持有与 `Play`/`CrossFade`，不决定哪一条技能特效在哪个时刻发。技能状态占用时长由 `MArtSkill.IsEnd`（`:255`）使用 `SkillStateTime / TimeScale` 判断；完整技能是否仍运行由 `Present`（`:531`）比较 `TimeElapsed` 与 `Time * _timeScale`。它们不能替代每条特效自己的 `At`。

## 三、特效在哪个时间释放

| 步骤 | 类与函数 | 实际处理 |
| --- | --- | --- |
| 排程 | `MArtSkill.Start → FireEvents`（`:1008`、`:259`）→ `fxListEvents`（`:237`） | 遍历非 `MultiTarget` 的 `FxList`；每条注册至少一次，循环条目第 `j` 次延迟为 `(At + LoopCycle * j) * _timeScale`。`At <= 0` 在 `MTimerMgr.SetTimer` 中同步回调。 |
| 计时 | `clientcode/CSProject/MoonClient/Utility/MTimer.cs:178`、`:217`、`:263` | `SetTimer` 把秒换成毫秒并舍入，时间轮在 `Update` 中派发。因此这是按时间调度，实际出现在处理该计时器的帧，不保证精确到动画剪辑的整数帧。 |
| 触发 | `MArtSkill.InnerProcessTimer`（`:79`）、`fx`（`:789`） | `EFx` 回调找施法者或目标；如果目标模型尚在加载，注册 `AddLoadGoCallback`，回调中再次检查 `Casting`，之后执行 `ProcessFx`。 |
| 定位与生成 | `MArtSkill.ProcessFx`（`:818`）→ `playFx`（`:676`） | `Type` 决定施法者/目标基准；`Follow` 决定挂模型骨骼还是用世界位置，另应用旋转、偏移、高度百分比和缩放。通过 `MFxMgr.CreateFx` 或 `MModel.CreateFx` 创建。 |
| 资源解析 | `Effect/Fx/MFxMgr.cs:267`、`:304`，`Model3D/MModel.cs:680` | `Fx` 是 `EffectTable` 的 ID；表里 `Path` 找预制体，并提供跟随位置/旋转/缩放与默认寿命。`MModel.CreateFx` 记录所属模型和骨骼。`MFx` 负责异步加载、播放与销毁。 |
| 生命周期 | `MArtSkill.playFx:703`、`Stop:413`、`MSkill.KillTimerAll:311` | 设置 `(DestroyDelay + End - At) * _timeScale` 的特效播放寿命；技能停止时取消未触发的计时器与模型加载回调。`Stop(cleanup)` 为真时主动销毁其记录的特效 ID。 |

`FxList` 是**本地技能表现时间轴**；`ResultList.ResultFxList` 不等同于它。`MArtSkill.FireServerEvents`（`:1016`）按服务端结果是否生效选择结果特效；多目标结果还通过 `PlayMultiTargetFx`（`:1050`）对目标列表逐项安排，计算 `max(0, (At + LoopCycle*j)*_timeScale - TimeElapsed)`。伤害与命中由权威结果决定，不能凭播放了刀光就认定打中。

## 四、真实配置样本与“帧”换算

样本 A：`config/Assets/Resources/SkillData/Dungeon_DaNaTuoSiZhiZhan/Monster_BeiAi_BeiAiHuoXinXi.txt`。

| 施法后时间 | 配置 | 表现 |
| --- | --- | --- |
| 0 秒 | `AnimList` 第一条 `...Skill_Singing`；`FxList` 的 ID 1129，`At=0, End=4` | 蓄力动作及持续特效开始。 |
| 4 秒 | `AnimList` 第二条 `...Skill_GuangYuChenMo`；`FxList` 的 ID 1072，`At=4, End=4` | 切换施放动作，并触发另一条特效。 |

样本 B：`config/Assets/Resources/SkillData/Dungeon_A5/Monster_GuNa_Attack.txt:101` 的 `FxList` 有 `At=0.233333349`、特效 ID 145。**假设**对应剪辑采样率为 30 FPS、正常倍率，则约是起点后第 7 帧；实际资源采样率、动画启动/过渡和时间轮更新可能改变用户看到的具体画面帧。源码没有把该特效直接绑定到 `AnimationClip.events`。

若制作“两次挥刀 + 两次刀光 + 蓄力”，可以按同一施法时钟配置：蓄力 Fx 的 `At=0`；第一段 `AnimList.BeginTime=0`、第一刀 Fx 的 `At=t1`；第二段 `BeginTime=t2`、第二刀 Fx 的 `At=t3`，其中 `t3 >= t2`。这些时间须对照剪辑挥刀姿态确定，再填入脚本；这只是配置方法，**不是上述样本的实际数值**。若要严格锁定某个 Unity 采样帧，需先核对该剪辑 FPS，再用 `t=帧号/FPS` 换算并在 Game View 校验计时器实际落帧。

## 五、根运动、冲锋与模型位移

本次检查的 RO 技能代码中，技能驱动的实体冲锋由**独立位移轨道**实现：

1. `MArtSkill.FireEvents:261` 按 `ChargeList.At * _timeScale` 注册 `ECharge`；`:580` 的 `charge` 取 `End - At`、X/Z 距离与曲线、朝向、技能 UUID，派发 `MEvent_Displacement`。特殊鹰类派发 `MEvent_Displacement_Fly`。
2. `clientcode/CSProject/MoonClient/Components/Common/MDisplacementComponent.cs:83` 订阅开始、飞行和停止事件；`:119` 的 `onStart` 记录起点、时间、方向并加载曲线；`:259` 的 `Update` 用 `p=elapsed/totalTime` 求 X/Z 位移，按朝向旋转后做阻挡、地形高度和空气墙检查，最终赋值 `Entity.Position`。
3. 同组件 `onStop:179` 只接受匹配的技能 UUID。`MArtSkill.Stop:413` 在技能结束或打断时发停止事件；正常停止与清理停止的处理不同，避免旧施法的停止消息影响新位移。

真实例子：`config/Assets/Resources/SkillData/Dungeon_BaiNvWuXia/Monster_BaiNvWu_AnSeChongJi.txt:169` 的 `ChargeList` 为 `At=0, End=0.2, DistanceZ=2, AimTarget=true`，并另有 `FxList` 在约 0.0333 秒、0.3333 秒触发。这说明**动画、特效和实体位移是可并行的配置轨道**，而不是从刀光或动画帧自动推算位移。

“根运动”还可能指 FBX/AnimationClip 的根骨骼曲线或 Unity Animator 的 `applyRootMotion`。在本次检查的 `MoonClient/Skill`、`Components`、`Model3D`、`AsyncObj` C# 中，没有找到 `OnAnimatorMove`、`Animator.deltaPosition` 或 `applyRootMotion` 的显式技能位移链；这只证明这些源码没有用该方式驱动上述冲锋，**不能证明所有动画资源都没有根节点曲线，也不能替代对具体 FBX 的 Root Transform Bake 和 Animator 组件设置检查**。骨架/模型在动画内上下起伏，与 `Entity.Position` 的世界位移应分别观察。

## 六、移植时的对应关系与边界

- 多段动画：保持一个施法实例的统一时钟，用 `AnimList.BeginTime` 切不同剪辑；不要把两段动作误当作两次技能请求。
- 多段特效：每条 Fx 独立配置 `At/Fx/End/Follow/Bone/Offset`。蓄力、第一刀、第二刀分别占一条或多条轨道；播放点由表现时间轴决定，目标命中另等权威结果。
- 位移：真正推动角色世界坐标时，配置 `ChargeList` 并交位移组件处理曲线、阻挡和落地高度；单纯动画姿态或粒子不应改实体坐标。
- 结束：`SkillStateTime` 控制何时退出技能动作状态，`Time` 控制完整技能表现生命周期，单条 Fx 的 `End/DestroyDelay` 控制自身寿命。三者不应合并为一个时长。
- 当前游戏已有的 1 号大剑落下采用**原动画事件适配**，与本文 RO 的 `FxList` 计时器数据链不同；本文仅是 RO 参考分析，不表示已把其全部轨道移植到当前项目。

本次核验为源码、proto 和可读配置静态检查，没有运行 RO 客户端或逐帧录屏。尤其“第几帧”需带上所选技能脚本、动画剪辑 FPS 和实机播放倍率才有唯一答案。
