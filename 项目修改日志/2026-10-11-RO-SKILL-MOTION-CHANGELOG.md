# RO 角色技能位移与根运动实现

- 日期：2026-10-11。
- 状态：已完成；开发、真实失败返工及独立终审通过，主已复核证据并完成正式归档。
- 用户目标：总结 RO 角色技能根运动及函数接口，根据真实源码总结实现相关代码，严格遵守项目约束，子 Agent 审查不通过则返工。
- 分工：主 Agent 拆解、范围与验收协调、正式记录；ro_skill_motion_analysis 只读核对 RO；ro_skill_motion_develop 修改客户端；ro_skill_motion_review 独立测试审查。

## 设计与修改范围

参考目录 `F:/UnityPro/ro/Ro_cs422nb`。先核验技能 FireEvents/OnProcessTimer/ECharge、实体位移事件、MDisplacementComponent、模型动画与移动碰撞入口，区分配置曲线位移和 Animator 根运动。不能把先前只读 ECharge 的结论扩大为整个 RO，也不能用同名空接口代替实现。

确认既有业务范围：MArtSkill、MSkill、MSkillTimerArgs、SkillRuntimePresentation、MMoveComponent、MModel、MTimerMgr，以及宿主 SkillEditorAudioMotionTools、SkillEditorAssetTools。新增 MSkillChargeData、MDisplacementComponent、MDisplacementEventArgs、MDisplacementStopEventArgs 各同名独立文件与 meta。均在 LYF_MMO/Assets/GM_Game 的现有 Common/Skill/Component/event/Editor/Skill 对应目录；最终清单按开发末稿核实。SkillRootMotionCollector 保持原算法，作者 Schema 与集中窗口不改。所有业务由开发子 Agent 修改，保留用户当前基线；不回退已有成果，不自动重导作者/Runtime、场景或资源，不修改参考 RO 或服务器。

真实 RO 普通技能采用 ChargeList 的累计配置曲线，动画姿态独立；查到的通用 OnAnimatorMove 桥没有角色业务订阅。实现 ECharge→charge→独立 MDisplacementComponent，保现有 Animator 根运动；Program 前向/Self/固定方向/水平累计曲线子集从原 moveSegmentList 导出 ChargeList，Animation 原窗口保持，交叉重叠拒绝。保 Y maxDistance 硬上限及 Clamp/Stop/Continue 语义，未知非默认参数拒绝，属于本项目数据适配而非 RO 原样参数。

全局计时原点与事件标称时间防首帧双推进、大步多段丢失；冷配置/模型只执行窗内剩余差量、过窗不补移动。动作离槽或 Interrupted 清运动不拉终点、停止匹配原 UUID/代次；Move 统一合重力与水平两种来源并一次碰撞提交，侧面撞墙停止。非本地角色只新增配置位移，不新增输入或重力系统。

正式分析 `项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md`；协作记录维护在四份 Docs/AI 入口。另一已完成的特效时间显示任务独立修改集中编辑窗口，不混入本轮实现；运动冻结捕获的两份并发授权变化在保护结果单列。

## 验收与交接

计划验收真实接口及单位、方向/曲线空间、动作/技能生命周期、当前所有者、Q 切段、自然结束/Interrupted、冷加载与池复用、重力和碰撞单次提交、对应真实源编译及规范检查。失败交开发修复并重新审查；原生/隔离/现场验证范围分别记录，不援引上一轮通过数量。

中间文件统一 `_temp/ro-skill-motion-20261011/`，保留失败及返工证据，未经产品经理确认不清理。

## 首稿返工记录

