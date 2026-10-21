# RO 三入口目标选择：客户端开发

## 需求与设计

用户要求把 RO 的点击敌方、瞄准切换、技能自动选敌三个入口接入本项目，类名、成员和方法名尽量一致，复杂机制简化。主 Agent 委派客户端开发，后端审核确认当前无需协议或表变更。正式方案见 `项目分析/RO_TARGET_SELECTION_IMPLEMENTATION.md`，参考分析见 `项目分析/RO_TARGET_SELECTION_FLOW.md`。

保留 RO `TouchObject`、`MSkillTargetMgr`、`MSkillTargetDefine` 与对应选择方法，将统一当前目标保存到现 `PlayerEntity.Target`。三入口共用当前世界、三十米距离、实际存活与可见性筛选，默认最近四个敌人循环。只使用现怪物类型作敌方，其他玩家暂没有阵营，不自动归敌。

当前正式技能仍无目标释放，Type 1/2 按技能时沿用或自动选敌，只辅助选择和水平朝向，没有敌人仍可释放。朝向在原技能准入及发送成功/原离线路径之后、预表现之前应用；发送异常不转向。Type 3 滑步及非攻击技能不自动选敌或转向。保留原技能发送、CD、跳跃、动作互断、F、根运动和移动同步流程。

## 修改文件

| 正式路径（项目根目录相对） | 修改内容 |
| --- | --- |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/TouchObject.cs` 及 `.meta` | 新增 RO 同名点击模块，射线模型层级映射、按下松开对账、UI 防点穿 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MSkillTargetDefine.cs` 及 `.meta` | 新增敌友候选、死活可见筛选、距离排序及 RO 同名血量排序接口 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MSkillTargetMgr.cs` 及 `.meta` | 新增统一选择、前四循环、技能自动找敌、目标失效清理与强类型 UI 通知 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs` | 新增当前目标属性，毁前/复用清理 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/SceneEntityWorld.cs` | 当前世界实体引用快照；被选实体移除回池前清目标 |
| `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Role/SceneWorldSystem.cs` | 快照与 RO 对应 GetOpponent 敌友查询接入 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs` | 现 Input System 鼠标点击和 Tab 选择采样；每帧检查当前目标 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs` | RO 同名 GetTargetFilterSkill 实际攻击自动选敌；瞄准/目标 UI 订阅和注销 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MNavigationComponent.cs` | 原准入和发送后、预表现前的攻击辅助水平转向；更正现移动同步注释 |
| `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/RoleCurrInfoWindow.cs` | 正式“瞄准 Tab”按钮与当前目标文本，复用既有中文字体 |

业务和 meta 共 13 路径，七个旧源修改，三个新源及三个新 meta；不改正式场景、预制体、字体、数据表、协议或服务器。旧文件按实施前原字节备份到 `_temp/ro-target-selection-20261012/client/before/`，不覆盖用户前次修改。

## 开发验证

- 本轮全量 GM_Game 280 源 Rebuild 通过，零错误、38 警告。基线 35 警告之外新增三条 `CS0436`，是当前已有 `MonsterEntity` 客户端/共享库同名冲突在新引用处的体现，编译器采用客户端真实怪物类型；未为消警告改共享库。独立验证待主 Agent 汇总。
- 修改差分检查新增代码未出现 namespace、foreach、三目、object/dynamic 弱载荷。新声明、参数、字段、函数与局部用途采用中文注释，新增三源各自一个类型并配唯一 meta GUID。
- 编译输入与正式末稿记录：`client/final-client-hashes.json`（13）、`compiled-source-hashes.json`（280）、`compiled-reference-hashes.json`（268）；`client-final.diff` 为实施前对照，`client-static-summary.json` 记录静态结果。
- 本轮临时编辑脚本、编译工程、日志、清单和备份全部在 `_temp/ro-target-selection-20261012/client/`，不把源码正式文件放入临时作为交付，不清理临时证据。

## 未解决边界与交接

当前正式主城没有生产怪物生成入口；真实怪物选择使用现世界工厂创建实体，在独立专用测试场景验证，不在正式主城添加 Mock。因此无敌人时显示“未选中目标”且攻击保持原无目标空放，不能宣称已接入目标伤害或正式主城已有敌人。

独立测试角色执行真实相机/Collider、Input System、EventSystem/UI 防点穿、目标有效性、循环切换、自动选择及 F/跳跃/CD/移动受影响回归；最终审查结果和四协作入口由主 Agent 更新。无自动寻路、选中即普攻、Buff 强制目标、头像系统、空气墙或导航落点拓展。未部署、重启正式服务或发布安装包。
