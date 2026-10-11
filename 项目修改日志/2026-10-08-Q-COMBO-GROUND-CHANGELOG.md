# 连续 Q 普攻下沉后复原修复

- 需求：连续使用 Q 普攻时人物短暂掉到地面下方，然后立即恢复。
- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`、本记录。
- 确认缺陷：RO 的 MAnimator.CrossFade 在当前即目标状态或过渡中直接 Play，其余使用 CrossFadeInFixedTime，过渡参数为秒。本项目改前无条件 Animator.CrossFade，对三段 Q 共用的 Skill 槽再次建立自过渡，而且把 0.1 秒当成归一化时长。三段约 2 秒的动作因此并非 0.1 秒过渡。该代码缺陷明确；尚无真实下沉画面证明全部原因都来自该缺陷。
- 实现思路：仅修改 CrossFade 两重载，恢复 RO 的三条分支；沿用当前完整状态路径解析。四参接口的归一化起点在固定时间 API 中按实际 ready 剪辑长度换成秒，无 ready 时取控制器原始剪辑，避免使用准备中新资源时长。所有当前业务调用使用默认零起点。
- 生命周期：既有冷加载原生剪辑行为、OnPreparedClipPromoted → PlayCurrent、对象池和延迟释放逻辑保持，不新增字段、状态或每帧回调。
- 资源保护：用户既有动画、场景、原表和 Q 三段 700ms 接段窗保留。用户修改过的 Atk02 SHA256 与本轮开发前基线相同：`6C903E088007CE2FFF3B15BA5611249D80182761C67F1593FA25135864DB2870`。未修改物理高度，未增加锁 Y、输入缓存、角色生成、模拟键盘或正式测试组件。
- 协作：q_ground_develop 开发，q_ground_review 独立审查，q_ground_test 独立验证；失败项交开发返工。
- 验证结果：开发与独立 GM_Game 隔离 Rebuild 均 0 错误；独立静态审查 PASS；真实 MAnimator 与兼容适配源码链接薄 Animator 边界回归 40 项断言 PASS，覆盖同槽接段、过渡中重播、固定秒过渡、非零起点、旧 ready/占位、异步恢复和兼容路径。目标差异空白与规范检查通过。全部编译/临时验证产物置于 `_temp/q-combo-ground-20261008`，未覆盖正式程序集。
- 证据：`_temp/q-combo-ground-20261008/develop-result.md`、`_temp/q-combo-ground-20261008/independent-code-review.md`、`_temp/q-combo-ground-20261008/test/independent-test-report.md`、`_temp/q-combo-ground-20261008/test/animator-boundary-result.log`。独立编译共 37 个诊断（35 C#、1 MSB3277、1 隔离 USG0001），无错误。修复源码 SHA256 为 `A6854F0E0A69B4321F9980F724234E5210DEC1F1384BDB4B0AA317A5D80412AB`。
- 未解决与下一步：当前 Unity MCP 不可用；真实角色/脚骨世界高度及 Game View 连续 Q 下沉前后画面未复验。正式 Unity GM_Game.dll 时间 10:05:22 早于修复源码 10:30:22，尚无当前 Editor 采用修复的证据。待 Unity 正常刷新编译后使用现有角色手动复验；若仍发生，再分别测实体、模型和脚骨高度并交开发返工，不把本次纯分支验证当作最终画面证明。不另开同工程 Editor，不清理临时产物。
- 相关协作记录：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。

## 当前播放链说明补充

2026-10-08应用户询问，只读核对按键、状态机、MArtSkill特效时间轴和当前配置；未改业务、资源或原表。结论写入 Docs/AI/CURRENT_TASK.md 与 Docs/AI/TEST_REPORT.md，并在对话说明。此核对未做运行测试，不改变上一轮画面待复验边界。

## 旧事件用途核对补充

只读核对确认clip.events消费循环是本项目旧兼容而非RO特效主链；RO使用FxList/At与MTimerMgr。当前10070仍依赖旧事件，直接删除会丢失蓄力/落剑。仅维护Docs/AI/CURRENT_TASK.md和TEST_REPORT.md用途核对记录，未修改代码/资源或做运行验收。
