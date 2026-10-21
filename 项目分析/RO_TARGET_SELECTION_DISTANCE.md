# RO 自动选敌距离与技能射程

本轮用户希望无目标释放技能时自动找敌，先举攻击10米、选敌20米为例，随后明确这个倍数只是经验，要求先核RO算法。实际RO将选择半径和技能射程分别计算，普通技能按钮检索没有“攻击距离乘二”。本项目沿其当前可用选择流程，不移植未要求的完整导航战斗生态。

## 参考事实

参考根为外部 `F:/UnityPro/ro/Ro_cs422nb/`，下面路径相对该根：

| 入口 | 实际职责 |
| --- | --- |
| `clientcode/CSProject/MoonClient/UISystem/Ctrl/UISkillController.cs` 的 `GetTargetFilterSkill` | 根据技能敌友等目标类型优先当前有效目标，无效则调用 `FindEnemyBySkillBtn` |
| `clientcode/CSProject/MoonClient/UISystem/Ctrl/Skill/MSkillTargetMgr.cs` 的 `FindEnemyBySkillBtn` | 将 `MPlayerInfo.singleton.MaxChooseDistance` 传给 `FindAllTarget`，取排序后首个合格目标，不读取技能射程 |
| 同文件的 `FindEnemyByAimBtn` | 同样使用 `MaxChooseDistance`，前四个候选循环 |
| `clientcode/CSProject/MoonClient/Info/MAutoBattlePlayerInfo.cs` 的 `MaxChooseDistance` | 默认读取全局 `chooseTargetMaxDistance`；副本 `OverrideChooseDis > 0` 时替换 |
| `clientcode/CSProject/MoonClient/Table/Config/MGlobalConfig.cs` | `chooseTargetMaxDistance = GetInt("MaxChooseRange")` |
| `config/Table/CSV/GlobalTableFight.csv:34` | `MaxChooseRange=18`；表注明“选取目标的最大范围（半径、米）（通过客户端点选TAB或技能无目标检索时的范围）” |
| `clientcode/CSProject/MoonClient/Entity/Mgr/MEntityMgr.cs` 的 `GetOpponent` | 用可达寻路距离筛候选，距离小于选择半径且不超过黑暗Buff限制，再由目标模块按距离或血量排序 |

`GlobalTableFight.csv:33` 的 `TargetSelection=18` 是AI等入口默认搜索范围。两个配置用途有区别，本次普通技能按钮使用的是 `MaxChooseRange`。

## 射程是另一条规则

`clientcode/CSProject/MoonClient/Skill/MSkillCore.cs` 的 `UpRange` 基于效果配置 `FarRange`，按技能开关加武器/基础攻击距离、Buff固定与百分比加成，再限制到黑暗半径。它不是选择半径。

`UISystem/Ctrl/Skill/MWaitingDataMgr.cs` 的 `DoCastSkill` 将 `UpRange/LowRange` 传给导航；以自身为中心的技能上限按 `UpRange/2`。`Skill/MNavigationComponent.cs` 的 `isTargetInRange` 单独判高度，再以水平距离检查上下限，进入范围后执行施法回调。其默认上下限比较含0.01米容差。

RO还有等待阶段 `CheckValidEntity`：缓存目标失效时，以当前技能 `UpRange` 重新找可用目标。这是等待阶段的补选，不能误认为普通技能按钮初次搜索都使用 `UpRange`，也不是两倍射程。

因此，一个射程10米的技能可以先选中18米内的敌人，需要目标的释放再走进射程；普通默认18米搜索不会选中20米敌人。副本若覆盖为更大的选择距离，则可先选中更远敌人，真正攻击距离依旧独立。

## 本项目最小适配

上一轮 `FindEnemyBySkillBtn` 已采用统一 `MaxChooseDistance`，但30米是本项目简化参数，并非RO默认。本轮仅将 `MSkillTargetMgr.MaxChooseDistance` 校正为18米，保RO类/成员/方法命名，不新增倍数公式。点击、瞄准/Tab、技能无当前有效敌人时的候选搜索和超距清理共用这一半径。

本项目尚无RO导航距离与Buff体系，仍用现有三维直线距离，并包含恰好18米的边界。RO参考使用寻路距离且严格小于；此处保现简化距离比较，不为常量校正扩导航模块。

当前六正式技能仍为无目标表现，自动选敌只是目标及水平朝向辅助；没有敌人仍保原释放，滑步/轻功不参与。当前代码没有“自动追到射程再打”的实现，也没有目标命中伤害，选中更远目标不代表更远的命中范围。本轮不改技能表、协议、服务器或正式场景。

实施日志 `项目修改日志/2026-10-12-RO-TARGET-RANGE-CHANGELOG.md`；前轮实现 `项目分析/RO_TARGET_SELECTION_IMPLEMENTATION.md`。证据 `_temp/ro-target-range-20261012/`。

## 本轮验收

只修改 `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MSkillTargetMgr.cs` 的常量和两中文注释，共三行。独立完整280源编译0错38既有警告；完整真实业务DLL原生28项通过，17.99/18米包含、18.01米排除，自动/点击/真实Tab/超距清理共用18米。五正式攻击技能原AtkDistance分别8/10/12/30/30，均采用独立18米检索；仅隔离内存临时改AtkDistance为1与100并恢复，证明不存在乘二公式。F/轻功保持不选敌，审查无业务返工。

主核7类输入清单、开发7参考文件及2073保护共2440去重文件SHA一致，原生使用本轮编译DLL并有实际运行28项标记。报告 `_temp/ro-target-range-20261012/review/review-report.md`、审计 `final-audit.json` 与根 `root-manifest-check.json`。未重跑无影响Timer/根运动/后端/HUD，也不累计前次93项。

测试使用专用GO/Collider，禁用Global网络启动并设置隔离世界初始状态；SkillCtrl查询使用未初始化对象，配置真实bytes。正式主城无生产怪物，未验正式联机/异步模型资源包/目标伤害/安装包，未部署重启发布或清理证据；下一步重新运行正式游戏核选择入口。
