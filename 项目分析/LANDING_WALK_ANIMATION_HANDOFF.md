# 落地后新移动输入没有及时切走路

## 问题与根因

2026-10-20用户反馈：跳跃后才按方向键，角色先保持待机般姿态平移，过一段时间才播放走路。需区分两种时序：落地瞬间已经有方向输入、无方向落地后再开始移动。上一轮跳跃同步验证覆盖前者，未覆盖后者，不能用原通过结果证明本轮没有问题。

`MJumpComponent.OnJumpEnded`只在落地事件发生瞬间检查方向。无输入时进入`_isEnding`阶段并播放Jump_End；真实`LYF_MMO/Assets/GM_Game/Resources/ResInfo/animInfo.txt`中该剪辑长度为0.83333343秒。

随后新方向输入经`MMoveComponent`记录并开始水平位移，也会触发默认动作刷新。然而`MDefaultActionComponent.PlayAnim`仍因落地保护早退；`MStateMachine.LateUpdate`消费本次刷新标记。原`MJumpComponent.Update`只等待整条落地动画时长后才再次交还默认动作，因此表现切换比移动晚。这里所谓待机平移是用户可见姿态描述，尚无原生采样证据证明这段实际播放的是Idle剪辑。

## 最小修复

仅修改`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`的Update。在原技能优先分支之后、待播落地动画及阶段计时之前，若处于落地阶段且已开始移动，则结束原跳跃表现并重新触发默认动作。本地主角同时读取现有方向输入与移动状态，远端按同步移动状态。

先清除落地保护和待播项，再标记默认动画，下一次正常帧末刷新即可选择Walk或Run。静止落地仍保完整落地动画；空中起跳/循环不受此分支影响，技能接管仍优先。不重播落地声、不缩短资源剪辑，不修改移动积分、协议、服务端、AOT接口或正式资源。

## 验证与生效边界

开发与独立测试分别保存本轮修前源和编译输入，证据统一为`_temp/landing-walk-slide-20261020/`。修前32项断言复现“无方向落地→再开始移动→旧保护挡住默认动作→等待剪辑结束”，修后59项通过。覆盖本地/远端、普通/战斗、Walk/Run组合，以及静止落地、残留移动标志、落地瞬间已移动、空中/再跳、技能接管、远端先落地后移动和点按松开。断言数不代表独立场景数量。

独立提取25段生产方法，包括实际StateMachine.LateUpdate与Default.PlayAnim；真实Protobuf连接通知和移动入口。动画播放、实体宿主、向量和碰撞为薄边界，夹具默认阶段长度0.2秒，证明前后时序，不作正式0.83333343秒动画采样或实测延迟。初次夹具IsMoved误填已改，仅影响测试；首次同名日志已覆盖，失败事实保留fixture-first-attempt.txt。

开发/独立280源Rebuild分别0错38/39警告（独立额外CS8032为生成器依赖）；独立14项审查PASS。主核283去重输入SHA一致、单文件16行新增、其余相关源保持，末稿和差分证据为root-final-validation.json。独立日志`项目修改日志/2026-10-20-LANDING-WALK-SLIDE-INDEPENDENT-REVIEW.md`、开发日志`项目修改日志/2026-10-20-LANDING-WALK-SLIDE-CLIENT-CHANGELOG.md`。

本轮可用工具未提供Unity MCP，无法原生现场采样或验证打包后的双端画面。隔离生产方法测试不替代真实Animator、物理接地或正式发布验收。当前只改热更业务源码；已生成的旧客户端/热更包不会自动更新，需按原热更构建及资源更新流程生效。本轮没有新增需要再次修改Network主包接口的内容。

来源：`项目修改日志/2026-10-20-LANDING-WALK-SLIDE-CHANGELOG.md`；前序专题`项目分析/JUMP_ANIMATION_SYNC.md`、`项目分析/PACKAGED_CHARACTER_MISSING_AOT_PROTOCOL.md`。
