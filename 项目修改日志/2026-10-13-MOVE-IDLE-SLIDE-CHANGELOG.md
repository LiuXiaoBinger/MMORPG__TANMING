# 本地及远端角色待机平移

日期：2026-10-13。

用户说明本地A偶发待机平移，B看A也有。按职责由客户端开发负责最小业务修改，独立审查测试，主统一读取当前Unity现场；不改协议/服务器或泛重构状态机。

当前真实现场存在两个玩家实体，均绑定控制器；本地默认动作期待FIdle但实际MAnimator停留Skill04且无触发，远端FIdle一致。本轮尚未在移动帧捕获待机平移，不把静止不一致作为完整问题结论。

参考RO核实后摇延期到期有MarkTriggered回调，本项目对应timer被注释且仍直接返回，实际会丢失默认动作播放。拟最小恢复正常PlayAnim，不引入新等待器。继续定位用户移动帧症状与公共动画链路。

当前修改文件为 Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md 及本日志；业务范围待开发提交更新。证据统一 _temp/move-idle-slide-20261013，尚无本轮末稿通过结论，保持当前Play、不保存正式场景或发布资源。

## 最终修改与设计

- LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs：InState沿RO比较现有_currentState最新Play/CrossFade目标。停止向Idle过渡时再次移动，旧实现读仍在采样的Walk源而短路，导致未撤销Idle过渡；改后重新发起Walk/Run，本地和远端共用这一逻辑。不新增字段或状态框架。
- LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs：删除没有Timer调度却提前返回的后摇等待分支及失效空方法/常量，正常提交默认动画。保跳跃/落地保护、走跑选择与速度/脚步声；这是现场Skill04停留对应的另一真实故障，不把它混称快速停走主因。
- Docs/AI/CURRENT_TASK.md、TEST_REPORT.md与本日志记录末稿验收及未验边界；独立日志 项目修改日志/2026-10-13-MOVE-IDLE-SLIDE-INDEPENDENT-REVIEW.md。前轮MainCity源码SHA保持75A4…，未改协议/服务/资源或旧移动组件。

## 验证结果与未解决边界

开发/独立各280真实源Rebuild0错、38/39既有警告。独立5关键方法逐字生产原文、before30项复现/回归、final30项通过：本地/远端、走跑、战斗8组合快速停走，以及正常停止、同状态去重、技能恢复和跳跃落地。主565条编译/运行输入SHA全一致、两源差分及独立final-audit PASS。不累计早期27项。

原生角色探针前两次缺临时Ctrl/表现表，后用户退出Play并构建资源，后续MCP未返回有效数据；原生Animator与正式双号画面尚未验收。记录 _temp/move-idle-slide-20261013/root-native-attempts.txt，未把薄边界30项包装成原生验收。未主动停止用户构建或发布热更；待末稿生效后在双客户端反复停走验证，当前构建开始于修改期间，不能默认包含最后源码。失败与临时文件全部保留。

末次收口仅移除修改范围行尾空格，Default最终SHA BB7966F721F650C67134B7D004FCCCDA0D1418230A5EC289076D2D521D7010AC，MAnimator仍3AAE49F3C58E670A71D73B0DF680F338B5725312A7C8E914D34CB2BB4BF78BF2。开发/独立重新编译、既有30项及逐字抽取audit通过，主565条最终SHA重新核对全一致，diff--check0，未用空白前清单代替末稿。客户端职责记录 项目修改日志/2026-10-13-MOVE-IDLE-SLIDE-CLIENT-CHANGELOG.md，专题 项目分析/MOVE_IDLE_SLIDE_ANIMATION.md。
