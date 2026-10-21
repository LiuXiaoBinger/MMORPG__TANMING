# 修改日志：客户端其余模块注释补充与废代码分析

- 日期：2026-10-12
- 执行人：客户端开发子 Agent
- 范围：`LYF_MMO/Assets/GM_Game/Scripts` 下 UI、controller、FunlModule、Common、Role、Manger、Data 子目录及根目录散落文件（不含 SceneEntity、不含 UI/test 测试脚本）
- 原则：只加中文注释，**不改变任何代码逻辑、不删除任何代码**；废代码只记录不删除。

## 一、本次加注释的文件清单（按子目录分组）

### Common（7 个）
- `Common/EnumDefine.cs`：补 WindowType/RoleState/RoleType 各枚举成员中文注释，加 RoleState 类摘要。
- `Common/ConstDefine.cs`：加类摘要与 BasePath/PrefabPath/EffectPath/SpritePath/ScenePath/RoleJobName 字段注释。
- `Common/Singleton.cs`：加类摘要与 instance/locker/Instance 字段说明。
- `Common/Utils/GameObjectUtils.cs`：加类摘要与两个 Show 扩展方法注释。
- `Common/Utils/MinMapHelp.cs`：加类摘要与 instance、Awake 注释。
- `Common/Utils/GhostEffect.cs`：加类摘要、Shader 属性缓存ID、MeshInfo/GhostObject 结构与字段、残影列表与材质懒加载字段注释。
- `Common/MCommonFunctions.cs`：加类摘要与自增ID、误差阈值、正弦/余弦表、NewId/UniqueToken 等说明。

### Manger（3 个）
- `Manger/PlayStateMgr.cs`：加类摘要与 stats 字段、init 注释。
- `Manger/NetErrorMsgMgr.cs`：加类摘要、Init、OnErrorMsgHandle 注释（替换只重复标识符的旧注释）。
- `Manger/ResourceMgr.cs`：加类摘要与 prefabDic/effectDic 注释。

### Role（10 个）
- `Role/RoleFSM.cs`：加类摘要与 _roleCtrl/_stateDic/构造注释。
- `Role/RoleFSMStateBase.cs`：加类摘要与 _animator/_roleCtrl/构造注释。
- `Role/RoleAnimaBehaviour.cs`：加类摘要与 _roleCtrl/_effectpos/AnimaEnd/DoAttackConfig 注释。
- `Role/FSM/IdleState.cs`、`RunState.cs`、`FastRunState.cs`、`JumpState.cs`、`AttackState.cs`、`HitState.cs`、`SliderState.cs`：加各类状态类摘要；AttackState 补 _atkindex/_obs/_atkType 注释；SliderState 补 AutoExitDelaySeconds 等字段注释。

### controller（7 个）
- `controller/CtrlBase.cs`：加类摘要与各虚方法、_view 字段注释。
- `controller/RoleController/NpcCtrl.cs`：加类摘要与 _npcEntity/NpcData/Initialize 注释。
- `controller/RoleController/MonsterCtrl.cs`、`OtherRoleCtrl.cs`：加类摘要（空壳类说明预留扩展点）。
- `controller/RoleController/RoleCtrlBase.cs`：加类摘要与 Animator/CharacterController/_roleState/_actionId/Highlight/目标/基础信息/LookAt/实体/状态机等字段注释。
- `controller/RoleController/MainRoleCtrl.cs`：加类摘要与输入/相机/移速/残影字段注释。

### FunlModule（3 个）
- `FunlModule/Bag/itemdef.cs`：给 ItemZhuDongGetResult 结构与字段加注释。
- `FunlModule/Bag/BagListener.cs`：加类摘要与字段/方法注释。
- `FunlModule/Bag/ItemTransition.cs`：加类摘要。

### UI（6 个）
- `UI/UIBase.cs`：加类摘要与 windowDic/InitView/Show 注释。
- `UI/WindowBase.cs`：加类摘要与各虚方法参数注释。
- `UI/UIRoot.cs`：加类摘要与 Instance/视图/控制器字段注释。
- `UI/Tips/SystemTips.cs`：加类摘要与字段/RefreshUI 注释。
- `UI/Style/UIDragWidget.cs`：加类摘要与拖拽相关字段注释。
- `UI/Style/ButtonStyle01.cs`：加类摘要与缩放/特效字段注释。

**合计加注释文件：36 个。**
其余文件（Global.cs、SceneMgr、NetSocketMgr、CameraMgr、MainCityMgr、NpcManager、TipsMgr、PlayerInputCtr、全部 ViewModel、MainCtrl/LoginCtrl/CreateRoleCtrl/SkillCtrl/KnapsackCtrl/RoleInfoCtrl/ShopCtrl、MoveComponent、BagTransition、RoleComponentBase 等）经通读确认已有完整中文注释，未做改动。
`controller/InputController/PlayerInput.cs` 为 Unity Input System 自动生成代码，按规范不改动。

## 二、废代码分类统计

详见 `项目分析/废代码分析报告-客户端-其余模块.md`。汇总：
- A 类（已确认无引用、可删除的 private 死方法/字段）：11 条
- B 类（大段注释掉的代码块）：3 处（其中 MCommonFunctions 约 140 行）
- C 类（空方法/仅注释体）：3 处（含 1 处疑似动画事件回调，建议保留）
- D 类（重复定义/需确认场景预制体引用）：4 处（两个 S_CameraController、MonsterCtrl、OtherRoleCtrl）
- E 类（文档占位类 Document.cs）：1 个
- F 类（疑似逻辑缺陷 space_still_need 重复累加）：1 处

## 三、验证结果

- 对全部 36 个改动文件执行静态检查：大括号 `{}` 配对、`#if/#endif` 配对，**全部 OK，0 个不匹配**。
- 回读核验了编辑过程中曾误删的 `RunState/FastRunState/HitState` 构造函数，已逐一恢复并确认存在，未遗留逻辑删除。
- 客户端无法命令行编译 Unity，以上为静态校验；建议在 Unity Editor 打开后做一次编译确认。

## 四、未解决问题 / 待办

1. D 类 4 个类是否被 .unity/.prefab 以 GUID 引用，需 Editor 内确认后再决定是否删除。
2. A 类废代码删除属代码改动，本任务只记录，待产品经理确认后单独下发删除任务。
3. F1（BagTransition 第 775 行 space_still_need 重复累加）疑似笔误，未改逻辑，待业务确认。
4. `RoItemComponent.cs` 等超大文件的私有辅助方法未逐行扫描，可后续补一轮。
