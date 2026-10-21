# RO 气墙施法检查只读核对

- 用户询问CheckWall是否防止穿模；读取MAirWallMgr.CheckWall和场景气墙组件的矩形/圆形检测。
- 结论：第三参数true选择技能规则，检测登记气墙的跨墙攻击开关IsCrossAtk，阻挡时取消请求；不是通用建筑碰撞或角色穿模修正。
- 修改文件：项目分析/RO_AIR_WALL_SKILL_CHECK.md、Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md与本日志，只有说明文档，业务/参考工程不改。
- 验证：源码调用参数、返回分支与检测范围核对；无运行或真实地图墙体测试，不自动实施移植。
