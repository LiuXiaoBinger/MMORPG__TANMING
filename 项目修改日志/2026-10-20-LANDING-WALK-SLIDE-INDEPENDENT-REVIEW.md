# 跳跃落地后待机平移：独立测试与审查

- 任务：用户跳跃后再按方向，角色已经位移但迟迟才切走路。测试角色只验证与审查，不修改业务；主 Agent 统一记录四入口与难点。
- 范围：唯一业务差分为 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs` 的 Update 新增 16 行；默认动作、移动、Animator、状态机四份修前冻结源逐字未变。修改思路与全文见 `项目分析/LANDING_WALK_ANIMATION_HANDOFF.md`。

## 独立复现与修后验收

- 顺序读取四协作入口、测试/审查职责，冻结 MJump、Default、MMove、MAnimator、MStateMachine 修前五源。所有夹具、编译产物与证据位于 `_temp/landing-walk-slide-20261020/review/`，不挂正式场景、不写测试到业务。
- `python _temp/landing-walk-slide-20261020/review/verify_landing.py before`：生产方法逐字提取后编译成功，32 项修前复现通过。8 组合为本地/远端 × 走/跑 × 普通/战斗。每组先无方向落地，之后才改变方向；MMove 已进入 IsMoving，但默认动作被落地保护挡住，实际生产 MStateMachine.LateUpdate 把本次触发消费；直到完整落地时长过去才恢复移动动作。
- `python _temp/landing-walk-slide-20261020/review/verify_landing.py final`：59 项修后通过。相同 8 组合下一逻辑帧清落地标志并经真实 LateUpdate 提交 Walk/Run/FWalk/FRun；无重复提交、无重复落地声、松键回待机。另含静止落地完整结束、本地残留 IsMoving 但无方向不打断、落地瞬间已有方向保原一次声、空中方向不盖起跳/循环、落地再跳、技能接管不 ForceDefault、远端 0 段先落后移动包、同帧点按松开保持落地。
- 专项提取 25 份生产方法/属性，含 MJump.Update/OnJumpEnded、MMove.onMoveEvent/onSyncMoveEvent/SetMoving、MainCity.SyncMoveHandle、Default.PlayAnim 及 MStateMachine.LateUpdate。动画 Play/GetAnimInfo、模型、向量、碰撞、基类播放等为薄记录器。夹具阶段长度 0.2 秒只用于验证等待与取消时序，不是正式落地剪辑时长，也没有原生 Animator 或正式双客户端画面验收。
- 首轮夹具远端通知误写 IsMoved=false，按实际字段语义修为 IsMoved=moving 后重跑；未为夹具改业务。原首次同名 runtime 日志被后跑覆盖，只保留会话输出及如实说明 `_temp/landing-walk-slide-20261020/review/fixture-first-attempt.txt`，不称完整首失败日志留存。

## 编译与审查

- 独立 `python _temp/landing-walk-slide-20261020/review/build_client.py`：280 份正式客户端源 Rebuild，0 错误、39 条警告；现有独立构建依赖版本警告与旧未用成员警告，不将本次视为零警告。日志 `client-build.log`，完整编译输入 `compiled-source-hashes.json`。
- 末稿 SHA256：`96609f87aed28d589d584b9928c5cdf79c11bdefd526a5f964fdddd9385302d5`。编译 280 输入、专项输入全保持，14 项审计全真，见 `final-audit.json`、`review-source.diff`。
- 修改在原技能早退之后、待播跳跃之前，仅落地阶段且移动运行态有效时取消落地，本地继续要求真实方向、远端沿同步移动标志。复用 ClearJumpPresentation/ForceToDefault/MarkTriggered，无新字段、方法、门禁、协议、队列或定时器；无三目/foreach/namespace，新增条件每表达式最多三项，注释说明用途与数据语义。
- 问题清单：Block/Critical/Warning 均无；Info 为原生及正式客户端画面未验收、末稿尚需按原流程热更生效。结论：本轮最小修复与隔离验收通过，不声称正式双端运行已通过。
- 不部署/重启服务、不发布正式资源、不更新 AOT 协议、不停止用户 Play、不清理临时。用户此前主包与热更不匹配是另一问题，本轮没有新增协议接口。