- 首稿开发编译：271 份真实 GM_Game 源零错误、37 条既有警告；宿主导出 8 源零错误、2 条既有警告。此前口头误将宿主计为历史包 Runtime 的 59 源，按本轮实际编译输入纠正；仅代表首稿编译，不作为最终验收。
- 独立新增运动首轮 52 项中 51 项通过，第 52 项真实失败：计时器 Init 后重新推进至旧时钟值，单纯倒退检测不能识别新会话，旧运动可能继续。已交开发为 MTimerMgr 增加 Init 自增的只读会话标识，由施法冻结并核验；不改计时调度。最小范围从八份增为九份既有源。
- 同一 MModel 直接 SetGameObject 替换时，不一定能观察到 IsLoaded=false；交开发核真实 NativeObject 身份，替换后丢旧待提交差量并按当前进度重新建立基线，独立验证后再记录结论。
- null 旧 ChargeList 按空兼容、曲线乘距离先检查非有限值再限幅已返工；仍须在末稿复验。
- 独立原生复现最小 Program 默认段误拒：Unity 默认竖直零曲线 wrap 实为 ClampForever；开发原假设 Default 错误，已按原真实默认曲线逐项比较，不改作者资产迎合。
- 独立末轮使用真实 BeginPrediction/ApplyCastResult 复现共享 Core 校正到较短动作时，当前施法仍锁原长动作，但 onStart 二次校验误用 Core 新时长拒绝合法指令。开发改为使用本次施法已经冻结的表现时长，只增内部只读查询，不改 Core 或原锁定机制；末稿回归通过。
- 原七绑定只读验证发现 3007001 作者声音 F60/30=2 秒，Runtime Timeline=1.8 秒/54 帧；本轮 before 两旧导出源对同资源复现同一 ReadAudio 失败，属于用户未显式发布差异，保留原稿并记录，不自动导出。此前口头“46帧”已按真实回读纠正。
- 本轮同步更新使用说明 `项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md` 的 Program 支持范围、三策略、发布及未实现参数门禁。

## 末稿文件清单

九份既有源：

- `LYF_MMO/Assets/GM_Game/Scripts/Common/MTimerMgr.cs`：只读 Init 会话版本。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：冻结朝向、时钟、会话和本次时长；位移归属检查。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：一次注册 ECharge、到期 charge、离槽停止及轨道冲突校验。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillTimerArgs.cs`：强类型程序指令及冷登记基线。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimePresentation.cs`：空兼容 ChargeList。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`：持有驱动、合并一次碰撞位移及侧撞停止。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs`：只读最近侧碰撞结果。
- `LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAudioMotionTools.cs`：Program 解析、曲线冻结复制、参数门禁及发布逐项校验。
- `LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAssetTools.cs`：原显式发布写入 ChargeList。

四份新增源及各同名 meta：

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillChargeData.cs`。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MDisplacementComponent.cs`。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/MDisplacementEventArgs.cs`。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/MDisplacementStopEventArgs.cs`。

正式文档为本日志、`项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md`、`项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md` 和四份 `Docs/AI` 入口。临时开发、独立验证及主核查证据仅保存于 `_temp/ro-skill-motion-20261011/`。

## 最终验收与未解决边界

- 独立两报告通过，主已读取复核。155项运行断言（63新运动＋92当前旧生命周期重跑）、64项隔离Unity原生导出断言通过；分别覆盖完整生产调度/生命周期的薄原生夹具，以及真实作者序列化/曲线/导出/发布回验，不混称现场物理或画面。
- 开发与独立末稿271份GM_Game源、8份宿主Editor源均Rebuild零错，37＋2既有警告。17业务/meta路径、31运行输入、271＋8独立编译输入、170原生输入及开发271＋8输入，主重算793项SHA零差异。证据 `_temp/ro-skill-motion-20261011/root-manifest-check.json`。
- 2788前置保护：2777字节保持、9本轮既有源变化、2并发授权Window变化；本轮新增8路径，零删除/未授权变化。13源码人工逐声明中文注释及独立禁语法审查通过，四meta GUID唯一。并发Timeline差分为前任务撤销自己的短暂无关动画等价改动、恢复其真正原函数，本轮未扩改。
- 七原作者门禁、Animation投影及空ChargeList兼容均通过，六发布完全一致；3007001声音2秒超过既有Runtime范围1.8秒，before真实导出源复现相同失配，保留原稿。要同步时由原绑定入口显式发布，本轮未自动重导。
- 失败证据保留首稿与返工日志；共享Core失败的同名日志后来刷新为终验成功，主将此前工具实际读到的尾段保存为review/shared-core-failure-captured-tail.txt并注明来源，不冒称完整原日志。
- 真实Animator根增量、Collector精确末帧/跨窗、正式PlayMode/角色画面/设备听感、原生墙体碰撞、服务器联调与Bundle发布未验。无剩余已复现代码阻断；不把保留的Collector算法边界算本轮已修复。不新挂正式测试组件，不改参考RO、服务器、作者Schema或正式资源。
- 交接：正式分析和Program使用说明、四协作入口已同步。独立证据review/independent-review.md、independent-test-report.md、final-audit.json及各真实编译/运行/原生日志；开发交接develop/develop-result.md/json、final-manifest.json。下一步原正式入口体验，临时资料由产品经理确认后清理。
