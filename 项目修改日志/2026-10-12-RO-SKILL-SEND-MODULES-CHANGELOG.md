# 2026-10-12 RO 技能发送模块接入

## 需求与设计

用户要求参考 RO 的释放技能流程，编写发送经过的模块，未实现功能保留空入口。源码事实与流程图见 `项目分析/RO_SKILL_SEND_MODULE_FLOW.md`。

由 `SkillCtrl` 直接发包改为 `MSkillClassicsHandler.CastSkill` → `MWaitingDataMgr.SetWaitSkill/DoCastSkill` → `MEvent_Navigation` → `MNavigationComponent.OnGetEvent/closeTargetAfter/OnAfterAction`。发送前调用 RO 同名 `PlayerEntity.UpdateMove`，发包及本地预测沿用现有实现。没有新增等待状态、请求队列、确认等待、额外代次或目标协议。

主 Agent 负责源码核对、范围限定、验收和文档；客户端由 `roll_movement_check` 开发，`roll_local_move_review` 独立审查和验证。

## 修改文件

新增以下五个业务类型及各自同路径 `.meta`：

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillClassicsHandler.cs`：RO 同名经典释放入口，交接当前已学技能。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MWaitingDataMgr.cs`：RO 同名提交与导航事件组装入口，本轮即时交接，不实现缓存等待。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MNavigationComponent.cs`：实体普通导航组件，沿 RO 到达回调发送原技能请求并启动原预测；`Interrupt` 空体。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/MSkillEventArgs.cs`：技能 ID、施法点和目标的强类型同步输入载荷。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/MNavigationEventArgs.cs`：持有技能到达动作，随现有分发器回收，不跨帧保存。

修改以下五个既有业务源文件：

- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs`：快捷键改走模块链，移除原直接发送方法，保留即时界面刷新；增加 RO 同名 `GetTargetFilterSkill`，当前返回空目标。原回包处理保持。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEntity.cs`：增加 RO 同名 `NavComp`，从现有组件容器查询，不再存一份实例。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Builder/PlayerEntityBuilder.cs`：创建并挂载本地主角导航组件一次。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/MEventType.cs`：末尾追加 `MEvent_Navigation`，已有事件编号保持。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`：新增发送前位置同步 `UpdateMove` 空入口，不执行本地移动或新增网络协议。

协作记录更新 `Docs/AI/CURRENT_TASK.md`、`TEST_REPORT.md`、`PROJECT_CONTEXT.md`、`DECISIONS.md`；正式分析和本日志归档于根目录指定分类。

## 验证

- 完整客户端 277 个生产源码 Rebuild：零错误，37 条既有警告。编译产物及日志在 `_temp/ro-skill-send-flow-20261012/develop/`。
- 主 Agent 对照本轮修改前副本检查五个既有文件，变更仅涉及上述入口；核对 277 个编译源 SHA，未发现输入漂移。
- `roll_local_move_review` 独立最终审查 PASS，无源码问题无需返工；`review/final-audit.json`、`send-flow-runtime-final.log`、`previous-regression.log` 已核，25项发送链专项和24项既有滑步回归通过。覆盖导航一次派发/一次发送、UpdateMove→发包→同UUID预测顺序、同步事件回收、空目标即时提交、原CD/动作/跳跃/Type3取消许可、发送失败及组件卸载/池复用。
- 文档收口实核277编译源、15正式路径、72专项生产源和49运行输入，共413项SHA256零漂移；四份清单摘要与独立final-audit一致，结果扩充到 `_temp/ro-skill-send-flow-20261012/root-manifest-check.json`。
- 实体、导航、事件分发器与组件池执行真实代码；`PlayerEntity.UpdateMove` 空入口采用记录器验证调用顺序，Builder静态核对，Unity/UI/网络为薄测试边界。首测试把既有缺组件Getter抛KeyNotFound误当返回null，仅改_temp断言为Contains，正式业务保持，失败日志保留。
- 本轮结果由原执行者`roll_local_move_review`完成；`skill_flow_records`只整理既有PASS证据和重算SHA，不重跑或伪称新增独立测试。未进行正式编辑器场景联机或安装包验收。

## 未实现功能及交接

本轮不实现选择目标、路径移动、自动追击、自动普攻、技能缓存或发送前位置同步。当前导航收到技能指令即时进入到达回调，不等待空功能。函数参数按本项目类型和现有协议作必要适配，不照搬 RO 的 C++、Lua、Buff、气墙、骑乘或吟唱依赖。

尚未进行正式编辑器场景联机或打包验收，也未替换运行产物。所有临时验证文件统一保留在 `_temp/ro-skill-send-flow-20261012/`。

