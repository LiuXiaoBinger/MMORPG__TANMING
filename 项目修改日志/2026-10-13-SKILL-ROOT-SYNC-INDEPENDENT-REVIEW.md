# 2026-10-13 技能根运动不同步：独立审查与测试

## 修改文件与原因

- 用户说明A释放技能时本地根运动正常，B只播放动画，动作后才补移。本轮业务仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs` 的 `Update`。
- 修前 `MArtSkill.TriggerAnim` 与 `OwnsRootMotion` 没有本地角色限制，远端同样绑定并采集根位移；但 `MMoveComponent.Update` 的远端分支只有影子追赶与程序位移消费，根采集器从未被消费。禁止移动的技能中影子追赶又直接返回，因此只播动画，结束才追服务器位置。
- RO普通 `LagSyncUpdate` 同样受动作主动移动开关限制；其技能位移通过独立Displacement链路推进。本项目已使用真实Animator根采集，应把当前采集结果接入现有远端实体移动，而非改协议、让禁止方向移动的技能全局可移动或重新实现RO位移系统。

## 设计及返工

- 远端消费动画根与可选程序位移，非零时合并一次 `MoveCharacter`；没有程序组件也能根移动。技能非零位移帧不同时影子追赶同一坐标。
- 使用已有 `_startPos`、`_syncToTargetDir`、`_syncToTargetDist`、`_server_time` 根据碰撞后的真实位置重建剩余追赶段，避免技能结束仍按施法前路程重复推进；没有新增字段、状态、队列、重试或确认流程。
- 独立审查发现首稿精确抵达同步点后 `_syncToTargetDir` 为零、原 `LagSyncUpdate` 提前返回，服务器moved标志留下 `_isMoving=true`。已反馈客户端，仅在现有重建距离为零的分支 `SetMoving(keep_moving)`，消除技能结束后原地保持走路的实际状态遗漏。
- 收口清本次Update内一处旧行尾空白时，曾误去掉Update外SetRunning声明缩进，范围审查发现后恢复。最终Update外逐字保持，无业务范围扩大。
- 独立审查员没有修改客户端或服务端业务，也没有调用Unity MCP、操作正式账号、保存场景、发布热更或清临时文件。

## 验证结果

- 独立当前280份真实客户端源Rebuild：0错误、39条既有警告。与前轮相同独立工程警告签名完全一致（去重36种），没有新增声明警告。编译输入及输出均在 `_temp/skill-root-sync-20261013/review/`，不使用旧DLL替代本轮编译结果。
- 专用测试逐字抽取当前生产 `MMoveComponent.Update`、`onSyncMoveEvent`、`LagSyncUpdate` 以及 `SkillRootMotionCollector.ConsumeDelta`；实体/碰撞提交、Animator归属与数学为薄边界，不是原生Animator或正式联机。修前最终15项复现与回归、修后最终19项全部通过，不累加早期13/16项。
- 覆盖远端有无程序组件的根运动、同一增量只消费一次、根与程序合并单次碰撞、没有根采集器的程序位移、失效技能清旧delta、纯根碰墙不访问空程序组件、程序碰墙停止、假实体与原本地入口保持、零增量不提交、精确抵达后不补移且停止动画标志、仅追实际残差且不越过目标、碰撞裁剪后按实际位置重建残差、MoveCasting允许时非零根帧不双叠、同步目标落后时非零根帧不回拉、普通服务器移动保持。
- 初始夹具把修前未调用ConsumeDelta误设为“已清无效根delta”，产生断言失败；修正为修前真实保留、修后调用并清零的对照断言，不改生产代码迎合夹具。首次原生示例可能每帧调用onSync而与正式大于0.1秒节拍不符，已提示主与客户端避免把人为同步小距离吸附当正式联机结果。
- `final-audit.json` 为PASS：280份编译源SHA无漂移、4个测试方法与当前源码逐字相同、Update外逐字相同、主冻结7个保护文件SHA均保持；`git diff --check` 退出0。MAnimator/Default/MainCity及Collector/MArtSkill/PlayerEntity/SkillCastingRules既有成果全部保持。
- 修改范围无新命名空间、foreach、三目或弱类型业务载荷；新局部变量含具体中文用途说明，新增布尔表达式最多2项。核心根消费顺序与本地已存在流程一致，没有为测试新增正式入口。
- 最终业务SHA：`0DB2686CC687D8BDA7616E88646C75449286C5870CA8CB6ADD128E017E70F0EE`。

## 未解决与交接

- 独立结论：源码审查、编译及实际关键方法专项通过，无待返工业务项。主统一执行的隔离原生测试及真实双客户端画面另按实际结果补充，本记录不宣称已经通过。
- 主30秒正式世界被动采样没有施法，只有静止空槽，不能当作用户症状的现场复现。
- 当前运行程序集或资源包不自动等于本轮末稿；正式双客户端必须让新代码生效，再验技能中同步移动、结束无整段补移和正常移动。没有擅自替换正式热更或重启服务。
- 主负责更新 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`；证据为 `_temp/skill-root-sync-20261013/review/compiled-source-hashes.json`、`before-methods.json`、`final-methods.json`、两份runtime日志与 `final-audit.json`。分析及主日志请参见 `项目分析/SKILL_ROOT_MOTION_SYNC.md` 与 `项目修改日志/2026-10-13-SKILL-ROOT-SYNC-CHANGELOG.md`（由主协调归档）。

