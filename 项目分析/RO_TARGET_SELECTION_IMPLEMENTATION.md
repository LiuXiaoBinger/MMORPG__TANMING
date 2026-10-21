# RO 三种目标选择的客户端接入

当前距离规则更新：后续核对RO正式表后，统一选择半径已由本轮最初30米校正为18米；下面30米为初次接入记录。技能无目标初次搜索与射程分别处理，算法及来源见 `项目分析/RO_TARGET_SELECTION_DISTANCE.md`，修改日志 `项目修改日志/2026-10-12-RO-TARGET-RANGE-CHANGELOG.md`。

## 范围与参考

本轮落实点击实体、瞄准按钮循环切换、攻击技能自动找敌三个入口。参考 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Input/TouchObject.cs`、`UISystem/Ctrl/Skill/MSkillTargetMgr.cs`、`MSkillTargetDefine.cs` 及 `UISkillController.GetTargetFilterSkill`，保留对应类名和方法名，继续使用项目现有 `PlayerEntity`、`SkillCtrl`，不为命名重构已有模块。

参考源码原流程说明见 `项目分析/RO_TARGET_SELECTION_FLOW.md`；后端与协议边界见 `项目分析/RO_TARGET_SELECTION_SERVER_BOUNDARY.md`。开发日志为 `项目修改日志/2026-10-12-RO-TARGET-SELECTION-CLIENT-CHANGELOG.md`，最终验收汇总见 `项目修改日志/2026-10-12-RO-TARGET-SELECTION-CHANGELOG.md`。

## 三个真实入口

| 玩家操作 | 当前调用链 | 与 RO 对应关系 |
| --- | --- | --- |
| 左键点击实体 | `PlayerInputCtr.PollTargetSelection → TouchObject.JudegeType → setPlayerTarget → MSkillTargetMgr.ResetTarget → PlayerEntity.Target` | 相机射线，按下与松开同一实体，检查最大选择距离后写当前目标 |
| 点击“瞄准 Tab”或按 Tab | `FindEnemyByAimBtn → MSkillTargetDefine.FindAllTarget → ResetTarget` | 在已排序的最近四个有效敌人中循环，最后一个返回第一个；无敌人则清空 |
| 按 Type 1/2 攻击技能快捷键 | `SkillCtrl.OnSkillKey → GetTargetFilterSkill → FindEnemyBySkillBtn → CastSkill → 原导航/发送/预表现` | 当前敌人有效则沿用，否则自动选择最近敌人 |

`TargetChanged` 是强类型实体通知，只更新正式 HUD 的目标文字。点击友方玩家可以查看昵称，按攻击时会重新筛成怪物敌方，不把友方作为敌人。选择本身不触发普攻、移动、伤害或服务器状态。

## 最小适配

- 现项目没有 RO 碰撞体 UID 命名及实体选择 Layer 配置，因此射线复用 32 条命中缓冲，并用实体模型根节点与碰撞体层级映射当前世界原实体；不改用户模型名称或碰撞体。
- `SceneEntityWorld.CopyEntities` 复制实体引用容器，`SceneWorldSystem.GetOpponent` 只筛当前世界实际存在的怪物敌方或角色友方。怪物由现 `MonsterEntity` 体系识别，覆盖精英和首领；其他玩家没有阵营数据，默认友方，NPC、假实体、宠物和佣兵不自动归敌。
- 最大选择距离简化为 `MSkillTargetMgr.MaxChooseDistance = 30f` 米；默认距离排序用三维直线距离，不移植导航路程、陷阱加权、自动战斗设置或 Lua 条件配置。保留 RO 同名 `SortTargetByRng`、`SortTargetByHP`，当前正式查找使用距离排序。
- `FindAllTarget/FindOneTarget` 保留敌友、距离、强类型条件函数参数，删除本项目没有业务来源的自动战斗 `filtrate` 参数；不创建空设置管理器。
- 目标必须仍为当前世界同一活跃实体，模型已加载、可见且活动。实际死亡判定：角色用 `BaseInfo`，怪物用 `AttrComp`；生命上限大于零且当前生命不大于零才判死亡。未初始化的 0/0 不判死亡，未改现全局 `MEntity.IsDead` 占位实现。
- 每帧仅检查已选目标有效性，不扫描候选列表。实体 `Remove` 回池前取消被选目标；本地角色销毁或复用清空 `Target`，输入禁用清理尚未松开的点击。
- UI 阻挡使用当前屏幕坐标 `EventSystem.RaycastAll` 检查 `GraphicRaycaster`，防止技能和瞄准按钮点击穿到场景；不把 `PhysicsRaycaster` 世界命中误当 UI。
- 正式 `RoleCurrInfoWindow` 初始化时创建一次瞄准按钮和目标提示，复用项目现 `Resources/UI/Font/Common SDF` 字体。文字关闭射线，按钮背景保留射线。控制器订阅与注销按钮、目标变化通知，不挂测试组件、不加调试 HUD。

## 无目标技能与辅助转向

目前六个正式技能效果表均为 `TargetMode = 0`，协议仍只有技能 ID 与 UUID，服务器只支持已有无目标技能。本次攻击选敌作为当前目标和本地朝向辅助，沿现技能事件传递目标，不上传本端 allocator 生成的 UID，不加入目标伤害、跨端 UID 映射或目标施法门禁。

原 `MNavigationComponent.OnAfterAction` 的 `CanStart` 校验通过后，保留原发送流程；发送成功或原无 Client 离线路径继续时，在 `BeginPrediction` 前水平朝向所选敌人。发送抛异常、跳跃、MP 不足、CD 或动作准入失败均不抢施法朝向。目标重合时保持原朝向，施法点仍按原角色位置，不移植地面选点。

Type 3 滑步与其他非攻击类别不自动找敌，也不根据目标转向；没有敌人仍走现无目标技能释放，保留 F、跳跃、CD、技能根运动、移动同步、服务器回包和原离线语义，不增加确认等待、重试或输入缓存。

## 正式场景边界

当前正式主城只有本地玩家、其他玩家和 NPC，没有生产怪物生成来源。三个入口已针对世界中真实怪物实例接入；正式主城无怪物时瞄准不会凭空找到敌人，攻击仍按原方式释放。本次不为演示在正式场景添加 Mock 怪物，不新增刷怪、战斗生态或后端目标结算。独立测试在专用隔离场景创建实体，不能将其描述为正式主城已有敌人或完成目标伤害联机。

## 验证交接

开发当前全量 280 个 GM_Game 源文件 Rebuild 为零错误、38 警告。原 35 警告外，本轮三个 `MonsterEntity` 引用新增 `CS0436`：现客户端源类型与 `MMoRpgCommon` 历史导入同名，编译器明确选用客户端 `SceneEntity/Entity/MonsterEntity.cs`；未为消警告改共享库。

开发证据统一位于 `_temp/ro-target-selection-20261012/client/`：`changed-client-paths.json`、七个旧业务文件事前备份、13 路径末稿 SHA、280 实际编译源 SHA、268 HintPath 引用 SHA、全量编译日志、逐文件差分和静态规范结果。

独立末稿280源编译0错38警告，完整真实业务DLL的Unity原生57项和72生产源薄边界技能/移动回归36项通过，合计93；审查无业务返工。原生使用专用GO/Collider而未加载正式异步模型/资源包，Global启动和网络未执行；回归外围为具名薄边界。HUD以正式角色子树与字体隔离渲染，四个目标控件方法逐字提取；截图目检中文、位置、F最右通过，不等于完整正式HUD联机。

主核11类清单3792去重路径SHA、原生DLL与独立编译、四控件方法和运行标记一致；2067保护仅七授权既有源变化，其余保持。报告 `_temp/ro-target-selection-20261012/review/review-report.md`、审计 `final-audit.json` 与根 `root-manifest-check.json`。所有首次失败保留，只有临时依赖/夹具/缓存或末尾注释重新编译，未为测试扩大业务。

协作四入口已更新。下一步正式重新运行查看提示与选择入口；主城无正式怪物/目标协议/伤害结算，未验正式联机或安装包，未部署发布、重启或清理临时证据。
