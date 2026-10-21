# 2026-10-21 技能位移改用RO配置曲线（源码、配置与发布检查通过）

## 需求、分支与执行者

用户明确确认“改成RO方案，移除采集器依赖”。采用既有ChargeList计时派发和实体MDisplacementComponent，删除动画增量采集；保留六技能实际位移、既有输入/导航/跳跃/同步/动画/特效/音效/CD和单次碰撞入口。基线`1e2e084269aacefc5f1f88c57bc1e843d0e3616d`，工作分支`codex/feature-development`，无main合并授权。

Codex负责逻辑设计、验收、记录及Git集成，不代改业务；WorkBuddy官方桌面工作任务`hub-46564742-574c-4b14-9691-480100ba2351`执行。项目LYFMMORGP，目录D:/unitypro/LYFMMORGP；首轮run-2c8d8efe-09ce-4610-b15b-c5d42961568a，会话5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0，desktop_queue，已领取。原请求保存在`_temp/ro-charge-only-20261021/request.json`。

## 设计与检查重点

运行保留`FireEvents→ECharge→charge→onStart/Update/onStop→MMoveComponent`。曲线是归一化时间下的累计距离比例，组件提交差量，不能当速度重复积分。关闭Animator自动根运动，防止删除OnAnimatorMove后由Unity自动写世界坐标。

六个动画窗口（3001001/3001002/3001003/3004001/3006001/3008001）须用实际Avatar原生动画采样迁移；旧程序字段未执行，禁止将零距离或Q1未执行的2米视为旧实际位移。动画窗口含末帧，Program末帧直接换秒，迁移需保持原实际窗口。滑步原倍率1.458485须计入。若前向子集不能表达真实横移，先基于证据报告最小扩展，不静默截断。

已发现普通动作制作工具SkillEditorAudioMotionSetup.ValidateLocomotion引用ReadRootMotion，交付时必须核对这处编译依赖。专题：`项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md`；跨模块迁移复盘后续补入`项目分析/项目难点.md`。

## 当前实际文件变化

以下为已核实的本轮33条正式路径（28修改、1新增、4删除）；中间失败已退原任务修正，最终独立检查通过。已有资源与源码的.meta保持，只有两个删除类型连同各自.meta删除，没有新增目录或类型。

|状态|文件（项目根相对路径）|用途与变化|
|---|---|---|
|修改|Docs/AI/CURRENT_TASK.md|记录用户确认、任务/会话/基线、迁移范围与待验边界。|
|修改|Docs/AI/TEST_REPORT.md|记录本轮独立验收目标、已发现旧接口依赖和待完成检查。|
|新增|项目修改日志/2026-10-21-RO-CHARGE-ONLY-CHANGELOG.md|本轮唯一正式修改日志，后续追加逐文件变化、验证和Git回执。|
|修改|项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md|记录最新只保留RO曲线的用户决定，说明其替代历史双驱动方案，后续补迁移及验收结果。|
|修改|项目分析/项目难点.md|新增跨运行/制作/资产迁移的真实难点，区分已取证事实和待验结论。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs|移除采集器持有、安装与释放，实际Animator加载后关闭自动根运动。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs|移除本地与远端动画delta消费，保留原程序差量、重力与单次碰撞及同步。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MDisplacementComponent.cs|在原组件内补固定右向X累计差量、迟到基线与停止清理，复用原接口。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs|删除根采集器所有权、缩放及双驱动校验，保留原Charge事件/停止及动画、音效、特效收尾。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillChargeData.cs|沿RO既有CurveX/DistanceX允许可选横移，复用既有曲线校验并补中文注释。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimePresentation.cs|删除旧RootMotionSegments字段，正式位移只用ChargeList。|
|删除|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/SkillRootMotionCollector.cs|移除Animator.deltaPosition采集组件。|
|删除|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/SkillRootMotionCollector.cs.meta|随不再使用的采集器源码删除GUID；正式资源无该GUID引用。|
|删除|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimeRootMotionSegment.cs|移除旧动画根窗口序列化类型。|
|删除|LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimeRootMotionSegment.cs.meta|随旧窗口类型删除；正式资源无该GUID引用。|
|修改|LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAudioMotionTools.cs|删除旧根窗口导出；ReadCharge复制X/Z、保原模式校验、发布逐项比较X。|
|修改|LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAssetTools.cs|删旧根字段写入，在原两预检接点改ReadCharge，发布保原ChargeList写入。|
|修改|LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAudioMotionSetup.cs|普通动作校验改ReadCharge，移除自动新增Animation段方法和调用，保声音制作。|
|修改|LYF_MMO/Packages/com.ethan.act-action-editor/Runtime/Data/Global.cs|仅既有MoveSegment补可选curveX/distanceX和具体中文说明。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3001001.asset|Q首段改Program，按真实120Hz累计X/Z与原窗口迁移，保非位移内容。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3001002.asset|Q第二段改Program，保有符号横移、前向峰值和回拉。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3001003.asset|Q第三段改Program，按完整轨迹及Continue保超峰回拉。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3004001.asset|E改Program，按自身30FPS保持3.1秒，迁移明显左移与前向轨迹。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3006001.asset|R改Program，以前向峰值归一化保末段回拉及横移。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3008001.asset|滑步改Program，旧1.458485倍率吸收入X/Z距离，说明改为程序曲线。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3001001.asset|移除旧根窗口并保存Q首段两轴ChargeList，保其它字段。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3001002.asset|保存Q第二段两轴曲线、尺度与Continue策略。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3001003.asset|保存Q第三段完整累计曲线，保超峰后回拉。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3004001.asset|保存E原3.1秒窗口、横向与前向累计轨迹。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3005001.asset|仅删除旧空根字段，ChargeList仍为空，不新增位移。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3006001.asset|保存R完整两轴轨迹和峰值尺度，末点可小于峰值。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3007001.asset|仅删除旧空根字段，保原无位移。|
|修改|LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3008001.asset|保存滑步0.6666667秒两轴曲线，最终前向约5米。|

