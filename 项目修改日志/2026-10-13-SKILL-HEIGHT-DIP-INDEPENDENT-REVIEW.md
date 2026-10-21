# 2026-10-13 技能退场模型下沉与再次施法失败：独立审查

## 范围与定位

- 用户说明本地和远端都出现技能释放后模型下降再复原，随后补充全部技能且一个技能后不能再释放。本轮独立职责只读定位、审查开发最小修复并测试，不修改业务、不操作Unity MCP或正式账号。
- 静态共用移动链的Collector已清掉原生delta.y，远端新根运动消费不增加重力Y，本地非跳跃贴地速度始终为-2米/秒；旧RoleCtrlBase绑定活实体时Update/OnAnimatorMove均提前返回。尚无证据支持修改全部高度、取消碰撞/重力或冻结骨盆。
- 45秒正式被动采样4082条全部待机，实体Y恒18.738920；10040普通入口后8秒样本也未进入技能，二者仅提供基线，不能当作释放时已复现或通过。
- 客户端原生隔离8技能和战斗待机、applyRootMotion开关共18组：rootY与deltaY全部为0；骨盆有动作自身下蹲，但最低脚骨高度均为根上方0.072米以上。该已安装剪辑的姿态采样不能覆盖资源释放回退空槽，也不能用“动作本来下蹲”解释用户全部技能瞬降。
- 主通过零耗蓝F正常施法捕获真实退场：t=0.3934已Default但原生仍播Slider_F且ready真；t=0.4204实际clip变空占位Skill、ready假、next为战斗待机，实体/模型/Animator根Y仍18.738920，脚骨18.321970，低于根约0.41695米；t=0.4754已恢复真实Idle。实际下沉层是骨骼姿态，原因是退出混合前真实剪辑被清掉，绝非根Y重复移动。当前现场只实际捕获本地F，另一端该次是待机，不把此采样说成双端所有技能均已复现。

## 修改与设计

- 开发仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs` 的 `TryReleasePendingSkillAnim`，把释放时 `InState(STATE_SKILL)` 改为原生 `GetCurrentAnimatorStateInfo(0).IsName(STATE_SKILL)`，保留原有 `IsInTransition(0)` 保护。
- 前轮为快速停走将InState改为“最新请求目标”，该语义本身正确；但释放资源依赖尚未同步，CrossFade已请求Idle、原生下一次采样前仍在Skill且IsInTransition还是假时，错误地清掉正在使用的剪辑。资源归还必须看实际采样状态，动作去重继续看请求目标，两者分工清楚且无需新字段或等待机制。
- InState自身、Play/CrossFade冷加载回退、根采集/移动、技能生命周期、资源与服务器均保持。冷入场空槽仅曾作为待证方向，正式证据锁定退场后没有扩大修复。
- 修改方法沿用既有局部变量与清理结构，关键条件新增中文用途说明，布尔表达式2项，没有新增类型、命名空间、foreach、三目、弱类型业务载荷或无依据防御流程。

## 编译与专项验证

- 独立280份当前真实客户端源Rebuild零错误、39条既有警告，与前轮独立工程警告签名完全一致。编译输入SHA清单为 `_temp/skill-height-dip-20261013/review/compiled-source-hashes.json`。
- 专用薄边界测试逐字抽取生产 `TryReleasePendingSkillAnim` 与 `InState`；修前10项复现/回归、修后10项通过。覆盖请求已Idle但原生仍Skill且尚未过渡的真实缝隙、Skill过渡源、进入Skill的过渡、实际退出后准确路径仅归还一次、原禁用/空Animator清理、无请求、既有新旧剪辑移交保护，以及前轮快停走的最新请求判断保持。
- `final-audit.json` 为PASS：释放方法外逐字保持、InState逐字保持、两测试方法与末稿相同、280编译输入无漂移、19个冻结资源/前轮移动输入SHA保持，`git diff --check` 退出0。
- 最终MAnimator SHA：`D7FDACE77C8F26003B4DAC59354291DA9211E1B0CECC1DAF3BD35F8CEF37639D`；前轮MMove SHA `0DB2686CC687D8BDA7616E88646C75449286C5870CA8CB6ADD128E017E70F0EE`保持。
- 上述10项的Animator采样及资源归还为薄边界，不是原生或正式联机。主统一原生退场缝隙、过渡与退出完成验证，实际结果另补，不虚报尚未执行项。

## 再次施法失败与交接

- 主正式门禁采样 `root-live-skill-gates.txt` 已独立读取：本地MP=0、Default、CurrentSkill空、Jump假、TimerPause假、全部CanSkill真/CD0；普通技能耗蓝20至180，coreCanStart及componentCanStart均假，而零耗蓝F两者均真。现场不能再放普通技能由法力不足直接解释，未发现状态锁死，无依据修改技能结束流程或增加超时恢复。
- MP初始值、服务扣费与回包由后端只读职责核查。本轮不擅自改正式MP、配置耗蓝、回复蓝玩法、数据库或后台进程。
- 独立当前结论：源码修复、编译、修前复现/修后专项与范围审查通过；原生修后和正式双端/安装包仍由主完成或明确交接。主负责更新 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。
- 证据统一在 `_temp/skill-height-dip-20261013/`；独立层级解析 `review/height-layer-analysis.json`、静态事实 `review/static-height-facts.json`、两份methods/runtime及final-audit均保留。未清理临时或覆盖用户已有修改。

## 原生退场补充验收

- 主在用户自然退出Play后执行原生末稿，独立职责只读核查 `client/native-exit-release-probe.cs.txt` 与实际产物 `client/native-exit-release-result.txt`，未重复操作Unity、未修改业务。当前生产MAnimator的SHA仍为 `D7FDACE77C8F26003B4DAC59354291DA9211E1B0CECC1DAF3BD35F8CEF37639D`，与已通过编译及专项的末稿一致。
- 实际缝隙记录为最新请求FIdle、原生当前Skill真、过渡假；调用生产wrapper.Update后真实Slider_F剪辑仍保留。这覆盖了本轮正式采样所指向的请求与原生评估时间差，不能仅用“处于过渡”的既有保护解释通过。
- 后续20帧以1/120秒推进原生Animator：帧0至11均在Skill过渡中且持有剪辑，帧12原生Skill假且过渡假时首次释放，帧13至19保持已释放。脚骨相对模型根最低0.1100968米，没有回退空Skill造成的根以下姿态；末行PASS，实际过渡帧数12、退出后释放真。
- 夹具使用正式远端Prefab/Humanoid/Controller及正式F/战斗Idle，临时PreviewScene中装配独立MAnimator与ClipInfo；真实CrossFade、Update和原生采样执行，资源引用计数为独立测试对象，不进入共享缓存。它验证原生退场释放路径，未走服务器确认、网络、正式计时器及完整角色生命周期，不能扩大为正式双客户端、全部技能或安装包验收。
- 主报告脚本重载后IL已含GetCurrentAnimatorStateInfo/IsName且释放方法不再调用InState，末次控制台错误/警告0；这两项为主Unity现场证据，本独立职责未重复查询。原生结果、最终源码SHA与本轮独立审查相互一致，源码/编译/专项/原生退场四层通过；正式双端画面及包生效仍按原流程后续核验。
