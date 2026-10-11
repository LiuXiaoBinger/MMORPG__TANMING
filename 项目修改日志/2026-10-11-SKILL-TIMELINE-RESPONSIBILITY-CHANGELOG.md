# 2026-10-11 技能时间线职责纠正

当前状态：验收完成，独立审查与测试PASS，无剩余代码阻断。

## 需求

用户要求严格对照 RO：技能表现由 `FireEvents` 注册 `MTimerMgr` 回调，到期回调直接执行；`MArtSkill.Update` 不应继续调用 `FireEvents`、`BindRootMotion` 或两条 `Drain`。用户同时要求检查 RO 动画根运动/位移的实际职责。

## 角色与最终修改文件

主 Agent 负责拆解、协调、验收与协作记录；`ro_timeline_responsibility_develop` 执行业务修改，`ro_timeline_responsibility_review` 独立编译和测试审查，`ro_root_motion_reference` 只读源码取证并按主指令机械写入记录。

最终业务范围为四份既有文件，开发与独立保护核对一致：

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimePresentationCache.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/Global.cs`

本轮文档同时修改：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`、`Docs/AI/PROJECT_CONTEXT.md`、`Docs/AI/DECISIONS.md`、`项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md` 与本日志 `项目修改日志/2026-10-11-SKILL-TIMELINE-RESPONSIBILITY-CHANGELOG.md`。所有文档由主确定内容后机械写入，业务仅由开发 Agent 修改。

## 设计思路与保护

RO 真实 `MArtSkill.Update` 只保留持续旋转与基类更新；`Start` 调用 `FireEvents` 注册，计时器经过 `OnProcessTimer → InnerProcessTimer` 直接执行 fx/audio/charge。主在本轮承认上轮职责迁移不完整：防重guard避免重复票据，却没有完成注册、pending消费与根绑定职责的迁移，本轮实际纠正该问题。

`MArtSkill.Update` 现仅base.Update与PruneAudio回收播完声源。Start/CacheReady承接一次FireEvents注册，时间以本次原施法累计秒数为基准；MTimerMgr到期回调InnerProcessTimer执行声音/粒子。真实MModel.FinishLoad在模型/动画/声音/根采集和外部回调完整收尾后发布PresentationReady，恢复已到期pending。Reset解除缓存/模型监听及计时票据，UUID/施法代次、缓存记录身份与模型session保护同步零秒再入、同UUID池复用及旧加载完成。

缓存暖终态同步、冷终态一次、失败空值结束等待。Global取得包后NotifyResourcePackageReady一次启动已有记录；没有技能或其它Update轮询。TriggerAnim/模型就绪登记当前根所有者，真实clip/配置/当前槽门控仍在Collector，Collector→MMove位移与碰撞两层本轮字节保持。

RO `ECharge` 发布配置曲线/距离位移事件，由独立实体 `MDisplacementComponent` 持续计算。它不等于本项目 `Animator.deltaPosition` 根运动；本轮保留既有 `SkillRootMotionCollector → MMoveComponent` 的水平增量及统一碰撞提交，不新增完整 Charge 业务、不改服务器结算。

上一轮 `TimelineDurationSeconds` 与原动画时长独立、自然 End 保尾、`Interrupted` 真清理契约保持。用户作者/Runtime 配置、动画、音频、特效、场景、meta/GUID 和所有未提交修改均受保护，不回退、不自动重导制作资产。

## 最终验证与未解决点

开发57项通过；独立94/94通过，包括45项最新后动画生命周期在当前源重新验证、49项完整真实MModel/Cache职责专项，未沿用上轮85项充作本轮。267完整GM_Game源Rebuild 0错/37既有警告，26完整生产运行链包含真实MModel/Cache、技能组件、计时器、工厂/池、声音/根采集。

验收覆盖单独驱动Timer消费、真实资源/模型Ready不依赖技能Update补发、冷/暖/失败终态、包迟到通知、取消与同步再入、同UUID旧listener拒绝、模型Load/Reset/reload/异常隔离、clip/config根门控、Q切段及自然End后F45/F46与完整声尾、20秒长音和Interrupted立即清理。缺固定挂点粒子pending保持，声音独立，带齐层级模型reloadReady补发已验；动态挂点无正式入口，本轮不扩新轮询。

当前保护2788路径基线（历史2786清单补纳本就存在MTimerMgr源/meta），2784原字节保持/4计划既有源变化，无新增/删除/未计划改变；无新GUID。四源开发manifest与独立冻结、当前SHA主支持重算一致，26运行源再核0差异，267编译输入独立核0差异。两独立报告PASS主已读取，无剩余代码阻断。两个夹具失败仅_temp补magnitude或修Q取消入口，失败/后续通过记录均保留。

未执行正式Unity GUI、设备听感、PlayMode、真实网络/服务器及Bundle发布；本轮未检测MCP，不引用旧无实例结果。原生对象、Animator状态/delta和YooAsset交付采用薄边界，声音结束Stop交付，不宣称实机体验已验。未改用户资产/现场，未自动重导。下一步使用原正式入口体验画面与声音，不扩大已通过隔离测试；临时资料保留，由产品经理确认后清理。

最终证据位于 `_temp/skill-timeline-responsibility-20261011/`：`review/independent-review.md`、`review/independent-test-report.md`、`review/runtime-final.log`、`review/GM_Game-build.log`、`review/independent-protection.json`、`review/source-consistency.json`、`review/verified-source-hashes.json`、`develop/final-manifest.json` 与 `root-manifest-check.json`。

## 引用与临时交接

- 当前协作入口：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。
- 原职责解析：`项目分析/RO_SKILL_FX_ANIMATION_ROOT_MOTION_REFERENCE.md`。
- 本轮正式分析：`项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`，保留外部 RO 真实路径/行号、源码片段、有限检查范围及修正前本项目职责，另章记录最终修正与独立验收。
- 原声音与根运动集成：`项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md`。
- 本轮临时资料：`_temp/skill-timeline-responsibility-20261011/`，只读取证报告位于其 `reference/ro-source-evidence.md`；临时保留，由产品经理确认后清理。
