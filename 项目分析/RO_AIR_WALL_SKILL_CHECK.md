# RO 气墙施法检查用途

- 参考MoonClient/Scene/AirWall/MAirWallMgr.cs的CheckWall(from,to,checkSkill=false)，及Components/SceneTriggerObject/MSceneTriggerAirWallComponent.cs。
- 本次施法调用第三参数true表示检查技能跨墙资格。遍历已登记的气墙场景触发对象，按阵营关系是否适用、起点是否在墙内筛选，再检查矩形/圆形区域阻挡；碰到不允许跨墙攻击的IsCrossAtk=false气墙则返回false，调用方立即返回不发送请求。
- 这负责防止隔气墙施法，不移动角色、不修正身体位置、不检测角色模型穿进建筑或动画武器穿模。普通移动同一接口使用checkSkill=false，矩形墙还检查双向/单向通行规则；两套资格不同。
- 没有用Physics.Raycast遍历所有建筑Collider，仅检查管理器登记的气墙。普通场景墙体没有登记为气墙时，不能据此宣称全部隔墙攻击被阻止。
- 本轮只读核对，不移植业务、不开新玩法，不做编译/运行验收。记录：项目修改日志/2026-10-13-RO-AIR-WALL-ANALYSIS-CHANGELOG.md。
