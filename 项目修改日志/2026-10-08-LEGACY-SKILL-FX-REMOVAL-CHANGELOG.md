# 删除旧动画事件技能特效消费链

- 日期：2026-10-08。
- 需求：删除 MArtSkill 读取 clip.events 的旧兼容逻辑，子 Agent 独立检查。
- 修改文件：运行侧 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/` 下 MArtSkill.cs、MSkill.cs、SkillRuntimeFxLifetime.cs、SkillRuntimePresentation.cs、SkillRuntimePresentationCache.cs；宿主 `LYF_MMO/Assets/GM_Game/Editor/Skill/` 下 SkillEditorAssetTools.cs、SkillEditorBinding.cs、SkillEditorBindingInspector.cs；SkillPresentationBuildVerification.cs、YCombatIntegrationContractTests.cs 两个专用验证文件；包内 SkillEditorPropertyLabels.cs 移除旧标签；Data/Skill 下8绑定、7运行资产共15配置。精确21编辑/配置清单在临时editor-assets/changes.json。
- 文档：更新 Docs/AI 四入口、项目分析/YCOMBAT_EDITOR_INTEGRATION_DESIGN.md、项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md、项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md 和本记录。原动画、旧配置表和资源保留，不改变 Q 接段或权威施法。
- 设计思路：所有技能特效统一消费导出 FxEvents；将 10070 蓄力/落剑完整迁入现有编辑轨道，移除旧路径标志与硬编码。配置冷加载只等待该次锁定表现身份的配置，不再读取动画事件。
- 分工：legacy_fx_develop 运行开发；legacy_fx_assets_develop 编辑/配置开发；legacy_fx_review_test 独立审查测试；主 Agent 验收记录。
- 实现：删除旧 AnimationEvent 快照/循环、TryPlayLegacyEffect、10070硬编码例外、ClipInfo特效租约、BindClip、PreserveLegacyFxEvents字段/资产/导出分支/Inspector说明/翻译标签。编辑器仍可首次从旧动画读取制作参考，运行消费仅 FxEvents。
- 冷加载：Fire锁定本次表现ID、动画与原表时长；MArtSkill等待该ID就绪并校验整轨，按经过秒数补发。实例Retain核心保持所有段配置引用，Q换段不改变旧实例身份；无效轨道只报一次并停止特效。没有新增捕获池实例的异步回调；粒子继续独立Retain运行资产直到尾迹结束。
- 10070配置：现有作者轨道30FPS不改，第4帧蓄力=0.13333334秒、第9帧落剑=0.3秒。相对原事件分别约+6.834ms/+3.113ms，为已有帧量化结果。蓄力挂EffectPos，落剑TargetTop，无目标前方10单位，源粒子/位置/旋转/缩放逐项保持。编辑轨道统一发布，可修改受支持参数，不再强制保持旧事件。
- 验证：开发运行隔离编译0错误；独立GM_Game、宿主Editor、Verification Editor、ContractTests Editor四程序集最终隔离Rebuild均0错误，警告37/2/2/2。链接真实技能/核心/缓存/生命周期代码薄边界27断言PASS，覆盖冷加载补发/不双播、Q段身份与700ms窗、模型晚到、目标头顶/前方落剑、回池旧回调隔离、无效整轨一次报错/Reset恢复与粒子尾租约释放。独立最终审查PASS，无阻断返工项。报告：`_temp/legacy-fx-removal-20261008/test/independent-review.md`、`_temp/legacy-fx-removal-20261008/test/independent-test-report.md`，编译摘要 independent-build-summary.json，保护/数据结果 independent-static-verification.json。中间产物全部放在该临时目录。
- 失败与纠正：开发静态时点容限1e-8小于单精度往返，改为生产一致1e-6通过，未改资产掩盖失败。独立临时工程encoding='utf8'导致MSB4025，改隔离生成器为utf-8后四构建通过；失败记录保留，业务源码未为夹具改写。
- 原资源保护：1578个动画/预制体/场景/原表/协议及MAnimator、Core、Data保护文件SHA256保持；八作者轨道与29meta保持原字节。
- 既有未发布差异：独立首次全轨道比较发现3001003编辑稿仅F23一条，运行发布仍F23+F54两条；开发前快照证明差异本轮之前即存在。其他运行资产本轮仅删除旧标志，作者轨道保持，因此没有替用户重导出或补回该段。保留首次失败证据后，本轮门禁按增量验证10070新导出双FX，其余配置原内容保护。后续编辑3001003并发布时以作者轨道为准，当前说明不能当作七条轨道全部已同步。
- 未解决：没有此次正式Unity刷新/域重载、GPU/Game View、实际YooAsset冷加载/Bundle或Editor合同执行证据；隔离边界只证明业务逻辑，不模拟真实粒子/物理画面。需当前Editor正常编译后使用现有角色手测Q/E/R/F/1特效；不生成角色或自动按键，不启动第二Editor，不清理_temp。
- 协作记录：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`；完成后同步 `Docs/AI/PROJECT_CONTEXT.md` 和 `Docs/AI/DECISIONS.md` 的旧兼容说明。
