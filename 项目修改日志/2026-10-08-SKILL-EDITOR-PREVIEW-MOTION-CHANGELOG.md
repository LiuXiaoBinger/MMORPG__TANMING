# 2026-10-08 技能编辑器预览位移修复

- 用户反馈：技能编辑器编辑动画时角色直接跑远。
- 修改范围：本项目嵌入 Y 编辑器预览采样、预览实例生命周期，具体文件由开发定位后冻结。原 Y 参考只读。
- 分工：preview_motion_develop 开发；preview_motion_review 独立审查/验证；主 Agent 维护入口和本日志。
- 思路：检查手动动画采样的根运动是否以采样后位置作为新起点、以及克隆角色是否执行运行控制；只修复可证实的预览缺陷。
- 保护：正式角色、原动画/技能表/资源、用户已改资产、Q 三段 1/1/2 特效配置保持本轮前置状态。
- 验证：进行中，证据保存于 _temp/skill-editor-preview-motion-20261008。编译和真实源码边界验证不能替代 Unity 画面验收。
- 交接：结果待开发及独立审查补充；协作入口 Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md。

## 已冻结修改与根因

- 唯一业务源码：LYF_MMO/Packages/com.ethan.act-action-editor/Editor/Window/SkillEditorWindow.Preview.cs。
- Role_MJS 的绑定根对象在预制体中保存 (67.43,19.414,80.668976) 米；旧 EnsurePreviewInstance 直接继承，导致预览远离原点，原 Y 实现也没有处理此坐标。
- 预制体隔离副本归零，朝向与缩放保持；显式时间轴保存角色根/实际 Animator 根姿态，采样前后（含异常 finally）恢复，只在副本关闭自动根运动。身体骨骼与挂点当前动画姿态保留；原 ActionClipAnimator 和正式角色业务不改。
- 反复 ResetOutput/重建图而无基准复位是额外漂移风险，未以原生实測证明它是本次唯一根因。场景源沿用既有实例化姿态，不扩展带父级的世界对齐；旧时间模式保持。Motion 位移轨道此前只绘制参考，仍不驱动预览根运动。
- 最终源码 SHA256：e57a70799716c018128cb97587aa88678416cdcc033e183d831ccb44a70a5175。

## 审查与验证

- 开发隔离 Editor 编译 0 错误、3 诊断（既有 MSB3277 与两个 CS0414）；真实源码四方法边界 74 断言通过。
- 独立 Editor 全源码隔离 Rebuild 0 错误；原样提取真实四方法/字段的边界 216 断言通过，覆盖重复同帧、往返拖帧/回零/循环、嵌套 Animator、骨骼保留、异常 finally、销毁重建和源对象保护。此边界不模拟原生 Humanoid/Playable。
- 独立审查 PASS。521 前置文件仅目标源码变化，520 项保护保持，Q 特效仍 1/1/2。随后主 Agent 有意同步下列制作说明，不属于未知资源修改。
- 文档：包 Documentation~/UserGuide.zh-CN.md、包 CHANGELOG.md、项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，均补原点、原地预览、英文控件名、F 聚焦事件位置和兼容边界。文档补审通过，并更正“F 聚焦挂点”为“F 聚焦事件位置”。
- 当前已开 Unity 通过 computer-use 正常 Assets/Refresh，最终 Ethan.ActionEditor.Editor.dll 于本机 20:26:05 更新，晚于最终修复源码；Editor.log 记录成功编译和域重载。未启动第二实例。
- 早期域重载出现 3 条 Unity 内置 TransformInspector/GameObjectInspector 失效异常，无本包调用栈；最终刷新后没有重现，保留完整 Editor.log，不隐瞒早期错误。
- 开发/独立报告：_temp/skill-editor-preview-motion-20261008/develop/develop-result.md；review/independent-review.md、review/independent-test-report.md。实际窗口预览证据与结论另补本日志，未保存或导出用户技能资产。

## 当前 Unity 画面验收与交接

- 最终编译后使用现有3001003编辑窗口，在固定Scene视角回第0帧；随后使用原播放按钮连续循环约56秒，人物仍在同一区域、姿态随帧变化，没有再次跑远。停止并回第0帧，窗口中文、配置3001003、问题0；最终Console0错误/3警告。
- 截图与时间：_temp/skill-editor-preview-motion-20261008/ui/final-frame0-*.png、final-loop-*.png、final-stopped-frame0-*.png、final-window-*.png；loop-observation.json。这里只观察第三段，不宣称全七动画/原生Transform数值/PlayMode测试。
- UI结束后521基线终检仅Preview.cs与两包文档有意变化，其余518项保持；Q作者/Runtime1/1/2保持。没有点击保存/导出或修改技能参数；原用户配置的“未保存”状态仍保留。当前Scene视角已聚焦预览事件位置，技能窗口回原附近，主窗口恢复最大化。
- 修改文件还包括 Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md、本日志与项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md。项目文档相互引用使用根相对路径，符合归档规范。
- 结果：预览缺陷修复、独立审查/编译/边界及当前画面观察完成。旧时间模式、场景源父级世界对齐和实际运动轨道未新增；制作预览按原地姿态使用。所有临时证据保留，不清理。
