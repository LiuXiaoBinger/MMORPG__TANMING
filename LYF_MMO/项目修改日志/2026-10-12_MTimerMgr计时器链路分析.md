# 2026-10-12 MTimerMgr 计时器链路分析

## 修改文件

- `项目分析/MTimerMgr更新时间与回调链路分析.md`
- `Docs/AI/PROJECT_CONTEXT.md`
- `Docs/AI/CURRENT_TASK.md`
- `Docs/AI/DECISIONS.md`
- `Docs/AI/TEST_REPORT.md`
- `项目分析/README.md`
- `项目修改日志/README.md`

## 修改内容和设计思路

根据用户对 `MTimerMgr` 更新时间和回调位置的提问，追踪 `Global.Update`、`MTimerMgr.Update`、`SetTimer` 与 `MArtSkill` 的注册调用，归档全局时钟推进、回调执行顺序、暂停和取消语义。

详细说明见 `项目分析/MTimerMgr更新时间与回调链路分析.md`。

## 验证结果

- 静态检索确认项目内唯一的 `MTimerMgr.singleton.Update` 调用位于 `Global.Update`。
- 核对 `MTimerMgr.Update` 的到期条件、移除顺序和委托调用逻辑。
- 未改动业务代码，未执行 Unity 编译或运行时测试。

## 未解决问题

- 本次没有未解决的实现问题；若需验证特定技能的表现回调，需要在运行中的 Unity 场景中触发对应技能。
