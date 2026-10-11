# 技能编辑器多段特效及轨道预览修复

用户反馈特效只播放第一段，并要求检查其他轨道。验收范围与分工见Docs/AI/CURRENT_TASK.md。本轮开发preview_pause_develop，独立测试审核preview_pause_review，主协调／验收／记录；根因与正式文件范围由开发和独立证据确认后补充。

重点检查真实Q3／F／1多段效果、特效子系统延迟、后续时间轴事件及其他已接入预览轨道的同类行为。保留上轮暂停／波形／Undo、原配置／声音／GUID／动画／特效／场景；不通过重复合成人造粒子计时替代真实问题复现。

临时材料统一_temp/skill-preview-multistage-20261011，未经确认不清理。使用与轨道边界说明归项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md，本轮尚在定位与验收中。

## 首轮定位

用户明确问题是1技能。独立真实3007001取证：F4蓄力16粒子系统、F13落剑13粒子系统均生成，END34／30FPS；落剑Sword延迟0.25秒，LightningMain0.8秒，Impact／Trail／Smoke1秒，fire／path／blast／dilie／light1.05秒。后段最早需要F37～45，而原预览F34循环清理时落剑年龄只有0.7秒，所以只见Sword前段。证据review/native-actual-before.json。

修复方向是已触发表现尾段的预览计时与循环清理，不修改正式END或用户事件帧。其它声音与飞行物尾段同类排查；根粒子stopAction=Destroy的副本清理副作用仍由独立实证决定是否需修。

## 开发文件与设计

六个既有文件均在LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor/Window：

- SkillEditorWindow.Preview.cs：GetPreviewContentEndFrame保持正式触发门限，GetPreviewEndFrame缓存粒子／声音预计尾帧；尾段固定动画结束帧，按真实存活继续已有实例，全部结束后循环。正式END后的新FX不补发，定位允许尾段。
- SkillEditorWindow.AudioPreview.cs：正式END后的新声音不补发，已触发声音保留独立播放位置与尾音。
- SkillEditorWindow.Toolbar.cs：显示正式END／预览尾帧／秒及原FPS，仍可输入帧定位。
- SkillEditorWindow.Timeline.cs：网格／标尺／滚动／播放头显示扩到预览尾部，作者制作总帧和拖动范围保持；结束线显示正式END。
- SkillEditorWindow.SoundTimeline.cs：Alt音频选点可定位预览尾部，资源变化清缓存。
- SkillEditorWindow.Interaction.cs：标尺和End键导航允许预览尾部，事件拖动继续沿原制作范围。

预览尾长只读正式END内允许触发的声音长度、活跃粒子延迟／duration／lifetime／倍率和自定义清理时间。实际尾粒若超过估算则继续保留，实例满足有限收尾条件后循环。缓存随编辑／切配置／Undo／资源导入失效，不重复每帧枚举资源。原inactive节点不自动激活。

独立跨Editor.update验证F13至60实例一直存活，证伪stopAction自动销毁猜测，本轮没有修改stopAction或原Prefab。真实落剑当前为TargetTop，未指定预览目标时前方10米兜底，原用户参数保持。

本轮没有新增生产文件，没有改正式角色、运行技能协议或执行器。开发Editor36源编译0错／4旧警告，独立最终原生回归和保护审查仍在进行；开发final-manifest和交接见_temp/skill-preview-multistage-20261011/develop。

## 独立打回与必要收尾修复

首轮真实1验证已看到F38／F45／F58后段6／119／303颗粒子，尾段暂停／倒拖／恢复、END后事件负例、声音采样和末帧动画冻结通过。但650帧之后全部29个粒子系统数量已归零，手工Simulate＋Pause下IsAlive仍为true，导致循环不结束。独立审核打回，不提前标记完成；夹具两处0帧前置／原最小制作范围误断言仅在_temp修正，旧失败保留。

必要新增修改范围为第七个既有文件LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor/Window/SkillEditorWindow.cs：SpawnPreviewFx缓存含活跃子层的delay＋duration＋startLifetime、simulationSpeed和事件倍率换算后的时间轴上界；Preview.cs有限结束判断结合该上界与实际粒子数量，继续保留非零尾粒，避免单靠IsAlive常真。原customLifetime＋0.5秒清理、自定义稿、原资源stopAction保持。候选仍待开发编译与独立复验。

## 最终验收完成

独立preview_pause_review返工复验PASS，主已读取review/independent-review.md和independent-test-report.md。原生58／58通过：真实1的F38／45／58延迟冲击6／119／303颗，285个30FPS tick完成尾部后循环；尾段暂停、倒拖、同帧重建、继续和停止正确。END后新FX／声音不补发；声音尾部准确定位及有限结束；手动飞行／派生提示和内嵌loop收尾；空动画容器末帧冻结；四组事件／系统倍率各换算一次、原失活层不激活不撑长。无剩余代码阻断。

独立三程序集103真实源Rebuild0错（Runtime59／Editor36／宿主8，警告2／4／2均既有）；2636基线2629字节保持，7计划既有源修改，无新增、删除及未计划改动。七生产末稿／原生worker／开发清单一致，103编译源冻结一致，主重算七SHA也通过。中文注释与增量规范审查通过。

所有作者／Runtime配置、音频／meta／GUID、原表／动画／特效Prefab／场景及正式技能运行链保持。真实TargetTop／无目标前方10米视野、GUI像素／真实鼠标和设备听感未验；原生使用内存作者副本世界定位与空动画容器，不生成正式角色，不把资源状态断言冒称现场画面。没有保存未保存资产、重导配置或开启第二正式工程。

四协作入口与项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md已同步本轮尾段和轨道能力边界。证据_temp/skill-preview-multistage-20261011/review的两独立报告、native-actual-before.json、native-tick-before.json、原始失败、native-multistage-final.json／after-rework.log、三编译日志、independent-build-summary.json、independent-protection.json／verified-source-hashes.json，develop交接／final-manifest及主root-manifest-check.json。临时资料保留，本轮结束。