临时文件均不提交：`_temp/ro-charge-only-20261021/request.json`保存任务单；`review/prepare_review.py`为独立客户端与Editor隔离编译；`review/audit_assets.py`为14资产非位移字段保护、窗口及程序模式审计；`review/hub-latest.json`保存最新真实执行状态。WorkBuddy临时成果在同目录workbuddy子目录，交付后核对。

恢复与独立测试新增临时文件：`review/pre-delivery-findings.md`集中失败证据；`review/inspect_native.py`及`inspect_pending.mjs`核查本任务原生状态（后者指定参数时只取消匹配本轮权限请求的会话）；`native-pending.json`、`native-stop.json`保存真实状态；`hub-cancel.json`、`hub-recovered.json`记录Hub停止及释放结果；`rework-2.json`为原任务失败反馈；`successor-request.json`为确认停止后的后继任务；`hub_call.py`按UTF-8保存状态回执并精简输出；`hub-get-latest.json`、`hub-wait-latest.json`为调用回执；`compile-client/`、`compile-editor/`为隔离编译输入、输出及日志。

WorkBuddy首轮额外写入旧临时工程`_temp/f-roll-slide-20261012/client/native-project/Assets/ChargeSample/`：ChargeClipSampler.cs、Anim_JX_Slider_F.anim、Anim_MJS_Atk01.anim、Anim_MJS_Atk02.anim、Anim_MJS_Atk03.anim、Anim_MJS_Skill01.anim、Anim_MJS_Skill03.anim、Role_MJS.prefab、SK_CALISTA_01.fbx、tmp_sampler.controller及对应.meta；目录元文件交付时继续核对。本轮新输出已要求只放当前任务目录，不清理旧临时证据。

## 首轮失败与执行恢复（真实状态）

- 首轮仅部分删除MModel字段，独立291源编译15错38既有警告；MModel自身以及MMoveComponent/MArtSkill仍依赖被删字段。Editor级联失败不能算有效验证。六项采样全部零，且探针用magnitude丢失方向，不可采用；原生真实滑步历史非零样本只作控制。
- 任务期限届满后，原生session:get显示pending/isProcessing=true、pendingInputKind=permission，停在Git恢复命令call_cbf62cd51a5a4e629752281e。用户反馈桌面无工作显示，主未要求重复寻找，也未批准恢复命令；通过官方session:cancel取消本轮，复查completed/isProcessing=false、待输入均空。
- 官方service recover成功释放过期占位，Hub原任务状态cancelled、holdsCapacity=false。原Hub rework明确拒绝cancelled状态；这是接口状态限制，不是自动审批拒绝。未修改网关、私有任务库或伪造worker报告。
- 确认原执行停止后，Hub后继任务`hub-17da090e-d497-48cd-86cc-43cff7ced9d9`继续本项目原会话，稳定键lyfmmorgp-ro-charge-only-20261021-successor-after-confirmed-stop-01；补充SkillEditorAudioMotionSetup最小编译适配，提供真实失败证据和采样控制，不再执行Git恢复命令。后继尚待交付，不报成功。

