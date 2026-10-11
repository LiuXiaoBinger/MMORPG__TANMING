# RO 到期回调与模型加载边界纠正

- 日期：2026-10-11。
- 状态：已完成；开发与独立验收通过，主已读取报告并核对末稿。
- 用户要求：删除技能 Start 中模型订阅和复杂恢复逻辑，特效/音效由 OnProcessTimer 到期链执行，参考真实 RO 实现。

## 范围与设计

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：Start 仅 base.Start/FireEvents；删除 OnModelReady、启动订阅、技能 pending 列表；到期 fx/audio 使用模型一次 GO 加载回调，保等待票据与真正取消。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：统一 TriggerAnim 管理动画所需当前模型所有者，冷 GO 完成恢复动画，不混入特效或音效消费。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs`：移植 RO 同名 AddLoadGoCallback/RemoveLoadGoCallback，暖 GO 同步返回零，冷 GO 返回可取消票据；加载重入与池回收边界明确。
- `Docs/AI/CURRENT_TASK.md`、`TEST_REPORT.md`、`PROJECT_CONTEXT.md`、`DECISIONS.md`：登记当前新职责和最终验收，历史方案保留。
- `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md`：补真正 RO 加载边界事实，不以先前净减代码行数替代职责一致性。

RO 只读参考 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/MSkill.cs`、`Skill/SkillLogic/MArtSkill.cs`、`Model3D/MModel.cs`。OnProcessTimer→InnerProcessTimer→fx/audio；fx 到期后调用模型 AddLoadGoCallback，Stop 取消票据。RO 音效通过 FMOD 模型接口执行，本项目 AudioSource 需 GO 的冷加载等待属于适配，不能称为原样复制。

保持当前全部用户修改、自然 End 后晚事件/完整声尾、Interrupted 立即取消、冷资产行锁定、同 UUID/施法代次/池、独立粒子租约和 Collector→MMove 碰撞链。不自动重导制作配置、保存现场或更改服务器协议。

完整已载模型缺固定挂点改为报告并跳过该粒子，不再保留无限技能 pending 或阻塞其他事件；正常冷 GO 在模型初始化完成后单次执行。此项行为变化纳入本轮测试。

## 验证与交接

独立末注释稿92/92通过（45当前生命周期＋47新GO职责），不复用旧99项。267真实GM_Game源Rebuild零错、37既有警告；26完整生产运行源含真实模型、计时器、技能/组件、缓存、工厂/池、声音和根采集。覆盖Start两句/无启动模型订阅、Update无派发、到期才申请GO、暖同步零票据/冷票据取消、一次交付、异常隔离、兄弟取消、外部回调Reset、旧加载会话、同模型重载、同UUID真池重借与暖同步重入、冷配置补发、自然End晚事件/完整声尾、Interrupted、Q唯一动画owner、缺挂点跳过及冷挂点正常。

独立实测首稿字典删除首票据再新增第三票据交付为3→2，开发补实际登记token顺序表，取消同步移除、交付取顺序快照，删除再添加及整数有符号回绕末稿均通过。两条MModel.Load中文注释反馈也已补齐、末稿重编重跑。首次运行薄MGameObject缺NativeObject、首次finalize先于编译缺哈希属于临时夹具问题，未为夹具改生产；开发首次临时csproj编码失败也只修_temp。失败日志保留。

当前2788路径基线中2785原字节保持，变化恰三计划源，无新增/删除/未计划变化；用户MArtSkill前置SHA为a822049e2332baab09b5480c92964db31c503d72c8077d1a416b8145fe154183，原SetTimer换行/尾随空格保持，没有从上轮末稿覆盖。增改字段/函数/参数/局部变量中文具体注释已独立人工核验，无新增namespace/foreach/三目或弱类型业务载荷。

主读取review两独立报告并重算开发3、独立3、运行26、编译267共299项SHA零差异，证据 `_temp/skill-timeline-ro-callback-20261011/root-manifest-check.json`。独立证据同目录review的independent-review.md、independent-test-report.md、runtime-candidate-final.log、GM_Game-build.log、final-manifest-check.json、independent-protection.json、static-style-check.json、verified-source-hashes.json；开发交接develop/develop-result.json及final-manifest.json；RO参考reference/ro-callback-boundaries.md。

无剩余代码阻断。当前四份协作记录和 `项目分析/RO_SKILL_TIMELINE_RESPONSIBILITY.md` 已同步。下一步原正式入口进行画面、动画/根运动和声音体验，不重跑已通过隔离用例、不自动导出配置。

临时资料 `_temp/skill-timeline-ro-callback-20261011/` 保留，不清理。隔离运行的原生对象、Animator/状态机/声音和资源交付为薄边界；正式GUI、PlayMode、实际根动画/听感、联机和Bundle发布未执行，未新调用MCP，不冒称隔离验证为现场体验。
