# 2026-10-12 F滑步翻滚改为本地移动动作

- 用户明确选择：作为移动动作，不走施法请求。复用原F滑步动画、约5米根运动、白残影和短冷却，不增加无敌、气力玩法或通用动作框架。
- 与此前实现的差异：Type3目前被SkillCtrl送入SkillCastReq，并占客户端/服务器普通技能活动状态；新需求改为本地移动入口，不等待施法结果。RO移动类本身仍检查CanSkill，不能将本轮需求描述为RO原样行为。
- 开发由roll_movement_check承担，roll_local_move_review独立审查；先确认最小文件范围和现有表现复用方案，保留普通攻击/技能原网络与CD和跳跃禁施法。当前尚未完成，不预记通过。
- 临时证据`_temp/roll-local-move-20261012/`；不发布、不清临时、不操作用户运行进程。最终修改文件、验证和交接待追加。

## 实现范围与编译

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`：TrySlide启动本地动作，持有SlideAction、推进并在动作结束或Shutdown清理；沿原CD记录，滑步期间只消费根水平位移，结束后恢复既有方向输入。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs`：主表Type3走移动入口，不装已学技能核心、不发施法包、不建预测；F槽沿原CD记录显示。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：原动作拥有权支持移动组件当前滑步，与普通技能槽共用原动画入口。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：根绑定/采集与原计时器残影使用上述拥有权；保留原FireEvents及计时器逻辑。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillFactory.cs`：Type3本地动作不依赖普通技能职业归属门禁，仍按当前真实模型选择原表现。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`：滑步期间保持动画及倍率，结束后沿原默认动作恢复。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`：滑步期间普通技能/普攻不覆盖本地动作，其余原施法及CD保持。
- `LYF_MMO/Assets/GM_Game/Scripts/Manger/SceneMgr.cs`：编译发现声明`_loadingVie`与使用`_loadingView`不一致；用户确认未在编辑并授权修正，只补字段名末尾一个w，保留其他字节。
- 正式272源Rebuild零错误、37既有警告；主独立核272编译源码SHA一致。首新增MEntity无NextSkillUUID错误已通过现成PlayerEntity身份入口修正；SceneMgr拼写首失败保留，授权后的正式编译为本轮验收依据。独立动作专项尚在收尾，不预记最终通过。
- 设计与当前职责见`项目分析/F_ROLL_SLIDE_DESIGN.md`顶部；现资源/服务器/协议保持，不新增无敌、气力、自动连发、移动同步或通用框架。

## 最终验收与交接

- 独立审查`review/review-report.md`与`review/final-audit.json`结论PASS，无需返工。64当前真实生产源55项断言通过：连续三次F零发包/无预测/零MP、未学习可用、0.5秒CD/667ms自然结束、原21残影、根唯一提交与结束清根；W/Shift不叠跑速且结束恢复方向移动，默认动作不重复播放，冷Runtime不追旧影，Shutdown取消未来Timer，普通2原请求预测与跳跃互斥保持。
- 主已审最终报告、实际运行日志与必要源差异，核272正式编译源/8末稿/64专项源/42配置与夹具SHA均一致，证据`_temp/roll-local-move-20261012/root-manifest-check.json`。
- 测试为真实业务源配Unity/网络/状态机薄边界；Collector运行真实方法但delta由夹具提供，CharacterController记录提交，Ghost记录原接口参数。原约5米资产未变，沿既有原生采样证据，本轮未正式场景测距/墙体/联机/包验收。首暖资产夹具准备失败仅修_temp，失败证据保留，不为假设极端情形扩业务机制。
- 四协作入口与分析已同步。无剩余已复现阻断；下一步正式游戏重新运行体验F，安装包需更新配套GM_Game热更代码。没有发布、替换运行包或清理临时文件。