## 后继采样、只读审查与最小追加范围

后继轮次`run-250daa1c-bb0a-4a88-832b-73bed923a587`已实际领取，仍为原本项目会话、desktop_queue。临时探针首次编译漏System.Exception引用失败，后续修正运行得到滑步控制(-0.0261410326,3.428114)米，与历史一致；倍率后Z=4.99985266米。六clip末点采样中3004001 X=-0.89585197米、3001002 X=-0.177980036米；3006001 Z比例峰值至少1.16072989后回到1。不能只填最终Z距离或抹掉回拉。

只读子Agent`ro_charge_review`核实际调用与原RO代码，未改文件。最小追加任务单`review/supplement-2.md`授权Global.cs既有MoveSegment增加curveX/distanceX、MSkillChargeData接受有效X、MDisplacementComponent计算固定右向X累计差量，原ReadCharge/发布校验和六资产同步X/Z；Z以正向峰值等合适尺度归一化并用Continue，保原Y硬上限而不截本批轨迹。不扩展UI/Gizmo或通用MoveSolver。SkillEditorAudioMotionSetup另需移除仍写Animation段的AddRootMotionIfEmpty调用/实现，保原声音。采样须补同一次120Hz完整X/Z时间序列及根旋转核对；当前九点Z不能算等价验收。

后继再次卡在git rm及通配删除命令，违反任务“不Git”约束。主核具体请求call_3de205ae1eb948878671c16d后，官方rejectPermission只拒绝该命令，并附读取补充单反馈，未执行删除。原生随之completed/isProcessing=false，但Hub领取仍running。`resume_same_attempt.mjs`调用既有安全唤醒接口被标记格式校验拒绝，未发送消息、不修改网关；之后核原生空闲与目录一致，以`continue_native_task.mjs`通过官方session:sendMessage发送一次原任务续办反馈，回读planning/isProcessing=true。未新领取、未伪造凭据、未代报结果；后继仍待交付。

新增临时证据逐项：`review/supplement-2.md`；`reject_git_removal.mjs`、`native-reject-git-removal.json`；`resume_same_attempt.mjs`（校验拒绝，无发送）；`continue_native_task.mjs`、`native-continue-intent.json`；`unrelated-input-hashes.json`冻结13个已有无关文件；`verify_displacement.py`及`displacement-runtime/`（真实源码输入哈希、MotionTests.csproj、历史Vector3/CurveBoundary/Mathf薄边界副本、GameObject/Quaternion/Debug/ModelGameObject/MModel/ROObject/EntityComponentBase/MSkill/MTimerMgr/Program测试文件、构建中间产物、runtime.log、before-x-support.log）。首次真实组件隔离测试编译成功，在现有未支持X的配置校验处准确失败；不是末稿通过结果。

补充原生预检回归：`review/native-round-2-precheck.log`在第二轮被冻结源码的专用副本中准确失败于“作者原预检拒绝旧Animation段”断言，实证删除旧接口后预检放行。该批处理主进程已退出，但PowerShell Start-Process -Wait仍等待后台子树；核实无本轮Unity进程、命令行匹配本轮日志后仅终止本轮等待壳59604，没有终止正式Unity或WorkBuddy。新增`review/run_native.py`改为Python只等待本轮Unity主进程并保存真实退出码，避免该验收启动器问题；后续产物`native-process.json`、`native-exit.json`及`native-charge-only-final.log`仅在实际运行后生成。

## 验证、未解决事项与交接

第三轮run-e20cf31b在2026-10-21T11:13:27Z回报completed，holdsCapacity=false。开发自检完整曲线174通过；主专用Unity重新编译/导入，恢复预检断言已通过，但3001001原alignFacing仍1，ReadCharge经ValidateConfig提前抛错，真实退出1。两预检传入制作时长而非原动作、一个4项表达式、旧Program参数尚未修，第三轮仍未验收。已沿原Hub第四轮run-539d1fab-f51b-47e7-8fa7-ba27a15827a7返工，截止2026-10-21T11:44:28.130Z；反馈review/rework-successor-4.json，回执rework-4-receipt.json，失败快照round-3-failure。新增只读review/final_inputs.py汇总末稿验收输入SHA、精确33路径与无关文件保护，只有真实各项通过才生成有效最终检查，不自动暂存。

