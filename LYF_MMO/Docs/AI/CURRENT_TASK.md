# 当前任务

- 状态：已完成。
- 需求：说明 `MTimerMgr` 从哪里更新时间，并在何处执行到期回调。
- 本次新增文档：`项目分析/MTimerMgr更新时间与回调链路分析.md`。
- 未修改业务代码、配置、资源或场景。
- 下一步：若需追踪某个具体技能回调，可从其 `SetTimer` 注册位置和 `TimerEventHandler` 委托继续定位。

## 2026-10-21 SkillRootMotionCollector 解析

- 状态：完成静态解析。
- 需求：解释当前脚本的用途及其与技能、模型、移动组件的关系。
- 新增：`项目分析/ro项目解析/SkillRootMotionCollector作用与调用链.md`、该子目录 `README.md` 及 `项目修改日志/2026-10-21_SkillRootMotionCollector解析.md`。
- 修改：本记录及 `Docs/AI/TEST_REPORT.md`，追加本次协作和验收信息。
- 验收：确认原生 Animator 采集、技能窗口筛选、移动组件消费与碰撞提交链路；业务代码无改动。
- 未解决事项：未运行具体技能验证动画位移与碰撞表现；本次源码用途说明已完成。
- 交接：如继续排查某个技能的实际移动距离，应结合该剪辑根运动和 RootMotionSegments 配置。
