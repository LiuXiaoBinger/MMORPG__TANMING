# RO 技能 FireEvents 与 MTimerMgr 时间线改造

- 日期：2026-10-11。
- 用户目标：参照 RO `protected virtual void FireEvents()` 的 timeline 回调注册方式，接口使用 RO 同名，计时器使用 `MTimerMgr`。
- 当前状态：实现、独立返工复验、主验收和记录归档已完成，无剩余代码阻断。

## 修改范围与思路

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：持有本次施法计时器票据，接入同名回调及取消链。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：通过 `FireEvents()` 注册已有声音和粒子事件，替换配置列表逐帧派发；到期但模型未就绪的表现保留重试。
- `LYF_MMO/Assets/GM_Game/Scripts/Global.cs`：使用现有实体毫秒帧间隔唯一驱动计时器。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/Common/MTimerMgr.cs` 与对应 meta：提供 RO 同名定时器管理入口。
- 新增 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillTimerArgs.cs` 与对应 meta：具名强类型技能事件载荷，替代 RO 的 `object[]`，满足本项目数据约束。
- `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`：维护任务状态、验收和交接。
- `Docs/AI/PROJECT_CONTEXT.md`、`Docs/AI/DECISIONS.md`：登记正式技能时间线入口及强类型适配决策。
- 本文件：统一归档本轮修改说明。

参考只读源码：`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs`、`Skill/MSkill.cs`、`Utility/MTimer.cs`。确认调用链 `FireEvents → SetTimer → OnProcessTimer → InnerProcessTimer`，票据管理使用 `AddedTimerToken/KillTimerAll`；不直接复制 RO 弱类型参数或其命名空间。

本轮保留单次施法表现行锁定、冷配置按已累计时钟补发、UUID/核心租约/池、统一动画、根运动及已触发粒子独立收尾。Q 退出动作槽仍保留旧实例，真正取消及回池必须清除计时器。编辑器新增制作轨道的运行门禁保持原范围，不因存在注册入口就宣称已支持所有轨道。

具体接口保留 `protected virtual void FireEvents()`、`protected void OnProcessTimer(uint token, MSkillTimerArgs args)`、`protected virtual bool InnerProcessTimer(int id, MSkillTimerArgs param)`、`protected void AddedTimerToken(uint token)` 和 `protected virtual void KillTimerAll()`；MArtSkill 覆盖实际分发。管理器使用 `singleton/Init/Uninit/Update/SetTimer/TimeLeft/TimeDuration/KillTimer/KillTimerAll/IsPause/ElapsedTime` 同名入口，只实现本轮需要的一次性技能计时，不宣称移植 RO 全套时间轮或全局计时接口。

时间来源是 Global 原有 `RoundToInt(Time.deltaTime * 1000)`，在实体更新前唯一推进管理器；冷资产注册以事件秒数减本次累计秒数，过期或零时刻沿 RO 同步执行、返回票据零。正延迟小于半毫秒仍保留至少一毫秒异步，避免取整零使接口行为含糊。计时器按到期毫秒及注册顺序稳定派发，异常隔离到单事件；本次持有票据只影响该施法。到期但模型未就绪的声音/粒子分别保留 pending，成功后移除，不再逐帧扫描未来配置事件。

零秒同步回调可能重置或重新 Fire；首稿被独立测试发现清空资产后继续注册的空引用，已返工。注册冻结资产引用、UUID 与实例施法代次，同步返回后核验原批次；Reset 更新代次且先注销票据，回调载荷也核代次，阻止同 UUID 复用串入新施法。Fire 返回值核实际启动的原代次是否仍存活。

## 验证与问题

- 独立审查员冻结 2765 路径保护基线；当前正式 GM_Game 的 265 真实源基线 Rebuild 为 0 错误、37 个既有警告。
- 本轮 Unity MCP 只读发现没有活动实例，正式画面、设备声音和 PlayMode 实测尚未执行。
- 末稿独立 GM_Game 267 真实源隔离 Rebuild：0 错误，37 个警告与基线一致；开发完整链边界 63 项通过，独立完整链边界 85 项通过。23 份生产 MTimerMgr/MSkill/MArtSkill/Core/Cache/Sound/Root/Factory/对象池等源参与测试，真实工厂借还链也通过；Unity 资源及模型边界由薄夹具提供，工厂外部表查询不在专用回归中装配；不等同 Unity 原生实机测试。
- 独立覆盖：零/负/正延迟、同毫秒稳定序、一帧跨多事件、暂停/恢复、查询与取消、回调内取消及重注册、异常后其他票据继续、初始化前旧票据不误杀新票据；暖/冷轨道、模型/挂点迟到、重复注册、真正取消、池复用、零时刻 Reset/不同及同 UUID 再 Fire、旧代次阴性和当前代次阳性对照、Q 双实例独立时钟与旧行锁定、根运动及粒子独立租约收尾。
- 保护 2765 基线中 2762 字节保持，仅三份计划源改变、四份计划新增，无删除/未计划修改。267 编译源及23份完整链边界输入与正式末稿 SHA 一致；两个新 meta GUID 各唯一。主重算开发 manifest 的七条路径全部一致。
- 新增/修改部分中文具体注释，五份生产源码没有 namespace/foreach/三目/弱类型业务载荷；顶级类型各同名文件。失败只由开发返工，测试逻辑仅位于 `_temp` 专用夹具。
- 证据：`_temp/skill-timeline-ro-20261011/develop/final-manifest.json`、`runtime-boundary-final.log`；`review/GM_Game-build.log`、`runtime-boundary-final-with-pool.log`（最终85项）、`runtime-boundary-final.log`（82项阶段）、`runtime-boundary-reentrant-failure.log`、`independent-protection.json`、`source-consistency.json`、`static-style-check.json`、`new-guid-check.json`、`independent-review.md`、`independent-test-report.md`；主 `root-manifest-check.json`。
- 临时资料：`_temp/skill-timeline-ro-20261011/`，保留，不清理。

## 交接

开发由 skill_timeline_develop 负责；测试审查由 skill_timeline_review 独立完成，主 Agent 维护正式记录与末稿 SHA 核对。正常运行时通过原技能入口消费现有发布资产；没有自动重导用户制作稿。后续在原 Unity 游戏现场验证各技能的声音/粒子触发与取消；制作界面、设备输出和实际 Bundle 发布不在本轮隔离验收范围。
