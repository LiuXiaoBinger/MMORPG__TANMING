# 技能 CD 提前清零修复（2026-10-11）

- 用户报告：技能可无限释放。
- 源码疑点：本地允许表现自行回池，但未收到结果仍IsPredicted；MSkill.Cease调用MSkillCore.Reload将唯一单技能CD置0。RO原逻辑预表现结束属于失败处理，本地正常早回池与该清CD逻辑不能混用。独立before运行需证实，不以源码推断代替实测。
- 拟业务范围：MSkill.cs、MSkillCore.cs。删除正常结束/实例归池触发清CD的逻辑与无其他调用的先前新增Reload入口；不改施放写入/倒计时/权威结果和明确清场，不添加等待层/额外状态/快照。
- 分工：ro_cd_lifetime_develop 最小开发，ro_cd_lifetime_review before真实SkillCtrl输入/protobuf/施放/回收复现及末稿独立编译、回归和保护；主记录与验收，不直接修改业务。
- 验收：10秒CD/0.5秒表现，无回包正常收尾后CD仍剩约9.39秒；未到期重复快捷输入不发包或创建新实例，到期可施放。旧回包/升级/中断/权威拒绝、自然尾/Timer/模型/声音/运动保既有功能。
- 正式分析 `项目分析/RO_SKILL_CD_LIFETIME.md`，新证据 `_temp/skill-cd-early-reset-20261011/`；待本轮before/final结果，不继承274通过数。
- 不改用户作者F13/end111/F49、Runtime、资源/场景/协议/表/服务器及共享DLL，不自动导出，不清临时。真实Unity现场与网络联调是否执行须最终如实说明。

## 修改前实际复现

- 用户补充全部技能受影响，已按共享技能结束链排查，不继续要求用户提供复现信息。
- 独立完整SkillCtrl/protobuf输入链确认：十秒CD、半秒表现，无服务器回包，0.61秒时表现实例正常归池但CD=0，第二次按键请求成功；日志输出 `BEFORE_CD_EARLY_RESET elapsed=0.61 cd=0 pooled=True request_count=2 repeated=True`。
- 根因为之前将RO预表现失败的Reload清CD与本地无回包正常早回收混合，导致配置冷却提前丢失。新before日志 `_temp/skill-cd-early-reset-20261011/review/runtime-before.log`，修改前2799路径已冻结，开发在复现后才实施。

## 最小修复与当前结果

- 实际仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs` 与 `MSkillCore.cs`，删除Cease清CD整段及唯一无其它使用Reload定义，补一条具体中文回收职责注释。原施放写入/倒计时/权威结果/明确清场、简单更新结构与异步时间线保持，不新增接口或状态。
- 当前正式xlsx和生成TbSkillEffectInfo/ByteBuf只读读取正式.bytes均五行CD为0.5/8/10/9/13秒、组全0；工厂乘0.001f正确。当前正式表没有零CD行，零CD仅作为合法接口场景验证。原表/bytes/DLL保持，不自动生成或发布。
- 独立同一保CD契约对冻结before实际失败，日志runtime-before-contract-failure.log；末稿0.61秒回池仍9.39秒，20次按键不再发包或创建实例，9.99秒前阻断/10秒边界允许，中断保9.9秒。正常回收不会以未确认身份清CD。
- 独立298断言通过＝274本轮回归重跑+9真实输入早回收CD+15正式配置五技能；272客户端源码Rebuild零错/37旧，真实SkillCtrl/protobuf/Factory源码参与，资源/输入SocketUI和Unity原生仍薄边界，不能称现场资源加载或网络联调。
- 最终独立两报告/final-audit已归档均PASS，主已读取并复核608输入SHA全一致；真实Unity/听感/服务器/原生墙体/Animator末帧/Bundle未验。修复针对真实已复现问题，没有重新引入等待层或旧快照。

## 最终保护与交接

- 2799前置路径2797保持，仅两授权旧源变化，无增删越界，生成工程事前完整字节保持。主核开发2末稿/272编译、独立2末稿/272编译/50运行/10实际配置读取共608SHA零差异。
- before50输入中baseline含42，另外8共享实体/生成协议按原before运行SHA核不变，同契约before与原before全部50一致；审计已明确覆盖范围，不用误导的单布尔false表达未纳入baseline。
- 文档修改Docs/AI四入口与项目分析/RO_SKILL_CD_LIFETIME.md，新增本日志；业务由开发Agent实施，独立Reviewer复现/终验，主仅协调/记录。没有回退此前简洁移植约束。
- 之前仅验证正常回池没有断言剩余合法CD的测试缺口已补，本轮无未解决已复现阻断。现表及资源保持，使用原入口体验；现场边界仍如上未验，不自动发布、生成表或清临时。