## 原生隔离验证补充审查

- 主执行末稿普通脚本刷新后，本独立职责只读检查 `client/native-root-movement-probe.cs.txt`、末次 `native-root-movement-result.txt` 及两份历史attempt。没有再次操作Unity、重跑已通过专项或改业务；当前源码仍为末稿SHA `0DB2686CC687D8BDA7616E88646C75449286C5870CA8CB6ADD128E017E70F0EE`。
- 夹具在Edit PreviewScene实例化正式远端Role_MJS_Other预制体、CharacterController、正式Humanoid Animator/Controller和10080滑步Runtime/原剪辑。反射装配当前有效技能及独立MAnimInfo、40帧显式原生Animator.Update；每帧原生delta已产生但预览场景没有自动采集回调时，反射调用同一生产 `SkillRootMotionCollector.OnAnimatorMove`，之后调用真实生产远端 `MMoveComponent.Update`。不是替身积分、复制根曲线或直接手动修改终点。
- 末次起点实体/原生模型/Animator/CharacterController均为 `(0,50,0)`；OwnsRoot真、CharacterController真、IsActiveMovable假，实际播放剪辑Anim_JX_Slider_F、Humanoid真。40帧均采得有效根位移，原生raw累计 `(-0.026141,0,3.428115)` 米，经正式倍率1.458485后采集 `(-0.038126,0,4.999855)` 米，实际实体与CharacterController位移 `(-0.038126,0,4.999854)` 米，误差 `9.536743E-07` 米；即水平前移约5米。
- pending消费后清零、精确目标点移动态假；退出技能后继续20帧生产移动更新，附加位移0、移动态假，末次结果明确PASS。该证据支持真实Animator产生的水平根增量被远端生产消费链和CharacterController正确提交，以及精确抵达后不重复推进。
- 边界：40帧采集均为显式派发生产回调（manualCollectorFrames=40），不声称证明Edit模式自动回调或正式Play帧调度；影子目标缓存每帧被设为累计采样点，没有执行onSyncMoveEvent、网络、预测完整启动或双账号请求。网络入口与非零残差仍由前述原文方法专项覆盖。预览无障碍物，仅验证CharacterController实际提交，不冒称墙体碰撞或全技能表现通过。
- 第一原生尝试没有自动采集到根位移、没有通过；第二尝试采集约5米但Animator.Rebind后模型/控制器原生起点与实体起点不一致导致偏差，也没有通过。仅调整临时夹具回调派发与统一起点，业务末稿保持；失败证据保留 `client/native-root-movement-first-attempt.txt`、`client/native-root-movement-second-attempt.txt`，不将失败覆盖为成功。
- 主最终 `root-final-hash-check.json` 已只读确认为569条输入SHA全一致、4方法匹配、无缺清单，PASS。本独立结论补充为：末稿源码、编译、关键方法及原生隔离消费链验证通过；正式双客户端/安装包生效后的技能画面仍待现场验收，不扩大到发布或网络时延验证。
- 清理边界补充：原生断言完成后Edit夹具world.Dispose间接触发生产Play模式Object.Destroy，留下1条旧控制台错误；仅修_temp夹具为先DestroyImmediate后Dispose，主真实MGameObject清理小验证错误数1→1、无新增，见 `root-preview-cleanup-error.txt`、`root-preview-cleanup-result.txt`。不修改正式清理逻辑、不清旧消息，原生位移断言保持有效，但最终Console不称0错误。
