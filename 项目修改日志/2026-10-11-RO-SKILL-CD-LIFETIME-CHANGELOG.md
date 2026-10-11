# RO 技能冷却与脱手生命周期管理

- 状态：已完成；开发及独立终验通过，主已读取两报告及final-audit并核613项末稿输入SHA。
- 用户要求：按RO的MSkillComponent.Update(float)补齐脱手仍存活技能的Update/Cease回收，以及SkillCDGroupList/Dic、SkillCDList/Dic逐帧倒计时和工厂归池；函数接口采用RO同名。
- 分工：ro_cd_lifetime_analysis只读核RO及现有链并编写分析；ro_cd_lifetime_develop修改客户端业务；ro_cd_lifetime_review独立测试审查；主协调、验收及正式记录，不直接修改业务。
- 已明确业务范围：本地MSkillComponent、MSkill、MArtSkill、MSkillFactory、必要MSkillCore，以及独立SkillCDGroupInfo类型和meta；如需更新既有GM_Game.csproj生成索引单列，最终按末稿manifest记录。协议、表、作者与Runtime资产、服务器、RO参考不修改。
- 设计约束：秒接口沿RO，本地实体毫秒入口只换算一次；不得技能状态机和常驻组件双推进。CD须接实际预测/权威结果及正式查询，保持原UUID/旧回包防覆盖；自然End动作退出后晚事件、冷GO、完整声音继续存活，Interrupted真取消，独立粒子尾迹保持。
- 验收：真实RO接口/键语义对照、单技能与组CD创建更新清除到期及池复用、预测/权威/失败/超时同步、脱手多实例与自然End/打断、真实链已有回归重新执行、客户端真实源码编译、逐声明中文注释与禁止语法、当前用户资产及末稿输入SHA保护。失败交开发返工。
- 分析统一 `项目分析/RO_SKILL_CD_LIFETIME.md`，协作入口 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`，临时新建 `_temp/ro-skill-cd-lifetime-20261011/`。当前F13/end111/F49保护，不自动导出，不覆盖旧证据，不清理临时。

## 已核事实与实现选择

- 参考准确类型为SkillCDGroupInfo，SkillGroupType在普通表是技能主键，在组表是组号；ClearGroupCd清两表。RO SetSkillCD零移除未归池和重复总量比较的问题不照搬，沿同名接口实现安全回收及权威剩余刷新。
- 本地已有脱手列表和自然End保尾；缺RO bool Update/Cease闭环及双CD容器。本地组件基类已有virtual Update(float)，直接override，实体毫秒入口桥接一次，不新增基类或世界循环。
- 主已确认常驻组件统一推进当前与脱手各一次，状态OnUpdate只判断动作到期，避免离槽当帧双推进；公开IsEnd沿RO区分动作结束与生命周期Update返回false。
- 已安装Core的预测、权威、回滚、CanStart和界面查询均接组件CD唯一数据源；Core旧截止仅在未安装核心独立使用兼容。现正式CD是不缩放realtime，组件CD用实际realtime差量递减，技能仍用实体帧秒；这是本地主机适配，不能声称原样按RO的fDeltaT扣CD。

## 最终修改文件

1. `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`：两类RO同名列表/字典及Get/Set/Clear接口，实际预测/权威结果/查询接入；毫秒桥进入秒Update，当前及脱手各推进一次；OnSkillEnd按原实例解除，动作OnUpdate仅IsEnd判定。
2. `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：Update返回bool；抽象IsEnd；Cease终止、通知组件、注销并归池；内部只读既有施法代次供更新快照验证。
3. `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：IsEnd读取动作窗，PruneAudio后返回基类存活状态；原FireEvents/MTimerMgr到期职责保持。
4. `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillCore.cs`：安装时BindCooldown绑定组件唯一冷却源，预测、权威、回滚及CanStart/旧查询统一读写；未安装独立核心兼容原时钟。
5. `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillFactory.cs`：保原静态调用，增加singleton及同名GetSkillCdGroupInfo/ReturnSkillCdGroupInfo，归池清旧字段。
6. `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillCDGroupInfo.cs` 及meta：同名CD记录，普通键为技能ID，组键为组号；CD/CurrentCD秒，内部UpdatedRealtime记录不缩放时间。
7. `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillUpdateEntry.cs` 及meta：值快照保存原引用及既有代次，配合MListPool避免回调删除兄弟或重借对象后旧返回值误停新施法。
8. `LYF_MMO/GM_Game.csproj`：既有生成工程登记两个新增源码Compile索引；不是新增工程。合计5既有业务源码、2新增源码/2meta及1生成工程，10路径。

## 最终验收

- 独立253运行断言通过：原189在当前源码重跑（63运动、92生命周期、34真实实体组件）＋48新CD＋16新生命周期。链接47完整生产源，包括真实实体、组件管理器/基类/容器、技能/Core/工厂/池、计时器、模型/缓存、声音/运动及两个新类型。Unity原生、配置/资源交付与动作状态机仍薄边界，网络仅静态核链及完整编译。
- 真实setter影响CanStart/UI、两表同引用/键隔离、同值刷新/缩短校正、零值/多项到期/清場归池、毫秒桥/实时跳变/缩放及睡眠时钟、预测成功/拒绝/超时/重复与旧UUID、升级/移除/Shutdown通过；当前与脱手各一次、默认Cease/true真结束、未来Timer/冷GO等待取消、正在播放声音停止、兄弟移除及同对象重Fire旧false隔离通过。
- 独立273完整GM_Game源码Rebuild零错、37既有警告；开发同样273零错。基线271构建另存，不混用为末稿。主重核开发10末稿＋273编译、独立10末稿＋273编译＋47运行，共613项SHA零差异，root-manifest-check.json=PASS。
- 独立2796前置保护中2791不变、5授权旧业务源修改；4授权新源码/meta，无删除或越界。用户F13/end111/F49、Runtime、原动画/粒子/声音、场景/预制体、共享DLL、服务器协议表及前置实体挂接成果保持。两新增meta GUID各唯一，七源码逐声明中文注释和禁namespace/foreach/三目/弱载荷审查通过。
- 生成GM_Game.csproj未被双方事前字节冻结：核两新增Compile各一次、原271源索引均在，开发两处replace脚本与登记一致；不能以此声称其他项目节点完整before字节不变。该单项限制在独立两报告及审计明确。
- 主审首稿Cease(false)等待自然End偏离RO用途，已返工为任意cleanUp值都真结束；本项目没有额外cleanUp策略，参数保兼容。自然End保尾继续仅原ApplyServerEnd(false)流程。末稿专门验证默认结束及资源取消，未留下二次自然恢复入口。
- 夹具XML编码名及1.7秒float临界值失败仅修_temp，生产未迎合测试；原189断言/数据不改，薄Tick按真实Skill先Machine顺序更新。候选239/249日志与首失败保留，不计末稿成果。新增差分无尾空白；HEAD整文件仍有MArtSkill既有登记行尾空格，不改用户旧行、不称全HEAD空白检查通过。

## 证据与交接

独立证据 `_temp/ro-skill-cd-lifetime-20261011/review/`：independent-review.md、independent-test-report.md、final-audit.json、verified-source-hashes.json、final-diff.txt、runtime-final.log/47输入、GM_Game-build.log/273输入及独立baseline日志。开发develop/保before、10末稿manifest、编译输入、规范/保护和develop-result.md；主root-manifest-check.json。旧任务证据不覆盖。

无剩余已复现业务或规范阻断。本轮未重建共享DLL、执行原生导出或修改正式场景；真实PlayMode/画面/键盘输入/设备听感、原生墙体及Animator精确末帧/跨窗、实际网络/服务器、Bundle未验。下一步在原正式入口体验，不继续扩大已通过测试或自动发布资产；临时资料保留待产品经理确认清理。分析 `项目分析/RO_SKILL_CD_LIFETIME.md` 和四协作入口已同步。