后继第二轮run-8910d41a在2026-10-21T10:46:45Z真实回报completed，desktop_queue、holdsCapacity=false。主核真实差异并独立检查：包59源/客户端289源/Editor8源均0错，警告2/38/2；组件14断言通过，14资产非位移保护48项通过。完整曲线174项有14项失败：六两轴缺零起点导致时间0提前交付首步、E作者FPS为30却写372帧、滑步旧根倍率未置1。原生专用Unity工程编译导入后又复现3001001的ReadCharge因alignFacing旧参数失败；其余五份还有原未执行的方向、偏移空间、容差及空竖直曲线，均需只在迁移段内重置为已有Program默认，不放松发布校验。

只读子Agent另核原Validate/ValidateConfig删除ReadRootMotion后漏ReadCharge、X完整逐key校验及中文注释/布尔规范未完成。主未代改业务；沿原Hub第三轮run-e20cf31b-8aa3-4847-ab71-76c7950709c3返工，截止2026-10-21T11:18:41.507Z。反馈review/rework-successor-3.json及pre-delivery-findings.md，第二轮失败快照review/round-2-failure。新增临时review/prepare_native.py、NativeChargeOnlyValidation.cs、native-worker/、native-inputs.json、native-charge-only.log，专用副本只写_temp且不动正式Unity。audit_assets.py首次编码名utf8-sig错误已纠为utf-8-sig后48通过，属于验收夹具失败，未改业务来适配。

后继首轮在2026-10-21T10:32:44Z到期仍无交付。主仅取消该过期原生执行，核completed/isProcessing=false后官方service recover；沿原Hub返工第二轮run-8910d41a-0940-4173-9c5f-0a5366b2ab6e，截止2026-10-21T11:04:37.218Z。2026-10-21T10:41Z再核原生planning/isProcessing=true、无权限或其它待输入，当前仍执行，不新建并发任务。

中间组件源码隔离14断言已通过：真实MSkillChargeData与MDisplacementComponent，0/90度两轴、峰值回拉、同帧一次消费、末点、中断及迟到基线。源输入摘要与日志在review/displacement-runtime；仍需末稿冻结后复测，不把该结果当全任务通过。完整120Hz数组已出现（16/236/300/372/380/80步）；新增独立临时review/audit_trajectory.py只读核正式曲线的全样本、初始零累计、窗口、作者Runtime一致与峰值上限。review/prepare_review.py改作者包→客户端→Editor的本轮隔离引用顺序；review/hub_call.py仅精简反馈输出，原始回执保留。round2反馈review/rework-successor-2.json、原生过期停止证据review/native-expired-stop.json一并保留。

本节以上保留各轮中间状态，最终结果以下方“末稿独立验收”覆盖；中间失败均沿原任务返工，Codex未接手代改业务。正式Unity Play、服务、DLL和场景不主动改动，不将隔离证据扩写为正式现场。

既有无关热更DLL、UserSettings、构建清单、客户端目录内Docs/AI及分析/日志、VisualScripting生成物和hfs临时文件均保留，不混入暂存。

## Git

实现提交：`d45df25ee1039bdffbf1ea180b4768ec99cee397`，中文说明“feat: 按RO配置曲线统一技能位移并移除采集器”；范围为基线1e2e084后的上述精确33路径。已检查暂存清单，无无关用户文件；正常git push成功，远端origin/codex/feature-development核同SHA。没有main合并或推送、没有强推。

随后仅修改三份收尾记录：`Docs/AI/CURRENT_TASK.md`记录实际提交和原桌面任务accepted；`Docs/AI/TEST_REPORT.md`补Git/验收回执；`项目修改日志/2026-10-21-RO-CHARGE-ONLY-CHANGELOG.md`记录本节及临时集成产物。中文文档提交的身份以本文件所属Git提交为准，属于同一任务记录收尾；没有业务代码再改。

## 末稿独立验收（2026-10-21）

第四轮真实completed/holdsCapacity=false，run-539d1fab-f51b-47e7-8fa7-ba27a15827a7；在既有授权内归一六作者原未执行的Program默认字段，恢复两处预检的旧表/SourceClip动作时长上限，并补中文注释与至多三项布尔表达式。末轮仅纠正SkillEditorAudioMotionTools.ReadCharge与MArtSkill程序段注释、开发报告3001002实际238键；没有重采样、重写轨迹或扩大功能。只读审查未发现阻塞性功能问题。

