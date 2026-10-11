# 技能编辑器特效暂停与声音对齐
## 2026-10-11 技能编辑器特效暂停与声音对齐（进行中）

- 用户反馈技能编辑器特效不能暂停，无法设置声音对齐。验收目标：播放/暂停/继续/停止与动画、粒子和声音共享预览时点；暂停保持特效姿态，逐帧/播放头定位可检查声音触发帧，音效触发帧可直观调整并保持上轮点事件拖动语义。
- 开发preview_pause_develop；独立preview_pause_review冻结基线、测试审核并打回，主仅协调及正式记录。开发先核根因/拟改范围，声音对齐是否需要波形已发可选澄清；答复前先推进公共暂停问题。
- 当前源码可见FxUpdate/ProjUpdate不按playFrame门控；工具栏播放按钮调用StopPreviewSession销毁预览，缺独立保持姿态暂停，AudioSource.PlayOneShot未跟随暂停/播放头定位。具体修复方案以开发独立定位为准。
- 预计修改集中ActionEditor窗口预览/声音/工具栏/交互相关源，不扩正式技能播放协议或服务器。全部用户作者Runtime资产、音频GUID、表/动画/FX/root/场景和上轮五源修复保留。不得保存用户现场、重导制作或造角色/游戏按键。
- 编译受影响真实程序集，原生Unity验证粒子时间冻结/继续/倒拖/多事件/子粒子及声音句柄生命周期；声音设备输出与窗口像素实测必须单列，不以方法断言替代。
- 临时_temp/skill-preview-pause-align-20261011，不清理。MCP当前instances0；不反复轮询或打开第二正式Unity。归档项目修改日志/2026-10-11-SKILL-PREVIEW-PAUSE-ALIGN-CHANGELOG.md；目前未完成。

### 2026-10-11 用户明确声音对齐范围

- 用户答复“两者都要”：暂停后精确调整触发帧，以及显示音频波形以对齐挥刀/撞击峰值。此前可选波形范围现为明确要求。
- 新委派sound_waveform_develop新增必要全局窗口分部/波形缓存源，不改主开发现有文件；preview_pause_develop接入Timeline/Inspector、暂停与公共拖动抓取偏移。preview_pause_review独立验收两部分，协调隔离worker使用。
- 波形真实音频长度仅影响制作显示，不写单次事件endKeyNumber，不改变技能正式时长或输出结构。触发帧精确编辑/对齐播放头与波形拖动沿原Undo提交链；显示多声道峰值、读取失败提示、缓存换clip、缩放/滚动边界。

## 开发范围与实现

既有集中窗口修改九份源码，均在LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor/Window：SkillEditorWindow.cs、SkillEditorWindow.Preview.cs、SkillEditorWindow.AudioPreview.cs、SkillEditorWindow.Toolbar.cs、SkillEditorWindow.Interaction.cs、SkillEditorWindow.Inspector.cs、SkillEditorWindow.Timeline.cs、SkillEditorWindow.SerializedInspector.cs、SkillEditorWindow.Persistence.cs。

新增四份正式文件：

- LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor/Infrastructure/SkillEditorAudioWaveform.cs及同名meta。
- LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor/Window/SkillEditorWindow.SoundTimeline.cs及同名meta。

暂停与停止分开：SetPreviewPlaying切换预览时钟，SeekPreviewFrame统一暂停定位，SampleAnim按配置FPS的固定步模拟粒子和飞行物，反向定位重建到目标帧。声音事件拥有独立隐藏AudioSource，暂停继续保持各自样本位置；关闭、停止和切配置回收。预览副本上活跃、启用且playOnAwake=true的内嵌音源统一参与，其他内嵌音源保持原行为。真实Skill04的Audio节点当前不活跃，不自动激活或凭空发声。

声音波形读取真实PCM，分块4096采样帧、最多8192包络桶，所有声道保留有符号极值；绝对主峰保留精确采样帧。完整声音显示、多段独立叠层和滚动范围仅影响界面。Alt选择音频内部点，再移播放头可执行选点对齐；无选点时采用主峰。拖波形保存抓取偏移，对齐沿原Undo提交链并保持点哨兵／区间跨度，越界明确拒绝。

同一预览时钟下，自定义寿命累计时间轴秒，粒子模拟应用速度倍率一次，避免重复倍率提前清理。审核要求补齐固定步dt和清理计时单位注释，并避免停用预览后重复Scene重绘；开发已最小返工，最终验收以独立末稿报告为准。

文档范围：Docs/AI/CURRENT_TASK.md、TEST_REPORT.md、PROJECT_CONTEXT.md、DECISIONS.md，以及项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md和本日志。正式使用说明补充暂停／继续／停止、逐帧、波形拖动、Alt选点及两种对齐按钮，原配置与发布限制仍按原链路处理。

证据目录：_temp/skill-preview-pause-align-20261011。开发交接develop/develop-result.md和waveform/develop-result.md，正式文件清单分别为develop/final-manifest.json和waveform/final-manifest.json。三程序集及原生验证、保护冻结和独立最终结论仍在收尾，不提前宣称通过。

## 最终验收与交接

独立preview_pause_review最终PASS，无剩余代码阻断。三真实程序集Runtime59／Editor36／宿主8共103源隔离Rebuild全部0错，警告2／4／2为既有项；103编译输入与末稿冻结一致。原生暂停／声音61断言、真实PCM／缓存／对齐／Undo34断言、相关轨道核心229断言通过。自动随机源倒拖同帧4颗粒子逐seed一致，位置最大差0。旧OverlapAndExport9项点跨度显示假设被本轮真实音频长度显示取代，长声重叠由新断言验证。

保护2632前置文件中2623字节保持、9计划修改、4计划新增，无删除及未计划变化；13正式路径与开发清单／原生worker一致。主再次核13路径SHA全部一致，新增两meta在Assets／Packages中GUID各唯一。配置、音频/meta/GUID、原表／动画／特效／root／Prefab／场景和正式运行链均保持。

旧暂停问题有原生复现证据；夹具循环上限假设、失活节点查询与batch缺GUI上下文等失败保留。开发必要返工包括固定步dt／清理单位中文说明、排除式循环帧上限说明、停用预览后不反复重绘、内嵌自动音源保真接管，不为测试覆盖资源或添加游戏测试入口。

明确边界：MCP当前无会话，computer-use缺node_repl入口；隔离batch无图形及图形各一次SendEvent未进入OnGUI，真实波形中部抓取手势未实测，抓取偏移公式和接线经源码审核。原生声音采样状态不等于设备输出；正式窗口像素、现场鼠标和实际听感尚未验证。未保存用户未保存现场、生成正式角色／游戏按键、重导作者稿或启动第二正式Unity。

原3007001的key4/end8声音区间单次发布限制保留，应按实际制作意图使用高级结束帧属性调整；本轮不自动改稿。使用方式已写项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，四协作入口已同步。本轮结束，临时文件不清理。

最终报告与证据：_temp/skill-preview-pause-align-20261011/review/independent-review.md、independent-test-report.md、native-preview-result.json、native-waveform-result.json、native-track-regression-result.json、native-random-seed.json、independent-build-summary.json、三编译日志、independent-protection.json及verified-source-hashes.json；主root-manifest-check.json和root-new-guid-check.json。
