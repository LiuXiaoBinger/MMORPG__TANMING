# 2026-09-27 RO 技能启动与统一动画入口整改

## 需求与原因

用户指出此前 `MSkill.Begin` 内直接调用 `TryPrepareAnimation`，并在 `Update` 重试，没有遵循 RO 的统一动画接口。对照源码确认 RO 使用 `Fire → Execute → Start` 启动技能表现，动画由 `MStateMachine.LateUpdate → MSkillComponent.PlayAnim → MSkill.TriggerAnim` 播放。应按原接口职责整改。

## 修改范围与分工

- 客户端开发子 Agent：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`、`MArtSkill.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`。
- 测试子 Agent：`_temp/ro-skill-pool-tests` 既有纯内存夹具，编译与回归产物仅在根目录 `_temp`。
- 独立审查子 Agent：只读核对 RO 接口职责、统一动画调用顺序和生命周期；失败打回开发。
- 主 Agent：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`、`项目分析/RO_SKILL_SUBTYPE_ALIGNMENT.md` 和本日志。

## 设计

技能父类采用 `Fire`、`Execute`、`Start` 和 `TriggerAnim` 职责，移除自创 `TryPrepareAnimation`。`Fire` 初始化一次施法并启动子类表现；`MArtSkill.Start` 持有原有 10070 特效事件资源；动画覆盖及播放只由状态机调用技能组件的 `PlayAnim` 触发。使用既有异步动画加载及退出后释放机制。

参考细节统一记录在 `项目分析/RO_SKILL_SUBTYPE_ALIGNMENT.md`。原表、原资源、Q 三段/700 毫秒状态窗、UUID、实例列表和池生命周期需要保持。飞行物暂不开发。

## 验证与交接

开发及独立静态审查通过。客户端与独立构建均 0 错误、42 条既有警告，独立产物 `_temp/ro-skill-animation-interface-review`。目标范围旧接口和禁语法扫描无命中，差异空白检查通过。

正式源码链接夹具执行 `dotnet run --project _temp/ro-skill-pool-tests/SkillPoolTests.csproj`，退出码 0，189 项断言通过，日志 `_temp/ro-skill-pool-tests/result.log`。原 171 项已迁移 Fire 接口；新增确认 Fire/Update 不安装或播放动画、组件 PlayAnim 只覆盖并播放一次且不调用基类 Idle 回退、延迟触发按归一进度 Play、旧实例退出后不能重新占槽。Q 状态窗、对象池和 Skill04 既有回归保持。

真实状态机 LateUpdate 的模型未就绪保留触发标记通过静态核查；运行夹具使用模型、Unity 和资源边界替身，不能代替实际画面验收。未运行 Unity Game View、真实异步资源或四服联调，用户继续在已有模拟角色手动按 Q/E/R/F/1 验证。没有新增测试角色或按键探针。

本轮按接口职责对齐 RO，参数仍使用本项目已有的强类型字段；未移植无配置的 AnimList、战斗坐骑等表现。技能 1 的已有无目标落点、有限粒子尾迹等边界保持，详见 `项目修改日志/2026-09-27-RO-SKILL-SUBTYPE-CHANGELOG.md`。