| 独立检查 | 实际结果 | 证据（均位于_temp/ro-charge-only-20261021/review） |
| --- | --- | --- |
| 作者包→客户端→Editor依赖重建 | 59/289/8源，均0错，既有警告2/38/2 | prepare_review.py，compile-*/build.log、result.json、compiled-inputs.json |
| 14资产非位移字段保护 | 48/48，动画/音效/FX保持 | audit_assets.py、asset-audit.json |
| 六技能完整120Hz有符号X/Z轨迹 | 174/174，零起点、帧率、窗口、倍率、两侧一致及峰值；最大样本点误差约9.17e-7米 | audit_trajectory.py、trajectory-audit.json |
| 真实位移源码隔离回归 | 14/14，0/90度、回拉、单次消费、结束、中断、迟到基线、旧仅Z | verify_displacement.py、displacement-runtime/inputs.json及runtime.log |
| 原生Unity真实序列化、预检与发布 | 58/58，主进程退出0；八表现校验、六表现Export后对照、作者字节保护 | prepare_native.py、NativeChargeOnlyValidation.cs、run_native.py、native-inputs.json、native-charge-only-result.json、native-exit.json、native-charge-only-final.log |
| 最终输入与无关文件保护 | 提交前核正式源与验收输入SHA、13份无关文件、功能分支和精确33路径 | final_inputs.py、final-input-verification.json、stage-paths.json |

真实采样16/236/300/372/380/80步，六根旋转均零；3001002每轴实际238键，其余各17/301/373/381/81。滑步旧倍率仅计入距离一次，前向终点4.999852847米；R前向保峰值4.400617米再回拉，E保约-0.896米横移。误差针对本轮120Hz样本，未推导正式运行或连续原动画的量化收益。

未完成边界：未启停正式Play/服务、替正式DLL或保存场景/预制体，正式真实碰撞和双端同步待正常加载后实测。时间轴Program移动预览未扩展，不能把运行两轴支持写成预览也支持。没有新增正式测试组件。原生临时工程只读复制180份当前输入，所有导出写入_temp专用副本。没有恢复采集器或增加第二次碰撞入口。

新增临时辅助逐项：review/complete_records.py汇总五份正式记录；review/rework-successor-5.json仅注释/报告勘误；review/final_inputs.py增加真实组件输入SHA核对。WorkBuddy本轮临时修正为workbuddy/fix_round3_assets.py、repair_round3_indent.py、round3-asset-fix.json、fix_round4_assets.py、round4-asset-fix.json；另有ChargeClipSampler.cs、clip-samples.json、compile_round3.py、verify_displacement_round3.py、development-report.md及各compile-*/displacement-runtime构建输入输出。完整本轮与旧采样目录临时文件清单单独保存review/temporary-file-manifest.json，仅作证据，不提交、不清理。

根协作与专题五份文件已按末稿结论更新。完整正式逐文件清单见上表；沿用资源和目录.meta没有修改，仅删除两个类型对应.meta。Git提交和推送以随后实际回执为准，主统一集成，任何普通提交均不代表获准合入main。


## Git后续记录与最终交接

Hub末轮run-89e03f5a-bfed-49f9-a88d-c6094416fa59沿原会话完成，实际completed/review=accepted/holdsCapacity=false。验收接受依据是Codex独立检查和只读审查，保留正式Play、真实碰撞、双端同步和时间轴预览边界。接受请求review/accept-request.json，真实回执hub-accepted.json；实现推送与远端一致性git-code-push.json。

集成临时新增逐项：review/stage_verified.py按冻结SHA只暂存33路径；staged-paths.json为实际清单；commit-message.txt为中文实现提交正文；record_git_result.py在真实推送后汇总三份记录；git-code-push.json为提交/推送/远端一致性；accept-request.json和hub-accepted.json为原任务验收；最终记录提交正文docs-commit-message.txt。所有这些均只保留_temp，不提交或清理。temporary-file-manifest.json已在收尾补充新增路径。

下一步由正常加载后的正式现场验证六技能完成/打断、贴墙碰撞及双端同步。无需重新挂SkillRootMotionCollector；运行通过配置ChargeList。专题见`项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md`，本次真实难点与失败取证见`项目分析/项目难点.md`。
