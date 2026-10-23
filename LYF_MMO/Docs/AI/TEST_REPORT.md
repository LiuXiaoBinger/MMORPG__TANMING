# 测试报告

- 任务：`MTimerMgr` 更新时间与到期回调链路解析。
- 静态验证：已核查 `Global.Awake` 初始化、`Global.Update` 调用顺序、`MTimerMgr.Update` 到期循环及 `MArtSkill.SetTimer` 注册用例。
- 结果：调用链可闭合；本次没有业务代码改动，因此未执行 Unity 编译或运行时场景测试。

## 2026-10-21 SkillRootMotionCollector 静态核查

- 已读取目标脚本及 `MModel`、`MArtSkill`、`SkillRuntimeRootMotionSegment`、`MMoveComponent`、`MEntity` 相关代码。
- 确认：采集原生 Animator 水平位移，按技能原动画时间窗口缩放；消费后清零，最终由模型移动入口提交。
- 确认：本地移动消费段位于生效的 `#if true` 分支；模型存在且启用 CharacterController 时执行碰撞移动，否则直接改位置。
- 修改文件：分析正文、分析子目录 README、本次修改日志、CURRENT_TASK 和本报告，均为 Markdown。
- 验证边界：只做源码静态核查和文档格式检查，无业务代码改动，不执行编译或场景测试。
- 未解决事项：具体技能距离、动画更新时序及碰撞表现未经运行时验证。下一步按用户指定的技能复现核查。
